using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public enum EffSequenceTable { EST = 0, SST = 1 }
public enum EffEntryKind : byte { Effect = 0, Generator = 1 }

public sealed class Ps2EffFile
{
    public string SourcePath { get; init; } = string.Empty;
    public uint[] TableOffsets { get; init; } = new uint[11];
    public List<EffGroup> EstGroups { get; init; } = new();
    public List<EffGroup> SstGroups { get; init; } = new();
    [Obsolete("Use EstGroups.")] public List<EffGroup> Effect0Groups => EstGroups;
    [Obsolete("Use SstGroups.")] public List<EffGroup> Effect1Groups => SstGroups;
    public int TexturePackageCount { get; init; }
    public bool IsModified { get; set; }
    public IEnumerable<EffEntry> Entries => EstGroups.Concat(SstGroups).SelectMany(x => x.Entries);
    public int EntryCount => Entries.Count();
}

public sealed class EffGroup
{
    public EffSequenceTable SequenceTable { get; init; }
    [Obsolete("Use SequenceTable.")] public int EffectTable => (int)SequenceTable;
    public int Index { get; init; }
    public int FileOffset { get; init; }
    public ushort Flags { get; set; }
    public byte DefaultParts { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public byte Version { get; set; }
    public ushort CoreFlags { get; set; }
    internal byte[] RawHeader { get; init; } = new byte[0x30];
    public List<EffEntry> Entries { get; init; } = new();
    public Vector3 Position => new(PositionX, PositionY, PositionZ);
    public override string ToString() => $"{SequenceTable} {Index:D2} ({Entries.Count} efeitos)";
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class EffEntry
{
    internal byte[] RawData { get; init; } = new byte[0x12C];
    [Browsable(false)] public EffGroup Group { get; init; } = null!;
    [Browsable(false)] public int FileOffset { get; init; }
    [Browsable(false)] public int Index { get; init; }
    [Category("Identity"), DisplayName("Sequence table"), ReadOnly(true)] public EffSequenceTable SequenceTable => Group.SequenceTable;
    [Browsable(false), Obsolete("Use SequenceTable.")] public int EffectTable => (int)SequenceTable;
    [Category("Identity"), DisplayName("Group"), ReadOnly(true)] public int GroupIndex => Group.Index;
    [Category("Identity"), DisplayName("Entry"), ReadOnly(true)] public int EntryIndex => Index;
    [Category("Identity"), DisplayName("File offset"), ReadOnly(true)] public string OffsetDisplay => $"0x{FileOffset:X}";
    [Category("Identity"), DisplayName("Enabled marker"), ReadOnly(true)] public byte EnabledMarker { get; set; }
    [Category("Identity"), DisplayName("ESP ID")] public byte EspId { get; set; }
    [Category("Identity"), DisplayName("Resource / Texture ID")] public byte ResourceId { get; set; }
    [Category("Identity")] public byte Type { get; set; }
    [Category("Identity")] public EffEntryKind Kind { get; set; }
    [Category("Generator"), DisplayName("Generator ID")] public byte EspgenId { get; set; }
    [Category("Generator"), DisplayName("Generator type")] public byte EspgenType { get; set; }
    [Category("Generator"), DisplayName("Generator flags")] public byte EspgenFlags { get; set; }
    [Category("Identity"), DisplayName("Behavior"), ReadOnly(true)] public string BehaviorDisplay => Kind == EffEntryKind.Generator ? GeneratorBehaviorName(EspgenId) : EffectBehaviorName(EspId);
    [Category("Options"), DisplayName("Tool flags")] public uint Flags { get; set; }
    [Category("Attachment"), DisplayName("Parent model number")] public byte Parent { get; set; }
    [Category("Attachment"), DisplayName("Model part / screen layer")] public byte ParentPart { get; set; }
    [Category("Attachment"), DisplayName("Attachment interpretation"), ReadOnly(true)] public string AttachmentDisplay => ParentPart switch { 0xFF => "None", 0xFE => "World / free", >= 0xF8 and <= 0xFD => $"Screen layer 0x{ParentPart:X2}", _ => Parent == 0 ? $"Default model, part {ParentPart}" : $"Model {Parent - 1}, part {ParentPart}" };
    [Category("Timing"), DisplayName("Set time")] public ushort Time { get; set; }
    [Category("Timing"), DisplayName("Maximum lifetime")] public ushort Lifetime { get; set; }
    [Category("Timing"), DisplayName("Current/start lifetime")] public ushort LifeTime { get; set; }
    [Category("Animation"), DisplayName("Start pattern")] public byte PatternNumber { get; set; }
    [Category("Animation"), DisplayName("Animation rate")] public sbyte AnimationRate { get; set; }
    [Category("Animation"), DisplayName("Animation counter")] public ushort AnimationCounter { get; set; }
    [Category("Attachment"), DisplayName("Release time")] public byte ReleaseTime { get; set; }
    [Category("Rendering"), DisplayName("Group number")] public byte GroupeNumber { get; set; }
    [Category("Rendering"), DisplayName("Blend type")] public byte BlendType { get; set; }
    [Category("Rendering"), DisplayName("Shimmer type")] public byte ShimmerType { get; set; }
    [Category("Rendering"), DisplayName("Shimmer power")] public byte ShimmerPower { get; set; }
    [Category("Rendering"), DisplayName("Mask texture ID")] public byte MaskTextureId { get; set; }
    [Category("Rendering"), DisplayName("Delete distance far / 10")] public byte DeleteFar { get; set; }
    [Category("Rendering"), DisplayName("Delete distance near / 10")] public byte DeleteNear { get; set; }
    [Category("Position")] public float PositionX { get; set; }
    [Category("Position")] public float PositionY { get; set; }
    [Category("Position")] public float PositionZ { get; set; }
    [Category("Position - Random")] public float RandomPositionX { get; set; }
    [Category("Position - Random")] public float RandomPositionY { get; set; }
    [Category("Position - Random")] public float RandomPositionZ { get; set; }
    [Category("Velocity")] public float SpeedX { get; set; }
    [Category("Velocity")] public float SpeedY { get; set; }
    [Category("Velocity")] public float SpeedZ { get; set; }
    [Category("Velocity"), DisplayName("Speed damping")] public float SpeedDamping { get; set; }
    [Category("Velocity - Random")] public float RandomSpeedX { get; set; }
    [Category("Velocity - Random")] public float RandomSpeedY { get; set; }
    [Category("Velocity - Random")] public float RandomSpeedZ { get; set; }
    [Category("Acceleration")] public float AccelerationX { get; set; }
    [Category("Acceleration")] public float AccelerationY { get; set; }
    [Category("Acceleration")] public float AccelerationZ { get; set; }
    [Category("Acceleration - Random")] public float RandomAccelerationX { get; set; }
    [Category("Acceleration - Random")] public float RandomAccelerationY { get; set; }
    [Category("Acceleration - Random")] public float RandomAccelerationZ { get; set; }
    [Category("Rotation")] public float RotationX { get; set; }
    [Category("Rotation")] public float RotationY { get; set; }
    [Category("Rotation")] public float RotationZ { get; set; }
    [Category("Rotation - Random")] public float RandomRotationX { get; set; }
    [Category("Rotation - Random")] public float RandomRotationY { get; set; }
    [Category("Rotation - Random")] public float RandomRotationZ { get; set; }
    [Category("Angular velocity")] public float AngularVelocityX { get; set; }
    [Category("Angular velocity")] public float AngularVelocityY { get; set; }
    [Category("Angular velocity")] public float AngularVelocityZ { get; set; }
    [Category("Angular velocity - Random")] public float RandomAngularVelocityX { get; set; }
    [Category("Angular velocity - Random")] public float RandomAngularVelocityY { get; set; }
    [Category("Angular velocity - Random")] public float RandomAngularVelocityZ { get; set; }
    [Category("Size")] public float Width { get; set; }
    [Category("Size")] public float Height { get; set; }
    [Category("Size"), DisplayName("Random size")] public float RandomSize { get; set; }
    [Category("Size"), DisplayName("Growth")] public float Grow { get; set; }
    [Category("Size"), DisplayName("Growth damping")] public float GrowDamping { get; set; }
    [Category("Color - Start"), DisplayName("Red")] public byte ColorR { get; set; }
    [Category("Color - Start"), DisplayName("Green")] public byte ColorG { get; set; }
    [Category("Color - Start"), DisplayName("Blue")] public byte ColorB { get; set; }
    [Category("Color - Start"), DisplayName("Alpha")] public byte ColorA { get; set; }
    [Category("Color - Step")] public float ColorStepR { get; set; }
    [Category("Color - Step")] public float ColorStepG { get; set; }
    [Category("Color - Step")] public float ColorStepB { get; set; }
    [Category("Color - Step")] public float ColorStepA { get; set; }
    [Category("Timing"), DisplayName("Color max count")] public ushort ColorMaxCount { get; set; }
    [Category("Timing"), DisplayName("Color start count")] public ushort ColorStartCount { get; set; }
    [Category("Timing"), DisplayName("Position start count")] public ushort PositionStartCount { get; set; }
    [Category("Timing"), DisplayName("Size start count")] public ushort SizeStartCount { get; set; }
    [Category("Effect parameters")] public byte Work8_0 { get; set; }
    [Category("Effect parameters")] public byte Work8_1 { get; set; }
    [Category("Effect parameters")] public byte Work8_2 { get; set; }
    [Category("Effect parameters")] public byte Work8_3 { get; set; }
    [Category("Effect parameters")] public uint Work32_0 { get; set; }
    [Category("Effect parameters")] public uint Work32_1 { get; set; }
    [Category("Effect parameters")] public uint Work32_2 { get; set; }
    [Category("Effect parameters")] public float Vector0X { get; set; }
    [Category("Effect parameters")] public float Vector0Y { get; set; }
    [Category("Effect parameters")] public float Vector0Z { get; set; }
    [Category("Effect parameters")] public float Vector1X { get; set; }
    [Category("Effect parameters")] public float Vector1Y { get; set; }
    [Category("Effect parameters")] public float Vector1Z { get; set; }
    [Category("Effect parameters")] public float Vector2X { get; set; }
    [Category("Effect parameters")] public float Vector2Y { get; set; }
    [Category("Effect parameters")] public float Vector2Z { get; set; }
    [Category("Effect parameters")] public byte WorkSpecial8_0 { get; set; }
    [Category("Effect parameters")] public byte WorkSpecial8_1 { get; set; }
    [Category("Effect parameters")] public byte WorkSpecial8_2 { get; set; }
    [Category("Effect parameters")] public byte WorkSpecial8_3 { get; set; }
    [Category("Advanced")] public uint Reserved100 { get; set; }
    [Category("Generator")] public byte GeneratorExtra0 { get; set; }
    [Category("Generator")] public byte GeneratorExtra1 { get; set; }
    [Category("Generator")] public byte GeneratorExtra2 { get; set; }
    [Category("Generator")] public byte GeneratorExtra3 { get; set; }
    [Category("Generator")] public sbyte GeneratorWork8_0 { get; set; }
    [Category("Generator")] public sbyte GeneratorWork8_1 { get; set; }
    [Category("Generator")] public sbyte GeneratorWork8_2 { get; set; }
    [Category("Generator")] public sbyte GeneratorWork8_3 { get; set; }
    [Category("Generator")] public short GeneratorWork16_0 { get; set; }
    [Category("Generator")] public short GeneratorWork16_1 { get; set; }
    [Category("Generator")] public short GeneratorWork16_2 { get; set; }
    [Category("Generator")] public short GeneratorWork16_3 { get; set; }
    [Category("Generator")] public float GeneratorVectorX { get; set; }
    [Category("Generator")] public float GeneratorVectorY { get; set; }
    [Category("Generator")] public float GeneratorVectorZ { get; set; }
    [Category("Generator curves")] public sbyte GeneratorCurve0 { get; set; }
    [Category("Generator curves")] public sbyte GeneratorCurve1 { get; set; }
    [Category("Generator curves")] public sbyte GeneratorCurve2 { get; set; }
    [Category("Generator curves")] public sbyte GeneratorCurve3 { get; set; }
    [Category("Generator parameters")] public byte GeneratorParameter0 { get; set; }
    [Category("Generator parameters")] public byte GeneratorParameter1 { get; set; }
    [Category("Generator parameters")] public byte GeneratorParameter2 { get; set; }
    [Category("Generator parameters")] public byte GeneratorParameter3 { get; set; }
    [Browsable(false)] public Vector3 WorldPosition => Group.Position + new Vector3(PositionX, PositionY, PositionZ);
    public override string ToString() => $"{SequenceTable} G{Group.Index:D2} #{Index:D2} • {(Kind == EffEntryKind.Generator ? $"GEN {EspgenId:X2}" : $"ESP {EspId:X2}")} • TEX {ResourceId:X2}";

    private static string GeneratorBehaviorName(byte id) => id switch
    {
        0x00 => "Generic emitter", 0x42 => "Room water surface", 0x45 => "Outdoor/weather water surface", 0xFF => "Loop marker", _ => $"Generator 0x{id:X2}"
    };

    private static string EffectBehaviorName(byte id) => id switch
    {
        0x00=>"Sprite",0x01=>"Trail strip",0x02=>"Ribbon",0x03=>"Line trail",0x04=>"Screen tiled overlay",0x05=>"Flutter particle",0x06=>"Path follower",0x07=>"Bounce/collision",0x08=>"Scrolling textured plane",0x09=>"Position trail",0x0A=>"Ghost trail",0x0B=>"Camera jitter",0x0C=>"Spawner/controller",0x0D=>"Attraction",0x0E=>"Lens flare",0x0F=>"Refraction",0x10=>"Ground/water decal",0x11=>"Dynamic light",0x12=>"History ribbon",0x14=>"Light shaft",0x15=>"Weather particle",0x16=>"Rope/chain",0x17=>"Camera-space sprite",0x18=>"Heat shimmer / radial blur",0x19=>"3D line",0x1A=>"Part-axis spawn",0x1B=>"Spline ribbon",0x3F=>"Buffer effect",0x40=>"Water-surface follower",0x41=>"Player attraction",0x42=>"Custom blend",0x43=>"Delayed trigger",0x44=>"Sound trigger",0x45=>"Bloom",0x46=>"Full-screen filter",0x47=>"Screen wrap",0x48=>"Oscillating rotation",0x49=>"Water sink",0x4A=>"Quake",0x4B=>"Fixed frame",0x4C=>"Water/weather controller",0x4D=>"Water ripple",0x4E=>"Cloth",0x4F=>"Area-gated sprite",_=>$"Effect 0x{id:X2}"
    };
}
