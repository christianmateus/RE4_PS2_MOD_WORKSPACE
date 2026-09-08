using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE.Core.Dat;

/// <summary>Native reader/writer for the little-endian RE4 PS2 DAT container.</summary>
public static partial class NativeDatService
{
    private const int FixedHeaderSize = 0x10;
    private const int Alignment = 0x20;
    private const int MaxEntryCount = 100_000;

    public static async Task<DatExtractionResult> ExtractAsync(string datPath, string outputDirectory) =>
        await Task.Run(() => Extract(datPath, outputDirectory));

    public static DatExtractionResult Extract(string datPath, string outputDirectory)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        byte[] data = File.ReadAllBytes(datPath);
        DatArchive archive = Parse(data, datPath);

        string datName = Path.GetFileName(datPath);
        string baseName = Path.GetFileNameWithoutExtension(datName);
        string entriesDirectory = Path.Combine(outputDirectory, baseName);
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(entriesDirectory);

        string localDat = Path.Combine(outputDirectory, datName);
        if (!Path.GetFullPath(datPath).Equals(Path.GetFullPath(localDat), StringComparison.OrdinalIgnoreCase))
            File.Copy(datPath, localDat, true);

        int digits = archive.Entries.Count >= 100 ? 3 : 2;
        var manifest = new StringBuilder().Append("FileCount = ").Append(archive.Entries.Count).Append("\r\n");
        foreach (DatEntry entry in archive.Entries)
        {
            string extension = entry.Type.Length == 0 ? "DMY" : entry.Type;
            string fileName = $"{baseName}_{entry.Index.ToString($"D{digits}")}.{extension}";
            string relativePath = baseName + "\\" + fileName;
            manifest.Append("File_").Append(entry.Index).Append(" = ").Append(relativePath).Append("\r\n");
            File.WriteAllBytes(Path.Combine(entriesDirectory, fileName), entry.Data);
        }
        string idxPath = Path.Combine(outputDirectory, baseName + ".idx");
        File.WriteAllText(idxPath, manifest.ToString(), new UTF8Encoding(false));
        return new DatExtractionResult(localDat, idxPath, entriesDirectory, archive.Entries.Count);
    }

    public static async Task<DatRepackResult> RepackAsync(string contentDirectory, string datName, string stagingDirectory, string outputDatPath) =>
        await Task.Run(() => Repack(contentDirectory, datName, stagingDirectory, outputDatPath));

    public static DatRepackResult Repack(string contentDirectory, string datName, string stagingDirectory, string outputDatPath)
    {
        if (!Directory.Exists(contentDirectory)) throw new DirectoryNotFoundException("A pasta Content do DAT não foi encontrada: " + contentDirectory);
        if (string.IsNullOrWhiteSpace(datName) || Path.GetFileName(datName) != datName) throw new ArgumentException("Nome do DAT inválido.", nameof(datName));
        string baseName = Path.GetFileNameWithoutExtension(datName);
        string idxPath = Path.Combine(contentDirectory, baseName + ".idx");
        if (!File.Exists(idxPath)) throw new FileNotFoundException("Manifesto IDX do DAT não encontrado.", idxPath);

        List<ManifestEntry> entries = ReadManifest(idxPath);
        if (entries.Count == 0) throw new InvalidDataException("O manifesto IDX não contém entradas.");
        int tableEnd = checked(FixedHeaderSize + entries.Count * 8);
        int dataStart = Align(tableEnd);
        var payloads = new byte[entries.Count][];
        long totalSize = dataStart;
        for (int i = 0; i < entries.Count; i++)
        {
            string path = ResolveManifestPath(contentDirectory, entries[i].RelativePath);
            payloads[i] = File.Exists(path) ? File.ReadAllBytes(path) : throw new FileNotFoundException($"Entrada DAT #{i} não encontrada.", path);
            totalSize = checked(totalSize + Align(payloads[i].Length));
        }
        if (totalSize > int.MaxValue) throw new InvalidDataException("DAT reconstruído excede o limite de 2 GB.");

        byte[] output = new byte[(int)totalSize];
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(0, 4), (uint)entries.Count);
        int cursor = dataStart;
        for (int i = 0; i < entries.Count; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(FixedHeaderSize + i * 4, 4), (uint)cursor);
            WriteType(output.AsSpan(FixedHeaderSize + entries.Count * 4 + i * 4, 4), entries[i].Type);
            payloads[i].CopyTo(output, cursor);
            cursor += Align(payloads[i].Length);
        }

        Directory.CreateDirectory(stagingDirectory);
        string stagedDat = Path.Combine(stagingDirectory, datName);
        File.WriteAllBytes(stagedDat, output);
        Directory.CreateDirectory(Path.GetDirectoryName(outputDatPath) ?? throw new InvalidOperationException("Destino do DAT inválido."));
        File.Copy(stagedDat, outputDatPath, true);
        long oldSize = File.Exists(Path.Combine(contentDirectory, datName)) ? new FileInfo(Path.Combine(contentDirectory, datName)).Length : 0;
        return new DatRepackResult(outputDatPath, oldSize, output.Length, stagingDirectory, entries.Count);
    }

    public static DatArchive Read(string datPath) => Parse(File.ReadAllBytes(datPath), datPath);

    private static DatArchive Parse(byte[] data, string sourcePath)
    {
        if (data.Length < FixedHeaderSize) throw new InvalidDataException("DAT menor que o cabeçalho mínimo de 0x10 bytes.");
        uint rawCount = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4));
        if (rawCount == 0 || rawCount > MaxEntryCount) throw new InvalidDataException($"Quantidade de entradas DAT inválida: {rawCount}.");
        int count = (int)rawCount;
        int tableEnd = checked(FixedHeaderSize + count * 8);
        if (tableEnd > data.Length) throw new InvalidDataException("Tabelas do DAT ultrapassam o arquivo.");
        int tagsStart = FixedHeaderSize + count * 4;
        var offsets = new uint[count];
        for (int i = 0; i < count; i++) offsets[i] = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(FixedHeaderSize + i * 4, 4));
        if (offsets[0] < tableEnd || offsets[0] > data.Length) throw new InvalidDataException("Primeiro offset do DAT é inválido.");

        var entries = new List<DatEntry>(count);
        for (int i = 0; i < count; i++)
        {
            uint start = offsets[i];
            uint end = i + 1 < count ? offsets[i + 1] : (uint)data.Length;
            if (start > end || end > data.Length) throw new InvalidDataException($"Intervalo inválido na entrada DAT #{i}: 0x{start:X}-0x{end:X}.");
            string type = ReadType(data.AsSpan(tagsStart + i * 4, 4), i);
            entries.Add(new DatEntry(i, type, start, data.AsSpan((int)start, (int)(end - start)).ToArray()));
        }
        return new DatArchive(sourcePath, entries);
    }

    private static List<ManifestEntry> ReadManifest(string idxPath)
    {
        string[] lines = File.ReadAllLines(idxPath);
        Match countMatch = lines.Select(line => FileCountRegex().Match(line)).FirstOrDefault(match => match.Success)
            ?? throw new InvalidDataException("FileCount ausente no IDX.");
        int count = int.Parse(countMatch.Groups[1].Value);
        if (count <= 0 || count > MaxEntryCount) throw new InvalidDataException($"FileCount inválido no IDX: {count}.");
        var byIndex = new Dictionary<int, ManifestEntry>();
        foreach (string line in lines)
        {
            Match match = FileEntryRegex().Match(line);
            if (!match.Success) continue;
            int index = int.Parse(match.Groups[1].Value);
            string relative = match.Groups[2].Value.Trim();
            if (index < 0 || index >= count || !byIndex.TryAdd(index, new ManifestEntry(index, relative, TypeFromPath(relative))))
                throw new InvalidDataException($"Entrada inválida ou duplicada no IDX: {line}");
        }
        if (byIndex.Count != count) throw new InvalidDataException($"IDX declara {count} entradas, mas descreve {byIndex.Count}.");
        return Enumerable.Range(0, count).Select(i => byIndex[i]).ToList();
    }

    private static string ResolveManifestPath(string root, string relativePath)
    {
        string normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized)) throw new InvalidDataException("O IDX contém um caminho absoluto não permitido: " + relativePath);
        string fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("O IDX aponta para fora da pasta Content: " + relativePath);
        return fullPath;
    }

    private static string TypeFromPath(string path)
    {
        string extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        if (extension == "DMY") return string.Empty;
        if (extension.Length is < 1 or > 3 || extension.Any(c => c is < 'A' or > 'Z')) throw new InvalidDataException("Tipo de entrada DAT inválido: " + extension);
        return extension;
    }

    private static string ReadType(ReadOnlySpan<byte> raw, int index)
    {
        int length = raw.IndexOf((byte)0);
        if (length < 0) length = raw.Length;
        if (length == 0) return string.Empty;
        string type = Encoding.ASCII.GetString(raw[..length]).ToUpperInvariant();
        if (type.Length > 3 || type.Any(c => c is < 'A' or > 'Z')) throw new InvalidDataException($"Tipo inválido na entrada DAT #{index}.");
        return type;
    }

    private static void WriteType(Span<byte> destination, string type)
    {
        destination.Clear();
        if (type.Length > 0) Encoding.ASCII.GetBytes(type, destination);
    }

    private static int Align(int value) => checked((value + Alignment - 1) & ~(Alignment - 1));

    [GeneratedRegex(@"^\s*FileCount\s*=\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileCountRegex();

    [GeneratedRegex(@"^\s*File_(\d+)\s*=\s*(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileEntryRegex();

    private sealed record ManifestEntry(int Index, string RelativePath, string Type);
}

public sealed record DatArchive(string SourcePath, IReadOnlyList<DatEntry> Entries);
public sealed record DatEntry(int Index, string Type, uint Offset, byte[] Data);
public sealed record DatExtractionResult(string DatPath, string IdxPath, string EntriesDirectory, int EntryCount);
public sealed record DatRepackResult(string OutputDatPath, long OldSize, long NewSize, string StagingDirectory, int EntryCount);
