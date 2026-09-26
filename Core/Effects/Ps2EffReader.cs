using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public static class Ps2EffReader
{
    public const int EntrySize = 0x12C;
    public const int GroupHeaderSize = 0x30;

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
        if (tables[0] != 0 && CheckedCount(data, (int)tables[0], 4096, "Texture IDs") != textureCount) throw new InvalidDataException("EFF inválido: quantidades das tabelas 0 e 5 não coincidem.");
        if (tables[6] != 0 && CheckedCount(data, (int)tables[6], 4096, "Texture metadata") != textureCount) throw new InvalidDataException("EFF inválido: quantidades das tabelas 5 e 6 não coincidem.");
        var file = new Ps2EffFile { SourcePath = path, TableOffsets = tables, TexturePackageCount = textureCount };
        ReadGroups(data, tables, 7, EffSequenceTable.EST, file.EstGroups);
        ReadGroups(data, tables, 8, EffSequenceTable.SST, file.SstGroups);
        ValidatePair(data, tables, 1, file.EstGroups.Count);
        ValidatePair(data, tables, 2, file.SstGroups.Count);
        return file;
    }

    private static void ValidatePair(byte[] data, uint[] tables, int table, int count)
    {
        if (tables[table] != 0 && CheckedCount(data, (int)tables[table], 65535, $"Table {table}") != count) throw new InvalidDataException($"EFF inválido: Table {table} não coincide com os grupos de efeitos.");
    }

    private static void ReadGroups(byte[] data, uint[] tables, int tableIndex, EffSequenceTable sequenceTable, List<EffGroup> output)
    {
        if (tables[tableIndex] == 0) return;
        int start = checked((int)tables[tableIndex]);
        int end = NextTableOrEnd(tables, tableIndex, data.Length);
        int count = CheckedCount(data, start, 65535, sequenceTable.ToString());
        Ensure(start + 4L + count * 4L <= end, "Tabela de grupos truncada.");
        for (int gi = 0; gi < count; gi++)
        {
            uint relative = U32(data, start + 4 + gi * 4);
            int p = checked(start + (int)relative);
            Ensure(p >= start && p + GroupHeaderSize <= end, "Offset de grupo fora da tabela.");
            int effectCount = U16(data, p);
            Ensure(p + GroupHeaderSize + effectCount * (long)EntrySize <= end, "EffectEntry ultrapassa a tabela.");
            var group = new EffGroup
            {
                SequenceTable = sequenceTable, Index = gi, FileOffset = p, RawHeader = data.AsSpan(p, GroupHeaderSize).ToArray(),
                Flags = U16(data, p + 8), DefaultParts = data[p + 0x0A],
                PositionX = F32(data, p + 0x0C), PositionY = F32(data, p + 0x10), PositionZ = F32(data, p + 0x14),
                RotationX = F32(data, p + 0x18), RotationY = F32(data, p + 0x1C), RotationZ = F32(data, p + 0x20),
                Version = data[p + 0x24], CoreFlags = U16(data, p + 0x26)
            };
            for (int ei = 0; ei < effectCount; ei++)
            {
                int ep = p + GroupHeaderSize + ei * EntrySize;
                Ensure(data[ep] == 1, $"EffectEntry sem marcador 01 em 0x{ep:X}.");
                group.Entries.Add(ReadEntry(data, ep, ei, group));
            }
            output.Add(group);
        }
    }

    private static EffEntry ReadEntry(byte[] d, int p, int index, EffGroup group) => new()
    {
        RawData=d.AsSpan(p,EntrySize).ToArray(), Group=group, FileOffset=p, Index=index,
        EnabledMarker=d[p], EspId=d[p+1], ResourceId=d[p+2], Type=d[p+3], Time=U16(d,p+4), Parent=d[p+6], ParentPart=d[p+7], Flags=U32(d,p+8),
        PositionX=F32(d,p+0x0C),PositionY=F32(d,p+0x10),PositionZ=F32(d,p+0x14),RandomPositionX=F32(d,p+0x18),RandomPositionY=F32(d,p+0x1C),RandomPositionZ=F32(d,p+0x20),
        SpeedX=F32(d,p+0x24),SpeedY=F32(d,p+0x28),SpeedZ=F32(d,p+0x2C),SpeedDamping=F32(d,p+0x30),RandomSpeedX=F32(d,p+0x34),RandomSpeedY=F32(d,p+0x38),RandomSpeedZ=F32(d,p+0x3C),
        AccelerationX=F32(d,p+0x40),AccelerationY=F32(d,p+0x44),AccelerationZ=F32(d,p+0x48),RandomAccelerationX=F32(d,p+0x4C),RandomAccelerationY=F32(d,p+0x50),RandomAccelerationZ=F32(d,p+0x54),
        RotationX=F32(d,p+0x58),RotationY=F32(d,p+0x5C),RotationZ=F32(d,p+0x60),RandomRotationX=F32(d,p+0x64),RandomRotationY=F32(d,p+0x68),RandomRotationZ=F32(d,p+0x6C),
        AngularVelocityX=F32(d,p+0x70),AngularVelocityY=F32(d,p+0x74),AngularVelocityZ=F32(d,p+0x78),RandomAngularVelocityX=F32(d,p+0x7C),RandomAngularVelocityY=F32(d,p+0x80),RandomAngularVelocityZ=F32(d,p+0x84),
        Width=F32(d,p+0x88),Height=F32(d,p+0x8C),RandomSize=F32(d,p+0x90),Grow=F32(d,p+0x94),GrowDamping=F32(d,p+0x98),
        ColorR=d[p+0x9C],ColorG=d[p+0x9D],ColorB=d[p+0x9E],ColorA=d[p+0x9F],ColorStepR=F32(d,p+0xA0),ColorStepG=F32(d,p+0xA4),ColorStepB=F32(d,p+0xA8),ColorStepA=F32(d,p+0xAC),
        ColorMaxCount=U16(d,p+0xB0),ColorStartCount=U16(d,p+0xB2),PositionStartCount=U16(d,p+0xB4),SizeStartCount=U16(d,p+0xB6),Lifetime=U16(d,p+0xB8),LifeTime=U16(d,p+0xBA),
        PatternNumber=d[p+0xBC],AnimationRate=unchecked((sbyte)d[p+0xBD]),AnimationCounter=U16(d,p+0xBE),ReleaseTime=d[p+0xC0],GroupeNumber=d[p+0xC1],BlendType=d[p+0xC2],ShimmerType=d[p+0xC3],ShimmerPower=d[p+0xC4],MaskTextureId=d[p+0xC5],DeleteFar=d[p+0xC6],DeleteNear=d[p+0xC7],
        Work8_0=d[p+0xC8],Work8_1=d[p+0xC9],Work8_2=d[p+0xCA],Work8_3=d[p+0xCB],Work32_0=U32(d,p+0xCC),Work32_1=U32(d,p+0xD0),Work32_2=U32(d,p+0xD4),
        Vector0X=F32(d,p+0xD8),Vector0Y=F32(d,p+0xDC),Vector0Z=F32(d,p+0xE0),Vector1X=F32(d,p+0xE4),Vector1Y=F32(d,p+0xE8),Vector1Z=F32(d,p+0xEC),Vector2X=F32(d,p+0xF0),Vector2Y=F32(d,p+0xF4),Vector2Z=F32(d,p+0xF8),
        WorkSpecial8_0=d[p+0xFC],WorkSpecial8_1=d[p+0xFD],WorkSpecial8_2=d[p+0xFE],WorkSpecial8_3=d[p+0xFF],Reserved100=U32(d,p+0x100),GeneratorExtra0=d[p+0x104],GeneratorExtra1=d[p+0x105],GeneratorExtra2=d[p+0x106],GeneratorExtra3=d[p+0x107],
        Kind=(EffEntryKind)d[p+0x108],EspgenId=d[p+0x109],EspgenType=d[p+0x10A],EspgenFlags=d[p+0x10B],GeneratorWork8_0=unchecked((sbyte)d[p+0x10C]),GeneratorWork8_1=unchecked((sbyte)d[p+0x10D]),GeneratorWork8_2=unchecked((sbyte)d[p+0x10E]),GeneratorWork8_3=unchecked((sbyte)d[p+0x10F]),
        GeneratorWork16_0=I16(d,p+0x110),GeneratorWork16_1=I16(d,p+0x112),GeneratorWork16_2=I16(d,p+0x114),GeneratorWork16_3=I16(d,p+0x116),GeneratorVectorX=F32(d,p+0x118),GeneratorVectorY=F32(d,p+0x11C),GeneratorVectorZ=F32(d,p+0x120),
        GeneratorCurve0=unchecked((sbyte)d[p+0x124]),GeneratorCurve1=unchecked((sbyte)d[p+0x125]),GeneratorCurve2=unchecked((sbyte)d[p+0x126]),GeneratorCurve3=unchecked((sbyte)d[p+0x127]),GeneratorParameter0=d[p+0x128],GeneratorParameter1=d[p+0x129],GeneratorParameter2=d[p+0x12A],GeneratorParameter3=d[p+0x12B]
    };

    private static int CheckedCount(byte[] d,int p,int max,string name){Ensure(p+4<=d.Length,$"{name} truncada.");uint n=U32(d,p);Ensure(n<=max,$"Contagem inválida em {name}: {n}.");return (int)n;}
    private static int NextTableOrEnd(uint[] t,int index,int length)=>checked((int)t.Skip(index+1).FirstOrDefault(x=>x>t[index],(uint)length));
    private static void Ensure(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
    internal static ushort U16(byte[] d,int p)=>BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p,2));
    internal static short I16(byte[] d,int p)=>BinaryPrimitives.ReadInt16LittleEndian(d.AsSpan(p,2));
    internal static uint U32(byte[] d,int p)=>BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p,4));
    internal static float F32(byte[] d,int p)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(d.AsSpan(p,4)));
}
