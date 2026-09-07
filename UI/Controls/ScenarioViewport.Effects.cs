using OpenTK.Graphics.OpenGL4;
using RE4_PS2_MOD_WORKSPACE.Core.Effects;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport
{
    private const float EffWorldScale=0.01f;
    private const int EffTickMilliseconds=33;
    private Ps2EffFile? effScene;
    private EffEntry? selectedEffEntry;
    private bool effGpuDirty;
    private int effVao, effVbo, effVertexCount, effShader, effMvp;
    private Bitmap? effTextureBitmap;
    private bool effTextureDirty;
    private int effTexture, effTextureVao, effTextureVbo, effTextureShader;
    private readonly Dictionary<byte,Bitmap> effTextureBitmaps=new();
    private readonly Dictionary<byte,int> effTextures=new();
    private readonly Dictionary<byte,List<Bitmap>> effAnimationBitmaps=new();
    private readonly Dictionary<byte,List<int>> effAnimationTextures=new();
    private System.Windows.Forms.Timer? effAnimationTimer;
    public bool EffectsVisible { get; set; } = true;
    public Ps2EffFile? EffScene => effScene;
    public event Action<EffEntry?>? EffEntryClicked;
    public int EffTextureCount=>effTextureBitmaps.Count;
    public int FireEmitterCount=>effScene?.Entries.Count(x=>x.ResourceId is 0x28 or 0x2E or 0x2C)??0;

    public void SetEffScene(Ps2EffFile? value){effScene=value;selectedEffEntry=null;effGpuDirty=true;Invalidate();}
    public void SelectEffEntry(EffEntry? entry){selectedEffEntry=entry;effGpuDirty=true;Invalidate();}
    public void SetEffTexture(Bitmap? bitmap)
    {
        Bitmap? old=effTextureBitmap;effTextureBitmap=bitmap==null?null:new Bitmap(bitmap);old?.Dispose();
        effTextureDirty=true;Invalidate();
    }
    public void SetEffTextures(IReadOnlyDictionary<byte,Bitmap>? textures)
    {
        foreach(Bitmap bitmap in effTextureBitmaps.Values)bitmap.Dispose();effTextureBitmaps.Clear();
        foreach(List<Bitmap> frames in effAnimationBitmaps.Values)foreach(Bitmap bitmap in frames)bitmap.Dispose();effAnimationBitmaps.Clear();effAnimationTimer?.Stop();
        if(textures!=null)foreach(var pair in textures)effTextureBitmaps[pair.Key]=new Bitmap(pair.Value);
        effTextureDirty=true;effGpuDirty=true;Invalidate();
    }
    public void SetEffAnimation(byte resourceId,IReadOnlyList<Bitmap>? frames)
    {
        if(effAnimationBitmaps.Remove(resourceId,out List<Bitmap>? old))foreach(Bitmap bitmap in old)bitmap.Dispose();
        if(frames is {Count:>0})effAnimationBitmaps[resourceId]=frames.Select(x=>new Bitmap(x)).ToList();
        effTextureDirty=true;
        if(effAnimationBitmaps.Count>0){effAnimationTimer??=CreateEffAnimationTimer();effAnimationTimer.Start();}else effAnimationTimer?.Stop();
        Invalidate();
    }
    private System.Windows.Forms.Timer CreateEffAnimationTimer(){var timer=new System.Windows.Forms.Timer{Interval=EffTickMilliseconds};timer.Tick+=(_,_)=>Invalidate();return timer;}
    public void RefreshEffGeometry(EffEntry? entry=null){if(entry!=null)selectedEffEntry=entry;effGpuDirty=true;Invalidate();}

    private void UploadEff()
    {
        effGpuDirty=false;
        var vertices=new List<float>();
        if(effScene!=null)foreach(EffEntry entry in effScene.Entries)
        {
            bool selected=ReferenceEquals(entry,selectedEffEntry);
            if(!selected)continue;
            NVector3 p=GetEffWorldPosition(entry);
            float pulse=1f+0.18f*MathF.Sin(Environment.TickCount64*0.008f);float size=1.8f*pulse,r=0.05f,g=0.95f,b=1f;
            if(r+g+b<0.15f){r=0.2f;g=0.8f;b=1f;}
            Line(p-new NVector3(size,0,0),p+new NVector3(size,0,0),r,g,b);
            Line(p-new NVector3(0,size,0),p+new NVector3(0,size,0),r,g,b);
            Line(p-new NVector3(0,0,size),p+new NVector3(0,0,size),r,g,b);
        }
        if(effShader==0)
        {
            const string vs="#version 330 core\nlayout(location=0)in vec3 aPos;layout(location=1)in vec3 aColor;uniform mat4 uMvp;out vec3 vColor;void main(){gl_Position=uMvp*vec4(aPos,1.0);vColor=aColor;}";
            const string fs="#version 330 core\nin vec3 vColor;out vec4 FragColor;void main(){FragColor=vec4(vColor,1.0);}";
            int v=CompileShader(ShaderType.VertexShader,vs),f=CompileShader(ShaderType.FragmentShader,fs);effShader=GL.CreateProgram();GL.AttachShader(effShader,v);GL.AttachShader(effShader,f);GL.LinkProgram(effShader);GL.GetProgram(effShader,GetProgramParameterName.LinkStatus,out int ok);GL.DeleteShader(v);GL.DeleteShader(f);if(ok==0)throw new InvalidOperationException("OpenGL EFF shader: "+GL.GetProgramInfoLog(effShader));effMvp=GL.GetUniformLocation(effShader,"uMvp");
        }
        if(effVao==0){effVao=GL.GenVertexArray();effVbo=GL.GenBuffer();}
        float[] data=vertices.ToArray();effVertexCount=data.Length/6;
        GL.BindVertexArray(effVao);GL.BindBuffer(BufferTarget.ArrayBuffer,effVbo);GL.BufferData(BufferTarget.ArrayBuffer,data.Length*sizeof(float),data,BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,6*sizeof(float),0);GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,6*sizeof(float),3*sizeof(float));GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        void Line(NVector3 a,NVector3 z,float r,float g,float b){vertices.AddRange(new[]{a.X,a.Y,a.Z,r,g,b,z.X,z.Y,z.Z,r,g,b});}
    }

    private void DrawEffGpu()
    {
        if(!EffectsVisible)return;
        if(effVertexCount>0){GL.UseProgram(effShader);var mvp=BuildMvp();GL.UniformMatrix4(effMvp,true,ref mvp);GL.Disable(EnableCap.CullFace);GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(effVao);GL.LineWidth(2f);GL.DrawArrays(PrimitiveType.Lines,0,effVertexCount);GL.LineWidth(1f);GL.Enable(EnableCap.CullFace);}
        DrawSelectedEffTexture();
    }

    private void UploadEffTexture()
    {
        effTextureDirty=false;
        if(effTexture!=0){GL.DeleteTexture(effTexture);effTexture=0;}
        foreach(int texture in effTextures.Values)if(texture!=0)GL.DeleteTexture(texture);effTextures.Clear();
        foreach(List<int> textures in effAnimationTextures.Values)foreach(int texture in textures)if(texture!=0)GL.DeleteTexture(texture);effAnimationTextures.Clear();
        foreach(var pair in effTextureBitmaps)effTextures[pair.Key]=CreateGlTexture(pair.Value);
        foreach(var pair in effAnimationBitmaps)effAnimationTextures[pair.Key]=pair.Value.Select(CreateGlTexture).ToList();
        if(effTextureBitmap!=null)effTexture=CreateGlTexture(effTextureBitmap);
    }

    private void DrawSelectedEffTexture()
    {
        if(effTextureDirty)UploadEffTexture();
        if(effScene==null||effTextures.Count==0)return;
        if(effTextureVao==0){effTextureVao=GL.GenVertexArray();effTextureVbo=GL.GenBuffer();}
        GetCameraBasis(out _,out NVector3 cameraRight,out NVector3 cameraUp);
        var vertices=new List<float>();var draws=new List<(EffEntry Entry,int Texture,int First)>();
        foreach(EffEntry entry in effScene.Entries)
        {
            bool selected=ReferenceEquals(entry,selectedEffEntry);bool animatedResource=effAnimationTextures.TryGetValue(entry.ResourceId,out List<int>? availableAnimation)&&availableAnimation.Count>0;
            if(!ShouldRenderEffBillboard(entry)&&!(animatedResource&&!IsEnvironmentalEff(entry)))continue;int texture;Bitmap bitmap;List<int>? animation=null;
            if(effAnimationTextures.TryGetValue(entry.ResourceId,out List<int>? animated)&&animated.Count>0&&effAnimationBitmaps.TryGetValue(entry.ResourceId,out List<Bitmap>? animatedBitmaps)){animation=animated;int frame=(int)((Environment.TickCount64/EffTickMilliseconds)%animated.Count);texture=animated[frame];bitmap=animatedBitmaps[frame];}
            else if(selected&&effTexture!=0&&effTextureBitmap!=null){texture=effTexture;bitmap=effTextureBitmap;}else if(effTextures.TryGetValue(entry.ResourceId,out texture)&&effTextureBitmaps.TryGetValue(entry.ResourceId,out Bitmap? found))bitmap=found;else continue;
            float aspect=bitmap.Width/(float)Math.Max(1,bitmap.Height);float width=MathF.Abs(entry.Width)*EffWorldScale,height=MathF.Abs(entry.Height)*EffWorldScale;
            if(width<0.01f&&height<0.01f){height=1.2f;width=height*aspect;}else if(width<0.01f)width=height*aspect;else if(height<0.01f)height=width/aspect;
            float maxSize=entry.ResourceId is 0x28 or 0x2E or 0x2C?12f:3f;
            width=Math.Clamp(width,0.08f,maxSize);height=Math.Clamp(height,0.08f,maxSize);NVector3 basePosition=GetEffWorldPosition(entry);
            if(entry.ResourceId==0x2E){width*=1.55f;height*=1.55f;}
            long tick=Environment.TickCount64/EffTickMilliseconds;
            if(entry.ResourceId==0x2E&&animation!=null)
            {
                int interval=Math.Max(1,GetEffControlInterval(entry));long newestSpawn=tick-tick%interval;
                for(long spawn=newestSpawn;spawn>tick-animation.Count;spawn-=interval)
                {
                    int age=(int)(tick-spawn);if(age<0||age>=animation.Count)continue;
                    uint seed=unchecked((uint)(spawn*747796405L+entry.FileOffset*2891336453L));
                    float randomX=(Hash01(seed)-0.5f)*2f*entry.RandomPositionX*EffWorldScale;
                    float randomY=(Hash01(seed+1)-0.5f)*2f*entry.RandomPositionY*EffWorldScale;
                    float randomZ=(Hash01(seed+2)-0.5f)*2f*entry.RandomPositionZ*EffWorldScale;
                    float rise=(entry.SpeedY*age+0.5f*entry.AccelerationY*age*age)*EffWorldScale;
                    float grow=Math.Max(0f,entry.Grow*age*EffWorldScale);float particleWidth=width+grow,particleHeight=height+grow;
                    NVector3 p=basePosition+new NVector3(randomX,randomY+rise,randomZ);NVector3 right=cameraRight*particleWidth*0.5f,up=cameraUp*particleHeight*0.5f;
                    Emit(p,right,up,animation[age]);
                }
            }
            else Emit(basePosition,cameraRight*width*0.5f,cameraUp*height*0.5f,texture);
            void Emit(NVector3 p,NVector3 right,NVector3 up,int particleTexture){NVector3 a=p-right-up,b=p+right-up,c=p+right+up,d=p-right+up;int first=vertices.Count/8;Add(a,0,1);Add(b,1,1);Add(c,1,0);Add(a,0,1);Add(c,1,0);Add(d,0,0);draws.Add((entry,particleTexture,first));}
        }
        if(draws.Count==0)return;float[] q=vertices.ToArray();
        GL.BindVertexArray(effTextureVao);GL.BindBuffer(BufferTarget.ArrayBuffer,effTextureVbo);GL.BufferData(BufferTarget.ArrayBuffer,q.Length*sizeof(float),q,BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,8*sizeof(float),0);GL.EnableVertexAttribArray(0);GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,8*sizeof(float),3*sizeof(float));GL.EnableVertexAttribArray(1);GL.VertexAttribPointer(2,2,VertexAttribPointerType.Float,false,8*sizeof(float),6*sizeof(float));GL.EnableVertexAttribArray(2);
        GL.UseProgram(shaderProgram);var mvp=BuildMvp();GL.UniformMatrix4(uMvp,true,ref mvp);GL.Uniform1(uTexture,0);GL.Uniform1(uUseTexture,1);GL.Uniform1(uUnlit,1);GL.Uniform1(uOpacity,1f);GL.Uniform3(uColor,1f,1f,1f);
        GL.ActiveTexture(TextureUnit.Texture0);GL.Disable(EnableCap.CullFace);GL.Enable(EnableCap.DepthTest);GL.DepthFunc(DepthFunction.Lequal);GL.Enable(EnableCap.Blend);GL.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);GL.DepthMask(false);
        foreach(var draw in draws){EffEntry entry=draw.Entry;bool fire=entry.ResourceId is 0x28 or 0x2E or 0x2C;GL.BlendFunc(BlendingFactor.SrcAlpha,fire?BlendingFactor.One:BlendingFactor.OneMinusSrcAlpha);float alpha=entry.ColorA==0?1f:entry.ColorA/255f;GL.Uniform1(uOpacity,alpha);GL.Uniform4(uTextureTint,entry.ColorR/255f,entry.ColorG/255f,entry.ColorB/255f,1f);GL.BindTexture(TextureTarget.Texture2D,draw.Texture);GL.DrawArrays(PrimitiveType.Triangles,draw.First,6);}
        GL.Uniform4(uTextureTint,1f,1f,1f,1f);GL.Uniform1(uUseTexture,0);GL.Uniform1(uOpacity,1f);GL.DepthMask(true);GL.Disable(EnableCap.Blend);GL.Enable(EnableCap.DepthTest);GL.Enable(EnableCap.CullFace);GL.BindTexture(TextureTarget.Texture2D,0);
        void Add(NVector3 point,float u,float v){vertices.Add(point.X);vertices.Add(point.Y);vertices.Add(point.Z);vertices.Add(0);vertices.Add(0);vertices.Add(1);vertices.Add(u);vertices.Add(v);}
    }

    private static int GetEffControlInterval(EffEntry entry)=>entry.RawData.Length>0x10C?entry.RawData[0x10C]:1;
    private static float Hash01(uint value){value^=value>>16;value*=0x7FEB352Du;value^=value>>15;value*=0x846CA68Bu;value^=value>>16;return(value&0x00FFFFFF)/16777215f;}

    private static bool ShouldRenderEffBillboard(EffEntry entry)
    {
        // Known particle sprites in the room EFFs. Fog/screen/light controllers need
        // dedicated volume or post-process renderers and must not become giant quads.
        // Current validation stage deliberately renders only flames.
        if(entry.ResourceId is 0x28 or 0x2E or 0x2C)return true;
        if(entry.ResourceId is 0xC3 or 0xC4 or 0xCD or 0x51 or 0x52 or 0x59)return false;
        if(entry.ResourceId is 0x1E or 0x1F or 0x79 or 0x08 or 0x13 or 0x72 or 0xE9 or 0xFE or 0xF7)return false;
        if(entry.EspId is 0x08 or 0x0B or 0x0C or 0x11 or 0x14 or 0x45 or 0x46 or 0x4A or 0x4B)return false;
        return entry.ResourceId!=0;
    }

    private static bool IsEnvironmentalEff(EffEntry entry)=>
        entry.ResourceId is 0x1E or 0x1F or 0x79 or 0x08 or 0x13 or 0x72 or 0xE9 or 0xFE or 0xF7 ||
        entry.EspId is 0x08 or 0x0C or 0x11 or 0x14 or 0x45 or 0x46 or 0x4A or 0x4B;

    private static NVector3 GetEffWorldPosition(EffEntry entry)
    {
        NVector3 result=entry.Group.Position+new NVector3(entry.PositionX,entry.PositionY,entry.PositionZ);
        EffEntry current=entry;var visited=new HashSet<int>{entry.Index};
        for(int depth=0;depth<16;depth++)
        {
            int encodedParent=current.Parent;if(encodedParent is 0x00 or 0xFE or 0xFF)break;
            int parent=encodedParent-1;if(parent<0||parent>=entry.Group.Entries.Count||!visited.Add(parent))break;
            current=entry.Group.Entries[parent];result+=new NVector3(current.PositionX,current.PositionY,current.PositionZ);
        }
        return result*EffWorldScale;
    }

    private EffEntry? PickEffEntry(Point mouse)
    {
        if(effScene==null||!EffectsVisible)return null;EffEntry? hit=null;float best=18f;
        foreach(EffEntry entry in effScene.Entries){if(!TryProjectWorldToScreen(GetEffWorldPosition(entry),out PointF p))continue;float dx=p.X-mouse.X,dy=p.Y-mouse.Y,d=MathF.Sqrt(dx*dx+dy*dy);if(d<best){best=d;hit=entry;}}
        return hit;
    }

    private void DisposeEffGpu(){effAnimationTimer?.Stop();effAnimationTimer?.Dispose();effAnimationTimer=null;if(effVbo!=0)GL.DeleteBuffer(effVbo);if(effVao!=0)GL.DeleteVertexArray(effVao);if(effShader!=0)GL.DeleteProgram(effShader);if(effTexture!=0)GL.DeleteTexture(effTexture);foreach(int texture in effTextures.Values)if(texture!=0)GL.DeleteTexture(texture);effTextures.Clear();foreach(List<int> textures in effAnimationTextures.Values)foreach(int texture in textures)if(texture!=0)GL.DeleteTexture(texture);effAnimationTextures.Clear();foreach(Bitmap bitmap in effTextureBitmaps.Values)bitmap.Dispose();effTextureBitmaps.Clear();foreach(List<Bitmap> frames in effAnimationBitmaps.Values)foreach(Bitmap bitmap in frames)bitmap.Dispose();effAnimationBitmaps.Clear();if(effTextureVbo!=0)GL.DeleteBuffer(effTextureVbo);if(effTextureVao!=0)GL.DeleteVertexArray(effTextureVao);if(effTextureShader!=0)GL.DeleteProgram(effTextureShader);effTextureBitmap?.Dispose();effTextureBitmap=null;effVbo=effVao=effShader=effTexture=effTextureVbo=effTextureVao=effTextureShader=0;}
}
