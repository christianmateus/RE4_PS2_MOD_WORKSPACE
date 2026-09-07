namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void Form1_Shown(object? sender, EventArgs e)
    {
        ShowStartupLoading("Preparando o aplicativo...");
        await Task.Yield();
        if (string.IsNullOrWhiteSpace(project.RootPath) || !Directory.Exists(project.RootPath))
        {
            HideStartupLoading();
            return;
        }
        restoringSession = true;
        try
        {
            SetStartupLoadingStatus("Preparando as pastas do workspace...");
            EnsureFolders();
            ApplyDataToUi(refreshContent: false, refreshBuild: false);
            ExtractLog("Restaurando último projeto: " + project.RootPath);
            if (!string.IsNullOrWhiteSpace(project.IsoPath) && File.Exists(project.IsoPath))
            {
                SetStartupLoadingStatus("Lendo a ISO e o índice do AFS...");
                await LoadIsoAfsAsync(project.ActiveAfsPath, project.ActiveDatName);
                ExtractLog("Sessão restaurada: " + (project.ActiveAfsPath ?? "AFS padrão") + " / " + (project.ActiveDatName ?? "DAT não definido"));
            }
            else if (!string.IsNullOrWhiteSpace(project.IsoPath)) ExtractLog("ISO salva não foi encontrada: " + project.IsoPath);
            SetStartupLoadingStatus("Carregando os arquivos extraídos...");
            RefreshExtractedContent();
            SetStartupLoadingStatus("Atualizando o estado de build...");
            UpdateBuildUi();
            await RefreshChangeStatusAsync();
            SetStartupLoadingStatus("Verificando os DATs acompanhados...");
            await RefreshTrackedDatsAsync();
            SetStartupLoadingStatus("Restaurando a última tela...");
            await RestoreMainPageAsync();
        }
        catch (Exception ex) { ExtractLog("Não foi possível restaurar toda a sessão: " + ex.Message); }
        finally
        {
            restoringSession = false;
            HideStartupLoading();
        }
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (messagesModified && !ConfirmDiscardMessageChanges()) { e.Cancel = true; return; }
        SaveVisualCameraStateForActiveDat();
        characterCustomizer?.SaveCameraState();
        SaveProject();
        SaveSettings();
    }

    private void CreateStartupLoadingOverlay()
    {
        var card = new Panel
        {
            Width = 520,
            Height = 150,
            BackColor = Color.FromArgb(28, 31, 37),
            BorderStyle = BorderStyle.FixedSingle
        };
        var title = new Label
        {
            Text = "RESTAURANDO WORKSPACE",
            Left = 24,
            Top = 22,
            Width = 470,
            Height = 26,
            Font = new Font("Segoe UI Semibold", 13F),
            ForeColor = Color.FromArgb(238, 240, 244)
        };
        lblStartupLoading = new Label
        {
            Text = "Preparando o aplicativo...",
            Left = 24,
            Top = 58,
            Width = 470,
            Height = 28,
            ForeColor = Color.FromArgb(145, 151, 163)
        };
        var progress = new ProgressBar
        {
            Left = 24,
            Top = 103,
            Width = 470,
            Height = 12,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 24
        };
        card.Controls.Add(title);
        card.Controls.Add(lblStartupLoading);
        card.Controls.Add(progress);

        pnlStartupLoading = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(13, 15, 18) };
        pnlStartupLoading.Controls.Add(card);
        pnlStartupLoading.Resize += (_, _) =>
        {
            card.Left = Math.Max(0, (pnlStartupLoading.ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(0, (pnlStartupLoading.ClientSize.Height - card.Height) / 2);
        };
        Controls.Add(pnlStartupLoading);
        pnlStartupLoading.BringToFront();
        card.Left = Math.Max(0, (pnlStartupLoading.ClientSize.Width - card.Width) / 2);
        card.Top = Math.Max(0, (pnlStartupLoading.ClientSize.Height - card.Height) / 2);
    }

    private void ShowStartupLoading(string message)
    {
        if (pnlStartupLoading == null) return;
        if (lblStartupLoading != null) lblStartupLoading.Text = message;
        pnlStartupLoading.Visible = true;
        pnlStartupLoading.BringToFront();
        pnlStartupLoading.Refresh();
    }

    private void SetStartupLoadingStatus(string message)
    {
        if (lblStartupLoading == null) return;
        lblStartupLoading.Text = message;
        lblStartupLoading.Refresh();
    }

    private void HideStartupLoading()
    {
        if (pnlStartupLoading != null) pnlStartupLoading.Visible = false;
    }

    private void ShowPage(Panel page, Button navButton, string title)
    {
        if (pnlCharacters != null && pnlCharacters.Visible && page != pnlCharacters) characterCustomizer?.SaveCameraState();
        foreach (Panel panel in new[] { pnlDashboard, pnlWorkspace, pnlAssets, pnlTextures, pnlMessages, pnlVisualEditor, pnlCharacters, pnlEnemies, pnlAnimations, pnlBuild, pnlTools, pnlSettings, pnlLogs }) panel.Visible = false;
        page.Visible = true;
        page.BringToFront();
        foreach (Button button in new[] { btnNavDashboard, btnNavWorkspace, btnNavAssets, btnNavTextures, btnNavMessages, btnNavVisualEditor, btnNavCharacters, btnNavEnemies, btnNavAnimations, btnNavBuild, btnNavTools, btnNavSettings, btnNavLogs })
        {
            button.BackColor = Color.FromArgb(18, 20, 24);
            button.ForeColor = Color.FromArgb(145, 151, 163);
        }
        navButton.BackColor = Color.FromArgb(28, 31, 37);
        navButton.ForeColor = Color.FromArgb(238, 240, 244);
        lblTopTitle.Text = title;
        bool visualEditorOpen = page == pnlVisualEditor;
        if (btnTopSaveScenario != null) btnTopSaveScenario.Visible = visualEditorOpen;
        if (!visualEditorOpen && lblTopVisualModified != null) lblTopVisualModified.Visible = false;
        else if (visualEditorOpen) UpdateTopVisualSaveState();
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
    private void btnNavCharacters_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving();
        EnsureCharacterCustomizer();
        ShowPage(pnlCharacters, btnNavCharacters, "Personagens");
        RememberMainPage("Characters");
    }

    private void EnsureCharacterCustomizer()
    {
        if (characterCustomizer != null && !characterCustomizer.IsDisposed) return;
        string[] characterRoots = string.IsNullOrWhiteSpace(project.RootPath)
            ? new[] { Path.Combine(Application.StartupPath, "Extracted") }
            : new[] { Path.Combine(project.RootPath, "Extracted"), Path.Combine(Application.StartupPath, "Extracted") };
        ScenarioCameraState? savedCamera = settings.HasCharacterCamera
            ? new ScenarioCameraState(settings.CharacterCameraX, settings.CharacterCameraY, settings.CharacterCameraZ, settings.CharacterCameraYaw, settings.CharacterCameraPitch)
            : null;
        characterCustomizer = new CharacterCustomizerForm(settings.LastCharacterDatPath, path =>
        {
            settings.LastCharacterDatPath = path;
            ActivateCharacterDat(path);
            if (!restoringSession) SaveSettings();
        }, characterRoots, savedCamera, camera =>
        {
            settings.HasCharacterCamera = true;
            settings.CharacterCameraX = camera.X; settings.CharacterCameraY = camera.Y; settings.CharacterCameraZ = camera.Z;
            settings.CharacterCameraYaw = camera.Yaw; settings.CharacterCameraPitch = camera.Pitch;
            if (!restoringSession) SaveSettings();
        }, settings.Ps2BinToolPath, path => { settings.Ps2BinToolPath = path; if (!restoringSession) SaveSettings(); })
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        pnlCharacters.Controls.Add(characterCustomizer);
        characterCustomizer.Show();
    }

    private void ActivateCharacterDat(string path)
    {
        string fullPath = Path.GetFullPath(path);
        DatProjectState? state = (project.DatStates ?? new List<DatProjectState>()).FirstOrDefault(candidate =>
            (!string.IsNullOrWhiteSpace(candidate.OriginalDatPath) && string.Equals(Path.GetFullPath(candidate.OriginalDatPath), fullPath, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(candidate.ContentPath) && string.Equals(Path.GetFullPath(Path.Combine(candidate.ContentPath, candidate.DatName)), fullPath, StringComparison.OrdinalIgnoreCase)) ||
            (Directory.GetParent(fullPath)?.Name.Equals("Enemies", StringComparison.OrdinalIgnoreCase) == true && candidate.DatName.Equals(Path.GetFileName(fullPath), StringComparison.OrdinalIgnoreCase)));
        if (state == null) return;

        project.ActiveDatName = state.DatName;
        project.ActiveDatPath = state.OriginalDatPath;
        project.ActiveContentPath = state.ContentPath;
        project.ActiveBuildDatPath = state.BuildDatPath;
        project.ActiveAfsPath = state.AfsPath ?? project.ActiveAfsPath;
        project.LastBuildUtc = state.LastBuildUtc;
        if (!restoringSession) SaveProject();
        RefreshDashboard();
        UpdateBuildUi();
    }
    private void btnNavMessages_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving();
        ShowPage(pnlMessages, btnNavMessages, "Mensagens");
        RememberMainPage("Messages");
        LoadMessagesForActiveDat(false);
    }
    private void btnNavBuild_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ApplyDataToUi(); UpdateBuildUi(); ShowPage(pnlBuild, btnNavBuild, "Build & Test"); RememberMainPage("Build"); _ = RefreshTrackedDatsAsync(); }
    private void btnNavTools_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ApplyDataToUi(); ShowPage(pnlTools, btnNavTools, "Ferramentas"); RememberMainPage("Tools"); }
    private void btnNavSettings_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ApplyGeneralSettingsToUi(); chkSettingsSmdProtectIndices.Checked=settings.SmdProtectSpecialEntryIndices;chkSettingsCamTimeline.Checked=settings.CamShowTimeline;chkSettingsCamProtectMotion.Checked=settings.CamProtectMotionIndices; ShowPage(pnlSettings,btnNavSettings,"Configurações"); RememberMainPage("Settings"); }
    private void btnNavLogs_Click(object? sender, EventArgs e) { SaveVisualCameraIfLeaving(); ShowPage(pnlLogs, btnNavLogs, "Console"); RememberMainPage("Logs"); }
    private void btnTopBuild_Click(object? sender, EventArgs e)
    {
        if (pnlBuild.Visible) btnBuildOneClick_Click(sender, e);
        else btnNavBuild_Click(sender, e);
    }
    private void btnDashboardWorkspace_Click(object? sender, EventArgs e) => btnNavWorkspace_Click(sender, e);

    private void RememberMainPage(string page)
    {
        settings.LastMainPage = page;
        if (!restoringSession) SaveSettings();
    }

    private void btnSidebarToggle_Click(object? sender, EventArgs e)
    {
        settings.SidebarCollapsed = !settings.SidebarCollapsed;
        ApplySidebarState(settings.SidebarCollapsed);
        if (!restoringSession) SaveSettings();
    }

    private void ApplySidebarState(bool collapsed)
    {
        if (pnlSidebar == null) return;
        pnlSidebar.SuspendLayout();
        try
        {
            pnlSidebar.Width = collapsed ? 64 : 184;
            pnlSidebar.Padding = collapsed ? new Padding(6, 16, 6, 12) : new Padding(12, 16, 12, 12);
            lblLogo.Text = collapsed ? "RE4" : "RE4 PS2";
            lblLogo.TextAlign = collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            lblLogoSub.Visible = !collapsed;
            lblVersion.Text = "v0.6.0";
            lblVersion.TextAlign = collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            btnSidebarToggle.Text = collapsed ? "›" : "RETRAIR  ‹";
            btnSidebarToggle.TextAlign = collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleRight;

            (Button Button, string Short)[] items =
            {
                (btnNavDashboard,"DB"),(btnNavWorkspace,"PR"),(btnNavAssets,"AR"),(btnNavTextures,"TX"),
                (btnNavMessages,"MS"),(btnNavVisualEditor,"VE"),(btnNavCharacters,"CH"),(btnNavEnemies,"IN"),
                (btnNavAnimations,"AN"),(btnNavBuild,"B&T"),(btnNavTools,"TL"),(btnNavSettings,"CFG"),(btnNavLogs,"LOG")
            };
            foreach ((Button button, string shortText) in items)
            {
                string fullText = button.Tag as string ?? button.Text.Trim();
                button.Text = collapsed ? shortText : "  " + fullText;
                button.TextAlign = collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            }
        }
        finally
        {
            pnlSidebar.ResumeLayout(true);
        }
    }

    private void RestoreMainPage()
    {
        switch (settings.LastMainPage)
        {
            case "Assets": btnNavAssets_Click(null, EventArgs.Empty); break;
            case "Textures": btnNavTextures_Click(null, EventArgs.Empty); break;
            case "VisualEditor": btnNavVisualEditor_Click(null, EventArgs.Empty); break;
            case "Characters": btnNavCharacters_Click(null, EventArgs.Empty); break;
            case "Messages": btnNavMessages_Click(null, EventArgs.Empty); break;
            case "Enemies": btnNavEnemies_Click(null, EventArgs.Empty); break;
            case "Animations": btnNavAnimations_Click(null, EventArgs.Empty); break;
            case "Build": btnNavBuild_Click(null, EventArgs.Empty); break;
            case "Tools": btnNavTools_Click(null, EventArgs.Empty); break;
            case "Settings": btnNavSettings_Click(null, EventArgs.Empty); break;
            case "Logs": btnNavLogs_Click(null, EventArgs.Empty); break;
            case "Dashboard": btnNavDashboard_Click(null, EventArgs.Empty); break;
            default: btnNavWorkspace_Click(null, EventArgs.Empty); break;
        }
    }

    private async Task RestoreMainPageAsync()
    {
        if (settings.LastMainPage == "VisualEditor")
        {
            ShowPage(pnlVisualEditor, btnNavVisualEditor, "Visual Editor");
            RememberMainPage("VisualEditor");
            SetStartupLoadingStatus("Carregando o cenário no Editor Visual...");
            await RefreshAndLoadVisualEditorAsync();
            return;
        }

        RestoreMainPage();
    }
}
