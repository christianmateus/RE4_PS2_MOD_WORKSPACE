using RE4_PS2_MOD_WORKSPACE.Core.Afs;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private bool HasValidSetup()
    {
        return !string.IsNullOrWhiteSpace(project.RootPath) && Directory.Exists(project.RootPath)
            && !string.IsNullOrWhiteSpace(project.IsoPath) && File.Exists(project.IsoPath)
            && !string.IsNullOrWhiteSpace(settings.Pcsx2Path) && File.Exists(settings.Pcsx2Path);
    }

    private bool SetupIsRequired()
    {
        bool valid = HasValidSetup();
        if (valid && !settings.SetupCompleted)
        {
            // Configurações criadas por versões anteriores já representam um setup concluído.
            settings.SetupCompleted = true;
            SaveSettings();
        }
        return settings.SetupShowOnStartup || !valid;
    }

    private async Task<bool> RunSetupWizardAsync(bool force = false)
    {
        if (!force && !SetupIsRequired()) return true;

        string isoPath = FirstFilled(project.IsoPath, settings.SetupDraftIsoPath);
        string workspacePath = FirstFilled(project.RootPath, settings.SetupDraftWorkspacePath);
        string pcsx2Path = FirstFilled(settings.Pcsx2Path, settings.SetupDraftPcsx2Path);
        string tplPath = FirstFilled(settings.TplManagerPath, settings.SetupDraftTplManagerPath);
        var initial = new SetupWizardResult(isoPath, workspacePath, pcsx2Path,
            string.IsNullOrWhiteSpace(tplPath) ? null : tplPath,
            SetupPreparation.FirstScenario, settings.SetupShowOnStartup);

        using var wizard = new SetupWizardForm(initial, SaveSetupDraft, CompleteSetupAsync);
        DialogResult result = wizard.ShowLocalizedDialog(this);
        if (result != DialogResult.OK) return HasValidSetup();

        setupConfiguredThisSession = true;
        ApplyDataToUi();
        return true;
    }

    private void SaveSetupDraft(SetupWizardResult value)
    {
        settings.SetupDraftIsoPath = value.IsoPath;
        settings.SetupDraftWorkspacePath = value.WorkspacePath;
        settings.SetupDraftPcsx2Path = value.Pcsx2Path;
        settings.SetupDraftTplManagerPath = value.TplManagerPath;
        settings.SetupShowOnStartup = value.ShowAgain;
        SaveSettings();
    }

    private async Task CompleteSetupAsync(SetupWizardResult value, IProgress<string> progress)
    {
        progress.Report("Validando a ISO e procurando o BIO4DAT.AFS...");
        var afsFiles = await Task.Run(() => AfsService.FindAfsFiles(value.IsoPath));
        if (afsFiles.Count == 0)
            throw new InvalidDataException("A ISO selecionada não contém nenhum arquivo AFS compatível.");

        progress.Report("Criando a estrutura do workspace...");
        SetWorkspace(value.WorkspacePath);
        project.IsoPath = value.IsoPath;
        settings.Pcsx2Path = value.Pcsx2Path;
        settings.TplManagerPath = string.IsNullOrWhiteSpace(value.TplManagerPath) ? null : value.TplManagerPath;
        SaveSetupDraft(value);
        ApplyDataToUi(refreshContent: false, refreshBuild: false);
        SaveProject();

        progress.Report("Lendo o índice de arquivos do AFS...");
        await LoadIsoAfsAsync();
        if (loadedAfs == null)
            throw new InvalidDataException("Não foi possível abrir o AFS principal da ISO selecionada.");

        List<AfsEntry> scenarios = GetBulkScenarioEntries();
        if (value.Preparation != SetupPreparation.None && scenarios.Count == 0)
            throw new InvalidDataException("Nenhum cenário principal entre r100.dat e r534.dat foi encontrado.");

        if (value.Preparation == SetupPreparation.FirstScenario)
        {
            progress.Report($"Extraindo o cenário inicial {scenarios[0].FileName}...");
            await ExtractScenarioAsync(scenarios[0], updateActiveProject: true);
        }
        else if (value.Preparation == SetupPreparation.AllScenarios)
        {
            for (int index = 0; index < scenarios.Count; index++)
            {
                progress.Report($"Extraindo cenário {index + 1} de {scenarios.Count}: {scenarios[index].FileName}...");
                await ExtractScenarioAsync(scenarios[index], updateActiveProject: index == 0);
            }
        }

        progress.Report("Finalizando e salvando as preferências...");
        settings.SetupCompleted = true;
        settings.SetupShowOnStartup = value.ShowAgain;
        if (!value.ShowAgain)
        {
            settings.SetupDraftIsoPath = null;
            settings.SetupDraftWorkspacePath = null;
            settings.SetupDraftPcsx2Path = null;
            settings.SetupDraftTplManagerPath = null;
        }
        SaveProject();
        RefreshExtractedContent();
        UpdateBuildUi();
        await RefreshTrackedDatsAsync();
    }

    private async void btnSettingsRunSetup_Click(object? sender, EventArgs e)
    {
        await RunSetupWizardAsync(force: true);
        ApplyGeneralSettingsToUi();
    }

    private static string FirstFilled(string? preferred, string? fallback)
        => !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback ?? string.Empty;
}
