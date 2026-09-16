using System.Buffers.Binary;
using RE4_PS2_MOD_WORKSPACE.Core.Effects;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures;

public static class EffTextureService
{
    public static IReadOnlyList<EffTplPackageInfo> ReadPackages(string path) => Parse(File.ReadAllBytes(path));

    public static void ExtractTpl(string path, int index, string output)
    {
        byte[] data = File.ReadAllBytes(path);
        EffTplPackageInfo info = At(Parse(data), index);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output, data.AsSpan(info.Offset, info.Length).ToArray());
    }

    public static EffTplPackageInfo InjectTpl(string path, int index, string tplPath, string? backupPath = null)
    {
        byte[] source = File.ReadAllBytes(path);
        byte[] replacement = File.ReadAllBytes(tplPath);
        ValidateTpl(replacement);
        IReadOnlyList<EffTplPackageInfo> packages = Parse(source);
        EffTplPackageInfo target = At(packages, index);
        int delta = checked(replacement.Length - target.Length);
        byte[] rebuilt = new byte[checked(source.Length + delta)];
        Buffer.BlockCopy(source, 0, rebuilt, 0, target.Offset);
        Buffer.BlockCopy(replacement, 0, rebuilt, target.Offset, replacement.Length);
        Buffer.BlockCopy(source, target.Offset + target.Length, rebuilt, target.Offset + replacement.Length, source.Length - target.Offset - target.Length);

        int table5 = checked((int)U32(rebuilt, 0x18));
        for (int i = index + 1; i < packages.Count; i++)
        {
            int field = checked(table5 + 4 + i * 4);
            W32(rebuilt, field, checked((uint)(U32(rebuilt, field) + delta)));
        }
        if (delta != 0)
            for (int i = 6; i < 11; i++)
            {
                int field = 4 + i * 4;
                uint offset = U32(rebuilt, field);
                if (offset != 0) W32(rebuilt, field, checked((uint)(offset + delta)));
            }

        if (!string.IsNullOrWhiteSpace(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(backupPath))!);
            File.Copy(path, backupPath, true);
        }
        string temp = path + ".workspace_tmp";
        try
        {
            File.WriteAllBytes(temp, rebuilt);
            _ = Ps2EffReader.Read(temp);
            _ = Parse(File.ReadAllBytes(temp));
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return ReadPackages(path)[index];
    }

    private static IReadOnlyList<EffTplPackageInfo> Parse(byte[] data)
    {
        if (data.Length < 0x30 || U32(data, 0) != 11) throw new InvalidDataException("EFF invalido.");
        int table5 = checked((int)U32(data, 0x18));
        int table6 = checked((int)U32(data, 0x1C));
        if (table5 == 0) return Array.Empty<EffTplPackageInfo>();
        if (table5 < 0x30 || table6 <= table5 || table6 > data.Length) throw new InvalidDataException("Tabela TPL do EFF invalida.");
        int count = checked((int)U32(data, table5));
        if (count > 4096 || table5 + 4L + count * 4L > table6) throw new InvalidDataException("Tabela TPL do EFF truncada.");
        var result = new List<EffTplPackageInfo>(count);
        for (int i = 0; i < count; i++)
        {
            int start = checked(table5 + (int)U32(data, table5 + 4 + i * 4));
            int end = i + 1 < count ? checked(table5 + (int)U32(data, table5 + 8 + i * 4)) : table6;
            if (start < table5 + 4 + count * 4 || end <= start || end > table6) throw new InvalidDataException($"Pacote TPL #{i:D3} invalido.");
            ValidateTpl(data.AsSpan(start, end - start));
            result.Add(new(i, start, end - start, checked((int)U32(data, start + 4))));
        }
        return result;
    }

    private static EffTplPackageInfo At(IReadOnlyList<EffTplPackageInfo> items, int index)
    {
        if ((uint)index >= (uint)items.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return items[index];
    }

    private static void ValidateTpl(ReadOnlySpan<byte> data)
    {
        if (data.Length < 0x10) throw new InvalidDataException("TPL do EFF truncado.");
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4, 4));
        if (count == 0 || count > 4096 || 0x10L + count * 0x30L > data.Length) throw new InvalidDataException("TPL do EFF invalido.");
    }

    private static uint U32(byte[] data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
    private static void W32(byte[] data, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);
}

public sealed record EffTplPackageInfo(int PackageIndex, int Offset, int Length, int TextureCount);
