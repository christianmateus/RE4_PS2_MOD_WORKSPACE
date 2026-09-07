using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using NVector3=System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class CamPreviewForm:Form
{
    private readonly ScenarioViewport viewport;
    private readonly Func<CamEntry?> entryProvider;
    private readonly ComboBox frames;
    private readonly ComboBox aspectRatios;
    private readonly TrackBar interpolation;
    private readonly CheckBox play;
    private readonly Label status;
    private readonly System.Windows.Forms.Timer timer;

    public CamPreviewForm(ScenarioScene? scene,string? texturePath,Func<CamEntry?> entryProvider)
    {
        this.entryProvider=entryProvider;Text="CAMERA FIXED • PREVIEW";StartPosition=FormStartPosition.CenterParent;MinimumSize=new Size(640,420);Size=new Size(960,600);BackColor=Color.FromArgb(12,15,20);
        var bar=new Panel{Dock=DockStyle.Top,Height=76,BackColor=Color.FromArgb(22,27,34)};
        frames=new ComboBox{Left=10,Top=9,Width=120,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(31,37,46),ForeColor=Color.White};frames.SelectedIndexChanged+=(_,_)=>RefreshPreview();
        var aspectLabel=new Label{Left=145,Top=13,AutoSize=true,ForeColor=Color.FromArgb(180,190,205),Text="Proporção"};
        aspectRatios=new ComboBox{Left=210,Top=9,Width=90,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(31,37,46),ForeColor=Color.White};aspectRatios.Items.AddRange(new object[]{"4:3","16:9","16:10","3:2","1:1","Livre"});aspectRatios.SelectedIndex=0;aspectRatios.SelectedIndexChanged+=(_,_)=>ApplyAspectRatio();
        status=new Label{Left=315,Top=12,AutoSize=true,ForeColor=Color.FromArgb(180,190,205),Text="Preview em tempo real"};bar.Controls.Add(frames);bar.Controls.Add(aspectLabel);bar.Controls.Add(aspectRatios);bar.Controls.Add(status);
        interpolation=new TrackBar{Left=8,Top=39,Width=292,Height=32,Minimum=0,Maximum=100,TickFrequency=10,Value=0};interpolation.ValueChanged+=(_,_)=>RefreshPreview();
        play=new CheckBox{Left=315,Top=46,Width=155,ForeColor=Color.FromArgb(220,225,234),Text="Animar interpolação"};bar.Controls.Add(interpolation);bar.Controls.Add(play);
        viewport=new ScenarioViewport{Dock=DockStyle.Fill,CamVisible=false,AevVisible=false,EnemiesVisible=false,ObjectsVisible=false,ForcedAspectRatio=4f/3f,FieldOfViewIsHorizontal=true};viewport.SetScene(scene);viewport.SetTextureSource(texturePath);Controls.Add(viewport);Controls.Add(bar);
        timer=new System.Windows.Forms.Timer{Interval=50};timer.Tick+=(_,_)=>{if(play.Checked&&entryProvider()?.Frames.Count>1){interpolation.Value=(interpolation.Value+2)%(interpolation.Maximum+1);}else RefreshPreview();};timer.Start();FormClosed+=(_,_)=>timer.Dispose();
        RefreshFrameList();
    }

    private void RefreshFrameList(){CamEntry? entry=entryProvider();int count=entry?.Frames.Count??0;if(frames.Items.Count==count)return;int old=frames.SelectedIndex;frames.Items.Clear();for(int i=0;i<count;i++)frames.Items.Add($"Frame {i:00}");if(count>0)frames.SelectedIndex=Math.Clamp(old,0,count-1);}
    public void SelectFrame(int index){RefreshFrameList();if(index>=0&&index<frames.Items.Count)frames.SelectedIndex=index;interpolation.Value=0;RefreshPreview();}
    private void ApplyAspectRatio(){viewport.ForcedAspectRatio=aspectRatios.SelectedItem?.ToString() switch{"4:3"=>4f/3f,"16:9"=>16f/9f,"16:10"=>16f/10f,"3:2"=>3f/2f,"1:1"=>1f,_=>null};viewport.Invalidate();RefreshPreview();}
    public void RefreshPreview(){RefreshFrameList();CamEntry? entry=entryProvider();if(entry==null||entry.Frames.Count==0)return;int index=Math.Clamp(frames.SelectedIndex,0,entry.Frames.Count-1),next=Math.Min(index+1,entry.Frames.Count-1);float amount=interpolation.Value/100f;CamFrame a=entry.Frames[index],b=entry.Frames[next];const float scale=.01f;NVector3 position=NVector3.Lerp(a.Position,b.Position,amount)*scale,target=NVector3.Lerp(a.Target,b.Target,amount)*scale;float fov=a.Fov+(b.Fov-a.Fov)*amount;viewport.SetCameraLookAt(position,target,fov);status.Text=$"{entry.CameraType} • {index:00}→{next:00} {interpolation.Value}% • FOV H {fov:0.##}° • {aspectRatios.SelectedItem??"4:3"}";}
}
