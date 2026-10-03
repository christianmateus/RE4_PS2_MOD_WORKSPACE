namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{

    private void OpenExecutableEditor()
    {
        string? executable = null;
        if (!string.IsNullOrWhiteSpace(project.RootPath))
        {
            string build = Path.Combine(project.RootPath, "Build");
            var candidates = new[] { "SLUS_211.34", "SLES_537.02", "SLPS_000.00_original", "SLPS_000.00" }
                .Select(name => Path.Combine(build, name)).Where(File.Exists).ToArray();
            executable = candidates.Length == 1 ? candidates[0] : null;
        }
        string? buildIso = null;
        if (!string.IsNullOrWhiteSpace(project.RootPath))
        {
            string conventional = Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso");
            buildIso = !string.IsNullOrWhiteSpace(project.ActiveBuildIsoPath) && File.Exists(project.ActiveBuildIsoPath)
                ? project.ActiveBuildIsoPath
                : File.Exists(conventional) ? conventional : null;
        }
        using var editor = new ExecutableEditorForm(executable, buildIso, settings.CreateIsoBackup);
        editor.ShowDialog(this);
    }

    private void OpenRelPatchManager()
    {
        string? conventional = string.IsNullOrWhiteSpace(project.RootPath) ? null : Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso");
        string? buildIso = !string.IsNullOrWhiteSpace(project.ActiveBuildIsoPath) && File.Exists(project.ActiveBuildIsoPath)
            ? project.ActiveBuildIsoPath : (!string.IsNullOrWhiteSpace(conventional) && File.Exists(conventional) ? conventional : null);
        using var manager = new RelPatchManagerForm(buildIso, project.GanadoScaleMultiplier, value =>
        {
            project.GanadoScaleMultiplier = value;
            SaveProject();
            WriteLog($"Tamanho global dos Ganados definido para {value:0.00}×. Será aplicado no próximo Build.");
        });
        manager.ShowDialog(this);
    }

    private void OpenRelInspector()
    {
        static bool HasRelFiles(string path) => Directory.Exists(path) &&
            Directory.EnumerateFiles(path, "*.rel", SearchOption.AllDirectories).Any();
        string? sourceRelDirectory = null;
        string? sourceSles = null;
        string[] roots = { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (string root in roots)
        {
            DirectoryInfo? directory = new(root);
            for (int i = 0; i < 6 && directory is not null; i++, directory = directory.Parent)
            {
                string references = Path.Combine(directory.FullName, "_references");
                string rels = Path.Combine(references, "REL");
                string elf = Path.Combine(references, "ELFs", "SLES_537.02");
                if (sourceRelDirectory is null && HasRelFiles(rels)) sourceRelDirectory = rels;
                if (sourceSles is null && File.Exists(elf)) sourceSles = elf;
            }
        }
        string? relDirectory = null;
        if (!string.IsNullOrWhiteSpace(project.RootPath))
        {
            string extracted = Path.Combine(project.RootPath, "Extracted", "_AFS", "BIO4DAT", "REL");
            string references = Path.Combine(project.RootPath, "_references", "REL");
            relDirectory = HasRelFiles(extracted) ? extracted : HasRelFiles(references) ? references : null;
        }
        relDirectory ??= sourceRelDirectory;
        string? sles = null;
        if (!string.IsNullOrWhiteSpace(project.RootPath))
        {
            string build = Path.Combine(project.RootPath, "Build", "SLES_537.02");
            string buildIso = Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso");
            if (!string.IsNullOrWhiteSpace(project.ActiveBuildIsoPath) && File.Exists(project.ActiveBuildIsoPath))
                sles = project.ActiveBuildIsoPath;
            else if (File.Exists(buildIso)) sles = buildIso;
            else if (File.Exists(build)) sles = build;
        }
        sles ??= sourceSles;
        using var inspector = new RelInspectorForm(relDirectory, sles);
        inspector.ShowDialog(this);
    }

    private void btnBrowseTpl_Click(object? sender, EventArgs e) => PickTool(txtTplManager, v => settings.TplManagerPath = v);

    private void btnBrowsePcsx2_Click(object? sender, EventArgs e) => PickTool(txtPcsx2, v => settings.Pcsx2Path = v);

    private void btnOpenTpl_Click(object? sender, EventArgs e) => Launch(settings.TplManagerPath, "TPL Manager");

    private void btnOpenPcsx2_Click(object? sender, EventArgs e) => Launch(settings.Pcsx2Path, "PCSX2");

    private void PickTool(TextBox target, Action<string> setter)
    {
        var path = BrowseFile("Executável (*.exe)|*.exe|Todos os arquivos (*.*)|*.*");
        if (path == null) return;
        target.Text = path; setter(path); SaveSettings();
    }

    private void Launch(string? path, string toolName)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            MessageBox.Show($"Configure o caminho do {toolName} primeiro.", "Ferramenta não configurada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNavSettings_Click(null, EventArgs.Empty);
            if (pnlSettings.Controls.OfType<TabControl>().FirstOrDefault() is TabControl tabs) tabs.SelectedIndex = 0;
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path)! });
            WriteLog("Aberto: " + toolName);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Erro ao abrir " + toolName, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
