using RE4_PS2_MOD_WORKSPACE.Core.Visual;
namespace RE4_PS2_MOD_WORKSPACE;
internal sealed class CharacterSmdExportDialog:AppForm
{
    private readonly ListView parts=new(){Dock=DockStyle.Fill,View=View.Details,CheckBoxes=true,FullRowSelect=true,HideSelection=false,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(23,27,33),ForeColor=Color.White};
    private readonly CheckBox animation=new(){Text="Incluir a animação selecionada (SMD separado, 30 FPS)",AutoSize=true};
    private readonly Label count=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.Silver};
    public IReadOnlyList<EnemyModelPart> SelectedParts=>parts.CheckedItems.Cast<ListViewItem>().Select(i=>(EnemyModelPart)i.Tag!).ToArray();public bool IncludeAnimation=>animation.Checked&&animation.Enabled;
    public CharacterSmdExportDialog(IReadOnlyList<EnemyModelPart> source,IReadOnlySet<int> visible,bool hasAnimation,IReadOnlyDictionary<int,string>? names=null)
    {
        Text="Exportar partes para Blender";ClientSize=new Size(730,610);MinimumSize=new Size(680,550);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(20,23,29);ForeColor=Color.White;Font=new Font("Segoe UI",9);LanguageService.PreserveContent(parts);
        var header=new Panel{Dock=DockStyle.Top,Height=100,Padding=new Padding(22,16,22,10)};header.Controls.Add(new Label{Text="PARTES PARA EXPORTAR",Dock=DockStyle.Top,Height=36,Font=new Font("Segoe UI Semibold",17)});header.Controls.Add(new Label{Text="Um SMD por BIN • esqueleto próprio, pesos, materiais e texturas PNG",Dock=DockStyle.Bottom,Height=30,ForeColor=Color.Silver});
        parts.Columns.Add("Parte",280);parts.Columns.Add("DAT",70);parts.Columns.Add("Faces",100);parts.Columns.Add("TPL",70);foreach(var p in source.Where(p=>p.Triangles.Count>0)){var row=new ListViewItem($"BIN {p.BinIndex:D2}"+(names?.TryGetValue(p.BinIndex,out var name)==true?$" • {name}":"")){Tag=p,Checked=visible.Contains(p.BinIndex)};row.SubItems.Add(p.DatEntryIndex.ToString("D3"));row.SubItems.Add(p.Triangles.Count.ToString("N0"));row.SubItems.Add(p.TplEntryIndex.ToString("D3"));parts.Items.Add(row);}
        var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42,Padding=new Padding(18,0,18,0)};
        Button B(string text,Color? color=null)=>new(){Text=text,Height=34,Width=120,FlatStyle=FlatStyle.Flat,BackColor=color??Color.FromArgb(43,49,59),ForeColor=Color.White,Margin=new Padding(4)};
        foreach(var setting in new[]{("VISÍVEIS",0),("TODAS",1),("NENHUMA",2)}){var button=B(setting.Item1);button.Click+=(_,_)=>{foreach(ListViewItem row in parts.Items)row.Checked=setting.Item2==1||setting.Item2==0&&visible.Contains(((EnemyModelPart)row.Tag!).BinIndex);};tools.Controls.Add(button);}
        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(22,8,22,8)};body.Controls.Add(parts);
        var options=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=82,ColumnCount=1,RowCount=2,Padding=new Padding(22,4,22,4)};options.RowStyles.Add(new(SizeType.Percent,50));options.RowStyles.Add(new(SizeType.Percent,50));animation.Enabled=hasAnimation;animation.Checked=hasAnimation;options.Controls.Add(animation,0,0);options.Controls.Add(count,0,1);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=66,Padding=new Padding(18,8,18,8),FlowDirection=FlowDirection.RightToLeft,BackColor=Color.FromArgb(27,31,39)};var export=B("EXPORTAR SMD",Color.FromArgb(188,53,62));export.Width=175;var cancel=B("CANCELAR");cancel.DialogResult=DialogResult.Cancel;export.Click+=(_,_)=>{if(SelectedParts.Count==0)return;DialogResult=DialogResult.OK;Close();};footer.Controls.Add(export);footer.Controls.Add(cancel);AcceptButton=export;CancelButton=cancel;
        void Update(){int n=parts.CheckedItems.Count;count.Text=$"{n} parte(s) • a pose da animação não será aplicada à malha exportada";export.Enabled=n>0;}parts.ItemChecked+=(_,_)=>Update();Update();Controls.Add(body);Controls.Add(tools);Controls.Add(options);Controls.Add(footer);Controls.Add(header);
    }
}
