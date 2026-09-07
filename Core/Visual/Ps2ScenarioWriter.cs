namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2ScenarioWriter
{
    public static void WriteTransforms(ScenarioScene scene, string path)
    {
        ArgumentNullException.ThrowIfNull(scene);
        byte[] output = File.ReadAllBytes(path);
        if (output.Length < 0x10) throw new InvalidDataException("SMD muito pequeno.");
        int count = BitConverter.ToUInt16(output, 2);
        if (count != scene.Entries.Count || 0x10L + count * 0x40L > output.Length)
            throw new InvalidDataException("A tabela de entries do SMD mudou desde que foi carregada.");
        foreach (ScenarioEntry e in scene.Entries)
        {
            if (e.FileOrder < 0 || e.FileOrder >= count) throw new InvalidDataException("Índice de entry SMD inválido.");
            Validate(e);
            int o = 0x10 + e.FileOrder * 0x40;
            Write(output, o, e.PositionX * 100f, e.PositionY * 100f, e.PositionZ * 100f);
            Write(output, o + 0x10, e.RotationX, e.RotationY, e.RotationZ);
            Write(output, o + 0x20, e.ScaleX, e.ScaleY, e.ScaleZ);
        }
        File.WriteAllBytes(path, output);
        foreach(var group in scene.PendingVertexEdits.GroupBy(x=>x.Key.BinId))SmdEmbeddedBinService.SetBinVertexPositions(path,group.Key,scene.BinCount,group.ToDictionary(x=>x.Key.VertexOffset,x=>x.Value));
        foreach(var group in scene.PendingFaceDeletes.GroupBy(x=>x.BinId))SmdEmbeddedBinService.DeleteBinFaces(path,group.Key,scene.BinCount,group.Select(x=>x.StripFlagOffset).ToArray());
        scene.PendingVertexEdits.Clear();
        scene.PendingFaceDeletes.Clear();
        scene.IsModified = false;
    }
    private static void Validate(ScenarioEntry e)
    {
        float[] values = { e.PositionX, e.PositionY, e.PositionZ, e.RotationX, e.RotationY, e.RotationZ, e.ScaleX, e.ScaleY, e.ScaleZ };
        if (values.Any(v => !float.IsFinite(v))) throw new InvalidDataException($"Entry {e.FileOrder}: transformação inválida.");
    }
    private static void Write(byte[] b, int o, float x, float y, float z)
    { BitConverter.GetBytes(x).CopyTo(b, o); BitConverter.GetBytes(y).CopyTo(b, o + 4); BitConverter.GetBytes(z).CopyTo(b, o + 8); }
}
