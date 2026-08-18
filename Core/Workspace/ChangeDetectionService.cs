using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RE4_PS2_MOD_WORKSPACE.Core.Workspace;

public static class ChangeDetectionService
{
    public static ContentSnapshot Capture(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return new ContentSnapshot();
        var files = new Dictionary<string, FileFingerprint>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var fi = new FileInfo(path);
            string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            files[relative] = new FileFingerprint(fi.Length, HashFile(path));
        }
        return new ContentSnapshot { Files = files, CapturedUtc = DateTime.UtcNow };
    }

    public static SnapshotDiff Compare(ContentSnapshot? baseline, ContentSnapshot current)
    {
        baseline ??= new ContentSnapshot();
        var changed = new List<string>();
        var added = new List<string>();
        var removed = new List<string>();
        foreach (var pair in current.Files)
        {
            if (!baseline.Files.TryGetValue(pair.Key, out var old)) added.Add(pair.Key);
            else if (old.Size != pair.Value.Size || !string.Equals(old.Sha256, pair.Value.Sha256, StringComparison.OrdinalIgnoreCase)) changed.Add(pair.Key);
        }
        foreach (string key in baseline.Files.Keys)
            if (!current.Files.ContainsKey(key)) removed.Add(key);
        return new SnapshotDiff(changed, added, removed);
    }

    public static void Save(string path, ContentSnapshot snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static ContentSnapshot? Load(string path)
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<ContentSnapshot>(File.ReadAllText(path)) : null; }
        catch { return null; }
    }

    public static string HashFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static string HashStreamSegment(string path, long offset, long length)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = offset;
        using var sha = SHA256.Create();
        byte[] buffer = new byte[1024 * 1024];
        long remaining = length;
        while (remaining > 0)
        {
            int read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) throw new EndOfStreamException();
            sha.TransformBlock(buffer, 0, read, null, 0);
            remaining -= read;
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }
}

public sealed class ContentSnapshot
{
    public DateTime CapturedUtc { get; set; }
    public Dictionary<string, FileFingerprint> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record FileFingerprint(long Size, string Sha256);
public sealed record SnapshotDiff(IReadOnlyList<string> Changed, IReadOnlyList<string> Added, IReadOnlyList<string> Removed)
{
    public int Total => Changed.Count + Added.Count + Removed.Count;
    public bool HasChanges => Total > 0;
    public IEnumerable<string> All => Changed.Select(x => "~ " + x).Concat(Added.Select(x => "+ " + x)).Concat(Removed.Select(x => "- " + x));
}
