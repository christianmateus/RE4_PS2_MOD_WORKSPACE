using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2AevWriter
{
    private const int HeaderSize = 0x10;
    private const float FileScale = 100f;

    public static bool Save(AevScene scene, string? firstBackupPath)
    {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (string.IsNullOrWhiteSpace(scene.SourcePath) || !File.Exists(scene.SourcePath))
            throw new FileNotFoundException("AEV original não encontrado.", scene.SourcePath);
        if (scene.EntrySize <= 0) throw new InvalidDataException("AEV sem layout gravável.");
        if (scene.Entries.Count > ushort.MaxValue) throw new InvalidDataException("Quantidade de entries AEV excede o limite do formato.");

        byte[] original = File.ReadAllBytes(scene.SourcePath);
        bool backupCreated = false;

        if (!string.IsNullOrWhiteSpace(firstBackupPath) && !File.Exists(firstBackupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(firstBackupPath))!);
            File.Copy(scene.SourcePath, firstBackupPath, false);
            backupCreated = true;
        }

        // Preserve anything after the original entry table. Most PS2 files end exactly
        // there, but this keeps unknown trailing data intact if encountered.
        int originalCount = original.Length >= 8 ? BitConverter.ToUInt16(original, 6) : 0;
        long originalTableEnd = HeaderSize + (long)originalCount * scene.EntrySize;
        byte[] trailing = originalTableEnd >= HeaderSize && originalTableEnd <= original.Length
            ? original.AsSpan((int)originalTableEnd).ToArray()
            : Array.Empty<byte>();

        int newLength = checked(HeaderSize + scene.Entries.Count * scene.EntrySize + trailing.Length);
        byte[] rebuilt = new byte[newLength];
        Buffer.BlockCopy(original, 0, rebuilt, 0, Math.Min(HeaderSize, original.Length));

        // Count occupies ushort @ +0x06 in the PS2 header. Fallback layouts also read
        // the low byte from this location, so keeping ushort correct covers both.
        byte[] countBytes = BitConverter.GetBytes((ushort)scene.Entries.Count);
        rebuilt[6] = countBytes[0];
        rebuilt[7] = countBytes[1];

        for (int i = 0; i < scene.Entries.Count; i++)
        {
            AevEntry entry = scene.Entries[i];
            entry.FileOrder = i;

            int offset = HeaderSize + i * scene.EntrySize;
            byte[] raw = entry.RawData.Length == scene.EntrySize
                ? (byte[])entry.RawData.Clone()
                : new byte[scene.EntrySize];

            PatchEditableFields(raw, 0, entry);
            PatchTypeAndIndex(raw, 0, entry);
            PatchDefinitionAndSpecialBytes(raw, 0, entry);
            PatchParameterBuffer(raw, 0, entry);
            Buffer.BlockCopy(raw, 0, rebuilt, offset, scene.EntrySize);

            if (entry.RawData.Length == scene.EntrySize)
                Buffer.BlockCopy(raw, 0, entry.RawData, 0, scene.EntrySize);
        }

        if (trailing.Length > 0)
            Buffer.BlockCopy(trailing, 0, rebuilt, HeaderSize + scene.Entries.Count * scene.EntrySize, trailing.Length);

        string temp = scene.SourcePath + ".workspace_save_tmp";
        try
        {
            File.WriteAllBytes(temp, rebuilt);
            File.Move(temp, scene.SourcePath, true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }

        return backupCreated;
    }

    public static bool HasEditableChanges(AevScene scene)
    {
        if (scene == null || scene.EntrySize <= 0) return false;

        if (File.Exists(scene.SourcePath))
        {
            byte[] file = File.ReadAllBytes(scene.SourcePath);
            if (file.Length >= 8 && BitConverter.ToUInt16(file, 6) != scene.Entries.Count) return true;
        }

        for (int i = 0; i < scene.Entries.Count; i++)
        {
            AevEntry entry = scene.Entries[i];
            if (entry.FileOrder != i) return true;
            if (entry.RawData.Length < scene.EntrySize) return true;

            byte[] expected = (byte[])entry.RawData.Clone();
            PatchEditableFields(expected, 0, entry);
            PatchTypeAndIndex(expected, 0, entry);
            PatchDefinitionAndSpecialBytes(expected, 0, entry);
            PatchParameterBuffer(expected, 0, entry);
            if (!expected.AsSpan().SequenceEqual(entry.RawData)) return true;
        }
        return false;
    }

    private static void PatchEditableFields(byte[] data, int offset, AevEntry entry)
    {
        WriteWorldFloat(data, offset + 0x08, entry.Y);
        WriteWorldFloat(data, offset + 0x0C, entry.Height);

        int positionsOffset;
        if (entry.HasExplicitRadius)
        {
            WriteWorldFloat(data, offset + 0x10, entry.CircleRadius);
            positionsOffset = 0x14;
        }
        else positionsOffset = 0x10;

        WriteWorldXZ(data, offset + positionsOffset + 0x00, entry.Position1);
        WriteWorldXZ(data, offset + positionsOffset + 0x08, entry.Position2);
        WriteWorldXZ(data, offset + positionsOffset + 0x10, entry.Position3);
        WriteWorldXZ(data, offset + positionsOffset + 0x18, entry.Position4);
    }

    private static void PatchTypeAndIndex(byte[] data, int offset, AevEntry entry)
    {
        int typeOffset = entry.HasExplicitRadius ? 0x35 : 0x31;
        int indexOffset = entry.HasExplicitRadius ? 0x36 : 0x32;
        data[offset + typeOffset] = entry.Type;
        data[offset + indexOffset] = checked((byte)entry.Index);
    }

    private static void PatchDefinitionAndSpecialBytes(byte[] data, int offset, AevEntry entry)
    {
        // Matches the old JS editor:
        // entry +0x44..0x47 = definition bytes 1..4
        // entry +0x48..0x4A = event-specific bytes used by Type 0x02.
        data[offset + 0x44] = entry.Priority;
        data[offset + 0x45] = entry.DefinitionByte2;
        data[offset + 0x46] = entry.DefinitionByte3;
        data[offset + 0x47] = entry.DefinitionByte4;
        data[offset + 0x48] = entry.HitAngle;
        data[offset + 0x49] = entry.OpenAngle;
        data[offset + 0x4A] = entry.ActionType;
    }

    private static void PatchParameterBuffer(byte[] data, int offset, AevEntry entry)
    {
        int parameterOffset = entry.HasExplicitRadius ? 0x5C : 0x58;
        if (offset + parameterOffset + entry.ParameterBuffer.Length > data.Length)
            throw new InvalidDataException("Entry AEV curta demais para gravar ParameterBuffer.");
        Buffer.BlockCopy(entry.ParameterBuffer, 0, data, offset + parameterOffset, entry.ParameterBuffer.Length);
    }

    private static void WriteWorldXZ(byte[] data, int offset, Vector2 value)
    {
        WriteWorldFloat(data, offset, value.X);
        WriteWorldFloat(data, offset + 4, value.Y);
    }

    private static void WriteWorldFloat(byte[] data, int offset, float value)
    {
        if (!float.IsFinite(value)) throw new InvalidDataException("Tentativa de gravar coordenada AEV não finita.");
        byte[] raw = BitConverter.GetBytes(value * FileScale);
        Buffer.BlockCopy(raw, 0, data, offset, 4);
    }
}
