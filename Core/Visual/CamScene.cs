using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class CamScene
{
    [Browsable(false)] public string SourcePath { get; init; } = string.Empty;
    [Browsable(false)] public byte[] OriginalData { get; set; } = Array.Empty<byte>();
    [Browsable(false)] public byte[] Header { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public List<CamEntry> Entries { get; init; } = new();
    [Browsable(false)] public List<CamCameraRecord> CameraRecords { get; init; } = new();
    [Browsable(false)] public bool IsModified { get; set; }
    [Browsable(false)] public bool StructureModified { get; set; }
    [Browsable(false)] public int OriginalEntryCount { get; init; }
    [Browsable(false)] public int OriginalCameraCount { get; init; }
    [Browsable(false)] public byte[] PreVertexData { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public byte[] TailPadding { get; init; } = Array.Empty<byte>();
    [Category("File"), DisplayName("Format")] public string Format => Header.Length >= 4 ? System.Text.Encoding.ASCII.GetString(Header, 0, 4) : "CAM";
    [Category("File"), DisplayName("Entries")] public int Count => Entries.Count;
    [Category("File"), DisplayName("Cameras")] public int CameraCount => CameraRecords.Count;
}

public sealed class CamEntry
{
    [Browsable(false)] public int FileOrder { get; set; }
    [Browsable(false)] public int EntryOffset { get; init; }
    [Browsable(false)] public int AreaOffset { get; init; }
    [Browsable(false)] public int CameraOffset => Camera?.FileOffset ?? 0;
    [Browsable(false)] public byte[] EntryRaw { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public byte[] AreaRaw { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public CamCameraRecord? Camera { get; set; }
    [Browsable(false)] public List<CamVertex> Vertices { get; init; } = new();
    [Browsable(false)] public List<CamFrame> Frames => Camera?.Frames ?? EmptyFrames;
    private static readonly List<CamFrame> EmptyFrames = new();

    [Category("Area"), DisplayName("Enabled")] public byte AreaEnabled { get; set; }
    [Category("Area"), DisplayName("Index")] public byte Index { get; set; }
    [Category("Area"), DisplayName("Sub-index")] public byte SubIndex { get; set; }
    [Category("Area"), DisplayName("Attributes")] public CamAreaAttributes AreaAttributes { get; set; }
    [Category("Area"), DisplayName("Facing radians")] public float FacingRadians { get; set; }
    [Category("Area"), DisplayName("Upper Y")] public float UpperY { get; set; }
    [Category("Area"), DisplayName("Lower Y")] public float LowerY { get; set; }
    [Category("Area"), DisplayName("Vertex count"), ReadOnly(true)] public int VertexCount => Vertices.Count;

    [Category("Camera"), DisplayName("Linked")] public bool HasCamera => Camera != null;
    [Category("Camera"), DisplayName("Enabled")] public byte CameraEnabled { get => Camera?.Enabled ?? 0; set { if(Camera!=null) Camera.Enabled=value; } }
    [Category("Camera"), DisplayName("Type")] public CamType CameraType { get => Camera?.Type ?? CamType.Fixed; set { if(Camera!=null) Camera.Type=value; } }
    [Category("Camera"), DisplayName("Attributes")] public CamCameraAttributes CameraAttributes { get => Camera?.Attributes ?? CamCameraAttributes.None; set { if(Camera!=null) Camera.Attributes=value; } }
    [Category("Camera offset"), DisplayName("X")] public float OffsetX { get => CameraOffsetVector.X; set => CameraOffsetVector = new Vector3(value, CameraOffsetVector.Y, CameraOffsetVector.Z); }
    [Category("Camera offset"), DisplayName("Y")] public float OffsetY { get => CameraOffsetVector.Y; set => CameraOffsetVector = new Vector3(CameraOffsetVector.X, value, CameraOffsetVector.Z); }
    [Category("Camera offset"), DisplayName("Z")] public float OffsetZ { get => CameraOffsetVector.Z; set => CameraOffsetVector = new Vector3(CameraOffsetVector.X, CameraOffsetVector.Y, value); }
    [Browsable(false)] public Vector3 CameraOffsetVector { get => Camera?.OffsetVector ?? Vector3.Zero; set { if(Camera!=null) Camera.OffsetVector=value; } }
    [Category("Camera"), DisplayName("Dataset count"), ReadOnly(true)] public int DatasetCount => Frames.Count;

    public override string ToString() => $"[{Index:00}-{SubIndex}] {(Camera==null?"No Camera":CamNames.TypeName((byte)CameraType))} • {Vertices.Count}v/{Frames.Count}f";
}

public sealed class CamCameraRecord
{
    [Browsable(false)] public int FileOrder { get; set; }
    [Browsable(false)] public int FileOffset { get; init; }
    [Browsable(false)] public byte[] Raw { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public List<CamFrame> Frames { get; init; } = new();
    [Browsable(false)] public byte Index { get; set; }
    [Browsable(false)] public byte Enabled { get; set; }
    [Browsable(false)] public CamType Type { get; set; }
    [Browsable(false)] public CamCameraAttributes Attributes { get; set; }
    [Browsable(false)] public Vector3 OffsetVector { get; set; }
}

public sealed class CamVertex
{
    [Browsable(false)] public int Index { get; set; }
    [Browsable(false)] public int FileOffset { get; init; }
    [Browsable(false)] public Vector3 Position { get; set; }
    [Category("Vertex"), DisplayName("X")] public float X { get => Position.X; set => Position = new Vector3(value, Position.Y, Position.Z); }
    [Category("Vertex"), DisplayName("Y")] public float Y { get => Position.Y; set => Position = new Vector3(Position.X, value, Position.Z); }
    [Category("Vertex"), DisplayName("Z")] public float Z { get => Position.Z; set => Position = new Vector3(Position.X, Position.Y, value); }
    public override string ToString() => $"Vertex {Index + 1} • {X:0.##}, {Y:0.##}, {Z:0.##}";
}

public sealed class CamFrame
{
    // O CAM guarda literalmente os bytes 3F 80 00 00 depois de cada vetor.
    // Este campo e gravado como inteiro little-endian opaco, portanto o valor e 0x0000803F.
    private const uint VectorSeparator = 0x0000803F;

    [Browsable(false)] public int Index { get; set; }
    [Browsable(false)] public int PositionOffset { get; init; }
    [Browsable(false)] public int TargetOffset { get; init; }
    [Browsable(false)] public int RollOffset { get; init; }
    [Browsable(false)] public int FovOffset { get; init; }
    [Browsable(false)] public int? TimeOffset { get; init; }
    [Browsable(false)] public uint PositionSuffix { get; init; } = VectorSeparator;
    [Browsable(false)] public uint TargetSuffix { get; init; } = VectorSeparator;
    [Browsable(false)] public Vector3 Position { get; set; }
    [Browsable(false)] public Vector3 Target { get; set; }
    [Category("Position"), DisplayName("X")] public float PositionX { get => Position.X; set => Position = new Vector3(value, Position.Y, Position.Z); }
    [Category("Position"), DisplayName("Y")] public float PositionY { get => Position.Y; set => Position = new Vector3(Position.X, value, Position.Z); }
    [Category("Position"), DisplayName("Z")] public float PositionZ { get => Position.Z; set => Position = new Vector3(Position.X, Position.Y, value); }
    [Category("Target"), DisplayName("X")] public float TargetX { get => Target.X; set => Target = new Vector3(value, Target.Y, Target.Z); }
    [Category("Target"), DisplayName("Y")] public float TargetY { get => Target.Y; set => Target = new Vector3(Target.X, value, Target.Z); }
    [Category("Target"), DisplayName("Z")] public float TargetZ { get => Target.Z; set => Target = new Vector3(Target.X, Target.Y, value); }
    [Category("Lens"), DisplayName("Roll")] public float Roll { get; set; }
    [Category("Lens"), DisplayName("FOV")] public float Fov { get; set; }
    [Category("Animation"), DisplayName("Time frame")] public ushort Time { get; set; }
    public override string ToString() => $"Frame {Index:00} • {Time}f • FOV {Fov:0.##}";
}

[Flags]
public enum CamAreaAttributes : byte { None=0, Normal=1, Battle=2, Event=4, Door=8, Once=16, Ahead=32, Direct=64, DisregardLight=128 }
[Flags]
public enum CamCameraAttributes : byte { None=0, Offset=1, RailEdge=4, RailOneWay=8, QfpsBoth=16, QfpsReady=32 }
[TypeConverter(typeof(CamTypeConverter))]
public enum CamType : byte { Fixed=0, Pan=1, Track=2, RailPan=3, Behind=4, Free=5, Motion=6, UpCut=7, ThirdPerson=8 }

public sealed class CamTypeConverter:EnumConverter
{
    public CamTypeConverter():base(typeof(CamType)){}
    public override object? ConvertTo(ITypeDescriptorContext? context,System.Globalization.CultureInfo? culture,object? value,Type destinationType)=>destinationType==typeof(string)&&value is CamType type&&type==CamType.ThirdPerson?"3rd Person":base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,System.Globalization.CultureInfo? culture,object value)=>value is string text&&text.Equals("3rd Person",StringComparison.OrdinalIgnoreCase)?CamType.ThirdPerson:base.ConvertFrom(context,culture,value);
}

public static class CamNames
{
    public static string TypeName(byte value) => value==8?"3rd Person":Enum.IsDefined(typeof(CamType), value) ? ((CamType)value).ToString() : $"Unknown 0x{value:X2}";
}
