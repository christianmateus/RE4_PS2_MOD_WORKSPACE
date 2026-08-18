namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private string GetChangeStatePath(string? datName = null)
    {
        datName ??= project.ActiveDatName;
        string scenario = string.IsNullOrWhiteSpace(datName) ? "active" : Path.GetFileNameWithoutExtension(datName);
        return Path.Combine(project.RootPath ?? AppContext.BaseDirectory, ".workspace", "state", scenario + ".lastbuild.json");
    }

    private async Task<(SnapshotDiff Diff, int PendingTpl)> GetChangeStateAsync()
    {
        string? content = GetActiveContentPath();
        if (string.IsNullOrWhiteSpace(content) || !Directory.Exists(content)) return (new SnapshotDiff(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()), 0);
        return await GetChangeStateAsync(project.ActiveDatName!, content);
    }

    private async Task<(SnapshotDiff Diff, int PendingTpl)> GetChangeStateAsync(string datName, string contentDir)
    {
        var current = await Task.Run(() => ChangeDetectionService.Capture(contentDir));
        var baseline = ChangeDetectionService.Load(GetChangeStatePath(datName));
        var diff = ChangeDetectionService.Compare(baseline, current);
        int pendingTpl = await Task.Run(() => CountPendingTplChanges(contentDir, datName));
        return (diff, pendingTpl);
    }

    private int CountPendingTplChanges(string contentDir, string? datName = null)
    {
        datName ??= project.ActiveDatName;
        int count = 0;
        foreach (string smd in Directory.GetFiles(contentDir, "*.SMD", SearchOption.AllDirectories))
        {
            string tpl = GetTplWorkPath(smd, datName);
            if (File.Exists(tpl) && !SmdTextureService.TplMatchesSmd(smd, tpl)) count++;
        }
        return count;
    }

    private int InjectPendingTplChanges(string contentDir, string? datName = null)
    {
        datName ??= project.ActiveDatName;
        int count = 0;
        foreach (string smd in Directory.GetFiles(contentDir, "*.SMD", SearchOption.AllDirectories))
        {
            string tpl = GetTplWorkPath(smd, datName);
            if (!File.Exists(tpl) || SmdTextureService.TplMatchesSmd(smd, tpl)) continue;
            string backup = GetSmdBackupPath(smd, datName);
            SmdTextureService.InjectTpl(smd, tpl, backup);
            WriteLog($"TPL alterado detectado e injetado automaticamente: {Path.GetFileName(tpl)} -> {Path.GetFileName(smd)}");
            count++;
        }
        return count;
    }

    private async void btnBuildRefreshChanges_Click(object? sender, EventArgs e) => await RefreshChangeStatusAsync();

    private async Task RefreshChangeStatusAsync()
    {
        if (lblBuildChangeStatus == null || lblBuildChangeStatus.IsDisposed) return;
        string? content = GetActiveContentPath();
        if (string.IsNullOrWhiteSpace(content) || !Directory.Exists(content))
        {
            lblBuildChangeStatus.Text = "Nenhum Content ativo. Extraia um cenário primeiro.";
            lblBuildChangeStatus.ForeColor = Color.FromArgb(145, 151, 163);
            if (btnBuildOneClick != null) btnBuildOneClick.Enabled = false;
            return;
        }
        try
        {
            var state = await GetChangeStateAsync();
            bool hasBuildIso = !string.IsNullOrWhiteSpace(project.RootPath) && File.Exists(Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso"));
            if (!state.Diff.HasChanges && state.PendingTpl == 0)
            {
                lblBuildChangeStatus.Text = !project.LastBuildUtc.HasValue ? "Cenário extraído e ainda não compilado. BUILD & TEST fará o primeiro build." : (hasBuildIso ? "✓ Nenhuma alteração desde o último build. BUILD & TEST apenas abrirá o PCSX2." : "Nenhuma alteração detectada, mas a ISO de Build ainda precisa ser criada.");
                lblBuildChangeStatus.ForeColor = Color.FromArgb(113, 190, 137);
            }
            else
            {
                var parts = new List<string>();
                if (state.Diff.Total > 0) parts.Add($"{state.Diff.Total} arquivo(s) de Content alterado(s)");
                if (state.PendingTpl > 0) parts.Add($"{state.PendingTpl} TPL(s) aguardando injeção");
                lblBuildChangeStatus.Text = "Alterações detectadas: " + string.Join(" • ", parts);
                lblBuildChangeStatus.ForeColor = Color.FromArgb(236, 180, 92);
            }
            btnBuildOneClick.Enabled = true;
        }
        catch (Exception ex)
        {
            lblBuildChangeStatus.Text = "Falha ao verificar alterações: " + ex.Message;
            lblBuildChangeStatus.ForeColor = Color.FromArgb(220, 105, 105);
        }
    }

    private DatProjectState? GetDatState(string datName, bool create)
    {
        project.DatStates ??= new List<DatProjectState>();
        var state = project.DatStates.FirstOrDefault(x => x.DatName.Equals(datName, StringComparison.OrdinalIgnoreCase));
        if (state == null && create)
        {
            state = new DatProjectState { DatName = datName, AfsPath = project.ActiveAfsPath };
            project.DatStates.Add(state);
        }
        return state;
    }

    private void MigrateActiveDatState()
    {
        if (string.IsNullOrWhiteSpace(project.ActiveDatName)) return;
        var state = GetDatState(project.ActiveDatName, true)!;
        state.OriginalDatPath ??= project.ActiveDatPath;
        state.ContentPath ??= project.ActiveContentPath;
        state.BuildDatPath ??= project.ActiveBuildDatPath;
        state.AfsPath ??= project.ActiveAfsPath;
        state.LastBuildUtc ??= project.LastBuildUtc;
    }

    private async Task<List<TrackedDatStatus>> GetTrackedDatStatusesAsync()
    {
        MigrateActiveDatState();
        var states = (project.DatStates ?? new List<DatProjectState>())
            .Where(x => !string.IsNullOrWhiteSpace(x.DatName) && !string.IsNullOrWhiteSpace(x.ContentPath) && Directory.Exists(x.ContentPath))
            .OrderBy(x => x.DatName, StringComparer.OrdinalIgnoreCase).ToArray();
        var result = new List<TrackedDatStatus>();
        foreach (var state in states)
        {
            var change = await GetChangeStateAsync(state.DatName, state.ContentPath!);
            bool buildExists = !string.IsNullOrWhiteSpace(state.BuildDatPath) && File.Exists(state.BuildDatPath);
            bool needsRepack = !state.LastBuildUtc.HasValue || change.Diff.HasChanges || change.PendingTpl > 0 || !buildExists;
            bool needsInject = needsRepack || state.InjectedGeneration != project.BuildIsoGeneration || string.IsNullOrWhiteSpace(project.ActiveBuildIsoPath) || !File.Exists(project.ActiveBuildIsoPath);
            result.Add(new TrackedDatStatus(state, change.Diff, change.PendingTpl, needsRepack, needsInject));
        }
        return result;
    }

    private async Task RefreshTrackedDatsAsync()
    {
        if (lvTrackedDats == null || lvTrackedDats.IsDisposed) return;
        try
        {
            var statuses = await GetTrackedDatStatusesAsync();
            lvTrackedDats.BeginUpdate(); lvTrackedDats.Items.Clear();
            foreach (var status in statuses)
            {
                bool neverBuilt = !status.State.LastBuildUtc.HasValue && !status.Diff.HasChanges && status.PendingTpl == 0 && (string.IsNullOrWhiteSpace(status.State.BuildDatPath) || !File.Exists(status.State.BuildDatPath));
                string stateText = neverBuilt ? "NÃO COMPILADO" : status.NeedsRepack ? (status.PendingTpl > 0 ? "MODIFICADO + TPL" : "MODIFICADO") : (status.NeedsInject ? "AGUARDA INJEÇÃO" : "ATUALIZADO");
                var item = new ListViewItem(status.State.DatName) { Tag = status.State.DatName };
                item.SubItems.Add(stateText);
                item.SubItems.Add(status.Diff.Total.ToString("N0"));
                item.SubItems.Add(status.PendingTpl.ToString("N0"));
                item.SubItems.Add(status.State.LastBuildUtc.HasValue ? status.State.LastBuildUtc.Value.ToLocalTime().ToString("dd/MM HH:mm:ss") : "Nunca");
                lvTrackedDats.Items.Add(item);
            }
            lblTrackedDatsSummary.Text = statuses.Count == 0 ? "Nenhum DAT acompanhado. Extraia dois ou mais cenários para usar o Build All." : $"{statuses.Count} DAT(s) acompanhado(s) • {statuses.Count(x => x.NeedsRepack)} para repack • {statuses.Count(x => x.NeedsInject)} para injeção";
            btnBuildAll.Enabled = statuses.Count > 0;
        }
        catch (Exception ex) { lblTrackedDatsSummary.Text = "Erro ao verificar DATs: " + ex.Message; }
        finally { if (lvTrackedDats != null && !lvTrackedDats.IsDisposed) lvTrackedDats.EndUpdate(); }
    }

    private async void btnBuildRefreshTracked_Click(object? sender, EventArgs e) => await RefreshTrackedDatsAsync();

    private void UpdateBuildUi(long? rebuiltSize = null)
    {
        if (lblBuildActiveDat == null || lblBuildActiveDat.IsDisposed) return;
        lblBuildActiveDat.Text = string.IsNullOrWhiteSpace(project.ActiveDatName) ? "DAT ativo: nenhum" : "DAT ativo: " + project.ActiveDatName;
        btnBuildRepackDat.Enabled = !string.IsNullOrWhiteSpace(project.ActiveDatName) && Directory.Exists(GetActiveContentPath());
        btnBuildInjectIso.Enabled = !string.IsNullOrWhiteSpace(project.ActiveBuildDatPath) && File.Exists(project.ActiveBuildDatPath) && !string.IsNullOrWhiteSpace(project.IsoPath) && File.Exists(project.IsoPath);
        if (btnBuildRecreateIso != null && !btnBuildRecreateIso.IsDisposed) btnBuildRecreateIso.Enabled = !string.IsNullOrWhiteSpace(project.IsoPath) && File.Exists(project.IsoPath);
        string conventionalBuildIso = string.IsNullOrWhiteSpace(project.RootPath) ? "" : Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso");
        string? buildIso = !string.IsNullOrWhiteSpace(project.ActiveBuildIsoPath) && File.Exists(project.ActiveBuildIsoPath)
            ? project.ActiveBuildIsoPath
            : (!string.IsNullOrWhiteSpace(conventionalBuildIso) && File.Exists(conventionalBuildIso) ? conventionalBuildIso : null);
        if (!string.IsNullOrWhiteSpace(buildIso) && File.Exists(buildIso))
        {
            lblBuildIsoStatus.Text = "FAST BUILD ativo • ISO reutilizável: " + buildIso;
            lblBuildIsoStatus.ForeColor = Color.FromArgb(113, 190, 137);
        }
        else
        {
            lblBuildIsoStatus.Text = "Fast Build: a primeira injeção cria a ISO; as próximas reutilizam a cópia existente.";
            lblBuildIsoStatus.ForeColor = Color.FromArgb(145, 151, 163);
        }

        string? buildDat = project.ActiveBuildDatPath;
        long? size = rebuiltSize;
        if (!size.HasValue && !string.IsNullOrWhiteSpace(buildDat) && File.Exists(buildDat)) size = new FileInfo(buildDat).Length;
        if (!size.HasValue)
        {
            lblBuildDatStatus.Text = string.IsNullOrWhiteSpace(project.ActiveDatName) ? "Extraia e edite um cenário antes de reconstruir." : "Pronto para reconstruir a partir de Content.";
            lblBuildDatStatus.ForeColor = Color.FromArgb(145, 151, 163);
            _ = RefreshChangeStatusAsync();
            return;
        }

        AfsEntry? activeAfsEntry = cmbDatEntries?.SelectedItem as AfsEntry;
        if (activeAfsEntry != null && activeAfsEntry.FileName.Equals(project.ActiveDatName, StringComparison.OrdinalIgnoreCase))
        {
            long difference = activeAfsEntry.AllocatedSize - size.Value;
            if (difference >= 0)
            {
                lblBuildDatStatus.Text = $"Build: {FormatBytes(size.Value)}  •  Reserved: {FormatBytes(activeAfsEntry.AllocatedSize)}  •  Cabe no slot ({FormatBytes(difference)} livres)";
                lblBuildDatStatus.ForeColor = Color.FromArgb(113, 190, 137);
            }
            else
            {
                lblBuildDatStatus.Text = $"Build: {FormatBytes(size.Value)}  •  Reserved: {FormatBytes(activeAfsEntry.AllocatedSize)}  •  Excede em {FormatBytes(-difference)}";
                lblBuildDatStatus.ForeColor = Color.FromArgb(220, 105, 105);
            }
        }
        else
        {
            lblBuildDatStatus.Text = $"DAT reconstruído: {FormatBytes(size.Value)}  •  {buildDat}";
            lblBuildDatStatus.ForeColor = Color.FromArgb(113, 190, 137);
        }
        _ = RefreshChangeStatusAsync();
    }
}
