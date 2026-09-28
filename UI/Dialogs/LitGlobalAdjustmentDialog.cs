using System.Drawing.Drawing2D;
using RE4_PS2_MOD_WORKSPACE.Core.Lighting;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class LitGlobalAdjustmentDialog : AppForm
{
    private static readonly Color Bg=Color.FromArgb(16,19,24),Surface=Color.FromArgb(24,28,35),Surface2=Color.FromArgb(31,36,44),Border=Color.FromArgb(48,55,66),TextColor=Color.FromArgb(235,239,246),Muted=Color.FromArgb(145,155,173),Accent=Color.FromArgb(206,54,62);
    private readonly LitScene scene;private readonly int selectedSlot;private readonly Action preview;
    private readonly SceneSnapshot original;
    private readonly ComboBox preset=new();private readonly TrackBar brightness=new(),saturation=new(),warmth=new(),fogDensity=new();
    private readonly Label brightnessValue=new(),saturationValue=new(),warmthValue=new(),fogDensityValue=new();
    private readonly CheckBox allGroups=new(),preserveSpecial=new(),enableFog=new();
    private readonly Button fogColorButton=new();private Color fogColor=Color.FromArgb(184,202,181),sceneTint=Color.White;private float sceneTintStrength;private bool syncing;private bool accepted;

    public LitGlobalAdjustmentDialog(LitScene scene,int selectedSlot,Action preview)
    {
        this.scene=scene;this.selectedSlot=selectedSlot;this.preview=preview;original=SceneSnapshot.Capture(scene);
        Text="Ajuste global de iluminação";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;ShowInTaskbar=false;ClientSize=new Size(760,680);BackColor=Bg;ForeColor=TextColor;Font=new Font("Segoe UI",9F);
        BuildUi();syncing=true;preset.SelectedIndex=0;syncing=false;ApplyPreset(0);FormClosing+=OnClosing;
    }

    private void BuildUi()
    {
        var header=new Panel{Dock=DockStyle.Top,Height=92,BackColor=Surface,Padding=new Padding(24,18,24,12)};
        header.Controls.Add(new Label{Text="AJUSTE GLOBAL",Left=24,Top=16,Width=320,Height=28,ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15F)});
        header.Controls.Add(new Label{Text="Direção de arte rápida para todos os grupos e luzes do LIT.",Left=25,Top=50,Width=650,Height=22,ForeColor=Muted});Controls.Add(header);
        var body=new Panel{Dock=DockStyle.Fill,Padding=Padding.Empty,BackColor=Bg};Controls.Add(body);body.BringToFront();
        var presetCard=Card(24,18,712,76);body.Controls.Add(presetCard);presetCard.Controls.Add(Caption("PREDEFINIÇÃO",18,12,170));
        preset.SetBounds(18,35,330,29);preset.DropDownStyle=ComboBoxStyle.DropDownList;preset.FlatStyle=FlatStyle.Flat;preset.BackColor=Surface2;preset.ForeColor=TextColor;preset.Items.AddRange(new object[]{"Personalizado","Primavera suave","Primavera com névoa","Neblina cinematográfica","Quente / pôr do sol","Sombrio","Noturno","Inferno","Lua de Sangue","Tóxico","Gélido","Tempestade","Sonho Dourado"});preset.SelectedIndexChanged+=(_,_)=>{if(!syncing)ApplyPreset(preset.SelectedIndex);};presetCard.Controls.Add(preset);
        allGroups.Text="Aplicar a todos os grupos";allGroups.SetBounds(390,18,285,24);StyleCheck(allGroups,true);allGroups.CheckedChanged+=Changed;presetCard.Controls.Add(allGroups);
        preserveSpecial.Text="Preservar animações e intensidades especiais";preserveSpecial.SetBounds(390,44,310,24);StyleCheck(preserveSpecial,true);preserveSpecial.CheckedChanged+=Changed;presetCard.Controls.Add(preserveSpecial);
        var lightCard=Card(24,106,350,322);body.Controls.Add(lightCard);lightCard.Controls.Add(Title("ILUMINAÇÃO",18,15));lightCard.Controls.Add(new Label{Text="Altera o conjunto proporcionalmente, mantendo a relação entre as luzes.",Left=18,Top=45,Width=310,Height=38,ForeColor=Muted});
        AddSlider(lightCard,"BRILHO",brightness,brightnessValue,86,25,160,100,v=>$"{v}%");AddSlider(lightCard,"SATURAÇÃO",saturation,saturationValue,158,0,180,100,v=>$"{v}%");AddSlider(lightCard,"TEMPERATURA",warmth,warmthValue,230,-50,50,0,v=>v==0?"NEUTRA":v>0?$"+{v} QUENTE":$"{v} FRIA");
        var fogCard=Card(386,106,350,322);body.Controls.Add(fogCard);fogCard.Controls.Add(Title("NÉVOA",18,15));fogCard.Controls.Add(new Label{Text="Ajusta alcance e cor em conjunto para preservar a leitura do cenário.",Left=18,Top=45,Width=310,Height=38,ForeColor=Muted});
        enableFog.Text="Ativar névoa nos grupos sem fog";enableFog.SetBounds(18,85,295,25);StyleCheck(enableFog,false);enableFog.CheckedChanged+=Changed;fogCard.Controls.Add(enableFog);
        AddSlider(fogCard,"DENSIDADE",fogDensity,fogDensityValue,120,-50,70,0,v=>v==0?"ORIGINAL":v>0?$"+{v}%":$"{v}%");fogCard.Controls.Add(Caption("COR DA NÉVOA",18,206,180));fogColorButton.SetBounds(18,232,314,48);fogColorButton.FlatStyle=FlatStyle.Flat;fogColorButton.FlatAppearance.BorderColor=Border;fogColorButton.FlatAppearance.BorderSize=1;fogColorButton.ForeColor=TextColor;fogColorButton.TextAlign=ContentAlignment.MiddleLeft;fogColorButton.Padding=new Padding(50,0,0,0);fogColorButton.Click+=ChooseFogColor;fogColorButton.Paint+=PaintColorSwatch;fogCard.Controls.Add(fogColorButton);
        var note=new Panel{Left=24,Top=440,Width=712,Height=56,BackColor=Color.FromArgb(22,32,38)};note.Controls.Add(new Label{Text="●  PRÉVIA AO VIVO",Left=16,Top=10,Width=150,Height=20,ForeColor=Color.FromArgb(105,200,255),Font=new Font("Segoe UI Semibold",8.5F)});note.Controls.Add(new Label{Text="Mova os controles e observe o viewport. Cancelar restaura todos os valores originais.",Left=16,Top=30,Width=675,Height=19,ForeColor=Muted});body.Controls.Add(note);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=68,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Padding=new Padding(16,14,16,10),BackColor=Surface};Controls.Add(actions);actions.BringToFront();
        var apply=Button("APLICAR AO LIT",Accent,154);apply.Click+=(_,_)=>{accepted=true;DialogResult=DialogResult.OK;Close();};var cancel=Button("CANCELAR",Surface2,112);cancel.FlatAppearance.BorderColor=Border;cancel.FlatAppearance.BorderSize=1;cancel.Click+=(_,_)=>Close();var reset=Button("RESTAURAR",Surface2,118);reset.FlatAppearance.BorderColor=Border;reset.FlatAppearance.BorderSize=1;reset.Click+=(_,_)=>ApplyPreset(0);actions.Controls.Add(apply);actions.Controls.Add(cancel);actions.Controls.Add(reset);AcceptButton=apply;CancelButton=cancel;
    }

    private Panel Card(int x,int y,int w,int h)=>new(){Left=x,Top=y,Width=w,Height=h,BackColor=Surface};
    private static Label Title(string text,int x,int y)=>new(){Text=text,Left=x,Top=y,Width=250,Height=24,ForeColor=TextColor,Font=new Font("Segoe UI Semibold",11F)};
    private static Label Caption(string text,int x,int y,int width)=>new(){Text=text,Left=x,Top=y,Width=width,Height=19,ForeColor=Muted,Font=new Font("Segoe UI Semibold",8F)};
    private static Button Button(string text,Color color,int width){var b=new Button{Text=text,Width=width,Height=36,BackColor=color,ForeColor=TextColor,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Margin=new Padding(8,0,0,0),Font=new Font("Segoe UI Semibold",8.7F)};b.FlatAppearance.BorderSize=0;return b;}
    private static void StyleCheck(CheckBox c,bool value){c.Checked=value;c.ForeColor=TextColor;c.BackColor=Surface;c.FlatStyle=FlatStyle.Flat;}
    private void AddSlider(Control parent,string title,TrackBar bar,Label value,int top,int min,int max,int initial,Func<int,string> format){parent.Controls.Add(Caption(title,18,top,160));value.SetBounds(205,top,125,19);value.TextAlign=ContentAlignment.TopRight;value.ForeColor=Color.FromArgb(105,200,255);value.Font=new Font("Segoe UI Semibold",8F);parent.Controls.Add(value);bar.SetBounds(12,top+22,326,36);bar.Minimum=min;bar.Maximum=max;bar.Value=initial;bar.TickStyle=TickStyle.None;bar.Tag=format;bar.ValueChanged+=Changed;parent.Controls.Add(bar);}
    private void ApplyPreset(int index)
    {
        syncing=true;try{(int b,int s,int w,int f,Color c,bool fog)=index switch
        {
            1=>(112,115,8,12,Color.FromArgb(194,211,188),true),
            2=>(110,112,5,32,Color.FromArgb(184,204,184),true),
            3=>(94,82,-5,52,Color.FromArgb(165,179,169),true),
            4=>(108,112,30,4,Color.FromArgb(211,184,151),false),
            5=>(72,62,-8,18,Color.FromArgb(107,119,112),false),
            6=>(42,48,-35,30,Color.FromArgb(58,73,98),true),
            7=>(78,180,50,48,Color.FromArgb(188,28,12),true),
            8=>(58,165,36,38,Color.FromArgb(126,12,20),true),
            9=>(82,150,-12,44,Color.FromArgb(72,139,34),true),
            10=>(88,72,-48,36,Color.FromArgb(137,181,213),true),
            11=>(62,42,-24,58,Color.FromArgb(74,84,94),true),
            12=>(122,128,42,-12,Color.FromArgb(226,187,112),true),
            _=>(100,100,0,0,Color.FromArgb(184,202,181),false)
        };sceneTint=index switch{7=>Color.FromArgb(255,42,20),8=>Color.FromArgb(198,18,30),9=>Color.FromArgb(96,210,48),10=>Color.FromArgb(122,190,242),11=>Color.FromArgb(82,98,116),12=>Color.FromArgb(255,202,96),_=>Color.White};sceneTintStrength=index switch{7=>.52f,8=>.42f,9=>.38f,10=>.34f,11=>.22f,12=>.3f,_=>0f};brightness.Value=b;saturation.Value=s;warmth.Value=w;fogDensity.Value=f;fogColor=c;enableFog.Checked=fog;UpdateLabels();}finally{syncing=false;}ApplyPreview();
    }
    private void Changed(object? sender,EventArgs e){if(syncing)return;if(sender is TrackBar&&preset.SelectedIndex!=0){syncing=true;preset.SelectedIndex=0;syncing=false;}UpdateLabels();ApplyPreview();}
    private void UpdateLabels(){brightnessValue.Text=$"{brightness.Value}%";saturationValue.Text=$"{saturation.Value}%";warmthValue.Text=warmth.Value==0?"NEUTRA":warmth.Value>0?$"+{warmth.Value} QUENTE":$"{warmth.Value} FRIA";fogDensityValue.Text=fogDensity.Value==0?"ORIGINAL":fogDensity.Value>0?$"+{fogDensity.Value}%":$"{fogDensity.Value}%";fogColorButton.Text=$"RGB  {fogColor.R}, {fogColor.G}, {fogColor.B}";fogColorButton.Invalidate();}
    private void ChooseFogColor(object? sender,EventArgs e){using var picker=new ColorDialog{Color=fogColor,FullOpen=true};if(picker.ShowDialog(this)!=DialogResult.OK)return;fogColor=picker.Color;if(preset.SelectedIndex!=0){syncing=true;preset.SelectedIndex=0;syncing=false;}UpdateLabels();ApplyPreview();}
    private void PaintColorSwatch(object? sender,PaintEventArgs e){using var brush=new SolidBrush(fogColor);using var pen=new Pen(Color.FromArgb(100,Color.White));e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.FillRectangle(brush,14,12,26,24);e.Graphics.DrawRectangle(pen,14,12,26,24);}
    private void ApplyPreview(){original.Restore(scene);IEnumerable<LitGroup> groups=allGroups.Checked?scene.Groups:scene.Groups.Where(x=>x.SlotIndex==selectedSlot);foreach(LitGroup g in groups)Adjust(g);preview();}
    private void Adjust(LitGroup g)
    {
        (g.BaseR,g.BaseG,g.BaseB)=Tone(g.BaseR,g.BaseG,g.BaseB);(g.ExtraAmbient1R,g.ExtraAmbient1G,g.ExtraAmbient1B)=Tone(g.ExtraAmbient1R,g.ExtraAmbient1G,g.ExtraAmbient1B);(g.ExtraAmbient2R,g.ExtraAmbient2G,g.ExtraAmbient2B)=Tone(g.ExtraAmbient2R,g.ExtraAmbient2G,g.ExtraAmbient2B);
        foreach(LitLight l in g.Lights){(l.ColorR,l.ColorG,l.ColorB)=Tone(l.ColorR,l.ColorG,l.ColorB);if(!preserveSpecial.Checked||l.Attribute==0)l.Intensity=Math.Clamp(l.Intensity*MathF.Sqrt(brightness.Value/100f),0f,64f);}
        bool valid=g.FogType!=0&&float.IsFinite(g.FogEnd)&&g.FogEnd>g.FogStart;if(!valid&&enableFog.Checked){g.FogType=2;g.FogStart=1200f;g.FogEnd=12000f;g.FogA=128;valid=true;}if(valid){float factor=Math.Clamp(1f-fogDensity.Value/100f,.25f,1.6f);g.FogStart*=1f+(factor-1f)*.45f;g.FogEnd*=factor;float blend=Math.Clamp(MathF.Abs(fogDensity.Value)/70f,0f,1f);g.FogR=Mix(g.FogR,fogColor.R,blend);g.FogG=Mix(g.FogG,fogColor.G,blend);g.FogB=Mix(g.FogB,fogColor.B,blend);g.MirrorFogType=g.FogType;g.MirrorFogStart=g.FogStart;g.MirrorFogEnd=g.FogEnd;g.MirrorFogR=g.FogR;g.MirrorFogG=g.FogG;g.MirrorFogB=g.FogB;}
    }
    private (byte,byte,byte) Tone(byte r,byte g,byte b){float gray=(r*.299f+g*.587f+b*.114f),sat=saturation.Value/100f,light=brightness.Value/100f,w=warmth.Value;float rr=(gray+(r-gray)*sat)*light+w*.65f,gg=(gray+(g-gray)*sat)*light+w*.12f,bb=(gray+(b-gray)*sat)*light-w*.55f;if(sceneTintStrength>0f){float mean=Math.Max(1f,(sceneTint.R+sceneTint.G+sceneTint.B)/3f);rr*=1f+(sceneTint.R/mean-1f)*sceneTintStrength;gg*=1f+(sceneTint.G/mean-1f)*sceneTintStrength;bb*=1f+(sceneTint.B/mean-1f)*sceneTintStrength;}return(ToByte(rr),ToByte(gg),ToByte(bb));}
    private static byte Mix(byte a,int b,float t)=>ToByte(a+(b-a)*t);private static byte ToByte(float v)=>(byte)Math.Clamp((int)MathF.Round(v),0,255);
    private void OnClosing(object? sender,FormClosingEventArgs e){if(!accepted){original.Restore(scene);preview();}}

    private sealed class SceneSnapshot
    {
        private readonly GroupState[] groups;private SceneSnapshot(GroupState[] groups)=>this.groups=groups;
        public static SceneSnapshot Capture(LitScene scene)=>new(scene.Groups.Select(GroupState.Capture).ToArray());
        public void Restore(LitScene scene){for(int i=0;i<Math.Min(groups.Length,scene.Groups.Count);i++)groups[i].Restore(scene.Groups[i]);}
    }
    private sealed record GroupState(byte BaseR,byte BaseG,byte BaseB,byte E1R,byte E1G,byte E1B,byte E2R,byte E2G,byte E2B,uint FogType,float FogStart,float FogEnd,byte FogR,byte FogG,byte FogB,byte FogA,uint MirrorType,float MirrorStart,float MirrorEnd,byte MirrorR,byte MirrorG,byte MirrorB,LightState[] Lights)
    {
        public static GroupState Capture(LitGroup g)=>new(g.BaseR,g.BaseG,g.BaseB,g.ExtraAmbient1R,g.ExtraAmbient1G,g.ExtraAmbient1B,g.ExtraAmbient2R,g.ExtraAmbient2G,g.ExtraAmbient2B,g.FogType,g.FogStart,g.FogEnd,g.FogR,g.FogG,g.FogB,g.FogA,g.MirrorFogType,g.MirrorFogStart,g.MirrorFogEnd,g.MirrorFogR,g.MirrorFogG,g.MirrorFogB,g.Lights.Select(LightState.Capture).ToArray());
        public void Restore(LitGroup g){g.BaseR=BaseR;g.BaseG=BaseG;g.BaseB=BaseB;g.ExtraAmbient1R=E1R;g.ExtraAmbient1G=E1G;g.ExtraAmbient1B=E1B;g.ExtraAmbient2R=E2R;g.ExtraAmbient2G=E2G;g.ExtraAmbient2B=E2B;g.FogType=FogType;g.FogStart=FogStart;g.FogEnd=FogEnd;g.FogR=FogR;g.FogG=FogG;g.FogB=FogB;g.FogA=FogA;g.MirrorFogType=MirrorType;g.MirrorFogStart=MirrorStart;g.MirrorFogEnd=MirrorEnd;g.MirrorFogR=MirrorR;g.MirrorFogG=MirrorG;g.MirrorFogB=MirrorB;for(int i=0;i<Math.Min(Lights.Length,g.Lights.Count);i++)Lights[i].Restore(g.Lights[i]);}
    }
    private sealed record LightState(byte R,byte G,byte B,float Intensity){public static LightState Capture(LitLight l)=>new(l.ColorR,l.ColorG,l.ColorB,l.Intensity);public void Restore(LitLight l){l.ColorR=R;l.ColorG=G;l.ColorB=B;l.Intensity=Intensity;}}
}
