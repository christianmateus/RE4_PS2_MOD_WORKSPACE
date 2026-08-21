namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2EslWriter
{
    public static void Save(EslScene scene, string? path = null)
    {
        path ??= scene.SourcePath;
        string backup = path + ".bak";
        if (File.Exists(path) && !File.Exists(backup)) File.Copy(path, backup);
        using var bw = new BinaryWriter(File.Create(path));
        foreach (var e in scene.Entries.OrderBy(x => x.Index)) { bw.Write(e.Active); bw.Write(e.EnemyType); bw.Write(e.Subtype); bw.Write(e.Animation); bw.Write(e.SightRange); bw.Write(e.Equip1); bw.Write(e.Equip2); bw.Write(e.Weapon); bw.Write(e.Health); bw.Write(e.Unknown1); bw.Write(e.ReturnSpawn); bw.Write(e.PosX); bw.Write(e.PosY); bw.Write(e.PosZ); bw.Write(e.RotX); bw.Write(e.RotY); bw.Write(e.RotZ); bw.Write(e.RoomID); bw.Write(e.StageID); bw.Write(e.Unknown2); bw.Write(e.Unknown3); bw.Write(e.Unknown4); bw.Write(e.Unknown5); bw.Write(e.Unknown6); bw.Write(e.Unknown7); }
    }
}
