using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using OpenTK.Graphics.OpenGL4;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport
{
    private RtpScene? rtpScene;
    private bool rtpGpuDirty;
    private int rtpConnectionsVao, rtpConnectionsVbo, rtpConnectionsVertexCount;
    private int rtpNodesVao, rtpNodesVbo, rtpNodesVertexCount;
    private readonly int[] rtpGizmoVaos=new int[3],rtpGizmoVbos=new int[3],rtpGizmoVertexCounts=new int[3];
    private int selectedRtpNode=-1,rtpDragAxis;
    private Point rtpDragMouse;
    private NVector3 rtpDragStart;

    public bool RtpVisible { get; set; }
    public RtpScene? RtpScene => rtpScene;
    public int SelectedRtpNodeIndex => selectedRtpNode;
    public bool IsRtpDragging => rtpDragAxis != 0;
    public event Action<RtpNode?>? RtpNodeSelected;
    public event Action? RtpSceneEdited;

    public void SetRtpScene(RtpScene? value)
    {
        rtpScene = value;
        selectedRtpNode = -1;
        rtpGpuDirty = true;
        Invalidate();
    }

    private void UploadRtp()
    {
        rtpGpuDirty = false;
        rtpConnectionsVertexCount = rtpNodesVertexCount = 0;
        if (rtpScene == null || !glReady) return;
        if (rtpConnectionsVao == 0) { rtpConnectionsVao = GL.GenVertexArray(); rtpConnectionsVbo = GL.GenBuffer(); }
        if (rtpNodesVao == 0) { rtpNodesVao = GL.GenVertexArray(); rtpNodesVbo = GL.GenBuffer(); }
        for(int i=0;i<3;i++){if(rtpGizmoVaos[i]==0){rtpGizmoVaos[i]=GL.GenVertexArray();rtpGizmoVbos[i]=GL.GenBuffer();}}

        var lines = new List<float>();
        var seen = new HashSet<(int, int)>();
        foreach (RtpConnection connection in rtpScene.Connections)
        {
            int a = Math.Min(connection.From, connection.To), b = Math.Max(connection.From, connection.To);
            if (!seen.Add((a, b))) continue;
            AddRtpVertex(lines, rtpScene.Nodes[connection.From].Position);
            AddRtpVertex(lines, rtpScene.Nodes[connection.To].Position);
        }
        UploadLineBuffer(rtpConnectionsVao, rtpConnectionsVbo, lines, out rtpConnectionsVertexCount);

        float markerRadius = Math.Clamp((scene?.Radius ?? 1000f) * 0.0025f, 0.35f, 2.5f);
        var markers = new List<float>(rtpScene.Nodes.Count * 18 * 6);var selectedMarkers=new List<float>();
        foreach (RtpNode node in rtpScene.Nodes)
        {
            NVector3 p = node.Position;
            List<float> target=node.Index==selectedRtpNode?selectedMarkers:markers;
            AddRtpSegment(target, p + new NVector3(-markerRadius, 0, 0), p + new NVector3(markerRadius, 0, 0));
            AddRtpSegment(target, p + new NVector3(0, -markerRadius, 0), p + new NVector3(0, markerRadius, 0));
            AddRtpSegment(target, p + new NVector3(0, 0, -markerRadius), p + new NVector3(0, 0, markerRadius));
        }
        UploadLineBuffer(rtpNodesVao, rtpNodesVbo, markers, out rtpNodesVertexCount);
        var axes=new[]{new List<float>(),new List<float>(),new List<float>()};
        if(selectedRtpNode>=0&&selectedRtpNode<rtpScene.Nodes.Count){NVector3 p=rtpScene.Nodes[selectedRtpNode].Position;float len=Math.Clamp(markerRadius*4f,3f,10f);AddRtpArrow(axes[0],p,NVector3.UnitX,len);AddRtpArrow(axes[1],p,NVector3.UnitY,len);AddRtpArrow(axes[2],p,NVector3.UnitZ,len);}
        for(int i=0;i<3;i++)UploadLineBuffer(rtpGizmoVaos[i],rtpGizmoVbos[i],axes[i],out rtpGizmoVertexCounts[i]);
    }

    private static void AddRtpArrow(List<float> values,NVector3 p,NVector3 axis,float len){NVector3 end=p+axis*len;AddRtpSegment(values,p,end);NVector3 side=axis==NVector3.UnitY?NVector3.UnitX:NVector3.UnitY;AddRtpSegment(values,end,end-axis*.8f+side*.35f);AddRtpSegment(values,end,end-axis*.8f-side*.35f);}

    private static void AddRtpSegment(List<float> values, NVector3 a, NVector3 b) { AddRtpVertex(values, a); AddRtpVertex(values, b); }
    private static void AddRtpVertex(List<float> values, NVector3 p)
    {
        values.Add(p.X); values.Add(p.Y); values.Add(p.Z);
        values.Add(0f); values.Add(1f); values.Add(0f);
    }

    private void DrawRtpGpu()
    {
        if (!RtpVisible || rtpScene == null) return;
        GL.Uniform1(uOpacity, 1f); GL.Uniform1(uUnlit, 1); GL.Uniform1(uUseTexture, 0);
        GL.Disable(EnableCap.CullFace);
        if (rtpConnectionsVertexCount > 0)
        {
            GL.Uniform3(uColor, 0.05f, 0.88f, 1f);
            GL.BindVertexArray(rtpConnectionsVao); GL.LineWidth(2.5f);
            GL.DrawArrays(PrimitiveType.Lines, 0, rtpConnectionsVertexCount);
        }
        if (rtpNodesVertexCount > 0)
        {
            GL.Uniform3(uColor, 1f, 0.76f, 0.08f);
            GL.BindVertexArray(rtpNodesVao); GL.LineWidth(4f);
            GL.DrawArrays(PrimitiveType.Lines, 0, rtpNodesVertexCount);
        }
        var colors=new[]{(1f,.15f,.12f),(.2f,.95f,.25f),(.12f,.48f,1f)};for(int i=0;i<3;i++)if(rtpGizmoVertexCounts[i]>0){GL.Uniform3(uColor,colors[i].Item1,colors[i].Item2,colors[i].Item3);GL.BindVertexArray(rtpGizmoVaos[i]);GL.LineWidth(5f);GL.DrawArrays(PrimitiveType.Lines,0,rtpGizmoVertexCounts[i]);}
        GL.LineWidth(1f); GL.Enable(EnableCap.CullFace);
    }

    private RtpNode? PickRtpNode(Point mouse)
    {RtpNode? hit=null;float best=18f;if(rtpScene==null)return null;foreach(RtpNode node in rtpScene.Nodes)if(TryProjectWorldToScreen(node.Position,out PointF p)){float dx=p.X-mouse.X,dy=p.Y-mouse.Y,d=MathF.Sqrt(dx*dx+dy*dy);if(d<best){best=d;hit=node;}}return hit;}
    private int PickRtpAxis(Point mouse,RtpNode node)
    {float len=Math.Clamp((scene?.Radius??1000f)*.01f,3f,10f);if(!TryProjectWorldToScreen(node.Position,out PointF p))return 0;int axis=0;float best=11f;NVector3[] ends={node.Position+NVector3.UnitX*len,node.Position+NVector3.UnitY*len,node.Position+NVector3.UnitZ*len};for(int i=0;i<3;i++)if(TryProjectWorldToScreen(ends[i],out PointF end)){float d=DistancePointToSegment(mouse,p,end);if(d<best){best=d;axis=i+1;}}return axis;}
    private bool TryBeginRtpDrag(Point mouse)
    {if(!RtpVisible||rtpScene==null||selectedRtpNode<0||selectedRtpNode>=rtpScene.Nodes.Count)return false;RtpNode node=rtpScene.Nodes[selectedRtpNode];int axis=PickRtpAxis(mouse,node);if(axis==0)return false;rtpDragAxis=axis;rtpDragMouse=mouse;rtpDragStart=node.Position;return true;}
    private void ClearRtpSelectionOnMiss(Point mouse)
    {if(RtpVisible&&rtpScene!=null&&selectedRtpNode>=0&&PickRtpNode(mouse)==null){selectedRtpNode=-1;rtpGpuDirty=true;RtpNodeSelected?.Invoke(null);Invalidate();}}
    private void UpdateRtpDrag(Point mouse)
    {if(rtpScene==null||selectedRtpNode<0)return;RtpNode node=rtpScene.Nodes[selectedRtpNode];NVector3 axis=rtpDragAxis==1?NVector3.UnitX:rtpDragAxis==2?NVector3.UnitY:NVector3.UnitZ;float len=Math.Clamp((scene?.Radius??1000f)*.01f,3f,10f);if(TryProjectWorldToScreen(rtpDragStart,out PointF a)&&TryProjectWorldToScreen(rtpDragStart+axis*len,out PointF b)){System.Numerics.Vector2 screenAxis=new(b.X-a.X,b.Y-a.Y);float pixels=screenAxis.Length();if(pixels>.1f){screenAxis/=pixels;System.Numerics.Vector2 delta=new(mouse.X-rtpDragMouse.X,mouse.Y-rtpDragMouse.Y);node.Position=rtpDragStart+axis*(System.Numerics.Vector2.Dot(delta,screenAxis)*len/pixels);rtpGpuDirty=true;Invalidate();}}}
    private void EndRtpDrag(){if(rtpScene==null)return;rtpDragAxis=0;rtpScene.IsModified=true;RecalculateRtpDistances();RtpSceneEdited?.Invoke();rtpGpuDirty=true;}
    public bool HandleRtpClick(Point mouse){if(!RtpVisible||rtpScene==null)return false;RtpNode? node=PickRtpNode(mouse);if(node==null){if(selectedRtpNode>=0){selectedRtpNode=-1;rtpGpuDirty=true;RtpNodeSelected?.Invoke(null);Invalidate();}return false;}SelectRtpNode(node.Index);return true;}
    public void SelectRtpNode(int index)
    {if(rtpScene==null||index<0||index>=rtpScene.Nodes.Count){selectedRtpNode=-1;RtpNodeSelected?.Invoke(null);}else{selectedRtpNode=index;RtpNodeSelected?.Invoke(rtpScene.Nodes[index]);}rtpGpuDirty=true;Invalidate();}
    public bool DuplicateSelectedRtpNodeInFront()=>DuplicateSelectedRtpNode(GetForward()*4f);
    public bool AddChildToSelectedRtpNodeInFront()=>AddChildToSelectedRtpNode(GetForward()*4f);
    public bool DuplicateSelectedRtpNode(NVector3 offset)
    {if(rtpScene==null||selectedRtpNode<0||rtpScene.Nodes.Count>=255)return false;int source=selectedRtpNode,newIndex=rtpScene.Nodes.Count;rtpScene.Nodes.Add(new RtpNode(newIndex,rtpScene.Nodes[source].Position+offset));foreach(RtpConnection c in rtpScene.Connections.Where(x=>x.From==source).ToArray())rtpScene.Connections.Add(new RtpConnection(newIndex,c.To,c.Distance));foreach(RtpConnection c in rtpScene.Connections.Where(x=>x.To==source).ToArray())rtpScene.Connections.Add(new RtpConnection(c.From,newIndex,c.Distance));selectedRtpNode=newIndex;rtpScene.IsModified=true;RecalculateRtpDistances();rtpGpuDirty=true;RtpNodeSelected?.Invoke(rtpScene.Nodes[newIndex]);RtpSceneEdited?.Invoke();Invalidate();return true;}
    public bool AddChildToSelectedRtpNode(NVector3 offset)
    {
        if(rtpScene==null||selectedRtpNode<0||selectedRtpNode>=rtpScene.Nodes.Count||rtpScene.Nodes.Count>=255)return false;
        int parent=selectedRtpNode,child=rtpScene.Nodes.Count;
        var node=new RtpNode(child,rtpScene.Nodes[parent].Position+offset);
        rtpScene.Nodes.Add(node);
        ushort distance=Ps2RtpWriter.CalculateDistance(rtpScene.Nodes[parent].Position,node.Position);
        rtpScene.Connections.Add(new RtpConnection(parent,child,distance));
        rtpScene.Connections.Add(new RtpConnection(child,parent,distance));
        selectedRtpNode=child;rtpScene.IsModified=true;rtpGpuDirty=true;
        RtpNodeSelected?.Invoke(node);RtpSceneEdited?.Invoke();Invalidate();return true;
    }
    public bool DeleteSelectedRtpNode()
    {if(rtpScene==null||selectedRtpNode<0||rtpScene.Nodes.Count<=1)return false;int removed=selectedRtpNode;rtpScene.Nodes.RemoveAt(removed);for(int i=0;i<rtpScene.Nodes.Count;i++)rtpScene.Nodes[i].Index=i;var remapped=rtpScene.Connections.Where(x=>x.From!=removed&&x.To!=removed).Select(x=>new RtpConnection(x.From>removed?x.From-1:x.From,x.To>removed?x.To-1:x.To,x.Distance)).Distinct().ToList();rtpScene.Connections.Clear();rtpScene.Connections.AddRange(remapped);selectedRtpNode=-1;rtpScene.IsModified=true;rtpGpuDirty=true;RtpNodeSelected?.Invoke(null);RtpSceneEdited?.Invoke();Invalidate();return true;}
    private void RecalculateRtpDistances(){if(rtpScene==null)return;for(int i=0;i<rtpScene.Connections.Count;i++){RtpConnection c=rtpScene.Connections[i];rtpScene.Connections[i]=c with{Distance=Ps2RtpWriter.CalculateDistance(rtpScene.Nodes[c.From].Position,rtpScene.Nodes[c.To].Position)};}}

    private void DisposeRtpGpu()
    {
        if (rtpConnectionsVbo != 0) GL.DeleteBuffer(rtpConnectionsVbo);
        if (rtpConnectionsVao != 0) GL.DeleteVertexArray(rtpConnectionsVao);
        if (rtpNodesVbo != 0) GL.DeleteBuffer(rtpNodesVbo);
        if (rtpNodesVao != 0) GL.DeleteVertexArray(rtpNodesVao);
        for(int i=0;i<3;i++){if(rtpGizmoVbos[i]!=0)GL.DeleteBuffer(rtpGizmoVbos[i]);if(rtpGizmoVaos[i]!=0)GL.DeleteVertexArray(rtpGizmoVaos[i]);}
        rtpConnectionsVbo = rtpConnectionsVao = rtpNodesVbo = rtpNodesVao = 0;
    }
}
