using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public static class Ps2EffReader
{
    public const int EntrySize = 0x12C;
    public static Ps2EffFile Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 0x30 || U32(data, 0) != 11) throw new InvalidDataException("EFF inválido: cabeçalho 0x0B ausente.");
        uint[] tables = Enumerable.Range(0, 11).Select(i => U32(data, 4 + i * 4)).ToArray();
        foreach (uint value in tables.Where(x => x != 0))
            if (value < 0x30 || value >= data.Length) throw new InvalidDataException($"EFF inválido: offset de tabela 0x{value:X} fora do arquivo.");
        uint previous = 0;
        foreach (uint value in tables.Where(x => x != 0)) { if (value < previous) throw new InvalidDataException("EFF inválido: tabelas fora de ordem."); previous = value; }

        int textureCount = tables[5] == 0 ? 0 : CheckedCount(data, (int)tables[5], 4096, "TPL");
        if (tables[0] != 0 && CheckedCount(data, (int)tables[0], 4096, "Texture IDs") != textureCount)
            throw new InvalidDataException("EFF inválido: quantidades das tabelas 0 e 5 não coincidem.");
        if (tables[6] != 0 && CheckedCount(data, (int)tables[6], 4096, "Texture metadata") != textureCount)
            throw new InvalidDataException("EFF inválido: quantidades das tabelas 5 e 6 não coincidem.");

        var file = new Ps2EffFile { SourcePath = path, TableOffsets = tables, TexturePackageCount = textureCount };
        ReadGroups(data, tables, 7, file.Effect0Groups);
        ReadGroups(data, tables, 8, file.Effect1Groups);
        ValidatePair(data, tables, 1, file.Effect0Groups.Count);
        ValidatePair(data, tables, 2, file.Effect1Groups.Count);
        return file;
    }

    private static void ValidatePair(byte[] data, uint[] tables, int table, int count)
    {
        if (tables[table] != 0 && CheckedCount(data, (int)tables[table], 65535, $"Table {table}") != count)
            throw new InvalidDataException($"EFF inválido: Table {table} não coincide com os grupos de efeitos.");
    }

    private static void ReadGroups(byte[] data, uint[] tables, int tableIndex, List<EffGroup> output)
    {
        if (tables[tableIndex] == 0) return;
        int start = checked((int)tables[tableIndex]);
        int end = NextTableOrEnd(tables, tableIndex, data.Length);
        int count = CheckedCount(data, start, 65535, $"Effect {tableIndex - 7}");
        Ensure(start + 4L + count * 4L <= end, "Tabela de grupos truncada.");
        for (int gi = 0; gi < count; gi++)
        {
            uint relative = U32(data, start + 4 + gi * 4);
            int p = checked(start + (int)relative);
            Ensure(p >= start && p + 0x30 <= end, "Offset de grupo fora da tabela.");
            int effectCount = U16(data, p);
            Ensure(p + 0x30L + effectCount * (long)EntrySize <= end, "EffectEntry ultrapassa a tabela.");
            var group = new EffGroup
            {
                EffectTable = tableIndex - 7, Index = gi, FileOffset = p,
                PositionX = F32(data, p + 0x0C), PositionY = F32(data, p + 0x10), PositionZ = F32(data, p + 0x14),
                RotationX = F32(data, p + 0x18), RotationY = F32(data, p + 0x1C), RotationZ = F32(data, p + 0x20)
            };
            for (int ei = 0; ei < effectCount; ei++)
            {
                int ep = p + 0x30 + ei * EntrySize;
                Ensure(data[ep] == 1, $"EffectEntry sem marcador 01 em 0x{ep:X}.");
                group.Entries.Add(ReadEntry(data, ep, ei, group));
            }
            output.Add(group);
        }
    }

    private static EffEntry ReadEntry(byte[] d, int p, int index, EffGroup group) => new()
    {
        RawData = d.AsSpan(p, EntrySize).ToArray(), Group = group, FileOffset = p, Index = index,
        EspId=d[p+1], ResourceId=d[p+2], Time=U16(d,p+4), Parent=d[p+6], ParentPart=d[p+7], Flags=U32(d,p+8),
        PositionX=F32(d,p+0x0C), PositionY=F32(d,p+0x10), PositionZ=F32(d,p+0x14),
        RandomPositionX=F32(d,p+0x18), RandomPositionY=F32(d,p+0x1C), RandomPositionZ=F32(d,p+0x20),
        SpeedX=F32(d,p+0x24), SpeedY=F32(d,p+0x28), SpeedZ=F32(d,p+0x2C),
        AccelerationX=F32(d,p+0x40), AccelerationY=F32(d,p+0x44), AccelerationZ=F32(d,p+0x48),
        RotationX=F32(d,p+0x58), RotationY=F32(d,p+0x5C), RotationZ=F32(d,p+0x60),
        Width=F32(d,p+0x88), Height=F32(d,p+0x8C), Grow=F32(d,p+0x94),
        ColorR=d[p+0x9C], ColorG=d[p+0x9D], ColorB=d[p+0x9E], ColorA=d[p+0x9F],
        Lifetime=U16(d,p+0xB8), AnimationSpeed=U32(d,p+0xBA), Blend=U16(d,p+0xC1)
    };

    private static int CheckedCount(byte[] d,int p,int max,string name){Ensure(p+4<=d.Length,$"{name} truncada.");uint n=U32(d,p);Ensure(n<=max,$"Contagem inválida em {name}: {n}.");return (int)n;}
    private static int NextTableOrEnd(uint[] t,int index,int length)=>checked((int)t.Skip(index+1).FirstOrDefault(x=>x>t[index],(uint)length));
    private static void Ensure(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
    private static ushort U16(byte[] d,int p)=>BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p,2));
    private static uint U32(byte[] d,int p)=>BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p,4));
    private static float F32(byte[] d,int p)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(d.AsSpan(p,4)));
}
