namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private bool buildOperationBusy;

    private void SetBuildBusy(bool busy, string message = "Processando build...")
    {
        buildOperationBusy = busy;
        if (pnlBuildBusy != null && !pnlBuildBusy.IsDisposed)
        {
            lblBuildBusy.Text = message;
            pnlBuildBusy.Visible = busy;
            if (busy) pnlBuildBusy.BringToFront();
        }

        foreach (Button button in new[] { btnBuildOneClick, btnBuildAll, btnBuildRefreshChanges, btnBuildRefreshTracked, btnBuildRecreateIso, btnBuildClean, btnBuildResetWorkspace, btnBuildFolder })
            if (button != null && !button.IsDisposed) button.Enabled = !busy;

        UseWaitCursor = busy;
    }

    private static void EnsureGeneratedPathIsSafe(string workspaceRoot, string target)
    {
        string root = Path.GetFullPath(workspaceRoot);
        string fullTarget = Path.GetFullPath(target);
        string relative = Path.GetRelativePath(root, fullTarget);
        if (relative == "." || Path.IsPathRooted(relative) || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || relative == "..")
            throw new InvalidOperationException("O diretório de artefatos está fora do workspace e não pode ser removido.");
    }

    private void CleanGeneratedBuildArtifacts()
    {
        string root = project.RootPath ?? throw new InvalidOperationException("Workspace não definido.");
        string buildDir = Path.Combine(root, "Build");
        string repackDir = Path.Combine(root, "Temp", "Repack");
        EnsureGeneratedPathIsSafe(root, buildDir);
        EnsureGeneratedPathIsSafe(root, repackDir);

        if (Directory.Exists(buildDir)) Directory.Delete(buildDir, recursive: true);
        if (Directory.Exists(repackDir)) Directory.Delete(repackDir, recursive: true);
        Directory.CreateDirectory(buildDir);

        project.ActiveBuildDatPath = null;
        project.ActiveBuildIsoPath = null;
        project.BuildIsoSourcePath = null;
        project.LastBuildUtc = null;
        foreach (DatProjectState state in project.DatStates ?? new List<DatProjectState>())
        {
            state.BuildDatPath = null;
            state.LastBuildUtc = null;
            state.InjectedGeneration = 0;
        }
    }

    private async void btnBuildClean_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        const string warning = "Isso apagará todos os arquivos gerados em Build e os temporários de repack.\n\nSeus arquivos editados em Extracted/Content serão preservados.\n\nContinuar?";
        if (MessageBox.Show(warning, "LIMPAR BUILD", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            SetBuildBusy(true, "Limpando artefatos de build...");
            await Task.Run(CleanGeneratedBuildArtifacts);
            SaveProject();
            WriteLog("Limpeza completa: artefatos de Build e Temp/Repack removidos.");
            MessageBox.Show("Artefatos de build removidos. Seus arquivos modificados em Content foram preservados.", "Build limpo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            WriteLog("ERRO AO LIMPAR BUILD: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro ao limpar build", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBuildBusy(false);
            UpdateBuildUi();
            await RefreshTrackedDatsAsync();
        }
    }

    private void DeleteWorkspaceDataDirectories()
    {
        string root = project.RootPath ?? throw new InvalidOperationException("Workspace não definido.");
        foreach (string name in new[] { "Extracted", "Build", "Mods", "Temp" })
        {
            string target = Path.Combine(root, name);
            EnsureGeneratedPathIsSafe(root, target);
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
        foreach (string name in new[] { "Extracted", "Build", "Mods", "Temp" })
            Directory.CreateDirectory(Path.Combine(root, name));
    }

    private void ResetWorkspaceDataState()
    {
        project.ActiveDatPath = null;
        project.ActiveDatName = null;
        project.ActiveContentPath = null;
        project.ActiveBuildDatPath = null;
        project.ActiveBuildIsoPath = null;
        project.BuildIsoSourcePath = null;
        project.LastBuildUtc = null;
        project.SelectedContentRelativePath = null;
        project.BuildIsoGeneration = 0;
        project.DatStates?.Clear();

        currentEnemyEslPath = null;
        currentEnemyAfsEntry = null;
        selectedEnemyScene = null;
        OnEnemySceneLoaded(null);
        ResetVisualEnemyModelCache();
        ClearVisualEditor("Nenhum DAT extraído disponível.");
        ResetTextureUi("Extraia um pacote DAT para começar.");
    }

    private async void btnBuildResetWorkspace_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        const string warning = "Esta ação apagará permanentemente todo o conteúdo das pastas:\n\n• Extracted\n• Build\n• Mods\n• Temp\n\nA ISO original, as configurações e o arquivo do projeto serão preservados. Depois disso, será necessário extrair os DATs novamente.\n\nContinuar?";
        if (MessageBox.Show(warning, "LIMPAR TODOS OS DADOS DE TRABALHO", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            SetBuildBusy(true, "Removendo todos os dados de trabalho...");
            await Task.Run(DeleteWorkspaceDataDirectories);
            ResetWorkspaceDataState();
            SaveProject();
            ApplyDataToUi();
            RefreshExtractedContent();
            RefreshTextureSmdList();
            WriteLog("Limpeza total concluída: Extracted, Build, Mods e Temp foram zerados.");
            MessageBox.Show("Limpeza total concluída. A ISO original e as configurações foram preservadas.", "Workspace limpo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            WriteLog("ERRO NA LIMPEZA TOTAL: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro na limpeza total", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBuildBusy(false);
            UpdateBuildUi();
            await RefreshTrackedDatsAsync();
        }
    }
}
