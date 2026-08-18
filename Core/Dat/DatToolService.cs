using System.Diagnostics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Dat;

public static class DatToolService
{
    public static async Task<(int ExitCode, string Output)> ExtractAsync(string exePath, string datPath, string outputDirectory)
    {
        if (!File.Exists(exePath)) throw new FileNotFoundException("RE4_UHD_DAT_Tool.exe não encontrado.", exePath);
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        Directory.CreateDirectory(outputDirectory);
        string localDat = Path.Combine(outputDirectory, Path.GetFileName(datPath));
        File.Copy(datPath, localDat, true);
        return await RunAsync(exePath, $"-x \"{localDat}\"", outputDirectory);
    }

    public static async Task<DatRepackResult> RepackAsync(string exePath, string contentDirectory, string datName, string stagingDirectory, string outputDatPath)
    {
        if (!File.Exists(exePath)) throw new FileNotFoundException("RE4_UHD_DAT_Tool.exe não encontrado.", exePath);
        if (!Directory.Exists(contentDirectory)) throw new DirectoryNotFoundException("A pasta Content do cenário não foi encontrada: " + contentDirectory);
        if (string.IsNullOrWhiteSpace(datName)) throw new ArgumentException("Nome do DAT inválido.", nameof(datName));

        if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, true);
        CopyDirectory(contentDirectory, stagingDirectory);

        // A RE4_UHD_DAT_Tool usa apenas "-p" e procura no diretório de trabalho
        // os arquivos auxiliares gerados pelo -x (por exemplo, r100 + IDX).
        // Portanto, o CWD correto é a pasta PAI que contém a estrutura extraída.
        string repackWorkingDirectory = stagingDirectory;

        string originalDat = Path.Combine(stagingDirectory, datName);
        long oldSize = File.Exists(originalDat) ? new FileInfo(originalDat).Length : 0;
        DateTime startedAt = DateTime.UtcNow;

        var run = await RunAsync(exePath, "-p", repackWorkingDirectory);
        if (run.ExitCode != 0) return new DatRepackResult(run.ExitCode, run.Output, null, oldSize, 0, stagingDirectory);

        string? rebuiltDat = FindRebuiltDat(stagingDirectory, repackWorkingDirectory, datName, startedAt, originalDat);
        if (rebuiltDat == null)
            throw new InvalidOperationException($"A DAT Tool terminou sem gerar '{datName}'. Pasta usada no repack: {repackWorkingDirectory}");

        var rebuilt = new FileInfo(rebuiltDat);
        Directory.CreateDirectory(Path.GetDirectoryName(outputDatPath)!);
        File.Copy(rebuiltDat, outputDatPath, true);
        return new DatRepackResult(run.ExitCode, run.Output, outputDatPath, oldSize, rebuilt.Length, stagingDirectory);
    }

    private static string? FindRebuiltDat(string stagingDirectory, string workingDirectory, string datName, DateTime startedAt, string originalDat)
    {
        var candidates = new List<string>();
        void AddIfExists(string path) { if (File.Exists(path) && !candidates.Contains(path, StringComparer.OrdinalIgnoreCase)) candidates.Add(path); }

        AddIfExists(Path.Combine(workingDirectory, datName));
        AddIfExists(Path.Combine(stagingDirectory, datName));
        string parent = Directory.GetParent(stagingDirectory)?.FullName ?? stagingDirectory;
        AddIfExists(Path.Combine(parent, datName));

        foreach (string file in Directory.EnumerateFiles(stagingDirectory, "*.dat", SearchOption.AllDirectories)) AddIfExists(file);

        string originalFull = Path.GetFullPath(originalDat);
        return candidates
            .Select(path => new FileInfo(path))
            .Where(fi => !string.Equals(fi.FullName, originalFull, StringComparison.OrdinalIgnoreCase) || fi.LastWriteTimeUtc >= startedAt.AddSeconds(-2))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .FirstOrDefault()?.FullName;
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(string exePath, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo(exePath, arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Não foi possível iniciar a DAT Tool.");

        // Essa DAT Tool antiga termina com "Press any key...". Como o Workspace
        // executa a ferramenta sem janela de console, enviamos ENTER antecipadamente;
        // ele fica no buffer de stdin e libera o pause quando a ferramenta terminar.
        try { await p.StandardInput.WriteLineAsync(); await p.StandardInput.FlushAsync(); } catch { }

        string stdout = await p.StandardOutput.ReadToEndAsync();
        string stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        string output = string.IsNullOrWhiteSpace(stderr) ? stdout : stdout + Environment.NewLine + stderr;
        return (p.ExitCode, output);
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (string file in Directory.GetFiles(sourceDirectory)) File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), true);
        foreach (string dir in Directory.GetDirectories(sourceDirectory)) CopyDirectory(dir, Path.Combine(destinationDirectory, Path.GetFileName(dir)));
    }
}

public sealed record DatRepackResult(int ExitCode, string Output, string? OutputDatPath, long OldSize, long NewSize, string StagingDirectory);
