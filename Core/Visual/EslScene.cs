using System.ComponentModel;
using System.Numerics;
using System.Globalization;
using System.Drawing.Design;
using System.Windows.Forms.Design;

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
    [Category("Enemy"), DisplayName("Enemy Type (raw)"), Description("Raw emXX type byte in hexadecimal."), TypeConverter(typeof(HexByteConverter))] public byte EnemyType { get; set; }
    [Category("Enemy"), DisplayName("Subtype (raw)"), Description("Raw subtype byte in hexadecimal."), TypeConverter(typeof(HexByteConverter))] public byte Subtype { get; set; }
    [Category("Enemy"), DisplayName("Enemy Name"), ReadOnly(true)] public string EnemyName => EslEnemyCatalog.GetEnemyLabel(EnemyType);
    [Category("Enemy"), DisplayName("Subtype Name"), ReadOnly(true)] public string SubtypeName => EslEnemyCatalog.GetSubtypeLabel(EnemyType, Subtype);
    [Browsable(false)] public string FriendlyName => EslEnemyCatalog.GetFullName(EnemyType, Subtype);
    [Category("Enemy"), DisplayName("Trocar inimigo"), Description("Seleciona diretamente qualquer família e subtipo conhecido."), Editor(typeof(EslEnemyVariantEditor),typeof(UITypeEditor))]
    public string EnemyVariant
    {
        get => EslEnemyCatalog.GetVariantLabel(EnemyType,Subtype);
        set { if(EslEnemyCatalog.TryParseVariantLabel(value,out byte type,out byte subtype)){EnemyType=type;Subtype=subtype;} }
    }
    [Category("Enemy")] public byte Animation { get; set; }
    [Category("Enemy"), DisplayName("Sight Range")] public byte SightRange { get; set; }
    [Category("Equipment"), DisplayName("Equip 1 / Flag byte 5"), Description("Bit flags. Village mapping is decoded in Equipment Summary.")] public byte Equip1 { get; set; }
    [Category("Equipment"), DisplayName("Equip 2 / Flag byte 6"), Description("Bit flags. Includes dynamite/accessory/Plaga behavior on documented Village Ganados.")] public byte Equip2 { get; set; }
    [Category("Equipment"), DisplayName("Weapon / Flag byte 7"), Description("Bit flags. Documented Village values include 08=Sickle, 10=Chainsaw, 20=Axe, 40=Bucket, 80=Pitchfork.")] public byte Weapon { get; set; }
    [Category("Equipment"), DisplayName("Preset de arma"), Description("Escolhe uma arma conhecida e atualiza automaticamente os bytes relacionados."), Editor(typeof(EslWeaponPresetEditor),typeof(UITypeEditor))]
    public string WeaponPreset { get=>EnemyEquipmentCatalog.GetWeaponPresetName(this);set=>EnemyEquipmentCatalog.ApplyWeaponPreset(this,value); }
    [Category("Equipment"), DisplayName("Preset de equipamento"), Description("Escolhe um acessório conhecido e atualiza automaticamente Equip 1 e Equip 2."), Editor(typeof(EslEquipmentPresetEditor),typeof(UITypeEditor))]
    public string EquipmentPreset { get=>EnemyEquipmentCatalog.GetEquipmentPresetName(this);set=>EnemyEquipmentCatalog.ApplyEquipmentPreset(this,value); }
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
    [Browsable(false)] public byte Unknown4 { get; set; }
    [Category("Transform"), DisplayName("Tamanho individual"), Description("Multiplicador exclusivo deste inimigo (em09 ou em10–em4F). Automático preserva o tamanho original do jogo."), TypeConverter(typeof(EslIndividualScaleConverter))]
    public EslIndividualScaleValue IndividualScale
    {
        get => Enum.IsDefined(typeof(EslIndividualScaleValue),Unknown4) ? (EslIndividualScaleValue)Unknown4 : EslIndividualScaleValue.Automatico;
        set => Unknown4=(byte)value;
    }
    [Browsable(false)] public float IndividualScaleMultiplier => Unknown4 == 0 ? 1f : Unknown4 / 16f;
    [Category("Unknown"), DisplayName("Unknown 5")] public byte Unknown5 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 6")] public byte Unknown6 { get; set; }
    [Category("Unknown"), DisplayName("Unknown 7")] public byte Unknown7 { get; set; }
    [Browsable(false)] public Vector3 Position => new(PosX, PosY, PosZ);
    public override string ToString() => $"#{Index:D3}  {EslEnemyCatalog.GetFullName(EnemyType, Subtype)}  HP {Health}";
}

public enum EslIndividualScaleValue : byte
{
    Automatico=0, Scale050=8, Scale075=12, Scale100=16, Scale125=20,
    Scale150=24, Scale200=32, Scale250=40, Scale300=48
}

public sealed class EslIndividualScaleConverter : EnumConverter
{
    public EslIndividualScaleConverter():base(typeof(EslIndividualScaleValue)){}
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)
        => sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
    {
        if(value is string text)
        {
            text=text.Trim();
            foreach(EslIndividualScaleValue scale in Enum.GetValues<EslIndividualScaleValue>())
                if(string.Equals(Label(scale),text,StringComparison.CurrentCultureIgnoreCase))return scale;
        }
        return base.ConvertFrom(context,culture,value);
    }
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        => destinationType==typeof(string)&&value is EslIndividualScaleValue scale ? Label(scale) : base.ConvertTo(context,culture,value,destinationType);
    private static string Label(EslIndividualScaleValue scale)=>scale switch
        {
            EslIndividualScaleValue.Automatico=>"Automático (original)", EslIndividualScaleValue.Scale050=>"0,50×",
            EslIndividualScaleValue.Scale075=>"0,75×", EslIndividualScaleValue.Scale100=>"1,00×",
            EslIndividualScaleValue.Scale125=>"1,25×", EslIndividualScaleValue.Scale150=>"1,50×",
            EslIndividualScaleValue.Scale200=>"2,00×", EslIndividualScaleValue.Scale250=>"2,50×",
            EslIndividualScaleValue.Scale300=>"3,00×", _=>scale.ToString()
        };
}

public sealed class EslEnemyVariantConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)=>true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)=>true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)=>new(EslEnemyCatalog.AllVariants.Cast<object>().ToArray());
    public override bool CanConvertFrom(ITypeDescriptorContext? context,Type sourceType)=>sourceType==typeof(string)||base.CanConvertFrom(context,sourceType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)=>value is string text?text:base.ConvertFrom(context,culture,value);
}

public sealed class EslEnemyVariantEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)=>UITypeEditorEditStyle.DropDown;
    public override object? EditValue(ITypeDescriptorContext? context,IServiceProvider provider,object? value)
    {
        if(provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service)return value;
        string current=value as string??string.Empty;var list=new ListBox{BorderStyle=BorderStyle.None,IntegralHeight=true,Font=Control.DefaultFont};
        list.Items.AddRange(EslEnemyCatalog.AllVariants.Cast<object>().ToArray());list.SelectedItem=current;
        int width=EslEnemyCatalog.AllVariants.Select(x=>TextRenderer.MeasureText(x,list.Font).Width).DefaultIfEmpty(260).Max()+SystemInformation.VerticalScrollBarWidth+14;
        list.Size=new Size(Math.Clamp(width,280,760),Math.Min(420,Math.Max(80,list.PreferredHeight)));
        object? selected=value;list.Click+=(_,_)=>{selected=list.SelectedItem;service.CloseDropDown();};service.DropDownControl(list);return selected??value;
    }
}

public abstract class EslEquipmentPresetEditorBase : UITypeEditor
{
    protected abstract IReadOnlyList<string> GetItems(byte enemyType);
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context)=>UITypeEditorEditStyle.DropDown;
    public override object? EditValue(ITypeDescriptorContext? context,IServiceProvider provider,object? value)
    {
        if(provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service)return value;
        byte type=context?.Instance is EslEnemyEntry entry?entry.EnemyType:(byte)0x12;
        IReadOnlyList<string> items=GetItems(type);var list=new ListBox{BorderStyle=BorderStyle.None,IntegralHeight=true,Font=Control.DefaultFont};
        list.Items.AddRange(items.Cast<object>().ToArray());list.SelectedItem=value as string;
        int width=items.Select(x=>TextRenderer.MeasureText(x,list.Font).Width).DefaultIfEmpty(260).Max()+SystemInformation.VerticalScrollBarWidth+14;
        list.Size=new Size(Math.Clamp(width,280,650),Math.Min(360,Math.Max(80,list.PreferredHeight)));
        object? selected=value;list.Click+=(_,_)=>{selected=list.SelectedItem;service.CloseDropDown();};service.DropDownControl(list);return selected??value;
    }
}
public sealed class EslWeaponPresetEditor:EslEquipmentPresetEditorBase
{ protected override IReadOnlyList<string> GetItems(byte enemyType)=>EnemyEquipmentCatalog.GetWeaponPresetNames(enemyType); }
public sealed class EslEquipmentPresetEditor:EslEquipmentPresetEditorBase
{ protected override IReadOnlyList<string> GetItems(byte enemyType)=>EnemyEquipmentCatalog.GetEquipmentPresetNames(enemyType); }

public sealed class HexByteConverter : ByteConverter
{
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType)
        =>destinationType==typeof(string)&&value is byte b?$"0x{b:X2}":base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
    {
        if(value is string text)
        {
            text=text.Trim();if(text.StartsWith("0x",StringComparison.OrdinalIgnoreCase))text=text[2..];
            if(byte.TryParse(text,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out byte result))return result;
            throw new FormatException("Informe um byte hexadecimal entre 0x00 e 0xFF.");
        }
        return base.ConvertFrom(context,culture,value);
    }
}
