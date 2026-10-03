using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private readonly ComboBox textureSource=new(){Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(30,34,40),ForeColor=Color.Gainsboro};
    private readonly Label textureDetails=new(){Dock=DockStyle.Fill,ForeColor=Color.FromArgb(158,166,181),TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label textureEmpty=new(){Dock=DockStyle.Fill,Text="SEM TEXTURAS\nEste objeto não possui texturas reconhecidas.",ForeColor=Color.FromArgb(133,143,159),TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(16,18,22)};
    private bool refreshingTextureSources;
    private sealed record TextureSourceChoice(EtmResource Resource){public override string ToString()=>Resource.Name;}
    private EtmResource? TextureResource()=>(textureSource.SelectedItem as TextureSourceChoice)?.Resource;

    private void SynchronizeTextureResource()
    {
        if(refreshingTextureSources)return;
        var selected=Selected();
        if(selected==null)return;
        if(selected.Name.EndsWith(".bin",StringComparison.OrdinalIgnoreCase))
        {
            for(int i=0;i<partSelector.Items.Count;i++)
                if(partSelector.Items[i] is PartChoice part&&part.Order==selected.FileOrder){partSelector.SelectedIndex=i;return;}
            return;
        }
        if(!IsTpl(selected)&&!IsEff(selected))return;
        for(int i=0;i<textureSource.Items.Count;i++)
            if(textureSource.Items[i] is TextureSourceChoice choice&&choice.Resource.FileOrder==selected.FileOrder){textureSource.SelectedIndex=i;return;}
    }

    private void RefreshTextureSources(int preferred)
    {
        int previous=TextureResource()?.FileOrder??-1;
        refreshingTextureSources=true;
        try
        {
            textureSource.Items.Clear();
            foreach(var resource in resources.Items.Cast<ListViewItem>().Select(r=>(EtmResource)r.Tag!))
                if(IsTpl(resource)||IsEff(resource)&&HasEffTextures(resource))textureSource.Items.Add(new TextureSourceChoice(resource));
            var choices=textureSource.Items.Cast<TextureSourceChoice>().ToArray();
            int index=Array.FindIndex(choices,c=>c.Resource.FileOrder==preferred);
            if(index<0)index=Array.FindIndex(choices,c=>c.Resource.FileOrder==previous);
            if(index<0 && objects.SelectedItem is ObjectItem item && catalog?.ModelParts.TryGetValue(item.Definition.Id,out var parts)==true)
            {
                var part=parts.FirstOrDefault(p=>p.Bin.FileOrder==activePartOrder);
                int associated=(part?.TextureFallback??part?.Effect)?.FileOrder??-1;
                index=Array.FindIndex(choices,c=>c.Resource.FileOrder==associated);
            }
            textureSource.SelectedIndex=choices.Length==0?-1:Math.Max(0,index);
            textureSource.Enabled=choices.Length>0;
        }
        finally{refreshingTextureSources=false;}
        PreviewSelectedTexture(true);
    }

    private static Label InspectorHeading(string title)=>new(){Text=title,Dock=DockStyle.Top,Height=36,Padding=new Padding(12,0,0,0),TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(219,223,230),Font=new Font("Segoe UI Semibold",9),BackColor=Color.FromArgb(27,31,38)};
    private void StyleEditorCombo(ComboBox combo)
    {
        combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=25;
        combo.DrawItem+=(_,e)=>
        {
            using var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?Color.FromArgb(74,39,47):Color.FromArgb(30,34,40));
            e.Graphics.FillRectangle(brush,e.Bounds);
            string label=e.Index>=0?combo.Items[e.Index].ToString()??"":combo==partSelector?"Nenhum BIN renderizável":combo==etmNavigation?"Selecione um ETM extraído":"Nenhuma textura";
            TextRenderer.DrawText(e.Graphics,label,Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-10,e.Bounds.Height),Color.Gainsboro,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        };
    }
    private Control BuildAssetHeading()
    {
        var panel=new Panel{Dock=DockStyle.Top,Height=etmOnly?128:74,BackColor=Color.FromArgb(18,21,27),Padding=new Padding(18,10,18,8)};
        panel.Controls.Add(new Label{Text=etmOnly?"EDITOR DE ETM":"EDITOR DE MODELOS",Left=18,Top=10,Width=600,Height=32,Font=new Font("Segoe UI Semibold",17),ForeColor=Color.White});
        panel.Controls.Add(new Label{Text="Objetos, variantes e materiais  /  RE4 PS2",Left=20,Top=43,Width=650,Height=20,ForeColor=Color.FromArgb(148,157,173)});
        InitializePackageNavigation(panel);
        return panel;
    }
    private Control BuildAssetWorkspace(SplitContainer main)
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(12,8,12,8),BackColor=Color.FromArgb(13,15,18)};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,220));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var browser=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(22,25,30),Margin=new Padding(0,0,10,0)};
        objects.Font=new Font("Segoe UI",10);objects.ItemHeight=30;objects.DrawMode=DrawMode.OwnerDrawFixed;
        objects.DrawItem+=(_,e)=>{if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using var bg=new SolidBrush(selected?Color.FromArgb(79,36,42):objects.BackColor);e.Graphics.FillRectangle(bg,e.Bounds);TextRenderer.DrawText(e.Graphics,objects.Items[e.Index].ToString(),objects.Font,new Rectangle(e.Bounds.X+12,e.Bounds.Y,e.Bounds.Width-20,e.Bounds.Height),Color.Gainsboro,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        browser.Controls.Add(objects);browser.Controls.Add(modelStats);browser.Controls.Add(InspectorHeading("OBJETOS DO PACOTE"));
        root.Controls.Add(browser,0,0);main.Margin=Padding.Empty;root.Controls.Add(main,1,0);return root;
    }
    private Control BuildAssetInspector()
    {
        var inspector=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Color.FromArgb(13,15,18)};
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));inspector.RowStyles.Add(new RowStyle(SizeType.Percent,32));inspector.RowStyles.Add(new RowStyle(SizeType.Percent,68));
        var resourcePanel=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(22,25,30),Margin=new Padding(8,0,0,8)};
        resources.MultiSelect=false;resources.HideSelection=false;resources.HeaderStyle=ColumnHeaderStyle.None;resources.OwnerDraw=true;
        resources.DrawColumnHeader+=(_,e)=>{using var brush=new SolidBrush(Color.FromArgb(32,37,45));e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header!.Text,Font,new Rectangle(e.Bounds.X+9,e.Bounds.Y,e.Bounds.Width-9,e.Bounds.Height),Color.Silver,TextFormatFlags.VerticalCenter);};
        resources.DrawItem+=(_,e)=>{if(resources.View!=View.Details)e.DrawDefault=true;};
        resources.DrawSubItem+=(_,e)=>{bool selected=e.Item!.Selected;using var brush=new SolidBrush(selected?Color.FromArgb(65,39,46):resources.BackColor);e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,e.SubItem!.Text,Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-8,e.Bounds.Height),Color.Gainsboro,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        resources.Resize+=(_,_)=>{if(resources.Columns.Count==3){resources.Columns[0].Width=Math.Max(120,resources.ClientSize.Width-175-SystemInformation.VerticalScrollBarWidth-8);resources.Columns[1].Width=92;resources.Columns[2].Width=75;}};
        var resourceActions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=47,Padding=new Padding(6),WrapContents=false};
        resourceActions.Controls.Add(B("Exportar",(_,_)=>ExportSelected(),95));resourceActions.Controls.Add(B("Substituir",(_,_)=>ReplaceSelected(),100));resourceActions.Controls.Add(B("Restaurar",(_,_)=>RestoreSelected(),96));
        if(!etmOnly)resourceActions.Controls.Add(B("Testar TPL",(_,_)=>UseSelectedTpl(),95));resourceActions.AutoScroll=true;
        resourcePanel.Controls.Add(resources);resourcePanel.Controls.Add(resourceActions);
        var columns=new TableLayoutPanel{Dock=DockStyle.Top,Height=28,ColumnCount=3,BackColor=Color.FromArgb(30,35,43)};
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,92));columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,75));
        foreach(string title in new[]{"Recurso","Tipo","Tamanho"})columns.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,ForeColor=Color.Silver,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0)});
        resourcePanel.Controls.Add(columns);resourcePanel.Controls.Add(InspectorHeading("RECURSOS DO OBJETO"));
        var texturePanel=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(22,25,30),Margin=new Padding(8,0,0,0)};
        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(12,4,12,10)};
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(float height in new[]{34f,34f,26f})content.RowStyles.Add(new RowStyle(SizeType.Absolute,height));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,124));
        content.Controls.Add(textureSource,0,0);textureIndex.Dock=DockStyle.Fill;textureIndex.Width=220;content.Controls.Add(textureIndex,0,1);content.Controls.Add(textureDetails,0,2);
        var canvas=new Panel{Dock=DockStyle.Fill,Margin=new Padding(0,8,0,10),BackColor=Color.FromArgb(16,18,22)};canvas.Controls.Add(texture);canvas.Controls.Add(textureEmpty);content.Controls.Add(canvas,0,3);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,Margin=Padding.Empty};for(int i=0;i<3;i++){actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));actions.RowStyles.Add(new RowStyle(SizeType.Percent,100f/3));}
        Button Add(string text,EventHandler click,int col,int row){var button=B(text,click,110);button.Dock=DockStyle.Fill;button.Height=32;actions.Controls.Add(button,col,row);return button;}
        Add("PNG",(_,_)=>ExportPng(),0,0);exportTplButton=Add("TPL",(_,_)=>ExportTextureTpl(),1,0);replacePngButton=Add("Importar PNG/TPL",(_,_)=>ImportTextureFile(),2,0);
        exportAllPngButton=Add("Todos PNG",(_,_)=>ExportAllTextures(false),0,1);exportAllTplButton=Add("Todos TPL",(_,_)=>ExportAllTextures(true),1,1);
        rotateTextureButton=Add("Girar 90°",(_,_)=>TransformSelectedTexture(RotateFlipType.Rotate90FlipNone,"rotacionada 90°",false),2,1);
        flipXTextureButton=Add("Inverter X",(_,_)=>TransformSelectedTexture(RotateFlipType.RotateNoneFlipX,"invertida em X",true),0,2);flipYTextureButton=Add("Inverter Y",(_,_)=>TransformSelectedTexture(RotateFlipType.RotateNoneFlipY,"invertida em Y",true),1,2);
        content.Controls.Add(actions,0,4);texturePanel.Controls.Add(content);texturePanel.Controls.Add(InspectorHeading("TEXTURAS / MATERIAIS"));
        textureSource.SelectedIndexChanged+=(_,_)=>{if(!refreshingTextureSources)PreviewSelectedTexture(true);};textureIndex.SelectedIndexChanged+=(_,_)=>PreviewSelectedTexture(false);
        inspector.Controls.Add(resourcePanel,0,0);inspector.Controls.Add(texturePanel,0,1);
        foreach(var combo in new[]{textureSource,textureIndex,partSelector})StyleEditorCombo(combo);
        status.Text="Ctrl/Shift + clique: selecionar vários elementos • arraste o gizmo para transformar";
        return inspector;
    }
}
