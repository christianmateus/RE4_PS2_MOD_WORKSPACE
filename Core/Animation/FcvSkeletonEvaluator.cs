using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

public sealed class FcvSkeletonPose
{
    public Vector3[] LocalPositions { get; init; } = Array.Empty<Vector3>();
    public Quaternion[] LocalRotations { get; init; } = Array.Empty<Quaternion>();
    public Vector3[] WorldPositions { get; init; } = Array.Empty<Vector3>();
    public Quaternion[] WorldRotations { get; init; } = Array.Empty<Quaternion>();
    // Portion of the FCV root translation that belongs to the clip's placement rather than to
    // the visible pose. Idle stabilization anchors this at frame zero so cyclic hip sway remains.
    public Vector3 RootMotionToRemove { get; init; }
}

public static class FcvSkeletonEvaluator
{
    public static FcvSkeletonPose Evaluate(Ps2BinSkeleton skeleton, FcvAnimation? animation, float frame, bool stabilizePlantedFeet = false)
        => Evaluate(skeleton,animation,frame,stabilizePlantedFeet,true);

    private static FcvSkeletonPose Evaluate(Ps2BinSkeleton skeleton, FcvAnimation? animation, float frame, bool stabilizePlantedFeet, bool applyTrackedFootIk)
    {
        int n=skeleton.Bones.Count; var wp=new Vector3[n]; var wr=new Quaternion[n];
        var localPos=new Vector3[n]; var localRot=new Quaternion[n];
        var absoluteWorldRotation=new bool[n];
        for(int i=0;i<n;i++){localPos[i]=skeleton.Bones[i].LocalPosition; localRot[i]=Quaternion.Identity;}
        if(animation!=null)
        {
            foreach(var t in animation.Tracks)
            {
                int bi=FindBoneIndex(skeleton,t.NodeId); if(bi<0) continue;
                int enc=t.DataType>>4;
                // Conservative FCV playback while the PS2 transform semantics are being validated:
                // - 0x02 is the only rotation type the reference MaxScript actually imports.
                // - encodings 0x00/0x10/0x50/0x60 are the rotation encodings handled by that importer.
                // - 0x10 uses the same angle encodings, but is resolved as a world-space rotation.
                if(t.Type==0x02 && IsSupportedRotationEncoding(enc))
                {
                    // FCV rotation tracks use several storage precisions. The old evaluator only
                    // accepted float/int16 encodings (00/10/50/60), so compressed 8-bit tracks
                    // such as A0 were silently ignored. em12 uses A0 rotations extensively on
                    // the lower-body chain (hips/legs/feet), which made those bones inherit the
                    // parent's rotation and visually "follow the hip".
                    float rx=RotationValue(EvalOrZero(t.X,frame,enc),enc), ry=RotationValue(EvalOrZero(t.Y,frame,enc),enc), rz=RotationValue(EvalOrZero(t.Z,frame,enc),enc);
                    localRot[bi]=UsesEm12HumanoidRotationProfile(animation) && t.NodeId is >=0x01 and <=0x10
                        ? CreateUpperBodyRotation(t.NodeId,rx,ry,rz)
                        : Quaternion.CreateFromYawPitchRoll(ry,rx,rz);
                }
                else if(t.Type==0x10 && IsSupportedRotationEncoding(enc))
                {
                    // Type 0x10 is the absolute/world-space counterpart of the relative FK
                    // rotation (0x02). em12_001 uses it for both thighs (nodes 0x12/0x16).
                    // Treating these bones as unanimated makes them inherit the complete pelvis
                    // rotation while the calf/foot tracks continue moving below them, which is
                    // the characteristic broken lower-body motion seen in the Visual Editor.
                    float rx=RotationValue(EvalOrZero(t.X,frame,enc),enc), ry=RotationValue(EvalOrZero(t.Y,frame,enc),enc), rz=RotationValue(EvalOrZero(t.Z,frame,enc),enc);
                    localRot[bi]=Quaternion.CreateFromYawPitchRoll(ry,rx,rz);
                    absoluteWorldRotation[bi]=true;
                }
                else if(t.Type==0x01 && skeleton.Bones[bi].ParentIndex<0 && enc==0x0)
                {
                    // Root Position is authored as actor displacement from the clip origin.
                    // It normally starts at zero; the viewport may remove it when previewing an
                    // animation in place, but the evaluated pose retains the actual motion.
                    Vector3 value=new((float)EvalOrZero(t.X,frame,enc),(float)EvalOrZero(t.Y,frame,enc),(float)EvalOrZero(t.Z,frame,enc));
                    localPos[bi]=skeleton.Bones[bi].LocalPosition+value;
                }
                else if(t.Type==0x04 && skeleton.Bones[bi].ParentIndex<0 && (enc==0x0 || enc==0x1 || enc==0x2))
                {
                    // Translation/IK handles store an absolute model-space target. Root tracks in
                    // em22 and em36 contain their actual standing height, close to the BIN bind
                    // height; adding the value as a delta doubles that height and breaks leg IK.
                    Vector3 value = new((float)EvalOrZero(t.X,frame,enc),(float)EvalOrZero(t.Y,frame,enc),(float)EvalOrZero(t.Z,frame,enc));
                    localPos[bi]=value;
                }
                else if(t.Type==0x40 && skeleton.Bones[bi].ParentIndex<0 && IsSupportedRotationEncoding(enc))
                {
                    float rx=RotationValue(EvalOrZero(t.X,frame,enc),enc),ry=RotationValue(EvalOrZero(t.Y,frame,enc),enc),rz=RotationValue(EvalOrZero(t.Z,frame,enc),enc);
                    localRot[bi]=Quaternion.CreateFromYawPitchRoll(ry,rx,rz);
                }
                // 0x08 scale, 0x20 toe IK and 0x80/0xA0
                // special tracks remain conservative until their exact semantics are validated.
            }
        }
        var resolveState=new byte[n];
        for(int i=0;i<n;i++) ResolveWorld(i,skeleton,localPos,localRot,absoluteWorldRotation,wp,wr,resolveState);
        Vector3 authoredRootMotion=animation==null?Vector3.Zero:GetAuthoredRootMotion(skeleton,animation,frame);
        if(animation!=null) ApplyTrackedHandIkChains(skeleton,animation,frame,authoredRootMotion,wp,wr);
        if(!stabilizePlantedFeet && animation!=null)
        {
            if(applyTrackedFootIk)ApplyTrackedFootIkChains(skeleton,animation,frame,authoredRootMotion,wp,wr);
            if(IsAnimationNumber(animation,2)) PreventRightHandHeadPenetration(skeleton,wp,wr);
        }
        FcvSkeletonPose? plantedReference = stabilizePlantedFeet && animation!=null
            ? (MathF.Abs(frame)<0.0001f
                ? new FcvSkeletonPose { WorldPositions=(Vector3[])wp.Clone(), WorldRotations=(Quaternion[])wr.Clone() }
                : Evaluate(skeleton,animation,0f,false,false))
            : null;
        if(stabilizePlantedFeet && animation!=null)
            StabilizePlantedLeg(skeleton, 0x12, 0x13, 0x14, wp, wr, plantedReference!);
        if(stabilizePlantedFeet && animation!=null)
            StabilizePlantedLeg(skeleton, 0x16, 0x17, 0x18, wp, wr, plantedReference!);
        // em12_901 is an authored seated-pose diagnostic: its root translation is placement
        // inside the pose, not locomotion to strip before drawing.
        Vector3 rootMotionToRemove=animation!=null && IsAnimationNumber(animation,901)
            ? Vector3.Zero
            : stabilizePlantedFeet && animation!=null
                ? GetRootDelta(skeleton,plantedReference!.WorldPositions)
                : authoredRootMotion;
        return new FcvSkeletonPose{LocalPositions=localPos,LocalRotations=localRot,WorldPositions=wp,WorldRotations=wr,RootMotionToRemove=rootMotionToRemove};
    }

    private static void ApplyTrackedHandIkChains(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame,Vector3 rootMotion,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        // Weapon FCVs use an absolute target on the hand (type 04) and an A0 IK controller on
        // the upper arm. Leon has an extra wrist joint, so this is a three-link hierarchy even
        // though the actual bend is still the usual shoulder/elbow two-bone solve.
        foreach(FcvTrack targetTrack in animation.Tracks.Where(t=>t.Type==0x04&&skeleton.FirstIndexById.ContainsKey(t.NodeId)))
        {
            int hand=FindBoneIndex(skeleton,targetTrack.NodeId);if(hand<0)continue;
            int wrist=skeleton.Bones[hand].ParentIndex;if(wrist<0)continue;
            int elbow=skeleton.Bones[wrist].ParentIndex;if(elbow<0)continue;
            int upper=skeleton.Bones[elbow].ParentIndex;if(upper<0)continue;
            byte upperId=skeleton.Bones[upper].Id;
            FcvTrack? controller=animation.Tracks.FirstOrDefault(t=>t.NodeId==upperId&&t.Type==0xA0);
            if(controller!=null)ApplyTrackedHandIk(skeleton,frame,upper,elbow,hand,targetTrack,controller,rootMotion,worldPositions,worldRotations);
        }
    }

    private static void ApplyTrackedHandIk(Ps2BinSkeleton skeleton,float frame,int upper,int elbow,int hand,FcvTrack targetTrack,FcvTrack controllerTrack,Vector3 rootMotion,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        if((targetTrack.DataType>>4) is not (0x0 or 0x1 or 0x2))return;
        int wrist=skeleton.Bones[hand].ParentIndex;if(wrist<0||skeleton.Bones[wrist].ParentIndex!=elbow)return;
        Vector3 shoulder=worldPositions[upper];
        Vector3 target=SampleTrackRaw(targetTrack,frame)+rootMotion;
        Vector3 toTarget=target-shoulder;float distance=toTarget.Length();
        float upperLength=skeleton.Bones[elbow].LocalPosition.Length();
        float lowerLength=(GetBindWorldPosition(skeleton,hand)-GetBindWorldPosition(skeleton,elbow)).Length();
        if(!float.IsFinite(distance)||distance<0.0001f||upperLength<0.0001f||lowerLength<0.0001f)return;
        Vector3 direction=toTarget/distance;float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);target=shoulder+direction*reachable;

        // A0 is an IK-control track, not an ordinary Euler rotation. Its low nibble only
        // identifies the pole compass direction. More importantly, the accompanying 0x02 FK
        // tracks have already authored the actual bend side for this frame. Project that
        // animated elbow onto the shoulder/target plane so the IK retains Leon's intended arm
        // silhouette instead of folding an arm over his chest.
        Vector3 pole=worldPositions[elbow]-shoulder;
        pole-=direction*Vector3.Dot(pole,direction);
        if(pole.LengthSquared()<0.0001f)
        {
            Vector3 localPoleAxis=(controllerTrack.DataType&0x0F) switch
            {
                0x0 => -Vector3.UnitX, // left
                0x4 => Vector3.UnitZ,  // forward
                0x5 => -Vector3.UnitZ, // backward
                0x6 => Vector3.UnitX,  // right
                _ => Vector3.UnitZ
            };
            int parent=skeleton.Bones[upper].ParentIndex;
            Quaternion parentRotation=parent>=0?worldRotations[parent]:Quaternion.Identity;
            pole=Vector3.Transform(localPoleAxis,parentRotation);
            pole-=direction*Vector3.Dot(pole,direction);
        }
        if(pole.LengthSquared()<0.0001f)return;pole=Vector3.Normalize(pole);

        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 elbowTarget=shoulder+direction*along+pole*bend;
        Quaternion[] before=(Quaternion[])worldRotations.Clone();Vector3 oldElbow=worldPositions[elbow],oldHand=worldPositions[hand];
        Quaternion upperCorrection=RotationBetween(Vector3.Transform(skeleton.Bones[elbow].LocalPosition,worldRotations[upper]),elbowTarget-shoulder);
        worldRotations[upper]=Quaternion.Normalize(upperCorrection*worldRotations[upper]);worldRotations[elbow]=Quaternion.Normalize(upperCorrection*worldRotations[elbow]);worldPositions[elbow]=elbowTarget;
        Vector3 oldLower=Vector3.Transform(oldHand-oldElbow,upperCorrection);
        Quaternion lowerCorrection=RotationBetween(oldLower,target-elbowTarget);
        worldRotations[elbow]=Quaternion.Normalize(lowerCorrection*worldRotations[elbow]);
        worldRotations[wrist]=Quaternion.Normalize(lowerCorrection*upperCorrection*before[wrist]);
        // Hand R (0x0A) carries the handgun's authored high-ready roll. Earlier it looked wrong
        // because the weapon itself was mistakenly attached to Hand L; discarding this rotation
        // then left the correctly attached pistol horizontal. Reapply the animated local grip on
        // the dominant hand. The supporting Hand L remains aligned with its solved wrist because
        // its terminal IK rotation rolls the loose palm down instead of around the weapon.
        if(skeleton.Bones[hand].Id==0x0A)
        {
            Quaternion animatedGripLocal=Quaternion.Normalize(Quaternion.Inverse(before[wrist])*before[hand]);
            worldRotations[hand]=Quaternion.Normalize(worldRotations[wrist]*animatedGripLocal);
        }
        else worldRotations[hand]=worldRotations[wrist];
        Vector3 lowerDirection=Vector3.Normalize(target-elbowTarget);
        worldPositions[wrist]=elbowTarget+lowerDirection*skeleton.Bones[wrist].LocalPosition.Length();
        worldPositions[hand]=target;
        UpdateDescendantsExcept(skeleton,upper,elbow,worldPositions,worldRotations,before);
        UpdateDescendantsExcept(skeleton,elbow,wrist,worldPositions,worldRotations,before);
        UpdateDescendantsExcept(skeleton,wrist,hand,worldPositions,worldRotations,before);
        UpdateDescendants(skeleton,hand,worldPositions,worldRotations,before);
    }

    private static void StabilizePlantedLeg(Ps2BinSkeleton skeleton, byte thighId, byte calfId, byte footId, Vector3[] worldPositions, Quaternion[] worldRotations, FcvSkeletonPose reference)
    {
        int thigh=FindBoneIndex(skeleton,thighId), calf=FindBoneIndex(skeleton,calfId), foot=FindBoneIndex(skeleton,footId);
        if(thigh<0 || calf<0 || foot<0 || skeleton.Bones[calf].ParentIndex!=thigh || skeleton.Bones[foot].ParentIndex!=calf) return;
        // em12's idle contains foot IK nodes that the game uses to preserve ground contact.
        // Their packed IK-pull semantics are not fully documented, so preserve the actual FCV
        // stance at frame zero (rather than the narrower neutral BIN stance) and solve the same
        // two-joint chain analytically. Root motion is added back here because skinning removes
        // it later; the final on-screen target therefore remains fixed at the reference stance.
        Vector3 referenceRootDelta=GetRootDelta(skeleton,reference.WorldPositions);
        Vector3 referenceTarget=reference.WorldPositions[foot]-referenceRootDelta;
        Vector3 bindTarget=GetBindWorldPosition(skeleton,foot);
        float upperLength=skeleton.Bones[calf].LocalPosition.Length();
        float lowerLength=skeleton.Bones[foot].LocalPosition.Length();
        float ankleLift=(upperLength+lowerLength)*0.06f;
        // FCV frame zero provides the useful lateral stance, but its unresolved IK/FK depth is
        // what put one em12 foot far in front of the other. Keep only lateral X from FCV, use the
        // neutral BIN depth, and lift the ankle slightly so the chain is not at maximum reach.
        // That produces the subtle idle knee bend visible in-game without moving the shoe floor.
        // The neutral skeleton still contains a small fore/aft offset between the ankles. The
        // game keeps a hint of that stagger, but not the exaggerated separation produced when
        // the unresolved FCV depth is used verbatim. Compress the bind-pose depth around the
        // midpoint of both feet while retaining a quarter of the original difference.
        byte otherFootId=footId==0x14 ? (byte)0x18 : (byte)0x14;
        int otherFoot=FindBoneIndex(skeleton,otherFootId);
        float targetLateral=referenceTarget.X;
        float targetDepth=bindTarget.Z;
        if(otherFoot>=0)
        {
            Vector3 otherReferenceTarget=reference.WorldPositions[otherFoot]-referenceRootDelta;
            float lateralMidpoint=(referenceTarget.X+otherReferenceTarget.X)*0.5f;
            targetLateral=lateralMidpoint+(referenceTarget.X-lateralMidpoint)*0.75f;
            float otherDepth=GetBindWorldPosition(skeleton,otherFoot).Z;
            float depthMidpoint=(bindTarget.Z+otherDepth)*0.5f;
            targetDepth=depthMidpoint+(bindTarget.Z-depthMidpoint)*0.05f;
        }
        // Keep the ankle anchored to the clip's frame-zero root while the current root is allowed
        // to sway. The changing hip-to-ankle distance is what drives the visible idle leg motion.
        Vector3 target=new Vector3(targetLateral,bindTarget.Y+ankleLift,targetDepth)+referenceRootDelta;
        Vector3 hip=worldPositions[thigh];
        Vector3 toTarget=target-hip;
        float distance=toTarget.Length();
        if(!float.IsFinite(distance) || distance<0.0001f || upperLength<0.0001f || lowerLength<0.0001f) return;

        Vector3 direction=toTarget/distance;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        // The FCV script documents the low DataType nibble as an IK pole/compass flag. Until
        // every flag is mapped, use the model's anatomical forward direction: the em12 toe/end
        // bones point along +Z in the BIN. Deriving the pole from the already imperfect FK pose
        // made the right knee change plane or lock straight.
        Vector3 pole=Vector3.UnitZ-direction*Vector3.Dot(Vector3.UnitZ,direction);
        if(pole.LengthSquared()<0.0001f)
        {
            Vector3 fallback=MathF.Abs(Vector3.Dot(direction,Vector3.UnitZ))<0.95f ? Vector3.UnitZ : Vector3.UnitX;
            pole=Vector3.Cross(fallback,direction);
        }
        pole=Vector3.Normalize(pole);

        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 knee=hip+direction*along+pole*bend;

        Vector3 currentUpper=Vector3.Transform(skeleton.Bones[calf].LocalPosition,worldRotations[thigh]);
        Quaternion upperCorrection=RotationBetween(currentUpper,knee-hip);
        worldRotations[thigh]=Quaternion.Normalize(upperCorrection*worldRotations[thigh]);
        worldRotations[calf]=Quaternion.Normalize(upperCorrection*worldRotations[calf]);
        worldPositions[calf]=knee;

        Vector3 currentLower=Vector3.Transform(skeleton.Bones[foot].LocalPosition,worldRotations[calf]);
        Quaternion lowerCorrection=RotationBetween(currentLower,target-knee);
        worldRotations[calf]=Quaternion.Normalize(lowerCorrection*worldRotations[calf]);
        // Idle feet remain oriented like frame zero. Continuing to apply every FCV ankle
        // rotation after positional IK makes the shoe visibly rock and slide around its pivot.
        worldRotations[foot]=reference.WorldRotations[foot];
        worldPositions[foot]=target;

        // Preserve the FCV foot orientation, but move any toe/end nodes with the corrected foot.
        UpdateDescendants(skeleton,foot,worldPositions,worldRotations,reference.WorldRotations);
    }

    private static void ApplyTrackedFootIkChains(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame,Vector3 rootMotion,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        // Discover two-bone IK chains from the data itself: an absolute translation target on
        // the end joint and an IK/IK-toe controller on its grandparent. This covers humanoid
        // legs without relying on fixed joint IDs or enemy-specific profiles.
        foreach(FcvTrack targetTrack in animation.Tracks.Where(t=>t.Type==0x04&&skeleton.FirstIndexById.ContainsKey(t.NodeId)))
        {
            int foot=FindBoneIndex(skeleton,targetTrack.NodeId);if(foot<0)continue;
            int calf=skeleton.Bones[foot].ParentIndex;if(calf<0)continue;
            int thigh=skeleton.Bones[calf].ParentIndex;if(thigh<0)continue;
            byte thighId=skeleton.Bones[thigh].Id;
            FcvTrack? controller=animation.Tracks.FirstOrDefault(t=>t.NodeId==thighId&&(t.Type==0x10||t.Type==0x20));
            if(controller!=null)ApplyTrackedFootIk(skeleton,animation,frame,thigh,calf,foot,targetTrack,controller,rootMotion,worldPositions,worldRotations);
        }
    }

    private static void ApplyTrackedFootIk(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame,int thigh,int calf,int foot,FcvTrack targetTrack,FcvTrack controllerTrack,Vector3 rootMotion,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        if(thigh<0 || calf<0 || foot<0 || skeleton.Bones[calf].ParentIndex!=thigh || skeleton.Bones[foot].ParentIndex!=calf) return;
        if((targetTrack.DataType>>4) is not (0x0 or 0x1 or 0x2))return;

        // Type 0x04 on the ankle is an absolute model-space IK target. The evaluator stores root
        // translation as a delta over the BIN root, so add that delta here; the viewport removes
        // it again when anchoring the enemy to its ESL position. The visible ankle consequently
        // lands on the FCV coordinates while the hip remains in the same coordinate space.
        Vector3 rawTarget=SampleTrackRaw(targetTrack,frame);
        Vector3 target=rawTarget+rootMotion;
        Vector3 hip=worldPositions[thigh];
        Vector3 toTarget=target-hip;
        float distance=toTarget.Length();
        float upperLength=skeleton.Bones[calf].LocalPosition.Length();
        float lowerLength=skeleton.Bones[foot].LocalPosition.Length();
        if(!float.IsFinite(distance) || distance<0.0001f || upperLength<0.0001f || lowerLength<0.0001f) return;

        Vector3 direction=toTarget/distance;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        target=hip+direction*reachable;

        // Type 0x10 is the animated IK-parent orientation. The low DataType nibble selects one
        // of its six signed local axes as the pole target. Transform that exact axis by the
        // animated IK parent: using the calf direction instead effectively asked the knee to
        // follow the upper leg itself, which is nearly invisible in walks but folds/crosses the
        // legs during large motions such as em12_027's vault.
        Vector3 localPoleAxis=(controllerTrack.DataType&0x0F) switch
        {
            0x0 => Vector3.UnitY,
            0x1 => -Vector3.UnitY,
            0x2 => Vector3.UnitX,
            0x3 => -Vector3.UnitX,
            0x4 => Vector3.UnitZ,
            0x5 => -Vector3.UnitZ,
            _ => Vector3.UnitZ
        };

        // Keep the knee on the anatomical side authored by the BIN skeleton. Deriving the pole
        // from the current FK calf is unstable: whenever the ankle passes the hip during a turn,
        // that projection can cross zero and select the mirrored two-bone IK solution. Build the
        // reference bend in bind/model space instead, carry it with the thigh's current parent
        // (normally the pelvis or an auxiliary hip bone), and only then project it onto the
        // current hip-to-ankle plane. This is deterministic when scrubbing and works for chains
        // with helper bones without relying on enemy or joint IDs.
        Vector3 bindHip=GetBindWorldPosition(skeleton,thigh);
        Vector3 bindKnee=GetBindWorldPosition(skeleton,calf);
        Vector3 bindAnkle=GetBindWorldPosition(skeleton,foot);
        Vector3 bindDirection=bindAnkle-bindHip;
        Vector3 bindPole=bindKnee-bindHip;
        if(bindDirection.LengthSquared()>0.0001f)
        {
            bindDirection=Vector3.Normalize(bindDirection);
            bindPole-=bindDirection*Vector3.Dot(bindPole,bindDirection);
        }

        int poleParent=skeleton.Bones[thigh].ParentIndex;
        Quaternion poleFrame=poleParent>=0?worldRotations[poleParent]:Quaternion.Identity;
        Vector3 pole=Vector3.Transform(bindPole,poleFrame);
        pole-=direction*Vector3.Dot(pole,direction);
        if(pole.LengthSquared()<0.0001f)
        {
            // A perfectly straight bind chain has no anatomical bend plane. In that case the
            // FCV controller's signed compass axis supplies the format-defined pole direction.
            pole=Vector3.Transform(localPoleAxis,worldRotations[thigh]);
            pole-=direction*Vector3.Dot(pole,direction);
        }
        if(pole.LengthSquared()<0.0001f) pole=Vector3.UnitX-direction*Vector3.Dot(Vector3.UnitX,direction);
        if(pole.LengthSquared()<0.0001f) return;
        pole=Vector3.Normalize(pole);

        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 knee=hip+direction*along+pole*bend;
        Quaternion[] rotationsBeforeIk=(Quaternion[])worldRotations.Clone();
        Quaternion animatedFootRotation=worldRotations[foot];

        Vector3 currentUpper=Vector3.Transform(skeleton.Bones[calf].LocalPosition,worldRotations[thigh]);
        Quaternion upperCorrection=RotationBetween(currentUpper,knee-hip);
        worldRotations[thigh]=Quaternion.Normalize(upperCorrection*worldRotations[thigh]);
        worldRotations[calf]=Quaternion.Normalize(upperCorrection*worldRotations[calf]);
        worldPositions[calf]=knee;

        Vector3 currentLower=Vector3.Transform(skeleton.Bones[foot].LocalPosition,worldRotations[calf]);
        Quaternion lowerCorrection=RotationBetween(currentLower,target-knee);
        worldRotations[calf]=Quaternion.Normalize(lowerCorrection*worldRotations[calf]);
        worldRotations[foot]=animatedFootRotation;
        worldPositions[foot]=target;
        UpdateDescendantsExcept(skeleton,thigh,calf,worldPositions,worldRotations,rotationsBeforeIk);
        UpdateDescendantsExcept(skeleton,calf,foot,worldPositions,worldRotations,rotationsBeforeIk);
        UpdateDescendants(skeleton,foot,worldPositions,worldRotations,rotationsBeforeIk);
    }

    private static void UpdateDescendantsExcept(Ps2BinSkeleton skeleton,int parent,int excludedChild,Vector3[] worldPositions,Quaternion[] worldRotations,Quaternion[] referenceRotations)
    {
        for(int i=0;i<skeleton.Bones.Count;i++)
        {
            if(i==excludedChild||skeleton.Bones[i].ParentIndex!=parent)continue;
            Quaternion localRotation=Quaternion.Normalize(Quaternion.Inverse(referenceRotations[parent])*referenceRotations[i]);
            worldPositions[i]=worldPositions[parent]+Vector3.Transform(skeleton.Bones[i].LocalPosition,worldRotations[parent]);
            worldRotations[i]=Quaternion.Normalize(worldRotations[parent]*localRotation);
            UpdateDescendants(skeleton,i,worldPositions,worldRotations,referenceRotations);
        }
    }

    private static Vector3 GetBindWorldPosition(Ps2BinSkeleton skeleton,int index)
    {
        Vector3 result=Vector3.Zero;
        int current=index, guard=0;
        while(current>=0 && current<skeleton.Bones.Count && guard++<skeleton.Bones.Count)
        {
            result+=skeleton.Bones[current].LocalPosition;
            current=skeleton.Bones[current].ParentIndex;
        }
        return result;
    }

    private static Vector3 GetRootDelta(Ps2BinSkeleton skeleton,Vector3[] worldPositions)
    {
        for(int i=0;i<skeleton.Bones.Count;i++)
            if(skeleton.Bones[i].ParentIndex<0)
                return worldPositions[i]-GetBindWorldPosition(skeleton,i);
        return Vector3.Zero;
    }

    private static Vector3 GetAuthoredRootMotion(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame)
    {
        foreach(FcvTrack track in animation.Tracks)
        {
            if(track.Type!=0x01)continue;
            int index=FindBoneIndex(skeleton,track.NodeId);
            if(index>=0&&skeleton.Bones[index].ParentIndex<0)return SampleTrackRaw(track,frame);
        }
        return Vector3.Zero;
    }

    private static Quaternion RotationBetween(Vector3 from,Vector3 to)
    {
        if(from.LengthSquared()<0.000001f || to.LengthSquared()<0.000001f) return Quaternion.Identity;
        from=Vector3.Normalize(from); to=Vector3.Normalize(to);
        float dot=Math.Clamp(Vector3.Dot(from,to),-1f,1f);
        if(dot>0.999999f) return Quaternion.Identity;
        if(dot<-0.999999f)
        {
            Vector3 axis=Vector3.Cross(from,MathF.Abs(from.X)<0.9f ? Vector3.UnitX : Vector3.UnitY);
            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis),MathF.PI);
        }
        Vector3 cross=Vector3.Cross(from,to);
        return Quaternion.Normalize(new Quaternion(cross,1f+dot));
    }

    private static Quaternion CreateUpperBodyRotation(byte nodeId,float x,float y,float z)
    {
        // The two arm chains are mirrored in the em12 BIN. Applying one Euler composition to
        // both sides puts FCV 002's left hand across the chest. Z-X-Y fixed that distance but
        // made the right hand pass through the center of the head only during a short portion of
        // FCV 002. Keep its otherwise correct Z-X-Y motion and resolve that local penetration
        // after posing. The mirrored left chain (0B..10) uses Z-Y-X.
        Quaternion qz=Quaternion.CreateFromAxisAngle(Vector3.UnitZ,z);
        Quaternion qx=Quaternion.CreateFromAxisAngle(Vector3.UnitX,x);
        Quaternion qy=Quaternion.CreateFromAxisAngle(Vector3.UnitY,y);
        if(nodeId is >=0x0B and <=0x10) return Quaternion.Normalize(qz*qy*qx);
        return Quaternion.Normalize(qz*qx*qy);
    }

    private static bool IsAnimationNumber(FcvAnimation animation,int number)
    {
        string stem=Path.GetFileNameWithoutExtension(animation.FilePath);
        int separator=stem.LastIndexOf('_');
        return separator>=0 && int.TryParse(stem[(separator+1)..],out int parsed) && parsed==number;
    }

    private static bool UsesEm12HumanoidRotationProfile(FcvAnimation animation)
    {
        string name=Path.GetFileName(animation.FilePath);
        return name.StartsWith("em12_",StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("em12.dat#",StringComparison.OrdinalIgnoreCase);
    }


    private static void PreventRightHandHeadPenetration(Ps2BinSkeleton skeleton,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        int head=FindBoneIndex(skeleton,0x04),upper=FindBoneIndex(skeleton,0x07),elbow=FindBoneIndex(skeleton,0x08);
        int wrist=FindBoneIndex(skeleton,0x09),hand=FindBoneIndex(skeleton,0x0A);
        if(head<0 || upper<0 || elbow<0 || wrist<0 || hand<0 || skeleton.Bones[elbow].ParentIndex!=upper || skeleton.Bones[wrist].ParentIndex!=elbow || skeleton.Bones[hand].ParentIndex!=wrist) return;

        // Bone origins alone underestimate the occupied volume: the hand mesh extends well past
        // node 0x0A and could still clip the face with its pivot 180 units away. 220 keeps the
        // rendered hand outside the head while remaining a local, continuous correction.
        const float headClearance=220f;
        Vector3 handFromHead=worldPositions[hand]-worldPositions[head];
        float clearance=handFromHead.Length();
        if(!float.IsFinite(clearance) || clearance>=headClearance) return;
        Vector3 outward=clearance>0.001f ? handFromHead/clearance : Vector3.Normalize(new Vector3(-1f,0.2f,1f));
        Vector3 desiredHand=worldPositions[head]+outward*headClearance;

        Vector3 shoulder=worldPositions[upper];
        Vector3 oldElbow=worldPositions[elbow];
        Vector3 oldHand=worldPositions[hand];
        Vector3 toTarget=desiredHand-shoulder;
        float distance=toTarget.Length();
        float upperLength=skeleton.Bones[elbow].LocalPosition.Length();
        float lowerLength=(skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition).Length();
        if(distance<0.001f || upperLength<0.001f || lowerLength<0.001f) return;
        Vector3 direction=toTarget/distance;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        desiredHand=shoulder+direction*reachable;

        // Retain the FCV elbow plane so the correction merely moves the hand out of the head
        // instead of replacing the authored arm gesture.
        Vector3 pole=oldElbow-shoulder-direction*Vector3.Dot(oldElbow-shoulder,direction);
        if(pole.LengthSquared()<0.0001f) pole=Vector3.UnitY-direction*Vector3.Dot(Vector3.UnitY,direction);
        if(pole.LengthSquared()<0.0001f) return;
        pole=Vector3.Normalize(pole);
        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 desiredElbow=shoulder+direction*along+pole*bend;

        Quaternion[] rotationsBeforeIk=(Quaternion[])worldRotations.Clone();
        Vector3 currentUpper=Vector3.Transform(skeleton.Bones[elbow].LocalPosition,worldRotations[upper]);
        Quaternion upperCorrection=RotationBetween(currentUpper,desiredElbow-shoulder);
        worldRotations[upper]=Quaternion.Normalize(upperCorrection*worldRotations[upper]);
        worldRotations[elbow]=Quaternion.Normalize(upperCorrection*worldRotations[elbow]);
        worldPositions[elbow]=desiredElbow;

        Vector3 currentLower=Vector3.Transform(oldHand-oldElbow,upperCorrection);
        Quaternion lowerCorrection=RotationBetween(currentLower,desiredHand-desiredElbow);
        worldRotations[elbow]=Quaternion.Normalize(lowerCorrection*worldRotations[elbow]);
        UpdateDescendants(skeleton,elbow,worldPositions,worldRotations,rotationsBeforeIk);
    }

    private static void UpdateDescendants(Ps2BinSkeleton skeleton,int parent,Vector3[] worldPositions,Quaternion[] worldRotations,Quaternion[] referenceRotations)
    {
        for(int i=0;i<skeleton.Bones.Count;i++)
        {
            if(skeleton.Bones[i].ParentIndex!=parent) continue;
            // A planted foot is a rigid chain in this idle. Use its frame-zero local rotation
            // instead of the current animated toe rotation, which made the shoe tip twitch even
            // though the ankle itself was locked.
            Quaternion referenceParent=referenceRotations[parent];
            Quaternion referenceChild=referenceRotations[i];
            Quaternion localRotation=Quaternion.Normalize(Quaternion.Inverse(referenceParent)*referenceChild);
            worldPositions[i]=worldPositions[parent]+Vector3.Transform(skeleton.Bones[i].LocalPosition,worldRotations[parent]);
            worldRotations[i]=Quaternion.Normalize(worldRotations[parent]*localRotation);
            UpdateDescendants(skeleton,i,worldPositions,worldRotations,referenceRotations);
        }
    }

    private static int FindBoneIndex(Ps2BinSkeleton s, byte id){ if(s.FirstIndexById.TryGetValue(id,out int i)) return i; return -1; }
    private static void ResolveWorld(int i, Ps2BinSkeleton s, Vector3[] lp, Quaternion[] lr, bool[] absoluteWorldRotation, Vector3[] wp, Quaternion[] wr, byte[] state)
    {
        if(state[i]==2)return; if(state[i]==1){wp[i]=lp[i];wr[i]=lr[i];state[i]=2;return;} state[i]=1;
        int p=s.Bones[i].ParentIndex;
        if(p<0){wp[i]=lp[i];wr[i]=lr[i];}
        else
        {
            ResolveWorld(p,s,lp,lr,absoluteWorldRotation,wp,wr,state);
            // Absolute rotation changes the bone orientation, not its joint origin: the joint
            // still follows the parent transform, while its children inherit the absolute pose.
            wr[i]=absoluteWorldRotation[i] ? Quaternion.Normalize(lr[i]) : Quaternion.Normalize(wr[p]*lr[i]);
            wp[i]=wp[p]+Vector3.Transform(lp[i],wr[p]);
        }
        state[i]=2;
    }
    private static bool IsSupportedRotationEncoding(int encoding)
        => encoding is 0x0 or 0x1 or 0x4 or 0x5 or 0x6 or 0x8 or 0x9 or 0xA;

    private static float RotationValue(double value,int encoding)
    {
        // 00/10 store the angle directly as float radians.
        // 50/60 store the value in signed 16-bit normalized angular space.
        // 80/90/A0 use the same angular range quantized to signed 8-bit.
        // Supporting A0 is the important lower-body fix for em12 FCV 001.
        return encoding switch
        {
            0x0 or 0x1 => (float)value,
            0x4 or 0x5 or 0x6 => (float)(value / 32767.0 * Math.PI),
            0x8 or 0x9 or 0xA => (float)(value / 127.0 * Math.PI),
            _ => 0f
        };
    }
    public static double SampleAxis(FcvAxis axis, float frame, int encoding=0) => axis.Keys.Count == 0 ? 0.0 : Eval(axis, frame, encoding);
    public static Vector3 SampleTrackRaw(FcvTrack track, float frame)
    {
        int encoding=track.DataType>>4;
        return new((float)SampleAxis(track.X,frame,encoding),(float)SampleAxis(track.Y,frame,encoding),(float)SampleAxis(track.Z,frame,encoding));
    }

    private static float EvalOr(FcvAxis a,float frame,float fallback,int encoding=0) => a.Keys.Count==0 ? fallback : (float)Eval(a,frame,encoding);
    private static double EvalOrZero(FcvAxis a,float frame,int encoding) => a.Keys.Count==0 ? 0.0 : Eval(a,frame,encoding);
    private static double Eval(FcvAxis a,float frame,int encoding)
    {
        if(a.Keys.Count==0)return 0; if(a.Keys.Count==1)return a.Keys[0].Value;
        if(frame<=a.Keys[0].Frame)return a.Keys[0].Value; if(frame>=a.Keys[^1].Frame)return a.Keys[^1].Value;
        int hi=1; while(hi<a.Keys.Count && a.Keys[hi].Frame<frame)hi++; var k0=a.Keys[hi-1]; var k1=a.Keys[hi];
        float span=Math.Max(1,k1.Frame-k0.Frame); float u=(frame-k0.Frame)/span;
        // FCV stores Bezier/Hermite handles alongside each value. Linear interpolation made the
        // walk visibly change velocity at every sparse key, which looked like skipped frames.
        // The handles are stored in the same value space as the channel and describe the whole
        // segment, so they are not multiplied by the key-frame distance.
        double outTangent=DecodeTangent(k0.TangentOut,encoding,true);
        double inTangent=DecodeTangent(k1.TangentIn,encoding,false);
        double u2=u*u,u3=u2*u;
        double h00=2*u3-3*u2+1;
        double h10=u3-2*u2+u;
        double h01=-2*u3+3*u2;
        double h11=u3-u2;
        return h00*k0.Value+h10*outTangent+h01*k1.Value+h11*inTangent;
    }

    private static double DecodeTangent(double value,int encoding,bool outgoing)
    {
        // Encoding 1 stores float values but packs each tangent into a signed normalized int16.
        // Treating that packed integer as a direct Hermite slope made subtle weapon idles jump
        // thousands of model units between keys (for example wep02 FCV 07's root-height track).
        if(encoding==0x1)return value/32767.0;
        // Encoding 0x5 stores the outgoing 16-bit handle as unsigned even though its bit pattern
        // represents a signed slope. Reinterpret it before Hermite interpolation.
        if(outgoing && encoding==0x5 && value>32767.0) return value-65536.0;
        return value;
    }
}
