using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class ItaChildItemDialog : AppForm
{
    private static readonly Color Background = Color.FromArgb(14, 17, 22);
    private static readonly Color Card = Color.FromArgb(25, 29, 37);
    private static readonly Color Field = Color.FromArgb(34, 39, 49);
    private static readonly Color Muted = Color.FromArgb(151, 160, 176);
    private static readonly Color Accent = Color.FromArgb(211, 55, 67);
    private readonly ComboBox item = new();
    private readonly NumericUpDown amount = new();
    private readonly CheckBox random = new();
    private readonly ComboBox aura = new();

    public byte ItemId => random.Checked ? (byte)0 : item.SelectedItem is ItemChoice choice ? choice.Id : (byte)0;
    public ushort Amount => random.Checked ? (ushort)1 : (ushort)amount.Value;
    public byte Randomness => random.Checked ? (byte)0x10 : (byte)0x00;
    public byte AuraType => aura.SelectedItem is AuraChoice choice ? choice.Id : (byte)0;
    public bool HasItems => item.Items.Count > 0;

    public ItaChildItemDialog()
    {
        Text = "Adicionar item ao objeto";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(580, 386);
        BackColor = Background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        Controls.Add(new Label { Text = "NOVO ITEM NO OBJETO", Left = 26, Top = 22, Width = 500, Height = 30, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Color.White });
        Controls.Add(new Label { Text = "O item ficará vinculado ao objeto e usará a posição dele (offset 0, 0, 0).", Left = 28, Top = 56, Width = 520, Height = 25, ForeColor = Muted });

        Panel card = new() { Left = 24, Top = 92, Width = 532, Height = 194, BackColor = Card };
        card.Controls.Add(new Label { Text = "CONFIGURAÇÃO DO ITEM", Left = 18, Top = 15, Width = 480, Height = 22, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(220, 225, 234) });
        LabelAt(card, "Item", 20, 54);
        ConfigureCombo(item, 112, 48, 392);
        foreach (byte id in ItaItemCatalog.ItemIds) item.Items.Add(new ItemChoice(id, ItaItemCatalog.GetName(id)));
        if (item.Items.Count > 0) item.SelectedIndex = 0;
        card.Controls.Add(item);

        LabelAt(card, "Quantidade", 20, 101);
        amount.SetBounds(112, 94, 130, 30);
        amount.Minimum = 1;
        amount.Maximum = 65000;
        amount.Value = 1;
        amount.BackColor = Field;
        amount.ForeColor = Color.White;
        card.Controls.Add(amount);

        random.Text = "Item aleatório";
        random.SetBounds(278, 95, 220, 28);
        random.AutoSize = true;
        random.ForeColor = Color.Gainsboro;
        random.BackColor = Card;
        random.FlatStyle = FlatStyle.Flat;
        random.CheckedChanged += (_, _) => { item.Enabled = !random.Checked; amount.Enabled = !random.Checked; };
        card.Controls.Add(random);

        LabelAt(card, "Aura / brilho", 20, 151);
        ConfigureCombo(aura, 112, 145, 392);
        aura.Items.AddRange(new object[]
        {
            new AuraChoice(0x00, "Normal — sem brilho"),
            new AuraChoice(0x01, "Brilho pequeno"),
            new AuraChoice(0x02, "Branco — faísca inferior"),
            new AuraChoice(0x03, "Azul"),
            new AuraChoice(0x04, "Verde"),
            new AuraChoice(0x05, "Vermelho"),
            new AuraChoice(0x06, "Automático"),
            new AuraChoice(0x07, "Branco intenso"),
            new AuraChoice(0x08, "Branco — faísca superior"),
            new AuraChoice(0x09, "Amarelo intenso")
        });
        aura.SelectedIndex = 0;
        card.Controls.Add(aura);
        Controls.Add(card);

        Controls.Add(new Label { Text = "A mesma configuração será aplicada a cada objeto selecionado.", Left = 28, Top = 302, Width = 500, Height = 22, ForeColor = Muted });
        Button cancel = MakeButton("CANCELAR", Field, 296, 336, 108);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Button add = MakeButton("ADICIONAR ITEM", Accent, 416, 336, 140);
        add.Enabled = HasItems;
        add.Click += (_, _) => { if (!HasItems) return; DialogResult = DialogResult.OK; Close(); };
        Controls.Add(cancel);
        Controls.Add(add);
        CancelButton = cancel;
        AcceptButton = add;
    }

    private static void LabelAt(Control parent, string text, int x, int y) => parent.Controls.Add(new Label { Text = text, Left = x, Top = y, AutoSize = true, ForeColor = Muted });
    private static void ConfigureCombo(ComboBox combo, int x, int y, int width)
    {
        combo.SetBounds(x, y, width, 31);
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = Field;
        combo.ForeColor = Color.White;
        combo.FlatStyle = FlatStyle.Flat;
    }
    private static Button MakeButton(string text, Color color, int x, int y, int width) => new() { Text = text, Left = x, Top = y, Width = width, Height = 36, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, UseVisualStyleBackColor = false };

    private sealed record ItemChoice(byte Id, string Label) { public override string ToString() => Label; }
    private sealed record AuraChoice(byte Id, string Label) { public override string ToString() => Label; }
}
