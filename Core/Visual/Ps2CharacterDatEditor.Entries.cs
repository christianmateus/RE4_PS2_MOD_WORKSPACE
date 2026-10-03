using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static partial class Ps2CharacterDatEditor
{
    public static byte[] CaptureTplEntry(string datPath, int tplEntryIndex)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        return dat.AsSpan(start, length).ToArray();
    }

    public static byte[] CaptureDatEntry(string datPath, int entryIndex)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, _) = GetEntry(dat, entryIndex);
        return dat.AsSpan(start, length).ToArray();
    }

    public static void RestoreDatEntry(string datPath, int entryIndex, byte[] entryData)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, _) = GetEntry(dat, entryIndex);
        CommitTpl(datPath, dat, start, length, entryData);
    }

    public static void ReplaceBinEntry(string targetDatPath, int targetEntryIndex, string sourceDatPath, int sourceEntryIndex, SkeletonAdaptMode mode = SkeletonAdaptMode.Automatic)
    {
        byte[] target = File.ReadAllBytes(targetDatPath);
        (int targetStart, int targetLength, string targetTag) = GetEntry(target, targetEntryIndex);
        if (!targetTag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada de destino não é um BIN.");
        byte[] source = File.ReadAllBytes(sourceDatPath);
        (int sourceStart, int sourceLength, string sourceTag) = GetEntry(source, sourceEntryIndex);
        if (!sourceTag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada de origem não é um BIN.");
        byte[] targetBin = target.AsSpan(targetStart, targetLength).ToArray();
        // Reapplying a donor over an already swapped slot must still adapt to the
        // receiver's original rig, not to the previously adapted donor subset.
        string targetBackup = targetDatPath + ".bak";
        if (File.Exists(targetBackup))
        {
            try { targetBin = CaptureDatEntry(targetBackup, targetEntryIndex); } catch { }
        }
        byte[] replacement = source.AsSpan(sourceStart, sourceLength).ToArray();
        if (mode != SkeletonAdaptMode.KeepDonor) AdaptMeshToTargetSkeleton(targetBin, replacement, mode);
        CommitTpl(targetDatPath, target, targetStart, targetLength, replacement);
    }

    public static void ReplaceBinEntryFromFile(string targetDatPath, int targetEntryIndex, string sourceBinPath, SkeletonAdaptMode mode = SkeletonAdaptMode.Automatic)
    {
        byte[] target = File.ReadAllBytes(targetDatPath);
        (int targetStart, int targetLength, string targetTag) = GetEntry(target, targetEntryIndex);
        if (!targetTag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada de destino não é um BIN.");
        byte[] replacement = File.ReadAllBytes(sourceBinPath);
        if (replacement.Length < 0x10 || BitConverter.ToUInt16(replacement, 0) != 0x30) throw new InvalidDataException("O conversor não produziu um BIN PS2 válido.");
        byte[] targetBin = target.AsSpan(targetStart, targetLength).ToArray();
        string targetBackup = targetDatPath + ".bak";
        if (File.Exists(targetBackup)) try { targetBin = CaptureDatEntry(targetBackup, targetEntryIndex); } catch { }
        if (mode != SkeletonAdaptMode.KeepDonor) AdaptMeshToTargetSkeleton(targetBin, replacement, mode);
        CommitTpl(targetDatPath, target, targetStart, targetLength, replacement);
    }

}
