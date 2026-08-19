namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void Form1_Shown(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(project.RootPath) || !Directory.Exists(project.RootPath)) return;
        restoringSession = true;
        try
        {
            EnsureFolders();
            ApplyDataToUi();
            ExtractLog("Restaurando último projeto: " + project.RootPath);
            if (!string.IsNullOrWhiteSpace(project.IsoPath) && File.Exists(project.IsoPath))
            {
                await LoadIsoAfsAsync(project.ActiveAfsPath, project.ActiveDatName);
                ExtractLog("Sessão restaurada: " + (project.ActiveAfsPath ?? "AFS padrão") + " / " + (project.ActiveDatName ?? "DAT não definido"));
            }
            else if (!string.IsNullOrWhiteSpace(project.IsoPath)) ExtractLog("ISO salva não foi encontrada: " + project.IsoPath);
            RefreshExtractedContent();
            RefreshTextureSmdList();
            UpdateBuildUi();
            await RefreshChangeStatusAsync();
            await RefreshTrackedDatsAsync();
            RestoreMainPage();
        }
        catch (Exception ex) { ExtractLog("Não foi possível restaurar toda a sessão: " + ex.Message); }
        finally { restoringSession = false; }
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveProject();
        SaveSettings();
    }

    private void ShowPage(Panel page, Button navButton, string title)
    {
        foreach (Panel panel in new[] { pnlDashboard, pnlWorkspace, pnlAssets, pnlTextures, pnlVisualEditor, pnlBuild, pnlTools, pnlLogs }) panel.Visible = false;
        page.Visible = true;
        page.BringToFront();
        foreach (Button button in new[] { btnNavDashboard, btnNavWorkspace, btnNavAssets, btnNavTextures, btnNavVisualEditor, btnNavBuild, btnNavTools, btnNavLogs })
        {
            button.BackColor = Color.FromArgb(18, 20, 24);
            button.ForeColor = Color.FromArgb(145, 151, 163);
        }
        navButton.BackColor = Color.FromArgb(28, 31, 37);
        navButton.ForeColor = Color.FromArgb(238, 240, 244);
        lblTopTitle.Text = title;
    }

    private void SaveVisualCameraIfLeaving()
    {
        if (pnlVisualEditor != null && pnlVisualEditor.Visible)
            SaveVisualCameraStateForActiveDat();
    }

    private void btnNavDashboard_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); RefreshDashboard(); ShowPage(pnlDashboard, btnNavDashboard, "Dashboard"); RememberMainPage("Dashboard"); }
    private void btnNavWorkspace_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ApplyDataToUi(); ShowPage(pnlWorkspace, btnNavWorkspace, "Projeto"); RememberMainPage("Project"); }
    private void btnNavAssets_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); RefreshExtractedContent(); ShowPage(pnlAssets, btnNavAssets, "Arquivos"); RememberMainPage("Assets"); }
    private async void btnNavTextures_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving();
        RefreshTextureDatList();
        RefreshTextureSmdList();
        ShowPage(pnlTextures, btnNavTextures, "Texturas");
        RememberMainPage("Textures");
        if (cmbTextureSmd.SelectedItem is TextureSmdItem item && (!string.Equals(activeTextureSmdPath, item.FullPath, StringComparison.OrdinalIgnoreCase) || lvTextures.Items.Count == 0))
            await LoadNativeTexturesAsync(false);
    }
    private async void btnNavVisualEditor_Click(object? sender, EventArgs e)
    {
        ShowPage(pnlVisualEditor, btnNavVisualEditor, "Visual Editor");
        RememberMainPage("VisualEditor");
        await RefreshAndLoadVisualEditorAsync();
    }
    private void btnNavBuild_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ApplyDataToUi(); UpdateBuildUi(); ShowPage(pnlBuild, btnNavBuild, "Build & Test"); RememberMainPage("Build"); _ = RefreshTrackedDatsAsync(); }
    private void btnNavTools_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ApplyDataToUi(); ShowPage(pnlTools, btnNavTools, "Ferramentas"); RememberMainPage("Tools"); }
    private void btnNavLogs_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ShowPage(pnlLogs, btnNavLogs, "Console"); RememberMainPage("Logs"); }
    private void btnTopBuild_Click(object? sender, EventArgs e) => btnNavBuild_Click(sender, e);
    private void btnDashboardWorkspace_Click(object? sender, EventArgs e) => btnNavWorkspace_Click(sender, e);

    private void RememberMainPage(string page)
    {
        settings.LastMainPage = page;
        if (!restoringSession) SaveSettings();
    }

    private void RestoreMainPage()
    {
        switch (settings.LastMainPage)
        {
            case "Assets": btnNavAssets_Click(null, EventArgs.Empty); break;
            case "Textures": btnNavTextures_Click(null, EventArgs.Empty); break;
            case "VisualEditor": btnNavVisualEditor_Click(null, EventArgs.Empty); break;
            case "Build": btnNavBuild_Click(null, EventArgs.Empty); break;
            case "Tools": btnNavTools_Click(null, EventArgs.Empty); break;
            case "Logs": btnNavLogs_Click(null, EventArgs.Empty); break;
            case "Dashboard": btnNavDashboard_Click(null, EventArgs.Empty); break;
            default: btnNavWorkspace_Click(null, EventArgs.Empty); break;
        }
    }
}
