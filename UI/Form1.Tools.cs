namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private void btnBrowseIsoAfs_Click(object? sender, EventArgs e) => PickTool(txtIsoAfs, v => settings.IsoAfsPath = v);

    private void btnBrowseDatTool_Click(object? sender, EventArgs e) => PickTool(txtDatTool, v => settings.DatToolPath = v);

    private void btnBrowseTpl_Click(object? sender, EventArgs e) => PickTool(txtTplManager, v => settings.TplManagerPath = v);

    private void btnBrowsePcsx2_Click(object? sender, EventArgs e) => PickTool(txtPcsx2, v => settings.Pcsx2Path = v);

    private void btnOpenIsoAfs_Click(object? sender, EventArgs e) => Launch(settings.IsoAfsPath, "ISOAFS");

    private void btnOpenDatTool_Click(object? sender, EventArgs e) => Launch(settings.DatToolPath, "DAT Tool");

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
