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
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button is MouseButtons.Left or MouseButtons.Right or MouseButtons.Middle)
        {
            dragButton = e.Button;
            lastMouse = e.Location;
            lastMovementTick = Environment.TickCount64;
            if (e.Button == MouseButtons.Left)
            {
                mouseDownPoint = e.Location;
                leftMouseMoved = false;

                if (TryBeginAssetGizmo(e.Location)) { Capture=true; return; }

                if (TryBeginRtpDrag(e.Location)) { Capture = true; return; }
                ClearRtpSelectionOnMiss(e.Location);

                if (!FseEditingEnabled && TryBeginCamDrag(e.Location)) { Capture = true; return; }

                if (TryBeginSmdDrag(e.Location)) { Capture = true; return; }
                if(SmdEditingEnabled&&SmdFaceEditMode&&SelectedSmd()!=null){BeginSmdFaceBoxSelection(e.Location);Capture=true;return;}

                if (TryBeginEnemyFaceGizmoDrag(e.Location)) { Capture = true; return; }

                if (TryBeginCollisionVertexDrag(e.Location)) { Capture=true; return; }

                if (ObjectsVisible && TryBeginEtsDrag(e.Location))
                {
                    Capture = true;
                    return;
                }

                if (ItaVisible && TryBeginItaDrag(e.Location)) { Capture=true; return; }
                if (TryBeginSoundDrag(e.Location)) { Capture=true; return; }

                AevEntry? selected = GetSelectedAevEntry();
                if (selected != null && (selected.IsSquare || selected.IsCircle))
                {
                    int handle = -1;
                    if (AevEditMode && selected.IsSquare)
                    {
                        handle = PickAevCornerHandle(e.Location, selected);
                        if (handle < 0) handle = PickAevHeightHandle(e.Location, selected);
                    }
                    else if (!AevEditMode) handle = PickAevTransformHandle(e.Location, selected);

                    if (handle >= 0)
                    {
                        draggingAevHandle = handle;
                        draggingAevEntry = selected;
                        dragStartState = AevVertexState.From(selected);

                        if (handle is 4 or 5)
                        {
                            GetAevYRange(selected, out heightDragStartBottomY, out heightDragStartTopY);
                            heightDragStartMouseY = e.Y;
                            heightDragPixelsPerWorldUnit = CalculateVerticalPixelsPerWorldUnit(selected);
                        }
                        else if (handle == 7)
                        {
                            verticalMoveDragStartMouseY = e.Y;
                            verticalMoveStartY = selected.Y;
                            verticalMovePixelsPerWorldUnit = CalculateVerticalPixelsPerWorldUnit(selected);
                        }
                        else if (handle == 9)
                        {
                            PointF centerScreen = ProjectAevCenter(selected);
                            aevRotationStartAngle = MathF.Atan2(e.Y - centerScreen.Y, e.X - centerScreen.X);
                        }
                    }
                }

                if (draggingAevHandle < 0 && EnemiesVisible)
                {
                    EslEnemyEntry? enemy = GetSelectedEnemyEntry();
                    int pickedEnemyHandle = enemy == null ? 0 : PickEnemyGizmoHandle(e.Location, enemy);
                    if (enemy != null && (pickedEnemyHandle > 0 || IsMouseNearEnemy(e.Location, enemy)))
                    {
                        draggingEnemy = enemy; enemyDragStartMouse = e.Location; enemyDragStartX=enemy.PosX; enemyDragStartY=enemy.PosY; enemyDragStartZ=enemy.PosZ; enemyDragStartRotX=enemy.RotX; enemyDragStartRotY=enemy.RotY; enemyDragStartRotZ=enemy.RotZ;
                        enemyDragStartWorld = EslToWorld(enemy);
                        enemyDragMode = pickedEnemyHandle;
                        if (enemyDragMode == 0) enemyDragMode = (ModifierKeys & Keys.Control) != 0 ? 5 : (ModifierKeys & Keys.Shift) != 0 ? 2 : 7;
                        enemyGpuPreviewActive=true;enemyGpuPreviewModel=Matrix4.Identity;
                        if (enemyDragMode == 2)
                        {
                            NVector3 a=enemyDragStartWorld,b=a+NVector3.UnitY;
                            if (TryProjectWorldToScreen(a,out PointF pa) && TryProjectWorldToScreen(b,out PointF pb)) enemyVerticalPixelsPerWorldUnit=Math.Max(0.05f,MathF.Abs(pb.Y-pa.Y));
                        }
                    }
                }
            }
            Capture = true;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == dragButton)
        {
            bool wasHandleDrag = e.Button == MouseButtons.Left && draggingAevHandle >= 0 && draggingAevEntry != null;
            bool wasEnemyDrag = e.Button == MouseButtons.Left && enemyDragMode != 0 && draggingEnemy != null;
            bool wasEtsDrag = e.Button == MouseButtons.Left && IsEtsDragging;
            bool wasItaDrag = e.Button == MouseButtons.Left && IsItaDragging;
            bool wasSoundDrag = e.Button == MouseButtons.Left && IsSoundDragging;
            bool wasSmdDrag = e.Button == MouseButtons.Left && IsSmdDragging;
            bool wasSmdFaceBox=e.Button==MouseButtons.Left&&smdFaceBoxSelecting;
            bool wasCollisionDrag = e.Button == MouseButtons.Left && draggingCollisionVertex;
            bool wasFaceGizmoDrag = e.Button == MouseButtons.Left && enemyFaceGizmoAxis != 0;
            bool wasRtpDrag = e.Button == MouseButtons.Left && IsRtpDragging;
            bool wasCamDrag = e.Button == MouseButtons.Left && IsCamDragging;
            bool wasCamPointClick = e.Button == MouseButtons.Left && ConsumeCamPointClick();
            bool wasAssetGizmoDrag=e.Button==MouseButtons.Left&&assetGizmoDragging;
            bool clickAev = e.Button == MouseButtons.Left && !leftMouseMoved && !wasHandleDrag && !wasEnemyDrag && !wasEtsDrag && !wasItaDrag && !wasSoundDrag && !wasSmdDrag && !wasCollisionDrag && !wasFaceGizmoDrag && !wasRtpDrag && !wasCamDrag;

            if (wasSmdDrag) EndSmdDrag();
            if(wasSmdFaceBox){if(leftMouseMoved){CompleteSmdFaceBoxSelection();dragButton=MouseButtons.None;Capture=false;return;}smdFaceBoxSelecting=false;Invalidate();}
            if (wasEtsDrag) EndEtsDrag();
            if (wasItaDrag) EndItaDrag();
            if (wasSoundDrag) EndSoundDrag();
            if (wasCollisionDrag) EndCollisionVertexDrag();
            if (wasRtpDrag) EndRtpDrag();
            if (wasCamDrag) EndCamDrag();
            if(wasAssetGizmoDrag)EndAssetGizmo();
            if (wasFaceGizmoDrag)
            {
                NVector3 delta = enemyFaceGizmoDelta;
                enemyFaceGizmoAxis = 0; enemyFaceGizmoDelta = NVector3.Zero;
                if (delta.LengthSquared() > 0.0000001f)
                {
                    if (EnemyModelFacePickingEnabled) EnemyModelFaceTranslationRequested?.Invoke(delta);
                    else EnemyModelPartTranslationRequested?.Invoke(delta);
                }
                enemyGpuDirty = true;
            }

            if (wasHandleDrag)
            {
                AevVertexState after = AevVertexState.From(draggingAevEntry!);
                if (dragStartState.HasValue && !dragStartState.Value.Equals(after))
                {
                    AevEntry undoEntry = draggingAevEntry!;
                    AevVertexState restore = dragStartState.Value;
                    aevUndo.Push(() =>
                    {
                        restore.Apply(undoEntry);
                        selectedAevFileOrder = undoEntry.FileOrder;
                        aevGpuDirty = true;
                        AevEntryEdited?.Invoke(undoEntry);
                        AevEntryClicked?.Invoke(undoEntry);
                        Invalidate();
                    });
                    TrimUndoStack();
                    AevEntryEdited?.Invoke(draggingAevEntry!);
                }

                draggingAevHandle = -1;
                draggingAevEntry = null;
                dragStartState = null;
            }

            if (wasEnemyDrag)
            {
                EslEnemyEntry edited = draggingEnemy!;
                bool changed = edited.PosX!=enemyDragStartX || edited.PosY!=enemyDragStartY || edited.PosZ!=enemyDragStartZ || edited.RotX!=enemyDragStartRotX || edited.RotY!=enemyDragStartRotY || edited.RotZ!=enemyDragStartRotZ;
                if (changed)
                {
                    short oldX=enemyDragStartX, oldY=enemyDragStartY, oldZ=enemyDragStartZ, oldRotX=enemyDragStartRotX, oldRotY=enemyDragStartRotY, oldRotZ=enemyDragStartRotZ;
                    RegisterEnemyUndo(() =>
                    {
                        edited.PosX=oldX; edited.PosY=oldY; edited.PosZ=oldZ; edited.RotX=oldRotX; edited.RotY=oldRotY; edited.RotZ=oldRotZ;
                        selectedEnemyIndex=edited.Index; enemyGpuDirty=true; EnemyEntryEdited?.Invoke(edited); EnemyEntryClicked?.Invoke(edited); Invalidate();
                    });
                }
                enemyGpuPreviewActive=false;enemyGpuPreviewModel=Matrix4.Identity;enemyGpuDirty=true;
                enemyDragMode=0; draggingEnemy=null;
                if(changed) EnemyEntryEdited?.Invoke(edited);
            }

            dragButton = MouseButtons.None;
            Capture = false;

            if(wasCamPointClick)return;

            if(e.Button==MouseButtons.Left&&!leftMouseMoved&&!wasSoundDrag&&!wasCamDrag&&!wasSmdDrag&&!wasCollisionDrag&&EnemiesVisible)
            {
                EslEnemyEntry? labelEnemy=PickEnemyLabel(e.Location);
                if(labelEnemy!=null){SelectEnemyEntry(labelEnemy);EnemyEntryClicked?.Invoke(labelEnemy);EnemyLabelClicked?.Invoke(labelEnemy,e.Location);return;}
            }
            if(e.Button==MouseButtons.Left&&!leftMouseMoved&&!wasSoundDrag&&HandleSoundClick(e.Location))return;

            if(e.Button==MouseButtons.Left&&!leftMouseMoved&&!wasItaDrag&&!wasAssetGizmoDrag&&ItaVisible&&itaScene!=null)
            {
                ItaEntry? item=PickIta(e.Location);if(item!=null){SelectItaEntry(item);ItaEntryClicked?.Invoke(item);if(AssetMeshEditingEnabled){AssetMeshSelection? itaSelection=PickItaSurface(e.Location,item);if(itaSelection.HasValue&&AssetSelectionMode==AssetMeshSelectionMode.Object)itaSelection=itaSelection.Value with{Mode=AssetMeshSelectionMode.Object};SetAssetMeshSelection(itaSelection,ita:item);}else SetAssetMeshSelection(null);return;}
                SelectItaEntry(null);ItaEntryClicked?.Invoke(null);SetAssetMeshSelection(null);
            }

            if(e.Button==MouseButtons.Left&&!leftMouseMoved&&!wasRtpDrag&&!wasCamDrag&&HandleRtpClick(e.Location))return;
            if(e.Button==MouseButtons.Left&&!leftMouseMoved&&!wasCamDrag&&HandleCamClick(e.Location))return;

            if (e.Button==MouseButtons.Left && !leftMouseMoved && EffectsVisible && effScene!=null)
            {
                EffEntry? effectHit=PickEffEntry(e.Location);
                if(effectHit!=null){SelectEffEntry(effectHit);EffEntryClicked?.Invoke(effectHit);return;}
            }

            if (SmdEditingEnabled && e.Button == MouseButtons.Left && !leftMouseMoved && !wasSmdDrag)
            { bool additive=(ModifierKeys&Keys.Control)!=0;if(HandleSmdFaceClick(e.Location,additive))return;ScenarioEntry? hit=PickSmd(e.Location);SelectSmdFromViewport(hit,additive);return; }

            bool collisionHit = e.Button == MouseButtons.Left && !leftMouseMoved && !wasCollisionDrag && TryPickCollision(e.Location);
            bool modelPartHit = false;
            if (!collisionHit && !wasFaceGizmoDrag && e.Button == MouseButtons.Left && !leftMouseMoved && EnemyModelFacePickingEnabled)
            {
                EnemyModelFaceHit? face = PickEnemyModelFace(e.Location);
                EnemyModelFaceClicked?.Invoke(face, (ModifierKeys & Keys.Control) != 0);
                modelPartHit = face.HasValue;
            }
            else if (!collisionHit && e.Button == MouseButtons.Left && !leftMouseMoved && EnemyModelPartPickingEnabled)
            {
                EnemyModelPart? part = PickEnemyModelPart(e.Location);
                EnemyModelPartClicked?.Invoke(part);
                modelPartHit = part != null;
            }

            if (!collisionHit && !modelPartHit && clickAev && AevVisible && aevScene != null)
            {
                AevEntry? hit = PickAevEntry(e.Location);
                SelectAevEntry(hit);
                AevEntryClicked?.Invoke(hit);
                if(hit!=null)return;
                if(hit==null && EnemiesVisible && eslScene!=null){EslEnemyEntry? eh=PickEnemyEntryFromModel(e.Location); float best=18f;if(eh==null)foreach(var en in eslScene.Entries.Where(EnemyIsVisible)){if(!TryProjectWorldToScreen(EslToWorld(en),out PointF sp)) continue; float dx=sp.X-e.X,dy=sp.Y-e.Y,d=MathF.Sqrt(dx*dx+dy*dy); if(d<best){best=d;eh=en;}} SelectEnemyEntry(eh); EnemyEntryClicked?.Invoke(eh);if(eh!=null)return;}
            }
            else if (!collisionHit && !modelPartHit && e.Button==MouseButtons.Left && !leftMouseMoved && EnemiesVisible && eslScene!=null)
            {
                // Prefer the visible model surface; the old marker-radius fallback remains useful
                // while a DAT is still loading or for enemies without renderable geometry.
                EslEnemyEntry? hit=PickEnemyEntryFromModel(e.Location); float best=18f;
                if(hit==null)foreach(var en in eslScene.Entries.Where(EnemyIsVisible)){if(!TryProjectWorldToScreen(EslToWorld(en),out PointF sp)) continue; float dx=sp.X-e.X,dy=sp.Y-e.Y,d=MathF.Sqrt(dx*dx+dy*dy); if(d<best){best=d;hit=en;}}
                SelectEnemyEntry(hit); EnemyEntryClicked?.Invoke(hit);if(hit!=null)return;
            }
            if (!collisionHit && e.Button == MouseButtons.Left && !leftMouseMoved && !wasEtsDrag&&!wasAssetGizmoDrag && ObjectsVisible && etsScene != null)
            {
                EtsEntry? hit=PickEtsEntry(e.Location);bool additive=(ModifierKeys&Keys.Control)!=0;
                if(additive&&hit!=null){if(!selectedEtsFileOrders.Add(hit.FileOrder))selectedEtsFileOrders.Remove(hit.FileOrder);selectedEtsFileOrder=selectedEtsFileOrders.Contains(hit.FileOrder)?hit.FileOrder:selectedEtsFileOrders.LastOrDefault(-1);etsGpuDirty=true;Invalidate();}
                else SelectEtsEntry(hit);
                EtsEntry? primary=SelectedEts();EtsEntryClicked?.Invoke(primary);EtsSelectionChanged?.Invoke(etsScene.Entries.Where(x=>selectedEtsFileOrders.Contains(x.FileOrder)).ToArray());
                if(AssetMeshEditingEnabled)SetAssetMeshSelection(primary==null?null:AssetSelectionMode==AssetMeshSelectionMode.Object?SelectWholeEtsMesh(primary):PickEtsMeshElement(e.Location,primary),primary);else SetAssetMeshSelection(null);
            }
        }
    }

    private EnemyModelPart? PickEnemyModelPart(Point mouse)
    {
        if (eslScene == null) return null;
        EnemyModelPart? bestPart = null;
        float bestDepth = float.PositiveInfinity;
        foreach (EslEnemyEntry entry in eslScene.Entries.Where(EnemyIsVisible))
        {
            if (!enemyModels.TryGetValue(entry.EnemyType, out EnemyModelScene? model)) continue;
            NVector3 origin = EslToWorld(entry);
            float rx = entry.RotX * (MathF.PI / 32768f), ry = entry.RotY * (MathF.PI / 32768f), rz = entry.RotZ * (MathF.PI / 32768f);
            foreach (EnemyModelPart part in model.Parts)
            {
                if (!IsEnemyModelPartVisible(entry.EnemyType, part.BinIndex)) continue;
                foreach (EnemyModelTriangle triangle in part.Triangles)
                {
                    NVector3 a = TransformEnemyModelVertex(triangle.A, origin, rx, ry, rz);
                    NVector3 b = TransformEnemyModelVertex(triangle.B, origin, rx, ry, rz);
                    NVector3 c = TransformEnemyModelVertex(triangle.C, origin, rx, ry, rz);
                    if (!TryProjectWorldToScreen(a, out PointF pa) || !TryProjectWorldToScreen(b, out PointF pb) || !TryProjectWorldToScreen(c, out PointF pc)) continue;
                    if (!PointInScreenTriangle(mouse, pa, pb, pc)) continue;
                    float depth = NVector3.DistanceSquared(cameraPosition, (a + b + c) / 3f);
                    if (depth < bestDepth) { bestDepth = depth; bestPart = part; }
                }
            }
        }
        return bestPart;
    }

    private EslEnemyEntry? PickEnemyEntryFromModel(Point mouse)
    {
        if(eslScene==null)return null; EslEnemyEntry? bestEntry=null; float bestDepth=float.PositiveInfinity;
        foreach(EslEnemyEntry entry in eslScene.Entries.Where(EnemyIsVisible))
        {
            if(!enemyModels.TryGetValue(entry.EnemyType,out EnemyModelScene? model))continue;
            NVector3 origin=EslToWorld(entry);float rx=entry.RotX*(MathF.PI/32768f),ry=entry.RotY*(MathF.PI/32768f),rz=entry.RotZ*(MathF.PI/32768f);
            foreach(EnemyModelPart part in model.Parts)
            {
                if(!IsEnemyModelPartAutomaticallyVisible(entry,part))continue;
                foreach(EnemyModelTriangle triangle in part.Triangles)
                {
                    NVector3 a=TransformEnemyModelVertex(triangle.A,origin,rx,ry,rz),b=TransformEnemyModelVertex(triangle.B,origin,rx,ry,rz),c=TransformEnemyModelVertex(triangle.C,origin,rx,ry,rz);
                    if(!TryProjectWorldToScreen(a,out PointF pa)||!TryProjectWorldToScreen(b,out PointF pb)||!TryProjectWorldToScreen(c,out PointF pc)||!PointInScreenTriangle(mouse,pa,pb,pc))continue;
                    float depth=NVector3.DistanceSquared(cameraPosition,(a+b+c)/3f);if(depth<bestDepth){bestDepth=depth;bestEntry=entry;}
                }
            }
        }
        return bestEntry;
    }

    public EnemyModelFaceHit? PickEnemyModelFaceAt(Point mouse) => PickEnemyModelFace(mouse);

    private EnemyModelFaceHit? PickEnemyModelFace(Point mouse)
    {
        if (eslScene == null) return null;
        EnemyModelFaceHit? best = null; float bestDepth = float.PositiveInfinity;
        foreach (EslEnemyEntry entry in eslScene.Entries.Where(EnemyIsVisible))
        {
            if (!enemyModels.TryGetValue(entry.EnemyType, out EnemyModelScene? model)) continue;
            NVector3 origin = EslToWorld(entry);
            float rx = entry.RotX * (MathF.PI / 32768f), ry = entry.RotY * (MathF.PI / 32768f), rz = entry.RotZ * (MathF.PI / 32768f);
            foreach (EnemyModelPart part in model.Parts)
            {
                if (!IsEnemyModelPartVisible(entry.EnemyType, part.BinIndex)) continue;
                foreach (EnemyModelTriangle triangle in part.Triangles)
                {
                    NVector3 a = TransformEnemyModelVertex(triangle.A, origin, rx, ry, rz), b = TransformEnemyModelVertex(triangle.B, origin, rx, ry, rz), c = TransformEnemyModelVertex(triangle.C, origin, rx, ry, rz);
                    if (!TryProjectWorldToScreen(a, out PointF pa) || !TryProjectWorldToScreen(b, out PointF pb) || !TryProjectWorldToScreen(c, out PointF pc) || !PointInScreenTriangle(mouse, pa, pb, pc)) continue;
                    float depth = NVector3.DistanceSquared(cameraPosition, (a + b + c) / 3f);
                    if (depth < bestDepth) { bestDepth = depth; best = new EnemyModelFaceHit(part, triangle); }
                }
            }
        }
        return best;
    }

    private static bool PointInScreenTriangle(Point p, PointF a, PointF b, PointF c)
    {
        static float Edge(PointF x, PointF y, PointF z) => (z.X - x.X) * (y.Y - x.Y) - (z.Y - x.Y) * (y.X - x.X);
        PointF point = new(p.X, p.Y);
        float e0 = Edge(a, b, point), e1 = Edge(b, c, point), e2 = Edge(c, a, point);
        const float tolerance = 0.01f;
        return (e0 >= -tolerance && e1 >= -tolerance && e2 >= -tolerance) || (e0 <= tolerance && e1 <= tolerance && e2 <= tolerance);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragButton == MouseButtons.None) return;

        int dx = e.X - lastMouse.X;
        int dy = e.Y - lastMouse.Y;
        lastMouse = e.Location;

        if (dragButton == MouseButtons.Left &&
            (Math.Abs(e.X - mouseDownPoint.X) > 4 || Math.Abs(e.Y - mouseDownPoint.Y) > 4))
            leftMouseMoved = true;


        if (dragButton == MouseButtons.Right)
        {
            // Fly camera: mouse right rotates only the view. The camera position
            // never jumps around a pivot, so WASD remains predictable.
            yaw -= dx * LookSensitivity;
            pitch -= dy * LookSensitivity;
            pitch = Math.Clamp(pitch, -1.553f, 1.553f);

            // MouseMove can flood the UI queue while RMB is held. Updating movement
            // here prevents the WinForms timer from being starved during simultaneous
            // look + WASD navigation.
            UpdateCameraMovement();
        }
        else if (dragButton == MouseButtons.Middle)
        {
            GetCameraBasis(out _, out NVector3 right, out NVector3 up);
            float amount = Math.Max(moveSpeed * 0.006f, 0.0005f);
            NVector3 delta = (-right * dx + up * dy) * amount;
            cameraPosition += delta;
            target += delta;
        }
        else if (dragButton == MouseButtons.Left)
        {
            if(smdFaceBoxSelecting){smdFaceBoxEnd=e.Location;Invalidate();}
            else if(assetGizmoDragging)UpdateAssetGizmo(e.Location);
            else if (IsRtpDragging) UpdateRtpDrag(e.Location);
            else if (IsCamDragging) UpdateCamDrag(e.Location);
            else if (IsSmdDragging) UpdateSmdDrag(e.Location);
            else if (enemyFaceGizmoAxis != 0)
            {
                if (TryGetEnemyMeshGizmo(out NVector3 center, out float length))
                {
                    NVector3 axis = enemyFaceGizmoAxis == 1 ? NVector3.UnitX : enemyFaceGizmoAxis == 2 ? NVector3.UnitY : NVector3.UnitZ;
                    center -= enemyFaceGizmoDelta;
                    if (eslScene?.Entries.FirstOrDefault(EnemyIsVisible) is EslEnemyEntry entry) center += EslToWorld(entry);
                    if (TryProjectWorldToScreen(center, out PointF a) && TryProjectWorldToScreen(center + axis * length, out PointF b))
                    {
                        System.Numerics.Vector2 screenAxis = new(b.X - a.X, b.Y - a.Y);
                        float pixels = screenAxis.Length();
                        if (pixels > 0.1f)
                        {
                            screenAxis /= pixels;
                            System.Numerics.Vector2 mouseDelta = new(e.X - enemyFaceGizmoStartMouse.X, e.Y - enemyFaceGizmoStartMouse.Y);
                            enemyFaceGizmoDelta = axis * (System.Numerics.Vector2.Dot(mouseDelta, screenAxis) * length / pixels);
                            enemyGpuDirty = true;
                        }
                    }
                }
            }
            else if (draggingCollisionVertex) UpdateCollisionVertexDrag(e.Location);
            else if (IsEtsDragging) UpdateEtsDrag(e.Location);
            else if (IsItaDragging) UpdateItaDrag(e.Location);
            else if (IsSoundDragging) UpdateSoundDrag(e.Location);
            else if (enemyDragMode != 0 && draggingEnemy != null)
            {
                if (enemyDragMode is 1 or 3 or 7)
                {
                    if (TryScreenPointOnHorizontalPlane(enemyDragStartMouse, enemyDragStartWorld.Y, out NVector3 start) && TryScreenPointOnHorizontalPlane(e.Location, enemyDragStartWorld.Y, out NVector3 now))
                    {
                        float rawX=enemyDragStartX+(now.X-start.X)/EslWorldScale, rawZ=enemyDragStartZ+(now.Z-start.Z)/EslWorldScale;
                        int step=EnemySnapEnabled?10:1;
                        if(enemyDragMode is 1 or 7) draggingEnemy.PosX=SnapEnemyShort(rawX,step);
                        if(enemyDragMode is 3 or 7) draggingEnemy.PosZ=SnapEnemyShort(rawZ,step);
                    }
                }
                else if (enemyDragMode == 2)
                {
                    float worldDelta = -(e.Y-enemyDragStartMouse.Y)/Math.Max(0.05f,enemyVerticalPixelsPerWorldUnit); draggingEnemy.PosY=SnapEnemyShort(enemyDragStartY + worldDelta/EslWorldScale,EnemySnapEnabled?10:1);
                }
                else if (enemyDragMode is >=4 and <=6)
                {
                    int delta=e.X-enemyDragStartMouse.X; int rawDelta=(int)MathF.Round(delta*(65536f/720f)); int step=EnemySnapEnabled?(int)MathF.Round(65536f*5f/360f):1;
                    short R(short start)=>SnapEnemyShort(start+rawDelta,step);
                    if(enemyDragMode==4) draggingEnemy.RotX=R(enemyDragStartRotX); else if(enemyDragMode==5) draggingEnemy.RotY=R(enemyDragStartRotY); else draggingEnemy.RotZ=R(enemyDragStartRotZ);
                }
                UpdateEnemyDragPreviewModel(draggingEnemy);Invalidate();
            }
            else if (draggingAevHandle >= 0 && draggingAevEntry != null)
            {
                if (draggingAevHandle <= 3)
                {
                    GetAevYRange(draggingAevEntry, out _, out float editY);
                    if (TryScreenPointOnHorizontalPlane(e.Location, editY, out NVector3 world))
                    {
                        SetAevCorner(draggingAevEntry, draggingAevHandle, new System.Numerics.Vector2(world.X, world.Z));
                        aevGpuDirty = true;
                    }
                }
                else if (draggingAevHandle is 4 or 5)
                {
                    float pixelDelta = e.Y - heightDragStartMouseY;
                    float worldDelta = -pixelDelta / Math.Max(0.001f, heightDragPixelsPerWorldUnit);

                    float bottom = heightDragStartBottomY;
                    float top = heightDragStartTopY;

                    if (draggingAevHandle == 4)
                        bottom = Math.Min(top - 0.01f, heightDragStartBottomY + worldDelta);
                    else
                        top = Math.Max(bottom + 0.01f, heightDragStartTopY + worldDelta);

                    SetAevDisplayedYRange(draggingAevEntry, bottom, top);
                    aevGpuDirty = true;
                }
                else if (draggingAevHandle is 6 or 8)
                {
                    GetAevYRange(draggingAevEntry, out _, out float topY);
                    float planeY = topY + Math.Max(0.05f, (scene?.Radius ?? 1f) * 0.002f);
                    if (TryScreenPointOnHorizontalPlane(mouseDownPoint, planeY, out NVector3 startWorld) &&
                        TryScreenPointOnHorizontalPlane(e.Location, planeY, out NVector3 currentWorld) &&
                        dragStartState.HasValue)
                    {
                        System.Numerics.Vector2 delta = new(currentWorld.X - startWorld.X, currentWorld.Z - startWorld.Z);
                        if (draggingAevHandle == 6) delta.Y = 0f;
                        else delta.X = 0f;
                        dragStartState.Value.Apply(draggingAevEntry);
                        TranslateAev(draggingAevEntry, delta);
                        aevGpuDirty = true;
                    }
                }
                else if (draggingAevHandle == 7)
                {
                    float pixelDelta = e.Y - verticalMoveDragStartMouseY;
                    float displayDelta = -pixelDelta / Math.Max(0.001f, verticalMovePixelsPerWorldUnit);

                    draggingAevEntry.Y = verticalMoveStartY + displayDelta;

                    aevGpuDirty = true;
                }
                else if (draggingAevHandle == 9 && dragStartState.HasValue)
                {
                    PointF centerScreen = ProjectAevCenter(draggingAevEntry);
                    float angle = MathF.Atan2(e.Y - centerScreen.Y, e.X - centerScreen.X);
                    dragStartState.Value.Apply(draggingAevEntry);
                    RotateAev(draggingAevEntry, angle - aevRotationStartAngle);
                    aevGpuDirty = true;
                }
            }
            // Without a handle drag, LMB remains selection-only.
        }
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        // Wheel controls fly speed instead of moving an invisible orbit pivot.
        // This makes close inspection much less sensitive and more predictable.
        float factor = e.Delta > 0 ? 1.20f : (1f / 1.20f);
        moveSpeed = Math.Clamp(moveSpeed * factor, 0.001f, Math.Max(100000f, (scene?.Radius ?? 1000f) * 100f));
        MovementSpeedChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E or Keys.F or Keys.Z or Keys.L or Keys.Delete or Keys.D1 or Keys.D2 or Keys.D3 or Keys.D4 or Keys.D5 or Keys.NumPad1 or Keys.NumPad2 or Keys.NumPad3 or Keys.NumPad4 or Keys.NumPad5 or Keys.ShiftKey or Keys.ControlKey) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if(FseEditingEnabled&&e.Control&&e.KeyCode is Keys.D1 or Keys.NumPad1 or Keys.D2 or Keys.NumPad2 or Keys.D3 or Keys.NumPad3)
        {SetFseTransformMode(e.KeyCode is Keys.D1 or Keys.NumPad1?FseGizmoMode.Move:e.KeyCode is Keys.D2 or Keys.NumPad2?FseGizmoMode.Vertex:FseGizmoMode.Face);e.Handled=true;e.SuppressKeyPress=true;return;}

        if(SmdEditingEnabled&&e.Control&&e.KeyCode is Keys.D1 or Keys.NumPad1 or Keys.D2 or Keys.NumPad2 or Keys.D3 or Keys.NumPad3)
        {SmdGizmoMode mode=e.KeyCode is Keys.D1 or Keys.NumPad1?SmdGizmoMode.Move:e.KeyCode is Keys.D2 or Keys.NumPad2?SmdGizmoMode.Rotate:SmdGizmoMode.Scale;if(SmdFaceEditMode)mode=SmdGizmoMode.Move;SmdTransformMode=mode;SmdTransformModeRequested?.Invoke(mode);smdOverlayDirty=true;Invalidate();e.Handled=true;e.SuppressKeyPress=true;return;}

        if(SmdEditingEnabled&&SmdFaceEditMode&&e.Control&&e.KeyCode==Keys.K){SeparateSmdFacesRequested?.Invoke();e.Handled=true;e.SuppressKeyPress=true;return;}

        if (EnemyModelPartPickingEnabled && e.Control && e.KeyCode is Keys.Z or Keys.Y)
        {
            if (e.KeyCode == Keys.Z) ExternalUndoRequested?.Invoke(); else ExternalRedoRequested?.Invoke();
            e.Handled = true; e.SuppressKeyPress = true; return;
        }

        if (e.Control && e.KeyCode == Keys.Z)
        {
            if(FseEditingEnabled)UndoFseEdit();else if (SmdEditingEnabled) UndoSmdEdit(); else if (!UndoEnemyEdit()) UndoAevEdit();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if(FseEditingEnabled&&e.Control&&e.KeyCode==Keys.Y){RedoFseEdit();e.Handled=true;e.SuppressKeyPress=true;return;}

        if(RtpVisible&&e.Control&&e.KeyCode==Keys.L){ConnectSelectedRtpNodes();e.Handled=true;e.SuppressKeyPress=true;return;}

        if (e.Control && e.KeyCode == Keys.D)
        {
            if(RtpVisible&&selectedRtpNode>=0&&e.Shift){AddChildToSelectedRtpNode(GetForward()*4f);}
            else if(RtpVisible&&selectedRtpNode>=0){DuplicateSelectedRtpNode(GetForward()*4f);}
            else if (SmdEditingEnabled) DuplicateSmdRequested?.Invoke();
            else DuplicateAevRequested?.Invoke();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Delete)
        {
            if(RtpVisible&&selectedRtpNode>=0){DeleteSelectedRtpNode();}
            else if(CamVisible&&selectedCamEntry>=0){DeleteCamRequested?.Invoke();}
            else if (SmdEditingEnabled && SmdFaceEditMode && DeleteSelectedSmdFaces()) { }
            else if (SmdEditingEnabled) DeleteSmdRequested?.Invoke();
            else DeleteAevRequested?.Invoke();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.F)
        {
            FitScene();
            e.Handled = true;
            return;
        }

        if (e.KeyCode is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E or Keys.ShiftKey or Keys.ControlKey)
        {
            movementKeys.Add(e.KeyCode);
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E or Keys.ShiftKey or Keys.ControlKey)
        {
            movementKeys.Remove(e.KeyCode);
            e.Handled = true;
        }
    }

    protected override void OnLostFocus(EventArgs e)
    {
        movementKeys.Clear();
        base.OnLostFocus(e);
    }

}
