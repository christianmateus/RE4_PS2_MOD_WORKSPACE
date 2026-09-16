using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class ItaScene
{
    public string SourcePath { get; init; } = string.Empty;
    public byte[] Header { get; init; } = new byte[Ps2ItaReader.HeaderSize];
    public bool HasFinalPadding { get; init; }
    public List<ItaEntry> Entries { get; } = new();
    public bool IsModified { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class ItaEntry
{
    internal byte[] RawData { get; set; } = new byte[Ps2ItaReader.EntrySize];
    [Browsable(false)] public int FileOrder { get; set; }

    [Category("Identity"), DisplayName("Data Block Index"), Description("Persistent ITA entry index. Gaps can be intentional and are preserved.")]
    public byte DataBlockIndex { get; set; }
    [Category("Identity"), DisplayName("Category (raw)")]
    public byte Category { get; set; }
    [Category("Identity"), DisplayName("Data Block Type (raw)")]
    public byte DataBlockType { get; set; }

    [Category("Item"), DisplayName("Item"), TypeConverter(typeof(ItaItemConverter)), Description("Item spawned by this entry.")]
    public byte ItemId { get; set; }
    [Browsable(false)]
    public string ItemName => ItaItemCatalog.GetName(ItemId);
    [Category("Item"), DisplayName("Amount")]
    public ushort Amount { get; set; }
    [Category("Item"), DisplayName("Random"), TypeConverter(typeof(ItaRandomConverter)), Description("No = 0x00; Yes = 0x10. A few original ITAs use the special value 0x01, which is preserved.")]
    public byte Randomness { get; set; }
    [Category("Item"), DisplayName("Aura / Glow"), TypeConverter(typeof(ItaAuraConverter)), Description("Visual glow effect displayed around the item.")]
    public byte AuraType { get; set; }

    [Category("Spawn"), DisplayName("Appearance Type"), TypeConverter(typeof(ItaAppearanceConverter)), Description("Normal = placed in room; Enemy = linked to an ELS enemy; Inside ETS = linked to an ETS object.")]
    public byte AppearanceType { get; set; }
    [Category("Spawn"), DisplayName("ETS / ELS ID"), Description("ETS instance for type 2, or ELS enemy index for type 1.")]
    public byte LinkedInstanceId { get; set; }
    [Category("Spawn"), DisplayName("Script Link")]
    public ushort ScriptLink { get; set; }
    [Category("Spawn"), DisplayName("Position Source"), TypeConverter(typeof(ItaPositionSourceConverter)), Description("Parent Position uses the linked Enemy/ETS position. Own Position uses this ITA entry's coordinates. Changes only bit 0 and preserves all other flags.")]
    public byte PositionSource
    {
        get => (byte)(SpawnFlags&1u);
        set => SpawnFlags=value==1?SpawnFlags|1u:SpawnFlags&~1u;
    }
    [Category("Advanced"), DisplayName("Flags 0x80 (raw)"), Description("Complete raw flags. Values other than bit 0 occur in original game files and are preserved.")]
    public uint SpawnFlags { get; set; }
    [Category("Spawn"), DisplayName("Flags 0x84 (raw)")]
    public uint ExtraFlags { get; set; }

    [Category("Position"), DisplayName("X")] public float PositionX { get; set; }
    [Category("Position"), DisplayName("Y")] public float PositionY { get; set; }
    [Category("Position"), DisplayName("Z")] public float PositionZ { get; set; }
    [Category("Rotation (radians)"), DisplayName("X")] public float RotationX { get; set; }
    [Category("Rotation (radians)"), DisplayName("Y")] public float RotationY { get; set; }
    [Category("Rotation (radians)"), DisplayName("Z")] public float RotationZ { get; set; }
    [Category("Pickup"), DisplayName("Radius")] public float PickupRadius { get; set; }

    [Category("Trigger bounds"), DisplayName("Higher limit")] public float HigherLimit { get; set; }
    [Category("Trigger bounds"), DisplayName("Lower limit")] public float LowerLimit { get; set; }
    [Category("Trigger bounds"), DisplayName("Unknown float")] public float BoundsUnknown { get; set; }
    [Category("Trigger bounds"), DisplayName("Corner 0 X")] public float Corner0X { get; set; }
    [Category("Trigger bounds"), DisplayName("Corner 0 Z")] public float Corner0Z { get; set; }
    [Category("Trigger bounds"), DisplayName("Corner 1 X")] public float Corner1X { get; set; }
    [Category("Trigger bounds"), DisplayName("Corner 1 Z")] public float Corner1Z { get; set; }
    [Category("Trigger bounds"), DisplayName("Corner 2 X")] public float Corner2X { get; set; }
    [Category("Trigger bounds"), DisplayName("Corner 2 Z")] public float Corner2Z { get; set; }

    [Browsable(false)] public Vector3 Position => new(PositionX, PositionY, PositionZ);
    public override string ToString() => $"#{DataBlockIndex:X2}  {ItaItemCatalog.GetDisplayName(ItemId)}  [{ItaAppearanceName(AppearanceType)}]";

    public ItaEntry Clone() => new()
    {
        RawData=(byte[])RawData.Clone(), FileOrder=FileOrder, DataBlockIndex=DataBlockIndex, Category=Category, DataBlockType=DataBlockType,
        ItemId=ItemId, Amount=Amount, Randomness=Randomness, AuraType=AuraType, AppearanceType=AppearanceType,
        LinkedInstanceId=LinkedInstanceId, ScriptLink=ScriptLink, SpawnFlags=SpawnFlags, ExtraFlags=ExtraFlags,
        PositionX=PositionX, PositionY=PositionY, PositionZ=PositionZ, RotationX=RotationX, RotationY=RotationY, RotationZ=RotationZ,
        PickupRadius=PickupRadius, HigherLimit=HigherLimit, LowerLimit=LowerLimit, BoundsUnknown=BoundsUnknown,
        Corner0X=Corner0X, Corner0Z=Corner0Z, Corner1X=Corner1X, Corner1Z=Corner1Z,
        Corner2X=Corner2X, Corner2Z=Corner2Z
    };

    private static string ItaAppearanceName(byte value) => value switch { 0 => "room", 1 => "enemy", 2 => "container", _ => $"type {value:X2}" };
}

public static class ItaItemCatalog
{
    private static readonly IReadOnlyDictionary<byte,string> Names=Load();
    public static IReadOnlyList<byte> ItemIds { get; }=Names.Keys.OrderBy(x=>x).ToArray();
    public static string GetDisplayName(byte id)=>Names.TryGetValue(id,out string? name)?name:"Unknown Item";
    public static string GetName(byte id)=>Names.TryGetValue(id,out string? name)?$"{name} (0x{id:X2})":$"Item 0x{id:X2}";

    private static IReadOnlyDictionary<byte,string> Load()
    {
        var result=new Dictionary<byte,string>();
        using Stream? stream=typeof(ItaItemCatalog).Assembly.GetManifestResourceStream("ItaItems.txt");
        if(stream==null)return result;
        using var reader=new StreamReader(stream);
        while(reader.ReadLine() is string line)
        {
            int separator=line.IndexOf(" - ",StringComparison.Ordinal);if(separator<=0)continue;
            string key=line[..separator].Trim();string name=line[(separator+3)..].Trim();
            if(key.Length>2||!byte.TryParse(key,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out byte id))continue;
            result[id]=Normalize(name);
        }
        return result;
    }

    private static string Normalize(string name)
    {
        string value=System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());
        return value.Replace("Tmp","TMP",StringComparison.Ordinal)
            .Replace("P.r.l.","P.R.L.",StringComparison.Ordinal)
            .Replace("J.j.","J.J.",StringComparison.Ordinal)
            .Replace("(Aaa)","(AAA)",StringComparison.Ordinal)
            .Replace("Red9","Red9",StringComparison.Ordinal)
            .Replace("Killer7","Killer7",StringComparison.Ordinal);
    }
}

public sealed class ItaItemConverter : TypeConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>false;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(ItaItemCatalog.ItemIds.ToArray());
    public override bool CanConvertTo(ITypeDescriptorContext? context,Type? destinationType)=>destinationType==typeof(string)||base.CanConvertTo(context,destinationType);
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)=>sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        =>destinationType==typeof(string)&&value is byte id?ItaItemCatalog.GetDisplayName(id):base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
    {
        if(value is string text)
        {
            foreach(byte id in ItaItemCatalog.ItemIds)if(string.Equals(ItaItemCatalog.GetDisplayName(id),text,StringComparison.OrdinalIgnoreCase))return id;
            string raw=text.Trim();if(raw.StartsWith("0x",StringComparison.OrdinalIgnoreCase))raw=raw[2..];
            if(byte.TryParse(raw,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out byte parsedId))return parsedId;
        }
        return base.ConvertFrom(context,culture,value)!;
    }
}

public sealed class ItaRandomConverter : TypeConverter
{
    private static readonly byte[] Values={0x00,0x10,0x01};
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>false;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(Values);
    public override bool CanConvertTo(ITypeDescriptorContext? context,Type? destinationType)=>destinationType==typeof(string)||base.CanConvertTo(context,destinationType);
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)=>sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        =>destinationType==typeof(string)&&value is byte raw?raw switch{0x00=>"No",0x10=>"Yes",0x01=>"Special (0x01)",_=>$"Unknown (0x{raw:X2})"}:base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
    {
        if(value is string text)
        {
            string label=text.Trim();
            if(label.Equals("No",StringComparison.OrdinalIgnoreCase)||label.Equals("Não",StringComparison.OrdinalIgnoreCase)||label.Equals("Nao",StringComparison.OrdinalIgnoreCase))return(byte)0x00;
            if(label.Equals("Yes",StringComparison.OrdinalIgnoreCase)||label.Equals("Sim",StringComparison.OrdinalIgnoreCase))return(byte)0x10;
            if(label.Equals("Special (0x01)",StringComparison.OrdinalIgnoreCase)||label.Equals("Especial (0x01)",StringComparison.OrdinalIgnoreCase))return(byte)0x01;
            return ParseByte(text);
        }
        return base.ConvertFrom(context,culture,value);
    }

    private static byte ParseByte(string text)
    {
        string raw=text.Trim();int marker=raw.IndexOf("0x",StringComparison.OrdinalIgnoreCase);
        if(marker>=0){raw=raw[(marker+2)..].TrimEnd(')');if(byte.TryParse(raw,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out byte parsed))return parsed;}
        throw new FormatException($"Invalid ITA random value: {text}");
    }
}

public sealed class ItaAppearanceConverter : TypeConverter
{
    private static readonly byte[] Values={0,1,2};
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(Values);
    public override bool CanConvertTo(ITypeDescriptorContext? context,Type? destinationType)=>destinationType==typeof(string)||base.CanConvertTo(context,destinationType);
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)=>sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        =>destinationType==typeof(string)&&value is byte raw?raw switch{0=>"Normal",1=>"Enemy",2=>"Inside ETS",_=>$"Unknown (0x{raw:X2})"}:base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
        =>value is string text?text.Trim() switch{"Normal"=>(byte)0,"Enemy"=>(byte)1,"Inside ETS"=>(byte)2,_=>throw new FormatException($"Invalid ITA appearance type: {text}")}:base.ConvertFrom(context,culture,value);
}

public sealed class ItaAuraConverter : TypeConverter
{
    private static readonly byte[] Values={0,1,2,3,4,5,6,7,8,9};
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(Values);
    public override bool CanConvertTo(ITypeDescriptorContext? context,Type? destinationType)=>destinationType==typeof(string)||base.CanConvertTo(context,destinationType);
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)=>sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        =>destinationType==typeof(string)&&value is byte raw?Name(raw):base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
    {
        if(value is string text)
        {
            string label=text.Trim();
            for(byte raw=0;raw<Values.Length;raw++)if(label.Equals(Name(raw),StringComparison.OrdinalIgnoreCase))return raw;
            int marker=label.IndexOf("0x",StringComparison.OrdinalIgnoreCase);
            if(marker>=0&&byte.TryParse(label[(marker+2)..].TrimEnd(')'),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out byte unknown))return unknown;
            throw new FormatException($"Invalid ITA aura value: {text}");
        }
        return base.ConvertFrom(context,culture,value);
    }
    private static string Name(byte raw)=>raw switch
    {
        0x00=>"Normal",
        0x01=>"Small glint",
        0x02=>"White (sparkle bottom)",
        0x03=>"Blue",
        0x04=>"Green",
        0x05=>"Red",
        0x06=>"Auto",
        0x07=>"Big white",
        0x08=>"White (sparkle top)",
        0x09=>"Big yellow",
        _=>$"Unknown (0x{raw:X2})"
    };
}

public sealed class ItaPositionSourceConverter : TypeConverter
{
    private static readonly byte[] Values={0,1};
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(Values);
    public override bool CanConvertTo(ITypeDescriptorContext? context,Type? destinationType)=>destinationType==typeof(string)||base.CanConvertTo(context,destinationType);
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)=>sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        =>destinationType==typeof(string)&&value is byte source?source switch
        {
            0=>"Parent Position (Enemy / ETS)",
            1=>"Own Position (ITA)",
            _=>$"Unknown ({source})"
        }:base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
        =>value is string text?text.Trim() switch{"Parent Position (Enemy / ETS)"=>(byte)0,"Own Position (ITA)"=>(byte)1,_=>throw new FormatException($"Invalid ITA position source: {text}")}:base.ConvertFrom(context,culture,value);
}

public static class Ps2ItaReader
{
    public const int HeaderSize=0x20;
    public const int EntrySize=0xB0;

    public static ItaScene Read(string path)
    {
        byte[] data=File.ReadAllBytes(path);
        if(data.Length<HeaderSize)throw new InvalidDataException("ITA is too small.");
        if(data[0]!='I'||data[1]!='T'||data[2]!='A'||data[3]!=0)throw new InvalidDataException("Invalid ITA signature.");
        ushort count=BitConverter.ToUInt16(data,6);
        long fullSize=HeaderSize+(long)count*EntrySize;
        long compactSize=count==0?HeaderSize:fullSize-0x10;
        if(data.Length!=fullSize&&data.Length!=compactSize)throw new InvalidDataException($"ITA size mismatch: header declares {count} entries (expected 0x{compactSize:X} or 0x{fullSize:X}, found 0x{data.Length:X}).");
        var scene=new ItaScene{SourcePath=path,Header=data[..HeaderSize],HasFinalPadding=data.Length==fullSize};
        for(int i=0;i<count;i++)
        {
            int sourceOffset=HeaderSize+i*EntrySize;
            int available=Math.Min(EntrySize,data.Length-sourceOffset);
            byte[] raw=new byte[EntrySize];data.AsSpan(sourceOffset,available).CopyTo(raw);
            scene.Entries.Add(new ItaEntry
            {
                FileOrder=i,RawData=raw,HigherLimit=F(raw,0),LowerLimit=F(raw,4),BoundsUnknown=F(raw,8),
                Corner0X=F(raw,0x0C),Corner0Z=F(raw,0x10),Corner1X=F(raw,0x14),Corner1Z=F(raw,0x18),
                Corner2X=F(raw,0x1C),Corner2Z=F(raw,0x20),
                Category=raw[0x24],DataBlockType=raw[0x25],DataBlockIndex=raw[0x26],
                AppearanceType=raw[0x36],LinkedInstanceId=raw[0x37],PositionX=F(raw,0x50),PositionY=F(raw,0x54),PositionZ=F(raw,0x58),
                ItemId=raw[0x74],Randomness=raw[0x75],Amount=BitConverter.ToUInt16(raw,0x78),ScriptLink=BitConverter.ToUInt16(raw,0x7A),
                AuraType=raw[0x7C],SpawnFlags=BitConverter.ToUInt32(raw,0x80),ExtraFlags=BitConverter.ToUInt32(raw,0x84),
                PickupRadius=F(raw,0x88),RotationX=F(raw,0x8C),RotationY=F(raw,0x90),RotationZ=F(raw,0x94)
            });
        }
        return scene;
    }
    private static float F(byte[] data,int offset)=>BitConverter.ToSingle(data,offset);
}

public static class Ps2ItaWriter
{
    public static bool Save(ItaScene scene,string? backupPath=null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if(scene.Entries.Count>ushort.MaxValue)throw new InvalidDataException("ITA has too many entries.");
        bool backup=false;
        if(File.Exists(scene.SourcePath)&&!string.IsNullOrWhiteSpace(backupPath)&&!File.Exists(backupPath)){File.Copy(scene.SourcePath,backupPath);backup=true;}
        int finalPadding=scene.Entries.Count>0&&!scene.HasFinalPadding?0x10:0;
        byte[] output=new byte[Ps2ItaReader.HeaderSize+scene.Entries.Count*Ps2ItaReader.EntrySize-finalPadding];
        scene.Header.AsSpan(0,Math.Min(scene.Header.Length,Ps2ItaReader.HeaderSize)).CopyTo(output);
        output[0]=(byte)'I';output[1]=(byte)'T';output[2]=(byte)'A';output[3]=0;
        BitConverter.GetBytes((ushort)scene.Entries.Count).CopyTo(output,6);
        for(int i=0;i<scene.Entries.Count;i++)
        {
            ItaEntry e=scene.Entries[i];Validate(e,i);int o=Ps2ItaReader.HeaderSize+i*Ps2ItaReader.EntrySize;
            int writable=Math.Min(Ps2ItaReader.EntrySize,output.Length-o);
            e.RawData.AsSpan(0,Math.Min(e.RawData.Length,writable)).CopyTo(output.AsSpan(o,writable));
            W(output,o,e.HigherLimit,e.LowerLimit,e.BoundsUnknown);W(output,o+0x0C,e.Corner0X,e.Corner0Z,e.Corner1X,e.Corner1Z,e.Corner2X,e.Corner2Z);
            output[o+0x24]=e.Category;output[o+0x25]=e.DataBlockType;output[o+0x26]=e.DataBlockIndex;output[o+0x36]=e.AppearanceType;output[o+0x37]=e.LinkedInstanceId;
            W(output,o+0x50,e.PositionX,e.PositionY,e.PositionZ);output[o+0x74]=e.ItemId;output[o+0x75]=e.Randomness;
            BitConverter.GetBytes(e.Amount).CopyTo(output,o+0x78);BitConverter.GetBytes(e.ScriptLink).CopyTo(output,o+0x7A);output[o+0x7C]=e.AuraType;
            BitConverter.GetBytes(e.SpawnFlags).CopyTo(output,o+0x80);BitConverter.GetBytes(e.ExtraFlags).CopyTo(output,o+0x84);
            W(output,o+0x88,e.PickupRadius,e.RotationX,e.RotationY,e.RotationZ);e.FileOrder=i;
        }
        File.WriteAllBytes(scene.SourcePath,output);scene.IsModified=false;return backup;
    }
    private static void W(byte[] data,int offset,params float[] values){foreach(float value in values){BitConverter.GetBytes(value).CopyTo(data,offset);offset+=4;}}
    private static void Validate(ItaEntry e,int index)
    {
        float[] values={e.HigherLimit,e.LowerLimit,e.BoundsUnknown,e.Corner0X,e.Corner0Z,e.Corner1X,e.Corner1Z,e.Corner2X,e.Corner2Z,e.PositionX,e.PositionY,e.PositionZ,e.PickupRadius,e.RotationX,e.RotationY,e.RotationZ};
        if(values.Any(v=>!float.IsFinite(v)))throw new InvalidDataException($"ITA entry {index} contains an invalid floating-point value.");
    }
}
