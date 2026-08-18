namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFile)) settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsFile)) ?? new();
            if (!string.IsNullOrWhiteSpace(settings.LastWorkspace)) LoadProject(settings.LastWorkspace);
        }
        catch { settings = new(); project = new(); }
    }

    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsFile)!);
        File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, JsonOptions()));
    }

    private void LoadProject(string root)
    {
        try
        {
            var file = Path.Combine(root, ".re4workspace.json");
            project = File.Exists(file) ? JsonSerializer.Deserialize<WorkspaceProject>(File.ReadAllText(file)) ?? new() : new WorkspaceProject();
            project.RootPath = root;
        }
        catch { project = new WorkspaceProject { RootPath = root }; }
    }

    private void SaveProject()
    {
        if (string.IsNullOrWhiteSpace(project.RootPath)) return;
        MigrateActiveDatState();
        EnsureFolders();
        File.WriteAllText(Path.Combine(project.RootPath, ".re4workspace.json"), JsonSerializer.Serialize(project, JsonOptions()));
        settings.LastWorkspace = project.RootPath;
        SaveSettings();
    }

    private static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true };
}
