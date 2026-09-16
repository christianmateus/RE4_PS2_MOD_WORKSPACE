namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private string GetAfsFileBuildStatePath(string key)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(key.ToUpperInvariant());
        string id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        return Path.Combine(project.RootPath ?? AppContext.BaseDirectory, ".workspace", "state", "afs-" + id + ".txt");
    }

    private List<TrackedAfsFileStatus> GetTrackedAfsFileStatuses()
    {
        var result = new List<TrackedAfsFileStatus>();
        if (string.IsNullOrWhiteSpace(project.RootPath)) return result;
        string extractedRoot = Path.Combine(project.RootPath, "Extracted", "_AFS");
        if (!Directory.Exists(extractedRoot)) return result;
        bool buildExists = File.Exists(Path.Combine(project.RootPath, "Build", "RE4_PS2_MOD.iso"));
        foreach (string afsDirectory in Directory.EnumerateDirectories(extractedRoot).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            string afsStem = Path.GetFileName(afsDirectory);
            foreach (string sourceFile in Directory.EnumerateFiles(afsDirectory, "*", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(sourceFile);
                if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                string key = $"afs:{afsStem}:{fileName}";
                string currentHash = ChangeDetectionService.HashFile(sourceFile);
                string expected = $"{project.BuildIsoGeneration}|{currentHash}";
                string statePath = GetAfsFileBuildStatePath(key);
                bool needsInject = !buildExists || !File.Exists(statePath) || !string.Equals(File.ReadAllText(statePath), expected, StringComparison.Ordinal);
                result.Add(new TrackedAfsFileStatus(key, afsStem, sourceFile, fileName, needsInject));
            }
        }
        return result;
    }

    /// <summary>
    /// Reinjeta arquivos soltos extraídos por "Exibir todos". A extensão não importa:
    /// o nome precisa apenas corresponder a uma entrada válida do AFS de origem.
    /// </summary>
    private async Task<int> InjectExtractedAfsFilesIntoBuildIsoAsync(string buildIso, ISet<string>? selectedKeys = null)
    {
        if (string.IsNullOrWhiteSpace(project.RootPath)) return 0;
        selectedKeys ??= GetTrackedAfsFileStatuses().Where(x => x.NeedsInject).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string extractedRoot = Path.Combine(project.RootPath, "Extracted", "_AFS");
        if (!Directory.Exists(extractedRoot)) return 0;

        int injected = 0;
        foreach (string afsDirectory in Directory.EnumerateDirectories(extractedRoot).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            string afsStem = Path.GetFileName(afsDirectory);
            foreach (string sourceFile in Directory.EnumerateFiles(afsDirectory, "*", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(sourceFile);
                // Backups criados por editores ficam ao lado do arquivo e não são entradas AFS.
                if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                string key = $"afs:{afsStem}:{fileName}";
                if (!selectedKeys.Contains(key)) continue;

                // Reabrimos a ISO a cada arquivo: uma expansão anterior pode alterar LBAs.
                IReadOnlyList<IsoFileEntry> afsFiles = await Task.Run(() => AfsService.FindAfsFiles(buildIso));
                IsoFileEntry? afsFile = afsFiles.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Name).Equals(afsStem, StringComparison.OrdinalIgnoreCase));
                if (afsFile == null)
                {
                    WriteLog($"AFS FILES: pasta {afsStem} ignorada; AFS correspondente não existe na ISO de Build.");
                    break;
                }

                AfsImage afs = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, afsFile));
                AfsEntry? entry = AfsService.FindFirstValidEntryByName(afs, fileName);
                if (entry == null)
                {
                    WriteLog($"AFS FILES: {fileName} ignorado; não existe em {afsFile.Name}.");
                    continue;
                }

                long size = new FileInfo(sourceFile).Length;
                WriteLog($"AFS FILE INJECT: {fileName} ({FormatBytes(size)}) em {afsFile.Name}...");
                if (size > entry.AllocatedSize) WriteLog($"AFS: {fileName} excede o Reserved em {FormatBytes(size - entry.AllocatedSize)}; realocando automaticamente.");
                await Task.Run(() => AfsService.InjectEntryAuto(afs, entry, sourceFile));

                IReadOnlyList<IsoFileEntry> verifyAfsFiles = await Task.Run(() => AfsService.FindAfsFiles(buildIso));
                IsoFileEntry verifyAfsFile = verifyAfsFiles.First(x => x.FullPath.Equals(afsFile.FullPath, StringComparison.OrdinalIgnoreCase));
                AfsImage verify = await Task.Run(() => AfsService.OpenAfsFromIso(buildIso, verifyAfsFile));
                AfsEntry verified = verify.Entries.First(x => x.Index == entry.Index);
                if (verified.CurrentSize != size) throw new InvalidDataException($"{fileName}: validação após injeção falhou; esperado {size:N0}, encontrado {verified.CurrentSize:N0} bytes.");
                injected++;
                string statePath = GetAfsFileBuildStatePath(key);
                Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
                File.WriteAllText(statePath, $"{project.BuildIsoGeneration}|{ChangeDetectionService.HashFile(sourceFile)}");
                WriteLog($"AFS FILE INJECT concluído e validado: {fileName}.");
            }
        }
        if (injected > 0) WriteLog($"Arquivos soltos reinjetados no AFS: {injected:N0}.");
        return injected;
    }
}
