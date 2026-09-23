namespace RE4_PS2_MOD_WORKSPACE;

internal enum SmdModelTextureImportMode { Preserve, Append, ReplaceShared }

internal sealed class SmdModelImportTextureDialog : AppForm
{
    private readonly RadioButton preserve = new(), append = new(), replace = new();
    private readonly CheckBox createNewEntry = new();
    public SmdModelTextureImportMode Mode => append.Checked ? SmdModelTextureImportMode.Append : replace.Checked ? SmdModelTextureImportMode.ReplaceShared : SmdModelTextureImportMode.Preserve;
    public bool CreateNewEntry => createNewEntry.Checked;
    public SmdModelImportTextureDialog(IReadOnlyList<ExternalModelMaterialTexture> textures, IReadOnlyList<byte> receiverTextures)
    {
        Text="Texturas do modelo";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;ShowInTaskbar=false;ClientSize=new Size(650,430);BackColor=Color.FromArgb(18,20,24);ForeColor=Color.Gainsboro;Font=new Font("Segoe UI",9F);
        string detected=textures.Count==0?"Nenhuma imagem foi encontrada no MTL ou ao lado do modelo.":string.Join(Environment.NewLine,textures.Select((x,i)=>$"Material {x.MaterialIndex+1}: {x.MaterialName} -> {Path.GetFileName(x.ImagePath)}"));string current=receiverTextures.Count==0?"nenhuma":string.Join(", ",receiverTextures.Select(x=>x.ToString("D3")));
        var heading=new Label{Text="COMO TRATAR AS TEXTURAS?",AutoSize=true,Font=new Font(Font,FontStyle.Bold),ForeColor=Color.White};var details=new Label{Text=$"Texturas detectadas:\r\n{detected}\r\n\r\nIndices usados atualmente pelo BIN receptor: {current}",AutoSize=false,Height=105,ForeColor=Color.FromArgb(175,184,199)};
        Configure(preserve,"SOMENTE O MODELO (RECOMENDADO)\r\nMantem o TPL intacto e faz o novo BIN usar as texturas atuais do receptor.",true);Configure(append,"MODELO + NOVAS TEXTURAS\r\nAdiciona as imagens ao final do TPL e aponta somente o novo BIN para elas.",false);Configure(replace,"SOBRESCREVER TEXTURAS GLOBAIS\r\nSubstitui os indices atuais. Outros modelos que compartilham esses indices tambem mudarao.",false);append.Enabled=replace.Enabled=textures.Count>0;
        ConfigureNewEntry();
        var ok=new Button{Text="CONTINUAR",DialogResult=DialogResult.OK,Width=125,Height=34,BackColor=Color.FromArgb(183,45,51),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};var cancel=new Button{Text="CANCELAR",DialogResult=DialogResult.Cancel,Width=105,Height=34,BackColor=Color.FromArgb(43,47,56),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};ok.FlatAppearance.BorderSize=cancel.FlatAppearance.BorderSize=0;var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};buttons.Controls.Add(ok);buttons.Controls.Add(cancel);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),RowCount=7,ColumnCount=1};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,110));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,62));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,62));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.Controls.Add(heading,0,0);layout.Controls.Add(details,0,1);layout.Controls.Add(createNewEntry,0,2);layout.Controls.Add(preserve,0,3);layout.Controls.Add(append,0,4);layout.Controls.Add(replace,0,5);layout.Controls.Add(buttons,0,6);Controls.Add(layout);AcceptButton=ok;CancelButton=cancel;
    }
    private void ConfigureNewEntry(){createNewEntry.Text="Criar um novo BIN e uma nova entry para o modelo importado (recomendado)";createNewEntry.Checked=true;createNewEntry.Dock=DockStyle.Fill;createNewEntry.ForeColor=Color.White;createNewEntry.BackColor=Color.FromArgb(31,34,41);createNewEntry.Padding=new Padding(8,2,0,2);}
    private static void Configure(RadioButton radio,string text,bool selected){radio.Text=text;radio.Checked=selected;radio.Dock=DockStyle.Fill;radio.Appearance=Appearance.Button;radio.FlatStyle=FlatStyle.Flat;radio.FlatAppearance.BorderColor=Color.FromArgb(68,74,87);radio.FlatAppearance.CheckedBackColor=Color.FromArgb(65,42,45);radio.BackColor=Color.FromArgb(31,34,41);radio.ForeColor=Color.Gainsboro;radio.Padding=new Padding(12,3,8,3);radio.TextAlign=ContentAlignment.MiddleLeft;}
}
internal sealed record ExternalModelMaterialTexture(int MaterialIndex,string MaterialName,string ImagePath);
