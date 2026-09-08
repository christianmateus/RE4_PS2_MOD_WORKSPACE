namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private string GetCharacterSyncStatePath(string datName) =>
        Path.Combine(project.RootPath ?? AppContext.BaseDirectory, ".workspace", "state", Path.GetFileNameWithoutExtension(datName) + ".character-dat.sha256");

    /// <summary>
    /// Character editing works on the complete DAT, while the regular build
    /// repacks the extracted Content directory. Before repacking, expand a DAT
    /// changed by the character viewer back into Content so the build includes it.
    /// </summary>
    private async Task SyncCharacterDatToContentAsync(DatProjectState state)
    {
        string? contentDir = state.ContentPath;
        if (string.IsNullOrWhiteSpace(contentDir) || !Directory.Exists(contentDir)) return;

        // Older versions of the viewer preferred Content/<name>.dat; newer ones
        // prefer OriginalDAT. Accept either location so existing edits are not lost.
        string? datPath = new[]
        {
            !string.IsNullOrWhiteSpace(project.RootPath) ? Path.Combine(project.RootPath, "Extracted", "Enemies", state.DatName) : null,
            state.OriginalDatPath,
            Path.Combine(contentDir, state.DatName)
        }
        .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path) && File.Exists(path + ".bak"))
        .OrderByDescending(path => File.GetLastWriteTimeUtc(path!))
        .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(datPath)) return;

        string backup = datPath + ".bak";

        string currentHash = await Task.Run(() => ChangeDetectionService.HashFile(datPath));
        string markerPath = GetCharacterSyncStatePath(state.DatName);
        string? synchronizedHash = File.Exists(markerPath) ? File.ReadAllText(markerPath).Trim() : null;
        if (!string.IsNullOrWhiteSpace(synchronizedHash))
        {
            if (string.Equals(currentHash, synchronizedHash, StringComparison.OrdinalIgnoreCase)) return;
        }
        else
        {
            string originalHash = await Task.Run(() => ChangeDetectionService.HashFile(backup));
            if (string.Equals(currentHash, originalHash, StringComparison.OrdinalIgnoreCase)) return;
        }

        var contentState = await GetChangeStateAsync(state.DatName, contentDir);
        bool IsViewerArtifact(string relativePath)
        {
            string fileName = Path.GetFileName(relativePath.Replace('/', Path.DirectorySeparatorChar));
            return fileName.Equals(state.DatName, StringComparison.OrdinalIgnoreCase) ||
                   fileName.Equals(state.DatName + ".bak", StringComparison.OrdinalIgnoreCase) ||
                   fileName.Equals(state.DatName + ".tmp", StringComparison.OrdinalIgnoreCase) ||
                   fileName.Equals(state.DatName + ".texture-map.json", StringComparison.OrdinalIgnoreCase);
        }
        string[] independentChanges = contentState.Diff.Changed
            .Concat(contentState.Diff.Added)
            .Concat(contentState.Diff.Removed)
            .Where(path => !IsViewerArtifact(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (independentChanges.Length > 0 || contentState.PendingTpl > 0)
            throw new InvalidOperationException(
                $"{state.DatName} foi alterado no módulo de Personagens, mas a pasta Content também possui mudanças independentes. " +
                "Faça primeiro um build dessas mudanças ou extraia novamente o DAT antes de continuar; a sincronização automática foi cancelada para não sobrescrever arquivos." +
                (independentChanges.Length > 0 ? $"\n\nArquivos detectados: {string.Join(", ", independentChanges.Take(5))}" : ""));

        WriteLog($"{state.DatName}: sincronizando edições do módulo de Personagens com Content...");
        string contentDatPath = Path.Combine(contentDir, state.DatName);
        string extractionSource = datPath;
        string? temporaryDirectory = null;
        try
        {
            // The native extractor copies the source DAT into Content. If the
            // viewer edited Content/<name>.dat directly, copying onto itself is
            // reported by Windows as a sharing violation. Stage it elsewhere.
            if (string.Equals(Path.GetFullPath(datPath), Path.GetFullPath(contentDatPath), StringComparison.OrdinalIgnoreCase))
            {
                temporaryDirectory = Path.Combine(Path.GetTempPath(), "re4_character_sync_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temporaryDirectory);
                extractionSource = Path.Combine(temporaryDirectory, state.DatName);
                File.Copy(datPath, extractionSource, true);
            }

            var extraction = await NativeDatService.ExtractAsync(extractionSource, contentDir);
            WriteLog($"{state.DatName}: parser nativo extraiu {extraction.EntryCount:N0} entrada(s).");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryDirectory))
                try { Directory.Delete(temporaryDirectory, true); } catch { }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        File.WriteAllText(markerPath, currentHash);
        WriteLog($"{state.DatName}: Content atualizado com as texturas e materiais do DAT editado.");
    }

    private async Task SyncActiveCharacterDatToContentAsync()
    {
        if (string.IsNullOrWhiteSpace(project.ActiveDatName)) return;
        DatProjectState? state = GetDatState(project.ActiveDatName, false);
        if (state != null) await SyncCharacterDatToContentAsync(state);
    }
}
