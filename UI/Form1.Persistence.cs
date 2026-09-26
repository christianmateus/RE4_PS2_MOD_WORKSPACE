namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private void LoadSettings()
    {
        try
        {
            settings = ReadJsonWithRecovery(settingsFile, new AppSettings());
            if (!string.IsNullOrWhiteSpace(settings.LastWorkspace)) LoadProject(settings.LastWorkspace);
        }
        catch (Exception ex) { settings = new(); project = new(); Trace.WriteLine("[Persistence] Não foi possível restaurar as configurações: " + ex.Message); }
    }

    private void SaveSettings()
    {
        WriteJsonAtomically(settingsFile, settings);
    }

    private void LoadProject(string root)
    {
        try
        {
            var file = Path.Combine(root, ".re4workspace.json");
            project = ReadJsonWithRecovery(file, new WorkspaceProject());
            project.RootPath = root;
        }
        catch { project = new WorkspaceProject { RootPath = root }; }
    }

    private void SaveProject()
    {
        if (string.IsNullOrWhiteSpace(project.RootPath)) return;
        MigrateActiveDatState();
        EnsureFolders();
        WriteJsonAtomically(Path.Combine(project.RootPath, ".re4workspace.json"), project);
        settings.LastWorkspace = project.RootPath;
        SaveSettings();
    }

    private static void WriteJsonAtomically<T>(string path, T value)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        string previous = path + ".previous";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, JsonOptions()));
            using (JsonDocument.Parse(File.ReadAllText(temporary))) { }
            if (File.Exists(path)) File.Replace(temporary, path, previous, true);
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) try { File.Delete(temporary); } catch { }
        }
    }

    private static T ReadJsonWithRecovery<T>(string path, T fallback)
    {
        foreach (string candidate in new[] { path, path + ".previous" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                T? value = JsonSerializer.Deserialize<T>(File.ReadAllText(candidate));
                if (value is not null) return value;
            }
            catch (Exception ex) { Trace.WriteLine($"[Persistence] JSON inválido em '{candidate}': {ex.Message}"); }
        }
        return fallback;
    }

    private static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true };
}
