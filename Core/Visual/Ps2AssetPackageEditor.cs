namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>Rebuilds ETM/ITM containers while preserving every untouched resource byte-for-byte.</summary>
public static class Ps2AssetPackageEditor
{
    public static void Save(string sourcePath, string destinationPath, IReadOnlyDictionary<int, byte[]> replacements)
    {
        string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        byte[] source = File.ReadAllBytes(sourcePath);
        string? folder = Path.GetDirectoryName(destinationPath); if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        if (replacements.Count == 0) { File.WriteAllBytes(destinationPath, source); return; }
        byte[] output = ext == ".etm" ? RebuildEtm(source, replacements) : ext == ".itm" ? RebuildItm(source, replacements) : throw new NotSupportedException("Abra um pacote ETM ou ITM.");
        File.WriteAllBytes(destinationPath, output);
    }

    private static byte[] RebuildEtm(byte[] source, IReadOnlyDictionary<int, byte[]> replacements)
    {
        int count = checked((int)BitConverter.ToUInt32(source, 0)), cursor = 0x20;
        using var output = new MemoryStream(); output.Write(source, 0, 0x20);
        for (int i = 0; i < count; i++)
        {
            int size = checked((int)BitConverter.ToUInt32(source, cursor));
            byte[] payload = replacements.TryGetValue(i, out byte[]? value) ? value : source.AsSpan(cursor + 0x40, size - 0x40).ToArray();
            byte[] header = source.AsSpan(cursor, 0x40).ToArray(); BitConverter.GetBytes(checked((uint)(0x40 + payload.Length))).CopyTo(header, 0);
            output.Write(header); output.Write(payload); cursor += size;
        }
        if (cursor < source.Length) output.Write(source, cursor, source.Length - cursor);
        return output.ToArray();
    }

    private static byte[] RebuildItm(byte[] source, IReadOnlyDictionary<int, byte[]> replacements)
    {
        int models = Offset(source, 8), textures = Offset(source, 12), count = checked((int)BitConverter.ToUInt32(source, models));
        byte[][] bins = new byte[count][], tpls = new byte[count][];
        for (int i = 0; i < count; i++)
        {
            int a = models + Offset(source, models + 8 + i * 4), next = i + 1 < count ? models + Offset(source, models + 12 + i * 4) : textures;
            int b = next > a && next <= textures ? next : textures;
            int ta = textures + Offset(source, textures + 8 + i * 4), textureNext = i + 1 < count ? textures + Offset(source, textures + 12 + i * 4) : source.Length;
            int tb = textureNext > ta && textureNext <= source.Length ? textureNext : source.Length;
            bins[i] = replacements.TryGetValue(i, out byte[]? bin) ? bin : source.AsSpan(a, b - a).ToArray();
            tpls[i] = replacements.TryGetValue(100000 + i, out byte[]? tpl) ? tpl : source.AsSpan(ta, tb - ta).ToArray();
        }
        byte[] modelPrefix = source.AsSpan(models, Offset(source, models + 8)).ToArray();
        using var modelSection = new MemoryStream(); modelSection.Write(modelPrefix); int modelCursor = modelPrefix.Length;
        for (int i = 0; i < count; i++) { Patch(modelSection, 8 + i * 4, modelCursor); modelSection.Position = modelSection.Length; modelSection.Write(bins[i]); modelCursor += bins[i].Length; }
        byte[] texturePrefix = source.AsSpan(textures, Offset(source, textures + 8)).ToArray();
        using var textureSection = new MemoryStream(); textureSection.Write(texturePrefix); int textureCursor = texturePrefix.Length;
        for (int i = 0; i < count; i++) { Patch(textureSection, 8 + i * 4, textureCursor); textureSection.Position = textureSection.Length; textureSection.Write(tpls[i]); textureCursor += tpls[i].Length; }
        using var output = new MemoryStream(); output.Write(source, 0, models); output.Write(modelSection.ToArray()); int newTextures = checked((int)output.Length); output.Write(textureSection.ToArray());
        byte[] result = output.ToArray(); BitConverter.GetBytes(newTextures).CopyTo(result, 12); return result;
    }

    private static int Offset(byte[] data, int at) => checked((int)BitConverter.ToUInt32(data, at));
    private static void Patch(MemoryStream stream, int at, int value) { long old = stream.Position; stream.Position = at; stream.Write(BitConverter.GetBytes(value)); stream.Position = old; }
}
