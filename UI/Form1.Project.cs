namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private void btnBrowseWorkspace_Click(object? sender, EventArgs e)
    {
        var selected = BrowseFolder(txtWorkspacePath.Text);
        if (selected == null) return;
        SetWorkspace(selected);
    }

    private void btnCreateWorkspace_Click(object? sender, EventArgs e)
    {
        string? root = string.IsNullOrWhiteSpace(txtWorkspacePath.Text) ? BrowseFolder() : txtWorkspacePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(root)) return;

        try
        {
            SetWorkspace(root);
            MessageBox.Show($"Workspace criado com sucesso.\n\n{root}\n\nPastas criadas: Original, Extracted, Mods, Build e Temp.", "RE4 PS2 Mod Workspace", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Não foi possível criar o workspace.\n\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetWorkspace(string root)
    {
        root = Path.GetFullPath(root);
        if (!string.Equals(project.RootPath, root, StringComparison.OrdinalIgnoreCase)) LoadProject(root);
        project.RootPath = root;
        EnsureFolders();
        SaveProject();
        txtWorkspacePath.Text = root;
        lblWorkspaceCurrent.Text = root;
        RefreshDashboard();
        WriteLog("Workspace definido: " + root);
    }

    private void btnOpenWorkspace_Click(object? sender, EventArgs e) => OpenFolder(project.RootPath);

    private void btnBrowseIso_Click(object? sender, EventArgs e)
    {
        var path = BrowseFile("ISO do PlayStation 2 (*.iso)|*.iso|Todos os arquivos (*.*)|*.*");
        if (path == null) return;
        project.IsoPath = path; txtIsoPath.Text = path; SaveProject(); RefreshDashboard();
        _ = LoadIsoAfsAsync();
    }

    private void btnBrowseDat_Click(object? sender, EventArgs e)
    {
        var path = BrowseFile("Arquivos DAT (*.dat)|*.dat|Todos os arquivos (*.*)|*.*");
        if (path == null) return;
        project.ActiveDatPath = path; txtDatPath.Text = path; SaveProject(); RefreshDashboard();
    }

    private void txtIsoPath_Leave(object? sender, EventArgs e) { project.IsoPath = Clean(txtIsoPath.Text); SaveProject(); RefreshDashboard(); }

    private void txtDatPath_Leave(object? sender, EventArgs e) { project.ActiveDatPath = Clean(txtDatPath.Text); SaveProject(); RefreshDashboard(); }

    private void ApplyDataToUi()
    {
        txtWorkspacePath.Text = project.RootPath ?? "";
        txtIsoPath.Text = project.IsoPath ?? "";
        if (txtDatPath != null) txtDatPath.Text = project.ActiveDatPath ?? "";
        txtIsoAfs.Text = settings.IsoAfsPath ?? "";
        txtDatTool.Text = settings.DatToolPath ?? "";
        txtTplManager.Text = settings.TplManagerPath ?? "";
        txtPcsx2.Text = settings.Pcsx2Path ?? "";
        lblWorkspaceCurrent.Text = string.IsNullOrWhiteSpace(project.RootPath) ? "Nenhum workspace selecionado" : project.RootPath;
        RefreshDashboard();
        RefreshExtractedContent();
        UpdateBuildUi();
    }

    private void RefreshDashboard()
    {
        lblCardWorkspaceValue.Text = ShortPath(project.RootPath, "Nenhum projeto");
        lblCardIsoValue.Text = ShortPath(project.IsoPath, "Não selecionada");
        lblCardDatValue.Text = !string.IsNullOrWhiteSpace(project.ActiveDatName) ? project.ActiveDatName : ShortPath(project.ActiveDatPath, "Nenhum");
        lblCardStatusValue.Text = string.IsNullOrWhiteSpace(project.RootPath) ? "Aguardando projeto" : "Pronto";
    }

    private bool RequireWorkspace()
    {
        if (!string.IsNullOrWhiteSpace(project.RootPath) && Directory.Exists(project.RootPath)) return true;
        MessageBox.Show("Crie ou selecione um workspace primeiro.", "Workspace necessário", MessageBoxButtons.OK, MessageBoxIcon.Information);
        btnNavWorkspace_Click(null, EventArgs.Empty);
        return false;
    }

    private void EnsureFolders()
    {
        if (string.IsNullOrWhiteSpace(project.RootPath)) throw new InvalidOperationException("Nenhuma pasta de workspace foi selecionada.");
        Directory.CreateDirectory(project.RootPath);
        foreach (var name in new[] { "Original", "Extracted", "Mods", "Build", "Temp" }) Directory.CreateDirectory(Path.Combine(project.RootPath, name));
    }
}
