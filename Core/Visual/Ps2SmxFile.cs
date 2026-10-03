using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms.Design;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

[Flags]
public enum SmxDisplayFlags : uint
{
    None = 0,
    Shadow = 1 << 0,
    VertexColor = 1 << 1,
    CastOn = 1 << 2,
    AlphaTestOff = 1 << 3,
    DisplayBit4 = 1 << 4,
    AttributeBit0 = 1 << 5
}

public sealed class Ps2SmxFile
{
    public const int HeaderSize = 0x10;
    public const int RecordSize = 0x90;
    private readonly byte[] original;

    private Ps2SmxFile(string sourcePath, byte[] original, List<SmxRecord> records)
    {
        SourcePath = sourcePath;
        this.original = original;
        Records = records;
        foreach (SmxRecord record in records) record.Owner = this;
    }

    public string SourcePath { get; }
    public List<SmxRecord> Records { get; }
    public bool IsModified { get; internal set; }

    public static Ps2SmxFile Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < HeaderSize || data[0] != HeaderSize)
            throw new InvalidDataException("Cabeçalho SMX inválido.");
        int count = data[1];
        long required = HeaderSize + (long)count * RecordSize;
        if (required > data.Length) throw new InvalidDataException("Tabela de registros SMX incompleta.");
        var records = new List<SmxRecord>(count);
        var ids = new HashSet<byte>();
        for (int i = 0; i < count; i++)
        {
            byte[] raw = data.AsSpan(HeaderSize + i * RecordSize, RecordSize).ToArray();
            if (!ids.Add(raw[0])) throw new InvalidDataException($"O SMX contém o ID duplicado 0x{raw[0]:X2}.");
            records.Add(new SmxRecord(raw));
        }
        return new Ps2SmxFile(path, data, records);
    }

    public SmxRecord? Find(byte id) => Records.FirstOrDefault(x => x.Id == id);

    public SmxRecord AddDefault(byte id)
    {
        if (id > 0xF9) throw new InvalidOperationException("O jogo aceita IDs SMX somente entre 0x00 e 0xF9.");
        if (Find(id) != null) throw new InvalidOperationException($"Já existe um registro SMX para o ID 0x{id:X2}.");
        byte[] raw = new byte[RecordSize];
        raw[0] = id;
        raw[2] = 3;
        // cLightInfo starts with SelectMask = 0: no room light is excluded.
        // Using uint.MaxValue here would make a newly created SMX block every light in-game.
        BitConverter.GetBytes(0u).CopyTo(raw, 4);
        raw[0x0C] = raw[0x0D] = raw[0x0E] = 0xFF;
        var record = new SmxRecord(raw) { Owner = this };
        Records.Add(record);
        Records.Sort((a, b) => a.Id.CompareTo(b.Id));
        IsModified = true;
        return record;
    }

    public bool Remove(byte id)
    {
        SmxRecord? record = Find(id);
        if (record == null) return false;
        Records.Remove(record);
        IsModified = true;
        return true;
    }

    public void Save(string? backupPath = null)
    {
        if (Records.Count > byte.MaxValue) throw new InvalidDataException("O SMX aceita no máximo 255 registros.");
        if (Records.Select(x => x.Id).Distinct().Count() != Records.Count)
            throw new InvalidDataException("Não é possível salvar um SMX com IDs duplicados.");
        if (!string.IsNullOrWhiteSpace(backupPath) && !File.Exists(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            File.Copy(SourcePath, backupPath);
        }
        int used = HeaderSize + Records.Count * RecordSize;
        int oldUsed = HeaderSize + original[1] * RecordSize;
        int padding = Math.Max(0, original.Length - oldUsed);
        byte[] output = new byte[used + padding];
        Buffer.BlockCopy(original, 0, output, 0, Math.Min(HeaderSize, original.Length));
        output[0] = HeaderSize;
        output[1] = checked((byte)Records.Count);
        for (int i = 0; i < Records.Count; i++) Buffer.BlockCopy(Records[i].RawData, 0, output, HeaderSize + i * RecordSize, RecordSize);
        if (padding > 0) Buffer.BlockCopy(original, oldUsed, output, used, padding);
        string temporary = SourcePath + ".smx_write_tmp";
        try { File.WriteAllBytes(temporary, output); File.Move(temporary, SourcePath, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        IsModified = false;
    }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class SmxRecord
{
    internal SmxRecord(byte[] raw) => RawData = raw.Length == Ps2SmxFile.RecordSize ? raw : throw new ArgumentException("Registro SMX inválido.", nameof(raw));
    internal byte[] RawData { get; }
    internal Ps2SmxFile? Owner { get; set; }

    private void Changed() { if (Owner != null) Owner.IsModified = true; }
    private uint U32(int offset) => BitConverter.ToUInt32(RawData, offset);
    private void U32(int offset, uint value) { BitConverter.GetBytes(value).CopyTo(RawData, offset); Changed(); }
    private float F32(int offset) => BitConverter.ToSingle(RawData, offset);
    private void F32(int offset, float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); BitConverter.GetBytes(value).CopyTo(RawData, offset); Changed(); }
    private void Byte(int offset, byte value) { RawData[offset] = value; Changed(); }

    [Category("SMX • Identidade"), DisplayName("ID"), ReadOnly(true), Description("ID associado ao byte SMX ID da entry SMD.")]
    public byte Id => RawData[0];
    [Category("SMX • Comportamento"), DisplayName("Type")]
    public byte Type { get => RawData[1]; set => Byte(1, value); }
    [Category("SMX • Renderização"), DisplayName("Order Type")]
    public byte OrderType { get => RawData[2]; set => Byte(2, value); }
    [Category("SMX • Renderização"), DisplayName("Cull Mode")]
    public byte CullMode { get => RawData[3]; set => Byte(3, value); }
    [Category("SMX • Iluminação"), DisplayName("Light Exclusion Mask"), Description("Máscara de exclusão usada pelo jogo: cada bit 1 bloqueia a luz correspondente; cada bit 0 permite que ela afete o modelo.")]
    public uint LightSelectMask { get => U32(4); set => U32(4, value); }
    [Category("SMX • Renderização"), DisplayName("Flags"), Description("Ativa uma ou mais flags de renderização do registro SMX."), TypeConverter(typeof(SmxDisplayFlagsConverter)), Editor(typeof(SmxDisplayFlagsEditor), typeof(UITypeEditor))]
    public SmxDisplayFlags Flags { get => (SmxDisplayFlags)U32(8); set => U32(8, (uint)value); }
    [Category("SMX • Cor"), DisplayName("Cor principal")]
    public Color PrimaryColor { get => Color.FromArgb(255, RawData[0x0C], RawData[0x0D], RawData[0x0E]); set { RawData[0x0C] = value.R; RawData[0x0D] = value.G; RawData[0x0E] = value.B; Changed(); } }
    [Category("SMX • Cor"), DisplayName("Blend Mode"), Description("Byte alpha da cor principal utilizado pelo jogo como modo de blend.")]
    public byte BlendMode { get => RawData[0x0F]; set => Byte(0x0F, value); }
    [Category("SMX • Cor"), DisplayName("Cor secundária")]
    public Color SecondaryColor { get => Color.FromArgb(255, RawData[0x84], RawData[0x85], RawData[0x86]); set { RawData[0x84] = value.R; RawData[0x85] = value.G; RawData[0x86] = value.B; Changed(); } }
    [Category("SMX • Cor"), DisplayName("Cor secundária • controle")]
    public byte SecondaryControl { get => RawData[0x87]; set => Byte(0x87, value); }
    [Category("SMX • UV Scroll"), DisplayName("Velocidade U")]
    public float UvScrollU { get => F32(0x88); set => F32(0x88, value); }
    [Category("SMX • UV Scroll"), DisplayName("Velocidade V")]
    public float UvScrollV { get => F32(0x8C); set => F32(0x8C, value); }
    [Category("SMX • Rotação (Type 1)"), DisplayName("Velocidade X"), Description("Velocidade de rotação no eixo X. Usado por registros SMX Type 1.")]
    public float RotationSpeedX { get => F32(0x10); set => F32(0x10, value); }
    [Category("SMX • Rotação (Type 1)"), DisplayName("Velocidade Y"), Description("Velocidade de rotação no eixo Y. Usado por registros SMX Type 1.")]
    public float RotationSpeedY { get => F32(0x14); set => F32(0x14, value); }
    [Category("SMX • Rotação (Type 1)"), DisplayName("Velocidade Z"), Description("Velocidade de rotação no eixo Z. Usado por registros SMX Type 1.")]
    public float RotationSpeedZ { get => F32(0x18); set => F32(0x18, value); }
    [Category("SMX • Rotação (Type 1)"), DisplayName("Rotação local"), Description("Quando ativado, aplica a rotação nos eixos locais do modelo. Usado por registros SMX Type 1.")]
    public bool LocalRotation { get => MathF.Abs(F32(0x1C)) > 0.000001f; set => F32(0x1C, value ? 1f : 0f); }
    [Category("SMX • Diagnóstico"), DisplayName("Work data"), ReadOnly(true), Description("Bloco comportamental preservado integralmente. O significado depende do Type.")]
    public string WorkData => Convert.ToHexString(RawData, 0x10, 0x74);

    public override string ToString() => $"ID 0x{Id:X2} • Type 0x{Type:X2}";
}

public sealed class SmxDisplayFlagsEditor : UITypeEditor
{
    private static readonly (SmxDisplayFlags Value, string Label)[] Choices =
    {
        (SmxDisplayFlags.Shadow, "Shadow"),
        (SmxDisplayFlags.VertexColor, "Vertex Color (definido pelo BIN)"),
        (SmxDisplayFlags.CastOn, "Cast On"),
        (SmxDisplayFlags.AlphaTestOff, "Alpha Test Off"),
        (SmxDisplayFlags.DisplayBit4, "Display Flag 0x8000"),
        (SmxDisplayFlags.AttributeBit0, "Object Attribute bit 0")
    };

    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.DropDown;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service) return value;

        SmxDisplayFlags original = value is SmxDisplayFlags flags ? flags : SmxDisplayFlags.None;
        using var panel = new Panel { Width = 230, Height = 170, BackColor = Color.FromArgb(28, 31, 37) };
        using var list = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            CheckOnClick = true,
            BackColor = panel.BackColor,
            ForeColor = Color.Gainsboro,
            Font = new Font("Segoe UI", 9F)
        };
        using var apply = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Text = "Aplicar",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 49, 58),
            ForeColor = Color.White
        };

        foreach ((SmxDisplayFlags flag, string label) in Choices)
            list.Items.Add(new FlagChoice(flag, label), (original & flag) != 0);

        SmxDisplayFlags result = original;
        apply.Click += (_, _) =>
        {
            const SmxDisplayFlags known = SmxDisplayFlags.Shadow | SmxDisplayFlags.VertexColor | SmxDisplayFlags.CastOn |
                                          SmxDisplayFlags.AlphaTestOff | SmxDisplayFlags.DisplayBit4 | SmxDisplayFlags.AttributeBit0;
            result &= ~known;
            for (int i = 0; i < list.Items.Count; i++)
                if (list.GetItemChecked(i) && list.Items[i] is FlagChoice choice) result |= choice.Value;
            service.CloseDropDown();
        };
        panel.Controls.Add(list);
        panel.Controls.Add(apply);
        service.DropDownControl(panel);
        return result;
    }

    private sealed record FlagChoice(SmxDisplayFlags Value, string Label)
    {
        public override string ToString() => Label;
    }
}

public sealed class SmxDisplayFlagsConverter : TypeConverter
{
    private static readonly (SmxDisplayFlags Value, string Label)[] Choices =
    {
        (SmxDisplayFlags.Shadow, "Shadow"),
        (SmxDisplayFlags.VertexColor, "Vertex Color (definido pelo BIN)"),
        (SmxDisplayFlags.CastOn, "Cast On"),
        (SmxDisplayFlags.AlphaTestOff, "Alpha Test Off"),
        (SmxDisplayFlags.DisplayBit4, "Display Flag 0x8000"),
        (SmxDisplayFlags.AttributeBit0, "Object Attribute bit 0")
    };

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is SmxDisplayFlags flags)
        {
            if (flags == SmxDisplayFlags.None) return "Nenhum";
            var labels = Choices.Where(x => (flags & x.Value) != 0).Select(x => x.Label).ToList();
            uint known = Choices.Aggregate(0u, (mask, x) => mask | (uint)x.Value);
            uint unknown = (uint)flags & ~known;
            if (unknown != 0) labels.Add($"0x{unknown:X8}");
            return string.Join(", ", labels);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text)
        {
            text = text.Trim();
            if (text.Length == 0 || text.Equals("Nenhum", StringComparison.OrdinalIgnoreCase) || text.Equals("None", StringComparison.OrdinalIgnoreCase))
                return SmxDisplayFlags.None;

            SmxDisplayFlags result = SmxDisplayFlags.None;
            foreach (string partValue in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string part = partValue.Trim();
                var choice = Choices.FirstOrDefault(x => x.Label.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (choice.Value != SmxDisplayFlags.None) { result |= choice.Value; continue; }
                if (Enum.TryParse(part.Replace(" ", string.Empty), true, out SmxDisplayFlags parsed)) { result |= parsed; continue; }
                if (part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && uint.TryParse(part[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint raw))
                { result |= (SmxDisplayFlags)raw; continue; }
                throw new FormatException($"'{part}' não é uma flag SMX válida.");
            }
            return result;
        }
        return base.ConvertFrom(context, culture, value);
    }
}
