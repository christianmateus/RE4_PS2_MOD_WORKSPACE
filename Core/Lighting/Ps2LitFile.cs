using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Lighting;

public sealed class LitScene
{
    internal byte[] OriginalBytes { get; init; } = Array.Empty<byte>();
    public string SourcePath { get; init; } = string.Empty;
    public ushort SlotCount { get; init; }
    public ushort HeaderValue { get; init; }
    public List<LitGroup> Groups { get; } = new();
    public bool IsModified { get; set; }
    public int LightCount => Groups.Sum(x => x.Lights.Count);
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class LitGroup
{
    internal int Offset { get; init; }
    internal byte[] Raw { get; init; } = new byte[0x64];
    [Browsable(false)] public int SlotIndex { get; init; }
    [Category("Grupo"), DisplayName("Slot")] public string SlotDisplay => $"{SlotIndex} (0x{SlotIndex:X2})";
    [Category("Ambiente"), DisplayName("Cor base R")] public byte BaseR { get; set; }
    [Category("Ambiente"), DisplayName("Cor base G")] public byte BaseG { get; set; }
    [Category("Ambiente"), DisplayName("Cor base B")] public byte BaseB { get; set; }
    [Category("Ambiente"), DisplayName("Cor base A")] public byte BaseA { get; set; }
    [Category("Fog"), DisplayName("Tipo")] public uint FogType { get; set; }
    [Category("Fog"), DisplayName("Início")] public float FogStart { get; set; }
    [Category("Fog"), DisplayName("Fim")] public float FogEnd { get; set; }
    [Category("Fog"), DisplayName("Cor R")] public byte FogR { get; set; }
    [Category("Fog"), DisplayName("Cor G")] public byte FogG { get; set; }
    [Category("Fog"), DisplayName("Cor B")] public byte FogB { get; set; }
    [Category("Fog"), DisplayName("Cor A")] public byte FogA { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Tipo")] public uint MirrorFogType { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Início")] public float MirrorFogStart { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Fim")] public float MirrorFogEnd { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Cor R")] public byte MirrorFogR { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Cor G")] public byte MirrorFogG { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Cor B")] public byte MirrorFogB { get; set; }
    [Category("Mirror/Sky Fog"), DisplayName("Cor A")] public byte MirrorFogA { get; set; }
    [Category("Foco"), DisplayName("Distância focal")] public int FocalDistance { get; set; }
    [Category("Foco"), DisplayName("Nível")] public byte FocusLevel { get; set; }
    [Category("Foco"), DisplayName("Desconhecido 0x2D")] public byte FocusUnknown { get; set; }
    [Category("Foco"), DisplayName("Modo")] public byte FocusMode { get; set; }
    [Category("Foco"), DisplayName("Blur rate")] public byte BlurRate { get; set; }
    [Category("Tuning SMD"), DisplayName("R")] public byte TuningSmdR { get; set; }
    [Category("Tuning SMD"), DisplayName("G")] public byte TuningSmdG { get; set; }
    [Category("Tuning SMD"), DisplayName("B")] public byte TuningSmdB { get; set; }
    [Category("Tuning SMD"), DisplayName("A")] public byte TuningSmdA { get; set; }
    [Category("Tuning Enemy"), DisplayName("R")] public byte TuningEnemyR { get; set; }
    [Category("Tuning Enemy"), DisplayName("G")] public byte TuningEnemyG { get; set; }
    [Category("Tuning Enemy"), DisplayName("B")] public byte TuningEnemyB { get; set; }
    [Category("Tuning Enemy"), DisplayName("A")] public byte TuningEnemyA { get; set; }
    [Category("Tuning Player"), DisplayName("R")] public byte TuningPlayerR { get; set; }
    [Category("Tuning Player"), DisplayName("G")] public byte TuningPlayerG { get; set; }
    [Category("Tuning Player"), DisplayName("B")] public byte TuningPlayerB { get; set; }
    [Category("Tuning Player"), DisplayName("A")] public byte TuningPlayerA { get; set; }
    [Category("Render"), DisplayName("Multiplicador SMD")] public byte SmdMultiplier { get; set; }
    [Category("Render"), DisplayName("Multiplicador player")] public byte PlayerMultiplier { get; set; }
    [Category("Render"), DisplayName("Distance culling")] public float DistanceCulling { get; set; }
    [Category("Render"), DisplayName("Hokan (desconhecido)")] public float Hokan { get; set; }
    [Category("Vento"), DisplayName("Direção")] public byte WindDirection { get; set; }
    [Category("Vento"), DisplayName("Força")] public byte WindPower { get; set; }
    [Category("Vento"), DisplayName("Frequência")] public byte WindFrequency { get; set; }
    [Category("Vento"), DisplayName("Desconhecido")] public byte WindUnknown { get; set; }
    [Category("Blur/Mipmap"), DisplayName("Tipo blur")] public byte BlurType { get; set; }
    [Category("Blur/Mipmap"), DisplayName("Força blur")] public byte BlurPower { get; set; }
    [Category("Blur/Mipmap"), DisplayName("Mipmap mínimo")] public byte MipmapMin { get; set; }
    [Category("Blur/Mipmap"), DisplayName("Mipmap máximo")] public byte MipmapMax { get; set; }
    [Category("Contraste"), DisplayName("Formato de filtro")] public byte FilteringFormat { get; set; }
    [Category("Contraste"), DisplayName("Nível")] public byte ContrastLevel { get; set; }
    [Category("Contraste"), DisplayName("Força")] public byte ContrastPower { get; set; }
    [Category("Contraste"), DisplayName("Bias")] public byte ContrastBias { get; set; }
    [Category("Render"), DisplayName("LOD bias provável")] public float LodBias { get; set; }
    [Browsable(false)] public List<LitLight> Lights { get; } = new();
    public override string ToString() => $"GRUPO {SlotIndex:00} • {Lights.Count} luz(es)";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class LitLight
{
    internal int Offset { get; init; }
    internal byte[] Raw { get; init; } = new byte[0x70];
    [Browsable(false)] public int Index { get; init; }
    [Category("Identificação"), DisplayName("Estado (07 ativo / 05 inativo)")] public byte State { get; set; }
    [Category("Identificação"), DisplayName("Tipo")] public byte Type { get; set; }
    [Category("Identificação"), DisplayName("Atributo")] public byte Attribute { get; set; }
    [Category("Identificação"), DisplayName("Máscara")] public byte Mask { get; set; }
    [Category("Luz"), DisplayName("Alcance")] public float Range { get; set; }
    [Category("Luz"), DisplayName("Cor R")] public byte ColorR { get; set; }
    [Category("Luz"), DisplayName("Cor G")] public byte ColorG { get; set; }
    [Category("Luz"), DisplayName("Cor B")] public byte ColorB { get; set; }
    [Category("Luz"), DisplayName("Cor A")] public byte ColorA { get; set; }
    [Category("Luz"), DisplayName("Intensidade")] public float Intensity { get; set; }
    [Category("Posição primária"), DisplayName("X")] public float PositionX { get; set; }
    [Category("Posição primária"), DisplayName("Y")] public float PositionY { get; set; }
    [Category("Posição primária"), DisplayName("Z")] public float PositionZ { get; set; }
    [Category("Posição/vetor secundário"), DisplayName("X2")] public float PositionX2 { get; set; }
    [Category("Posição/vetor secundário"), DisplayName("Y2")] public float PositionY2 { get; set; }
    [Category("Posição/vetor secundário"), DisplayName("Z2")] public float PositionZ2 { get; set; }
    [Category("Identificação"), DisplayName("Origem/attachment")]
    public byte Origin { get; set; }
    [Browsable(false)] public bool IsActive => State == 0x07;
    [Browsable(false)] public Vector3 DisplayPosition
    {
        get
        {
            var primary = new Vector3(PositionX, PositionY, PositionZ);
            var secondary = new Vector3(PositionX2, PositionY2, PositionZ2);
            return (primary.LengthSquared() > 0.0001f ? primary : secondary) / 100f;
        }
    }
    public override string ToString() => $"LUZ {Index:00} • T{Type} • M 0x{Mask:X2}{(IsActive ? "" : " • INATIVA")}";
}

public static class Ps2LitReader
{
    public static LitScene Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 8) throw new InvalidDataException("LIT pequeno demais.");
        ushort slots = BitConverter.ToUInt16(data, 0);
        int tableEnd = checked(4 + slots * 4);
        if (tableEnd > data.Length) throw new InvalidDataException("Tabela de grupos LIT truncada.");
        var scene = new LitScene { SourcePath = path, SlotCount = slots, HeaderValue = BitConverter.ToUInt16(data, 2), OriginalBytes = data };
        for (int slot = 0; slot < slots; slot++)
        {
            uint pointer = BitConverter.ToUInt32(data, 4 + slot * 4);
            if (pointer == 0) continue;
            int offset = checked((int)pointer);
            if (offset < tableEnd || offset + 0x64 > data.Length) throw new InvalidDataException($"Ponteiro inválido no slot {slot}: 0x{offset:X}.");
            uint count = BitConverter.ToUInt32(data, offset + 4);
            long end = (long)offset + 0x64L + count * 0x70L;
            if (count > 4096 || end > data.Length) throw new InvalidDataException($"Grupo LIT {slot} truncado ou com contagem inválida.");
            var group = new LitGroup
            {
                SlotIndex=slot, Offset=offset, Raw=data.AsSpan(offset,0x64).ToArray(),
                BaseR=data[offset], BaseG=data[offset+1], BaseB=data[offset+2], BaseA=data[offset+3],
                FogType=BitConverter.ToUInt32(data,offset+8), FogStart=BitConverter.ToSingle(data,offset+12), FogEnd=BitConverter.ToSingle(data,offset+16),
                FogR=data[offset+20], FogG=data[offset+21], FogB=data[offset+22], FogA=data[offset+23],
                MirrorFogType=BitConverter.ToUInt32(data,offset+24),MirrorFogStart=BitConverter.ToSingle(data,offset+28),MirrorFogEnd=BitConverter.ToSingle(data,offset+32),MirrorFogR=data[offset+36],MirrorFogG=data[offset+37],MirrorFogB=data[offset+38],MirrorFogA=data[offset+39],
                FocalDistance=BitConverter.ToInt32(data,offset+40),FocusLevel=data[offset+44],FocusUnknown=data[offset+45],FocusMode=data[offset+46],BlurRate=data[offset+47],
                TuningSmdR=data[offset+52],TuningSmdG=data[offset+53],TuningSmdB=data[offset+54],TuningSmdA=data[offset+55],TuningEnemyR=data[offset+56],TuningEnemyG=data[offset+57],TuningEnemyB=data[offset+58],TuningEnemyA=data[offset+59],TuningPlayerR=data[offset+60],TuningPlayerG=data[offset+61],TuningPlayerB=data[offset+62],TuningPlayerA=data[offset+63],
                SmdMultiplier=data[offset+64],PlayerMultiplier=data[offset+65],DistanceCulling=BitConverter.ToSingle(data,offset+68),Hokan=BitConverter.ToSingle(data,offset+72),WindDirection=data[offset+76],WindPower=data[offset+77],WindFrequency=data[offset+78],WindUnknown=data[offset+79],BlurType=data[offset+80],BlurPower=data[offset+81],MipmapMin=data[offset+82],MipmapMax=data[offset+83],FilteringFormat=data[offset+84],ContrastLevel=data[offset+85],ContrastPower=data[offset+86],ContrastBias=data[offset+87],LodBias=BitConverter.ToSingle(data,offset+88)
            };
            for (int i=0;i<count;i++)
            {
                int p=offset+0x64+i*0x70;
                group.Lights.Add(new LitLight
                {
                    Index=i, Offset=p, Raw=data.AsSpan(p,0x70).ToArray(), State=data[p], Type=data[p+1], Attribute=data[p+2], Mask=data[p+3],
                    Range=BitConverter.ToSingle(data,p+4), ColorR=data[p+8], ColorG=data[p+9], ColorB=data[p+10], ColorA=data[p+11], Intensity=BitConverter.ToSingle(data,p+12),
                    PositionX=BitConverter.ToSingle(data,p+16), PositionY=BitConverter.ToSingle(data,p+20), PositionZ=BitConverter.ToSingle(data,p+24),
                    PositionX2=BitConverter.ToSingle(data,p+44), PositionY2=BitConverter.ToSingle(data,p+48), PositionZ2=BitConverter.ToSingle(data,p+52), Origin=data[p+56]
                });
            }
            scene.Groups.Add(group);
        }
        return scene;
    }
}

public static class Ps2LitWriter
{
    public static void Write(LitScene scene, string path)
    {
        byte[] data=(byte[])scene.OriginalBytes.Clone();
        foreach (LitGroup g in scene.Groups)
        {
            int p=g.Offset; data[p]=g.BaseR;data[p+1]=g.BaseG;data[p+2]=g.BaseB;data[p+3]=g.BaseA;
            Put(data,p+8,g.FogType);Put(data,p+12,g.FogStart);Put(data,p+16,g.FogEnd);data[p+20]=g.FogR;data[p+21]=g.FogG;data[p+22]=g.FogB;data[p+23]=g.FogA;
            Put(data,p+24,g.MirrorFogType);Put(data,p+28,g.MirrorFogStart);Put(data,p+32,g.MirrorFogEnd);data[p+36]=g.MirrorFogR;data[p+37]=g.MirrorFogG;data[p+38]=g.MirrorFogB;data[p+39]=g.MirrorFogA;BitConverter.GetBytes(g.FocalDistance).CopyTo(data,p+40);data[p+44]=g.FocusLevel;data[p+45]=g.FocusUnknown;data[p+46]=g.FocusMode;data[p+47]=g.BlurRate;
            data[p+52]=g.TuningSmdR;data[p+53]=g.TuningSmdG;data[p+54]=g.TuningSmdB;data[p+55]=g.TuningSmdA;data[p+56]=g.TuningEnemyR;data[p+57]=g.TuningEnemyG;data[p+58]=g.TuningEnemyB;data[p+59]=g.TuningEnemyA;data[p+60]=g.TuningPlayerR;data[p+61]=g.TuningPlayerG;data[p+62]=g.TuningPlayerB;data[p+63]=g.TuningPlayerA;data[p+64]=g.SmdMultiplier;data[p+65]=g.PlayerMultiplier;Put(data,p+68,g.DistanceCulling);Put(data,p+72,g.Hokan);data[p+76]=g.WindDirection;data[p+77]=g.WindPower;data[p+78]=g.WindFrequency;data[p+79]=g.WindUnknown;data[p+80]=g.BlurType;data[p+81]=g.BlurPower;data[p+82]=g.MipmapMin;data[p+83]=g.MipmapMax;data[p+84]=g.FilteringFormat;data[p+85]=g.ContrastLevel;data[p+86]=g.ContrastPower;data[p+87]=g.ContrastBias;Put(data,p+88,g.LodBias);
            foreach(LitLight l in g.Lights)
            {
                p=l.Offset;data[p]=l.State;data[p+1]=l.Type;data[p+2]=l.Attribute;data[p+3]=l.Mask;Put(data,p+4,l.Range);data[p+8]=l.ColorR;data[p+9]=l.ColorG;data[p+10]=l.ColorB;data[p+11]=l.ColorA;Put(data,p+12,l.Intensity);Put(data,p+16,l.PositionX);Put(data,p+20,l.PositionY);Put(data,p+24,l.PositionZ);Put(data,p+44,l.PositionX2);Put(data,p+48,l.PositionY2);Put(data,p+52,l.PositionZ2);data[p+56]=l.Origin;
            }
        }
        File.WriteAllBytes(path,data); scene.IsModified=false;
    }
    private static void Put(byte[] data,int offset,float value)=>BitConverter.GetBytes(value).CopyTo(data,offset);
    private static void Put(byte[] data,int offset,uint value)=>BitConverter.GetBytes(value).CopyTo(data,offset);
}
