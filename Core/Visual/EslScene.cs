using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class EslScene
{
    public string SourcePath { get; }
    public List<EslEnemyEntry> Entries { get; }
    public int ActiveCount => Entries.Count(x => x.Active != 0);
    public EslScene(string sourcePath, List<EslEnemyEntry> entries) { SourcePath = sourcePath; Entries = entries; }
}

public sealed class EslEnemyEntry
{
    [Browsable(false)] public int Index { get; set; }
    [Category("Enemy"), DisplayName("Active"), Description("0 = inactive, 1 = active.")] public byte Active { get; set; }
    [Category("Enemy"), DisplayName("Enemy Type (raw)"), Description("Raw emXX type byte. The friendly name is shown above.")] public byte EnemyType { get; set; }
    [Category("Enemy"), DisplayName("Subtype (raw)"), Description("Raw subtype byte. The friendly subtype name is shown above.")] public byte Subtype { get; set; }
    [Category("Enemy"), DisplayName("Enemy Name"), ReadOnly(true)] public string EnemyName => EslEnemyCatalog.GetEnemyLabel(EnemyType);
    [Category("Enemy"), DisplayName("Subtype Name"), ReadOnly(true)] public string SubtypeName => EslEnemyCatalog.GetSubtypeLabel(EnemyType, Subtype);
    [Category("Enemy"), DisplayName("Full Name"), ReadOnly(true)] public string FriendlyName => EslEnemyCatalog.GetFullName(EnemyType, Subtype);
    [Category("Enemy")] public byte Animation { get; set; }
    [Category("Enemy"), DisplayName("Sight Range")] public byte SightRange { get; set; }
    [Category("Equipment"), DisplayName("Equip 1 / Flag byte 5"), Description("Bit flags. Village mapping is decoded in Equipment Summary.")] public byte Equip1 { get; set; }
    [Category("Equipment"), DisplayName("Equip 2 / Flag byte 6"), Description("Bit flags. Includes dynamite/accessory/Plaga behavior on documented Village Ganados.")] public byte Equip2 { get; set; }
    [Category("Equipment"), DisplayName("Weapon / Flag byte 7"), Description("Bit flags. Documented Village values include 08=Sickle, 10=Chainsaw, 20=Axe, 40=Bucket, 80=Pitchfork.")] public byte Weapon { get; set; }
    [Category("Equipment"), DisplayName("Equipment Summary"), ReadOnly(true)] public string EquipmentSummary => EnemyEquipmentCatalog.GetSummary(this);
    [Category("Enemy"), DisplayName("Health / HP")] public ushort Health { get; set; }
    [Category("Flags"), DisplayName("Unknown 1")] public byte Unknown1 { get; set; }
    [Category("Flags"), DisplayName("Return / Spawn")] public byte ReturnSpawn { get; set; }
    [Category("Transform"), DisplayName("Position X")] public short PosX { get; set; }
    [Category("Transform"), DisplayName("Position Y")] public short PosY { get; set; }
    [Category("Transform"), DisplayName("Position Z")] public short PosZ { get; set; }
    [Category("Transform"), DisplayName("Rotation X (raw)")] public short RotX { get; set; }
    [Category("Transform"), DisplayName("Rotation Y (raw)")] public short RotY { get; set; }
    [Category("Transform"), DisplayName("Rotation Z (raw)")] public short RotZ { get; set; }
    [Category("Location"), DisplayName("Room ID")] public byte RoomID { get; set; }
    [Category("Location"), DisplayName("Stage ID")] public byte StageID { get; set; }
    [Category("Location"), DisplayName("Scenario"), ReadOnly(true), Description("Stage + Room: Stage 1 / Room 02 = r102.dat")] public string ScenarioName => $"r{StageID:X1}{RoomID:X2}.dat";
    [Category("Unknown"), DisplayName("Unknown 2")] public byte Unknown2 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 3")] public byte Unknown3 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 4")] public byte Unknown4 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 5")] public byte Unknown5 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 6")] public byte Unknown6 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 7")] public byte Unknown7 { get; set; }
    [Browsable(false)] public Vector3 Position => new(PosX, PosY, PosZ);
    public override string ToString() => $"#{Index:D3}  {EslEnemyCatalog.GetFullName(EnemyType, Subtype)}  HP {Health}";
}
