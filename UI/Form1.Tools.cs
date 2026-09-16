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
            btnNavTools_Click(null, EventArgs.Empty); return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path)! });
            WriteLog("Aberto: " + toolName);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Erro ao abrir " + toolName, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
