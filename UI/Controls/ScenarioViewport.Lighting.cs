using OpenTK.Graphics.OpenGL4;
using RE4_PS2_MOD_WORKSPACE.Core.Lighting;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport
{
    private LitScene? litScene;
    private int selectedLitGroup=-1, selectedLitLight=-1;
    private bool litGpuDirty;
    private int litVao,litVbo,litVertexCount,litShader,litMvp;
    private int uLitEnabled,uLitCount,uLitAmbient;
    private int uFogEnabled,uFogType,uFogColor,uFogRange,uCameraPosition;
    private readonly int[] uLitPosRange=new int[32],uLitColorIntensity=new int[32],uLitDirectionType=new int[32];
    public bool LightingVisible { get; set; } = true;
    public LitScene? LitScene => litScene;

    public void SetLitScene(LitScene? value){litScene=value;selectedLitGroup=selectedLitLight=-1;litGpuDirty=true;Invalidate();}
    public void SelectLitLight(int group,int light){selectedLitGroup=group;selectedLitLight=light;litGpuDirty=true;Invalidate();}
    public void RefreshLitGeometry(){litGpuDirty=true;Invalidate();}

    private void InitializeLitShaderBindings()
    {
        uLitEnabled=GL.GetUniformLocation(shaderProgram,"uLitEnabled");uLitCount=GL.GetUniformLocation(shaderProgram,"uLitCount");uLitAmbient=GL.GetUniformLocation(shaderProgram,"uLitAmbient");
        uFogEnabled=GL.GetUniformLocation(shaderProgram,"uFogEnabled");uFogType=GL.GetUniformLocation(shaderProgram,"uFogType");uFogColor=GL.GetUniformLocation(shaderProgram,"uFogColor");uFogRange=GL.GetUniformLocation(shaderProgram,"uFogRange");uCameraPosition=GL.GetUniformLocation(shaderProgram,"uCameraPosition");
        for(int i=0;i<32;i++){uLitPosRange[i]=GL.GetUniformLocation(shaderProgram,$"uLitPosRange[{i}]");uLitColorIntensity[i]=GL.GetUniformLocation(shaderProgram,$"uLitColorIntensity[{i}]");uLitDirectionType[i]=GL.GetUniformLocation(shaderProgram,$"uLitDirectionType[{i}]");}
    }

    private void ApplyLitShaderUniforms()
    {
        LitGroup? group=litScene?.Groups.FirstOrDefault(x=>x.SlotIndex==selectedLitGroup)??litScene?.Groups.FirstOrDefault();
        if(!LightingVisible||group==null){GL.Uniform1(uLitEnabled,0);GL.Uniform1(uLitCount,0);GL.Uniform1(uFogEnabled,0);return;}
        float fogStart=group.FogStart/100f,fogEnd=group.FogEnd/100f;bool fogValid=group.FogType!=0&&float.IsFinite(fogStart)&&float.IsFinite(fogEnd)&&fogEnd>fogStart;
        GL.Uniform1(uFogEnabled,fogValid?1:0);GL.Uniform1(uFogType,(int)group.FogType);GL.Uniform3(uFogColor,group.FogR/255f,group.FogG/255f,group.FogB/255f);GL.Uniform2(uFogRange,fogStart,fogEnd);GL.Uniform3(uCameraPosition,cameraPosition.X,cameraPosition.Y,cameraPosition.Z);
        LitLight[] lights=group.Lights.Where(x=>x.IsActive).Take(32).ToArray();GL.Uniform1(uLitEnabled,1);GL.Uniform1(uLitCount,lights.Length);
        float ambientScale=Math.Max(.35f,group.SmdMultiplier/4f);GL.Uniform4(uLitAmbient,.12f+group.BaseR/255f*ambientScale,.12f+group.BaseG/255f*ambientScale,.12f+group.BaseB/255f*ambientScale,1f);
        for(int i=0;i<lights.Length;i++)
        {
            LitLight light=lights[i];NVector3 p=light.DisplayPosition;float range=Math.Max(.01f,MathF.Abs(light.Range)/100f);
            NVector3 direction=new(light.PositionX2,light.PositionY2,light.PositionZ2);if(light.Type!=5){NVector3 primary=new(light.PositionX,light.PositionY,light.PositionZ);if(primary.LengthSquared()>.001f)direction-=primary;}if(direction.LengthSquared()<.001f)direction=new NVector3(-.35f,.75f,-.55f);else direction=NVector3.Normalize(direction);
            GL.Uniform4(uLitPosRange[i],p.X,p.Y,p.Z,range);GL.Uniform4(uLitColorIntensity[i],light.ColorR/255f,light.ColorG/255f,light.ColorB/255f,Math.Clamp(light.Intensity,0f,16f));GL.Uniform4(uLitDirectionType[i],direction.X,direction.Y,direction.Z,(float)light.Type);
        }
    }

    private void UploadLit()
    {
        litGpuDirty=false;litVertexCount=0;
        if(litScene==null)return;
        var vertices=new List<float>();
        foreach(LitGroup group in litScene.Groups)
        foreach(LitLight light in group.Lights)
        {
            if(selectedLitGroup>=0&&group.SlotIndex!=selectedLitGroup)continue;
            if(!light.IsActive)continue;
            NVector3 c=light.DisplayPosition;
            float radius=Math.Clamp(MathF.Abs(light.Range)/100f,2f,5000f);
            float marker=Math.Clamp(radius*0.035f,2f,40f);
            float r=light.ColorR/255f,g=light.ColorG/255f,b=light.ColorB/255f;
            bool selected=group.SlotIndex==selectedLitGroup&&light.Index==selectedLitLight;
            if(selected){r=1f;g=.8f;b=.1f;marker*=1.6f;}
            Line(c-new NVector3(marker,0,0),c+new NVector3(marker,0,0),r,g,b);
            Line(c-new NVector3(0,marker,0),c+new NVector3(0,marker,0),r,g,b);
            Line(c-new NVector3(0,0,marker),c+new NVector3(0,0,marker),r,g,b);
            const int segments=40;
            for(int i=0;i<segments;i++)
            {
                float a=i*MathF.Tau/segments,z=(i+1)*MathF.Tau/segments;
                Line(c+new NVector3(MathF.Cos(a)*radius,0,MathF.Sin(a)*radius),c+new NVector3(MathF.Cos(z)*radius,0,MathF.Sin(z)*radius),r,g,b);
                Line(c+new NVector3(MathF.Cos(a)*radius,MathF.Sin(a)*radius,0),c+new NVector3(MathF.Cos(z)*radius,MathF.Sin(z)*radius,0),r,g,b);
            }
        }
        if(litShader==0)
        {
            const string vs="#version 330 core\nlayout(location=0) in vec3 aPos;layout(location=1) in vec3 aColor;uniform mat4 uMvp;out vec3 vColor;void main(){gl_Position=vec4(aPos,1.0)*uMvp;vColor=aColor;}";
            const string fs="#version 330 core\nin vec3 vColor;out vec4 FragColor;void main(){FragColor=vec4(vColor,1.0);}";
            int v=CompileShader(ShaderType.VertexShader,vs),f=CompileShader(ShaderType.FragmentShader,fs);litShader=GL.CreateProgram();GL.AttachShader(litShader,v);GL.AttachShader(litShader,f);GL.LinkProgram(litShader);GL.GetProgram(litShader,GetProgramParameterName.LinkStatus,out int ok);GL.DeleteShader(v);GL.DeleteShader(f);if(ok==0)throw new InvalidOperationException("OpenGL LIT shader: "+GL.GetProgramInfoLog(litShader));litMvp=GL.GetUniformLocation(litShader,"uMvp");
        }
        if(litVao==0){litVao=GL.GenVertexArray();litVbo=GL.GenBuffer();}
        float[] data=vertices.ToArray();litVertexCount=data.Length/6;
        GL.BindVertexArray(litVao);GL.BindBuffer(BufferTarget.ArrayBuffer,litVbo);GL.BufferData(BufferTarget.ArrayBuffer,data.Length*sizeof(float),data,BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,6*sizeof(float),0);GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,6*sizeof(float),3*sizeof(float));GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        void Line(NVector3 a,NVector3 b,float r,float g,float blue){vertices.AddRange(new[]{a.X,a.Y,a.Z,r,g,blue,b.X,b.Y,b.Z,r,g,blue});}
    }

    private void DrawLitGpu()
    {
        if(!LightingVisible||litVertexCount==0)return;
        GL.UseProgram(litShader);var mvp=BuildMvp();GL.UniformMatrix4(litMvp,true,ref mvp);GL.Disable(EnableCap.CullFace);GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(litVao);GL.LineWidth(2f);GL.DrawArrays(PrimitiveType.Lines,0,litVertexCount);GL.LineWidth(1f);GL.Enable(EnableCap.CullFace);
    }

    private void DisposeLitGpu(){if(litVbo!=0)GL.DeleteBuffer(litVbo);if(litVao!=0)GL.DeleteVertexArray(litVao);if(litShader!=0)GL.DeleteProgram(litShader);litVbo=litVao=litShader=0;}
}
