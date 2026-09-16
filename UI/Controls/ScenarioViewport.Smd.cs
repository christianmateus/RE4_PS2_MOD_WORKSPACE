using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using OpenTK.Graphics.OpenGL4;
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
    private readonly Stack<Action> smdUndo = new();
    private readonly HashSet<int> selectedSmdFaces=new();
    private Dictionary<int,NVector3>? smdFaceDragStart;
    private bool smdFaceDragging;
    private int smdFaceVao,smdFaceVbo,smdFaceCount;

    public bool SmdEditingEnabled { get; set; }
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
        NVector3 o=GetSmdGizmoOrigin(e); if(!TryProjectWorldToScreen(o,out PointF po)) return 0;
        NVector3[] basis=GetSmdAxes(e);
        float length=SmdGizmoLength(e),best=11f; int axis=0;
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
        ScenarioEntry? e=SelectedSmd(); if(e==null) return false;
        int axis=PickSmdAxis(mouse,e); if(axis==0) return false;
        draggingSmd=e; smdDragAxis=axis; smdDragMouse=mouse; smdDragStart=SmdTransformState.From(e);smdGroupDragStart=SelectedSmdGroup().ToDictionary(x=>x,SmdTransformState.From);if(smdGroupDragStart.Count==0)smdGroupDragStart[e]=smdDragStart;smdGroupPivot=GetSmdGizmoOrigin(e);
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
        NVector3 direction=axis==4?NVector3.UnitX:GetSmdAxes(e)[axis-1];smdDragWorldAxis=direction;
        float length=SmdGizmoLength(e);
        if(axis==4){smdDragScreenAxis=new(1f,0f);smdDragUnitsPerPixel=length/72f;}
        else if(TryProjectWorldToScreen(smdGroupPivot,out PointF po)&&TryProjectWorldToScreen(smdGroupPivot+direction*length,out PointF pe))
        {
            smdDragScreenAxis=new(pe.X-po.X,pe.Y-po.Y);float pixels=smdDragScreenAxis.Length();
            if(pixels>.1f){smdDragScreenAxis/=pixels;smdDragUnitsPerPixel=length/pixels;}
            else{smdDragScreenAxis=new(1f,0f);smdDragUnitsPerPixel=.01f;}
        }
        else{smdDragScreenAxis=new(1f,0f);smdDragUnitsPerPixel=.01f;}
        if(SmdTransformMode==SmdGizmoMode.Rotate&&TryProjectWorldToScreen(GetSmdGizmoOrigin(e),out PointF center))
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
            float amount=pixels*smdDragUnitsPerPixel*.25f*SmdTransformSpeed;if(SmdSnapEnabled)amount=MathF.Round(amount/.25f)*.25f;NVector3 world=smdDragWorldAxis*amount;NVector3 local=WorldDeltaToSmdLocal(world,e);var moved=smdFaceDragStart!.ToDictionary(x=>x.Key,x=>x.Value+local);ApplySmdVertexPositions(e,moved);foreach(var pair in moved)scene!.PendingVertexEdits[(e.BinId,pair.Key)]=(pair.Value,FindSmdFactor(e,pair.Key));scene!.IsModified=true;RebuildScenarioGeometry();gpuDirty=true;smdOverlayDirty=true;SmdEntryEdited?.Invoke(e);Invalidate();return;
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
        scene!.IsModified=true; RebuildScenarioGeometry(); gpuDirty=true;smdOverlayDirty=true;foreach(ScenarioEntry item in smdGroupDragStart!.Keys)SmdEntryEdited?.Invoke(item);Invalidate();
    }
    private void EndSmdDrag()
    {
        ScenarioEntry e=draggingSmd!; SmdTransformState before=smdDragStart;var groupBefore=smdGroupDragStart;bool wasFace=smdFaceDragging;var faceBefore=smdFaceDragStart; bool changed=wasFace||(groupBefore?.Any(x=>!x.Value.Equals(SmdTransformState.From(x.Key)))??!before.Equals(SmdTransformState.From(e))); draggingSmd=null;smdDragAxis=0;smdFaceDragging=false;smdFaceDragStart=null;smdGroupDragStart=null;
        smdOverlayDirty=true;Invalidate();
        if(changed)smdUndo.Push(()=>{if(wasFace&&faceBefore!=null){ApplySmdVertexPositions(e,faceBefore);foreach(var pair in faceBefore)scene!.PendingVertexEdits[(e.BinId,pair.Key)]=(pair.Value,FindSmdFactor(e,pair.Key));}else if(groupBefore!=null)foreach(var pair in groupBefore)pair.Value.Apply(pair.Key);else before.Apply(e);scene!.IsModified=true;selectedSmdEntry=e.FileOrder;RebuildScenarioGeometry();gpuDirty=true;smdOverlayDirty=true;foreach(ScenarioEntry item in (groupBefore?.Keys.Cast<ScenarioEntry>()??new[]{e}))SmdEntryEdited?.Invoke(item);SmdSelectionChanged?.Invoke(SelectedSmdGroup());Invalidate();});
    }

    private void UploadSmdOverlay()
    {
        smdOverlayDirty=false; EnsureSmdBuffers(); var selected=new List<float>();var faces=new List<float>();var axes=new[]{new List<float>(),new List<float>(),new List<float>(),new List<float>()};ScenarioEntry? e=SelectedSmd();
        if(e!=null){void L(List<float> v,NVector3 a,NVector3 b)=>v.AddRange(new[]{a.X,a.Y,a.Z,0f,0f,0f,b.X,b.Y,b.Z,0f,0f,0f});foreach(ScenarioEntry item in SelectedSmdGroup())for(int ti=0;ti<item.LocalTriangles.Count;ti++){var t=item.LocalTriangles[ti];var a=TransformSmd(t.A,item);var b=TransformSmd(t.B,item);var c=TransformSmd(t.C,item);var list=SmdFaceEditMode&&ReferenceEquals(item,e)&&selectedSmdFaces.Contains(ti)?faces:selected;L(list,a,b);L(list,b,c);L(list,c,a);}float len=SmdGizmoLength(e);NVector3 origin=GetSmdGizmoOrigin(e);NVector3[] basis=GetSmdAxes(e);if(SmdTransformMode==SmdGizmoMode.Rotate){const int segments=72;for(int ring=0;ring<3;ring++)for(int i=0;i<segments;i++){float a=MathF.Tau*i/segments,b=MathF.Tau*(i+1)/segments,ca=MathF.Cos(a)*len,sa=MathF.Sin(a)*len,cb=MathF.Cos(b)*len,sb=MathF.Sin(b)*len;NVector3 p=ring switch{0=>origin+basis[1]*ca+basis[2]*sa,1=>origin+basis[0]*ca+basis[2]*sa,_=>origin+basis[0]*ca+basis[1]*sa};NVector3 q=ring switch{0=>origin+basis[1]*cb+basis[2]*sb,1=>origin+basis[0]*cb+basis[2]*sb,_=>origin+basis[0]*cb+basis[1]*sb};L(axes[ring],p,q);}}else for(int i=0;i<3;i++){if(SmdTransformMode==SmdGizmoMode.Move)AddCamArrow(axes[i],origin,basis[i],len);else AddSmdScaleHandle(axes[i],origin,basis[i],basis[(i+1)%3],basis[(i+2)%3],len);}if(SmdTransformMode==SmdGizmoMode.Scale)AddSmdCenterHandle(axes[3],origin,basis,len*.075f);}
        UploadLineBuffer(smdSelectedVao,smdSelectedVbo,selected,out smdSelectedCount);UploadLineBuffer(smdFaceVao,smdFaceVbo,faces,out smdFaceCount);for(int i=0;i<4;i++)UploadLineBuffer(smdGizmoVaos[i],smdGizmoVbos[i],axes[i],out smdGizmoCounts[i]);
    }
    private void EnsureSmdBuffers(){if(smdSelectedVao==0)smdSelectedVao=GL.GenVertexArray();if(smdSelectedVbo==0)smdSelectedVbo=GL.GenBuffer();if(smdFaceVao==0)smdFaceVao=GL.GenVertexArray();if(smdFaceVbo==0)smdFaceVbo=GL.GenBuffer();for(int i=0;i<4;i++){if(smdGizmoVaos[i]==0)smdGizmoVaos[i]=GL.GenVertexArray();if(smdGizmoVbos[i]==0)smdGizmoVbos[i]=GL.GenBuffer();}}
    private void DrawSmdOverlay()
    {
        if(!SmdEditingEnabled||SelectedSmd()==null)return;GL.Uniform1(uUseTexture,0);GL.Uniform1(uUnlit,1);GL.Disable(EnableCap.DepthTest);GL.Disable(EnableCap.CullFace);GL.Uniform3(uColor,1f,.82f,.12f);GL.BindVertexArray(smdSelectedVao);GL.LineWidth(2f);GL.DrawArrays(PrimitiveType.Lines,0,smdSelectedCount);GL.Uniform3(uColor,1f,.35f,.08f);GL.BindVertexArray(smdFaceVao);GL.LineWidth(5f);GL.DrawArrays(PrimitiveType.Lines,0,smdFaceCount);var colors=new[]{(1f,.15f,.12f),(.2f,.9f,.25f),(.15f,.48f,1f),(.92f,.92f,.92f)};for(int i=0;i<4;i++){bool active=draggingSmd!=null&&smdDragAxis==i+1;if(active)GL.Uniform3(uColor,1f,.95f,.25f);else GL.Uniform3(uColor,colors[i].Item1,colors[i].Item2,colors[i].Item3);GL.BindVertexArray(smdGizmoVaos[i]);GL.LineWidth(active?10f:6f);GL.DrawArrays(PrimitiveType.Lines,0,smdGizmoCounts[i]);}GL.LineWidth(1f);GL.Enable(EnableCap.DepthTest);GL.Enable(EnableCap.CullFace);GL.Uniform1(uUnlit,0);
    }
    private NVector3 GetSmdGizmoOrigin(ScenarioEntry e)
    {if(!SmdFaceEditMode){NVector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);bool any=false;foreach(ScenarioEntry item in SelectedSmdGroup())foreach(ScenarioTriangle t in item.LocalTriangles){NVector3 a=TransformSmd(t.A,item),b=TransformSmd(t.B,item),c=TransformSmd(t.C,item);min=NVector3.Min(min,NVector3.Min(a,NVector3.Min(b,c)));max=NVector3.Max(max,NVector3.Max(a,NVector3.Max(b,c)));any=true;}return any?(min+max)*.5f:e.Position;}if(selectedSmdFaces.Count==0)return GetSmdEntryVisualCenter(e);NVector3 sum=NVector3.Zero;int count=0;foreach(int i in selectedSmdFaces)if(i>=0&&i<e.LocalTriangles.Count){var t=e.LocalTriangles[i];sum+=TransformSmd((t.A+t.B+t.C)/3f,e);count++;}return count==0?GetSmdEntryVisualCenter(e):sum/count;}
    private NVector3 GetSmdEntryVisualCenter(ScenarioEntry e)
    {NVector3 min=new(float.PositiveInfinity),max=new(float.NegativeInfinity);bool any=false;foreach(ScenarioTriangle t in e.LocalTriangles){NVector3 a=TransformSmd(t.A,e),b=TransformSmd(t.B,e),c=TransformSmd(t.C,e);min=NVector3.Min(min,NVector3.Min(a,NVector3.Min(b,c)));max=NVector3.Max(max,NVector3.Max(a,NVector3.Max(b,c)));any=true;}return any?(min+max)*.5f:e.Position;}
    private float SmdGizmoLength(ScenarioEntry e)=>CamScreenSize(GetSmdGizmoOrigin(e),72f);
    private static void AddSmdScaleHandle(List<float> vertices,NVector3 origin,NVector3 axis,NVector3 sideA,NVector3 sideB,float length)
    {NVector3 end=origin+axis*length;AddCamLine(vertices,origin,end);float size=length*.065f;NVector3 a=sideA*size,b=sideB*size;NVector3 p0=end-a-b,p1=end+a-b,p2=end+a+b,p3=end-a+b;AddCamLine(vertices,p0,p1);AddCamLine(vertices,p1,p2);AddCamLine(vertices,p2,p3);AddCamLine(vertices,p3,p0);AddCamLine(vertices,p0,end+axis*size);AddCamLine(vertices,p1,end+axis*size);AddCamLine(vertices,p2,end+axis*size);AddCamLine(vertices,p3,end+axis*size);}
    private static void AddSmdCenterHandle(List<float> vertices,NVector3 origin,NVector3[] basis,float size)
    {var p=new NVector3[8];for(int i=0;i<8;i++)p[i]=origin+basis[0]*((i&1)==0?-size:size)+basis[1]*((i&2)==0?-size:size)+basis[2]*((i&4)==0?-size:size);foreach((int a,int b) in new[]{(0,1),(2,3),(4,5),(6,7),(0,2),(1,3),(4,6),(5,7),(0,4),(1,5),(2,6),(3,7)})AddCamLine(vertices,p[a],p[b]);}
    private NVector3[] GetSmdAxes(ScenarioEntry e)
    {if(SmdTransformSpace==SmdTransformSpace.World)return new[]{NVector3.UnitX,NVector3.UnitY,NVector3.UnitZ};NVector3 R(NVector3 v){v=RotateX(v,e.RotationX);v=RotateY(v,e.RotationY);v=RotateZ(v,e.RotationZ);return NVector3.Normalize(v);}return new[]{R(NVector3.UnitX),R(NVector3.UnitY),R(NVector3.UnitZ)};}
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
}
