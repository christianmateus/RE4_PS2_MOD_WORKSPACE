namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void btnScanIso_Click(object? sender, EventArgs e) => await LoadIsoAfsAsync();

    private async Task LoadIsoAfsAsync(string? preferredAfsPath = null, string? preferredDatName = null)
    {
        if (!RequireWorkspace()) return;
        string? iso = Clean(txtIsoPath.Text);
        if (string.IsNullOrWhiteSpace(iso) || !File.Exists(iso)) { MessageBox.Show("Selecione uma ISO válida primeiro.", "ISO", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        try
        {
            btnScanIso.Enabled = false; btnExtractScenario.Enabled = false; btnExtractAllScenarios.Enabled = false; cmbAfsEntries.Enabled = false; cmbAfsEntries.Items.Clear(); cmbDatEntries.Items.Clear(); loadedAfs = null;
            lblAfsName.Text = "AFS: procurando...";
            ExtractLog("Lendo ISO9660 e procurando arquivos AFS...");
            var afsFiles = await Task.Run(() => AfsService.FindAfsFiles(iso));
            if (afsFiles.Count == 0) throw new InvalidDataException("Nenhum arquivo .AFS foi encontrado na ISO.");
            cmbAfsEntries.Items.AddRange(afsFiles.Cast<object>().ToArray());
            int preferred = -1;
            if (!string.IsNullOrWhiteSpace(preferredAfsPath))
                preferred = Array.FindIndex(afsFiles.ToArray(), x => x.FullPath.Equals(preferredAfsPath, StringComparison.OrdinalIgnoreCase));
            if (preferred < 0) preferred = Array.FindIndex(afsFiles.ToArray(), x => x.Name.Equals("BIO4DAT.AFS", StringComparison.OrdinalIgnoreCase));
            cmbAfsEntries.SelectedIndex = preferred >= 0 ? preferred : 0;
            cmbAfsEntries.Enabled = true;
            project.IsoPath = iso; SaveProject();
            ExtractLog($"{afsFiles.Count:N0} arquivo(s) AFS encontrado(s). Selecionado: {((IsoFileEntry)cmbAfsEntries.SelectedItem!).FullPath}");
            await LoadSelectedAfsAsync(preferredDatName);
        }
        catch (Exception ex) { ExtractLog("ERRO: " + ex.Message); MessageBox.Show(ex.Message, "Erro ao ler ISO/AFS", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { btnScanIso.Enabled = true; cmbAfsEntries.Enabled = cmbAfsEntries.Items.Count > 0; btnExtractScenario.Enabled = cmbDatEntries.SelectedItem is AfsEntry; btnExtractAllScenarios.Enabled = GetBulkScenarioEntries().Count > 0; }
    }

    private async Task LoadSelectedAfsAsync(string? preferredDatName = null)
    {
        string? iso = Clean(txtIsoPath.Text);
        if (string.IsNullOrWhiteSpace(iso) || !File.Exists(iso) || cmbAfsEntries.SelectedItem is not IsoFileEntry selected) return;
        try
        {
            cmbAfsEntries.Enabled = false; btnExtractScenario.Enabled = false; btnExtractAllScenarios.Enabled = false; cmbDatEntries.Items.Clear(); loadedAfs = null;
            ExtractLog($"Abrindo AFS: {selected.FullPath}...");
            loadedAfs = await Task.Run(() => AfsService.OpenAfsFromIso(iso, selected));
            lblAfsName.Text = $"AFS ativo: {selected.FullPath}  |  {FormatBytes(selected.Size)}";
            AfsEntry[] entries = GetVisibleAfsEntries();
            cmbDatEntries.Items.AddRange(entries.Cast<object>().ToArray());
            project.ActiveAfsPath = selected.FullPath; SaveProject();
            ExtractLog($"AFS carregado: {selected.FullPath}");
            ExtractLog($"{AfsService.GetUniqueValidEntries(loadedAfs).Count:N0} arquivo(s) válido(s) no AFS • {AfsService.GetUniqueValidDatEntries(loadedAfs).Count:N0} DAT(s).");
            if (entries.Length > 0)
            {
                int datIndex = !string.IsNullOrWhiteSpace(preferredDatName) ? Array.FindIndex(entries, x => x.FileName.Equals(preferredDatName, StringComparison.OrdinalIgnoreCase)) : -1;
                cmbDatEntries.SelectedIndex = datIndex >= 0 ? datIndex : 0;
            }
        }
        catch (Exception ex)
        {
            lblAfsName.Text = "AFS: erro ao carregar";
            ExtractLog("ERRO AO ABRIR AFS: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro ao abrir AFS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { cmbAfsEntries.Enabled = true; btnExtractScenario.Enabled = cmbDatEntries.SelectedItem is AfsEntry; btnExtractAllScenarios.Enabled = GetBulkScenarioEntries().Count > 0; }
    }

    private async void cmbAfsEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!cmbAfsEntries.Enabled) return;
        await LoadSelectedAfsAsync();
    }

    private void cmbDatEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbDatEntries.SelectedItem is not AfsEntry entry) { lblDatCurrentSize.Text = "—"; lblDatReservedSize.Text = "—"; lblDatFreeSpace.Text = "—"; btnExtractScenario.Enabled = false; btnRestoreDat.Enabled = false; return; }
        lblDatCurrentSize.Text = FormatBytes(entry.CurrentSize);
        lblDatReservedSize.Text = FormatBytes(entry.AllocatedSize);
        lblDatFreeSpace.Text = FormatBytes(entry.FreeSpace);
        btnExtractScenario.Enabled = true;
        bool isDat = entry.FileName.EndsWith(".DAT", StringComparison.OrdinalIgnoreCase);
        btnExtractScenario.Text = isDat ? "EXTRAIR DAT" : "EXTRAIR ARQUIVO";
        btnRestoreDat.Enabled = isDat && IsDatExtracted(entry.FileName);
        if (!isDat) return;
        project.ActiveDatName = entry.FileName;
        var datState = GetDatState(entry.FileName, false);
        if (datState != null)
        {
            project.ActiveDatPath = datState.OriginalDatPath;
            project.ActiveContentPath = datState.ContentPath;
            project.ActiveBuildDatPath = datState.BuildDatPath;
            project.LastBuildUtc = datState.LastBuildUtc;
        }
        else if (!string.IsNullOrWhiteSpace(project.RootPath))
        {
            project.ActiveDatPath = null;
            project.ActiveContentPath = Path.Combine(project.RootPath, "Extracted", Path.GetFileNameWithoutExtension(entry.FileName), "Content");
            project.ActiveBuildDatPath = null;
            project.LastBuildUtc = null;
        }
        if (restoringSession)
        {
            RefreshDashboard();
            return;
        }
        SaveProject(); RefreshDashboard(); RefreshExtractedContent(); UpdateBuildUi(); _ = RefreshTrackedDatsAsync();
        if (pnlMessages?.Visible == true) LoadMessagesForActiveDat(false);
    }

    private async void btnExtractScenario_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace() || loadedAfs == null || cmbDatEntries.SelectedItem is not AfsEntry entry) return;
        if (entry.IsDummy) { MessageBox.Show("Este arquivo é um dummy file com tamanho real 0 e não pode ser extraído.", "Dummy file", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        bool isDat = entry.FileName.EndsWith(".DAT", StringComparison.OrdinalIgnoreCase);
        if (isDat && (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath))) { MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe na tela Tools primeiro.", "DAT Tool", MessageBoxButtons.OK, MessageBoxIcon.Information); btnNavTools_Click(null, EventArgs.Empty); return; }
        try
        {
            btnExtractScenario.Enabled = false;
            if (isDat)
            {
                await ExtractScenarioAsync(entry, updateActiveProject: true);
                string root = Path.Combine(project.RootPath!, "Extracted", Path.GetFileNameWithoutExtension(entry.FileName));
                ApplyDataToUi(); await RefreshTrackedDatsAsync();
                MessageBox.Show($"Pacote DAT extraído e aberto com sucesso.\n\n{root}", "Extrair DAT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                string afsName = Path.GetFileNameWithoutExtension(loadedAfs.IsoAfsEntry.Name);
                string destination = Path.Combine(project.RootPath!, "Extracted", "_AFS", afsName, Path.GetFileName(entry.FileName));
                ExtractLog($"Extraindo arquivo {entry.FileName} do AFS...");
                await Task.Run(() => AfsService.ExtractEntry(loadedAfs, entry, destination));
                ExtractLog($"Arquivo extraído: {destination}");
                MessageBox.Show($"Arquivo extraído com sucesso.\n\n{destination}", "Extrair arquivo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex) { ExtractLog("ERRO: " + ex.Message); MessageBox.Show(ex.Message, "Erro na extração", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            btnExtractScenario.Enabled = true;
            btnRestoreDat.Enabled = isDat && IsDatExtracted(entry.FileName);
        }
    }

    private bool IsDatExtracted(string datName)
    {
        if (string.IsNullOrWhiteSpace(project.RootPath)) return false;
        return Directory.Exists(Path.Combine(project.RootPath, "Extracted", Path.GetFileNameWithoutExtension(datName), "Content"));
    }

    private void DeleteDatWorkingArtifacts(string datName)
    {
        string root = project.RootPath ?? throw new InvalidOperationException("Workspace não definido.");
        string scenario = Path.GetFileNameWithoutExtension(datName);
        foreach (string target in new[]
        {
            Path.Combine(root, "Extracted", scenario),
            Path.Combine(root, "Mods", scenario),
            Path.Combine(root, "Build", scenario),
            Path.Combine(root, "Temp", "Repack", scenario)
        })
        {
            EnsureGeneratedPathIsSafe(root, target);
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
        string statePath = GetChangeStatePath(datName);
        EnsureGeneratedPathIsSafe(root, statePath);
        if (File.Exists(statePath)) File.Delete(statePath);
    }

    private async void btnRestoreDat_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace() || loadedAfs == null || cmbDatEntries.SelectedItem is not AfsEntry entry || !entry.FileName.EndsWith(".DAT", StringComparison.OrdinalIgnoreCase)) return;
        if (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath)) { MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe na tela Tools primeiro.", "DAT Tool", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        string warning = $"Restaurar {entry.FileName} diretamente da ISO original?\n\nSerão descartados somente os dados derivados desse DAT em Extracted, Mods, Build e Temp. Os demais pacotes não serão alterados.";
        if (MessageBox.Show(warning, "RESTAURAR DAT DA ISO", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            btnRestoreDat.Enabled = false; btnExtractScenario.Enabled = false; btnExtractAllScenarios.Enabled = false;
            ClearVisualEditor("Restaurando DAT original...");
            ResetTextureUi("Restaurando DAT original...");
            await Task.Run(() => DeleteDatWorkingArtifacts(entry.FileName));
            project.DatStates?.RemoveAll(x => x.DatName.Equals(entry.FileName, StringComparison.OrdinalIgnoreCase));
            await ExtractScenarioAsync(entry, updateActiveProject: true);
            ApplyDataToUi(); await RefreshTrackedDatsAsync();
            ExtractLog($"DAT restaurado da ISO original: {entry.FileName}");
            MessageBox.Show($"{entry.FileName} foi restaurado e extraído novamente da ISO original.", "DAT restaurado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ExtractLog("ERRO AO RESTAURAR DAT: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro ao restaurar DAT", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExtractScenario.Enabled = true; btnExtractAllScenarios.Enabled = GetBulkScenarioEntries().Count > 0; btnRestoreDat.Enabled = IsDatExtracted(entry.FileName);
        }
    }

    private AfsEntry[] GetVisibleAfsEntries()
    {
        if (loadedAfs == null) return Array.Empty<AfsEntry>();
        return (chkShowAllAfsFiles?.Checked == true
                ? AfsService.GetUniqueValidEntries(loadedAfs)
                : AfsService.GetUniqueValidDatEntries(loadedAfs))
            .ToArray();
    }

    private void chkShowAllAfsFiles_CheckedChanged(object? sender, EventArgs e)
    {
        if (loadedAfs == null) return;
        string? previous = (cmbDatEntries.SelectedItem as AfsEntry)?.FileName;
        AfsEntry[] entries = GetVisibleAfsEntries();
        cmbDatEntries.BeginUpdate(); cmbDatEntries.Items.Clear(); cmbDatEntries.Items.AddRange(entries.Cast<object>().ToArray()); cmbDatEntries.EndUpdate();
        int selected = !string.IsNullOrWhiteSpace(previous) ? Array.FindIndex(entries, x => x.FileName.Equals(previous, StringComparison.OrdinalIgnoreCase)) : -1;
        if (selected < 0 && !string.IsNullOrWhiteSpace(project.ActiveDatName)) selected = Array.FindIndex(entries, x => x.FileName.Equals(project.ActiveDatName, StringComparison.OrdinalIgnoreCase));
        cmbDatEntries.SelectedIndex = selected >= 0 ? selected : (entries.Length > 0 ? 0 : -1);
        ExtractLog(chkShowAllAfsFiles.Checked ? $"Exibindo todos os {entries.Length:N0} arquivos válidos do AFS." : $"Filtro DAT ativo: {entries.Length:N0} pacote(s).");
    }

    private async Task ExtractScenarioAsync(AfsEntry entry, bool updateActiveProject)
    {
        string scenario = Path.GetFileNameWithoutExtension(entry.FileName);
        string root = Path.Combine(project.RootPath!, "Extracted", scenario);
        string originalDir = Path.Combine(root, "OriginalDAT");
        string contentDir = Path.Combine(root, "Content");
        string datPath = Path.Combine(originalDir, entry.FileName);
        ExtractLog($"Extraindo {entry.FileName} do AFS...");
        await Task.Run(() => AfsService.ExtractEntry(loadedAfs!, entry, datPath));
            ExtractLog($"DAT extraído: {datPath}");
            if (entry.FileName.Length == 8 && entry.FileName.StartsWith("em", StringComparison.OrdinalIgnoreCase))
            {
                string enemiesDir = Path.Combine(project.RootPath!, "Extracted", "Enemies");
                Directory.CreateDirectory(enemiesDir);
                string enemyCache = Path.Combine(enemiesDir, entry.FileName);
                File.Copy(datPath, enemyCache, true);
                ExtractLog($"Cache de inimigo atualizado: {enemyCache}");
            }
            ExtractLog("Executando RE4_UHD_DAT_Tool.exe -x...");
            var result = await DatToolService.ExtractAsync(settings.DatToolPath!, datPath, contentDir);
            if (!string.IsNullOrWhiteSpace(result.Output)) foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) ExtractLog("DAT Tool: " + line);
            if (result.ExitCode != 0) throw new InvalidOperationException($"A DAT Tool terminou com código {result.ExitCode}.");
            if (updateActiveProject)
            {
                project.ActiveDatPath = datPath; project.ActiveDatName = entry.FileName; project.ActiveContentPath = contentDir; project.ActiveBuildDatPath = null; project.LastBuildUtc = null;
            }
            var datState = GetDatState(entry.FileName, true)!;
            datState.OriginalDatPath = datPath; datState.ContentPath = contentDir; datState.BuildDatPath = null; datState.AfsPath = project.ActiveAfsPath; datState.LastBuildUtc = null;
            var initialSnapshot = await Task.Run(() => ChangeDetectionService.Capture(contentDir));
            ChangeDetectionService.Save(GetChangeStatePath(entry.FileName), initialSnapshot);
            SaveProject();
            int files = Directory.Exists(contentDir) ? Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories).Length : 0;
            ExtractLog($"Pacote pronto. {files:N0} arquivo(s) em Content.");
    }

    private List<AfsEntry> GetBulkScenarioEntries()
    {
        if (loadedAfs == null) return new List<AfsEntry>();
        return AfsService.GetUniqueValidDatEntries(loadedAfs)
            .Select(entry => (Entry: entry, Number: ParseMainScenarioNumber(entry.FileName)))
            .Where(item => item.Number is >= 100 and <= 534)
            .OrderBy(item => item.Number)
            .Select(item => item.Entry)
            .ToList();
    }

    private static int? ParseMainScenarioNumber(string fileName)
    {
        // Exact rNNN.dat names only. Variants such as r100_01.dat are intentionally ignored.
        if (fileName.Length != 8 || (fileName[0] != 'r' && fileName[0] != 'R') ||
            !fileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)) return null;
        return int.TryParse(fileName.AsSpan(1, 3), out int number) ? number : null;
    }

    private async void btnExtractAllScenarios_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace() || loadedAfs == null) return;
        if (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath))
        {
            MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe na tela Tools primeiro.", "DAT Tool", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNavTools_Click(null, EventArgs.Empty);
            return;
        }

        List<AfsEntry> entries = GetBulkScenarioEntries();
        if (entries.Count == 0) { MessageBox.Show("Nenhum cenário entre r100.dat e r534.dat foi encontrado.", "Extrair todos", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (MessageBox.Show($"Extrair e abrir {entries.Count:N0} cenários, de r100.dat até r534.dat?\n\nArquivos com underline, como r100_01.dat, serão ignorados. Esse processo pode demorar.", "EXTRAIR TODOS OS CENÁRIOS", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        int completed = 0;
        var failures = new List<string>();
        try
        {
            btnExtractAllScenarios.Enabled = false; btnExtractScenario.Enabled = false; btnScanIso.Enabled = false; cmbAfsEntries.Enabled = false; cmbDatEntries.Enabled = false;
            foreach (AfsEntry entry in entries)
            {
                btnExtractAllScenarios.Text = $"EXTRAINDO {completed + 1}/{entries.Count}";
                lblAfsName.Text = $"Extraindo {entry.FileName} • {completed + 1}/{entries.Count}";
                Application.DoEvents();
                try { await ExtractScenarioAsync(entry, updateActiveProject: false); completed++; }
                catch (Exception ex) { failures.Add($"{entry.FileName}: {ex.Message}"); ExtractLog($"ERRO EM {entry.FileName}: {ex.Message}"); }
            }
            SaveProject(); ApplyDataToUi(); await RefreshTrackedDatsAsync();
            string summary = $"Extração concluída: {completed:N0} de {entries.Count:N0} cenário(s)." + (failures.Count > 0 ? $"\n\nFalhas: {failures.Count:N0}. Consulte o Console para detalhes." : "");
            MessageBox.Show(summary, "Extração em lote concluída", MessageBoxButtons.OK, failures.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ExtractLog("ERRO NA EXTRAÇÃO EM LOTE: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro na extração em lote", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExtractAllScenarios.Text = "EXTRAIR CENÁRIOS"; btnExtractAllScenarios.Enabled = entries.Count > 0; btnExtractScenario.Enabled = cmbDatEntries.SelectedItem is AfsEntry; btnScanIso.Enabled = true; cmbAfsEntries.Enabled = true; cmbDatEntries.Enabled = true;
            if (cmbAfsEntries.SelectedItem is IsoFileEntry selected) lblAfsName.Text = $"AFS ativo: {selected.FullPath}  |  {FormatBytes(selected.Size)}";
        }
    }

    private void ExtractLog(string text)
    {
        if (rtbExtractLog == null || rtbExtractLog.IsDisposed) return;
        rtbExtractLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}"); rtbExtractLog.ScrollToCaret();
    }
}
