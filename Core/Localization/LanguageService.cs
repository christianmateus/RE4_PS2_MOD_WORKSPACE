using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed record LanguageInfo(string Code, string Name, string FilePath)
{
    public override string ToString() => Name;
}

/// <summary>Localization catalog backed by permanent keys and a Portuguese source catalog.</summary>
public static class LanguageService
{
    public const string DefaultLanguage = "pt-BR";
    private static readonly Regex PlaceholderRegex = new(@"\{[^{}]+\}", RegexOptions.Compiled);
    private static readonly Dictionary<string, string> SourceByKey = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> TranslationByKey = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> KeyBySource = new(StringComparer.Ordinal);
    private static readonly List<TemplateTranslation> Templates = new();
    private static DateTime lastUiPassUtc;
    private static readonly HashSet<Control> ObservedControls = new();
    private static bool applying;

    public static string CurrentLanguage { get; private set; } = DefaultLanguage;
    public static string LocalesDirectory => Path.Combine(AppContext.BaseDirectory, "locales");
    public static event EventHandler? LanguageChanged;

    public static IReadOnlyList<LanguageInfo> Discover()
    {
        if (!Directory.Exists(LocalesDirectory)) return Array.Empty<LanguageInfo>();
        var result = new List<LanguageInfo>();
        foreach (string file in Directory.EnumerateFiles(LocalesDirectory, "*.json").OrderBy(Path.GetFileName))
        {
            try
            {
                using JsonDocument json = JsonDocument.Parse(File.ReadAllText(file));
                JsonElement root = json.RootElement;
                string code = root.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? Path.GetFileNameWithoutExtension(file) : Path.GetFileNameWithoutExtension(file);
                string name = root.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? code : code;
                result.Add(new LanguageInfo(code, name, file));
            }
            catch (Exception ex) { Trace.WriteLine($"[Localization] Invalid locale '{file}': {ex.Message}"); }
        }
        return result;
    }

    public static void SetLanguage(LanguageInfo? language)
    {
        CurrentLanguage = language?.Code ?? DefaultLanguage;
        SourceByKey.Clear(); TranslationByKey.Clear(); KeyBySource.Clear(); Templates.Clear();
        LoadCatalog(Path.Combine(LocalesDirectory, DefaultLanguage + ".json"), SourceByKey);
        if (string.Equals(CurrentLanguage, DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            foreach ((string key, string value) in SourceByKey) TranslationByKey[key] = value;
        else if (language is not null) LoadCatalog(language.FilePath, TranslationByKey);

        foreach ((string key, string source) in SourceByKey)
        {
            KeyBySource.TryAdd(source, key);
            if (!TranslationByKey.ContainsKey(key)) TranslationByKey[key] = source;
            if (!Placeholders(source).SetEquals(Placeholders(TranslationByKey[key])))
            {
                Trace.WriteLine($"[Localization] Placeholder mismatch in '{key}'.");
                TranslationByKey[key] = source;
            }
        }

        // Accept text left on screen in any installed language, so switching is reversible.
        foreach (LanguageInfo installed in Discover())
        {
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            LoadCatalog(installed.FilePath, aliases);
            foreach ((string key, string alias) in aliases)
            {
                if (!TranslationByKey.TryGetValue(key, out string? target)) continue;
                if (!Placeholders(alias).SetEquals(Placeholders(target))) continue;
                KeyBySource.TryAdd(alias, key);
                if (PlaceholderRegex.IsMatch(alias)) Templates.Add(TemplateTranslation.Create(alias, target));
            }
        }
        Templates.Sort((a, b) => b.Source.Length.CompareTo(a.Source.Length));
        LanguageChanged?.Invoke(null, EventArgs.Empty);
        foreach (Form form in Application.OpenForms) Apply(form);
    }

    public static string T(string key, params object?[] arguments)
    {
        string value = TranslationByKey.TryGetValue(key, out string? translated)
            ? translated : SourceByKey.TryGetValue(key, out string? source) ? source : key;
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
    public static string Translate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        string leading = value[..(value.Length - value.TrimStart().Length)];
        string trailing = value[(value.TrimEnd().Length)..];
        string core = value.Trim();
        if (KeyBySource.TryGetValue(core, out string? key)) return leading + TranslationByKey[key] + trailing;
        foreach (TemplateTranslation template in Templates)
        {
            Match match = template.Pattern.Match(core);
            if (!match.Success) continue;
            string result = template.Translation;
            for (int i = 0; i < template.Placeholders.Length; i++)
                result = result.Replace(template.Placeholders[i], match.Groups[i + 1].Value, StringComparison.Ordinal);
            return leading + result + trailing;
        }
        return value;
    }

    public static void Apply(Control root)
    {
        applying = true;
        try
        {
            root.Text = Translate(root.Text);
            if (root.Tag is string tag) root.Tag = Translate(tag);
            if (root is ComboBox combo)
                for (int i = 0; i < combo.Items.Count; i++) if (combo.Items[i] is string item) combo.Items[i] = Translate(item);
            if (root is ListBox list)
                for (int i = 0; i < list.Items.Count; i++) if (list.Items[i] is string item) list.Items[i] = Translate(item);
            if (root is ListView view)
                foreach (ColumnHeader column in view.Columns) column.Text = Translate(column.Text);
            if (root.ContextMenuStrip is not null) Apply(root.ContextMenuStrip.Items);
            if (root is ToolStrip strip) Apply(strip.Items);
            if (ObservedControls.Add(root))
            {
                root.TextChanged += LocalizeChangedText;
                root.ControlAdded += (_, e) => { if (e.Control is not null) Apply(e.Control); };
            }
            foreach (Control child in root.Controls) Apply(child);
        }
        finally { applying = false; }
    }

    private static void LocalizeChangedText(object? sender, EventArgs e)
    {
        if (applying || sender is not Control control) return;
        string translated = Translate(control.Text);
        if (translated == control.Text) return;
        applying = true;
        try { control.Text = translated; }
        finally { applying = false; }
    }

    public static void InstallRuntimeLocalization()
    {
        Application.Idle += (_, _) =>
        {
            if ((DateTime.UtcNow - lastUiPassUtc).TotalMilliseconds < 200) return;
            lastUiPassUtc = DateTime.UtcNow;
            ApplyToOpenForms();
        };
    }

    private static void ApplyToOpenForms()
    {
        if (Application.OpenForms.Count == 0) return;
        foreach (Form form in Application.OpenForms)
            if (!ObservedControls.Contains(form)) Apply(form);
    }

    private static void Apply(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.Text = Translate(item.Text);
            if (item is ToolStripDropDownItem dropDown) Apply(dropDown.DropDownItems);
        }
    }

    private static void LoadCatalog(string file, Dictionary<string, string> destination)
    {
        if (!File.Exists(file)) return;
        try
        {
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(file));
            if (!json.RootElement.TryGetProperty("translations", out JsonElement translations)) return;
            foreach (JsonProperty item in translations.EnumerateObject())
                if (item.Value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(item.Value.GetString()))
                    destination[item.Name] = item.Value.GetString()!;
        }
        catch (Exception ex) { Trace.WriteLine($"[Localization] Could not load '{file}': {ex.Message}"); }
    }

    private static HashSet<string> Placeholders(string value) =>
        PlaceholderRegex.Matches(value).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);

    private sealed record TemplateTranslation(string Source, string Translation, Regex Pattern, string[] Placeholders)
    {
        public static TemplateTranslation Create(string source, string translation)
        {
            var pattern = new StringBuilder("^");
            var placeholders = new List<string>();
            int offset = 0;
            foreach (Match match in PlaceholderRegex.Matches(source))
            {
                pattern.Append(Regex.Escape(source[offset..match.Index]));
                pattern.Append("([\\s\\S]*?)");
                placeholders.Add(match.Value);
                offset = match.Index + match.Length;
            }
            pattern.Append(Regex.Escape(source[offset..])).Append('$');
            return new(source, translation, new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant), placeholders.ToArray());
        }
    }
}