using System.Text.Json;
using RE4_PS2_MOD_WORKSPACE.Core.Dat;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

public sealed record FcvCatalogEntry(string Path, byte[]? EmbeddedData)
{
    public string FileName => System.IO.Path.GetFileName(Path);
    public FcvAnimation Read() => EmbeddedData is null ? FcvReader.Read(Path) : FcvReader.Read(EmbeddedData, Path);
}

public static class FcvCatalog
{
    public static IReadOnlyList<FcvCatalogEntry> List(string datPath)
    {
        string stem = Path.GetFileNameWithoutExtension(datPath);
        string directory = Path.GetDirectoryName(datPath)!;
        var entries = new List<FcvCatalogEntry>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            DatArchive archive = NativeDatService.Read(datPath);
            foreach (DatEntry entry in archive.Entries.Where(x => x.Type.Equals("FCV", StringComparison.OrdinalIgnoreCase)))
            {
                string fileName = $"{stem}_{entry.Index:D2}.FCV";
                entries.Add(new FcvCatalogEntry(Path.Combine(directory, fileName), entry.Data));
                identities.Add(Identity(fileName));
            }
        }
        catch (InvalidDataException) { /* Arquivos FCV extraídos ainda podem estar disponíveis. */ }

        string extracted = Path.Combine(directory, stem);
        var searchRoots = new List<string> { directory };
        if (Directory.Exists(extracted)) searchRoots.Add(extracted);
        if (Path.GetFileName(directory).Equals("Content", StringComparison.OrdinalIgnoreCase))
            searchRoots.Add(Path.GetDirectoryName(directory)!);
        foreach (string searchRoot in searchRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (string file in Directory.EnumerateFiles(searchRoot, "*.fcv", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (!name.StartsWith(stem + "_", StringComparison.OrdinalIgnoreCase) || !identities.Add(Identity(name))) continue;
                entries.Add(new FcvCatalogEntry(file, null));
            }
        }
        return entries.OrderBy(x => FileNumber(x.FileName)).ThenBy(x => x.FileName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int FileNumber(string name)
    {
        string stem = Path.GetFileNameWithoutExtension(name);
        int separator = stem.LastIndexOf('_');
        return separator >= 0 && int.TryParse(stem[(separator + 1)..], out int number) ? number : int.MaxValue;
    }

    internal static string Identity(string name)
    {
        string stem = Path.GetFileNameWithoutExtension(name);
        int separator = stem.LastIndexOf('_');
        return separator >= 0 && int.TryParse(stem[(separator + 1)..], out int number)
            ? $"{stem[..separator].ToLowerInvariant()}_{number}.fcv"
            : Path.GetFileName(name).ToLowerInvariant();
    }
}

public static class FcvNameCatalog
{
    public static string Key(string datPath, string fcvPath) => $"{Path.GetFileNameWithoutExtension(datPath).ToLowerInvariant()}/{FcvCatalog.Identity(fcvPath)}";

    public static string GetFilePath(string datPath, string? projectRoot)
    {
        string root = !string.IsNullOrWhiteSpace(projectRoot) ? projectRoot : StableRoot(datPath);
        return Path.Combine(root, ".re4-animation-names.json");
    }

    public static Dictionary<string, string> Load(string datPath, string? projectRoot)
    {
        string primary = GetFilePath(datPath, projectRoot);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ReadInto(primary, result, overwrite: true);
        string local = Path.Combine(Path.GetDirectoryName(datPath)!, ".re4-animation-names.json");
        if (!string.Equals(local, primary, StringComparison.OrdinalIgnoreCase)) ReadInto(local, result, overwrite: false);
        string root = Path.GetDirectoryName(primary)!;
        string extracted = Path.Combine(root, "Extracted", Path.GetFileNameWithoutExtension(datPath));
        if (Directory.Exists(extracted))
            foreach (string legacy in Directory.EnumerateFiles(extracted, ".re4-animation-names.json", SearchOption.AllDirectories))
                if (!string.Equals(legacy, primary, StringComparison.OrdinalIgnoreCase) && !string.Equals(legacy, local, StringComparison.OrdinalIgnoreCase))
                    ReadInto(legacy, result, overwrite: false);
        return result;
    }

    public static void Save(string datPath, string? projectRoot, Dictionary<string, string> names)
    {
        string file = GetFilePath(datPath, projectRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(names, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ReadInto(string file, Dictionary<string, string> result, bool overwrite)
    {
        if (!File.Exists(file)) return;
        var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));
        if (loaded is null) return;
        foreach (var entry in loaded)
        {
            string[] parts = entry.Key.Split('/', 2);
            string key = parts.Length == 2 ? Key(parts[0], parts[1]) : entry.Key;
            if (overwrite || !result.ContainsKey(key)) result[key] = entry.Value;
        }
    }

    private static string StableRoot(string datPath)
    {
        for (DirectoryInfo? directory = new FileInfo(datPath).Directory; directory != null; directory = directory.Parent)
            if (directory.Name.Equals("Extracted", StringComparison.OrdinalIgnoreCase) && directory.Parent != null)
                return directory.Parent.FullName;
        return Path.GetDirectoryName(datPath)!;
    }
}
