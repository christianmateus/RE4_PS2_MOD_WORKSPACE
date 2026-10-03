using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static partial class Ps2CharacterDatEditor
{
    private static void CommitTpl(string datPath, byte[] dat, int start, int length, byte[] replacement)
    {
        // Preserve the original entry alignment when an edit changes the payload
        // size (resize and 4/8-bit conversion both legitimately do this).
        int alignmentPad = (length - replacement.Length) & 0x0F;
        if (alignmentPad != 0) Array.Resize(ref replacement, checked(replacement.Length + alignmentPad));
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) File.Copy(datPath, backup, false);
        int delta = replacement.Length - length;
        byte[] output = new byte[checked(dat.Length + delta)];
        Buffer.BlockCopy(dat, 0, output, 0, start);
        Buffer.BlockCopy(replacement, 0, output, start, replacement.Length);
        Buffer.BlockCopy(dat, start + length, output, start + replacement.Length, dat.Length - start - length);
        if (delta != 0)
        {
            int count = checked((int)BitConverter.ToUInt32(output, 0));
            for (int i = 0; i < count; i++)
            {
                int offsetPosition = 0x10 + i * 4;
                int offset = checked((int)BitConverter.ToUInt32(output, offsetPosition));
                if (offset > start) Buffer.BlockCopy(BitConverter.GetBytes(checked(offset + delta)), 0, output, offsetPosition, 4);
            }
        }
        string staged = datPath + ".tmp";
        File.WriteAllBytes(staged, output);
        File.Move(staged, datPath, true);
    }

    private static (int Start, int Length, string Tag) GetEntry(byte[] dat, int entryIndex)
    {
        if (dat.Length < 0x20) throw new InvalidDataException("DAT muito pequeno.");
        int count = checked((int)BitConverter.ToUInt32(dat, 0));
        if (count <= 0 || entryIndex < 0 || entryIndex >= count) throw new ArgumentOutOfRangeException(nameof(entryIndex));
        int tableEnd = checked(0x10 + count * 8);
        if (tableEnd > dat.Length) throw new InvalidDataException("Tabelas do DAT inválidas.");
        int start = checked((int)BitConverter.ToUInt32(dat, 0x10 + entryIndex * 4));
        int end = dat.Length;
        for (int i = entryIndex + 1; i < count; i++)
        {
            int candidate = checked((int)BitConverter.ToUInt32(dat, 0x10 + i * 4));
            if (candidate > start && candidate <= dat.Length) { end = candidate; break; }
        }
        if (start < tableEnd || start >= dat.Length || end <= start) throw new InvalidDataException("Offset da entrada inválido.");
        int tagOffset = checked(0x10 + count * 4 + entryIndex * 4);
        string tag = Encoding.ASCII.GetString(dat, tagOffset, 4).TrimEnd('\0');
        return (start, end - start, tag);
    }
}
