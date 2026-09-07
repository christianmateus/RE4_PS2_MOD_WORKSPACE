namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private static readonly int[] AutoSaveIntervals = { 1, 3, 5, 10 };

    private void InitializeAutoSave()
    {
        autoSaveTimer.Tick += autoSaveTimer_Tick;
        ApplyGeneralSettingsToUi();
        ConfigureAutoSaveTimer();
    }

    private void ApplyGeneralSettingsToUi()
    {
        if (chkSettingsAutoSave == null || cmbSettingsAutoSaveInterval == null || chkSettingsShowFps == null) return;
        int interval = AutoSaveIntervals.Contains(settings.AutoSaveIntervalMinutes) ? settings.AutoSaveIntervalMinutes : 5;
        chkSettingsAutoSave.Checked = settings.AutoSaveEnabled;
        cmbSettingsAutoSaveInterval.SelectedIndex = Array.IndexOf(AutoSaveIntervals, interval);
        cmbSettingsAutoSaveInterval.Enabled = settings.AutoSaveEnabled;
        chkSettingsShowFps.Checked = settings.VisualShowFps;
        if(chkSettingsCamTimeline!=null)chkSettingsCamTimeline.Checked=settings.CamShowTimeline;
        if(chkSettingsCamProtectMotion!=null)chkSettingsCamProtectMotion.Checked=settings.CamProtectMotionIndices;
        ApplyVisualFpsVisibility();
    }

    private void ConfigureAutoSaveTimer()
    {
        int minutes = AutoSaveIntervals.Contains(settings.AutoSaveIntervalMinutes) ? settings.AutoSaveIntervalMinutes : 5;
        settings.AutoSaveIntervalMinutes = minutes;
        autoSaveTimer.Stop();
        autoSaveTimer.Interval = minutes * 60 * 1000;
        if (settings.AutoSaveEnabled) autoSaveTimer.Start();
    }

    private void chkSettingsAutoSave_CheckedChanged(object? sender, EventArgs e)
    {
        settings.AutoSaveEnabled = chkSettingsAutoSave.Checked;
        cmbSettingsAutoSaveInterval.Enabled = settings.AutoSaveEnabled;
        ConfigureAutoSaveTimer();
        if (!restoringSession) SaveSettings();
    }

    private void cmbSettingsAutoSaveInterval_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbSettingsAutoSaveInterval.SelectedIndex < 0) return;
        settings.AutoSaveIntervalMinutes = AutoSaveIntervals[cmbSettingsAutoSaveInterval.SelectedIndex];
        ConfigureAutoSaveTimer();
        if (!restoringSession) SaveSettings();
    }

    private void chkSettingsShowFps_CheckedChanged(object? sender, EventArgs e)
    {
        settings.VisualShowFps = chkSettingsShowFps.Checked;
        ApplyVisualFpsVisibility();
        if (!restoringSession) SaveSettings();
    }

    private void ApplyVisualFpsVisibility()
    {
        if (visualViewport == null) return;
        visualViewport.ShowFps = settings.VisualShowFps;
        if (lblVisualFps != null)
        {
            lblVisualFps.Visible = settings.VisualShowFps;
            if (settings.VisualShowFps) lblVisualFps.BringToFront();
        }
    }

    private async void autoSaveTimer_Tick(object? sender, EventArgs e)
    {
        if (autoSaveRunning || restoringSession || loadingVisualEditor || visualViewport?.Scene == null) return;
        autoSaveRunning = true;
        autoSaveTimer.Stop();
        try
        {
            lblVisualStatus.Text = "Auto-save em andamento...";
            SaveVisualCameraStateForActiveDat();
            bool saved = await SaveVisualEditorAllAsync(false);
            ExtractLog(saved ? "Visual Editor: auto-save concluído." : "Visual Editor: auto-save verificado; nenhuma alteração pendente.");
            UpdateVisualStatus();
            UpdateTopVisualSaveState();
        }
        catch (Exception ex)
        {
            lblVisualStatus.Text = "Falha no auto-save";
            ExtractLog("Visual Editor: erro no auto-save: " + ex.Message);
        }
        finally
        {
            autoSaveRunning = false;
            if (settings.AutoSaveEnabled) autoSaveTimer.Start();
        }
    }

    private void InitializeVisualSpeedPersistence()
    {
        visualSpeedSaveTimer.Interval = 600;
        visualSpeedSaveTimer.Tick += (_, _) =>
        {
            visualSpeedSaveTimer.Stop();
            SaveVisualCameraStateForActiveDat();
        };
        if (visualViewport != null)
        {
            visualViewport.MovementSpeedChanged += (_, _) =>
            {
                if (loadingVisualEditor || restoringSession) return;
                DatProjectState? state = string.IsNullOrWhiteSpace(project.ActiveDatName) ? null : GetDatState(project.ActiveDatName, false);
                if (state == null) return;
                state.VisualFlySpeed = visualViewport.FlySpeed;
                visualSpeedSaveTimer.Stop();
                visualSpeedSaveTimer.Start();
            };
        }
    }

    private void chkSettingsSmdProtectIndices_CheckedChanged(object? sender,EventArgs e)
    {
        settings.SmdProtectSpecialEntryIndices=chkSettingsSmdProtectIndices.Checked;
        if(!restoringSession)SaveSettings();
    }
    private void chkSettingsCamTimeline_CheckedChanged(object? sender,EventArgs e){settings.CamShowTimeline=chkSettingsCamTimeline.Checked;ApplyCamTimelineVisibility();if(!restoringSession)SaveSettings();}
    private void chkSettingsCamProtectMotion_CheckedChanged(object? sender,EventArgs e){settings.CamProtectMotionIndices=chkSettingsCamProtectMotion.Checked;if(!restoringSession)SaveSettings();}
    private void ApplyCamTimelineVisibility(){if(visualCamTimeline!=null)visualCamTimeline.Visible=settings.CamShowTimeline&&tabVisualEntities?.SelectedIndex==8;}
}
