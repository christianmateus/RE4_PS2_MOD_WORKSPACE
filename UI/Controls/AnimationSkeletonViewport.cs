using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class AnimationSkeletonViewport : GLControl
{
    private Ps2BinSkeleton? skeleton; private FcvAnimation? animation; private float frame;
    private bool glReady, gpuDirty=true; private int shader,uMvp,uColor,vao,vbo; private int vertexCount;
    private float yaw=.55f,pitch=-.18f,distance=1800f; private NVector3 target=new(0,900,0); private Point lastMouse; private bool orbiting;
    private FcvSkeletonPose? lastPose; private Matrix4 lastMvp; private bool haveProjection; private int[] segmentStartByBone=Array.Empty<int>();
    private int selectedBoneIndex=-1, hoverBoneIndex=-1; private readonly ToolTip hoverTip=new(){InitialDelay=120,ReshowDelay=60,AutoPopDelay=1800,ShowAlways=true};
    private readonly Font boneIdFont=new("Segoe UI",7.5f,FontStyle.Bold);

    public bool ShowBoneIds { get; set; }
    public int SelectedBoneIndex => selectedBoneIndex;
    public Ps2BinSkeleton? Skeleton => skeleton;
    public FcvSkeletonPose? CurrentPose => lastPose;
    public event EventHandler? SelectedBoneChanged;

    public AnimationSkeletonViewport():base(new GLControlSettings{API=ContextAPI.OpenGL,APIVersion=new Version(3,3),Profile=ContextProfile.Core,NumberOfSamples=4,IsEventDriven=true})
    { BackColor=Color.FromArgb(8,10,13); TabStop=true; }

    public void SetSkeleton(Ps2BinSkeleton? value){skeleton=value;selectedBoneIndex=-1;hoverBoneIndex=-1;gpuDirty=true;Fit();Invalidate();SelectedBoneChanged?.Invoke(this,EventArgs.Empty);}
    public void SetAnimation(FcvAnimation? value){animation=value;frame=0;gpuDirty=true;if(skeleton!=null)Fit();Invalidate();}
    public void SetFrame(float value){frame=value;gpuDirty=true;Invalidate();}
    public void SelectBone(int index){if(skeleton==null||index<0||index>=skeleton.Bones.Count)index=-1;if(selectedBoneIndex==index)return;selectedBoneIndex=index;Invalidate();SelectedBoneChanged?.Invoke(this,EventArgs.Empty);}
    public void Fit()
    {
        if(skeleton==null||skeleton.Bones.Count==0)return;
        // Enquadra a pose que está realmente sendo exibida. O BIN possui alguns joints auxiliares
        // nas extremidades; usar o min/max absoluto fazia esses pontos dominarem a câmera e deixava
        // o humanoide pequeno e deslocado para baixo. Um bounds aparado mantém mãos/pés visíveis,
        // mas impede um único helper de estragar o enquadramento.
        var pose=FcvSkeletonEvaluator.Evaluate(skeleton,animation,frame);
        var xs=pose.WorldPositions.Select(p=>p.X).OrderBy(v=>v).ToArray();
        var ys=pose.WorldPositions.Select(p=>p.Y).OrderBy(v=>v).ToArray();
        var zs=pose.WorldPositions.Select(p=>p.Z).OrderBy(v=>v).ToArray();
        int trim=pose.WorldPositions.Length>=20?1:0;
        int hi = xs.Length - 1 - trim;
        float minX=xs[trim],maxX=xs[hi],minY=ys[trim],maxY=ys[hi],minZ=zs[trim],maxZ=zs[hi];
        var min=new NVector3(minX,minY,minZ);var max=new NVector3(maxX,maxY,maxZ);
        target=(min+max)*.5f;
        var ext=max-min;float size=Math.Max(280f,Math.Max(ext.X,Math.Max(ext.Y,ext.Z)));
        distance=size*1.35f; yaw=.55f;pitch=-.10f;
    }

    protected override void OnLoad(EventArgs e){base.OnLoad(e);MakeCurrent();GL.ClearColor(BackColor);GL.Enable(EnableCap.DepthTest);CreateShader();GL.GenVertexArrays(1,out vao);GL.GenBuffers(1,out vbo);glReady=true;gpuDirty=true;}
    protected override void OnResize(EventArgs e){base.OnResize(e);if(glReady){MakeCurrent();GL.Viewport(0,0,Math.Max(1,ClientSize.Width),Math.Max(1,ClientSize.Height));Invalidate();}}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); if(!glReady)return; MakeCurrent();GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit); if(skeleton==null){SwapBuffers();return;} if(gpuDirty)Upload();
        GL.UseProgram(shader); var eye=GetEye(); var view=Matrix4.LookAt(new Vector3(eye.X,eye.Y,eye.Z),new Vector3(target.X,target.Y,target.Z),Vector3.UnitY); float aspect=Math.Max(.1f,ClientSize.Width/(float)Math.Max(1,ClientSize.Height)); var proj=Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45),aspect,1f,100000f); var mvp=view*proj; lastMvp=mvp;haveProjection=true;GL.UniformMatrix4(uMvp,true,ref mvp);
        GL.BindVertexArray(vao); GL.LineWidth(2.2f); GL.Uniform4(uColor,.30f,.78f,1f,1f); GL.DrawArrays(PrimitiveType.Lines,0,vertexCount);
        GL.PointSize(7f);GL.Uniform4(uColor,1f,.78f,.25f,1f);GL.DrawArrays(PrimitiveType.Points,0,vertexCount);
        if(selectedBoneIndex>=0 && selectedBoneIndex<segmentStartByBone.Length && segmentStartByBone[selectedBoneIndex]>=0)
        {
            GL.LineWidth(5f);GL.Uniform4(uColor,1f,.22f,.22f,1f);GL.DrawArrays(PrimitiveType.Lines,segmentStartByBone[selectedBoneIndex],2);
            GL.PointSize(11f);GL.DrawArrays(PrimitiveType.Points,segmentStartByBone[selectedBoneIndex],2);
        }
        GL.BindVertexArray(0);SwapBuffers();
        if(ShowBoneIds && lastPose!=null) DrawBoneIds(e.Graphics);
    }
    private void Upload()
    {
        gpuDirty=false;if(skeleton==null)return;lastPose=FcvSkeletonEvaluator.Evaluate(skeleton,animation,frame);var verts=new List<float>();segmentStartByBone=Enumerable.Repeat(-1,skeleton.Bones.Count).ToArray();
        for(int i=0;i<skeleton.Bones.Count;i++){int p=skeleton.Bones[i].ParentIndex;if(p<0)continue;segmentStartByBone[i]=verts.Count/3;Add(verts,lastPose.WorldPositions[p]);Add(verts,lastPose.WorldPositions[i]);}
        vertexCount=verts.Count/3;GL.BindVertexArray(vao);GL.BindBuffer(BufferTarget.ArrayBuffer,vbo);GL.BufferData(BufferTarget.ArrayBuffer,verts.Count*sizeof(float),verts.ToArray(),BufferUsageHint.DynamicDraw);GL.EnableVertexAttribArray(0);GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,3*sizeof(float),0);GL.BindVertexArray(0);
        if(selectedBoneIndex>=0)SelectedBoneChanged?.Invoke(this,EventArgs.Empty);
    }
    private static void Add(List<float> v,NVector3 p){v.Add(p.X);v.Add(p.Y);v.Add(p.Z);}
    private NVector3 GetEye(){float cp=MathF.Cos(pitch);var dir=new NVector3(MathF.Sin(yaw)*cp,MathF.Sin(pitch),MathF.Cos(yaw)*cp);return target-dir*distance;}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);Focus();
        if(e.Button==MouseButtons.Right){orbiting=true;lastMouse=e.Location;Cursor=Cursors.SizeAll;return;}
        if(e.Button==MouseButtons.Left){int hit=HitTestBone(e.Location,14f);if(hit>=0)SelectBone(hit);}
    }
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Right){orbiting=false;Cursor=Cursors.Default;}}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(orbiting){int dx=e.X-lastMouse.X,dy=e.Y-lastMouse.Y;lastMouse=e.Location;yaw-=dx*.006f;pitch=Math.Clamp(pitch-dy*.006f,-1.45f,1.45f);Invalidate();return;}
        int hit=HitTestBone(e.Location,11f); if(hit!=hoverBoneIndex){hoverBoneIndex=hit;if(hit>=0&&skeleton!=null&&lastPose!=null){var b=skeleton.Bones[hit];var p=lastPose.WorldPositions[hit];hoverTip.Show($"Bone 0x{b.Id:X2}  Index {hit}\nParent: {(b.ParentIndex<0?"ROOT":$"0x{skeleton.Bones[b.ParentIndex].Id:X2}")}\nWorld: {p.X:0.##}, {p.Y:0.##}, {p.Z:0.##}",this,e.X+14,e.Y+16,1400);}else hoverTip.Hide(this);}
    }
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);distance*=e.Delta>0?.88f:1.14f;distance=Math.Clamp(distance,25f,100000f);Invalidate();}
    protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);Fit();Invalidate();}
    private int HitTestBone(Point mouse,float radius)
    {
        if(skeleton==null||lastPose==null||!haveProjection)return-1;float best=radius*radius;int bestIndex=-1;
        for(int i=0;i<lastPose.WorldPositions.Length;i++)
        {
            if(TryProject(lastPose.WorldPositions[i],out PointF s)){float dx=s.X-mouse.X,dy=s.Y-mouse.Y,d2=dx*dx+dy*dy;if(d2<best){best=d2;bestIndex=i;}}
            int p=skeleton.Bones[i].ParentIndex;if(p<0)continue;if(!TryProject(lastPose.WorldPositions[p],out PointF a)||!TryProject(lastPose.WorldPositions[i],out PointF b))continue;
            float dLine=DistanceToSegmentSquared(mouse,a,b);if(dLine<best){best=dLine;bestIndex=i;}
        }
        return bestIndex;
    }
    private static float DistanceToSegmentSquared(Point p,PointF a,PointF b)
    {
        float vx=b.X-a.X,vy=b.Y-a.Y,wx=p.X-a.X,wy=p.Y-a.Y;float len=vx*vx+vy*vy;if(len<.0001f)return wx*wx+wy*wy;float t=Math.Clamp((wx*vx+wy*vy)/len,0f,1f);float dx=p.X-(a.X+t*vx),dy=p.Y-(a.Y+t*vy);return dx*dx+dy*dy;
    }
    private bool TryProject(NVector3 p,out PointF screen)
    {
        screen=default;var v=new Vector4(p.X,p.Y,p.Z,1f);Vector4 c=Vector4.TransformRow(v,lastMvp);if(Math.Abs(c.W)<.00001f||c.W<=0)return false;float nx=c.X/c.W,ny=c.Y/c.W;if(nx < -1.25f||nx > 1.25f||ny < -1.25f||ny > 1.25f)return false;screen=new PointF((nx*.5f+.5f)*ClientSize.Width,(1f-(ny*.5f+.5f))*ClientSize.Height);return true;
    }
    private void DrawBoneIds(Graphics g)
    {
        if(skeleton==null||lastPose==null)return;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        for(int i=0;i<skeleton.Bones.Count;i++)if(TryProject(lastPose.WorldPositions[i],out PointF p)){string text=$"{skeleton.Bones[i].Id:X2}";SizeF sz=g.MeasureString(text,boneIdFont);var r=new RectangleF(p.X+5,p.Y-8,sz.Width+4,sz.Height);using var bg=new SolidBrush(Color.FromArgb(175,0,0,0));g.FillRectangle(bg,r);using var fg=new SolidBrush(i==selectedBoneIndex?Color.OrangeRed:Color.WhiteSmoke);g.DrawString(text,boneIdFont,fg,p.X+7,p.Y-8);}
    }
    private void CreateShader()
    {
        const string vs="#version 330 core\nlayout(location=0) in vec3 aPos;uniform mat4 uMvp;void main(){gl_Position=vec4(aPos,1.0)*uMvp;}";
        const string fs="#version 330 core\nout vec4 FragColor;uniform vec4 uColor;void main(){FragColor=uColor;}";
        int v=GL.CreateShader(ShaderType.VertexShader);GL.ShaderSource(v,vs);GL.CompileShader(v);int f=GL.CreateShader(ShaderType.FragmentShader);GL.ShaderSource(f,fs);GL.CompileShader(f);shader=GL.CreateProgram();GL.AttachShader(shader,v);GL.AttachShader(shader,f);GL.LinkProgram(shader);GL.DeleteShader(v);GL.DeleteShader(f);uMvp=GL.GetUniformLocation(shader,"uMvp");uColor=GL.GetUniformLocation(shader,"uColor");
    }
    protected override void Dispose(bool disposing){if(disposing){hoverTip.Dispose();boneIdFont.Dispose();}if(glReady&&!IsDisposed){try{MakeCurrent();if(vbo!=0)GL.DeleteBuffer(vbo);if(vao!=0)GL.DeleteVertexArray(vao);if(shader!=0)GL.DeleteProgram(shader);}catch{}}base.Dispose(disposing);}
}
