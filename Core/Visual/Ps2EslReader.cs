namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2EslReader
{
    public const int EntrySize = 0x20;
    public static EslScene Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length % EntrySize != 0) throw new InvalidDataException($"ESL inválido: tamanho 0x{data.Length:X} não é múltiplo de 0x20.");
        var entries = new List<EslEnemyEntry>(data.Length / EntrySize);
        using var br = new BinaryReader(new MemoryStream(data));
        for (int i = 0; i < data.Length / EntrySize; i++) entries.Add(new EslEnemyEntry { Index=i, Active=br.ReadByte(), EnemyType=br.ReadByte(), Subtype=br.ReadByte(), Animation=br.ReadByte(), SightRange=br.ReadByte(), Equip1=br.ReadByte(), Equip2=br.ReadByte(), Weapon=br.ReadByte(), Health=br.ReadUInt16(), Unknown1=br.ReadByte(), ReturnSpawn=br.ReadByte(), PosX=br.ReadInt16(), PosY=br.ReadInt16(), PosZ=br.ReadInt16(), RotX=br.ReadInt16(), RotY=br.ReadInt16(), RotZ=br.ReadInt16(), RoomID=br.ReadByte(), StageID=br.ReadByte(), Unknown2=br.ReadByte(), Unknown3=br.ReadByte(), Unknown4=br.ReadByte(), Unknown5=br.ReadByte(), Unknown6=br.ReadByte(), Unknown7=br.ReadByte() });
        return new EslScene(path, entries);
    }
}
