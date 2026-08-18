namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void btnBuildAll_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        if (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath)) { MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe em Tools.", "Build All", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(settings.Pcsx2Path) || !File.Exists(settings.Pcsx2Path)) { MessageBox.Show("Configure o PCSX2 em Tools.", "Build All", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(project.IsoPath) || !File.Exists(project.IsoPath)) { MessageBox.Show("Selecione uma ISO base válida.", "Build All", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        string buildIso = Path.Combine(project.RootPath!, "Build", "RE4_PS2_MOD.iso");
        try
        {
            btnBuildAll.Enabled = false; btnBuildOneClick.Enabled = false;
            WriteLog("=== BUILD ALL & TEST ===");
            var statuses = await GetTrackedDatStatusesAsync();
            var targets = statuses.Where(x => x.NeedsRepack || x.NeedsInject).ToArray();
            if (targets.Length == 0)
            {
                WriteLog("Nenhum DAT modificado. Abrindo a ISO de Build existente.");
                if (!File.Exists(buildIso)) throw new FileNotFoundException("Nenhum DAT precisa de build, mas a ISO de Build ainda não existe.");
                LaunchPcsx2WithIso(buildIso); return;
            }
            if (File.Exists(buildIso) && !string.IsNullOrWhiteSpace(project.BuildIsoSourcePath) && !string.Equals(Path.GetFullPath(project.BuildIsoSourcePath), Path.GetFullPath(project.IsoPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A ISO base mudou. Use RECRIAR ISO LIMPA antes do Build All.");
            if (!File.Exists(buildIso))
            {
                WriteLog("Criando ISO de Build inicial..."); Directory.CreateDirectory(Path.GetDirectoryName(buildIso)!);
                await Task.Run(() => File.Copy(project.IsoPath!, buildIso, false));
                project.BuildIsoSourcePath = project.IsoPath; project.ActiveBuildIsoPath = buildIso;
                if (project.BuildIsoGeneration <= 0) project.BuildIsoGeneration = 1;
                WriteLog("ISO de Build criada.");
            }
            else WriteLog("FAST BUILD: reutilizando ISO de Build existente.");

            foreach (var target in targets)
            {
                var st = target.State; string scenario = Path.GetFileNameWithoutExtension(st.DatName);
                WriteLog($"--- {st.DatName} ---");
                if (target.NeedsRepack)
                {
                    if (target.PendingTpl > 0)
                    {
                        int injectedTpl = InjectPendingTplChanges(st.ContentPath!, st.DatName);
                        WriteLog($"TPLs pendentes injetados: {injectedTpl}.");
                    }
                    string stagingDir = Path.Combine(project.RootPath!, "Temp", "Repack", scenario);
                    string outputDat = Path.Combine(project.RootPath!, "Build", scenario, st.DatName);
                    var repack = await DatToolService.RepackAsync(settings.DatToolPath!, st.ContentPath!, st.DatName, stagingDir, outputDat);
                    if (!string.IsNullOrWhiteSpace(repack.Output)) foreach (string line in repack.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) WriteLog("DAT Tool: " + line);
                    if (repack.ExitCode != 0) throw new InvalidOperationException($"{st.DatName}: DAT Tool terminou com código {repack.ExitCode}.");
                    st.BuildDatPath = repack.OutputDatPath;
                }
                else WriteLog("Content já compilado; pulando repack e reinjetando o DAT de Build existente.");

                if (string.IsNullOrWhiteSpace(st.BuildDatPath) || !File.Exists(st.BuildDatPath)) throw new FileNotFoundException($"DAT de Build não encontrado para {st.DatName}.");
                string afsPath = st.AfsPath ?? project.ActiveAfsPath ?? "DATA/BIO4DAT.AFS";
                var afsFiles = await Task.Run(() => AfsService.FindAfsFiles(buildIso));
                var afsFile = afsFiles.FirstOrDefault(x => x.FullPath.Equals(afsPath, StringComparison.OrdinalIgnoreCase)) ?? afsFiles.FirstOrDefault(x => x.Name.Equals("BIO4DAT.AFS", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException("BIO4DAT.AFS não encontrado na ISO de Build.");
                var afs = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, afsFile));
                var entry = AfsService.FindFirstValidEntryByName(afs, st.DatName) ?? throw new InvalidDataException($"{st.DatName} não encontrado em {afsFile.Name}.");
                long buildSize = new FileInfo(st.BuildDatPath).Length;
                if (buildSize > entry.AllocatedSize) throw new InvalidOperationException($"{st.DatName} excede o Reserved Space em {FormatBytes(buildSize - entry.AllocatedSize)}. Build All interrompido antes de corromper a ISO.");
                await Task.Run(() => AfsService.InjectEntryInPlace(afs, entry, st.BuildDatPath));
                var verify = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, afsFile));
                if (verify.Entries.First(x => x.Index == entry.Index).CurrentSize != buildSize) throw new InvalidDataException($"{st.DatName}: validação após injeção falhou.");
                var snapshot = await Task.Run(() => ChangeDetectionService.Capture(st.ContentPath!));
                ChangeDetectionService.Save(GetChangeStatePath(st.DatName), snapshot);
                if (target.NeedsRepack) st.LastBuildUtc = DateTime.UtcNow;
                st.InjectedGeneration = project.BuildIsoGeneration;
                WriteLog($"{st.DatName}: {(target.NeedsRepack ? "repack + " : "")}Fast Inject concluído ({FormatBytes(buildSize)}).");
            }
            project.ActiveBuildIsoPath = buildIso; project.BuildIsoSourcePath = project.IsoPath;
            var activeState = !string.IsNullOrWhiteSpace(project.ActiveDatName) ? GetDatState(project.ActiveDatName, false) : null;
            if (activeState != null) { project.ActiveBuildDatPath = activeState.BuildDatPath; project.LastBuildUtc = activeState.LastBuildUtc; }
            SaveProject(); UpdateBuildUi(); await RefreshTrackedDatsAsync(); await RefreshChangeStatusAsync();
            WriteLog($"BUILD ALL concluído: {targets.Length} DAT(s) reconstruído(s) e injetado(s). Abrindo PCSX2...");
            LaunchPcsx2WithIso(buildIso);
        }
        catch (Exception ex) { WriteLog("ERRO NO BUILD ALL: " + ex.Message); MessageBox.Show(ex.Message, "Build All", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { btnBuildAll.Enabled = true; btnBuildOneClick.Enabled = true; await RefreshTrackedDatsAsync(); }
    }

    private void lvTrackedDats_DoubleClick(object? sender, EventArgs e)
    {
        if (lvTrackedDats.SelectedItems.Count != 1 || lvTrackedDats.SelectedItems[0].Tag is not string datName) return;
        for (int i = 0; i < cmbDatEntries.Items.Count; i++)
        {
            if (cmbDatEntries.Items[i] is AfsEntry entry && entry.FileName.Equals(datName, StringComparison.OrdinalIgnoreCase))
            {
                cmbDatEntries.SelectedIndex = i;
                btnNavAssets_Click(null, EventArgs.Empty);
                return;
            }
        }
        MessageBox.Show($"{datName} pertence a outro AFS ou não está na lista atualmente carregada.", "DAT acompanhado", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LaunchPcsx2WithIso(string isoPath)
    {
        if (string.IsNullOrWhiteSpace(settings.Pcsx2Path) || !File.Exists(settings.Pcsx2Path)) throw new FileNotFoundException("PCSX2 não configurado.");
        if (!File.Exists(isoPath)) throw new FileNotFoundException("ISO de Build não encontrada.", isoPath);
        var psi = new ProcessStartInfo(settings.Pcsx2Path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(settings.Pcsx2Path)! };
        psi.ArgumentList.Add(isoPath);
        Process.Start(psi);
    }
}
