namespace RE4_PS2_MOD_WORKSPACE.Core.Afs;

public sealed record AfsBatchFile(AfsEntry Entry, string Format, string Group);

public static class AfsBatchExtraction
{
    public static IReadOnlyList<AfsBatchFile> FindFiles(AfsImage image)
    {
        var files = new List<AfsBatchFile>();
        foreach (AfsEntry entry in AfsService.GetUniqueValidEntries(image))
        {
            string name = Path.GetFileName(entry.FileName);
            string stem = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name).ToUpperInvariant();
            if (stem.StartsWith("idm", StringComparison.OrdinalIgnoreCase) && extension is ".BIN" or ".TPL")
            {
                files.Add(new AfsBatchFile(entry, "IDM", "IDM"));
                continue;
            }

            string? format = extension switch
            {
                ".SND" => "SND", ".REL" => "REL", ".DAT" => "DAT",
                ".ADX" => "ADX", ".FNT" => "FNT", ".ESL" => "ESL", _ => null
            };
            if (format == null) continue;

            string group = format switch
            {
                "SND" or "REL" or "DAT" => ClassifyCharacterScenarioEnemy(stem),
                "ESL" when stem.StartsWith("emleon", StringComparison.OrdinalIgnoreCase) => "Emleon",
                "ESL" when stem.StartsWith("emgirl", StringComparison.OrdinalIgnoreCase) => "Emgirl",
                "ESL" when stem.StartsWith("emlist", StringComparison.OrdinalIgnoreCase) => "Emlist",
                _ => "Gerais"
            };
            files.Add(new AfsBatchFile(entry, format, group));
        }
        return files;
    }

    private static string ClassifyCharacterScenarioEnemy(string stem)
    {
        if (stem.Length >= 4 && (stem[0] == 'r' || stem[0] == 'R') &&
            char.IsDigit(stem[1]) && char.IsDigit(stem[2]) && char.IsDigit(stem[3])) return "Cenários";
        if (stem.StartsWith("pl", StringComparison.OrdinalIgnoreCase)) return "Personagens";
        if (stem.StartsWith("em", StringComparison.OrdinalIgnoreCase)) return "Inimigos";
        return "Gerais";
    }
}
