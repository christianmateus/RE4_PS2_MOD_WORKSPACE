using System.Buffers.Binary;
using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Lighting;

public sealed class LitScene
{
    internal byte[] OriginalBytes { get; init; } = Array.Empty<byte>();
    public string SourcePath { get; init; } = string.Empty;
    public ushort SlotCount { get; init; }
    public byte Version { get; init; }
    public byte MaxLightCount { get; init; }
    [Browsable(false)] public ushort HeaderValue => (ushort)(Version | MaxLightCount << 8);
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
    [Category("Ambiente"), DisplayName("Cor do cenário R")] public byte BaseR { get; set; }
    [Category("Ambiente"), DisplayName("Cor do cenário G")] public byte BaseG { get; set; }
    [Category("Ambiente"), DisplayName("Cor do cenário B")] public byte BaseB { get; set; }
    [Category("Ambiente"), DisplayName("Cor do cenário A")] public byte BaseA { get; set; }
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
    [Category("Foco"), DisplayName("Flags")] public byte FocusFlag { get; set; }
    [Category("Foco"), DisplayName("Nível")] public byte FocusLevel { get; set; }
    [Category("Foco"), DisplayName("Modo")] public byte FocusMode { get; set; }
    [Category("Foco"), DisplayName("Blur rate")] public byte BlurRate { get; set; }
    [Category("Tuning"), DisplayName("Ativar tuning/flags")] public uint TuningFlags { get; set; }
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
    [Category("Render"), DisplayName("Escala TEV/SMD 0")] public byte SmdMultiplier { get; set; }
    [Category("Render"), DisplayName("Escala TEV/player 1")] public byte PlayerMultiplier { get; set; }
    [Category("Render"), DisplayName("Escala TEV 2")] public byte TevScale2 { get; set; }
    [Category("Render"), DisplayName("Escala TEV 3")] public byte TevScale3 { get; set; }
    [Category("Render"), DisplayName("Razão do plano distante")] public float DistanceCulling { get; set; }
    [Category("Fog"), DisplayName("Transição/Hokan")] public float Hokan { get; set; }
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
    [Category("Render"), DisplayName("LOD bias")] public float LodBias { get; set; }
    [Category("Ambiente adicional 1"), DisplayName("R")] public byte ExtraAmbient1R { get; set; }
    [Category("Ambiente adicional 1"), DisplayName("G")] public byte ExtraAmbient1G { get; set; }
    [Category("Ambiente adicional 1"), DisplayName("B")] public byte ExtraAmbient1B { get; set; }
    [Category("Ambiente adicional 1"), DisplayName("A")] public byte ExtraAmbient1A { get; set; }
    [Category("Ambiente adicional 2"), DisplayName("R")] public byte ExtraAmbient2R { get; set; }
    [Category("Ambiente adicional 2"), DisplayName("G")] public byte ExtraAmbient2G { get; set; }
    [Category("Ambiente adicional 2"), DisplayName("B")] public byte ExtraAmbient2B { get; set; }
    [Category("Ambiente adicional 2"), DisplayName("A")] public byte ExtraAmbient2A { get; set; }
    [Browsable(false)] public List<LitLight> Lights { get; } = new();
    public override string ToString() => $"GRUPO {SlotIndex:00} • {Lights.Count} luz(es)";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class LitLight
{
    internal int Offset { get; init; }
    internal byte[] Raw { get; init; } = new byte[0x70];
    [Browsable(false)] public int Index { get; init; }
    [Category("Identificação"), DisplayName("Flags de estado")] public byte State { get; set; }
    [Category("Identificação"), DisplayName("Modelo da luz")] public byte Type { get; set; }
    [Category("Identificação"), DisplayName("Comportamento")] public byte Attribute { get; set; }
    [Category("Identificação"), DisplayName("Máscara de aplicação")] public byte Mask { get; set; }
    [Category("Identificação"), DisplayName("Modelo (descrição)")] public string TypeDescription => Type switch { 0=>"Constante",1=>"Linear",2=>"Quadrática",3=>"Spotlight",4=>"Customizada",5=>"Direcional/paralela",6=>"Spot quadrática",7=>"Ambiente local",_=>$"Desconhecida {Type}" };
    [Category("Identificação"), DisplayName("Comportamento (descrição)")] public string BehaviorDescription => Attribute switch { 0=>"Estática",1=>"Flicker",2=>"Pulso senoidal",3=>"Rotação",4=>"Sombra",5=>"Light path",6=>"Fade linear",7=>"Direção rotativa",8=>"Rastrear personagem",0x10=>"Fade rápido",_=>$"Desconhecido {Attribute}" };
    [Category("Luz"), DisplayName("Raio (0 = infinito)")] public float Range { get; set; }
    [Category("Luz"), DisplayName("Cor R")] public byte ColorR { get; set; }
    [Category("Luz"), DisplayName("Cor G")] public byte ColorG { get; set; }
    [Category("Luz"), DisplayName("Cor B")] public byte ColorB { get; set; }
    [Category("Luz"), DisplayName("Cor A (128 = normal)")] public byte ColorA { get; set; }
    [Category("Luz"), DisplayName("Intensidade")] public float Intensity { get; set; }
    [Category("Posição"), DisplayName("X")] public float PositionX { get; set; }
    [Category("Posição"), DisplayName("Y")] public float PositionY { get; set; }
    [Category("Posição"), DisplayName("Z")] public float PositionZ { get; set; }
    [Category("Parent/attachment"), DisplayName("Tipo do parent")] public byte ParentType { get; set; }
    [Category("Parent/attachment"), DisplayName("Kind")] public byte Kind { get; set; }
    [Category("Parent/attachment"), DisplayName("Atributo runtime")] public byte RuntimeAttribute { get; set; }
    [Category("Parent/attachment"), DisplayName("Prioridade")] public byte Priority { get; set; }
    [Category("Parent/attachment"), DisplayName("Parent/partes combinado")] public uint ParentNumber { get; set; }
    [Category("Parent/attachment"), DisplayName("ID do parent")] public ushort ParentId { get => (ushort)ParentNumber; set => ParentNumber=(ParentNumber&0xFFFF0000u)|value; }
    [Category("Parent/attachment"), DisplayName("Índice da parte")] public ushort ParentPart { get => (ushort)(ParentNumber>>16); set => ParentNumber=(ParentNumber&0xFFFFu)|((uint)value<<16); }
    [Category("Parent/attachment"), DisplayName("Raio de colisão")] public ushort HitRadius { get; set; }
    [Category("Avançado"), DisplayName("Unknown 0x26")] public ushort Unknown26 { get; set; }
    [Category("Avançado"), DisplayName("Unknown 0x28")] public uint Unknown28 { get; set; }
    [Category("Direção/spot"), DisplayName("Direção X")] public float DirectionX { get; set; }
    [Category("Direção/spot"), DisplayName("Direção Y")] public float DirectionY { get; set; }
    [Category("Direção/spot"), DisplayName("Direção Z")] public float DirectionZ { get; set; }
    [Category("Direção/spot"), DisplayName("A0 / ângulo")] public float A0 { get; set; }
    [Category("Direção/spot"), DisplayName("A1 / largura do fade")] public float A1 { get; set; }
    [Category("Direção/spot"), DisplayName("A2")] public float A2 { get; set; }
    [Category("Direção/spot"), DisplayName("K0")] public float K0 { get; set; }
    [Category("Direção/spot"), DisplayName("K1")] public float K1 { get; set; }
    [Category("Direção/spot"), DisplayName("K2")] public float K2 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 0 (raw)")] public uint Work0 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 1 (raw)")] public uint Work1 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 2 (raw)")] public uint Work2 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 3 (raw)")] public uint Work3 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 4 (raw)")] public uint Work4 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 5 (raw)")] public uint Work5 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 6 (raw)")] public uint Work6 { get; set; }
    [Category("Dados do comportamento"), DisplayName("Work 7 (raw)")] public uint Work7 { get; set; }
    [Browsable(false)] public bool IsActive => (State&0x03)==0x03;
    [Browsable(false)] public bool IsRoomLight => (State&0x04)!=0;
    [Browsable(false)] public Vector3 DisplayPosition => new Vector3(PositionX,PositionY,PositionZ)/100f;
    [Browsable(false)] public Vector3 Direction => new(DirectionX,DirectionY,DirectionZ);
    [Browsable(false)] public float WorkFloat(int index)=>BitConverter.Int32BitsToSingle(unchecked((int)GetWork(index)));
    [Browsable(false)] public sbyte FlickerRange=>unchecked((sbyte)(Work1&0xFF));
    private uint GetWork(int index)=>index switch{0=>Work0,1=>Work1,2=>Work2,3=>Work3,4=>Work4,5=>Work5,6=>Work6,7=>Work7,_=>0};
    public override string ToString()=>$"LUZ {Index:00} • {TypeDescription} • M 0x{Mask:X2}{(IsActive?"":" • INATIVA")}";
}

public static class Ps2LitReader
{
    public static LitScene Read(string path)
    {
        byte[] data=File.ReadAllBytes(path);if(data.Length<8)throw new InvalidDataException("LIT pequeno demais.");
        ushort slots=U16(data,0);int tableEnd=checked(4+slots*4);if(tableEnd>data.Length)throw new InvalidDataException("Tabela de grupos LIT truncada.");
        var scene=new LitScene{SourcePath=path,SlotCount=slots,Version=data[2],MaxLightCount=data[3],OriginalBytes=data};
        for(int slot=0;slot<slots;slot++)
        {
            uint pointer=U32(data,4+slot*4);if(pointer==0)continue;int offset=checked((int)pointer);if(offset<tableEnd||offset+0x64>data.Length)throw new InvalidDataException($"Ponteiro inválido no slot {slot}: 0x{offset:X}.");
            uint count=U32(data,offset+4);long end=(long)offset+0x64L+count*0x70L;if(count>4096||end>data.Length)throw new InvalidDataException($"Grupo LIT {slot} truncado ou com contagem inválida.");
            var g=new LitGroup{SlotIndex=slot,Offset=offset,Raw=data.AsSpan(offset,0x64).ToArray(),BaseR=data[offset],BaseG=data[offset+1],BaseB=data[offset+2],BaseA=data[offset+3],FogType=U32(data,offset+8),FogStart=F32(data,offset+12),FogEnd=F32(data,offset+16),FogR=data[offset+20],FogG=data[offset+21],FogB=data[offset+22],FogA=data[offset+23],MirrorFogType=U32(data,offset+24),MirrorFogStart=F32(data,offset+28),MirrorFogEnd=F32(data,offset+32),MirrorFogR=data[offset+36],MirrorFogG=data[offset+37],MirrorFogB=data[offset+38],MirrorFogA=data[offset+39],FocalDistance=I32(data,offset+40),FocusFlag=data[offset+44],FocusLevel=data[offset+45],FocusMode=data[offset+46],BlurRate=data[offset+47],TuningFlags=U32(data,offset+48),TuningSmdR=data[offset+52],TuningSmdG=data[offset+53],TuningSmdB=data[offset+54],TuningSmdA=data[offset+55],TuningEnemyR=data[offset+56],TuningEnemyG=data[offset+57],TuningEnemyB=data[offset+58],TuningEnemyA=data[offset+59],TuningPlayerR=data[offset+60],TuningPlayerG=data[offset+61],TuningPlayerB=data[offset+62],TuningPlayerA=data[offset+63],SmdMultiplier=data[offset+64],PlayerMultiplier=data[offset+65],TevScale2=data[offset+66],TevScale3=data[offset+67],DistanceCulling=F32(data,offset+68),Hokan=F32(data,offset+72),WindDirection=data[offset+76],WindPower=data[offset+77],WindFrequency=data[offset+78],WindUnknown=data[offset+79],BlurType=data[offset+80],BlurPower=data[offset+81],MipmapMin=data[offset+82],MipmapMax=data[offset+83],FilteringFormat=data[offset+84],ContrastLevel=data[offset+85],ContrastPower=data[offset+86],ContrastBias=data[offset+87],LodBias=F32(data,offset+88),ExtraAmbient1R=data[offset+92],ExtraAmbient1G=data[offset+93],ExtraAmbient1B=data[offset+94],ExtraAmbient1A=data[offset+95],ExtraAmbient2R=data[offset+96],ExtraAmbient2G=data[offset+97],ExtraAmbient2B=data[offset+98],ExtraAmbient2A=data[offset+99]};
            for(int i=0;i<count;i++){int p=offset+0x64+i*0x70;g.Lights.Add(new LitLight{Index=i,Offset=p,Raw=data.AsSpan(p,0x70).ToArray(),State=data[p],Type=data[p+1],Attribute=data[p+2],Mask=data[p+3],Range=F32(data,p+4),ColorR=data[p+8],ColorG=data[p+9],ColorB=data[p+10],ColorA=data[p+11],Intensity=F32(data,p+12),PositionX=F32(data,p+16),PositionY=F32(data,p+20),PositionZ=F32(data,p+24),ParentType=data[p+28],Kind=data[p+29],RuntimeAttribute=data[p+30],Priority=data[p+31],ParentNumber=U32(data,p+32),HitRadius=U16(data,p+36),Unknown26=U16(data,p+38),Unknown28=U32(data,p+40),DirectionX=F32(data,p+44),DirectionY=F32(data,p+48),DirectionZ=F32(data,p+52),A0=F32(data,p+56),A1=F32(data,p+60),A2=F32(data,p+64),K0=F32(data,p+68),K1=F32(data,p+72),K2=F32(data,p+76),Work0=U32(data,p+80),Work1=U32(data,p+84),Work2=U32(data,p+88),Work3=U32(data,p+92),Work4=U32(data,p+96),Work5=U32(data,p+100),Work6=U32(data,p+104),Work7=U32(data,p+108)});}
            scene.Groups.Add(g);
        }
        return scene;
    }
    private static ushort U16(byte[]d,int p)=>BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p,2));
    private static uint U32(byte[]d,int p)=>BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p,4));
    private static int I32(byte[]d,int p)=>BinaryPrimitives.ReadInt32LittleEndian(d.AsSpan(p,4));
    private static float F32(byte[]d,int p)=>BitConverter.Int32BitsToSingle(I32(d,p));
}

public static class Ps2LitWriter
{
    public static void Write(LitScene scene,string path)
    {
        byte[] data=(byte[])scene.OriginalBytes.Clone();
        foreach(LitGroup g in scene.Groups)
        {
            int p=g.Offset;data[p]=g.BaseR;data[p+1]=g.BaseG;data[p+2]=g.BaseB;data[p+3]=g.BaseA;Put(data,p+8,g.FogType);Put(data,p+12,g.FogStart);Put(data,p+16,g.FogEnd);data[p+20]=g.FogR;data[p+21]=g.FogG;data[p+22]=g.FogB;data[p+23]=g.FogA;Put(data,p+24,g.MirrorFogType);Put(data,p+28,g.MirrorFogStart);Put(data,p+32,g.MirrorFogEnd);data[p+36]=g.MirrorFogR;data[p+37]=g.MirrorFogG;data[p+38]=g.MirrorFogB;data[p+39]=g.MirrorFogA;Put(data,p+40,g.FocalDistance);data[p+44]=g.FocusFlag;data[p+45]=g.FocusLevel;data[p+46]=g.FocusMode;data[p+47]=g.BlurRate;Put(data,p+48,g.TuningFlags);data[p+52]=g.TuningSmdR;data[p+53]=g.TuningSmdG;data[p+54]=g.TuningSmdB;data[p+55]=g.TuningSmdA;data[p+56]=g.TuningEnemyR;data[p+57]=g.TuningEnemyG;data[p+58]=g.TuningEnemyB;data[p+59]=g.TuningEnemyA;data[p+60]=g.TuningPlayerR;data[p+61]=g.TuningPlayerG;data[p+62]=g.TuningPlayerB;data[p+63]=g.TuningPlayerA;data[p+64]=g.SmdMultiplier;data[p+65]=g.PlayerMultiplier;data[p+66]=g.TevScale2;data[p+67]=g.TevScale3;Put(data,p+68,g.DistanceCulling);Put(data,p+72,g.Hokan);data[p+76]=g.WindDirection;data[p+77]=g.WindPower;data[p+78]=g.WindFrequency;data[p+79]=g.WindUnknown;data[p+80]=g.BlurType;data[p+81]=g.BlurPower;data[p+82]=g.MipmapMin;data[p+83]=g.MipmapMax;data[p+84]=g.FilteringFormat;data[p+85]=g.ContrastLevel;data[p+86]=g.ContrastPower;data[p+87]=g.ContrastBias;Put(data,p+88,g.LodBias);data[p+92]=g.ExtraAmbient1R;data[p+93]=g.ExtraAmbient1G;data[p+94]=g.ExtraAmbient1B;data[p+95]=g.ExtraAmbient1A;data[p+96]=g.ExtraAmbient2R;data[p+97]=g.ExtraAmbient2G;data[p+98]=g.ExtraAmbient2B;data[p+99]=g.ExtraAmbient2A;
            foreach(LitLight l in g.Lights){p=l.Offset;data[p]=l.State;data[p+1]=l.Type;data[p+2]=l.Attribute;data[p+3]=l.Mask;Put(data,p+4,l.Range);data[p+8]=l.ColorR;data[p+9]=l.ColorG;data[p+10]=l.ColorB;data[p+11]=l.ColorA;Put(data,p+12,l.Intensity);Put(data,p+16,l.PositionX);Put(data,p+20,l.PositionY);Put(data,p+24,l.PositionZ);data[p+28]=l.ParentType;data[p+29]=l.Kind;data[p+30]=l.RuntimeAttribute;data[p+31]=l.Priority;Put(data,p+32,l.ParentNumber);Put(data,p+36,l.HitRadius);Put(data,p+38,l.Unknown26);Put(data,p+40,l.Unknown28);Put(data,p+44,l.DirectionX);Put(data,p+48,l.DirectionY);Put(data,p+52,l.DirectionZ);Put(data,p+56,l.A0);Put(data,p+60,l.A1);Put(data,p+64,l.A2);Put(data,p+68,l.K0);Put(data,p+72,l.K1);Put(data,p+76,l.K2);Put(data,p+80,l.Work0);Put(data,p+84,l.Work1);Put(data,p+88,l.Work2);Put(data,p+92,l.Work3);Put(data,p+96,l.Work4);Put(data,p+100,l.Work5);Put(data,p+104,l.Work6);Put(data,p+108,l.Work7);}
        }
        File.WriteAllBytes(path,data);scene.IsModified=false;
    }
    private static void Put(byte[]d,int p,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(d.AsSpan(p,2),v);
    private static void Put(byte[]d,int p,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(p,4),v);
    private static void Put(byte[]d,int p,int v)=>BinaryPrimitives.WriteInt32LittleEndian(d.AsSpan(p,4),v);
    private static void Put(byte[]d,int p,float v)=>Put(d,p,BitConverter.SingleToInt32Bits(v));
}
