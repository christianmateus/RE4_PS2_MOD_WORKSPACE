namespace RE4_PS2_MOD_WORKSPACE.Core.Smd;

public static class SmdTextureService
{
    public static SmdTextureInfo ReadInfo(string smdPath)
    {
        if (string.IsNullOrWhiteSpace(smdPath)) throw new ArgumentException("Caminho do SMD inválido.", nameof(smdPath));
        if (!File.Exists(smdPath)) throw new FileNotFoundException("Arquivo SMD não encontrado.", smdPath);

        using var fs = new FileStream(smdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (fs.Length < 0x0C) throw new InvalidDataException("O arquivo é pequeno demais para conter um cabeçalho SMD válido.");

        using var br = new BinaryReader(fs);
        fs.Position = 0x08;
        uint textureOffset = br.ReadUInt32();
        long tplStart = (long)textureOffset + 0x10;
        if (tplStart < 0 || tplStart > fs.Length)
            throw new InvalidDataException($"Offset de textura inválido no SMD: 0x{textureOffset:X8} (TPL em 0x{tplStart:X}).");

        return new SmdTextureInfo(textureOffset, tplStart, fs.Length - tplStart, fs.Length);
    }

    public static SmdTextureInfo ExtractTpl(string smdPath, string tplPath)
    {
        var info = ReadInfo(smdPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(tplPath))!);

        using var input = new FileStream(smdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        input.Position = info.TplStart;
        using var output = new FileStream(tplPath, FileMode.Create, FileAccess.Write, FileShare.None);
        input.CopyTo(output);
        return info;
    }

    public static SmdTextureInfo InjectTpl(string smdPath, string tplPath, string? backupPath = null)
    {
        if (!File.Exists(tplPath)) throw new FileNotFoundException("Arquivo TPL não encontrado.", tplPath);
        var info = ReadInfo(smdPath);

        if (!string.IsNullOrWhiteSpace(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(backupPath))!);
            File.Copy(smdPath, backupPath, true);
        }

        string temp = smdPath + ".workspace_tmp";
        try
        {
            using (var input = new FileStream(smdPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CopyBytes(input, output, info.TplStart);
                using var tpl = new FileStream(tplPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                tpl.CopyTo(output);
            }

            File.Move(temp, smdPath, true);
            return ReadInfo(smdPath);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public static bool TplMatchesSmd(string smdPath, string tplPath)
    {
        if (!File.Exists(tplPath)) return true;
        var info = ReadInfo(smdPath);
        var tpl = new FileInfo(tplPath);
        if (tpl.Length != info.TplSize) return false;
        string smdHash = ChangeDetectionService.HashStreamSegment(smdPath, info.TplStart, info.TplSize);
        string tplHash = ChangeDetectionService.HashFile(tplPath);
        return string.Equals(smdHash, tplHash, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyBytes(Stream input, Stream output, long count)
    {
        byte[] buffer = new byte[1024 * 1024];
        long remaining = count;
        while (remaining > 0)
        {
            int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) throw new EndOfStreamException("Fim inesperado ao copiar o cabeçalho do SMD.");
            output.Write(buffer, 0, read);
            remaining -= read;
        }
    }
}

public sealed record SmdTextureInfo(uint TextureOffset, long TplStart, long TplSize, long SmdSize);
