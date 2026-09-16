using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class ExternalModelImportDialog:AppForm
{
    private readonly CheckBox autoFit=new(){Text="Encaixar no tamanho e centro do original",Checked=true,AutoSize=true};
    private readonly CheckBox weights=new(){Text="Gerar pesos automáticos usando os ossos do receptor",AutoSize=true};
    private readonly CheckBox companionTexture=new(){Text="Importar PNG com o mesmo nome para o TPL ativo",Checked=true,AutoSize=true};
    private readonly NumericUpDown scale=N(1,.01m,100m,.05m),tx=N(0,-100000,100000,.1m),ty=N(0,-100000,100000,.1m),tz=N(0,-100000,100000,.1m),rx=N(0,-360,360,1),ry=N(0,-360,360,1),rz=N(0,-360,360,1);
    private readonly ModelAlignmentPreview preview;
    public bool AutoWeight=>weights.Checked;public bool ImportCompanionTexture=>companionTexture.Checked;
    public ExternalModelImportOptions Options=>new(autoFit.Checked,(float)scale.Value,new((float)tx.Value,(float)ty.Value,(float)tz.Value),new((float)rx.Value,(float)ry.Value,(float)rz.Value));
    public ExternalModelImportDialog(string file,ExternalModelStats stats,bool receiverHasBones,IReadOnlyList<ScenarioTriangle> receiver)
    {
        Text="Alinhar e importar modelo";StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(1080,720);MinimumSize=new Size(880,620);BackColor=Color.FromArgb(18,20,24);ForeColor=Color.Gainsboro;Font=new Font("Segoe UI",9);
        weights.Enabled=receiverHasBones&&Path.GetExtension(file).Equals(".obj",StringComparison.OrdinalIgnoreCase);weights.Checked=weights.Enabled;companionTexture.Enabled=File.Exists(Path.ChangeExtension(file,".png"));companionTexture.Checked=companionTexture.Enabled;
        preview=new ModelAlignmentPreview(receiver,ExternalPs2ModelConverter.ReadPreview(file),()=>Options){Dock=DockStyle.Fill,BackColor=Color.FromArgb(11,13,17)};
        var controls=new TableLayoutPanel{Dock=DockStyle.Right,Width=355,ColumnCount=4,Padding=new Padding(14),AutoScroll=true,BackColor=Color.FromArgb(24,27,32)};controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,122));for(int i=0;i<3;i++)controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.33f));
        var title=new Label{Text=Path.GetFileName(file),Dock=DockStyle.Fill,Height=34,Font=new Font(Font.FontFamily,11,FontStyle.Bold),ForeColor=Color.White};controls.Controls.Add(title,0,0);controls.SetColumnSpan(title,4);
        var info=new Label{Text=$"{stats.Vertices:N0} vértices • {stats.Faces:N0} faces • {stats.Uvs:N0} UVs\n{(stats.HasSkeleton?"Com esqueleto":"Sem esqueleto")}{(stats.IsLarge?"  •  ⚠ malha pesada":"")}",Dock=DockStyle.Fill,Height=55,ForeColor=stats.IsLarge?Color.FromArgb(255,180,90):Color.Silver};controls.Controls.Add(info,0,1);controls.SetColumnSpan(info,4);
        controls.Controls.Add(autoFit,0,2);controls.SetColumnSpan(autoFit,4);controls.Controls.Add(weights,0,3);controls.SetColumnSpan(weights,4);controls.Controls.Add(companionTexture,0,4);controls.SetColumnSpan(companionTexture,4);
        AddRow(controls,5,"Escala",scale,null,null);AddRow(controls,6,"Posição X/Y/Z",tx,ty,tz);AddRow(controls,7,"Rotação X/Y/Z",rx,ry,rz);
        var hints=new Label{Text="Mouse esquerdo: orbitar\nRoda: zoom\nCinza: original  •  Amarelo: novo",Dock=DockStyle.Fill,Height=64,ForeColor=Color.FromArgb(150,160,176)};controls.Controls.Add(hints,0,8);controls.SetColumnSpan(hints,4);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=50,FlowDirection=FlowDirection.RightToLeft};Button import=B("IMPORTAR",DialogResult.OK,115),cancel=B("CANCELAR",DialogResult.Cancel,95),reset=B("RESETAR",DialogResult.None,90);reset.Click+=(_,_)=>{autoFit.Checked=true;scale.Value=1;tx.Value=ty.Value=tz.Value=rx.Value=ry.Value=rz.Value=0;};buttons.Controls.Add(import);buttons.Controls.Add(cancel);buttons.Controls.Add(reset);controls.Controls.Add(buttons,0,9);controls.SetColumnSpan(buttons,4);
        foreach(Control c in new Control[]{autoFit,scale,tx,ty,tz,rx,ry,rz})if(c is NumericUpDown n)n.ValueChanged+=(_,_)=>preview.Invalidate();else if(c is CheckBox k)k.CheckedChanged+=(_,_)=>preview.Invalidate();
        Controls.Add(preview);Controls.Add(controls);AcceptButton=import;CancelButton=cancel;
    }
    private static NumericUpDown N(decimal value,decimal min,decimal max,decimal increment)=>new(){Value=value,Minimum=min,Maximum=max,Increment=increment,DecimalPlaces=2,Dock=DockStyle.Fill,BackColor=Color.FromArgb(30,34,40),ForeColor=Color.White};
    private static void AddRow(TableLayoutPanel p,int row,string label,Control a,Control? b,Control? c){p.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.Silver},0,row);p.Controls.Add(a,1,row);if(b!=null)p.Controls.Add(b,2,row);if(c!=null)p.Controls.Add(c,3,row);}
    private static Button B(string text,DialogResult result,int width){var b=new Button{Text=text,DialogResult=result,Width=width,Height=32,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(56,64,76),ForeColor=Color.White};b.FlatAppearance.BorderSize=0;return b;}
}

internal sealed class ModelAlignmentPreview:Control
{
    private readonly IReadOnlyList<ScenarioTriangle> receiver,source;private readonly Func<ExternalModelImportOptions> options;private float yaw=-.65f,pitch=.35f,zoom=1;private Point drag;
    public ModelAlignmentPreview(IReadOnlyList<ScenarioTriangle> receiver,IReadOnlyList<ScenarioTriangle> source,Func<ExternalModelImportOptions> options){this.receiver=receiver;this.source=source;this.options=options;DoubleBuffered=true;MouseDown+=(_,e)=>drag=e.Location;MouseMove+=(_,e)=>{if(e.Button!=MouseButtons.Left)return;yaw+=(e.X-drag.X)*.01f;pitch=Math.Clamp(pitch+(e.Y-drag.Y)*.01f,-1.45f,1.45f);drag=e.Location;Invalidate();};MouseWheel+=(_,e)=>{zoom=Math.Clamp(zoom*(e.Delta>0?1.12f:.89f),.15f,8);Invalidate();};}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;ExternalModelImportOptions o=options();var transformed=source.Select(t=>new[]{ExternalPs2ModelConverter.TransformPreviewPoint(t.A,source,receiver,o),ExternalPs2ModelConverter.TransformPreviewPoint(t.B,source,receiver,o),ExternalPs2ModelConverter.TransformPreviewPoint(t.C,source,receiver,o)}).ToArray();var all=receiver.SelectMany(t=>new[]{t.A,t.B,t.C}).Concat(transformed.SelectMany(x=>x)).ToArray();if(all.Length==0)return;Vector3 min=all.Aggregate(new Vector3(float.PositiveInfinity),Vector3.Min),max=all.Aggregate(new Vector3(float.NegativeInfinity),Vector3.Max),center=(min+max)*.5f;float extent=Math.Max(.001f,(max-min).Length());float factor=Math.Min(Width,Height)*.72f/extent*zoom;PointF P(Vector3 p){p-=center;float cy=MathF.Cos(yaw),sy=MathF.Sin(yaw),cp=MathF.Cos(pitch),sp=MathF.Sin(pitch);float x=p.X*cy-p.Z*sy,z=p.X*sy+p.Z*cy,y=p.Y*cp-z*sp;return new(Width*.5f+x*factor,Height*.5f-y*factor);}using var oldPen=new Pen(Color.FromArgb(105,155,165,180),1);using var newPen=new Pen(Color.FromArgb(225,255,190,35),1.5f);foreach(var t in receiver)e.Graphics.DrawPolygon(oldPen,new[]{P(t.A),P(t.B),P(t.C)});foreach(var t in transformed)e.Graphics.DrawPolygon(newPen,new[]{P(t[0]),P(t[1]),P(t[2])});using var axis=new Pen(Color.FromArgb(80,255,255,255));e.Graphics.DrawLine(axis,Width/2-8,Height/2,Width/2+8,Height/2);e.Graphics.DrawLine(axis,Width/2,Height/2-8,Width/2,Height/2+8);}
}
