using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static partial class Ps2CharacterDatEditor
{
    public static int ReplaceBinTextureIndex(string datPath, int binEntryIndex, int oldTextureIndex, int newTextureIndex)
    {
        if (oldTextureIndex is < -1 or > 254) throw new ArgumentOutOfRangeException(nameof(oldTextureIndex));
        if (newTextureIndex is < -1 or > 254) throw new ArgumentOutOfRangeException(nameof(newTextureIndex));
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, binEntryIndex);
        if (!tag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{binEntryIndex:D3} não é um BIN.");
        if (length < 0x10) throw new InvalidDataException("BIN muito pequeno.");
        ushort materialCount = BitConverter.ToUInt16(dat, start + 0x0A);
        uint materialOffset = BitConverter.ToUInt32(dat, start + 0x0C);
        long table = start + materialOffset;
        if (materialCount == 0 || materialOffset == 0 || table < start || table + materialCount * 16L > start + length)
            throw new InvalidDataException("Tabela de materiais do BIN inválida.");
        byte oldRaw = oldTextureIndex < 0 ? (byte)0xFF : checked((byte)oldTextureIndex);
        byte newRaw = newTextureIndex < 0 ? (byte)0xFF : checked((byte)newTextureIndex);
        int changed = 0;
        for (int i = 0; i < materialCount; i++)
        {
            int diffuseOffset = checked((int)table + i * 16 + 1);
            if (dat[diffuseOffset] != oldRaw) continue;
            dat[diffuseOffset] = newRaw; changed++;
        }
        if (changed == 0) throw new InvalidOperationException($"Nenhum material usando o índice {oldTextureIndex} foi encontrado neste BIN.");
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) File.Copy(datPath, backup, false);
        string staged = datPath + ".tmp"; File.WriteAllBytes(staged, dat); File.Move(staged, datPath, true);
        return changed;
    }

    public static void RestoreTplEntry(string datPath, int tplEntryIndex, byte[] tplData)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        CommitTpl(datPath, dat, start, length, tplData);
    }

    public static void ReplaceTextureFromPng(string datPath, int tplEntryIndex, int textureIndex, string pngPath)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        if (!File.Exists(pngPath)) throw new FileNotFoundException("PNG não encontrado.", pngPath);

        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");

        string temp = Path.Combine(Path.GetTempPath(), $"re4_character_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(temp, dat.AsSpan(start, length).ToArray());
            new TextureWorkspaceService().ReplaceFromImage(temp, textureIndex, pngPath);
            byte[] replacement = File.ReadAllBytes(temp);
            if (replacement.Length != length)
                throw new InvalidDataException($"A textura alterou o tamanho do TPL ({length:N0} → {replacement.Length:N0} bytes). A gravação no DAT foi cancelada.");

            string backup = datPath + ".bak";
            if (!File.Exists(backup)) File.Copy(datPath, backup, false);
            Buffer.BlockCopy(replacement, 0, dat, start, length);
            string staged = datPath + ".tmp";
            File.WriteAllBytes(staged, dat);
            File.Move(staged, datPath, true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }

    public static void CopyTextureFromDat(string targetDatPath, int targetTplEntry, int targetTextureIndex, string sourceDatPath, int sourceTplEntry, int sourceTextureIndex)
    {
        byte[] targetDat = File.ReadAllBytes(targetDatPath);
        (int targetStart, int targetLength, string targetTag) = GetEntry(targetDat, targetTplEntry);
        if (!targetTag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada TPL de destino é inválida.");
        byte[] sourceTpl = CaptureTplEntry(sourceDatPath, sourceTplEntry);
        string targetTemp = Path.Combine(Path.GetTempPath(), $"re4_mesh_tex_target_{Guid.NewGuid():N}.tpl");
        string sourceTemp = Path.Combine(Path.GetTempPath(), $"re4_mesh_tex_source_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(targetTemp, targetDat.AsSpan(targetStart, targetLength).ToArray());
            File.WriteAllBytes(sourceTemp, sourceTpl);
            var reader = new TplReader();
            new TplWriter(reader).ReplaceTexture(targetTemp, targetTextureIndex, sourceTemp, sourceTextureIndex);
            CommitTpl(targetDatPath, targetDat, targetStart, targetLength, File.ReadAllBytes(targetTemp));
        }
        finally
        {
            try { if (File.Exists(targetTemp)) File.Delete(targetTemp); } catch { }
            try { if (File.Exists(sourceTemp)) File.Delete(sourceTemp); } catch { }
        }
    }

    public static void TransformTexture(string datPath, int tplEntryIndex, int textureIndex, TextureTransform transform)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");

        string temp = Path.Combine(Path.GetTempPath(), $"re4_character_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(temp, dat.AsSpan(start, length).ToArray());
            var service = new TextureWorkspaceService();
            using Bitmap bitmap = service.Decode(temp, textureIndex);
            bitmap.RotateFlip(transform switch
            {
                TextureTransform.Rotate90 => RotateFlipType.Rotate90FlipNone,
                TextureTransform.FlipX => RotateFlipType.RotateNoneFlipX,
                TextureTransform.FlipY => RotateFlipType.RotateNoneFlipY,
                _ => RotateFlipType.RotateNoneFlipNone
            });
            service.ReplaceFromBitmap(temp, textureIndex, bitmap, preserveDimensions: transform != TextureTransform.Rotate90);
            CommitTpl(datPath, dat, start, length, File.ReadAllBytes(temp));
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }

    public static void ResizeTexture(string datPath, int tplEntryIndex, int textureIndex, int width, int height, TextureResizeResampling resampling)
    {
        EditEmbeddedTpl(datPath, tplEntryIndex, (temp, service) =>
        {
            using Bitmap source = service.Decode(temp, textureIndex);
            using var resized = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = resampling switch
                {
                    TextureResizeResampling.NearestNeighbor => System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor,
                    TextureResizeResampling.Bilinear => System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear,
                    _ => System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                };
                graphics.PixelOffsetMode = resampling == TextureResizeResampling.NearestNeighbor
                    ? System.Drawing.Drawing2D.PixelOffsetMode.Half
                    : System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
            }
            service.ReplaceFromBitmap(temp, textureIndex, resized, preserveDimensions: false);
        });
    }

    public static void ConvertTextureBitDepth(string datPath, int tplEntryIndex, int textureIndex, int colors) =>
        EditEmbeddedTpl(datPath, tplEntryIndex, (temp, service) => service.ConvertBitDepth(temp, textureIndex, colors));

    private static void EditEmbeddedTpl(string datPath, int tplEntryIndex, Action<string, TextureWorkspaceService> edit)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        string temp = Path.Combine(Path.GetTempPath(), $"re4_character_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(temp, dat.AsSpan(start, length).ToArray());
            edit(temp, new TextureWorkspaceService());
            CommitTpl(datPath, dat, start, length, File.ReadAllBytes(temp));
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }

    public static void RestoreTextureFromBackup(string datPath, int tplEntryIndex, int textureIndex)
    {
        string backupPath = datPath + ".bak";
        if (!File.Exists(backupPath)) throw new FileNotFoundException("O backup original do DAT ainda não existe.", backupPath);
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        byte[] originalTpl = CaptureTplEntry(backupPath, tplEntryIndex);
        string targetTemp = Path.Combine(Path.GetTempPath(), $"re4_restore_target_{Guid.NewGuid():N}.tpl");
        string sourceTemp = Path.Combine(Path.GetTempPath(), $"re4_restore_source_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(targetTemp, dat.AsSpan(start, length).ToArray());
            File.WriteAllBytes(sourceTemp, originalTpl);
            var reader = new TplReader();
            new TplWriter(reader).ReplaceTexture(targetTemp, textureIndex, sourceTemp, textureIndex);
            CommitTpl(datPath, dat, start, length, File.ReadAllBytes(targetTemp));
        }
        finally
        {
            try { if (File.Exists(targetTemp)) File.Delete(targetTemp); } catch { }
            try { if (File.Exists(sourceTemp)) File.Delete(sourceTemp); } catch { }
        }
    }

}
