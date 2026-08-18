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
            btnScanIso.Enabled = false; btnExtractScenario.Enabled = false; cmbAfsEntries.Enabled = false; cmbAfsEntries.Items.Clear(); cmbDatEntries.Items.Clear(); loadedAfs = null;
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
        finally { btnScanIso.Enabled = true; cmbAfsEntries.Enabled = cmbAfsEntries.Items.Count > 0; btnExtractScenario.Enabled = cmbDatEntries.SelectedItem is AfsEntry; }
    }

    private async Task LoadSelectedAfsAsync(string? preferredDatName = null)
    {
        string? iso = Clean(txtIsoPath.Text);
        if (string.IsNullOrWhiteSpace(iso) || !File.Exists(iso) || cmbAfsEntries.SelectedItem is not IsoFileEntry selected) return;
        try
        {
            cmbAfsEntries.Enabled = false; btnExtractScenario.Enabled = false; cmbDatEntries.Items.Clear(); loadedAfs = null;
            ExtractLog($"Abrindo AFS: {selected.FullPath}...");
            loadedAfs = await Task.Run(() => AfsService.OpenAfsFromIso(iso, selected));
            lblAfsName.Text = $"AFS ativo: {selected.FullPath}  |  {FormatBytes(selected.Size)}";
            var dats = AfsService.GetUniqueValidDatEntries(loadedAfs).ToArray();
            cmbDatEntries.Items.AddRange(dats.Cast<object>().ToArray());
            project.ActiveAfsPath = selected.FullPath; SaveProject();
            ExtractLog($"AFS carregado: {selected.FullPath}");
            ExtractLog($"{dats.Length:N0} arquivos DAT encontrados.");
            if (dats.Length > 0)
            {
                int datIndex = !string.IsNullOrWhiteSpace(preferredDatName) ? Array.FindIndex(dats, x => x.FileName.Equals(preferredDatName, StringComparison.OrdinalIgnoreCase)) : -1;
                cmbDatEntries.SelectedIndex = datIndex >= 0 ? datIndex : 0;
            }
        }
        catch (Exception ex)
        {
            lblAfsName.Text = "AFS: erro ao carregar";
            ExtractLog("ERRO AO ABRIR AFS: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro ao abrir AFS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { cmbAfsEntries.Enabled = true; btnExtractScenario.Enabled = cmbDatEntries.SelectedItem is AfsEntry; }
    }

    private async void cmbAfsEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!cmbAfsEntries.Enabled) return;
        await LoadSelectedAfsAsync();
    }

    private void cmbDatEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbDatEntries.SelectedItem is not AfsEntry entry) { lblDatCurrentSize.Text = "—"; lblDatReservedSize.Text = "—"; lblDatFreeSpace.Text = "—"; btnExtractScenario.Enabled = false; return; }
        lblDatCurrentSize.Text = FormatBytes(entry.CurrentSize);
        lblDatReservedSize.Text = FormatBytes(entry.AllocatedSize);
        lblDatFreeSpace.Text = FormatBytes(entry.FreeSpace);
        btnExtractScenario.Enabled = true; project.ActiveDatName = entry.FileName;
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
        SaveProject(); RefreshDashboard(); RefreshExtractedContent(); RefreshTextureSmdList(); UpdateBuildUi(); _ = RefreshTrackedDatsAsync();
    }

    private async void btnExtractScenario_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace() || loadedAfs == null || cmbDatEntries.SelectedItem is not AfsEntry entry) return;
        if (entry.IsDummy) { MessageBox.Show("Este arquivo é um dummy file com tamanho real 0 e não pode ser extraído.", "Dummy file", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath)) { MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe na tela Tools primeiro.", "DAT Tool", MessageBoxButtons.OK, MessageBoxIcon.Information); btnNavTools_Click(null, EventArgs.Empty); return; }
        string scenario = Path.GetFileNameWithoutExtension(entry.FileName);
        string root = Path.Combine(project.RootPath!, "Extracted", scenario);
        string originalDir = Path.Combine(root, "OriginalDAT");
        string contentDir = Path.Combine(root, "Content");
        string datPath = Path.Combine(originalDir, entry.FileName);
        try
        {
            btnExtractScenario.Enabled = false;
            ExtractLog($"Extraindo {entry.FileName} do AFS...");
            await Task.Run(() => AfsService.ExtractEntry(loadedAfs, entry, datPath));
            ExtractLog($"DAT extraído: {datPath}");
            ExtractLog("Executando RE4_UHD_DAT_Tool.exe -x...");
            var result = await DatToolService.ExtractAsync(settings.DatToolPath!, datPath, contentDir);
            if (!string.IsNullOrWhiteSpace(result.Output)) foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) ExtractLog("DAT Tool: " + line);
            if (result.ExitCode != 0) throw new InvalidOperationException($"A DAT Tool terminou com código {result.ExitCode}.");
            project.ActiveDatPath = datPath; project.ActiveDatName = entry.FileName; project.ActiveContentPath = contentDir; project.ActiveBuildDatPath = null;
            var datState = GetDatState(entry.FileName, true)!;
            datState.OriginalDatPath = datPath; datState.ContentPath = contentDir; datState.BuildDatPath = null; datState.AfsPath = project.ActiveAfsPath; datState.LastBuildUtc = null;
            var initialSnapshot = await Task.Run(() => ChangeDetectionService.Capture(contentDir));
            ChangeDetectionService.Save(GetChangeStatePath(entry.FileName), initialSnapshot);
            project.LastBuildUtc = null;
            SaveProject(); ApplyDataToUi();
            int files = Directory.Exists(contentDir) ? Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories).Length : 0;
            RefreshExtractedContent();
            await RefreshTrackedDatsAsync();
            ExtractLog($"Scenario ready. {files:N0} arquivo(s) em Content.");
            MessageBox.Show($"Cenário extraído com sucesso.\n\n{root}", "Extract Scenario", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ExtractLog("ERRO: " + ex.Message); MessageBox.Show(ex.Message, "Erro na extração", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { btnExtractScenario.Enabled = true; }
    }

    private void ExtractLog(string text)
    {
        if (rtbExtractLog == null || rtbExtractLog.IsDisposed) return;
        rtbExtractLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}"); rtbExtractLog.ScrollToCaret();
    }
}
