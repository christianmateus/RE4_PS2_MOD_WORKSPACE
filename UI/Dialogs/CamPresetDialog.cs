using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class CamPresetDialog : Form
{
    private static readonly Color Bg=Color.FromArgb(18,21,27),Surface=Color.FromArgb(27,31,39),Surface2=Color.FromArgb(37,42,52),TextColor=Color.FromArgb(230,234,241),Muted=Color.FromArgb(145,155,173),Accent=Color.FromArgb(206,54,62);
    private readonly FlowLayoutPanel cards=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(14,8,14,8)};
    private readonly List<CamPresetDefinition> custom;
    public CamPresetDefinition? SelectedPreset { get; private set; }
    public bool CustomPresetsChanged { get; private set; }

    public CamPresetDialog(List<CamPresetDefinition> customPresets)
    {
        custom=customPresets;Text="Add camera";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;ClientSize=new Size(470,560);BackColor=Bg;ForeColor=TextColor;Font=new Font("Segoe UI",9F);ShowInTaskbar=false;
        var header=new Panel{Dock=DockStyle.Top,Height=76,Padding=new Padding(18,14,18,4)};
        header.Controls.Add(new Label{Dock=DockStyle.Top,Height=29,Text="Choose a camera preset",ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15F)});
        header.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=25,Text="A triggerzone will be created from the current selection.",ForeColor=Muted});
        var footer=new Panel{Dock=DockStyle.Bottom,Height=68,Padding=new Padding(14,10,14,14)};var create=new Button{Dock=DockStyle.Fill,Text="＋  CREATE NEW PRESET",FlatStyle=FlatStyle.Flat,BackColor=Surface2,ForeColor=TextColor,Cursor=Cursors.Hand};create.FlatAppearance.BorderColor=Color.FromArgb(65,72,86);create.Click+=CreatePreset;footer.Controls.Add(create);
        Controls.Add(cards);Controls.Add(footer);Controls.Add(header);RenderCards();
    }

    private static IEnumerable<(CamPresetDefinition Preset,string Description,string Badge)> Defaults()
    {
        yield return(new(){Name="3rd Person Animated",CameraType=(byte)CamType.ThirdPerson,VertexCount=4,FrameCount=24,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Animated third-person gameplay camera","ANIMATED");
        yield return(new(){Name="3rd Person Standard",CameraType=(byte)CamType.ThirdPerson,VertexCount=4,FrameCount=0,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Standard third-person gameplay camera","STANDARD");
        yield return(new(){Name="Fixed Camera",CameraType=(byte)CamType.Fixed,VertexCount=4,FrameCount=1,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Single fixed viewpoint","FIXED");
        yield return(new(){Name="Fixed Camera + Rotation",CameraType=(byte)CamType.RailPan,VertexCount=4,FrameCount=2,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Fixed position that rotates to follow the player","FIXED, ROTATE");
        yield return(new(){Name="Track Camera",CameraType=(byte)CamType.Track,VertexCount=4,FrameCount=2,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Moving camera that follows the player","TRACK, FOLLOW");
        yield return(new(){Name="Pan Camera",CameraType=(byte)CamType.Pan,VertexCount=4,FrameCount=1,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Starts behind the player on entry, stays fixed and rotates to follow","PAN, FOLLOW");
        yield return(new(){Name="Behind Camera",CameraType=(byte)CamType.Behind,VertexCount=4,FrameCount=2,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Follows behind the player with rotation controlled by the analog stick","BEHIND, MANUAL");
        yield return(new(){Name="Free Camera",CameraType=(byte)CamType.Free,VertexCount=4,FrameCount=2,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Follows the player with analog-controlled 360° orbit","FREE, 360°");
        yield return(new(){Name="Entry-Locked Camera",CameraType=(byte)CamType.UpCut,VertexCount=4,FrameCount=1,AreaAttributes=(byte)(CamAreaAttributes.Normal|CamAreaAttributes.Battle)},"Starts behind the player on entry, then locks position and rotation","ENTRY, LOCKED");
        yield return(new(){Name="Motion Transition",CameraType=(byte)CamType.Motion,VertexCount=4,FrameCount=2,AreaAttributes=(byte)CamAreaAttributes.Event},"Transition between two camera states","MOTION");
    }

    private void RenderCards()
    {
        cards.SuspendLayout();cards.Controls.Clear();
        foreach(var item in Defaults())cards.Controls.Add(MakeCard(item.Preset,item.Description,item.Badge,false));
        if(custom.Count>0){cards.Controls.Add(new Label{Width=410,Height=30,Padding=new Padding(2,8,0,0),Text="YOUR PRESETS",ForeColor=Muted,Font=new Font("Segoe UI Semibold",8F)});foreach(var preset in custom)cards.Controls.Add(MakeCard(preset,string.IsNullOrWhiteSpace(preset.Description)?"Custom camera preset":preset.Description,"CUSTOM",true));}
        cards.ResumeLayout();
    }

    private Control MakeCard(CamPresetDefinition preset,string description,string badge,bool isCustom)
    {
        var card=new Panel{Width=410,Height=83,BackColor=Surface,Margin=new Padding(0,0,0,8),Cursor=Cursors.Hand,Tag=preset};
        var stripe=new Panel{Dock=DockStyle.Left,Width=4,BackColor=isCustom?Color.FromArgb(63,142,214):Accent};
        var title=new Label{Left=17,Top=10,Width=260,Height=24,Text=preset.Name,ForeColor=TextColor,Font=new Font("Segoe UI Semibold",11F),Cursor=Cursors.Hand,Tag=preset};
        var info=new Label{Left=17,Top=36,Width=isCustom?295:370,Height=20,Text=$"{CamNames.TypeName(preset.CameraType)}  •  {preset.VertexCount} vertices  •  {preset.FrameCount} frames",AutoEllipsis=true,ForeColor=Color.FromArgb(181,190,204),Cursor=Cursors.Hand,Tag=preset};
        var desc=new Label{Left=17,Top=58,Width=isCustom?295:370,Height=18,Text=description,AutoEllipsis=true,ForeColor=Muted,Font=new Font("Segoe UI",8F),Cursor=Cursors.Hand,Tag=preset};
        var tag=new Label{Left=280,Top=12,Width=112,Height=20,Text=badge,TextAlign=ContentAlignment.MiddleRight,ForeColor=isCustom?Color.FromArgb(99,174,238):Color.FromArgb(238,105,111),Font=new Font("Segoe UI Semibold",7F),Cursor=Cursors.Hand,Tag=preset};
        void choose(object? s,EventArgs e){SelectedPreset=(CamPresetDefinition)((Control)s!).Tag!;DialogResult=DialogResult.OK;Close();}
        foreach(Control c in new Control[]{card,title,info,desc,tag})c.Click+=choose;card.Controls.AddRange(new Control[]{stripe,title,info,desc,tag});
        if(isCustom)
        {
            var edit=new Button{Left=326,Top=48,Width=31,Height=26,Text="✎",FlatStyle=FlatStyle.Flat,BackColor=Surface2,ForeColor=TextColor,Cursor=Cursors.Hand,TabStop=false};edit.FlatAppearance.BorderColor=Color.FromArgb(65,72,86);edit.Click+=(_,_)=>EditPreset(preset);
            var delete=new Button{Left=361,Top=48,Width=31,Height=26,Text="×",FlatStyle=FlatStyle.Flat,BackColor=Surface2,ForeColor=Color.FromArgb(238,105,111),Cursor=Cursors.Hand,TabStop=false};delete.FlatAppearance.BorderColor=Color.FromArgb(80,58,63);delete.Click+=(_,_)=>DeletePreset(preset);
            card.Controls.AddRange(new Control[]{edit,delete});
        }
        return card;
    }

    private void CreatePreset(object? sender,EventArgs e)
    {
        using var dialog=new CamPresetEditDialog();if(dialog.ShowDialog(this)!=DialogResult.OK||dialog.Preset==null)return;custom.Add(dialog.Preset);CustomPresetsChanged=true;RenderCards();
    }

    private void EditPreset(CamPresetDefinition preset)
    {
        using var dialog=new CamPresetEditDialog(preset);if(dialog.ShowDialog(this)!=DialogResult.OK||dialog.Preset==null)return;int index=custom.IndexOf(preset);if(index<0)return;custom[index]=dialog.Preset;CustomPresetsChanged=true;RenderCards();
    }

    private void DeletePreset(CamPresetDefinition preset)
    {
        if(MessageBox.Show(this,$"Delete preset '{preset.Name}'?","Delete camera preset",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;custom.Remove(preset);CustomPresetsChanged=true;RenderCards();
    }
}

internal sealed class CamPresetEditDialog : Form
{
    public CamPresetDefinition? Preset { get; private set; }
    private readonly TextBox name=new(){Text="My camera preset",Dock=DockStyle.Fill};
    private readonly TextBox description=new(){Dock=DockStyle.Fill,MaxLength=120};
    private readonly ComboBox type=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill};
    private readonly NumericUpDown vertices=new(){Minimum=3,Maximum=32,Value=4,Dock=DockStyle.Fill};
    private readonly NumericUpDown frames=new(){Minimum=0,Maximum=1024,Value=2,Dock=DockStyle.Fill};
    public CamPresetEditDialog(CamPresetDefinition? existing=null)
    {
        bool editing=existing!=null;Text=editing?"Edit camera preset":"Create camera preset";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;ShowInTaskbar=false;ClientSize=new Size(400,330);BackColor=Color.FromArgb(18,21,27);ForeColor=Color.White;Font=new Font("Segoe UI",9F);
        foreach(CamType value in Enum.GetValues<CamType>())type.Items.Add(value);type.SelectedItem=CamType.ThirdPerson;
        if(existing!=null){name.Text=existing.Name;description.Text=existing.Description;type.SelectedItem=(CamType)existing.CameraType;vertices.Value=Math.Clamp(existing.VertexCount,(int)vertices.Minimum,(int)vertices.Maximum);frames.Value=Math.Clamp(existing.FrameCount,(int)frames.Minimum,(int)frames.Maximum);}
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=6};grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,105));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        AddRow(grid,0,"NAME",name);AddRow(grid,1,"DESCRIPTION",description);AddRow(grid,2,"CAMERA TYPE",type);AddRow(grid,3,"VERTICES",vertices);AddRow(grid,4,"FRAMES",frames);
        var save=new Button{Text=editing?"SAVE CHANGES":"CREATE PRESET",Dock=DockStyle.Fill,BackColor=Color.FromArgb(206,54,62),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};save.FlatAppearance.BorderSize=0;save.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(name.Text)){name.Focus();return;}CamType selected=(CamType)type.SelectedItem!;bool gameplay=selected is CamType.ThirdPerson or CamType.Fixed or CamType.Track or CamType.RailPan;Preset=new(){Name=name.Text.Trim(),Description=description.Text.Trim(),CameraType=(byte)selected,VertexCount=(int)vertices.Value,FrameCount=(int)frames.Value,AreaAttributes=(byte)(gameplay?CamAreaAttributes.Normal|CamAreaAttributes.Battle:CamAreaAttributes.Event)};DialogResult=DialogResult.OK;Close();};grid.Controls.Add(save,0,5);grid.SetColumnSpan(save,2);Controls.Add(grid);AcceptButton=save;
    }
    private static void AddRow(TableLayoutPanel grid,int row,string label,Control control){grid.RowStyles.Add(new RowStyle(SizeType.Absolute,46));grid.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(145,155,173),Font=new Font("Segoe UI Semibold",8F)},0,row);grid.Controls.Add(control,1,row);}
}
