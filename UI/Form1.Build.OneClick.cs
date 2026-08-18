namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void btnBuildOneClick_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        if (string.IsNullOrWhiteSpace(project.ActiveDatName) || string.IsNullOrWhiteSpace(GetActiveContentPath())) { MessageBox.Show("Extraia um cenário primeiro.", "Build & Test", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath)) { MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe em Tools.", "Build & Test", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(settings.Pcsx2Path) || !File.Exists(settings.Pcsx2Path)) { MessageBox.Show("Configure o PCSX2 em Tools.", "Build & Test", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (string.IsNullOrWhiteSpace(project.IsoPath) || !File.Exists(project.IsoPath)) { MessageBox.Show("Selecione uma ISO base válida.", "Build & Test", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        string contentDir = GetActiveContentPath()!;
        string scenario = Path.GetFileNameWithoutExtension(project.ActiveDatName);
        string buildIso = Path.Combine(project.RootPath!, "Build", "RE4_PS2_MOD.iso");
        try
        {
            btnBuildOneClick.Enabled = false;
            WriteLog("=== BUILD & TEST ===");
            if (File.Exists(buildIso) && !string.IsNullOrWhiteSpace(project.BuildIsoSourcePath) && !string.Equals(Path.GetFullPath(project.BuildIsoSourcePath), Path.GetFullPath(project.IsoPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A ISO base mudou desde a criação da ISO de Build. Use RECRIAR ISO LIMPA antes do Build & Test.");

            var stateBefore = await GetChangeStateAsync();
            var activeDatState = GetDatState(project.ActiveDatName!, true)!;
            bool needRepack = !activeDatState.LastBuildUtc.HasValue || stateBefore.Diff.HasChanges || stateBefore.PendingTpl > 0 || string.IsNullOrWhiteSpace(project.ActiveBuildDatPath) || !File.Exists(project.ActiveBuildDatPath);
            bool needInject = needRepack || !File.Exists(buildIso) || activeDatState.InjectedGeneration != project.BuildIsoGeneration;

            if (stateBefore.PendingTpl > 0)
            {
                WriteLog($"Detectados {stateBefore.PendingTpl} TPL(s) editado(s) ainda não injetado(s). Injetando automaticamente...");
                int injectedTpl = InjectPendingTplChanges(contentDir);
                WriteLog($"TPLs injetados automaticamente: {injectedTpl}.");
            }

            if (needRepack)
            {
                string stagingDir = Path.Combine(project.RootPath!, "Temp", "Repack", scenario);
                string outputDat = Path.Combine(project.RootPath!, "Build", scenario, project.ActiveDatName);
                WriteLog("Mudanças detectadas: executando REPACK DAT...");
                var result = await DatToolService.RepackAsync(settings.DatToolPath!, contentDir, project.ActiveDatName, stagingDir, outputDat);
                if (!string.IsNullOrWhiteSpace(result.Output)) foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) WriteLog("DAT Tool: " + line);
                if (result.ExitCode != 0) throw new InvalidOperationException($"A DAT Tool terminou com código {result.ExitCode}.");
                project.ActiveBuildDatPath = result.OutputDatPath;
                var activeState = GetDatState(project.ActiveDatName!, true)!;
                activeState.BuildDatPath = result.OutputDatPath;
                SaveProject();
                WriteLog($"DAT reconstruído: {result.OutputDatPath} ({FormatBytes(result.NewSize)})");
            }
            else WriteLog("Nenhuma mudança em Content/TPL: repack ignorado.");

            if (needInject)
            {
                if (string.IsNullOrWhiteSpace(project.ActiveBuildDatPath) || !File.Exists(project.ActiveBuildDatPath)) throw new FileNotFoundException("DAT de Build não encontrado.");
                if (!File.Exists(buildIso))
                {
                    WriteLog("ISO de Build inexistente: criando cópia inicial da ISO original...");
                    Directory.CreateDirectory(Path.GetDirectoryName(buildIso)!);
                    await Task.Run(() => File.Copy(project.IsoPath!, buildIso, false));
                    if (project.BuildIsoGeneration <= 0) project.BuildIsoGeneration = 1;
                    WriteLog("ISO de Build criada.");
                }
                else WriteLog("FAST BUILD: reutilizando ISO de Build existente.");

                string afsPath = project.ActiveAfsPath ?? "DATA/BIO4DAT.AFS";
                long buildSize = new FileInfo(project.ActiveBuildDatPath).Length;
                var afsFiles = await Task.Run(() => AfsService.FindAfsFiles(buildIso));
                var afsFile = afsFiles.FirstOrDefault(x => x.FullPath.Equals(afsPath, StringComparison.OrdinalIgnoreCase))
                    ?? afsFiles.FirstOrDefault(x => x.Name.Equals("BIO4DAT.AFS", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidDataException("BIO4DAT.AFS não encontrado na ISO de Build.");
                var afs = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, afsFile));
                var entry = AfsService.FindFirstValidEntryByName(afs, project.ActiveDatName)
                    ?? throw new InvalidDataException($"{project.ActiveDatName} não encontrado no AFS.");
                if (buildSize > entry.AllocatedSize) throw new InvalidOperationException($"O DAT excede o Reserved Space em {FormatBytes(buildSize - entry.AllocatedSize)}. A injeção foi bloqueada.");
                WriteLog($"FAST INJECT: {project.ActiveDatName} ({FormatBytes(buildSize)}) em {afsFile.Name}...");
                await Task.Run(() => AfsService.InjectEntryInPlace(afs, entry, project.ActiveBuildDatPath!));
                var verify = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, afsFile));
                var verified = verify.Entries.First(x => x.Index == entry.Index);
                if (verified.CurrentSize != buildSize) throw new InvalidDataException("A validação do Current Size após a injeção falhou.");
                project.ActiveBuildIsoPath = buildIso;
                project.BuildIsoSourcePath = project.IsoPath;
                activeDatState.InjectedGeneration = project.BuildIsoGeneration;
                WriteLog("FAST INJECT concluído e validado.");
            }
            else WriteLog("ISO de Build já contém o último build: injeção ignorada.");

            var builtSnapshot = await Task.Run(() => ChangeDetectionService.Capture(contentDir));
            ChangeDetectionService.Save(GetChangeStatePath(project.ActiveDatName), builtSnapshot);
            project.LastBuildUtc = DateTime.UtcNow;
            var builtState = GetDatState(project.ActiveDatName!, true)!;
            builtState.BuildDatPath = project.ActiveBuildDatPath; builtState.LastBuildUtc = project.LastBuildUtc; builtState.AfsPath = project.ActiveAfsPath;
            SaveProject();
            UpdateBuildUi();
            await RefreshChangeStatusAsync();
            WriteLog("Abrindo ISO de Build no PCSX2...");
            LaunchPcsx2WithIso(buildIso);
            WriteLog("=== BUILD & TEST concluído ===");
        }
        catch (Exception ex)
        {
            WriteLog("ERRO NO BUILD & TEST: " + ex.Message);
            MessageBox.Show(ex.Message, "Build & Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnBuildOneClick.Enabled = true;
            await RefreshChangeStatusAsync();
            await RefreshTrackedDatsAsync();
        }
    }
}
