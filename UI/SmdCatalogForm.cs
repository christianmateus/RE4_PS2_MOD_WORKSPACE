using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class SmdCatalogForm : AppForm
{
    private readonly SmdObjectCatalogService service;
    private readonly TextBox search=new(){PlaceholderText="Pesquisar por nome ou descrição…",Dock=DockStyle.Fill,BorderStyle=BorderStyle.FixedSingle};
    private readonly ComboBox categories=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    private readonly ListView list=new(){Dock=DockStyle.Fill,View=View.LargeIcon,MultiSelect=false,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(22,25,30),ForeColor=Color.Gainsboro,HideSelection=false};
    private readonly ImageList images=new(){ColorDepth=ColorDepth.Depth32Bit};
    private readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(14,16,20)};
    private readonly Label title=new(){Dock=DockStyle.Fill,Font=new Font("Segoe UI Semibold",13),ForeColor=Color.White,AutoEllipsis=true,Padding=new Padding(0,8,0,0)};
    private readonly Label categoryLabel=new(){Dock=DockStyle.Fill,ForeColor=Color.FromArgb(255,170,90),AutoEllipsis=true};
    private readonly TextBox details=new(){Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(27,30,35),ForeColor=Color.Silver};
    private readonly Button use=new(){Text="INSERIR NO CENÁRIO",Dock=DockStyle.Fill,BackColor=Color.FromArgb(255,122,26),FlatStyle=FlatStyle.Flat,ForeColor=Color.White};
    private readonly Button delete=new(){Text="REMOVER DO CATÁLOGO",Dock=DockStyle.Fill,BackColor=Color.FromArgb(44,47,54),FlatStyle=FlatStyle.Flat,ForeColor=Color.Gainsboro};
    private readonly TrackBar thumbnailSize=new(){Minimum=96,Maximum=240,TickStyle=TickStyle.None,Width=175,Height=30,SmallChange=8,LargeChange=24};
    private readonly Label sizeLabel=new(){AutoSize=false,Width=60,Height=28,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.Silver};
    private readonly Label countLabel=new(){Dock=DockStyle.Fill,ForeColor=Color.Silver,TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label empty=new(){Dock=DockStyle.Fill,Text="Nenhum objeto encontrado.\nAdicione objetos pelo menu de uma entry SMD.",TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.Silver,Visible=false};
    private readonly System.Windows.Forms.Timer resizeTimer=new(){Interval=160};
    private readonly SplitContainer split;
    private List<SmdCatalogItem> items=new();
    public SmdCatalogItem? SelectedItem {get;private set;}
    public SmdCatalogForm(SmdObjectCatalogService service)
    {
        this.service=service;Text="Catálogo de Objetos SMD";ClientSize=new Size(1120,720);MinimumSize=new Size(980,610);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(18,20,24);ForeColor=Color.White;Font=new Font("Segoe UI",9);
        foreach(Control c in new Control[]{categories,title,categoryLabel,details})LanguageService.PreserveContent(c);
        search.BackColor=categories.BackColor=Color.FromArgb(36,40,47);search.ForeColor=categories.ForeColor=Color.Gainsboro;
        var header=new TableLayoutPanel{Dock=DockStyle.Top,Height=126,ColumnCount=2,RowCount=3,Padding=new Padding(18,12,18,8),BackColor=Color.FromArgb(27,30,35)};
        header.ColumnStyles.Add(new(SizeType.Percent,100));header.ColumnStyles.Add(new(SizeType.Absolute,238));header.RowStyles.Add(new(SizeType.Absolute,38));header.RowStyles.Add(new(SizeType.Absolute,32));header.RowStyles.Add(new(SizeType.Percent,100));
        header.Controls.Add(new Label{Text="BIBLIOTECA DE OBJETOS",Dock=DockStyle.Fill,Font=new Font("Segoe UI Semibold",15),ForeColor=Color.White},0,0);header.Controls.Add(search,0,1);header.Controls.Add(categories,1,1);
        var sizing=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,3,0,0)};sizing.Controls.Add(new Label{Text="MINIATURAS",AutoSize=false,Width=95,Height=28,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.Silver});sizing.Controls.Add(thumbnailSize);sizing.Controls.Add(sizeLabel);header.Controls.Add(sizing,0,2);header.Controls.Add(countLabel,1,2);
        split=new SplitContainer{Size=new Size(1084,550),Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel2,SplitterWidth=6,BackColor=Color.FromArgb(36,39,45),Panel1MinSize=360,Panel2MinSize=300,SplitterDistance=758};
        list.LargeImageList=images;split.Panel1.Padding=new Padding(8);split.Panel1.Controls.Add(list);split.Panel1.Controls.Add(empty);
        split.Panel2.BackColor=Color.FromArgb(27,30,35);split.Panel2.Padding=new Padding(14);
        var inspector=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6};inspector.ColumnStyles.Add(new(SizeType.Percent,100));
        foreach(float height in new[]{220f,62f,28f})inspector.RowStyles.Add(new(SizeType.Absolute,height));inspector.RowStyles.Add(new(SizeType.Percent,100));inspector.RowStyles.Add(new(SizeType.Absolute,42));inspector.RowStyles.Add(new(SizeType.Absolute,48));
        inspector.Controls.Add(preview,0,0);inspector.Controls.Add(title,0,1);inspector.Controls.Add(categoryLabel,0,2);inspector.Controls.Add(details,0,3);inspector.Controls.Add(delete,0,4);inspector.Controls.Add(use,0,5);use.FlatAppearance.BorderSize=delete.FlatAppearance.BorderSize=0;split.Panel2.Controls.Add(inspector);
        inspector.SizeChanged+=(_,_)=>inspector.RowStyles[0].Height=Math.Clamp(inspector.ClientSize.Height-270,140,230);
        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12)};body.Controls.Add(split);Controls.Add(body);Controls.Add(header);
        thumbnailSize.Value=service.ThumbnailSize;SetImageSize();
        thumbnailSize.ValueChanged+=(_,_)=>{sizeLabel.Text=$"{thumbnailSize.Value} px";resizeTimer.Stop();resizeTimer.Start();};
        resizeTimer.Tick+=(_,_)=>{resizeTimer.Stop();SetImageSize();Filter();};
        list.HandleCreated+=(_,_)=>SetIconSpacing();search.TextChanged+=(_,_)=>Filter();categories.SelectedIndexChanged+=(_,_)=>Filter();list.SelectedIndexChanged+=(_,_)=>ShowSelection();list.DoubleClick+=(_,_)=>Accept();use.Click+=(_,_)=>Accept();delete.Click+=(_,_)=>DeleteSelected();
        Shown+=(_,_)=>{split.SplitterDistance=Math.Max(split.Panel1MinSize,split.ClientSize.Width-326);Reload();};
        FormClosing+=(_,_)=>{try{service.ThumbnailSize=thumbnailSize.Value;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar preferências do catálogo",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam);
    private void SetIconSpacing(){if(list.IsHandleCreated)SendMessage(list.Handle,0x1035,IntPtr.Zero,(IntPtr)((images.ImageSize.Width+38)|((images.ImageSize.Height+58)<<16)));}
    private void SetImageSize(){int size=thumbnailSize.Value;images.ImageSize=new Size(size,size*3/4);sizeLabel.Text=$"{size} px";SetIconSpacing();}
    private void Reload()
    {
        string? selectedCategory=categories.SelectedIndex>0?categories.SelectedItem?.ToString():null;items=service.LoadAll().ToList();categories.BeginUpdate();try{categories.Items.Clear();categories.Items.Add("Todas as categorias");foreach(string c in service.GetCategories())categories.Items.Add(c);categories.SelectedIndex=selectedCategory!=null&&categories.Items.Contains(selectedCategory)?categories.Items.IndexOf(selectedCategory):0;}finally{categories.EndUpdate();}Filter();
    }
    private void Filter()
    {
        if(categories.SelectedIndex<0)return;string? selectedId=list.SelectedItems.Count>0?(list.SelectedItems[0].Tag as SmdCatalogItem)?.Id:null;string query=search.Text.Trim();string? category=categories.SelectedIndex==0?null:categories.SelectedItem?.ToString();list.BeginUpdate();try{list.Items.Clear();images.Images.Clear();foreach(SmdCatalogItem item in items.Where(x=>(category==null||x.Category.Equals(category,StringComparison.OrdinalIgnoreCase))&&(query.Length==0||x.Name.Contains(query,StringComparison.OrdinalIgnoreCase)||x.Notes.Contains(query,StringComparison.OrdinalIgnoreCase)))){using var image=CreateThumbnail(item);images.Images.Add(item.Id,image);var row=new ListViewItem(item.Name,item.Id){Tag=item};list.Items.Add(row);if(item.Id==selectedId)row.Selected=true;}SetIconSpacing();}finally{list.EndUpdate();}empty.Visible=list.Items.Count==0;countLabel.Text=$"{list.Items.Count} de {items.Count} objetos";ShowSelection();
    }
    private Bitmap CreateThumbnail(SmdCatalogItem item)
    {
        var bitmap=new Bitmap(images.ImageSize.Width,images.ImageSize.Height);using var g=Graphics.FromImage(bitmap);g.Clear(list.BackColor);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        if(File.Exists(item.PreviewPath)){using var source=new Bitmap(item.PreviewPath);float scale=Math.Min((float)bitmap.Width/source.Width,(float)bitmap.Height/source.Height);float w=source.Width*scale,h=source.Height*scale;g.DrawImage(source,(bitmap.Width-w)/2,(bitmap.Height-h)/2,w,h);}
        else{using var font=new Font("Segoe UI Semibold",Math.Max(14,bitmap.Width/5));TextRenderer.DrawText(g,"SMD",font,new Rectangle(Point.Empty,bitmap.Size),Color.FromArgb(255,122,26),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}return bitmap;
    }
    private void ShowSelection()
    {
        var item=list.SelectedItems.Count==0?null:list.SelectedItems[0].Tag as SmdCatalogItem;use.Enabled=delete.Enabled=item!=null;title.Text=item?.Name??"Selecione um objeto";categoryLabel.Text=item?.Category??"CATÁLOGO SMD";string description=item==null?"Selecione uma miniatura para ver os detalhes e inserir o objeto no cenário.":$"{item.Entries.Count} entry(s) • {item.Bins.Count} modelo(s)\n{item.Textures.Count} textura(s)\n{item.Collisions.Count} faces de colisão SAT/EAT\n\n{item.Notes}";details.Text=description.Replace("\r\n","\n").Replace("\n",Environment.NewLine);Image? old=preview.Image;preview.Image=null;if(item!=null&&File.Exists(item.PreviewPath)){using var source=new Bitmap(item.PreviewPath);preview.Image=new Bitmap(source);}old?.Dispose();
    }
    private void Accept(){if(list.SelectedItems.Count==0)return;SelectedItem=(SmdCatalogItem)list.SelectedItems[0].Tag;DialogResult=DialogResult.OK;Close();}
    private void DeleteSelected(){if(list.SelectedItems.Count==0||list.SelectedItems[0].Tag is not SmdCatalogItem item)return;if(MessageBox.Show(this,$"Remover “{item.Name}” do catálogo?","Catálogo SMD",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;try{RemoveCatalogItem(item);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Remover do catálogo",MessageBoxButtons.OK,MessageBoxIcon.Error);Reload();}}
    private void RemoveCatalogItem(SmdCatalogItem item)
    {
        Image? old=preview.Image;preview.Image=null;old?.Dispose();
        service.Delete(item);SelectedItem=null;Reload();list.Invalidate();
    }
    protected override void Dispose(bool disposing){if(disposing){resizeTimer.Dispose();preview.Image?.Dispose();images.Dispose();}base.Dispose(disposing);}
}
