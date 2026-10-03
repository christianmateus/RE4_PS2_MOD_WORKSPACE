using RE4_PS2_MOD_WORKSPACE.Core.Dat;
using System.Text.Json;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>Adapts existing DAT codecs to an IDX workspace; only extracted payloads are saved.</summary>
public sealed class ExtractedCharacterWorkspace : IDisposable
{
    private sealed class Session
    {
        public required string Idx;
        public required string Dat;
        public required IReadOnlyList<(int Index, string Path, string Type)> Entries;
        public byte[][] Baseline = [];
    }
    private readonly string temporary = Path.Combine(Path.GetTempPath(), "re4_character_workspace_" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, Session> sessions = new(StringComparer.OrdinalIgnoreCase);

    public string Source(string path) => sessions.TryGetValue(path, out var s) ? s.Idx : path;
    public string MetadataPath(string path) => sessions.TryGetValue(path, out var s)
        ? Path.ChangeExtension(s.Idx, ".dat") : path;

    public IReadOnlyList<RE4_PS2_MOD_WORKSPACE.Core.Animation.FcvCatalogEntry>? Animations(string path)
        => sessions.TryGetValue(path, out var s)
            ? s.Entries.Where(e => e.Type.Equals("FCV", StringComparison.OrdinalIgnoreCase))
                .Select(e => new RE4_PS2_MOD_WORKSPACE.Core.Animation.FcvCatalogEntry(e.Path, null)).ToArray()
            : null;

    public string Open(string path)
    {
        if (sessions.TryGetValue(path, out var existing)) { Refresh(existing); return path; }
        if (!Path.GetExtension(path).Equals(".idx", StringComparison.OrdinalIgnoreCase)) return path;
        string idx = Path.GetFullPath(path);
        MigrateMetadata(idx);
        var session = sessions.Values.FirstOrDefault(s => s.Idx.Equals(idx, StringComparison.OrdinalIgnoreCase));
        if (session == null)
        {
            string directory = Path.Combine(temporary, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            session = new Session { Idx = idx, Dat = Path.Combine(directory, Path.GetFileNameWithoutExtension(idx) + ".dat"), Entries = NativeDatService.ListExtractedEntries(idx) };
            Refresh(session);
            string original = Path.Combine(Directory.GetParent(Path.GetDirectoryName(idx)!)!.FullName, "OriginalDAT", Path.GetFileName(session.Dat));
            File.Copy(File.Exists(original) ? original : session.Dat, session.Dat + ".bak");
            sessions.Add(session.Dat, session);
        }
        else Refresh(session);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(idx)!, ".character-extracted.json"), "{\"version\":1}");
        return session.Dat;
    }

    private static void MigrateMetadata(string idx)
    {
        string content = Path.GetDirectoryName(idx)!;
        string datName = Path.GetFileNameWithoutExtension(idx) + ".dat";
        // Keep the existing sidecar filename and BIN-index keys. Only its folder
        // changes when a legacy DAT is replaced by an extracted IDX workspace.
        DirectoryInfo? extracted = new DirectoryInfo(content);
        while (extracted != null && !extracted.Name.Equals("Extracted", StringComparison.OrdinalIgnoreCase)) extracted = extracted.Parent;
        var candidates = new List<string>();
        if (extracted != null) candidates.Add(Path.Combine(extracted.FullName, "Enemies", datName));
        if (Directory.GetParent(content) is { } package)
        {
            candidates.Add(Path.Combine(package.FullName, "OriginalDAT", datName));
            candidates.Add(Path.Combine(package.FullName, datName));
        }
        foreach (string suffix in new[] { ".character-view.json", ".texture-map.json" })
        {
            string destination = Path.Combine(content, datName + suffix);
            if (File.Exists(destination)) continue;
            string? source = candidates.Select(p => p + suffix).FirstOrDefault(File.Exists);
            if (source == null) continue;
            using var json = JsonDocument.Parse(File.ReadAllText(source));
            File.Copy(source, destination, overwrite: false);
        }
    }

    private static void Refresh(Session session)
    {
        session.Entries = NativeDatService.ListExtractedEntries(session.Idx);
        string directory = Path.GetDirectoryName(session.Dat)!;
        NativeDatService.Repack(Path.GetDirectoryName(session.Idx)!, Path.GetFileName(session.Dat), Path.Combine(directory, "stage"), session.Dat);
        session.Baseline = NativeDatService.Read(session.Dat).Entries.Select(e => e.Data).ToArray();
    }

    private static bool MatchesPayload(byte[] extracted, byte[] packed)
    {
        if (extracted.Length > packed.Length || !extracted.AsSpan().SequenceEqual(packed.AsSpan(0, extracted.Length))) return false;
        return packed.AsSpan(extracted.Length).IndexOfAnyExcept((byte)0) < 0;
    }

    public void SaveChanges(string dat)
    {
        if (!sessions.TryGetValue(dat, out var session)) return;
        var archive = NativeDatService.Read(dat);
        if (archive.Entries.Count != session.Baseline.Length) throw new InvalidDataException("A edição alterou a quantidade de entradas do personagem.");
        var changed = archive.Entries.Where(e => !e.Data.SequenceEqual(session.Baseline[e.Index])).ToArray();
        // Preflight every affected file before writing any payload; independent
        // texture edits in other entries are left untouched.
        foreach (var entry in changed)
        {
            string target = session.Entries[entry.Index].Path;
            if (!File.Exists(target) || !MatchesPayload(File.ReadAllBytes(target), session.Baseline[entry.Index]))
                throw new IOException($"O arquivo extraído mudou durante a edição: {target}. Atualize o personagem antes de tentar novamente.");
        }
        var written = new List<(string Path, byte[] Before)>();
        try
        {
            foreach (var entry in changed)
            {
                string target = session.Entries[entry.Index].Path;
                byte[] before = File.ReadAllBytes(target);
                string backupDir = Path.Combine(Directory.GetParent(Path.GetDirectoryName(session.Idx)!)!.FullName, "CharacterBackups");
                Directory.CreateDirectory(backupDir);
                string backup = Path.Combine(backupDir, Path.GetFileName(target) + ".bak");
                if (!File.Exists(backup)) File.WriteAllBytes(backup, before);
                written.Add((target, before));
                WritePayload(target, entry.Data);
            }
            // The build must use Content directly, never the legacy DAT viewer copy.
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(session.Idx)!, ".character-extracted.json"), "{\"version\":1}");
            session.Baseline = archive.Entries.Select(e => e.Data).ToArray();
        }
        catch
        {
            foreach (var item in written.AsEnumerable().Reverse()) WritePayload(item.Path, item.Before);
            throw;
        }
    }

    private static void WritePayload(string path, byte[] data)
    {
        string stage = path + ".character.tmp";
        try { File.WriteAllBytes(stage, data); File.Move(stage, path, true); }
        finally { if (File.Exists(stage)) File.Delete(stage); }
    }

    public void Dispose() { try { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); } catch { } }
}
