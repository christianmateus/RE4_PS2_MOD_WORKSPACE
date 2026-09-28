using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using RE4_PS2_MOD_WORKSPACE.Core.Effects;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using NVector3 = System.Numerics.Vector3;
using NQuaternion = System.Numerics.Quaternion;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport : GLControl
{
    private void DrawStatusMessageGpu(string message)
    {
        LabelTexture label = GetOrCreateLabelTexture(message, false);
        float left = (ClientSize.Width - label.Width) * 0.5f;
        float top = (ClientSize.Height - label.Height) * 0.5f;
        float x0 = left / ClientSize.Width * 2f - 1f;
        float x1 = (left + label.Width) / ClientSize.Width * 2f - 1f;
        float y0 = 1f - top / ClientSize.Height * 2f;
        float y1 = 1f - (top + label.Height) / ClientSize.Height * 2f;
        float[] quad = { x0,y0,0f,0f, x0,y1,0f,1f, x1,y1,1f,1f, x0,y0,0f,0f, x1,y1,1f,1f, x1,y0,1f,0f };

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.UseProgram(labelShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(labelTextureUniform, 0);
        GL.BindVertexArray(labelVao);
        GL.BindTexture(TextureTarget.Texture2D, label.TextureId);
        GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }
    private void DrawAevLabelsGpu()
    {
        if (aevScene == null || labelShaderProgram == 0) return;

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.UseProgram(labelShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(labelTextureUniform, 0);
        GL.BindVertexArray(labelVao);

        foreach (AevEntry entry in aevScene.Entries)
        {
            if (aevTypeFilter.HasValue && entry.Type != aevTypeFilter.Value) continue;
            if (!entry.IsSquare && !entry.IsCircle) continue;

            GetAevYRange(entry, out _, out float y1);
            System.Numerics.Vector2 center = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);
            NVector3 world = new(center.X, y1 + Math.Max(0.05f, (scene?.Radius ?? 1f) * 0.001f), center.Y);
            if (!TryProjectWorldToScreen(world, out PointF screen)) continue;

            bool selected = entry.FileOrder == selectedAevFileOrder;
            string text = $"#{entry.Index:X2} {AevNames.EventTypeName(entry.Type)}";
            LabelTexture label = GetOrCreateLabelTexture(text, selected);

            float leftPx = screen.X - label.Width * 0.5f;
            float topPx = screen.Y - label.Height - 8f;
            if (leftPx + label.Width < 0 || topPx + label.Height < 0 ||
                leftPx > ClientSize.Width || topPx > ClientSize.Height)
                continue;

            float x0 = leftPx / ClientSize.Width * 2f - 1f;
            float x1 = (leftPx + label.Width) / ClientSize.Width * 2f - 1f;
            float y0 = 1f - topPx / ClientSize.Height * 2f;
            float y1Ndc = 1f - (topPx + label.Height) / ClientSize.Height * 2f;

            float[] quad =
            {
                x0, y0,    0f, 0f,
                x0, y1Ndc, 0f, 1f,
                x1, y1Ndc, 1f, 1f,
                x0, y0,    0f, 0f,
                x1, y1Ndc, 1f, 1f,
                x1, y0,    1f, 0f
            };

            GL.BindTexture(TextureTarget.Texture2D, label.TextureId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StreamDraw);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void DrawEnemyLabelsGpu()
    {
        if (eslScene == null || labelShaderProgram == 0) return;

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.UseProgram(labelShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(labelTextureUniform, 0);
        GL.BindVertexArray(labelVao);

        foreach (EslEnemyEntry entry in eslScene.Entries.Where(EnemyIsVisible))
        {
            NVector3 world = EslToWorld(entry) + new NVector3(0f, Math.Max(0.08f, (scene?.Radius ?? 1f) * 0.0012f), 0f);
            if (!TryProjectWorldToScreen(world, out PointF screen)) continue;

            bool selected = entry.Index == selectedEnemyIndex;
            string text = $"#{entry.Index:D3} {entry.FriendlyName}";
            LabelTexture label = GetOrCreateLabelTexture(text, selected);
            float leftPx = screen.X - label.Width * 0.5f;
            float topPx = screen.Y - label.Height - 10f;
            if (leftPx + label.Width < 0 || topPx + label.Height < 0 || leftPx > ClientSize.Width || topPx > ClientSize.Height) continue;

            float x0 = leftPx / ClientSize.Width * 2f - 1f;
            float x1 = (leftPx + label.Width) / ClientSize.Width * 2f - 1f;
            float y0 = 1f - topPx / ClientSize.Height * 2f;
            float y1 = 1f - (topPx + label.Height) / ClientSize.Height * 2f;
            float[] quad = { x0,y0,0f,0f, x0,y1,0f,1f, x1,y1,1f,1f, x0,y0,0f,0f, x1,y1,1f,1f, x1,y0,1f,0f };
            GL.BindTexture(TextureTarget.Texture2D, label.TextureId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StreamDraw);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0); GL.BindBuffer(BufferTarget.ArrayBuffer, 0); GL.BindVertexArray(0); GL.UseProgram(0);
        GL.Disable(EnableCap.Blend); GL.Enable(EnableCap.DepthTest); GL.Enable(EnableCap.CullFace);
    }

    private EslEnemyEntry? PickEnemyLabel(Point mouse)
    {
        if(!ShowEnemyLabels||eslScene==null)return null;EslEnemyEntry? best=null;float bestDepth=float.PositiveInfinity;
        foreach(EslEnemyEntry entry in eslScene.Entries.Where(EnemyIsVisible))
        {
            NVector3 world=EslToWorld(entry)+new NVector3(0f,Math.Max(.08f,(scene?.Radius??1f)*.0012f),0f);
            if(!TryProjectWorldToScreen(world,out PointF screen))continue;
            bool selected=entry.Index==selectedEnemyIndex;string text=$"#{entry.Index:D3} {entry.FriendlyName}";LabelTexture label=GetOrCreateLabelTexture(text,selected);
            var bounds=new RectangleF(screen.X-label.Width*.5f,screen.Y-label.Height-10f,label.Width,label.Height);
            if(bounds.Contains(mouse)){float depth=NVector3.DistanceSquared(cameraPosition,world);if(depth<bestDepth){bestDepth=depth;best=entry;}}
        }
        return best;
    }

    private LabelTexture GetOrCreateLabelTexture(string text, bool selected)
    {
        string key = (selected ? "S|" : "N|") + text;
        if (labelTextures.TryGetValue(key, out LabelTexture? existing)) return existing;

        using Font font = new Font("Segoe UI Semibold", 8.5f);
        Size textSize = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        int width = Math.Max(1, textSize.Width + 10);
        int height = Math.Max(1, textSize.Height + 6);

        using Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(selected ? Color.FromArgb(220, 74, 48, 8) : Color.FromArgb(195, 12, 17, 23));
            TextRenderer.DrawText(g, text, font, new Rectangle(5, 3, width - 10, height - 6),
                selected ? Color.FromArgb(255, 232, 176) : Color.FromArgb(238, 242, 248),
                TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.Top);
        }

        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        var created = new LabelTexture(texture, width, height);
        labelTextures[key] = created;
        return created;
    }

    private sealed record LabelTexture(int TextureId, int Width, int Height);

    private void InitializeGl()
    {
        if (glReady) return;

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.Multisample);

        const string vertexShader = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUv;
layout(location = 3) in float aAlpha;
layout(location = 4) in vec3 aVertexColor;
uniform mat4 uMvp;
uniform mat4 uModel;
uniform mat4 uNormalMatrix;
uniform float uNormalSign;
out vec3 vNormal;
out vec2 vUv;
out vec3 vWorldPos;
out float vAlpha;
out vec3 vVertexColor;
void main()
{
    vec4 world = vec4(aPos, 1.0) * uModel;
    gl_Position = world * uMvp;
    vNormal = normalize((aNormal * mat3(uNormalMatrix)) * uNormalSign);
    vUv = aUv;
    vWorldPos = world.xyz;
    vAlpha = aAlpha;
    vVertexColor = aVertexColor;
}";

        const string fragmentShader = @"#version 330 core
in vec3 vNormal;
in vec2 vUv;
in vec3 vWorldPos;
in float vAlpha;
in vec3 vVertexColor;
uniform vec3 uColor;
uniform int uUnlit;
uniform int uUseTexture;
uniform sampler2D uTexture;
uniform vec4 uTextureTint;
uniform float uOpacity;
uniform int uLitEnabled;
uniform int uLitCount;
uniform vec4 uLitAmbient;
uniform vec4 uLitPosRange[32];
uniform vec4 uLitColorIntensity[32];
uniform vec4 uLitDirectionType[32];
uniform vec4 uLitAttnA[32];
uniform vec4 uLitAttnK[32];
uniform vec4 uLitBehavior0[32];
uniform vec4 uLitBehavior1[32];
uniform vec4 uLitMeta[32];
uniform float uLitTime;
uniform int uFogEnabled;
uniform int uFogType;
uniform vec3 uFogColor;
uniform vec2 uFogRange;
uniform vec3 uCameraPosition;
uniform vec3 uCameraForward;
out vec4 FragColor;

vec3 litRotateX(vec3 v,float a){float c=cos(a),s=sin(a);return vec3(v.x,v.y*c-v.z*s,v.y*s+v.z*c);}
vec3 litRotateY(vec3 v,float a){float c=cos(a),s=sin(a);return vec3(v.x*c+v.z*s,v.y,-v.x*s+v.z*c);}
vec3 litRotateZ(vec3 v,float a){float c=cos(a),s=sin(a);return vec3(v.x*c-v.y*s,v.x*s+v.y*c,v.z);}
float litRangeFade(float distanceToLight,float radius,float width)
{
    if(radius<=0.00001)return 1.0;
    width=clamp(abs(width),0.00001,radius);
    if(distanceToLight<=radius-width)return 1.0;
    return clamp((radius-distanceToLight)/width,0.0,1.0);
}
void main()
{
    float shade = 1.0;
    if (uUnlit == 0)
    {
        vec3 n = normalize(vNormal);
        if (uLitEnabled == 0)
        {
            vec3 l = normalize(vec3(-0.35, 0.75, -0.55));
            float diffuse = abs(dot(n, l));
            shade = 0.42 + diffuse * 0.58;
        }
    }

    vec4 baseColor = vec4(uColor, 1.0);
    if (uUseTexture != 0)
    {
        baseColor = texture(uTexture, vUv) * uTextureTint;
        // PS2 scenario textures frequently use transparent black texels for
        // foliage/fences/cutout geometry. Do not force those texels opaque.
        if (baseColor.a <= 0.06) discard;
    }
    vec3 rgb = baseColor.rgb * vVertexColor * shade;
    if (uUnlit == 0 && uLitEnabled != 0)
    {
        vec3 n = normalize(vNormal);
        vec3 illumination = uLitAmbient.rgb;
        for (int i=0; i<32; i++)
        {
            if (i >= uLitCount) break;
            vec3 color = uLitColorIntensity[i].rgb;
            float intensity = max(0.0, uLitColorIntensity[i].a);
            int kind = int(uLitDirectionType[i].w + 0.5);
            int behavior = int(uLitMeta[i].x + 0.5);
            vec3 direction = normalize(uLitDirectionType[i].xyz);
            float frames = uLitTime * 30.0;
            if(behavior==1)
            {
                float noise=fract(sin(floor(frames)+float(i)*91.73)*43758.5453)*2.0-1.0;
                color=clamp(color+vec3(noise*uLitMeta[i].y),0.0,2.0);
            }
            else if(behavior==2)intensity*=max(0.0,uLitBehavior0[i].x+uLitBehavior0[i].y*sin(uLitBehavior0[i].w+uLitBehavior0[i].z*6.2831853*uLitTime));
            else if(behavior==3){vec3 rotation=uLitBehavior0[i].xyz*frames;direction=normalize(litRotateZ(litRotateY(litRotateX(direction,rotation.x),rotation.y),rotation.z));}
            else if(behavior==6)intensity*=clamp(uLitBehavior0[i].x+uLitBehavior0[i].y*frames,0.0,1.0);
            else if(behavior==7){vec3 angle=uLitBehavior0[i].xyz;vec3 speed=vec3(uLitBehavior0[i].w,uLitBehavior1[i].x,uLitBehavior1[i].y);angle+=speed*frames;direction=normalize(vec3(cos(angle.x)*sin(angle.y),sin(angle.x),cos(angle.x)*cos(angle.y)));}
            else if(behavior==16){float wait=max(0.0,uLitMeta[i].z);intensity*=pow(0.3,max(0.0,frames-wait));}

            vec3 delta=uLitPosRange[i].xyz-vWorldPos;
            float distanceToLight=length(delta);
            float radius=uLitPosRange[i].w;
            vec3 toLight=distanceToLight>0.001?delta/distanceToLight:n;
            float diffuse=max(dot(n,toLight),0.0);
            float attenuation=1.0;
            if(kind==0)attenuation=litRangeFade(distanceToLight,radius,abs(uLitAttnA[i].w));
            else if(kind==1)attenuation=radius<=0.00001?1.0:max(0.0,1.0-distanceToLight/radius);
            else if(kind==2){attenuation=radius<=0.00001?1.0:1.0/(1.0+9.0*(distanceToLight/radius)*(distanceToLight/radius));attenuation*=litRangeFade(distanceToLight,radius,abs(uLitAttnA[i].w));}
            else if(kind==3||kind==6)
            {
                attenuation=litRangeFade(distanceToLight,radius,uLitAttnA[i].y);
                float cutoff=cos(radians(clamp(abs(uLitAttnA[i].x),0.1,179.0)));
                float cone=dot(-toLight,direction);
                attenuation*=smoothstep(cutoff,min(1.0,cutoff+0.08),cone);
                if(kind==6&&radius>0.00001)attenuation*=1.0/(1.0+9.0*(distanceToLight/radius)*(distanceToLight/radius));
            }
            else if(kind==4)
            {
                float cone=dot(-toLight,direction);
                float angular=max(0.0,uLitAttnA[i].x+uLitAttnA[i].y*cone+uLitAttnA[i].z*cone*cone);
                float gameDistance=distanceToLight*100.0;
                float denominator=uLitAttnK[i].x+uLitAttnK[i].y*gameDistance+uLitAttnK[i].z*gameDistance*gameDistance;
                attenuation=angular/max(0.0001,denominator);
            }
            else if(kind==5){diffuse=max(dot(n,-direction),0.0);attenuation=litRangeFade(distanceToLight,radius,uLitAttnA[i].y);}
            else if(kind==7){float local=litRangeFade(distanceToLight,radius,abs(uLitAttnA[i].w));illumination=max(illumination,color*intensity*local);continue;}
            illumination+=color*intensity*diffuse*max(0.0,attenuation);
        }
        rgb = baseColor.rgb * vVertexColor * clamp(illumination, vec3(0.0), vec3(4.0));
    }
    if (uFogEnabled != 0)
    {
        // GX fog is based on camera-space Z, not radial distance. The low three
        // bits select the hardware curve for perspective and orthographic modes.
        float cameraDepth = max(0.0,dot(vWorldPos-uCameraPosition,normalize(uCameraForward)));
        float fogFactor = clamp((cameraDepth-uFogRange.x)/max(0.001,uFogRange.y-uFogRange.x),0.0,1.0);
        int fogFunction = uFogType & 7;
        if (fogFunction == 4) fogFactor = 1.0-exp2(-8.0*fogFactor);
        else if (fogFunction == 5) fogFactor = 1.0-exp2(-8.0*fogFactor*fogFactor);
        else if (fogFunction == 6) fogFactor = exp2(-8.0*(1.0-fogFactor));
        else if (fogFunction == 7) fogFactor = exp2(-8.0*(1.0-fogFactor)*(1.0-fogFactor));
        rgb = mix(rgb,uFogColor,clamp(fogFactor,0.0,1.0));
    }
    FragColor = vec4(rgb, baseColor.a * uOpacity * vAlpha);
}";

        int vs = CompileShader(ShaderType.VertexShader, vertexShader);
        int fs = CompileShader(ShaderType.FragmentShader, fragmentShader);
        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vs);
        GL.AttachShader(shaderProgram, fs);
        GL.LinkProgram(shaderProgram);
        GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0) throw new InvalidOperationException("OpenGL shader link failed: " + GL.GetProgramInfoLog(shaderProgram));
        GL.DetachShader(shaderProgram, vs);
        GL.DetachShader(shaderProgram, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        // VAOs used for helpers do not provide per-vertex alpha. Attribute 3 is
        // disabled in those VAOs, so keep its generic value fully opaque.
        GL.VertexAttrib1(3, 1f);
        GL.VertexAttrib3(4, 1f, 1f, 1f);

        uMvp = GL.GetUniformLocation(shaderProgram, "uMvp");
        uModel = GL.GetUniformLocation(shaderProgram, "uModel");
        uNormalMatrix = GL.GetUniformLocation(shaderProgram, "uNormalMatrix");
        uNormalSign = GL.GetUniformLocation(shaderProgram, "uNormalSign");
        uColor = GL.GetUniformLocation(shaderProgram, "uColor");
        uUnlit = GL.GetUniformLocation(shaderProgram, "uUnlit");
        uTexture = GL.GetUniformLocation(shaderProgram, "uTexture");
        uUseTexture = GL.GetUniformLocation(shaderProgram, "uUseTexture");
        uTextureTint = GL.GetUniformLocation(shaderProgram, "uTextureTint");
        uOpacity = GL.GetUniformLocation(shaderProgram, "uOpacity");
        InitializeLitShaderBindings();

        const string labelVertexShader = @"#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUv;
out vec2 vUv;
void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
    vUv = aUv;
}";
        const string labelFragmentShader = @"#version 330 core
in vec2 vUv;
uniform sampler2D uLabelTexture;
out vec4 FragColor;
void main()
{
    FragColor = texture(uLabelTexture, vUv);
}";

        int labelVs = CompileShader(ShaderType.VertexShader, labelVertexShader);
        int labelFs = CompileShader(ShaderType.FragmentShader, labelFragmentShader);
        labelShaderProgram = GL.CreateProgram();
        GL.AttachShader(labelShaderProgram, labelVs);
        GL.AttachShader(labelShaderProgram, labelFs);
        GL.LinkProgram(labelShaderProgram);
        GL.GetProgram(labelShaderProgram, GetProgramParameterName.LinkStatus, out int labelLinked);
        if (labelLinked == 0) throw new InvalidOperationException("OpenGL label shader link failed: " + GL.GetProgramInfoLog(labelShaderProgram));
        GL.DetachShader(labelShaderProgram, labelVs);
        GL.DetachShader(labelShaderProgram, labelFs);
        GL.DeleteShader(labelVs);
        GL.DeleteShader(labelFs);
        labelTextureUniform = GL.GetUniformLocation(labelShaderProgram, "uLabelTexture");

        labelVao = GL.GenVertexArray();
        labelVbo = GL.GenBuffer();
        GL.BindVertexArray(labelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 6 * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        meshVao = GL.GenVertexArray();
        meshVbo = GL.GenBuffer();
        gridVao = GL.GenVertexArray();
        gridVbo = GL.GenBuffer();
        aevVao = GL.GenVertexArray();
        aevVbo = GL.GenBuffer();
        aevSelectedVao = GL.GenVertexArray();
        aevSelectedVbo = GL.GenBuffer();
        aevFaceVao = GL.GenVertexArray();
        aevFaceVbo = GL.GenBuffer();
        aevSelectedFaceVao = GL.GenVertexArray();
        aevSelectedFaceVbo = GL.GenBuffer();
        aevHandleVao = GL.GenVertexArray();
        aevHandleVbo = GL.GenBuffer();
        for (int i = 0; i < 3; i++) { aevGizmoVaos[i] = GL.GenVertexArray(); aevGizmoVbos[i] = GL.GenBuffer(); }
        enemyVao=GL.GenVertexArray(); enemyVbo=GL.GenBuffer(); selectedEnemyVao=GL.GenVertexArray(); selectedEnemyVbo=GL.GenBuffer();
        for(int i=0;i<3;i++){enemyGizmoVaos[i]=GL.GenVertexArray();enemyGizmoVbos[i]=GL.GenBuffer();}
        enemyModelVao=GL.GenVertexArray(); enemyModelVbo=GL.GenBuffer(); selectedEnemyModelVao=GL.GenVertexArray(); selectedEnemyModelVbo=GL.GenBuffer();
        glReady = true;
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"OpenGL {type} compile failed: {log}");
        }
        return shader;
    }

    private void UploadScene()
    {
        gpuDirty = false;
        // Keep one scenario rendering path regardless of the selected editor tab.
        // The old combined VBO recalculated smooth normals across every world-space
        // triangle, while the SMD tab used per-entry local normals and a normal matrix.
        // Switching tabs therefore changed the apparent lighting of the same model.
        if(scene!=null)
        {
            ReleaseSmdEntryGpu();
            foreach(ScenarioEntry entry in scene.Entries)UploadSmdEntryGpu(entry);float[] grid=BuildGridData(scene);gridVertexCount=grid.Length/6;GL.BindVertexArray(gridVao);GL.BindBuffer(BufferTarget.ArrayBuffer,gridVbo);GL.BufferData(BufferTarget.ArrayBuffer,grid.Length*sizeof(float),grid,BufferUsageHint.StaticDraw);GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,6*sizeof(float),0);GL.EnableVertexAttribArray(0);GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,6*sizeof(float),3*sizeof(float));GL.EnableVertexAttribArray(1);GL.DisableVertexAttribArray(2);GL.VertexAttrib2(2,0f,0f);GL.BindVertexArray(0);
            meshVertexCount=0;meshBatches.Clear();return;
        }
        ReleaseSmdEntryGpu();
        meshVertexCount = 0;
        gridVertexCount = 0;
        meshBatches.Clear();
        if (scene == null) return;

        int triCount = scene.Triangles.Count;

        // Keep triangles grouped by diffuse texture. This lets one VBO serve the
        // entire SMD while OpenGL changes texture only between material batches.
        ScenarioTriangle[] ordered = scene.Triangles
            .OrderBy(x => x.TextureIndex)
            .ThenBy(UsesVertexAlpha)
            .ToArray();

        float[] meshData = new float[triCount * 36]; // 3 vertices * (position3 + normal3 + uv2 + alpha + RGB)

        var normalSums = new Dictionary<NVector3, NVector3>(Math.Min(triCount * 2, 1_000_000));
        foreach (ScenarioTriangle tri in ordered)
        {
            NVector3 n = NVector3.Cross(tri.B - tri.A, tri.C - tri.A);
            float lenSq = n.LengthSquared();
            if (lenSq < 0.000001f || !float.IsFinite(lenSq)) continue;
            AddNormal(normalSums, tri.A, n);
            AddNormal(normalSums, tri.B, n);
            AddNormal(normalSums, tri.C, n);
        }

        int o = 0;
        int currentTexture = int.MinValue;
        bool currentVertexAlpha = false;
        int batchFirst = 0;
        int batchVertices = 0;

        foreach (ScenarioTriangle tri in ordered)
        {
            bool usesVertexAlpha = UsesVertexAlpha(tri);
            if (tri.TextureIndex != currentTexture || usesVertexAlpha != currentVertexAlpha)
            {
                if (batchVertices > 0) meshBatches.Add(new ScenarioDrawBatch(currentTexture, batchFirst, batchVertices, currentVertexAlpha));
                currentTexture = tri.TextureIndex;
                currentVertexAlpha = usesVertexAlpha;
                batchFirst = o / 12;
                batchVertices = 0;
            }

            WriteTexturedVertex(meshData, ref o, tri.A, GetSmoothNormal(normalSums, tri.A), tri.UvA, tri.AlphaA,tri.ColorA);
            WriteTexturedVertex(meshData, ref o, tri.B, GetSmoothNormal(normalSums, tri.B), tri.UvB, tri.AlphaB,tri.ColorB);
            WriteTexturedVertex(meshData, ref o, tri.C, GetSmoothNormal(normalSums, tri.C), tri.UvC, tri.AlphaC,tri.ColorC);
            batchVertices += 3;
        }
        if (batchVertices > 0) meshBatches.Add(new ScenarioDrawBatch(currentTexture, batchFirst, batchVertices, currentVertexAlpha));
        meshVertexCount = o / 12;

        GL.BindVertexArray(meshVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, meshVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, o * sizeof(float), meshData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 12 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 12 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 12 * sizeof(float), 8 * sizeof(float));
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(4,3,VertexAttribPointerType.Float,false,12*sizeof(float),9*sizeof(float));
        GL.EnableVertexAttribArray(4);

        float[] gridData = BuildGridData(scene);
        gridVertexCount = gridData.Length / 6;
        GL.BindVertexArray(gridVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, gridVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, gridData.Length * sizeof(float), gridData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.DisableVertexAttribArray(2);
        GL.VertexAttrib2(2, 0f, 0f);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    private void UploadTextures()
    {
        texturesDirty = false;
        ReleaseTextures();
        if (string.IsNullOrWhiteSpace(textureSourcePath) || !File.Exists(textureSourcePath)) return;

        var service = new TextureWorkspaceService();
        IReadOnlyList<TextureInfo> catalog;
        try { catalog = service.ReadCatalog(textureSourcePath); }
        catch { return; }

        foreach (TextureInfo info in catalog)
        {
            try
            {
                using Bitmap bitmap = service.Decode(textureSourcePath, info.Index);
                bool hasTransparency = BitmapRequiresScenarioBlending(bitmap);
                int texture = CreateGlTexture(bitmap);
                glTextures[info.Index] = texture;
                glTextureHasTransparency[info.Index] = hasTransparency;
            }
            catch
            {
                // One unsupported/broken texture must not prevent the rest of the SMD.
            }
        }
    }

    private void UploadEnemyTextures()
    {
        enemyTexturesDirty = false;
        ReleaseEnemyTextures();
        if (enemyModels.Count == 0) return;

        var reader = new TplReader();
        var decoder = new TextureDecoder();

        foreach (var modelPair in enemyModels)
        {
            byte enemyType = modelPair.Key;
            EnemyModelScene model = modelPair.Value;

            foreach (EnemyTexturePackage package in model.TexturePackages.Values)
            {
                try
                {
                    using var stream = new MemoryStream(package.Data, writable: false);
                    using var br = new BinaryReader(stream);
                    if (stream.Length < 8) continue;
                    stream.Position = 4;
                    uint rawCount = br.ReadUInt32();
                    int count = rawCount > 128 ? 128 : (int)rawCount;

                    for (int textureIndex = 0; textureIndex < count; textureIndex++)
                    {
                        try
                        {
                            stream.Position = 0;
                            var tpl = reader.ReadTexture(br, textureIndex);
                            stream.Position = 0;
                            using Bitmap bitmap = decoder.Decode(tpl, br);
                            // Character BIN UVs expect horizontal PS2-swizzled
                            // textures in the same orientation produced by the
                            // editor's Rotate 90° + Flip Y operations. Apply that
                            // orientation only to the GPU preview; never mutate the
                            // TPL or affect PNG export/import.
                            if (tpl.interlace is 2 or 3 && tpl.width > tpl.height)
                            {
                                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                            }
                            int texture = CreateGlTexture(bitmap);
                            var key = new EnemyTextureKey(enemyType, package.DatEntryIndex, textureIndex);
                            glEnemyTextures[key] = texture;
                            glEnemyTextureHasTransparency[key] = BitmapHasTransparency(bitmap);
                        }
                        catch
                        {
                            // One unsupported texture should not prevent the remaining enemy textures.
                        }
                    }
                }
                catch
                {
                    // Keep the model usable as untextured if a package is malformed.
                }
            }
        }
    }

    private void ReleaseEnemyTextures()
    {
        foreach (int texture in glEnemyTextures.Values)
            if (texture != 0) GL.DeleteTexture(texture);
        glEnemyTextures.Clear();
        glEnemyTextureHasTransparency.Clear();
    }

    private static bool BitmapHasTransparency(Bitmap source)
    {
        using var bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap)) g.DrawImageUnscaled(source, 0, 0);

        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] row = new byte[stride];

            for (int y = 0; y < bitmap.Height; y++)
            {
                IntPtr rowPtr = IntPtr.Add(data.Scan0, y * data.Stride);
                Marshal.Copy(rowPtr, row, 0, stride);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    byte alpha = row[x * 4 + 3];
                    if (alpha < 250) return true;
                }
            }
            return false;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static bool BitmapRequiresScenarioBlending(Bitmap source)
    {
        using Bitmap bitmap=source.PixelFormat==System.Drawing.Imaging.PixelFormat.Format32bppArgb?new Bitmap(source):source.Clone(new Rectangle(0,0,source.Width,source.Height),System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Rectangle rect=new(0,0,bitmap.Width,bitmap.Height);BitmapData data=bitmap.LockBits(rect,ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int stride=Math.Abs(data.Stride);byte[] row=new byte[stride];long partial=0,opaque=0,total=(long)bitmap.Width*bitmap.Height;
            for(int y=0;y<bitmap.Height;y++){IntPtr rowPtr=IntPtr.Add(data.Scan0,y*data.Stride);Marshal.Copy(rowPtr,row,0,stride);for(int x=0;x<bitmap.Width;x++){byte alpha=row[x*4+3];if(alpha>=245)opaque++;else if(alpha>15)partial++;}}
            // Cutout textures (foliage, fences, decals, or an opaque texture with a
            // few antialiased pixels) belong in the depth-writing pass. A material is
            // blended only when intermediate alpha is a meaningful part of an image
            // that is not predominantly opaque, as with fake shadows and glass.
            return partial>total*.08&&opaque<total*.50;
        }
        finally{bitmap.UnlockBits(data);}
    }

    private static int CreateGlTexture(Bitmap source)
    {
        // Convert once to a known BGRA byte layout and upload directly.
        // OpenGL receives BGRA bytes and stores them internally as RGBA8.
        using var bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap)) g.DrawImageUnscaled(source, 0, 0);

        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                bitmap.Width, bitmap.Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private void ReleaseTextures()
    {
        foreach (int texture in glTextures.Values)
            if (texture != 0) GL.DeleteTexture(texture);
        glTextures.Clear();
        glTextureHasTransparency.Clear();
    }

}
