namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void btnBuildRepackDat_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        if (string.IsNullOrWhiteSpace(settings.DatToolPath) || !File.Exists(settings.DatToolPath))
        {
            MessageBox.Show("Configure o RE4_UHD_DAT_Tool.exe na tela Tools primeiro.", "DAT Tool", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNavTools_Click(null, EventArgs.Empty);
            return;
        }
        if (string.IsNullOrWhiteSpace(project.ActiveDatName))
        {
            MessageBox.Show("Nenhum DAT ativo. Extraia um cenário primeiro.", "Repack DAT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string? contentDir = GetActiveContentPath();
        if (string.IsNullOrWhiteSpace(contentDir) || !Directory.Exists(contentDir))
        {
            MessageBox.Show("A pasta Content do cenário ativo não foi encontrada. Extraia o cenário novamente.", "Repack DAT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string scenario = Path.GetFileNameWithoutExtension(project.ActiveDatName);
        string stagingDir = Path.Combine(project.RootPath!, "Temp", "Repack", scenario);
        string buildDir = Path.Combine(project.RootPath!, "Build", scenario);
        string outputDat = Path.Combine(buildDir, project.ActiveDatName);

        try
        {
            btnBuildRepackDat.Enabled = false;
            WriteLog($"Preparando repack de {project.ActiveDatName}...");
            WriteLog("Copiando Content para a área temporária de repack...");
            WriteLog("Executando RE4_UHD_DAT_Tool.exe -p...");
            var result = await DatToolService.RepackAsync(settings.DatToolPath!, contentDir, project.ActiveDatName, stagingDir, outputDat);
            if (!string.IsNullOrWhiteSpace(result.Output)) foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) WriteLog("DAT Tool: " + line);
            if (result.ExitCode != 0) throw new InvalidOperationException($"A DAT Tool terminou com código {result.ExitCode}.");

            project.ActiveBuildDatPath = result.OutputDatPath;
            var activeState = GetDatState(project.ActiveDatName!, true)!; activeState.BuildDatPath = result.OutputDatPath;
            SaveProject();
            WriteLog($"DAT reconstruído: {result.OutputDatPath}");
            WriteLog($"Tamanho: {FormatBytes(result.OldSize)} -> {FormatBytes(result.NewSize)}");
            UpdateBuildUi(result.NewSize);
            MessageBox.Show($"DAT reconstruído com sucesso.\n\n{result.OutputDatPath}", "Repack DAT", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            WriteLog("ERRO NO REPACK: " + ex.Message);
            lblBuildDatStatus.Text = "Falha no repack: " + ex.Message;
            lblBuildDatStatus.ForeColor = Color.FromArgb(220, 105, 105);
            MessageBox.Show(ex.Message, "Erro no Repack DAT", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { btnBuildRepackDat.Enabled = true; await RefreshTrackedDatsAsync(); }
    }

    private async void btnBuildInjectIso_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        if (string.IsNullOrWhiteSpace(project.IsoPath) || !File.Exists(project.IsoPath))
        {
            MessageBox.Show("Selecione e leia uma ISO válida primeiro.", "Injetar DAT na ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNavWorkspace_Click(null, EventArgs.Empty);
            return;
        }
        if (string.IsNullOrWhiteSpace(project.ActiveBuildDatPath) || !File.Exists(project.ActiveBuildDatPath))
        {
            MessageBox.Show("Reconstrua o DAT primeiro usando o passo 1.", "Injetar DAT na ISO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(project.ActiveDatName)) return;

        string afsPath = project.ActiveAfsPath ?? loadedAfs?.IsoAfsEntry.FullPath ?? "DATA/BIO4DAT.AFS";
        long buildSize = new FileInfo(project.ActiveBuildDatPath).Length;
        AfsEntry? sourceEntry = loadedAfs == null ? null : AfsService.FindFirstValidEntryByName(loadedAfs, project.ActiveDatName);
        if (sourceEntry != null && buildSize > sourceEntry.AllocatedSize)
        {
            long over = buildSize - sourceEntry.AllocatedSize;
            MessageBox.Show($"O DAT reconstruído excede o Reserved Space em {FormatBytes(over)}.\n\nO Workspace bloqueia esta operação para não arriscar corromper a ISO. A realocação automática será portada da ISOAFS em uma versão posterior.", "Reserved Space excedido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string buildIso = Path.Combine(project.RootPath!, "Build", "RE4_PS2_MOD.iso");
        bool alreadyExists = File.Exists(buildIso);
        if (alreadyExists && !string.IsNullOrWhiteSpace(project.BuildIsoSourcePath) && !string.Equals(Path.GetFullPath(project.BuildIsoSourcePath), Path.GetFullPath(project.IsoPath), StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("A ISO base deste workspace mudou desde que a ISO de Build foi criada. Use RECRIAR ISO LIMPA antes do Fast Inject para evitar misturar duas ISOs diferentes.", "ISO de Build desatualizada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string confirmation = alreadyExists
            ? $"O Workspace vai injetar {project.ActiveDatName} na ISO de Build existente:\n\n{buildIso}\n\nAFS: {afsPath}\nDAT: {project.ActiveBuildDatPath}\n\nContinuar?"
            : $"O Workspace vai criar uma cópia da ISO original e injetar {project.ActiveDatName}:\n\nOrigem: {project.IsoPath}\nDestino: {buildIso}\n\nAFS: {afsPath}\n\nA ISO original NÃO será modificada. Continuar?";
        if (MessageBox.Show(confirmation, "2  FAST INJECT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try
        {
            btnBuildInjectIso.Enabled = false;
            if (!alreadyExists)
            {
                WriteLog("Criando ISO de Build a partir da ISO original...");
                Directory.CreateDirectory(Path.GetDirectoryName(buildIso)!);
                await Task.Run(() => File.Copy(project.IsoPath!, buildIso, overwrite: false));
                if (project.BuildIsoGeneration <= 0) project.BuildIsoGeneration = 1;
                WriteLog("ISO de Build criada: " + buildIso);
            }
            else WriteLog("FAST BUILD: reutilizando ISO de Build existente (sem copiar a ISO original): " + buildIso);

            WriteLog($"Abrindo AFS na ISO de Build: {afsPath}");
            var buildAfsFiles = await Task.Run(() => AfsService.FindAfsFiles(buildIso));
            var buildAfsFile = buildAfsFiles.FirstOrDefault(x => x.FullPath.Equals(afsPath, StringComparison.OrdinalIgnoreCase))
                ?? buildAfsFiles.FirstOrDefault(x => x.Name.Equals("BIO4DAT.AFS", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"AFS não encontrado na ISO de Build: {afsPath}");
            var buildAfs = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, buildAfsFile));
            var buildEntry = AfsService.FindFirstValidEntryByName(buildAfs, project.ActiveDatName)
                ?? throw new InvalidDataException($"DAT não encontrado no AFS de Build: {project.ActiveDatName}");

            WriteLog($"Reserved: {FormatBytes(buildEntry.AllocatedSize)} | Novo DAT: {FormatBytes(buildSize)}");
            WriteLog($"Injetando {project.ActiveDatName} no slot original...");
            await Task.Run(() => AfsService.InjectEntryInPlace(buildAfs, buildEntry, project.ActiveBuildDatPath!));

            // Reabre para validar o Current Size gravado na TOC.
            var verify = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, buildAfsFile));
            var verifiedEntry = verify.Entries.First(x => x.Index == buildEntry.Index);
            if (verifiedEntry.CurrentSize != buildSize) throw new InvalidDataException($"Validação falhou: Current Size esperado {buildSize:N0}, encontrado {verifiedEntry.CurrentSize:N0}.");

            project.ActiveBuildIsoPath = buildIso; project.BuildIsoSourcePath = project.IsoPath;
            string? activeContent = GetActiveContentPath();
            if (!string.IsNullOrWhiteSpace(activeContent) && Directory.Exists(activeContent))
            {
                ChangeDetectionService.Save(GetChangeStatePath(), ChangeDetectionService.Capture(activeContent));
                project.LastBuildUtc = DateTime.UtcNow;
            }
            var injectedState = GetDatState(project.ActiveDatName!, true)!;
            injectedState.BuildDatPath = project.ActiveBuildDatPath; injectedState.LastBuildUtc = project.LastBuildUtc; injectedState.InjectedGeneration = project.BuildIsoGeneration;
            SaveProject();
            WriteLog($"DAT injetado com sucesso no {buildAfsFile.Name}.");
            WriteLog($"Validação OK: Current Size = {FormatBytes(verifiedEntry.CurrentSize)}.");
            WriteLog("ISO pronta para teste: " + buildIso);
            UpdateBuildUi();
            MessageBox.Show($"DAT injetado com sucesso.\n\nISO pronta para teste:\n{buildIso}\n\nSua ISO original não foi alterada.", "ISO de Build pronta", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            WriteLog("ERRO NA INJEÇÃO: " + ex.Message);
            lblBuildIsoStatus.Text = "Falha na injeção: " + ex.Message;
            lblBuildIsoStatus.ForeColor = Color.FromArgb(220, 105, 105);
            MessageBox.Show(ex.Message, "Erro ao injetar DAT na ISO", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { UpdateBuildUi(); await RefreshTrackedDatsAsync(); }
    }

    private async void btnBuildRecreateIso_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        if (string.IsNullOrWhiteSpace(project.IsoPath) || !File.Exists(project.IsoPath))
        {
            MessageBox.Show("Selecione uma ISO base válida primeiro.", "Recriar ISO limpa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNavWorkspace_Click(null, EventArgs.Empty);
            return;
        }

        string buildIso = Path.Combine(project.RootPath!, "Build", "RE4_PS2_MOD.iso");
        string warning = File.Exists(buildIso)
            ? $"Isso vai APAGAR a ISO de Build atual e copiá-la novamente da ISO original.\n\nBuild atual: {buildIso}\nOrigem: {project.IsoPath}\n\nTodas as injeções feitas somente na ISO de Build serão descartadas. Continuar?"
            : $"Criar uma ISO de Build limpa a partir da ISO original?\n\nOrigem: {project.IsoPath}\nDestino: {buildIso}\n\nEsta cópia pode levar algum tempo.";
        if (MessageBox.Show(warning, "RECRIAR ISO LIMPA", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            btnBuildRecreateIso.Enabled = false;
            btnBuildInjectIso.Enabled = false;
            WriteLog("Recriando ISO de Build limpa a partir da ISO original...");
            Directory.CreateDirectory(Path.GetDirectoryName(buildIso)!);
            await Task.Run(() =>
            {
                string tempIso = buildIso + ".new";
                if (File.Exists(tempIso)) File.Delete(tempIso);
                File.Copy(project.IsoPath!, tempIso, overwrite: true);
                if (File.Exists(buildIso)) File.Delete(buildIso);
                File.Move(tempIso, buildIso);
            });
            project.ActiveBuildIsoPath = buildIso;
            project.BuildIsoSourcePath = project.IsoPath;
            project.BuildIsoGeneration = Math.Max(1, project.BuildIsoGeneration + 1);
            project.LastBuildUtc = null;
            SaveProject();
            WriteLog("ISO de Build limpa criada: " + buildIso);
            UpdateBuildUi();
            MessageBox.Show($"ISO de Build limpa criada com sucesso.\n\n{buildIso}\n\nAgora o FAST INJECT reutilizará esta cópia sem copiá-la novamente.", "ISO de Build pronta", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            WriteLog("ERRO AO RECRIAR ISO: " + ex.Message);
            MessageBox.Show(ex.Message, "Erro ao recriar ISO", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (btnBuildRecreateIso != null && !btnBuildRecreateIso.IsDisposed) btnBuildRecreateIso.Enabled = true;
            UpdateBuildUi();
            await RefreshTrackedDatsAsync();
        }
    }

    private void btnBuildOpenDat_Click(object? sender, EventArgs e) => Launch(settings.DatToolPath, "DAT Tool");

    private void btnBuildOpenIsoAfs_Click(object? sender, EventArgs e) => Launch(settings.IsoAfsPath, "ISOAFS");

    private void btnBuildOpenPcsx2_Click(object? sender, EventArgs e)
    {
        string? iso = !string.IsNullOrWhiteSpace(project.ActiveBuildIsoPath) && File.Exists(project.ActiveBuildIsoPath) ? project.ActiveBuildIsoPath : (string.IsNullOrWhiteSpace(project.RootPath) ? null : Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso"));
        if (!string.IsNullOrWhiteSpace(iso) && File.Exists(iso))
        {
            try { LaunchPcsx2WithIso(iso); WriteLog("PCSX2 aberto com a ISO de Build: " + iso); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "PCSX2", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        else Launch(settings.Pcsx2Path, "PCSX2");
    }

    private void btnBuildFolder_Click(object? sender, EventArgs e)
    {
        if (!RequireWorkspace()) return;
        OpenFolder(Path.Combine(project.RootPath!, "Build"));
    }
}
