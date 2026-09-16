using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class SmdCatalogForm : AppForm
{
    private readonly SmdObjectCatalogService service;
    private readonly TextBox search=new(){PlaceholderText="Pesquisar objetos...",Dock=DockStyle.Fill,BorderStyle=BorderStyle.FixedSingle};
    private readonly ComboBox categories=new(){Width=170,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ListView list=new(){Dock=DockStyle.Fill,View=View.LargeIcon,MultiSelect=false,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(22,25,30),ForeColor=Color.Gainsboro,HideSelection=false};
    private readonly ImageList images=new(){ImageSize=new Size(144,112),ColorDepth=ColorDepth.Depth32Bit};
    private readonly PictureBox preview=new(){Dock=DockStyle.Top,Height=220,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(14,16,20)};
    private readonly Label title=new(){Dock=DockStyle.Top,Height=34,Font=new Font("Segoe UI Semibold",13),ForeColor=Color.White,Padding=new Padding(8,5,0,0)};
    private readonly Label details=new(){Dock=DockStyle.Fill,ForeColor=Color.Silver,Padding=new Padding(8),Font=new Font("Segoe UI",9)};
    private readonly Button use=new(){Text="INSERIR NO CENÁRIO",Dock=DockStyle.Bottom,Height=42,BackColor=Color.FromArgb(255,122,26),FlatStyle=FlatStyle.Flat,ForeColor=Color.White};
    private readonly Button delete=new(){Text="REMOVER DO CATÁLOGO",Dock=DockStyle.Bottom,Height=34,BackColor=Color.FromArgb(44,47,54),FlatStyle=FlatStyle.Flat,ForeColor=Color.Gainsboro};
    private List<SmdCatalogItem> items=new();
    public SmdCatalogItem? SelectedItem {get;private set;}
    public SmdCatalogForm(SmdObjectCatalogService service)
    {
        this.service=service;Text="Catálogo de Objetos SMD";Width=980;Height=650;MinimumSize=new Size(760,480);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(18,20,24);ForeColor=Color.White;
        var top=new TableLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(10,8,10,6),ColumnCount=2,BackColor=Color.FromArgb(30,33,39)};top.ColumnStyles.Add(new(SizeType.Percent,100));top.ColumnStyles.Add(new(SizeType.Absolute,180));top.Controls.Add(search,0,0);top.Controls.Add(categories,1,0);
        var split=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=680,BackColor=Color.FromArgb(36,39,45),FixedPanel=FixedPanel.Panel2};list.LargeImageList=images;split.Panel1.Padding=new Padding(8);split.Panel1.Controls.Add(list);split.Panel2.Padding=new Padding(8);split.Panel2.BackColor=Color.FromArgb(27,30,35);split.Panel2.Controls.Add(details);split.Panel2.Controls.Add(title);split.Panel2.Controls.Add(preview);split.Panel2.Controls.Add(delete);split.Panel2.Controls.Add(use);Controls.Add(split);Controls.Add(top);
        search.TextChanged+=(_,_)=>Filter();categories.SelectedIndexChanged+=(_,_)=>Filter();list.SelectedIndexChanged+=(_,_)=>ShowSelection();list.DoubleClick+=(_,_)=>Accept();use.Click+=(_,_)=>Accept();delete.Click+=(_,_)=>DeleteSelected();Shown+=(_,_)=>Reload();
    }
    private void Reload()
    {
        items=service.LoadAll().ToList();categories.Items.Clear();categories.Items.Add("Todas as categorias");foreach(string c in items.Select(x=>x.Category).Distinct().OrderBy(x=>x))categories.Items.Add(c);categories.SelectedIndex=0;Filter();
    }
    private void Filter()
    {
        if(categories.SelectedIndex<0)return;string query=search.Text.Trim();string? category=categories.SelectedIndex==0?null:categories.SelectedItem?.ToString();list.BeginUpdate();try{list.Items.Clear();images.Images.Clear();foreach(SmdCatalogItem item in items.Where(x=>(category==null||x.Category==category)&&(query.Length==0||x.Name.Contains(query,StringComparison.OrdinalIgnoreCase)||x.Notes.Contains(query,StringComparison.OrdinalIgnoreCase)))){using Image image=File.Exists(item.PreviewPath)?new Bitmap(item.PreviewPath):CreatePlaceholder();images.Images.Add(item.Id,image);list.Items.Add(new ListViewItem($"{item.Name}\n{item.Category}",item.Id){Tag=item});}}finally{list.EndUpdate();}ShowSelection();
    }
    private static Bitmap CreatePlaceholder(){var image=new Bitmap(144,112);using Graphics g=Graphics.FromImage(image);g.Clear(Color.FromArgb(38,42,49));using var font=new Font("Segoe UI Semibold",28);TextRenderer.DrawText(g,"SMD",font,new Rectangle(0,0,144,112),Color.FromArgb(255,122,26),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return image;}
    private void ShowSelection()
    {
        SmdCatalogItem? item=list.SelectedItems.Count==0?null:list.SelectedItems[0].Tag as SmdCatalogItem;use.Enabled=delete.Enabled=item!=null;title.Text=item?.Name??"Selecione um objeto";details.Text=item==null?"Sua biblioteca portátil de modelos SMD.":$"Categoria: {item.Category}\n\n{item.Entries.Count} entry(s) • {item.Bins.Count} modelo(s) • {item.Textures.Count} textura(s)\n\n{item.Notes}";Image? old=preview.Image;preview.Image=item!=null&&File.Exists(item.PreviewPath)?new Bitmap(item.PreviewPath):null;old?.Dispose();
    }
    private void Accept(){if(list.SelectedItems.Count==0)return;SelectedItem=(SmdCatalogItem)list.SelectedItems[0].Tag;DialogResult=DialogResult.OK;Close();}
    private void DeleteSelected()
    {
        if(list.SelectedItems.Count==0||list.SelectedItems[0].Tag is not SmdCatalogItem item)return;if(MessageBox.Show(this,$"Remover “{item.Name}” do catálogo?","Catálogo SMD",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;service.Delete(item);Reload();
    }
    protected override void Dispose(bool disposing){if(disposing){preview.Image?.Dispose();foreach(Image image in images.Images)image.Dispose();images.Dispose();}base.Dispose(disposing);}
}
