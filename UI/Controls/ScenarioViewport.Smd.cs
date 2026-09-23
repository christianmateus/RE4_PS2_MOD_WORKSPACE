using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public enum SmdGizmoMode { Move, Rotate, Scale }
public enum SmdTransformSpace { World, Local }

public sealed partial class ScenarioViewport
{
    private int selectedSmdEntry = -1, smdDragAxis;
    private readonly HashSet<int> selectedSmdEntries = new();
    private ScenarioEntry? draggingSmd;
    private SmdTransformState smdDragStart;
    private Dictionary<ScenarioEntry,SmdTransformState>? smdGroupDragStart;
    private NVector3 smdGroupPivot;
    private NVector3 smdDragWorldAxis;
    private Point smdDragMouse;
    private System.Numerics.Vector2 smdDragScreenAxis;
    private float smdDragUnitsPerPixel;
    private bool smdOverlayDirty = true;
    private int smdSelectedVao, smdSelectedVbo, smdSelectedCount;
    private readonly int[] smdGizmoVaos = new int[4], smdGizmoVbos = new int[4], smdGizmoCounts = new int[4];
    private int smdDisplayedGizmoEntry=-1;
    private NVector3 smdDisplayedGizmoOrigin;
    private NVector3[] smdDisplayedGizmoAxes={NVector3.UnitX,NVector3.UnitY,NVector3.UnitZ};
    private float smdDisplayedGizmoLength;
    private readonly Stack<Action> smdUndo = new();
    private readonly HashSet<int> selectedSmdFaces=new();
    private Dictionary<int,NVector3>? smdFaceDragStart;
    private bool smdFaceDragging;
    private int smdFaceVao,smdFaceVbo,smdFaceCount;
    private bool smdFaceBoxSelecting,smdFaceBoxAdditive;
    private Point smdFaceBoxStart,smdFaceBoxEnd;
    private int smdFaceBoxVao,smdFaceBoxVbo,smdFaceBoxCount;
    private readonly Dictionary<int, int> smdEntryTriangleStarts = new();
    private readonly HashSet<int> smdGpuDirtyEntries = new();
    private readonly Dictionary<int, SmdEntryGpu> smdEntryGpu = new();
    private bool smdOverlayCameraKnown;
    private NVector3 smdOverlayCameraPosition;
    private float smdOverlayYaw,smdOverlayPitch;
    private Size smdOverlayClientSize;

    private bool smdEditingEnabled;
    public bool SmdEditingEnabled { get => smdEditingEnabled; set { if(smdEditingEnabled==value)return;smdEditingEnabled=value;gpuDirty=true;Invalidate(); } }
    public SmdGizmoMode SmdTransformMode { get; set; }
    public SmdTransformSpace SmdTransformSpace { get; set; }
    public bool SmdSnapEnabled { get; set; }
    public float SmdTransformSpeed { get; set; } = 4f;
    public bool SmdFaceEditMode { get; set; }
    public bool IsSmdDragging => draggingSmd != null;
    public void SetSmdTransformSpace(SmdTransformSpace value){SmdTransformSpace=value;smdOverlayDirty=true;Invalidate();}
    public event Action<IReadOnlyList<ScenarioEntry>>? SmdSelectionChanged;
    public event Action<ScenarioEntry>? SmdEntryEdited;
    public event Action<SmdGizmoMode>? SmdTransformModeRequested;
    public event Action? DuplicateSmdRequested;
    public event Action? DeleteSmdRequested;
    public event Action? SeparateSmdFacesRequested;
    public int SelectedSmdFaceCount=>selectedSmdFaces.Count;
    public IReadOnlyList<int> GetSelectedSmdFaceFlags()=>SelectedSmd()?.LocalTriangles.Where((_,i)=>selectedSmdFaces.Contains(i)).Select(t=>t.SourceStripFlagOffset).Where(x=>x>=0).Distinct().ToArray()??Array.Empty<int>();
    public IReadOnlyList<int> GetAllSmdFaceFlags()=>SelectedSmd()?.LocalTriangles.Select(t=>t.SourceStripFlagOffset).Where(x=>x>=0).Distinct().ToArray()??Array.Empty<int>();

    public void SelectSmdEntry(ScenarioEntry? entry)
    { SelectSmdEntries(entry==null?Array.Empty<ScenarioEntry>():new[]{entry},entry); }
    public void SelectSmdEntries(IEnumerable<ScenarioEntry> entries,ScenarioEntry? primary=null)
    { int previous=selectedSmdEntry;selectedSmdEntries.Clear();foreach(ScenarioEntry entry in entries)selectedSmdEntries.Add(entry.FileOrder);selectedSmdEntry=primary?.FileOrder??selectedSmdEntries.LastOrDefault(-1);if(previous!=selectedSmdEntry)selectedSmdFaces.Clear();smdOverlayDirty=true;Invalidate(); }
    public void SelectSmdFromViewport(ScenarioEntry? entry,bool additive)
    { if(SmdFaceEditMode)return;if(!additive)selectedSmdEntries.Clear();if(entry==null){if(!additive)selectedSmdEntry=-1;}else if(additive&&selectedSmdEntries.Contains(entry.FileOrder)){selectedSmdEntries.Remove(entry.FileOrder);selectedSmdEntry=selectedSmdEntries.LastOrDefault(-1);}else{selectedSmdEntries.Add(entry.FileOrder);selectedSmdEntry=entry.FileOrder;}selectedSmdFaces.Clear();smdOverlayDirty=true;Invalidate();SmdSelectionChanged?.Invoke(SelectedSmdGroup()); }
    public void SetSmdFaceEditMode(bool enabled){SmdFaceEditMode=enabled;selectedSmdFaces.Clear();smdOverlayDirty=true;Invalidate();}
    public void RefreshSmdGeometry(ScenarioEntry? entry = null)
    { if (entry != null) selectedSmdEntry = entry.FileOrder; RebuildScenarioGeometry(); smdOverlayDirty = true; gpuDirty = true; Invalidate(); }
    public void FocusSmd(ScenarioEntry? entry)
    { if (entry == null) return; target = entry.Position; distance = Math.Max(8f, Math.Min(distance, 100f)); cameraPosition = target - GetForward() * distance; Invalidate(); Focus(); }
    public bool UndoSmdEdit() { if (smdUndo.Count == 0) return false; smdUndo.Pop()(); return true; }
    public bool DeleteSelectedSmdFaces()
    {
        ScenarioEntry? selected=SelectedSmd();if(!SmdEditingEnabled||!SmdFaceEditMode||selected==null||selectedSmdFaces.Count==0||scene==null)return false;
        int[] flags=selectedSmdFaces.Where(i=>i>=0&&i<selected.LocalTriangles.Count).Select(i=>selected.LocalTriangles[i].SourceStripFlagOffset).Where(o=>o>=0).Distinct().ToArray();if(flags.Length==0)return false;
        var before=scene.Entries.Where(e=>e.BinId==selected.BinId).ToDictionary(e=>e,e=>e.LocalTriangles);var flagSet=flags.ToHashSet();
        foreach(var entry in before.Keys)entry.LocalTriangles=entry.LocalTriangles.Where(t=>!flagSet.Contains(t.SourceStripFlagOffset)).ToArray();
        foreach(int flag in flags)scene.PendingFaceDeletes.Add((selected.BinId,flag));selectedSmdFaces.Clear();scene.IsModified=true;RebuildScenarioGeometry();gpuDirty=true;smdOverlayDirty=true;SmdEntryEdited?.Invoke(selected);Invalidate();
        smdUndo.Push(()=>{foreach(var pair in before)pair.Key.LocalTriangles=pair.Value;foreach(int flag in flags)scene.PendingFaceDeletes.Remove((selected.BinId,flag));scene.IsModified=true;RebuildScenarioGeometry();gpuDirty=true;smdOverlayDirty=true;SmdEntryEdited?.Invoke(selected);Invalidate();});return true;
    }

    private void BeginSmdFaceBoxSelection(Point point)
    {smdFaceBoxSelecting=true;smdFaceBoxAdditive=(ModifierKeys&Keys.Control)!=0;smdFaceBoxStart=smdFaceBoxEnd=point;}
    private void CompleteSmdFaceBoxSelection()
    {
        smdFaceBoxSelecting=false;RectangleF rect=MakeSelectionRect(smdFaceBoxStart,smdFaceBoxEnd);if(!smdFaceBoxAdditive)selectedSmdFaces.Clear();
        if(rect.Width>=3||rect.Height>=3)
        {
            ScenarioEntry? entry=SelectedSmd();if(entry!=null)for(int i=0;i<entry.LocalTriangles.Count;i++)
            {ScenarioTriangle t=entry.LocalTriangles[i];if(!TryProjectWorldToScreen(TransformSmd(t.A,entry),out PointF a)||!TryProjectWorldToScreen(TransformSmd(t.B,entry),out PointF b)||!TryProjectWorldToScreen(TransformSmd(t.C,entry),out PointF c))continue;if(TriangleIntersectsRect(a,b,c,rect))selectedSmdFaces.Add(i);}
        }
        smdOverlayDirty=true;Invalidate();
    }
    private static RectangleF MakeSelectionRect(Point a,Point b)=>RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));
    private static bool TriangleIntersectsRect(PointF a,PointF b,PointF c,RectangleF r)
    {
        bool Inside(PointF p)=>r.Contains(p);if(Inside(a)||Inside(b)||Inside(c))return true;
        bool InTriangle(PointF p){float d1=Cross(a,b,p),d2=Cross(b,c,p),d3=Cross(c,a,p);return !((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0));}
        if(InTriangle(new(r.Left,r.Top))||InTriangle(new(r.Right,r.Top))||InTriangle(new(r.Right,r.Bottom))||InTriangle(new(r.Left,r.Bottom)))return true;
        PointF[] corners={new(r.Left,r.Top),new(r.Right,r.Top),new(r.Right,r.Bottom),new(r.Left,r.Bottom)};
        for(int i=0;i<4;i++)if(SegmentsIntersect(a,b,corners[i],corners[(i+1)%4])||SegmentsIntersect(b,c,corners[i],corners[(i+1)%4])||SegmentsIntersect(c,a,corners[i],corners[(i+1)%4]))return true;return false;
        static float Cross(PointF p,PointF q,PointF z)=>(q.X-p.X)*(z.Y-p.Y)-(q.Y-p.Y)*(z.X-p.X);
        static bool SegmentsIntersect(PointF p,PointF q,PointF u,PointF v){float a=Cross(p,q,u),b=Cross(p,q,v),c=Cross(u,v,p),d=Cross(u,v,q);if(((a<0&&b>0)||(a>0&&b<0))&&((c<0&&d>0)||(c>0&&d<0)))return true;static bool On(PointF x,PointF y,PointF z)=>z.X>=Math.Min(x.X,y.X)-.001f&&z.X<=Math.Max(x.X,y.X)+.001f&&z.Y>=Math.Min(x.Y,y.Y)-.001f&&z.Y<=Math.Max(x.Y,y.Y)+.001f;return MathF.Abs(a)<.001f&&On(p,q,u)||MathF.Abs(b)<.001f&&On(p,q,v)||MathF.Abs(c)<.001f&&On(u,v,p)||MathF.Abs(d)<.001f&&On(u,v,q);}
    }
    private void DrawSmdFaceSelectionBox()
    {
        if(!smdFaceBoxSelecting||!glReady||ClientSize.Width<=0||ClientSize.Height<=0)return;RectangleF r=MakeSelectionRect(smdFaceBoxStart,smdFaceBoxEnd);float x0=r.Left/ClientSize.Width*2f-1f,x1=r.Right/ClientSize.Width*2f-1f,y0=1f-r.Top/ClientSize.Height*2f,y1=1f-r.Bottom/ClientSize.Height*2f;
        var lines=new List<float>();void L(float x,float y,float z=0)=>lines.AddRange(new[]{x,y,z,0f,0f,0f});L(x0,y0);L(x1,y0);L(x1,y0);L(x1,y1);L(x1,y1);L(x0,y1);L(x0,y1);L(x0,y0);
        if(smdFaceBoxVao==0)smdFaceBoxVao=GL.GenVertexArray();if(smdFaceBoxVbo==0)smdFaceBoxVbo=GL.GenBuffer();UploadLineBuffer(smdFaceBoxVao,smdFaceBoxVbo,lines,out smdFaceBoxCount);
        Rectangle viewport=GetRenderViewport();GL.Viewport(0,0,ClientSize.Width,ClientSize.Height);GL.UseProgram(shaderProgram);Matrix4 identity=Matrix4.Identity;GL.UniformMatrix4(uMvp,true,ref identity);GL.Uniform1(uUseTexture,0);GL.Uniform1(uUnlit,1);GL.Disable(EnableCap.DepthTest);GL.Disable(EnableCap.CullFace);GL.Uniform3(uColor,.25f,.85f,1f);GL.LineWidth(1.5f);GL.BindVertexArray(smdFaceBoxVao);GL.DrawArrays(PrimitiveType.Lines,0,smdFaceBoxCount);GL.BindVertexArray(0);GL.LineWidth(1f);GL.Enable(EnableCap.DepthTest);GL.Enable(EnableCap.CullFace);GL.Uniform1(uUnlit,0);GL.UseProgram(0);GL.Viewport(viewport.X,ClientSize.Height-viewport.Bottom,viewport.Width,viewport.Height);
    }

    private ScenarioEntry? SelectedSmd() => scene?.Entries.FirstOrDefault(x => x.FileOrder == selectedSmdEntry);
    private IReadOnlyList<ScenarioEntry> SelectedSmdGroup()=>scene?.Entries.Where(e=>selectedSmdEntries.Contains(e.FileOrder)).ToArray()??Array.Empty<ScenarioEntry>();
    private static NVector3 TransformSmd(NVector3 v, ScenarioEntry e)
    {
        v *= new NVector3(SafeSmdScale(e.ScaleX), SafeSmdScale(e.ScaleY), SafeSmdScale(e.ScaleZ));
        if (MathF.Abs(e.RotationX) > 0.000001f) { float c=MathF.Cos(e.RotationX),s=MathF.Sin(e.RotationX); v=new(v.X,v.Y*c-v.Z*s,v.Y*s+v.Z*c); }
        if (MathF.Abs(e.RotationY) > 0.000001f) { float c=MathF.Cos(e.RotationY),s=MathF.Sin(e.RotationY); v=new(v.X*c+v.Z*s,v.Y,-v.X*s+v.Z*c); }
        if (MathF.Abs(e.RotationZ) > 0.000001f) { float c=MathF.Cos(e.RotationZ),s=MathF.Sin(e.RotationZ); v=new(v.X*c-v.Y*s,v.X*s+v.Y*c,v.Z); }
        return v + e.Position;
    }
    private static float SafeSmdScale(float v) => float.IsFinite(v) ? v : 1f;

    private void RebuildScenarioGeometry()
    {
        if (scene == null || scene.Entries.Count == 0) return;
        scene.Triangles.Clear();
        NVector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);
        foreach (ScenarioEntry e in scene.Entries) foreach (ScenarioTriangle t in e.LocalTriangles)
        {
            NVector3 a=TransformSmd(t.A,e),b=TransformSmd(t.B,e),c=TransformSmd(t.C,e);
            var uvA=t.UvA;var uvB=t.UvB;var uvC=t.UvC;
            if (e.ScaleX*e.ScaleY*e.ScaleZ < 0f) {(b,c)=(c,b);(uvB,uvC)=(uvC,uvB);}
            scene.Triangles.Add(new(a,b,c,uvA,uvB,uvC,t.TextureIndex));
            min=NVector3.Min(min,NVector3.Min(a,NVector3.Min(b,c))); max=NVector3.Max(max,NVector3.Max(a,NVector3.Max(b,c)));
        }
        if (scene.Triangles.Count > 0) { scene.BoundsMin=min; scene.BoundsMax=max; }
    }

    private ScenarioEntry? PickSmd(Point mouse)
    {
        if (scene == null || !TryBuildPickRay(mouse,out NVector3 origin,out NVector3 direction)) return null;
        ScenarioEntry? best=null; float bestDistance=float.PositiveInfinity;
        foreach (ScenarioEntry e in scene.Entries) foreach (ScenarioTriangle t in e.LocalTriangles)
        {
            NVector3 a=TransformSmd(t.A,e),b=TransformSmd(t.B,e),c=TransformSmd(t.C,e);
            if (RayTriangle(origin,direction,a,b,c,out float d) && d<bestDistance) { bestDistance=d; best=e; }
        }
        return best;
    }

    private int PickSmdAxis(Point mouse, ScenarioEntry e)
    {
        (NVector3 o,NVector3[] basis,float length)=GetDisplayedSmdGizmoFrame(e); if(!TryProjectWorldToScreen(o,out PointF po)) return 0;
        float best=11f; int axis=0;
        if(SmdTransformMode==SmdGizmoMode.Scale&&MathF.Sqrt((mouse.X-po.X)*(mouse.X-po.X)+(mouse.Y-po.Y)*(mouse.Y-po.Y))<=10f)return 4;
        if(SmdTransformMode==SmdGizmoMode.Rotate)
        {
            const int segments=64;
            for(int ring=0;ring<3;ring++)
            {
                PointF? previous=null;
                for(int i=0;i<=segments;i++)
                {
                    float angle=MathF.Tau*i/segments,c=MathF.Cos(angle)*length,s=MathF.Sin(angle)*length;
                    NVector3 world=ring switch{0=>o+basis[1]*c+basis[2]*s,1=>o+basis[0]*c+basis[2]*s,_=>o+basis[0]*c+basis[1]*s};
                    if(!TryProjectWorldToScreen(world,out PointF point)){previous=null;continue;}
                    if(previous.HasValue){float distance=DistancePointToSegment(mouse,previous.Value,point);if(distance<best){best=distance;axis=ring+1;}}
                    previous=point;
                }
            }
            return axis;
        }
        NVector3[] ends={o+basis[0]*length,o+basis[1]*length,o+basis[2]*length};
        for(int i=0;i<3;i++) if(TryProjectWorldToScreen(ends[i],out PointF pe)){float d=DistancePointToSegment(mouse,po,pe);if(d<best){best=d;axis=i+1;}}
        return axis;
    }
    private bool TryBeginSmdDrag(Point mouse)
    {
        if (!SmdEditingEnabled) return false;
        if(SmdFaceEditMode&&selectedSmdFaces.Count==0)return false;
        ScenarioEntry? e=SelectedSmd(); if(e==null) return false;
        int axis=PickSmdAxis(mouse,e); if(axis==0) return false;
        (NVector3 drawnOrigin,NVector3[] drawnAxes,float drawnLength)=GetDisplayedSmdGizmoFrame(e);
        draggingSmd=e; smdDragAxis=axis; smdDragMouse=mouse; smdDragStart=SmdTransformState.From(e);smdGroupDragStart=SelectedSmdGroup().ToDictionary(x=>x,SmdTransformState.From);if(smdGroupDragStart.Count==0)smdGroupDragStart[e]=smdDragStart;smdGroupPivot=drawnOrigin;
        smdFaceDragging=SmdFaceEditMode&&selectedSmdFaces.Count>0;if(smdFaceDragging)
        {
            smdFaceDragStart=new();var weldPositions=new List<NVector3>();
            foreach(int index in selectedSmdFaces)if(index>=0&&index<e.LocalTriangles.Count){ScenarioTriangle t=e.LocalTriangles[index];weldPositions.Add(t.A);weldPositions.Add(t.B);weldPositions.Add(t.C);}
            // PS2 strips frequently duplicate a logical vertex at segment/material seams.
            // Move every coincident copy so adjacent faces stay welded instead of opening holes.
            float epsilon=Math.Max(0.00001f,(scene?.Radius??1f)*0.000001f);float epsilonSq=epsilon*epsilon;
            foreach(ScenarioTriangle t in e.LocalTriangles){AddIfWelded(t.SourceOffsetA,t.A);AddIfWelded(t.SourceOffsetB,t.B);AddIfWelded(t.SourceOffsetC,t.C);}
            void AddIfWelded(int offset,NVector3 position){if(offset<0||smdFaceDragStart.ContainsKey(offset))return;if(weldPositions.Any(p=>NVector3.DistanceSquared(p,position)<=epsilonSq))smdFaceDragStart[offset]=position;}
        }
        NVector3 direction=axis==4?NVector3.UnitX:drawnAxes[axis-1];smdDragWorldAxis=direction;
        float length=drawnLength;
        if(axis==4){smdDragScreenAxis=new(1f,0f);smdDragUnitsPerPixel=length/72f;}
        else if(TryProjectWorldToScreen(smdGroupPivot,out PointF po)&&TryProjectWorldToScreen(smdGroupPivot+direction*length,out PointF pe))
        {
            smdDragScreenAxis=new(pe.X-po.X,pe.Y-po.Y);float pixels=smdDragScreenAxis.Length();
            if(pixels>.1f){smdDragScreenAxis/=pixels;smdDragUnitsPerPixel=length/pixels;}
            else{smdDragScreenAxis=new(1f,0f);smdDragUnitsPerPixel=.01f;}
        }
        else{smdDragScreenAxis=new(1f,0f);smdDragUnitsPerPixel=.01f;}
        if(SmdTransformMode==SmdGizmoMode.Rotate&&TryProjectWorldToScreen(drawnOrigin,out PointF center))
        {
            var radial=new System.Numerics.Vector2(mouse.X-center.X,mouse.Y-center.Y);
            if(radial.LengthSquared()>.01f){radial=System.Numerics.Vector2.Normalize(radial);smdDragScreenAxis=new(-radial.Y,radial.X);}
        }
        smdOverlayDirty=true;Invalidate();return true;
    }
    private void UpdateSmdDrag(Point mouse)
    {
        ScenarioEntry e=draggingSmd!; int dx=mouse.X-smdDragMouse.X,dy=mouse.Y-smdDragMouse.Y;
        float pixels=System.Numerics.Vector2.Dot(new System.Numerics.Vector2(dx,dy),smdDragScreenAxis);
        if(smdFaceDragging)
        {
            float amount=pixels*smdDragUnitsPerPixel*.25f*SmdTransformSpeed;if(SmdSnapEnabled)amount=MathF.Round(amount/.25f)*.25f;NVector3 world=smdDragWorldAxis*amount;NVector3 local=WorldDeltaToSmdLocal(world,e);var moved=smdFaceDragStart!.ToDictionary(x=>x.Key,x=>x.Value+local);ApplySmdVertexPositions(e,moved);foreach(var pair in moved)scene!.PendingVertexEdits[(e.BinId,pair.Key)]=(pair.Value,FindSmdFactor(e,pair.Key));scene!.IsModified=true;RefreshSmdWorldTriangles(e,true);smdOverlayDirty=true;Invalidate();return;
        }
        if(SmdTransformMode==SmdGizmoMode.Move)
        {
            // One screen-space axis length moves one quarter of the displayed gizmo.
            // This keeps large scenarios from making tiny mouse movements jump meters.
            float amount=pixels*smdDragUnitsPerPixel*.25f*SmdTransformSpeed; if(SmdSnapEnabled) amount=MathF.Round(amount/.25f)*.25f;
            NVector3 delta=smdDragWorldAxis*amount;foreach(var pair in smdGroupDragStart!){SmdTransformState start=pair.Value;ScenarioEntry item=pair.Key;item.PositionX=start.Px+delta.X;item.PositionY=start.Py+delta.Y;item.PositionZ=start.Pz+delta.Z;}
        }
        else if(SmdTransformMode==SmdGizmoMode.Rotate)
        {
            float amount=pixels*(MathF.PI/1800f)*SmdTransformSpeed;if(SmdSnapEnabled)amount=MathF.Round(amount/(MathF.PI/36f))*(MathF.PI/36f);
            foreach(var pair in smdGroupDragStart!){SmdTransformState start=pair.Value;ScenarioEntry item=pair.Key;NVector3 relative=new(start.Px-smdGroupPivot.X,start.Py-smdGroupPivot.Y,start.Pz-smdGroupPivot.Z);relative=RotateAroundAxis(relative,smdDragWorldAxis,amount);item.PositionX=smdGroupPivot.X+relative.X;item.PositionY=smdGroupPivot.Y+relative.Y;item.PositionZ=smdGroupPivot.Z+relative.Z;if(smdDragAxis==1)item.RotationX=start.Rx+amount;else if(smdDragAxis==2)item.RotationY=start.Ry+amount;else item.RotationZ=start.Rz+amount;}
        }
        else
        {
            float factor=Math.Max(.01f,1f+pixels*SmdTransformSpeed/500f);if(SmdSnapEnabled)factor=Math.Max(.05f,MathF.Round(factor/.1f)*.1f);
            foreach(var pair in smdGroupDragStart!){SmdTransformState start=pair.Value;ScenarioEntry item=pair.Key;NVector3 relative=new(start.Px-smdGroupPivot.X,start.Py-smdGroupPivot.Y,start.Pz-smdGroupPivot.Z);NVector3 changed;if(smdDragAxis==4){changed=relative*factor;item.ScaleX=start.Sx*factor;item.ScaleY=start.Sy*factor;item.ScaleZ=start.Sz*factor;}else{float along=NVector3.Dot(relative,smdDragWorldAxis);changed=relative+smdDragWorldAxis*along*(factor-1f);if(smdDragAxis==1)item.ScaleX=start.Sx*factor;else if(smdDragAxis==2)item.ScaleY=start.Sy*factor;else item.ScaleZ=start.Sz*factor;}item.PositionX=smdGroupPivot.X+changed.X;item.PositionY=smdGroupPivot.Y+changed.Y;item.PositionZ=smdGroupPivot.Z+changed.Z;}
        }
        scene!.IsModified=true; foreach(ScenarioEntry item in smdGroupDragStart!.Keys)RefreshSmdWorldTriangles(item);smdOverlayDirty=true;Invalidate();
    }
    private void EndSmdDrag()
    {
        ScenarioEntry e=draggingSmd!; SmdTransformState before=smdDragStart;var groupBefore=smdGroupDragStart;bool wasFace=smdFaceDragging;var faceBefore=smdFaceDragStart; bool changed=wasFace||(groupBefore?.Any(x=>!x.Value.Equals(SmdTransformState.From(x.Key)))??!before.Equals(SmdTransformState.From(e)));var editedEntries=groupBefore?.Keys.ToArray()??new[]{e}; draggingSmd=null;smdDragAxis=0;smdFaceDragging=false;smdFaceDragStart=null;smdGroupDragStart=null;
        smdOverlayDirty=true;Invalidate();
        if(changed){foreach(ScenarioEntry item in editedEntries)BakeSmdWorldTriangles(item);RefreshSmdBounds();foreach(ScenarioEntry item in editedEntries)SmdEntryEdited?.Invoke(item);smdUndo.Push(()=>{if(wasFace&&faceBefore!=null){ApplySmdVertexPositions(e,faceBefore);foreach(var pair in faceBefore)scene!.PendingVertexEdits[(e.BinId,pair.Key)]=(pair.Value,FindSmdFactor(e,pair.Key));}else if(groupBefore!=null)foreach(var pair in groupBefore)pair.Value.Apply(pair.Key);else before.Apply(e);scene!.IsModified=true;selectedSmdEntry=e.FileOrder;RebuildScenarioGeometry();gpuDirty=true;smdOverlayDirty=true;foreach(ScenarioEntry item in (groupBefore?.Keys.Cast<ScenarioEntry>()??new[]{e}))SmdEntryEdited?.Invoke(item);SmdSelectionChanged?.Invoke(SelectedSmdGroup());Invalidate();});}
    }

    private void UploadSmdOverlay()
    {
        smdOverlayDirty=false; EnsureSmdBuffers(); var selected=new List<float>();var faces=new List<float>();var axes=new[]{new List<float>(),new List<float>(),new List<float>(),new List<float>()};ScenarioEntry? e=SelectedSmd();
        if(e==null)smdDisplayedGizmoEntry=-1;
        if(e!=null&&!SmdFaceEditMode){UploadSmdGizmoOnly(e);return;}
        if(e!=null){void L(List<float> v,NVector3 a,NVector3 b)=>v.AddRange(new[]{a.X,a.Y,a.Z,0f,0f,0f,b.X,b.Y,b.Z,0f,0f,0f});foreach(ScenarioEntry item in SelectedSmdGroup())for(int ti=0;ti<item.LocalTriangles.Count;ti++){var t=item.LocalTriangles[ti];var a=TransformSmd(t.A,item);var b=TransformSmd(t.B,item);var c=TransformSmd(t.C,item);var list=SmdFaceEditMode&&ReferenceEquals(item,e)&&selectedSmdFaces.Contains(ti)?faces:selected;L(list,a,b);L(list,b,c);L(list,c,a);}float len=SmdGizmoLength(e);NVector3 origin=GetSmdGizmoOrigin(e);NVector3[] basis=GetSmdAxes(e);SetDisplayedSmdGizmoFrame(e,origin,basis,len);if(SmdTransformMode==SmdGizmoMode.Rotate){const int segments=72;for(int ring=0;ring<3;ring++)for(int i=0;i<segments;i++){float a=MathF.Tau*i/segments,b=MathF.Tau*(i+1)/segments,ca=MathF.Cos(a)*len,sa=MathF.Sin(a)*len,cb=MathF.Cos(b)*len,sb=MathF.Sin(b)*len;NVector3 p=ring switch{0=>origin+basis[1]*ca+basis[2]*sa,1=>origin+basis[0]*ca+basis[2]*sa,_=>origin+basis[0]*ca+basis[1]*sa};NVector3 q=ring switch{0=>origin+basis[1]*cb+basis[2]*sb,1=>origin+basis[0]*cb+basis[2]*sb,_=>origin+basis[0]*cb+basis[1]*cb};L(axes[ring],p,q);}}else for(int i=0;i<3;i++){if(SmdTransformMode==SmdGizmoMode.Move)AddCamArrow(axes[i],origin,basis[i],len);else AddSmdScaleHandle(axes[i],origin,basis[i],basis[(i+1)%3],basis[(i+2)%3],len);}if(SmdTransformMode==SmdGizmoMode.Scale)AddSmdCenterHandle(axes[3],origin,basis,len*.075f);}
        UploadLineBuffer(smdSelectedVao,smdSelectedVbo,selected,out smdSelectedCount);UploadLineBuffer(smdFaceVao,smdFaceVbo,faces,out smdFaceCount);for(int i=0;i<4;i++)UploadLineBuffer(smdGizmoVaos[i],smdGizmoVbos[i],axes[i],out smdGizmoCounts[i]);
    }
    private void UploadSmdGizmoOnly(ScenarioEntry e)
    {
        smdSelectedCount=0;var axes=new[]{new List<float>(),new List<float>(),new List<float>(),new List<float>()};void L(List<float> v,NVector3 a,NVector3 b)=>v.AddRange(new[]{a.X,a.Y,a.Z,0f,0f,0f,b.X,b.Y,b.Z,0f,0f,0f});
        float len=SmdGizmoLength(e);NVector3 origin=GetSmdGizmoOrigin(e);NVector3[] basis=GetSmdAxes(e);SetDisplayedSmdGizmoFrame(e,origin,basis,len);
        if(SmdTransformMode==SmdGizmoMode.Rotate){const int segments=72;for(int ring=0;ring<3;ring++)for(int i=0;i<segments;i++){float a=MathF.Tau*i/segments,b=MathF.Tau*(i+1)/segments,ca=MathF.Cos(a)*len,sa=MathF.Sin(a)*len,cb=MathF.Cos(b)*len,sb=MathF.Sin(b)*len;NVector3 p=ring switch{0=>origin+basis[1]*ca+basis[2]*sa,1=>origin+basis[0]*ca+basis[2]*sa,_=>origin+basis[0]*ca+basis[1]*sa};NVector3 q=ring switch{0=>origin+basis[1]*cb+basis[2]*sb,1=>origin+basis[0]*cb+basis[2]*sb,_=>origin+basis[0]*cb+basis[1]*sb};L(axes[ring],p,q);}}
        else for(int i=0;i<3;i++){if(SmdTransformMode==SmdGizmoMode.Move)AddCamArrow(axes[i],origin,basis[i],len);else AddSmdScaleHandle(axes[i],origin,basis[i],basis[(i+1)%3],basis[(i+2)%3],len);}
        if(SmdTransformMode==SmdGizmoMode.Scale)AddSmdCenterHandle(axes[3],origin,basis,len*.075f);for(int i=0;i<4;i++)UploadLineBuffer(smdGizmoVaos[i],smdGizmoVbos[i],axes[i],out smdGizmoCounts[i]);
    }
    private void EnsureSmdBuffers(){if(smdSelectedVao==0)smdSelectedVao=GL.GenVertexArray();if(smdSelectedVbo==0)smdSelectedVbo=GL.GenBuffer();if(smdFaceVao==0)smdFaceVao=GL.GenVertexArray();if(smdFaceVbo==0)smdFaceVbo=GL.GenBuffer();for(int i=0;i<4;i++){if(smdGizmoVaos[i]==0)smdGizmoVaos[i]=GL.GenVertexArray();if(smdGizmoVbos[i]==0)smdGizmoVbos[i]=GL.GenBuffer();}}
    private void DrawSmdOverlay()
    {
        if(!SmdEditingEnabled||SelectedSmd()==null)return;GL.Uniform1(uUseTexture,0);GL.Uniform1(uUnlit,1);GL.Disable(EnableCap.DepthTest);GL.Disable(EnableCap.CullFace);GL.Uniform3(uColor,1f,.82f,.12f);GL.BindVertexArray(smdSelectedVao);GL.LineWidth(2f);GL.DrawArrays(PrimitiveType.Lines,0,smdSelectedCount);GL.Uniform3(uColor,1f,.35f,.08f);GL.BindVertexArray(smdFaceVao);GL.LineWidth(5f);GL.DrawArrays(PrimitiveType.Lines,0,smdFaceCount);var colors=new[]{(1f,.15f,.12f),(.2f,.9f,.25f),(.15f,.48f,1f),(.92f,.92f,.92f)};for(int i=0;i<4;i++){bool active=draggingSmd!=null&&smdDragAxis==i+1;if(active)GL.Uniform3(uColor,1f,.95f,.25f);else GL.Uniform3(uColor,colors[i].Item1,colors[i].Item2,colors[i].Item3);GL.BindVertexArray(smdGizmoVaos[i]);GL.LineWidth(active?10f:6f);GL.DrawArrays(PrimitiveType.Lines,0,smdGizmoCounts[i]);}GL.LineWidth(1f);GL.Enable(EnableCap.DepthTest);GL.Enable(EnableCap.CullFace);GL.Uniform1(uUnlit,0);
    }
    private NVector3 GetSmdGizmoOrigin(ScenarioEntry e)
    {if(draggingSmd!=null&&smdGroupDragStart!=null&&smdGroupDragStart.TryGetValue(e,out SmdTransformState start)){if(SmdTransformMode==SmdGizmoMode.Move)return smdGroupPivot+(e.Position-new NVector3(start.Px,start.Py,start.Pz));return smdGroupPivot;}if(!SmdFaceEditMode){IReadOnlyList<ScenarioEntry> group=SelectedSmdGroup();if(group.Count==1)return GetSmdEntryVisualCenter(e);NVector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);bool any=false;foreach(ScenarioEntry item in group)foreach(ScenarioTriangle t in item.LocalTriangles){NVector3 a=TransformSmd(t.A,item),b=TransformSmd(t.B,item),c=TransformSmd(t.C,item);min=NVector3.Min(min,NVector3.Min(a,NVector3.Min(b,c)));max=NVector3.Max(max,NVector3.Max(a,NVector3.Max(b,c)));any=true;}return any?(min+max)*.5f:e.Position;}if(selectedSmdFaces.Count==0)return GetSmdEntryVisualCenter(e);NVector3 sum=NVector3.Zero;int count=0;foreach(int i in selectedSmdFaces)if(i>=0&&i<e.LocalTriangles.Count){var t=e.LocalTriangles[i];sum+=TransformSmd((t.A+t.B+t.C)/3f,e);count++;}return count==0?GetSmdEntryVisualCenter(e):sum/count;}
    private NVector3 GetSmdEntryVisualCenter(ScenarioEntry e)
    {NVector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);bool any=false;foreach(ScenarioTriangle t in e.LocalTriangles){min=NVector3.Min(min,NVector3.Min(t.A,NVector3.Min(t.B,t.C)));max=NVector3.Max(max,NVector3.Max(t.A,NVector3.Max(t.B,t.C)));any=true;}return any?TransformSmd((min+max)*.5f,e):e.Position;}
    private float SmdGizmoLength(ScenarioEntry e)
    {
        NVector3 origin=GetSmdGizmoOrigin(e);float depth=Math.Max(.05f,NVector3.Dot(origin-cameraPosition,GetForward()));Rectangle viewport=GetRenderViewport();float aspect=Math.Max(.01f,viewport.Width/(float)Math.Max(1,viewport.Height));float verticalFov=FieldOfViewIsHorizontal?2f*MathF.Atan(MathF.Tan(fieldOfViewDegrees*MathF.PI/360f)/aspect):fieldOfViewDegrees*MathF.PI/180f;float worldPerPixel=2f*depth*MathF.Tan(verticalFov*.5f)/Math.Max(1,viewport.Height);return Math.Clamp(worldPerPixel*72f,.03f,10000f);
    }
    private static void AddSmdScaleHandle(List<float> vertices,NVector3 origin,NVector3 axis,NVector3 sideA,NVector3 sideB,float length)
    {NVector3 end=origin+axis*length;AddCamLine(vertices,origin,end);float size=length*.065f;NVector3 a=sideA*size,b=sideB*size;NVector3 p0=end-a-b,p1=end+a-b,p2=end+a+b,p3=end-a+b;AddCamLine(vertices,p0,p1);AddCamLine(vertices,p1,p2);AddCamLine(vertices,p2,p3);AddCamLine(vertices,p3,p0);AddCamLine(vertices,p0,end+axis*size);AddCamLine(vertices,p1,end+axis*size);AddCamLine(vertices,p2,end+axis*size);AddCamLine(vertices,p3,end+axis*size);}
    private static void AddSmdCenterHandle(List<float> vertices,NVector3 origin,NVector3[] basis,float size)
    {var p=new NVector3[8];for(int i=0;i<8;i++)p[i]=origin+basis[0]*((i&1)==0?-size:size)+basis[1]*((i&2)==0?-size:size)+basis[2]*((i&4)==0?-size:size);foreach((int a,int b) in new[]{(0,1),(2,3),(4,5),(6,7),(0,2),(1,3),(4,6),(5,7),(0,4),(1,5),(2,6),(3,7)})AddCamLine(vertices,p[a],p[b]);}
    private NVector3[] GetSmdAxes(ScenarioEntry e)
    {if(SmdTransformSpace==SmdTransformSpace.World)return new[]{NVector3.UnitX,NVector3.UnitY,NVector3.UnitZ};NVector3 R(NVector3 v){v=RotateX(v,e.RotationX);v=RotateY(v,e.RotationY);v=RotateZ(v,e.RotationZ);return NVector3.Normalize(v);}return new[]{R(NVector3.UnitX),R(NVector3.UnitY),R(NVector3.UnitZ)};}
    private void SetDisplayedSmdGizmoFrame(ScenarioEntry e,NVector3 origin,NVector3[] axes,float length)
    {smdDisplayedGizmoEntry=e.FileOrder;smdDisplayedGizmoOrigin=origin;smdDisplayedGizmoAxes=axes;smdDisplayedGizmoLength=length;}
    private (NVector3 Origin,NVector3[] Axes,float Length) GetDisplayedSmdGizmoFrame(ScenarioEntry e)
    {return smdDisplayedGizmoEntry==e.FileOrder?(smdDisplayedGizmoOrigin,smdDisplayedGizmoAxes,smdDisplayedGizmoLength):(GetSmdGizmoOrigin(e),GetSmdAxes(e),SmdGizmoLength(e));}
    private static NVector3 RotateAroundAxis(NVector3 value,NVector3 axis,float angle)
    {axis=NVector3.Normalize(axis);float c=MathF.Cos(angle),s=MathF.Sin(angle);return value*c+NVector3.Cross(axis,value)*s+axis*NVector3.Dot(axis,value)*(1f-c);}
    private bool HandleSmdFaceClick(Point mouse,bool additive)
    {
        if(!SmdEditingEnabled||!SmdFaceEditMode)return false;ScenarioEntry? e=SelectedSmd();if(e==null||!TryBuildPickRay(mouse,out NVector3 origin,out NVector3 direction))return true;int hit=-1;float best=float.PositiveInfinity;for(int i=0;i<e.LocalTriangles.Count;i++){var t=e.LocalTriangles[i];if(RayTriangle(origin,direction,TransformSmd(t.A,e),TransformSmd(t.B,e),TransformSmd(t.C,e),out float d)&&d<best){best=d;hit=i;}}if(!additive)selectedSmdFaces.Clear();if(hit>=0){if(additive&&!selectedSmdFaces.Add(hit))selectedSmdFaces.Remove(hit);else selectedSmdFaces.Add(hit);}smdOverlayDirty=true;Invalidate();return true;
    }
    private static NVector3 WorldDeltaToSmdLocal(NVector3 v,ScenarioEntry e)
    {v=RotateZ(v,-e.RotationZ);v=RotateY(v,-e.RotationY);v=RotateX(v,-e.RotationX);return new NVector3(MathF.Abs(e.ScaleX)<.000001f?0:v.X/e.ScaleX,MathF.Abs(e.ScaleY)<.000001f?0:v.Y/e.ScaleY,MathF.Abs(e.ScaleZ)<.000001f?0:v.Z/e.ScaleZ);}
    private static NVector3 RotateX(NVector3 v,float a){float c=MathF.Cos(a),s=MathF.Sin(a);return new(v.X,v.Y*c-v.Z*s,v.Y*s+v.Z*c);}
    private static NVector3 RotateY(NVector3 v,float a){float c=MathF.Cos(a),s=MathF.Sin(a);return new(v.X*c+v.Z*s,v.Y,-v.X*s+v.Z*c);}
    private static NVector3 RotateZ(NVector3 v,float a){float c=MathF.Cos(a),s=MathF.Sin(a);return new(v.X*c-v.Y*s,v.X*s+v.Y*c,v.Z);}
    private static float FindSmdFactor(ScenarioEntry e,int offset)=>e.LocalTriangles.FirstOrDefault(t=>t.SourceOffsetA==offset||t.SourceOffsetB==offset||t.SourceOffsetC==offset).SourceFactor;
    private static void ApplySmdVertexPositions(ScenarioEntry e,IReadOnlyDictionary<int,NVector3> positions)
    {e.LocalTriangles=e.LocalTriangles.Select(t=>new ScenarioTriangle(positions.TryGetValue(t.SourceOffsetA,out var a)?a:t.A,positions.TryGetValue(t.SourceOffsetB,out var b)?b:t.B,positions.TryGetValue(t.SourceOffsetC,out var c)?c:t.C,t.UvA,t.UvB,t.UvC,t.TextureIndex,t.SourceOffsetA,t.SourceOffsetB,t.SourceOffsetC,t.SourceFactor,t.SourceStripFlagOffset)).ToArray();}
    private readonly record struct SmdTransformState(float Px,float Py,float Pz,float Rx,float Ry,float Rz,float Sx,float Sy,float Sz)
    {public static SmdTransformState From(ScenarioEntry e)=>new(e.PositionX,e.PositionY,e.PositionZ,e.RotationX,e.RotationY,e.RotationZ,e.ScaleX,e.ScaleY,e.ScaleZ);public void Apply(ScenarioEntry e){e.PositionX=Px;e.PositionY=Py;e.PositionZ=Pz;e.RotationX=Rx;e.RotationY=Ry;e.RotationZ=Rz;e.ScaleX=Sx;e.ScaleY=Sy;e.ScaleZ=Sz;}}
    private void RefreshSmdWorldTriangles(ScenarioEntry entry,bool geometryChanged=false)
    {
        if(scene==null)return;if(!geometryChanged){smdOverlayDirty=true;return;}
        if(!smdEntryTriangleStarts.TryGetValue(entry.FileOrder,out int start)||start+entry.LocalTriangles.Count>scene.Triangles.Count)
        {RebuildScenarioGeometry();gpuDirty=true;return;}
        for(int i=0;i<entry.LocalTriangles.Count;i++)
        {
            ScenarioTriangle t=entry.LocalTriangles[i];NVector3 a=TransformSmd(t.A,entry),b=TransformSmd(t.B,entry),c=TransformSmd(t.C,entry);var uvB=t.UvB;var uvC=t.UvC;
            if(entry.ScaleX*entry.ScaleY*entry.ScaleZ<0f){(b,c)=(c,b);(uvB,uvC)=(uvC,uvB);}
            scene.Triangles[start+i]=new ScenarioTriangle(a,b,c,t.UvA,uvB,uvC,t.TextureIndex);
        }
        smdGpuDirtyEntries.Add(entry.FileOrder);
    }

    private void BakeSmdWorldTriangles(ScenarioEntry entry)
    {
        if(scene==null)return;if(!smdEntryTriangleStarts.TryGetValue(entry.FileOrder,out int start)||start+entry.LocalTriangles.Count>scene.Triangles.Count){RebuildScenarioGeometry();return;}
        for(int i=0;i<entry.LocalTriangles.Count;i++){ScenarioTriangle t=entry.LocalTriangles[i];NVector3 a=TransformSmd(t.A,entry),b=TransformSmd(t.B,entry),c=TransformSmd(t.C,entry);var uvB=t.UvB;var uvC=t.UvC;if(entry.ScaleX*entry.ScaleY*entry.ScaleZ<0f){(b,c)=(c,b);(uvB,uvC)=(uvC,uvB);}scene.Triangles[start+i]=new(a,b,c,t.UvA,uvB,uvC,t.TextureIndex);}
    }

    private void RefreshSmdBounds()
    {
        if(scene==null||scene.Triangles.Count==0)return;NVector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);
        foreach(ScenarioTriangle t in scene.Triangles){min=NVector3.Min(min,NVector3.Min(t.A,NVector3.Min(t.B,t.C)));max=NVector3.Max(max,NVector3.Max(t.A,NVector3.Max(t.B,t.C)));}
        scene.BoundsMin=min;scene.BoundsMax=max;
    }

    private void UploadAllSmdEntryGpu()
    {
        ReleaseSmdEntryGpu();if(scene==null)return;
        foreach(ScenarioEntry entry in scene.Entries)UploadSmdEntryGpu(entry);
        smdGpuDirtyEntries.Clear();meshVertexCount=0;meshBatches.Clear();
    }

    private void UploadDirtySmdEntryGpu()
    {
        if(!SmdEditingEnabled||scene==null||smdGpuDirtyEntries.Count==0)return;
        foreach(int fileOrder in smdGpuDirtyEntries.ToArray())
        {ScenarioEntry? entry=scene.Entries.FirstOrDefault(x=>x.FileOrder==fileOrder);if(entry!=null)UploadSmdEntryGpu(entry);}
        smdGpuDirtyEntries.Clear();
    }

    private void UploadSmdEntryGpu(ScenarioEntry entry)
    {
        if(!smdEntryGpu.TryGetValue(entry.FileOrder,out SmdEntryGpu? gpu))
        {gpu=new SmdEntryGpu{Entry=entry,Vao=GL.GenVertexArray(),Vbo=GL.GenBuffer()};smdEntryGpu.Add(entry.FileOrder,gpu);}gpu.Entry=entry;
        ScenarioTriangle[] ordered=entry.LocalTriangles.OrderBy(t=>t.TextureIndex).ToArray();
        float[] data=new float[ordered.Length*24];var normalSums=new Dictionary<NVector3,NVector3>(ordered.Length*2);
        foreach(ScenarioTriangle t in ordered)
        {NVector3 a=t.A,b=t.B,c=t.C;NVector3 n=NVector3.Cross(b-a,c-a);float length=n.LengthSquared();if(length>0.000001f&&float.IsFinite(length)){AddNormal(normalSums,a,n);AddNormal(normalSums,b,n);AddNormal(normalSums,c,n);}}
        gpu.Batches.Clear();int offset=0,currentTexture=int.MinValue,first=0,count=0;
        foreach(ScenarioTriangle t in ordered)
        {
            if(t.TextureIndex!=currentTexture){if(count>0)gpu.Batches.Add(new ScenarioDrawBatch(currentTexture,first,count));currentTexture=t.TextureIndex;first=offset/8;count=0;}
            NVector3 a=t.A,b=t.B,c=t.C;var uvB=t.UvB;var uvC=t.UvC;
            WriteTexturedVertex(data,ref offset,a,GetSmoothNormal(normalSums,a),t.UvA);WriteTexturedVertex(data,ref offset,b,GetSmoothNormal(normalSums,b),uvB);WriteTexturedVertex(data,ref offset,c,GetSmoothNormal(normalSums,c),uvC);count+=3;
        }
        if(count>0)gpu.Batches.Add(new ScenarioDrawBatch(currentTexture,first,count));gpu.VertexCount=offset/8;
        GL.BindVertexArray(gpu.Vao);GL.BindBuffer(BufferTarget.ArrayBuffer,gpu.Vbo);GL.BufferData(BufferTarget.ArrayBuffer,offset*sizeof(float),data,BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,8*sizeof(float),0);GL.EnableVertexAttribArray(0);GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,8*sizeof(float),3*sizeof(float));GL.EnableVertexAttribArray(1);GL.VertexAttribPointer(2,2,VertexAttribPointerType.Float,false,8*sizeof(float),6*sizeof(float));GL.EnableVertexAttribArray(2);GL.BindVertexArray(0);
    }

    private void ReleaseSmdEntryGpu()
    {
        foreach(SmdEntryGpu gpu in smdEntryGpu.Values){if(gpu.Vbo!=0)GL.DeleteBuffer(gpu.Vbo);if(gpu.Vao!=0)GL.DeleteVertexArray(gpu.Vao);}smdEntryGpu.Clear();smdGpuDirtyEntries.Clear();
    }

    private void DrawSmdEntryMeshes()
    {
        if(RenderMode==ScenarioRenderMode.Wireframe){GL.Uniform1(uUseTexture,0);GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line);foreach(SmdEntryGpu gpu in smdEntryGpu.Values){ApplySmdEntryModel(gpu);GL.BindVertexArray(gpu.Vao);foreach(ScenarioDrawBatch batch in gpu.Batches)GL.DrawArrays(PrimitiveType.Triangles,batch.FirstVertex,batch.VertexCount);}GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);DrawSmdSelectionOutlines();ResetSmdEntryModel();return;}
        GL.BindVertexArray(0);GL.Uniform1(uUnlit,0);GL.Uniform3(uColor,185f/255f,190f/255f,198f/255f);GL.ActiveTexture(TextureUnit.Texture0);GL.Uniform1(uTexture,0);
        foreach(bool transparentPass in new[]{false,true})
        {
            GL.Enable(EnableCap.Blend);if(!transparentPass)GL.Disable(EnableCap.Blend);GL.DepthMask(!transparentPass);if(transparentPass){GL.BlendEquation(BlendEquationMode.FuncAdd);GL.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);}
            foreach(SmdEntryGpu gpu in smdEntryGpu.Values){ApplySmdEntryModel(gpu);GL.BindVertexArray(gpu.Vao);foreach(ScenarioDrawBatch batch in gpu.Batches){bool transparent=glTextureHasTransparency.TryGetValue(batch.TextureIndex,out bool has)&&has;if(transparent!=transparentPass)continue;DrawScenarioBatch(batch);}}
        }
        GL.DepthMask(true);GL.Disable(EnableCap.Blend);GL.BindTexture(TextureTarget.Texture2D,0);
        if(RenderMode==ScenarioRenderMode.SolidWireframe){GL.Uniform1(uUseTexture,0);GL.Uniform1(uUnlit,1);GL.Uniform3(uColor,25f/255f,30f/255f,36f/255f);GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line);foreach(SmdEntryGpu gpu in smdEntryGpu.Values){ApplySmdEntryModel(gpu);GL.BindVertexArray(gpu.Vao);GL.DrawArrays(PrimitiveType.Triangles,0,gpu.VertexCount);}GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);}
        DrawSmdSelectionOutlines();ResetSmdEntryModel();
    }

    private void DrawSmdSelectionOutlines()
    {
        if(!SmdEditingEnabled||SelectedSmd()==null)return;GL.Uniform1(uUseTexture,0);GL.Uniform1(uUnlit,1);GL.Uniform3(uColor,1f,.82f,.12f);GL.Disable(EnableCap.DepthTest);GL.Disable(EnableCap.CullFace);GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line);GL.LineWidth(2f);
        foreach(ScenarioEntry entry in SelectedSmdGroup())if(smdEntryGpu.TryGetValue(entry.FileOrder,out SmdEntryGpu? gpu)){ApplySmdEntryModel(gpu);GL.BindVertexArray(gpu.Vao);GL.DrawArrays(PrimitiveType.Triangles,0,gpu.VertexCount);}
        GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);GL.LineWidth(1f);GL.Enable(EnableCap.DepthTest);GL.Enable(EnableCap.CullFace);GL.Uniform1(uUnlit,0);ResetSmdEntryModel();
    }

    private void ApplySmdEntryModel(SmdEntryGpu gpu)
    {
        ScenarioEntry e=gpu.Entry;SmdTransformState current=SmdTransformState.From(e);
        if(!gpu.HasModel||gpu.CachedTransform!=current)
        {
            float sx=SafeSmdScale(e.ScaleX),sy=SafeSmdScale(e.ScaleY),sz=SafeSmdScale(e.ScaleZ);Matrix4 rx=Matrix4.CreateRotationX(e.RotationX),ry=Matrix4.CreateRotationY(e.RotationY),rz=Matrix4.CreateRotationZ(e.RotationZ);gpu.Model=Matrix4.CreateScale(sx,sy,sz)*rx*ry*rz*Matrix4.CreateTranslation(e.PositionX,e.PositionY,e.PositionZ);
            Matrix4 normalSource=Matrix4.CreateScale(MathF.Abs(sx)<.000001f?1f:sx,MathF.Abs(sy)<.000001f?1f:sy,MathF.Abs(sz)<.000001f?1f:sz)*rx*ry*rz;Matrix4.Invert(normalSource,out Matrix4 inverse);gpu.Normal=Matrix4.Transpose(inverse);gpu.NormalSign=sx*sy*sz<0f?-1f:1f;gpu.CachedTransform=current;gpu.HasModel=true;
        }
        GL.UniformMatrix4(uModel,true,ref gpu.Model);GL.UniformMatrix4(uNormalMatrix,true,ref gpu.Normal);GL.Uniform1(uNormalSign,gpu.NormalSign);GL.FrontFace(gpu.NormalSign<0f?FrontFaceDirection.Cw:FrontFaceDirection.Ccw);
    }
    private void ResetSmdEntryModel(){Matrix4 identity=Matrix4.Identity;GL.UniformMatrix4(uModel,true,ref identity);GL.UniformMatrix4(uNormalMatrix,true,ref identity);GL.Uniform1(uNormalSign,1f);GL.FrontFace(FrontFaceDirection.Ccw);}

    private sealed class SmdEntryGpu { public ScenarioEntry Entry=null!;public int Vao,Vbo,VertexCount;public bool HasModel;public float NormalSign=1f;public SmdTransformState CachedTransform;public Matrix4 Model=Matrix4.Identity,Normal=Matrix4.Identity;public List<ScenarioDrawBatch> Batches{get;}=new(); }

    private void RefreshSmdOverlayCamera()
    {
        Size size=ClientSize;if(!smdOverlayCameraKnown||smdOverlayCameraPosition!=cameraPosition||smdOverlayYaw!=yaw||smdOverlayPitch!=pitch||smdOverlayClientSize!=size)
        {smdOverlayCameraKnown=true;smdOverlayCameraPosition=cameraPosition;smdOverlayYaw=yaw;smdOverlayPitch=pitch;smdOverlayClientSize=size;smdOverlayDirty=true;}
    }
}
