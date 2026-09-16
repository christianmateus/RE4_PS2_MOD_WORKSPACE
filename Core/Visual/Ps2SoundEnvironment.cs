using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public abstract class Ps2SoundScene
{
    public string SourcePath { get; init; } = string.Empty;
    public byte[] Header { get; set; } = new byte[16];
    public byte[] TrailingData { get; set; } = Array.Empty<byte>();
    public bool IsPlaceholder { get; set; }
    public bool IsModified { get; set; }
}

public sealed class EseScene : Ps2SoundScene { public List<EseEntry> Entries { get; } = new(); }
public sealed class FseScene : Ps2SoundScene { public List<FseEntry> Entries { get; } = new(); }
public sealed class DseScene
{
    public string SourcePath { get; init; } = string.Empty;
    public List<DseEntry> Entries { get; } = new();
    public byte[] TrailingData { get; set; } = Array.Empty<byte>();
    public bool IsBigEndian { get; set; }
    public bool IsModified { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class DseEntry
{
    internal byte[] Raw { get; set; } = new byte[Ps2DseReader.EntrySize];
    [Browsable(false)] public int FileOrder { get; set; }
    [Category("Identity"), DisplayName("Entry"), ReadOnly(true)] public int EntryNumber => FileOrder;
    [Category("Door"), DisplayName("Destination room"), Description("Destination room ID. Example: 0x0101 is r101.")] public ushort DestinationRoom { get; set; }
    [Category("Sound"), DisplayName("DoorSE ID"), Description("Door effect index for this transition. Example: 0 selects door000.")] public ushort DoorSoundId { get; set; }
    [Category("Unknown"), DisplayName("Unknown 0x04")] public ushort Unknown04 { get; set; } = ushort.MaxValue;
    [Category("Unknown"), DisplayName("Unknown 0x06")] public ushort Unknown06 { get; set; } = ushort.MaxValue;
    [Category("Unknown"), DisplayName("Unknown 0x08")] public ushort Unknown08 { get; set; } = ushort.MaxValue;
    [Category("Unknown"), DisplayName("Unknown 0x0A")] public ushort Unknown0A { get; set; } = ushort.MaxValue;
    [Category("Door"), DisplayName("Formatted room"), Description("Sala em hexadecimal no formato rXXX. Aceita, por exemplo, r102 ou 102.")]
    public string DestinationRoomName
    {
        get=>$"r{DestinationRoom:X3}";
        set
        {
            string text=(value??string.Empty).Trim();if(text.StartsWith("r",StringComparison.OrdinalIgnoreCase))text=text[1..];
            if(text.StartsWith("0x",StringComparison.OrdinalIgnoreCase))text=text[2..];
            if(text.Length==0||!ushort.TryParse(text,System.Globalization.NumberStyles.AllowHexSpecifier,System.Globalization.CultureInfo.InvariantCulture,out ushort room))throw new FormatException("Use uma sala hexadecimal válida, por exemplo r102.");
            DestinationRoom=room;
        }
    }
    public DseEntry Clone()=>new(){Raw=(byte[])Raw.Clone(),DestinationRoom=DestinationRoom,DoorSoundId=DoorSoundId,Unknown04=Unknown04,Unknown06=Unknown06,Unknown08=Unknown08,Unknown0A=Unknown0A};
    public override string ToString()=>$"#{FileOrder:D3} - r{DestinationRoom:X3} - door{DoorSoundId:D3} (0x{DoorSoundId:X4})";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class EseEntry
{
    internal byte[] Raw { get; set; } = new byte[Ps2EseReader.EntrySize];
    [Browsable(false)] public int FileOrder { get; set; }
    [Category("Identity"), DisplayName("Entry"), ReadOnly(true)] public int EntryNumber => FileOrder;
    [Category("Identity"), DisplayName("ID")] public byte Id { get; set; } = 3;
    [Category("Identity"), DisplayName("Index")] public byte Index { get; set; }
    [Category("Identity"), DisplayName("Flags")] public byte Flags { get; set; }
    [Category("Position")] public float PositionX { get; set; }
    [Category("Position")] public float PositionY { get; set; }
    [Category("Position")] public float PositionZ { get; set; }
    [Category("Sound"), DisplayName("Type / Cue")] public uint SoundType { get; set; }
    [Category("Playback"), DisplayName("Parameter 1 (0x1C)")] public uint Parameter1 { get; set; }
    [Category("Playback"), DisplayName("Parameter 2 (0x20)")] public uint Parameter2 { get; set; }
    [Category("Playback"), DisplayName("Parameter 3A (0x24)")] public ushort Parameter3A { get; set; }
    [Category("Playback"), DisplayName("Parameter 3B (0x26)")] public ushort Parameter3B { get; set; }
    [Category("Playback"), DisplayName("Parameter 4 (0x28)")] public uint Parameter4 { get; set; }
    [Category("Playback"), DisplayName("Parameter 5 (0x2C)")] public uint Parameter5 { get; set; }
    [Browsable(false)] public Vector3 Position => new(PositionX,PositionY,PositionZ);
    public EseEntry Clone()=>new(){Raw=(byte[])Raw.Clone(),Id=Id,Index=Index,Flags=Flags,PositionX=PositionX,PositionY=PositionY,PositionZ=PositionZ,SoundType=SoundType,Parameter1=Parameter1,Parameter2=Parameter2,Parameter3A=Parameter3A,Parameter3B=Parameter3B,Parameter4=Parameter4,Parameter5=Parameter5};
    public override string ToString()=>$"#{FileOrder:D3} • Type 0x{SoundType:X2} • Index {Index}";
}

public enum FseZoneShape : byte { Quadrilateral=1, Circle=2 }

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class FseEntry
{
    internal byte[] Raw { get; set; } = new byte[Ps2FseReader.EntrySize];
    [Browsable(false)] public int FileOrder { get; set; }
    [Category("Identity"), DisplayName("Entry"), ReadOnly(true)] public int EntryNumber=>FileOrder;
    [Category("Identity"), DisplayName("ID / Type")] public ushort Id { get; set; }=3;
    [Category("Identity")] public ushort Index { get; set; }
    [Category("Identity"), DisplayName("Parameter (0x04)")] public uint IdentityParameter { get; set; }=8;
    [Category("Zone")] public byte State { get; set; }=1;
    [Category("Zone"), DisplayName("Shape")] public FseZoneShape Shape { get; set; }=FseZoneShape.Quadrilateral;
    [Category("Zone"), DisplayName("Zone flags")] public ushort ZoneFlags { get; set; }
    [Category("Zone"), DisplayName("Base Y")] public float BaseY { get; set; }
    [Category("Zone"), DisplayName("Height / Top Y")] public float Height { get; set; }=1000f;
    [Category("Zone"), DisplayName("Circle radius")] public float Radius { get; set; }=750f;
    [Category("Corners")] public float Corner0X { get; set; }=750f;
    [Category("Corners")] public float Corner0Z { get; set; }=750f;
    [Category("Corners")] public float Corner1X { get; set; }=-750f;
    [Category("Corners")] public float Corner1Z { get; set; }=750f;
    [Category("Corners")] public float Corner2X { get; set; }=-750f;
    [Category("Corners")] public float Corner2Z { get; set; }=-750f;
    [Category("Corners")] public float Corner3X { get; set; }=750f;
    [Category("Corners")] public float Corner3Z { get; set; }=-750f;
    [Category("Sound"), DisplayName("Selector 1 (0x44)")] public byte Selector1 { get; set; }
    [Category("Sound"), DisplayName("Selector 2 (0x45)")] public byte Selector2 { get; set; }
    [Category("Sound"), DisplayName("Selector 3 (0x46)")] public byte Selector3 { get; set; }
    [Category("Sound"), DisplayName("Selector 4 (0x47)")] public byte Selector4 { get; set; }=7;
    [Category("Playback"), DisplayName("Parameter 1 (0x48)")] public uint Parameter1 { get; set; }
    [Category("Playback"), DisplayName("Parameter 2 (0x4C)")] public uint Parameter2 { get; set; }
    [Category("Playback"), DisplayName("Parameter 3 (0x50)")] public uint Parameter3 { get; set; }
    [Category("Playback"), DisplayName("Parameter 4 (0x54)")] public uint Parameter4 { get; set; }
    public FseEntry Clone()=>new(){Raw=(byte[])Raw.Clone(),Id=Id,Index=Index,IdentityParameter=IdentityParameter,State=State,Shape=Shape,ZoneFlags=ZoneFlags,BaseY=BaseY,Height=Height,Radius=Radius,Corner0X=Corner0X,Corner0Z=Corner0Z,Corner1X=Corner1X,Corner1Z=Corner1Z,Corner2X=Corner2X,Corner2Z=Corner2Z,Corner3X=Corner3X,Corner3Z=Corner3Z,Selector1=Selector1,Selector2=Selector2,Selector3=Selector3,Selector4=Selector4,Parameter1=Parameter1,Parameter2=Parameter2,Parameter3=Parameter3,Parameter4=Parameter4};
    public override string ToString()=>$"#{FileOrder:D3} • ID 0x{Id:X4} • Index 0x{Index:X2} • {Shape}";
}

public static class Ps2EseReader
{
    public const int HeaderSize=16,EntrySize=48;
    public static EseScene Read(string path)
    {
        byte[] d=ReadFile(path,"ESE");var s=new EseScene{SourcePath=path,Header=d[..HeaderSize],IsPlaceholder=IsPlaceholder(d)};
        if(s.IsPlaceholder){s.TrailingData=d[HeaderSize..];return s;}ValidateMagic(d,"ESE");int count=BitConverter.ToUInt16(d,6);ValidateLength(d,count,EntrySize,"ESE");
        for(int i=0;i<count;i++){int p=HeaderSize+i*EntrySize;byte[] r=d.AsSpan(p,EntrySize).ToArray();s.Entries.Add(new EseEntry{Raw=r,FileOrder=i,PositionX=F(r,0),PositionY=F(r,4),PositionZ=F(r,8),Id=r[0x10],Index=r[0x11],Flags=r[0x12],SoundType=U32(r,0x18),Parameter1=U32(r,0x1C),Parameter2=U32(r,0x20),Parameter3A=U16(r,0x24),Parameter3B=U16(r,0x26),Parameter4=U32(r,0x28),Parameter5=U32(r,0x2C)});}
        s.TrailingData=d[(HeaderSize+count*EntrySize)..];return s;
    }
    internal static byte[] ReadFile(string p,string kind){if(!File.Exists(p))throw new FileNotFoundException($"Arquivo {kind} não encontrado.",p);byte[] d=File.ReadAllBytes(p);if(d.Length<HeaderSize)throw new InvalidDataException($"{kind} muito pequeno.");return d;}
    internal static bool IsPlaceholder(byte[] d)=>d.Length==32&&d.AsSpan(0,16).IndexOfAnyExcept((byte)0)<0&&d.AsSpan(16,16).IndexOfAnyExcept((byte)0xCD)<0;
    internal static void ValidateMagic(byte[] d,string m){if(d[0]!=m[0]||d[1]!=m[1]||d[2]!=m[2]||d[3]!=0)throw new InvalidDataException($"Assinatura {m} inválida.");}
    internal static void ValidateLength(byte[] d,int c,int z,string k){long n=HeaderSize+(long)c*z;if(n>d.Length)throw new InvalidDataException($"{k} truncado: {c} entradas requerem {n} bytes.");}
    internal static float F(byte[] d,int o)=>BitConverter.ToSingle(d,o);internal static ushort U16(byte[] d,int o)=>BitConverter.ToUInt16(d,o);internal static uint U32(byte[] d,int o)=>BitConverter.ToUInt32(d,o);
}

public static class Ps2FseReader
{
    public const int HeaderSize=16,EntrySize=132;
    public static FseScene Read(string path)
    {
        byte[] d=Ps2EseReader.ReadFile(path,"FSE");var s=new FseScene{SourcePath=path,Header=d[..HeaderSize],IsPlaceholder=Ps2EseReader.IsPlaceholder(d)};
        if(s.IsPlaceholder){s.TrailingData=d[HeaderSize..];return s;}Ps2EseReader.ValidateMagic(d,"FSE");int count=BitConverter.ToUInt16(d,6);Ps2EseReader.ValidateLength(d,count,EntrySize,"FSE");
        for(int i=0;i<count;i++){int p=HeaderSize+i*EntrySize;byte[] r=d.AsSpan(p,EntrySize).ToArray();s.Entries.Add(new FseEntry{Raw=r,FileOrder=i,Id=Ps2EseReader.U16(r,0),Index=Ps2EseReader.U16(r,2),IdentityParameter=Ps2EseReader.U32(r,4),State=r[0x14],Shape=(FseZoneShape)r[0x15],ZoneFlags=Ps2EseReader.U16(r,0x16),BaseY=Ps2EseReader.F(r,0x18),Height=Ps2EseReader.F(r,0x1C),Radius=Ps2EseReader.F(r,0x20),Corner0X=Ps2EseReader.F(r,0x24),Corner0Z=Ps2EseReader.F(r,0x28),Corner1X=Ps2EseReader.F(r,0x2C),Corner1Z=Ps2EseReader.F(r,0x30),Corner2X=Ps2EseReader.F(r,0x34),Corner2Z=Ps2EseReader.F(r,0x38),Corner3X=Ps2EseReader.F(r,0x3C),Corner3Z=Ps2EseReader.F(r,0x40),Selector1=r[0x44],Selector2=r[0x45],Selector3=r[0x46],Selector4=r[0x47],Parameter1=Ps2EseReader.U32(r,0x48),Parameter2=Ps2EseReader.U32(r,0x4C),Parameter3=Ps2EseReader.U32(r,0x50),Parameter4=Ps2EseReader.U32(r,0x54)});}
        s.TrailingData=d[(HeaderSize+count*EntrySize)..];return s;
    }
}

public static class Ps2DseReader
{
    public const int HeaderSize=4,EntrySize=12;
    public static DseScene Read(string path)
    {
        if(!File.Exists(path))throw new FileNotFoundException("Arquivo DSE não encontrado.",path);
        byte[] d=File.ReadAllBytes(path);if(d.Length<HeaderSize)throw new InvalidDataException("DSE muito pequeno.");
        uint little=BitConverter.ToUInt32(d,0),big=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(d);bool bigEndian=HeaderSize+(long)little*EntrySize>d.Length&&HeaderSize+(long)big*EntrySize<=d.Length;uint count=bigEndian?big:little;long used=HeaderSize+(long)count*EntrySize;
        if(used>d.Length)throw new InvalidDataException($"DSE truncado: a contagem não é válida em little-endian ({little}) nem big-endian ({big}) para {d.Length} bytes.");
        if(count>1000000)throw new InvalidDataException($"DSE contém uma quantidade inválida de entradas: {count}.");
        var s=new DseScene{SourcePath=path,IsBigEndian=bigEndian};ushort U(byte[] r,int o)=>bigEndian?System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(r.AsSpan(o,2)):Ps2EseReader.U16(r,o);
        for(int i=0;i<(int)count;i++){int p=HeaderSize+i*EntrySize;byte[] r=d.AsSpan(p,EntrySize).ToArray();s.Entries.Add(new DseEntry{Raw=r,FileOrder=i,DestinationRoom=U(r,0),DoorSoundId=U(r,2),Unknown04=U(r,4),Unknown06=U(r,6),Unknown08=U(r,8),Unknown0A=U(r,10)});}
        s.TrailingData=d[(int)used..];return s;
    }
}

public static class Ps2SoundWriter
{
    public static bool Save(EseScene s,string backup)=>SaveCore(s,s.Entries.Count,Ps2EseReader.EntrySize,"ESE",backup,(d,p,i)=>WriteEse(d,p,s.Entries[i]));
    public static bool Save(FseScene s,string backup)=>SaveCore(s,s.Entries.Count,Ps2FseReader.EntrySize,"FSE",backup,(d,p,i)=>WriteFse(d,p,s.Entries[i]));
    public static bool Save(DseScene s,string backup)
    {
        bool made=false;Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup)){File.Copy(s.SourcePath,backup);made=true;}
        int used=checked(Ps2DseReader.HeaderSize+s.Entries.Count*Ps2DseReader.EntrySize);bool cdPadding=s.TrailingData.Length>0&&s.TrailingData.All(x=>x==0xCD);byte[] trailing=cdPadding?Enumerable.Repeat((byte)0xCD,((used+31)&~31)-used).ToArray():s.TrailingData;byte[] d=new byte[checked(used+trailing.Length)];if(s.IsBigEndian)System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(d,(uint)s.Entries.Count);else Put(d,0,(uint)s.Entries.Count);
        void U16(int p,ushort v){if(s.IsBigEndian)System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(d.AsSpan(p,2),v);else Put(d,p,v);}for(int i=0;i<s.Entries.Count;i++){int p=Ps2DseReader.HeaderSize+i*Ps2DseReader.EntrySize;DseEntry e=s.Entries[i];Copy(e.Raw,d,p,Ps2DseReader.EntrySize);U16(p,e.DestinationRoom);U16(p+2,e.DoorSoundId);U16(p+4,e.Unknown04);U16(p+6,e.Unknown06);U16(p+8,e.Unknown08);U16(p+10,e.Unknown0A);e.FileOrder=i;}
        trailing.CopyTo(d,used);string temp=s.SourcePath+".tmp";
        try{File.WriteAllBytes(temp,d);DseScene verified=Ps2DseReader.Read(temp);if(verified.Entries.Count!=s.Entries.Count)throw new InvalidDataException($"Validação DSE falhou: esperadas {s.Entries.Count} entradas, encontradas {verified.Entries.Count}.");File.Move(temp,s.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}s.TrailingData=trailing;s.IsModified=false;return made;
    }
    private static bool SaveCore(Ps2SoundScene s,int count,int size,string magic,string backup,Action<byte[],int,int> write)
    {
        if(count>ushort.MaxValue)throw new InvalidDataException($"{magic} excede 65535 entradas.");bool made=false;Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup)){File.Copy(s.SourcePath,backup);made=true;}
        if(s.IsPlaceholder&&count==0){s.IsModified=false;return made;}
        int used=16+count*size,total=(used+31)&~31;byte[] d=new byte[total];Array.Fill(d,(byte)0xCD,used,total-used);byte[] h=s.Header.Length>=16?s.Header:new byte[16];h.AsSpan(0,16).CopyTo(d);d[0]=(byte)magic[0];d[1]=(byte)magic[1];d[2]=(byte)magic[2];d[3]=0;if(magic=="ESE"){d[4]=0;d[5]=1;}else{d[4]=3;d[5]=1;}Put(d,6,(ushort)count);for(int i=0;i<count;i++)write(d,16+i*size,i);
        string temp=s.SourcePath+".tmp";
        try
        {
            File.WriteAllBytes(temp,d);
            int verified=magic=="ESE"?Ps2EseReader.Read(temp).Entries.Count:Ps2FseReader.Read(temp).Entries.Count;
            if(verified!=count)throw new InvalidDataException($"Validação {magic} falhou: esperadas {count} entradas, encontradas {verified}.");
            File.Move(temp,s.SourcePath,true);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
        s.Header=d[..16];s.TrailingData=d[used..];s.IsPlaceholder=false;s.IsModified=false;return made;
    }
    private static void WriteEse(byte[] d,int p,EseEntry e){Copy(e.Raw,d,p,48);Put(d,p,e.PositionX);Put(d,p+4,e.PositionY);Put(d,p+8,e.PositionZ);d[p+0x0C]=0x3F;d[p+0x0D]=0x80;d[p+0x0E]=d[p+0x0F]=0;d[p+0x10]=e.Id;d[p+0x11]=e.Index;d[p+0x12]=e.Flags;Put(d,p+0x18,e.SoundType);Put(d,p+0x1C,e.Parameter1);Put(d,p+0x20,e.Parameter2);Put(d,p+0x24,e.Parameter3A);Put(d,p+0x26,e.Parameter3B);Put(d,p+0x28,e.Parameter4);Put(d,p+0x2C,e.Parameter5);e.FileOrder=(p-16)/48;}
    private static void WriteFse(byte[] d,int p,FseEntry e){Copy(e.Raw,d,p,132);Put(d,p,e.Id);Put(d,p+2,e.Index);Put(d,p+4,e.IdentityParameter);Put(d,p+8,0u);Put(d,p+12,0u);Put(d,p+16,0u);d[p+0x14]=e.State;d[p+0x15]=(byte)e.Shape;Put(d,p+0x16,e.ZoneFlags);float[] f={e.BaseY,e.Height,e.Radius,e.Corner0X,e.Corner0Z,e.Corner1X,e.Corner1Z,e.Corner2X,e.Corner2Z,e.Corner3X,e.Corner3Z};for(int i=0;i<f.Length;i++)Put(d,p+0x18+i*4,f[i]);d[p+0x44]=e.Selector1;d[p+0x45]=e.Selector2;d[p+0x46]=e.Selector3;d[p+0x47]=e.Selector4;Put(d,p+0x48,e.Parameter1);Put(d,p+0x4C,e.Parameter2);Put(d,p+0x50,e.Parameter3);Put(d,p+0x54,e.Parameter4);e.FileOrder=(p-16)/132;}
    private static void Copy(byte[] s,byte[] d,int p,int n)=>s.AsSpan(0,Math.Min(s.Length,n)).CopyTo(d.AsSpan(p,n));private static void Put(byte[] d,int p,float v)=>BitConverter.GetBytes(v).CopyTo(d,p);private static void Put(byte[] d,int p,uint v)=>BitConverter.GetBytes(v).CopyTo(d,p);private static void Put(byte[] d,int p,ushort v)=>BitConverter.GetBytes(v).CopyTo(d,p);
}
