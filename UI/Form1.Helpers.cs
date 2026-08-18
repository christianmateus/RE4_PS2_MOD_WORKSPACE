namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private string? BrowseFolder(string? initial = null)
    {
        using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta do RE4 PS2 Mod Workspace", ShowNewFolderButton = true };
        if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial)) dialog.SelectedPath = initial;
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedPath : null;
    }

    private string? BrowseFile(string filter)
    {
        using var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private void OpenFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) { MessageBox.Show("Pasta não encontrada."); return; }
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    private void WriteLog(string text)
    {
        if (rtbBuildLog == null || rtbBuildLog.IsDisposed) return;
        rtbBuildLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private static string? Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ShortPath(string? path, string fallback) => string.IsNullOrWhiteSpace(path) ? fallback : Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
