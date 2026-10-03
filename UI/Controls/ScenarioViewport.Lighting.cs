using OpenTK.Graphics.OpenGL4;
using RE4_PS2_MOD_WORKSPACE.Core.Lighting;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport
{
    private const int MaxPreviewLights=32;
    private LitScene? litScene;
    private long litAnimationEpoch=Stopwatch.GetTimestamp();
    private int selectedLitGroup=-1,selectedLitLight=-1;
    private bool litGpuDirty;
    private int litVao,litVbo,litVertexCount,litShader,litMvp;
    private int uLitEnabled,uLitCount,uLitAmbient,uLitTime,uLitSelectMask,uLitSceneScale;
    private float unlitSceneExposure=1f;
    private int uFogEnabled,uFogType,uFogColor,uFogRange,uCameraPosition,uCameraForward;
    private readonly int[] uLitPosRange=new int[MaxPreviewLights],uLitColorIntensity=new int[MaxPreviewLights],uLitDirectionType=new int[MaxPreviewLights];
    private readonly int[] uLitAttnA=new int[MaxPreviewLights],uLitAttnK=new int[MaxPreviewLights],uLitBehavior0=new int[MaxPreviewLights],uLitBehavior1=new int[MaxPreviewLights],uLitMeta=new int[MaxPreviewLights];
    public bool LightingVisible { get; set; }=true;
    public LitScene? LitScene=>litScene;

    public void SetLitScene(LitScene? value)
    {
        litScene=value;selectedLitGroup=selectedLitLight=-1;litGpuDirty=true;litAnimationEpoch=Stopwatch.GetTimestamp();
        if(value!=null)fpsRenderTimer.Start();else if(!ShowFps)fpsRenderTimer.Stop();
        Invalidate();
    }
    public void SelectLitLight(int group,int light){selectedLitGroup=group;selectedLitLight=light;litGpuDirty=true;Invalidate();}
    public void RefreshLitGeometry(){litGpuDirty=true;Invalidate();}
    private bool HasAnimatedLit=>LightingVisible&&litScene?.Groups.Any(g=>g.Lights.Any(l=>l.IsActive&&l.Attribute is 1 or 2 or 3 or 6 or 7 or 0x10))==true;

    private void InitializeLitShaderBindings()
    {
        uLitEnabled=GL.GetUniformLocation(shaderProgram,"uLitEnabled");uLitCount=GL.GetUniformLocation(shaderProgram,"uLitCount");uLitAmbient=GL.GetUniformLocation(shaderProgram,"uLitAmbient");uLitTime=GL.GetUniformLocation(shaderProgram,"uLitTime");uLitSelectMask=GL.GetUniformLocation(shaderProgram,"uLitSelectMask");uLitSceneScale=GL.GetUniformLocation(shaderProgram,"uLitSceneScale");
        uFogEnabled=GL.GetUniformLocation(shaderProgram,"uFogEnabled");uFogType=GL.GetUniformLocation(shaderProgram,"uFogType");uFogColor=GL.GetUniformLocation(shaderProgram,"uFogColor");uFogRange=GL.GetUniformLocation(shaderProgram,"uFogRange");uCameraPosition=GL.GetUniformLocation(shaderProgram,"uCameraPosition");uCameraForward=GL.GetUniformLocation(shaderProgram,"uCameraForward");
        for(int i=0;i<MaxPreviewLights;i++)
        {
            uLitPosRange[i]=GL.GetUniformLocation(shaderProgram,$"uLitPosRange[{i}]");uLitColorIntensity[i]=GL.GetUniformLocation(shaderProgram,$"uLitColorIntensity[{i}]");uLitDirectionType[i]=GL.GetUniformLocation(shaderProgram,$"uLitDirectionType[{i}]");
            uLitAttnA[i]=GL.GetUniformLocation(shaderProgram,$"uLitAttnA[{i}]");uLitAttnK[i]=GL.GetUniformLocation(shaderProgram,$"uLitAttnK[{i}]");uLitBehavior0[i]=GL.GetUniformLocation(shaderProgram,$"uLitBehavior0[{i}]");uLitBehavior1[i]=GL.GetUniformLocation(shaderProgram,$"uLitBehavior1[{i}]");uLitMeta[i]=GL.GetUniformLocation(shaderProgram,$"uLitMeta[{i}]");
        }
    }

    private void ApplyLitShaderUniforms()
    {
        LitGroup? group=litScene?.Groups.FirstOrDefault(x=>x.SlotIndex==selectedLitGroup)??litScene?.Groups.FirstOrDefault();
        if(!LightingVisible||group==null){GL.Uniform1(uLitEnabled,0);GL.Uniform1(uLitCount,0);GL.Uniform1(uLitSceneScale,unlitSceneExposure);GL.Uniform1(uFogEnabled,0);return;}
        float fogStart=group.FogStart/100f,fogEnd=group.FogEnd/100f;bool fogValid=group.FogType!=0&&float.IsFinite(fogStart)&&float.IsFinite(fogEnd)&&fogEnd>fogStart;
        NVector3 cameraForward=GetForward();
        GL.Uniform1(uFogEnabled,fogValid?1:0);GL.Uniform1(uFogType,(int)group.FogType);GL.Uniform3(uFogColor,group.FogR/255f,group.FogG/255f,group.FogB/255f);GL.Uniform2(uFogRange,fogStart,fogEnd);GL.Uniform3(uCameraPosition,cameraPosition.X,cameraPosition.Y,cameraPosition.Z);GL.Uniform3(uCameraForward,cameraForward.X,cameraForward.Y,cameraForward.Z);
        var lights=group.Lights.Select((light,index)=>(Light:light,SourceIndex:index)).Where(x=>x.Light.IsActive).Take(MaxPreviewLights).ToArray();GL.Uniform1(uLitEnabled,1);GL.Uniform1(uLitCount,lights.Length);GL.Uniform1(uLitTime,(float)((Stopwatch.GetTimestamp()-litAnimationEpoch)/(double)Stopwatch.Frequency));GL.Uniform1(uLitSelectMask,uint.MaxValue);
        GL.Uniform4(uLitAmbient,group.BaseR/255f,group.BaseG/255f,group.BaseB/255f,group.BaseA/255f);
        // GX TEV scale is encoded as 0/4 = 1x, 1/5 = 2x, 2/6 = 4x.
        // r106 uses 5, which is therefore 2x rather than a literal 5x.
        float sceneScale=group.SmdMultiplier switch{1 or 5=>2f,2 or 6=>4f,_=>1f};
        GL.Uniform1(uLitSceneScale,sceneScale);
        for(int i=0;i<lights.Length;i++)
        {
            LitLight l=lights[i].Light;ResolveLitTransform(l,out NVector3 p,out NVector3 rawDirection);NVector3 d=rawDirection;if(!Finite(d)||d.LengthSquared()<.000001f)d=new NVector3(-.35f,.75f,-.55f);else d=NVector3.Normalize(d);
            float radius=float.IsFinite(l.Range)?MathF.Abs(l.Range)/100f:0f;float intensity=float.IsFinite(l.Intensity)?Math.Clamp(l.Intensity,0f,64f):0f;float alpha=l.ColorA/128f;
            GL.Uniform4(uLitPosRange[i],p.X,p.Y,p.Z,radius);GL.Uniform4(uLitColorIntensity[i],l.ColorR/255f*alpha,l.ColorG/255f*alpha,l.ColorB/255f*alpha,intensity);GL.Uniform4(uLitDirectionType[i],d.X,d.Y,d.Z,l.Type);
            GL.Uniform4(uLitAttnA[i],Safe(l.A0),Safe(l.A1)/100f,Safe(l.A2),Safe(rawDirection.X)/100f);GL.Uniform4(uLitAttnK[i],Safe(l.K0),Safe(l.K1),Safe(l.K2),0f);
            GL.Uniform4(uLitBehavior0[i],Safe(l.WorkFloat(0)),Safe(l.WorkFloat(1)),Safe(l.WorkFloat(2)),Safe(l.WorkFloat(3)));GL.Uniform4(uLitBehavior1[i],Safe(l.WorkFloat(4)),Safe(l.WorkFloat(5)),Safe(l.WorkFloat(6)),Safe(l.WorkFloat(7)));
            int packedMaskAndIndex=(l.Mask<<8)|(lights[i].SourceIndex&0xFF);
            GL.Uniform4(uLitMeta[i],l.Attribute,l.FlickerRange/255f,(float)(l.Work0&0xFF),packedMaskAndIndex);
        }
        static bool Finite(NVector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
        static float Safe(float v)=>float.IsFinite(v)?v:0f;
    }

    private static float CalculateUnlitSceneExposure(ScenarioScene? scene)
    {
        if(scene==null)return 1f;
        // Average each embedded BIN once. Repeated entries must not bias the
        // exposure, and imported full-white models should remain a small part
        // of the room-wide reference rather than defining it.
        var levels=new List<float>();
        foreach(ScenarioEntry entry in scene.Entries.GroupBy(x=>x.BinId).Select(x=>x.First()))
        {
            if(entry.LocalTriangles.Count==0)continue;
            double sum=0;long count=0;
            foreach(ScenarioTriangle triangle in entry.LocalTriangles)
            {
                sum+=(triangle.ColorA.X+triangle.ColorA.Y+triangle.ColorA.Z)/3.0;
                sum+=(triangle.ColorB.X+triangle.ColorB.Y+triangle.ColorB.Z)/3.0;
                sum+=(triangle.ColorC.X+triangle.ColorC.Y+triangle.ColorC.Z)/3.0;
                count+=3;
            }
            if(count>0){float level=(float)(sum/count);if(float.IsFinite(level)&&level>0.001f)levels.Add(level);}
        }
        if(levels.Count==0)return 1f;
        levels.Sort();
        // Median is robust against a few unusually bright/dark special models.
        float median=levels[levels.Count/2];
        return Math.Clamp(0.65f/MathF.Max(0.01f,median),1f,6f);
    }

    private void UploadLit()
    {
        litGpuDirty=false;litVertexCount=0;if(litScene==null)return;var vertices=new List<float>();
        foreach(LitGroup group in litScene.Groups)foreach(LitLight light in group.Lights)
        {
            if(selectedLitGroup>=0&&group.SlotIndex!=selectedLitGroup||!light.IsActive)continue;
            ResolveLitTransform(light,out NVector3 c,out NVector3 displayDirection);float rawRadius=MathF.Abs(light.Range)/100f;float radius=Math.Clamp(rawRadius,2f,5000f);float marker=Math.Clamp((rawRadius>0?radius:.0f)*.035f,2f,40f);float r=light.ColorR/255f,g=light.ColorG/255f,b=light.ColorB/255f;bool selected=group.SlotIndex==selectedLitGroup&&light.Index==selectedLitLight;if(selected){r=1f;g=.8f;b=.1f;marker*=1.6f;}
            Line(c-new NVector3(marker,0,0),c+new NVector3(marker,0,0),r,g,b);Line(c-new NVector3(0,marker,0),c+new NVector3(0,marker,0),r,g,b);Line(c-new NVector3(0,0,marker),c+new NVector3(0,0,marker),r,g,b);
            if(rawRadius>0f){const int segments=40;for(int i=0;i<segments;i++){float a=i*MathF.Tau/segments,z=(i+1)*MathF.Tau/segments;Line(c+new NVector3(MathF.Cos(a)*radius,0,MathF.Sin(a)*radius),c+new NVector3(MathF.Cos(z)*radius,0,MathF.Sin(z)*radius),r,g,b);Line(c+new NVector3(MathF.Cos(a)*radius,MathF.Sin(a)*radius,0),c+new NVector3(MathF.Cos(z)*radius,MathF.Sin(z)*radius,0),r,g,b);}}
            NVector3 dir=displayDirection;if(float.IsFinite(dir.X)&&float.IsFinite(dir.Y)&&float.IsFinite(dir.Z)&&dir.LengthSquared()>.000001f){dir=NVector3.Normalize(dir);float length=rawRadius>0f?Math.Clamp(rawRadius*.35f,marker*4f,800f):marker*10f;NVector3 end=c+dir*length;Line(c,end,r,g,b);if(light.Type is 3 or 6&&light.A0>0f&&light.A0<89f){NVector3 side=NVector3.Cross(dir,MathF.Abs(dir.Y)<.95f?NVector3.UnitY:NVector3.UnitX);side=NVector3.Normalize(side)*MathF.Tan(light.A0*MathF.PI/180f)*length;Line(c,end+side,r,g,b);Line(c,end-side,r,g,b);}}
        }
        if(litShader==0){const string vs="#version 330 core\nlayout(location=0) in vec3 aPos;layout(location=1) in vec3 aColor;uniform mat4 uMvp;out vec3 vColor;void main(){gl_Position=vec4(aPos,1.0)*uMvp;vColor=aColor;}";const string fs="#version 330 core\nin vec3 vColor;out vec4 FragColor;void main(){FragColor=vec4(vColor,1.0);}";int v=CompileShader(ShaderType.VertexShader,vs),f=CompileShader(ShaderType.FragmentShader,fs);litShader=GL.CreateProgram();GL.AttachShader(litShader,v);GL.AttachShader(litShader,f);GL.LinkProgram(litShader);GL.GetProgram(litShader,GetProgramParameterName.LinkStatus,out int ok);GL.DeleteShader(v);GL.DeleteShader(f);if(ok==0)throw new InvalidOperationException("OpenGL LIT shader: "+GL.GetProgramInfoLog(litShader));litMvp=GL.GetUniformLocation(litShader,"uMvp");}
        if(litVao==0){litVao=GL.GenVertexArray();litVbo=GL.GenBuffer();}float[] data=vertices.ToArray();litVertexCount=data.Length/6;GL.BindVertexArray(litVao);GL.BindBuffer(BufferTarget.ArrayBuffer,litVbo);GL.BufferData(BufferTarget.ArrayBuffer,data.Length*sizeof(float),data,BufferUsageHint.DynamicDraw);GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,6*sizeof(float),0);GL.EnableVertexAttribArray(0);GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,6*sizeof(float),3*sizeof(float));GL.EnableVertexAttribArray(1);GL.BindVertexArray(0);GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        void Line(NVector3 a,NVector3 z,float cr,float cg,float cb){vertices.AddRange(new[]{a.X,a.Y,a.Z,cr,cg,cb,z.X,z.Y,z.Z,cr,cg,cb});}
    }

    private void ResolveLitTransform(LitLight light,out NVector3 position,out NVector3 direction)
    {
        position=light.DisplayPosition;direction=light.Direction;
        if((light.State&0x02)==0||light.ParentType!=2||scene==null)return;
        ScenarioEntry? parent=scene.Entries.FirstOrDefault(x=>x.FileOrder==light.ParentId);if(parent==null)return;
        position=new(position.X*parent.ScaleX,position.Y*parent.ScaleY,position.Z*parent.ScaleZ);
        position=Rotate(position,parent.RotationX,parent.RotationY,parent.RotationZ)+parent.Position;
        direction=Rotate(direction,parent.RotationX,parent.RotationY,parent.RotationZ);
        static NVector3 Rotate(NVector3 v,float x,float y,float z)
        {
            float c=MathF.Cos(x),s=MathF.Sin(x);v=new(v.X,v.Y*c-v.Z*s,v.Y*s+v.Z*c);c=MathF.Cos(y);s=MathF.Sin(y);v=new(v.X*c+v.Z*s,v.Y,-v.X*s+v.Z*c);c=MathF.Cos(z);s=MathF.Sin(z);return new(v.X*c-v.Y*s,v.X*s+v.Y*c,v.Z);
        }
    }

    private void DrawLitGpu(){if(!LightingVisible||litVertexCount==0)return;GL.UseProgram(litShader);var mvp=BuildMvp();GL.UniformMatrix4(litMvp,true,ref mvp);GL.Disable(EnableCap.CullFace);GL.Disable(EnableCap.Blend);GL.BindVertexArray(litVao);GL.LineWidth(2f);GL.DrawArrays(PrimitiveType.Lines,0,litVertexCount);GL.LineWidth(1f);GL.Enable(EnableCap.CullFace);GL.UseProgram(shaderProgram);}
    private void DisposeLitGpu(){if(litVbo!=0)GL.DeleteBuffer(litVbo);if(litVao!=0)GL.DeleteVertexArray(litVao);if(litShader!=0)GL.DeleteProgram(litShader);litVbo=litVao=litShader=0;}
}
