using System.ComponentModel;
using System.Globalization;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class AevPropertyView : ICustomTypeDescriptor
{
    public AevEntry Entry { get; }
    public AevPropertyView(AevEntry entry) => Entry = entry;

    [Category("Entry"), DisplayName("Index")]
    public int Index
    {
        get => Entry.Index;
        set
        {
            if (value < 0 || value > 255) throw new ArgumentOutOfRangeException(nameof(value), "Index deve ficar entre 0 e 255.");
            Entry.Index = value;
        }
    }

    [Category("Entry"), DisplayName("Type"), TypeConverter(typeof(AevEventTypeNameConverter))]
    public string Type { get => AevNames.EventTypeName(Entry.Type); set => Entry.Type = AevEventTypeNameConverter.NameToValue(value); }

    [Category("Entry"), DisplayName("Type ID"), ReadOnly(true)] public string TypeId => $"0x{Entry.Type:X2}";
    [Category("Entry"), DisplayName("Active"), ReadOnly(true)] public string Active => AevNames.ActiveName(Entry.Active);
    [Category("Entry"), DisplayName("Function Pointer"), ReadOnly(true)] public string FunctionPointer => $"0x{Entry.FunctionPointer:X8}";

    // Old aev.js: absolute first-entry offsets 84..87 => entry +0x44..0x47.
    [Category("Definition Bytes"), DisplayName("Definition 1 (+0x44)")] public byte Definition1 { get => Entry.Priority; set => Entry.Priority = value; }
    [Category("Definition Bytes"), DisplayName("Definition 2 (+0x45)")] public byte Definition2 { get => Entry.DefinitionByte2; set => Entry.DefinitionByte2 = value; }
    [Category("Definition Bytes"), DisplayName("Definition 3 (+0x46)")] public byte Definition3 { get => Entry.DefinitionByte3; set => Entry.DefinitionByte3 = value; }
    [Category("Definition Bytes"), DisplayName("Definition 4 (+0x47)")] public byte Definition4 { get => Entry.DefinitionByte4; set => Entry.DefinitionByte4 = value; }

    [Category("Trigger"), DisplayName("Shape"), ReadOnly(true)] public string Shape => AevNames.AreaShapeName(Entry.AreaHitType, Entry.HasExplicitRadius);
    [Category("Trigger"), DisplayName("Hit Type"), ReadOnly(true)] public string HitType => AevNames.HitTypeName(Entry.HitType);
    [Category("Trigger"), DisplayName("Trigger Type"), ReadOnly(true)] public string TriggerType => AevNames.TriggerTypeName(Entry.TriggerType);
    [Category("Trigger"), DisplayName("Target"), ReadOnly(true)] public string Target => AevNames.TargetTypeName(Entry.TargetType);

    [Category("Volume"), DisplayName("Y")] public float Y { get => Entry.Y; set => Entry.Y = value; }
    [Category("Volume"), DisplayName("Height")] public float Height { get => Entry.Height; set => Entry.Height = value; }
    [Category("Volume"), DisplayName("Circle Radius")] public float CircleRadius { get => Entry.CircleRadius; set => Entry.CircleRadius = value; }

    [Category("Volume • Points"), DisplayName("Point 1 X")] public float Point1X { get => Entry.Point1X; set => Entry.Point1X = value; }
    [Category("Volume • Points"), DisplayName("Point 1 Z")] public float Point1Z { get => Entry.Point1Z; set => Entry.Point1Z = value; }
    [Category("Volume • Points"), DisplayName("Point 2 X")] public float Point2X { get => Entry.Point2X; set => Entry.Point2X = value; }
    [Category("Volume • Points"), DisplayName("Point 2 Z")] public float Point2Z { get => Entry.Point2Z; set => Entry.Point2Z = value; }
    [Category("Volume • Points"), DisplayName("Point 3 X")] public float Point3X { get => Entry.Point3X; set => Entry.Point3X = value; }
    [Category("Volume • Points"), DisplayName("Point 3 Z")] public float Point3Z { get => Entry.Point3Z; set => Entry.Point3Z = value; }
    [Category("Volume • Points"), DisplayName("Point 4 X")] public float Point4X { get => Entry.Point4X; set => Entry.Point4X = value; }
    [Category("Volume • Points"), DisplayName("Point 4 Z")] public float Point4Z { get => Entry.Point4Z; set => Entry.Point4Z = value; }

    // TYPE 0x01 - Door Way. aev.js absolute 112/116/120/124/128/129.
    // ParameterBuffer begins entry +0x5C => +0x60 is buffer[4].
    [Category("Door Parameters"), DisplayName("Teleport X")] public float DoorX { get => GetF32(4); set => SetF32(4, value); }
    [Category("Door Parameters"), DisplayName("Teleport Y")] public float DoorY { get => GetF32(8); set => SetF32(8, value); }
    [Category("Door Parameters"), DisplayName("Teleport Z")] public float DoorZ { get => GetF32(12); set => SetF32(12, value); }
    [Category("Door Parameters"), DisplayName("Facing Angle")] public float DoorFacing { get => GetF32(16); set => SetF32(16, value); }
    [Category("Door Parameters"), DisplayName("Stage ID")] public byte DoorStageId { get => GetU8(20); set => SetU8(20, value); }
    [Category("Door Parameters"), DisplayName("Room ID")] public byte DoorRoomId { get => GetU8(21); set => SetU8(21, value); }

    // TYPE 0x02 - Cutscene. aev.js absolute 88/89/90 => entry +0x48/+0x49/+0x4A.
    [Category("Cutscene Parameters"), DisplayName("Offset +0x48")] public byte CutsceneByte1 { get => Entry.HitAngle; set => Entry.HitAngle = value; }
    [Category("Cutscene Parameters"), DisplayName("Offset +0x49")] public byte CutsceneByte2 { get => Entry.OpenAngle; set => Entry.OpenAngle = value; }
    [Category("Cutscene Parameters"), DisplayName("Offset +0x4A")] public byte CutsceneByte3 { get => Entry.ActionType; set => Entry.ActionType = value; }

    // TYPE 0x04 - Grouped Enemy Spawn. aev.js absolute 114 => entry +0x62 => buffer[6].
    [Category("Enemy Spawn Parameters"), DisplayName("Enemy Group")] public byte EnemyGroup { get => GetU8(6); set => SetU8(6, value); }

    // TYPE 0x05 - Message. aev.js absolute 114/116/118 => +0x62/+0x64/+0x66.
    [Category("Message Parameters"), DisplayName("Message")] public byte Message { get => GetU8(6); set => SetU8(6, value); }
    [Category("Message Parameters"), DisplayName("Camera")] public byte MessageCamera { get => GetU8(8); set => SetU8(8, value); }
    [Category("Message Parameters"), DisplayName("Sound")] public byte MessageSound { get => GetU8(10); set => SetU8(10, value); }

    // TYPE 0x0A - Damage.
    [Category("Damage Parameters"), DisplayName("Reaction Time")] public byte DamageReactionTime { get => GetU8(4); set => SetU8(4, value); }
    [Category("Damage Parameters"), DisplayName("Animation Type")] public byte DamageAnimationType { get => GetU8(8); set => SetU8(8, value); }
    [Category("Damage Parameters"), DisplayName("Damage Origin")] public byte DamageOrigin { get => GetU8(9); set => SetU8(9, value); }
    [Category("Damage Parameters"), DisplayName("Damage Amount")] public byte DamageAmount { get => GetU8(12); set => SetU8(12, value); }

    // TYPE 0x10 - Ladder.
    [Category("Ladder Parameters"), DisplayName("Position X")] public float LadderX { get => GetF32(4); set => SetF32(4, value); }
    [Category("Ladder Parameters"), DisplayName("Position Y")] public float LadderY { get => GetF32(8); set => SetF32(8, value); }
    [Category("Ladder Parameters"), DisplayName("Position Z")] public float LadderZ { get => GetF32(12); set => SetF32(12, value); }
    [Category("Ladder Parameters"), DisplayName("Facing Angle")] public float LadderFacing { get => GetF32(20); set => SetF32(20, value); }
    [Category("Ladder Parameters"), DisplayName("Steps")] public byte LadderSteps { get => GetU8(24); set => SetU8(24, value); }
    [Category("Ladder Parameters"), DisplayName("Unknown 1")] public byte LadderUnknown1 { get => GetU8(25); set => SetU8(25, value); }
    [Category("Ladder Parameters"), DisplayName("Unknown 2")] public byte LadderUnknown2 { get => GetU8(26); set => SetU8(26, value); }
    [Category("Ladder Parameters"), DisplayName("Camera Start")] public byte LadderCameraStart { get => GetU8(27); set => SetU8(27, value); }
    [Category("Ladder Parameters"), DisplayName("Camera End")] public byte LadderCameraEnd { get => GetU8(28); set => SetU8(28, value); }

    // TYPE 0x11 - Item dependent. JS uses one byte at absolute 112.
    [Category("Item Parameters"), DisplayName("Item ID")] public byte ItemId { get => GetU8(4); set => SetU8(4, value); }

    // TYPE 0x12 - Ashley HIDE. JS confirms only X/Y/Z.
    [Category("Hide Parameters"), DisplayName("Position X")] public float HideX { get => GetF32(4); set => SetF32(4, value); }
    [Category("Hide Parameters"), DisplayName("Position Y")] public float HideY { get => GetF32(8); set => SetF32(8, value); }
    [Category("Hide Parameters"), DisplayName("Position Z")] public float HideZ { get => GetF32(12); set => SetF32(12, value); }

    // TYPE 0x15 - Ada Grapple Gun.
    [Category("Grapple Parameters"), DisplayName("Area X")] public float GrappleAreaX { get => GetF32(4); set => SetF32(4, value); }
    [Category("Grapple Parameters"), DisplayName("Area Y")] public float GrappleAreaY { get => GetF32(8); set => SetF32(8, value); }
    [Category("Grapple Parameters"), DisplayName("Area Z")] public float GrappleAreaZ { get => GetF32(12); set => SetF32(12, value); }
    [Category("Grapple Parameters"), DisplayName("Destination X")] public float GrappleDestinationX { get => GetF32(20); set => SetF32(20, value); }
    [Category("Grapple Parameters"), DisplayName("Destination Y")] public float GrappleDestinationY { get => GetF32(24); set => SetF32(24, value); }
    [Category("Grapple Parameters"), DisplayName("Destination Z")] public float GrappleDestinationZ { get => GetF32(28); set => SetF32(28, value); }

    [Category("Event Parameters"), DisplayName("Raw Parameters (64 bytes)")]
    public string ParametersHex
    {
        get => Convert.ToHexString(Entry.ParameterBuffer);
        set
        {
            string clean = new string((value ?? "").Where(Uri.IsHexDigit).ToArray());
            if (clean.Length != Entry.ParameterBuffer.Length * 2)
                throw new FormatException($"Informe exatamente {Entry.ParameterBuffer.Length * 2} dígitos hexadecimais.");
            byte[] parsed = Convert.FromHexString(clean);
            Buffer.BlockCopy(parsed, 0, Entry.ParameterBuffer, 0, parsed.Length);
        }
    }

    private byte GetU8(int o) => Ensure(o, 1) ? Entry.ParameterBuffer[o] : (byte)0;
    private float GetF32(int o) => Ensure(o, 4) ? BitConverter.ToSingle(Entry.ParameterBuffer, o) : 0f;
    private void SetU8(int o, byte v) { Require(o, 1); Entry.ParameterBuffer[o] = v; }
    private void SetF32(int o, float v)
    {
        if (!float.IsFinite(v)) throw new ArgumentOutOfRangeException(nameof(v));
        Require(o, 4); Buffer.BlockCopy(BitConverter.GetBytes(v), 0, Entry.ParameterBuffer, o, 4);
    }
    private bool Ensure(int o, int n) => o >= 0 && o + n <= Entry.ParameterBuffer.Length;
    private void Require(int o, int n)
    {
        if (!Ensure(o, n)) throw new InvalidDataException("ParameterBuffer AEV menor que o esperado.");
    }

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        PropertyDescriptorCollection all = TypeDescriptor.GetProperties(this, attributes, true);
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(Index), nameof(Type), nameof(TypeId), nameof(Active), nameof(FunctionPointer),
            nameof(Definition1), nameof(Definition2), nameof(Definition3), nameof(Definition4),
            nameof(Shape), nameof(HitType), nameof(TriggerType), nameof(Target),
            nameof(Y), nameof(Height)
        };

        if (Entry.IsCircle) names.Add(nameof(CircleRadius));
        if (Entry.IsSquare)
            names.UnionWith(new[] { nameof(Point1X), nameof(Point1Z), nameof(Point2X), nameof(Point2Z),
                nameof(Point3X), nameof(Point3Z), nameof(Point4X), nameof(Point4Z) });

        switch (Entry.Type)
        {
            case 0x01:
                names.UnionWith(new[] { nameof(DoorX), nameof(DoorY), nameof(DoorZ), nameof(DoorFacing), nameof(DoorStageId), nameof(DoorRoomId) });
                break;
            case 0x02:
                names.UnionWith(new[] { nameof(CutsceneByte1), nameof(CutsceneByte2), nameof(CutsceneByte3) });
                break;
            case 0x04:
                names.Add(nameof(EnemyGroup));
                break;
            case 0x05:
                names.UnionWith(new[] { nameof(Message), nameof(MessageCamera), nameof(MessageSound) });
                break;
            case 0x0A:
                names.UnionWith(new[] { nameof(DamageReactionTime), nameof(DamageAnimationType), nameof(DamageOrigin), nameof(DamageAmount) });
                break;
            case 0x10:
                names.UnionWith(new[] { nameof(LadderX), nameof(LadderY), nameof(LadderZ), nameof(LadderFacing), nameof(LadderSteps),
                    nameof(LadderUnknown1), nameof(LadderUnknown2), nameof(LadderCameraStart), nameof(LadderCameraEnd) });
                break;
            case 0x11:
                names.Add(nameof(ItemId));
                break;
            case 0x12:
                names.UnionWith(new[] { nameof(HideX), nameof(HideY), nameof(HideZ) });
                break;
            case 0x15:
                names.UnionWith(new[] { nameof(GrappleAreaX), nameof(GrappleAreaY), nameof(GrappleAreaZ),
                    nameof(GrappleDestinationX), nameof(GrappleDestinationY), nameof(GrappleDestinationZ) });
                break;
            default:
                names.Add(nameof(ParametersHex));
                break;
        }

        return new PropertyDescriptorCollection(all.Cast<PropertyDescriptor>().Where(x => names.Contains(x.Name)).ToArray(), true);
    }

    public PropertyDescriptorCollection GetProperties() => GetProperties(null);
    public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);
    public string? GetClassName() => TypeDescriptor.GetClassName(this, true);
    public string? GetComponentName() => TypeDescriptor.GetComponentName(this, true);
    public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);
    public EventDescriptor? GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
    public PropertyDescriptor? GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);
    public object? GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
    public EventDescriptorCollection GetEvents(Attribute[]? attributes) => TypeDescriptor.GetEvents(this, attributes, true);
    public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);
    public object GetPropertyOwner(PropertyDescriptor? pd) => this;
}

public sealed class AevEventTypeNameConverter : TypeConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) =>
        new(AevNames.KnownEventTypes.Select(x => x.Value).ToArray());

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s) return AevNames.EventTypeName(NameToValue(s));
        return base.ConvertFrom(context, culture, value);
    }

    public static byte NameToValue(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        foreach (var kv in AevNames.KnownEventTypes)
            if (kv.Value.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                return kv.Key;

        string text = name.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            byte.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hex))
            return hex;
        if (byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte dec))
            return dec;
        throw new FormatException("Use um tipo conhecido, decimal ou 0xHH.");
    }
}
