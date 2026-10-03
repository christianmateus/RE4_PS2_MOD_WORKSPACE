using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

internal sealed class SmdCatalogMetadataDialog:AppForm
{
    private readonly SmdObjectCatalogService service;
    private readonly TextBox nameBox=new(){Dock=DockStyle.Fill,MaxLength=180};
    private readonly ComboBox categoryBox=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    private readonly TextBox notesBox=new(){Dock=DockStyle.Fill,Multiline=true,ScrollBars=ScrollBars.Vertical,PlaceholderText="Detalhes, origem do modelo ou observações…"};
    public bool IncludeCollision=>includeCollision.Checked;
    private readonly CheckBox includeCollision=new(){Text="Incluir colisão SAT / EAT",AutoSize=true};
    private readonly Label collisionSummary=new(){AutoSize=true,ForeColor=Color.Silver,Text="Nenhuma face associada"};
    public void SetCollisionCount(int sat,int eat){collisionSummary.Text=$"{sat} faces SAT • {eat} faces EAT";}
    public string ItemName=>nameBox.Text.Trim();
    public string Category=>categoryBox.SelectedItem?.ToString()??"Cenário";
    public string Notes=>notesBox.Text.Trim();
    public SmdCatalogMetadataDialog(SmdObjectCatalogService service,IReadOnlyList<ScenarioEntry> entries)
    {
        this.service=service;Text="Adicionar ao Catálogo SMD";ClientSize=new Size(620,610);MinimumSize=new Size(620,610);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(20,22,27);ForeColor=Color.Gainsboro;Font=new Font("Segoe UI",9);MaximizeBox=false;
        LanguageService.PreserveContent(categoryBox);
        nameBox.Text=entries.Count==1?$"Objeto BIN {entries[0].BinId:D3}":$"Conjunto SMD ({entries.Count})";
        foreach(Control input in new Control[]{nameBox,categoryBox,notesBox}){input.BackColor=Color.FromArgb(34,38,45);input.ForeColor=Color.White;input.Font=new Font("Segoe UI",10);}
        var header=new Panel{Dock=DockStyle.Top,Height=96,Padding=new Padding(24,16,24,10)};
        header.Controls.Add(new Label{Text="ADICIONAR À BIBLIOTECA",Dock=DockStyle.Top,Height=34,Font=new Font("Segoe UI Semibold",16),ForeColor=Color.White});
        header.Controls.Add(new Label{Text=entries.Count==1?$"Entry #{entries[0].FileOrder:D3} • modelo reutilizável com suas texturas":$"{entries.Count} entries • conjunto reutilizável com suas texturas",Dock=DockStyle.Bottom,Height=26,ForeColor=Color.Silver});
        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(24,8,24,18)};content.ColumnStyles.Add(new(SizeType.Percent,100));
        foreach(float height in new[]{26f,38f,32f,40f,34f})content.RowStyles.Add(new(SizeType.Absolute,height));content.RowStyles.Add(new(SizeType.Percent,100));
        Label Caption(string text)=>new(){Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(160,173,192),Font=new Font("Segoe UI Semibold",9)};
        content.Controls.Add(Caption("NOME DO OBJETO"),0,0);content.Controls.Add(nameBox,0,1);content.Controls.Add(Caption("CATEGORIA"),0,2);
        var categoryRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};categoryRow.ColumnStyles.Add(new(SizeType.Percent,100));categoryRow.ColumnStyles.Add(new(SizeType.Absolute,130));categoryRow.Controls.Add(categoryBox,0,0);var add=Button("+ NOVA",Color.FromArgb(48,55,67));add.Dock=DockStyle.Fill;add.Click+=(_,_)=>AddCategory();categoryRow.Controls.Add(add,1,0);content.Controls.Add(categoryRow,0,3);content.Controls.Add(Caption("DESCRIÇÃO  •  opcional"),0,4);content.Controls.Add(notesBox,0,5);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=70,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(20,12,20,12),BackColor=Color.FromArgb(26,29,35)};
        var save=Button("ADICIONAR AO CATÁLOGO",Color.FromArgb(188,53,62));save.Width=225;var cancel=Button("CANCELAR",Color.FromArgb(45,50,59));cancel.Width=110;cancel.DialogResult=DialogResult.Cancel;
        save.Click+=(_,_)=>{if(ItemName.Length==0){nameBox.Focus();MessageBox.Show(this,"Informe um nome para o objeto.",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);return;}DialogResult=DialogResult.OK;Close();};footer.Controls.Add(save);footer.Controls.Add(cancel);
        var collisionPanel=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100,Padding=new Padding(24,8,24,8),BackColor=Color.FromArgb(28,32,39),FlowDirection=FlowDirection.TopDown,WrapContents=false};
        var choose=Button("SELECIONAR COLISÃO NO CENÁRIO",Color.FromArgb(48,55,67));choose.Width=330;choose.Enabled=false;includeCollision.CheckedChanged+=(_,_)=>choose.Enabled=includeCollision.Checked;choose.Click+=(_,_)=>{DialogResult=DialogResult.Retry;Close();};
        collisionPanel.Controls.Add(includeCollision);collisionPanel.Controls.Add(choose);collisionPanel.Controls.Add(collisionSummary);
        Controls.Add(content);Controls.Add(collisionPanel);Controls.Add(footer);Controls.Add(header);AcceptButton=save;CancelButton=cancel;RefreshCategories("Cenário");Shown+=(_,_)=>{nameBox.Focus();nameBox.SelectAll();};
    }
    private static Button Button(string text,Color background){var b=new Button{Text=text,Dock=DockStyle.None,Height=38,Width=118,FlatStyle=FlatStyle.Flat,BackColor=background,ForeColor=Color.White,Margin=new Padding(6,0,0,0)};b.FlatAppearance.BorderSize=0;return b;}
    private void RefreshCategories(string selected){categoryBox.Items.Clear();foreach(string c in service.GetCategories())categoryBox.Items.Add(c);categoryBox.SelectedItem=selected;if(categoryBox.SelectedIndex<0)categoryBox.SelectedIndex=0;}
    private void AddCategory()
    {
        using var dialog=new AppForm{Text="Nova categoria",ClientSize=new Size(440,190),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=BackColor,ForeColor=ForeColor,Font=Font};
        var input=new TextBox{Left=22,Top=63,Width=396,MaxLength=100,BackColor=Color.FromArgb(34,38,45),ForeColor=Color.White,Font=new Font("Segoe UI",11)};
        dialog.Controls.Add(new Label{Text="Nome da nova categoria",Left=22,Top=23,Width=380,Height=30,Font=new Font("Segoe UI Semibold",12),ForeColor=Color.White});dialog.Controls.Add(input);
        var save=Button("CRIAR CATEGORIA",Color.FromArgb(188,53,62));save.Dock=DockStyle.None;save.Width=175;save.Location=new Point(243,122);var cancel=Button("CANCELAR",Color.FromArgb(45,50,59));cancel.Dock=DockStyle.None;cancel.Location=new Point(115,122);cancel.DialogResult=DialogResult.Cancel;
        save.Click+=(_,_)=>{try{string category=service.AddCategory(input.Text);RefreshCategories(category);dialog.DialogResult=DialogResult.OK;dialog.Close();}catch(Exception ex){MessageBox.Show(dialog,ex.Message,"Nova categoria",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
        dialog.Controls.Add(save);dialog.Controls.Add(cancel);dialog.AcceptButton=save;dialog.CancelButton=cancel;dialog.Shown+=(_,_)=>input.Focus();dialog.ShowLocalizedDialog(this);
    }
}
