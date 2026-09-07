using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public sealed class Ps2EffFile
{
    public string SourcePath { get; init; } = string.Empty;
    public uint[] TableOffsets { get; init; } = new uint[11];
    public List<EffGroup> Effect0Groups { get; init; } = new();
    public List<EffGroup> Effect1Groups { get; init; } = new();
    public int TexturePackageCount { get; init; }
    public bool IsModified { get; set; }
    public IEnumerable<EffEntry> Entries => Effect0Groups.Concat(Effect1Groups).SelectMany(x => x.Entries);
    public int EntryCount => Entries.Count();
}

public sealed class EffGroup
{
    public int EffectTable { get; init; }
    public int Index { get; init; }
    public int FileOffset { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }
    public float RotationX { get; init; }
    public float RotationY { get; init; }
    public float RotationZ { get; init; }
    public List<EffEntry> Entries { get; init; } = new();
    public Vector3 Position => new(PositionX, PositionY, PositionZ);
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class EffEntry
{
    internal byte[] RawData { get; init; } = new byte[0x12C];
    [Browsable(false)] public EffGroup Group { get; init; } = null!;
    [Browsable(false)] public int FileOffset { get; init; }
    [Browsable(false)] public int Index { get; init; }
    [Category("Identity"), DisplayName("Effect table"), ReadOnly(true)] public int EffectTable => Group.EffectTable;
    [Category("Identity"), DisplayName("Group"), ReadOnly(true)] public int GroupIndex => Group.Index;
    [Category("Identity"), DisplayName("Entry"), ReadOnly(true)] public int EntryIndex => Index;
    [Category("Identity"), DisplayName("File offset"), ReadOnly(true)] public string OffsetDisplay => $"0x{FileOffset:X}";
    [Category("Identity"), DisplayName("ESP ID")] public byte EspId { get; set; }
    [Category("Identity"), DisplayName("Resource / Texture ID")] public byte ResourceId { get; set; }
    [Category("Identity"), DisplayName("Flags")] public uint Flags { get; set; }
    [Category("Hierarchy")] public byte Parent { get; set; }
    [Category("Hierarchy"), DisplayName("Parent part")] public byte ParentPart { get; set; }
    [Category("Timing")] public ushort Time { get; set; }
    [Category("Timing")] public ushort Lifetime { get; set; }
    [Category("Timing"), DisplayName("Animation speed")] public uint AnimationSpeed { get; set; }
    [Category("Position")] public float PositionX { get; set; }
    [Category("Position")] public float PositionY { get; set; }
    [Category("Position")] public float PositionZ { get; set; }
    [Category("Position - Random")] public float RandomPositionX { get; set; }
    [Category("Position - Random")] public float RandomPositionY { get; set; }
    [Category("Position - Random")] public float RandomPositionZ { get; set; }
    [Category("Velocity")] public float SpeedX { get; set; }
    [Category("Velocity")] public float SpeedY { get; set; }
    [Category("Velocity")] public float SpeedZ { get; set; }
    [Category("Acceleration")] public float AccelerationX { get; set; }
    [Category("Acceleration")] public float AccelerationY { get; set; }
    [Category("Acceleration")] public float AccelerationZ { get; set; }
    [Category("Rotation")] public float RotationX { get; set; }
    [Category("Rotation")] public float RotationY { get; set; }
    [Category("Rotation")] public float RotationZ { get; set; }
    [Category("Size")] public float Width { get; set; }
    [Category("Size")] public float Height { get; set; }
    [Category("Size")] public float Grow { get; set; }
    [Category("Color"), DisplayName("Red")] public byte ColorR { get; set; }
    [Category("Color"), DisplayName("Green")] public byte ColorG { get; set; }
    [Category("Color"), DisplayName("Blue")] public byte ColorB { get; set; }
    [Category("Color"), DisplayName("Alpha")] public byte ColorA { get; set; }
    [Category("Rendering")] public ushort Blend { get; set; }
    [Browsable(false)] public Vector3 WorldPosition => Group.Position + new Vector3(PositionX, PositionY, PositionZ);
    public override string ToString() => $"E{EffectTable} G{Group.Index:D2} #{Index:D2}  •  01 {EspId:X2} {ResourceId:X2}";
}
