using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class AevScene
{
    public required string SourcePath { get; init; }
    public required byte HeaderUnknown1 { get; init; }
    public required byte HeaderUnknown2 { get; init; }
    public required int EntrySize { get; init; }
    public required string LayoutName { get; init; }
    public required List<AevEntry> Entries { get; init; }
    public int Count => Entries.Count;
}

public sealed class AevEntry
{
    [Browsable(false)] public int FileOrder { get; set; }
    [Browsable(false)] public byte[] RawData { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public byte[] ParameterBuffer { get; init; } = Array.Empty<byte>();
    [Browsable(false)] public bool HasExplicitRadius { get; init; }
    [Browsable(false)] public bool IsPs2Layout { get; init; }

    [Category("Entry"), DisplayName("Index")] public int Index { get; set; }
    [Category("Entry"), DisplayName("Event")] public string EventType => AevNames.EventTypeName(Type);
    [Category("Entry"), DisplayName("Type ID")] public string TypeId => $"0x{Type:X2}";
    [Browsable(false)] public byte Type { get; set; }
    [Category("Entry"), DisplayName("Active")] public string ActiveName => AevNames.ActiveName(Active);
    [Browsable(false)] public byte Active { get; init; }
    [Category("Entry")] public byte Priority { get; set; }
    [Browsable(false)] public byte DefinitionByte2 { get; set; }
    [Browsable(false)] public byte DefinitionByte3 { get; set; }
    [Browsable(false)] public byte DefinitionByte4 { get; set; }
    [Category("Entry"), DisplayName("Function Pointer")] public string FunctionPointerHex => $"0x{FunctionPointer:X8}";
    [Browsable(false)] public uint FunctionPointer { get; init; }

    [Category("Trigger"), DisplayName("Shape")] public string AreaShape => AevNames.AreaShapeName(AreaHitType, HasExplicitRadius);
    [Category("Trigger"), DisplayName("Area Hit Type")] public string AreaHitTypeHex => $"0x{AreaHitType:X2}";
    [Browsable(false)] public byte AreaHitType { get; init; }
    [Category("Trigger"), DisplayName("Hit Type")] public string HitTypeName => AevNames.HitTypeName(HitType);
    [Browsable(false)] public byte HitType { get; init; }
    [Category("Trigger"), DisplayName("Trigger Type")] public string TriggerTypeName => AevNames.TriggerTypeName(TriggerType);
    [Browsable(false)] public byte TriggerType { get; init; }
    [Category("Trigger"), DisplayName("Target")] public string TargetTypeName => AevNames.TargetTypeName(TargetType);
    [Browsable(false)] public byte TargetType { get; init; }
    [Category("Trigger"), DisplayName("Hit Angle")] public byte HitAngle { get; set; }
    [Category("Trigger"), DisplayName("Open Angle")] public byte OpenAngle { get; set; }
    [Category("Trigger"), DisplayName("Action Type")] public string ActionTypeName => AevNames.ActionTypeName(ActionType);
    [Browsable(false)] public byte ActionType { get; set; }

    [Category("Volume"), DisplayName("Y")] public float Y { get; set; }
    [Category("Volume"), DisplayName("Height")] public float Height { get; set; }
    [Category("Volume"), DisplayName("Circle Radius")] public float CircleRadius { get; set; }
    [Browsable(false)] public Vector2 Position1 { get; set; }
    [Browsable(false)] public Vector2 Position2 { get; set; }
    [Browsable(false)] public Vector2 Position3 { get; set; }
    [Browsable(false)] public Vector2 Position4 { get; set; }

    [Category("Volume • Points"), DisplayName("Point 1 X")] public float Point1X { get => Position1.X; set => Position1 = new Vector2(value, Position1.Y); }
    [Category("Volume • Points"), DisplayName("Point 1 Z")] public float Point1Z { get => Position1.Y; set => Position1 = new Vector2(Position1.X, value); }
    [Category("Volume • Points"), DisplayName("Point 2 X")] public float Point2X { get => Position2.X; set => Position2 = new Vector2(value, Position2.Y); }
    [Category("Volume • Points"), DisplayName("Point 2 Z")] public float Point2Z { get => Position2.Y; set => Position2 = new Vector2(Position2.X, value); }
    [Category("Volume • Points"), DisplayName("Point 3 X")] public float Point3X { get => Position3.X; set => Position3 = new Vector2(value, Position3.Y); }
    [Category("Volume • Points"), DisplayName("Point 3 Z")] public float Point3Z { get => Position3.Y; set => Position3 = new Vector2(Position3.X, value); }
    [Category("Volume • Points"), DisplayName("Point 4 X")] public float Point4X { get => Position4.X; set => Position4 = new Vector2(value, Position4.Y); }
    [Category("Volume • Points"), DisplayName("Point 4 Z")] public float Point4Z { get => Position4.Y; set => Position4 = new Vector2(Position4.X, value); }

    [Browsable(false)] public bool IsSquare => AreaHitType == 1;
    [Browsable(false)] public bool IsCircle => HasExplicitRadius ? AreaHitType == 2 : AreaHitType == 0;
    [Browsable(false)] public bool IsEyeTrigger => HasExplicitRadius ? AreaHitType == 0 : AreaHitType == 2;
    [Browsable(false)] public float VisualRadius => CircleRadius > 0.001f && float.IsFinite(CircleRadius) ? CircleRadius : 0.35f;

    public override string ToString() => $"[{Index:X2}] {AevNames.EventTypeName(Type)}  •  {AevNames.AreaShapeName(AreaHitType, HasExplicitRadius)}";
}

public static class AevNames
{
    private static readonly Dictionary<byte, string> EventTypes = new()
    {
        [0x00] = "General Purpose",
        [0x01] = "Door Way",
        [0x02] = "Cutscene",
        [0x04] = "Grouped Enemy Spawn",
        [0x05] = "Message",
        [0x08] = "Typewriter Save",
        [0x0B] = "Map Block",
        [0x0D] = "Unknown",
        [0x0E] = "Crouch Prompt",
        [0x10] = "Ladder Climb-Up",
        [0x11] = "Item / Locked Door / Puzzle",
        [0x12] = "Ashley HIDE Command",
        [0x14] = "Elevator",
        [0x15] = "Ada Grapple Gun"
    };

    public static IReadOnlyList<KeyValuePair<byte, string>> KnownEventTypes =>
        EventTypes.OrderBy(x => x.Key).ToArray();

    public static bool IsKnownEventType(byte type) => EventTypes.ContainsKey(type);
    public static string EventTypeName(byte type) => EventTypes.TryGetValue(type, out string? name) ? name : $"Unknown Event 0x{type:X2}";
    public static string AreaShapeName(byte type, bool hasExplicitRadius) => hasExplicitRadius
        ? type switch { 1 => "Rectangle", 2 => "Circle", 0 => "Special / Category 00", _ => $"Shape 0x{type:X2}" }
        : type switch { 0 => "Circle", 1 => "Rectangle", 2 => "Eye Trigger", _ => $"Shape 0x{type:X2}" };
    public static string ActiveName(byte value) => value switch { 2 => "Inactive (2)", 3 => "Active (3)", _ => $"0x{value:X2}" };
    public static string HitTypeName(byte value) => value switch { 0 => "Under", 1 => "Front", 2 => "Under + Angle", 3 => "Front + Angle", _ => $"0x{value:X2}" };
    public static string TriggerTypeName(byte value)
    {
        var names = new List<string>();
        if ((value & 1) != 0) names.Add("Auto"); if ((value & 2) != 0) names.Add("Manual"); if ((value & 4) != 0) names.Add("Semi-auto");
        if ((value & 8) != 0) names.Add("Action Button"); if ((value & 0x80) != 0) names.Add("One-time");
        return names.Count > 0 ? string.Join(" + ", names) : $"0x{value:X2}";
    }
    public static string TargetTypeName(byte value)
    {
        var names = new List<string>();
        if ((value & 1) != 0) names.Add("Player"); if ((value & 2) != 0) names.Add("Enemy"); if ((value & 4) != 0) names.Add("Object"); if ((value & 8) != 0) names.Add("Ashley");
        return names.Count > 0 ? string.Join(" + ", names) : $"0x{value:X2}";
    }
    public static string ActionTypeName(byte value) => value switch { 0 => "None / Automatic", 1 => "Action", 2 => "Check", 3 => "Open", _ => $"0x{value:X2}" };
}
