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
    public static FcvSkeletonPose Evaluate(Ps2BinSkeleton skeleton, FcvAnimation? animation, float frame, bool stabilizePlantedFeet = false, bool ignoreRootMotion = false)
        => Evaluate(skeleton,animation,frame,stabilizePlantedFeet,true,ignoreRootMotion);

    private static FcvSkeletonPose Evaluate(Ps2BinSkeleton skeleton, FcvAnimation? animation, float frame, bool stabilizePlantedFeet, bool applyTrackedFootIk, bool ignoreRootMotion)
    {
        int n=skeleton.Bones.Count; var wp=new Vector3[n]; var wr=new Quaternion[n];
        var localPos=new Vector3[n]; var localRot=new Quaternion[n];
        for(int i=0;i<n;i++){localPos[i]=skeleton.Bones[i].LocalPosition; localRot[i]=Quaternion.Identity;}
        if(animation!=null)
        {
            foreach(var t in animation.Tracks)
            {
                int bi=FindBoneIndex(skeleton,t.NodeId); if(bi<0) continue;
                int enc=t.DataType>>4;
                // MotionMoveCore tests the kind bits in this exact order. Types carrying 0x10
                // or 0x20 are still local Euler rotations; those bits additionally mark the
                // joint as an IK-chain root during IKInit. They are not absolute transforms.
                if((t.Type&0x02)!=0 && IsSupportedRotationEncoding(enc))
                {
                    float rx=(float)EvalOrZero(t.X,frame,enc), ry=(float)EvalOrZero(t.Y,frame,enc), rz=(float)EvalOrZero(t.Z,frame,enc);
                    localRot[bi]=CreateGameEulerRotation(rx,ry,rz);
                }
                else if((t.Type&0x04)!=0)
                {
                    // Positional tracks are local translations in normal playback. On an IK end
                    // effector the game later interprets this position as the model-space target;
                    // the analytical IK pass below handles that case from the same sampled data.
                    localPos[bi]=new Vector3((float)EvalOrZero(t.X,frame,enc),(float)EvalOrZero(t.Y,frame,enc),(float)EvalOrZero(t.Z,frame,enc));
                }
                else if(t.Type==0x01 && skeleton.Bones[bi].ParentIndex<0 && enc==0x0)
                {
                    // Root Position is authored as actor displacement from the clip origin.
                    // It normally starts at zero; the viewport may remove it when previewing an
                    // animation in place, but the evaluated pose retains the actual motion.
                    if(!ignoreRootMotion)
                    {
                        Vector3 value=new((float)EvalOrZero(t.X,frame,enc),(float)EvalOrZero(t.Y,frame,enc),(float)EvalOrZero(t.Z,frame,enc));
                        localPos[bi]=skeleton.Bones[bi].LocalPosition+value;
                    }
                }
                else if(t.Type==0x40 && skeleton.Bones[bi].ParentIndex<0 && IsSupportedRotationEncoding(enc))
                {
                    float rx=(float)EvalOrZero(t.X,frame,enc),ry=(float)EvalOrZero(t.Y,frame,enc),rz=(float)EvalOrZero(t.Z,frame,enc);
                    localRot[bi]=CreateGameEulerRotation(rx,ry,rz);
                }
                else if((t.Type&0x30)!=0 && IsSupportedRotationEncoding(enc))
                {
                    float rx=(float)EvalOrZero(t.X,frame,enc),ry=(float)EvalOrZero(t.Y,frame,enc),rz=(float)EvalOrZero(t.Z,frame,enc);
                    localRot[bi]=CreateGameEulerRotation(rx,ry,rz);
                }
                // Scale tracks are not exposed by FcvSkeletonPose yet. IK-root rotations are
                // already applied above; their positional solve happens after world transforms.
            }
        }
        var resolveState=new byte[n];
        for(int i=0;i<n;i++) ResolveWorld(i,skeleton,localPos,localRot,wp,wr,resolveState);
        Vector3 authoredRootMotion=animation==null||ignoreRootMotion?Vector3.Zero:GetAuthoredRootMotion(skeleton,animation,frame);
        Quaternion authoredRootRotation=GetRootWorldRotation(skeleton,wr);
        if(animation!=null) ApplyTrackedHandIkChains(skeleton,animation,frame,authoredRootMotion,authoredRootRotation,wp,wr);
        if(!stabilizePlantedFeet && animation!=null)
        {
            if(applyTrackedFootIk)ApplyTrackedFootIkChains(skeleton,animation,frame,authoredRootMotion,authoredRootRotation,wp,wr);
            if(IsAnimationNumber(animation,2)) PreventRightHandHeadPenetration(skeleton,wp,wr);
        }
        FcvSkeletonPose? plantedReference = stabilizePlantedFeet && animation!=null
            ? (MathF.Abs(frame)<0.0001f
                ? new FcvSkeletonPose { WorldPositions=(Vector3[])wp.Clone(), WorldRotations=(Quaternion[])wr.Clone() }
                : Evaluate(skeleton,animation,0f,false,false,ignoreRootMotion))
            : null;
        if(stabilizePlantedFeet && animation!=null)
            StabilizePlantedLeg(skeleton, 0x12, 0x13, 0x14, wp, wr, plantedReference!);
        if(stabilizePlantedFeet && animation!=null)
            StabilizePlantedLeg(skeleton, 0x16, 0x17, 0x18, wp, wr, plantedReference!);
        ApplyJointBlendTable(skeleton,wp,wr);
        // em12_901 is an authored seated-pose diagnostic: its root translation is placement
        // inside the pose, not locomotion to strip before drawing.
        Vector3 rootMotionToRemove=ignoreRootMotion
            ? Vector3.Zero
            : animation!=null && IsAnimationNumber(animation,901)
            ? Vector3.Zero
            : stabilizePlantedFeet && animation!=null
                ? GetRootDelta(skeleton,plantedReference!.WorldPositions)
                : authoredRootMotion;
        return new FcvSkeletonPose{LocalPositions=localPos,LocalRotations=localRot,WorldPositions=wp,WorldRotations=wr,RootMotionToRemove=rootMotionToRemove};
    }

    private static void ApplyJointBlendTable(Ps2BinSkeleton skeleton,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        foreach(Ps2BinJointBlend blend in skeleton.JointBlends)
        {
            int destination=blend.Destination,a=blend.A,c=blend.C;
            if(destination>=worldRotations.Length||a>=worldRotations.Length||c>=worldRotations.Length)continue;
            float amount=Math.Clamp(blend.Percent/100f,0f,1f);
            worldRotations[destination]=Quaternion.Normalize(Quaternion.Slerp(worldRotations[c],worldRotations[a],amount));
            // The original engine replaces only the destination's global matrix rotation.
            // Its translation remains the value already produced by partsWorldCalc/IK.
            _=worldPositions;
        }
    }

    private static void ApplyTrackedHandIkChains(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame,Vector3 rootMotion,Quaternion rootRotation,Vector3[] worldPositions,Quaternion[] worldRotations)
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
            if(controller!=null)ApplyTrackedHandIk(skeleton,frame,upper,elbow,hand,targetTrack,controller,rootMotion,rootRotation,worldPositions,worldRotations);
        }
    }

    private static void ApplyTrackedHandIk(Ps2BinSkeleton skeleton,float frame,int upper,int elbow,int hand,FcvTrack targetTrack,FcvTrack controllerTrack,Vector3 rootMotion,Quaternion rootRotation,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        if((targetTrack.DataType>>4) is not (0x0 or 0x1 or 0x2))return;
        int wrist=skeleton.Bones[hand].ParentIndex;if(wrist<0||skeleton.Bones[wrist].ParentIndex!=elbow)return;
        Vector3 shoulder=worldPositions[upper];
        Vector3 handTarget=Vector3.Transform(SampleTrackRaw(targetTrack,frame),rootRotation)+rootMotion;
        Quaternion[] before=(Quaternion[])worldRotations.Clone();
        Quaternion animatedWristLocal=Quaternion.Normalize(Quaternion.Inverse(before[elbow])*before[wrist]);
        Quaternion animatedHandLocal=Quaternion.Normalize(Quaternion.Inverse(before[wrist])*before[hand]);
        Vector3 toTarget=handTarget-shoulder;float distance=toTarget.Length();
        float upperLength=skeleton.Bones[elbow].LocalPosition.Length();
        // IKInit measures the second link straight from the elbow bind position to the terminal
        // hand bind position, deliberately spanning both wrist and hand records.
        float lowerLength=(GetBindWorldPosition(skeleton,hand)-GetBindWorldPosition(skeleton,elbow)).Length();
        if(!float.IsFinite(distance)||distance<0.0001f||upperLength<0.0001f||lowerLength<0.0001f)return;
        Vector3 direction=toTarget/distance;

        // Exact IKInit + ikCalc orientation construction. The nibble axis is expressed in the
        // bind-chain frame; it is not a pole in torso/world space. IKInit builds a ZY frame from
        // bind root->effector, stores its transpose and its X axis. ikCalc carries that X axis
        // through the animated upper-arm matrix, constructs a ZX frame aimed at the target and
        // concatenates the stored bind correction before applying the two cosine-law angles.
        Vector3 localBendAxis=(controllerTrack.DataType&0x0F) switch
        {
            0x0 => Vector3.UnitY,
            0x1 => -Vector3.UnitY,
            0x2 => Vector3.UnitX,
            0x3 => -Vector3.UnitX,
            0x4 => Vector3.UnitZ,
            0x5 => -Vector3.UnitZ,
            _ => -Vector3.UnitZ
        };
        Vector3 bindDirection=GetBindWorldPosition(skeleton,upper)-GetBindWorldPosition(skeleton,hand);
        GameBasis bindFrame=CreateOrientationZY(bindDirection,localBendAxis);
        Vector3 storedAxis=bindFrame.Transform(Vector3.UnitX);
        Vector3 animatedAxis=Vector3.Transform(storedAxis,before[upper]);
        GameBasis aimFrame=CreateOrientationZX(direction,animatedAxis);
        // The ZX frame's Y column is the actual bend-plane direction selected by ikCalc. Use it
        // for the geometric two-link solution; this preserves the source's plane construction
        // while avoiding a row/column convention dependency when converting the final GC matrix.
        Vector3 pole=aimFrame.Y;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 elbowTarget=shoulder+direction*along+pole*bend;
        Quaternion upperCorrection=RotationBetween(Vector3.Transform(skeleton.Bones[elbow].LocalPosition,worldRotations[upper]),elbowTarget-shoulder);
        worldRotations[upper]=Quaternion.Normalize(upperCorrection*worldRotations[upper]);
        worldRotations[elbow]=Quaternion.Normalize(upperCorrection*worldRotations[elbow]);
        worldPositions[elbow]=elbowTarget;
        Vector3 bindLower=skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition;
        Vector3 currentLower=Vector3.Transform(bindLower,worldRotations[elbow]);
        Quaternion lowerCorrection=RotationBetween(currentLower,handTarget-elbowTarget);
        worldRotations[elbow]=Quaternion.Normalize(lowerCorrection*worldRotations[elbow]);
        // Original A0 path: recompute the wrist from the solved elbow using its authored local
        // matrix, while the terminal hand matrix is evaluated directly from the model/root.
        // Treating the hand as a normal wrist child applies the wrist transform twice.
        worldRotations[wrist]=Quaternion.Normalize(worldRotations[elbow]*animatedWristLocal);
        worldPositions[wrist]=worldPositions[elbow]+Vector3.Transform(skeleton.Bones[wrist].LocalPosition,worldRotations[elbow]);
        worldRotations[hand]=Quaternion.Normalize(rootRotation*animatedHandLocal);
        worldPositions[hand]=handTarget;
        // The A0/0x210 path does one more indispensable step after ikCalc. The absolute hand
        // matrix carries an authored roll that no longer agrees with the solved wrist matrix;
        // the engine measures that residual twist and spreads it over wrist/elbow/upper arm.
        // pl00_083 animates both the 02 rotation and 04 target on each hand, making omission of
        // this step especially visible as a corkscrewed, stretched forearm.
        Quaternion handRelativeToWrist=Quaternion.Normalize(Quaternion.Inverse(worldRotations[wrist])*worldRotations[hand]);
        float twist=MeasureGameIkTwist(handRelativeToWrist);
        if(float.IsFinite(twist)&&MathF.Abs(twist)>0.000001f)
        {
            ApplyWorldAxisTwist(wrist,0.5f*twist,worldRotations);
            ApplyWorldAxisTwist(elbow,0.25f*twist,worldRotations);
            ApplyWorldAxisTwist(upper,0.125f*twist,worldRotations);
        }
        UpdateDescendantsExcept(skeleton,upper,elbow,worldPositions,worldRotations,before);
        UpdateDescendantsExcept(skeleton,elbow,wrist,worldPositions,worldRotations,before);
        UpdateDescendantsExcept(skeleton,wrist,hand,worldPositions,worldRotations,before);
        UpdateDescendants(skeleton,hand,worldPositions,worldRotations,before);
    }

    private static float MeasureGameIkTwist(Quaternion relative)
    {
        // Direct translation of IK_TWIST_ANGLE from ik.cpp (matrix columns 0 and 2).
        Vector3 a=Vector3.Transform(Vector3.UnitX,relative);
        Vector3 b=Vector3.Transform(Vector3.UnitZ,relative);
        Vector3 c=Vector3.Cross(a,Vector3.UnitY);
        if(c.LengthSquared()<0.000001f||b.LengthSquared()<0.000001f)return 0f;
        c=Vector3.Normalize(c);b=Vector3.Normalize(b);
        float angle=MathF.Acos(Math.Clamp(Vector3.Dot(c,b),-1f,1f));
        Vector3 d=Vector3.Cross(c,b);
        return Vector3.Dot(d,a)<0f?-angle:angle;
    }

    private static void ApplyWorldAxisTwist(int bone,float angle,Quaternion[] worldRotations)
    {
        Vector3 axis=Vector3.Transform(Vector3.UnitX,worldRotations[bone]);
        if(axis.LengthSquared()<0.000001f)return;
        Quaternion correction=Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis),angle);
        worldRotations[bone]=Quaternion.Normalize(correction*worldRotations[bone]);
    }

    private static GameBasis CreateOrientationZX(Vector3 z,Vector3 x)
    {
        if(z.LengthSquared()<0.000001f||x.LengthSquared()<0.000001f)return GameBasis.Identity;
        Vector3 vz=Vector3.Normalize(z),vx=Vector3.Normalize(x);
        Vector3 vy=Vector3.Cross(vz,vx);
        if(vy.LengthSquared()<0.000001f)return GameBasis.Identity;
        vy=Vector3.Normalize(vy);vx=Vector3.Normalize(Vector3.Cross(vy,vz));
        return new GameBasis(vx,vy,vz);
    }

    private static GameBasis CreateOrientationZY(Vector3 z,Vector3 y)
    {
        if(z.LengthSquared()<0.000001f||y.LengthSquared()<0.000001f)return GameBasis.Identity;
        Vector3 vz=Vector3.Normalize(z),vy=Vector3.Normalize(y);
        Vector3 vx=Vector3.Cross(vy,vz);
        if(vx.LengthSquared()<0.000001f)return GameBasis.Identity;
        vx=Vector3.Normalize(vx);vy=Vector3.Normalize(Vector3.Cross(vz,vx));
        return new GameBasis(vx,vy,vz);
    }

    private static Quaternion QuaternionFromAxes(Vector3 x,Vector3 y,Vector3 z)
    {
        // System.Numerics stores transformed basis vectors in the matrix rows.
        Matrix4x4 matrix=new(
            x.X,x.Y,x.Z,0f,
            y.X,y.Y,y.Z,0f,
            z.X,z.Y,z.Z,0f,
            0f,0f,0f,1f);
        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(matrix));
    }

    private readonly record struct GameBasis(Vector3 X,Vector3 Y,Vector3 Z)
    {
        public static GameBasis Identity => new(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ);
        public Vector3 Transform(Vector3 v)=>X*v.X+Y*v.Y+Z*v.Z;
        public GameBasis Transpose()=>new(
            new Vector3(X.X,Y.X,Z.X),
            new Vector3(X.Y,Y.Y,Z.Y),
            new Vector3(X.Z,Y.Z,Z.Z));
        public static GameBasis Multiply(GameBasis a,GameBasis b)=>new(a.Transform(b.X),a.Transform(b.Y),a.Transform(b.Z));
        public static GameBasis FromQuaternion(Quaternion q)=>new(
            Vector3.Transform(Vector3.UnitX,q),
            Vector3.Transform(Vector3.UnitY,q),
            Vector3.Transform(Vector3.UnitZ,q));
        public Quaternion ToQuaternion()=>QuaternionFromAxes(X,Y,Z);
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

    private static void ApplyTrackedFootIkChains(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame,Vector3 rootMotion,Quaternion rootRotation,Vector3[] worldPositions,Quaternion[] worldRotations)
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
            if(controller!=null)ApplyTrackedFootIk(skeleton,animation,frame,thigh,calf,foot,targetTrack,controller,rootMotion,rootRotation,worldPositions,worldRotations);
        }
    }

    private static void ApplyTrackedFootIk(Ps2BinSkeleton skeleton,FcvAnimation animation,float frame,int thigh,int calf,int foot,FcvTrack targetTrack,FcvTrack controllerTrack,Vector3 rootMotion,Quaternion rootRotation,Vector3[] worldPositions,Quaternion[] worldRotations)
    {
        if(thigh<0 || calf<0 || foot<0 || skeleton.Bones[calf].ParentIndex!=thigh || skeleton.Bones[foot].ParentIndex!=calf) return;
        if((targetTrack.DataType>>4) is not (0x0 or 0x1 or 0x2))return;

        // Type 0x04 on the ankle is an absolute model-space IK target. The evaluator stores root
        // translation as a delta over the BIN root, so add that delta here; the viewport removes
        // it again when anchoring the enemy to its ESL position. The visible ankle consequently
        // lands on the FCV coordinates while the hip remains in the same coordinate space.
        Vector3 rawTarget=SampleTrackRaw(targetTrack,frame);
        Vector3 target=Vector3.Transform(rawTarget,rootRotation)+rootMotion;
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

    private static Quaternion GetRootWorldRotation(Ps2BinSkeleton skeleton,Quaternion[] worldRotations)
    {
        for(int i=0;i<skeleton.Bones.Count&&i<worldRotations.Length;i++)
            if(skeleton.Bones[i].ParentIndex<0)
                return Quaternion.Normalize(worldRotations[i]);
        return Quaternion.Identity;
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

    private static Quaternion CreateGameEulerRotation(float x,float y,float z)
    {
        // RotMatrix in the original engine builds Rz * Ry * Rx: X is applied first, followed by
        // Y and Z. Quaternion multiplication follows the same right-to-left composition.
        Quaternion qz=Quaternion.CreateFromAxisAngle(Vector3.UnitZ,z);
        Quaternion qx=Quaternion.CreateFromAxisAngle(Vector3.UnitX,x);
        Quaternion qy=Quaternion.CreateFromAxisAngle(Vector3.UnitY,y);
        return Quaternion.Normalize(qz*qy*qx);
    }

    private static bool IsAnimationNumber(FcvAnimation animation,int number)
    {
        string stem=Path.GetFileNameWithoutExtension(animation.FilePath);
        int separator=stem.LastIndexOf('_');
        return separator>=0 && int.TryParse(stem[(separator+1)..],out int parsed) && parsed==number;
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
    private static void ResolveWorld(int i, Ps2BinSkeleton s, Vector3[] lp, Quaternion[] lr, Vector3[] wp, Quaternion[] wr, byte[] state)
    {
        if(state[i]==2)return; if(state[i]==1){wp[i]=lp[i];wr[i]=lr[i];state[i]=2;return;} state[i]=1;
        int p=s.Bones[i].ParentIndex;
        if(p<0){wp[i]=lp[i];wr[i]=lr[i];}
        else
        {
            ResolveWorld(p,s,lp,lr,wp,wr,state);
            wr[i]=Quaternion.Normalize(wr[p]*lr[i]);
            wp[i]=wp[p]+Vector3.Transform(lp[i],wr[p]);
        }
        state[i]=2;
    }
    private static bool IsSupportedRotationEncoding(int encoding)
        => encoding is 0x0 or 0x1 or 0x2 or 0x4 or 0x5 or 0x6 or 0x8 or 0x9 or 0xA or 0xF;

    private static double DecodeValue(double value,int encoding)
    {
        // The original engine's FCC_S16/FCC_S8 decoders use the same 1/10000
        // scale for every compact integer. Float-valued layouts are already radians.
        return encoding switch
        {
            0x0 or 0x1 or 0x2 or 0xF => value,
            0x4 or 0x5 or 0x6 or 0x8 or 0x9 or 0xA => value * 0.0001,
            _ => 0.0
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
        if(a.Keys.Count==0)return 0; if(a.Keys.Count==1)return DecodeValue(a.Keys[0].Value,encoding);
        if(frame<=a.Keys[0].Frame)return DecodeValue(a.Keys[0].Value,encoding); if(frame>=a.Keys[^1].Frame)return DecodeValue(a.Keys[^1].Value,encoding);
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
        return h00*DecodeValue(k0.Value,encoding)+h10*outTangent+h01*DecodeValue(k1.Value,encoding)+h11*inTangent;
    }

    private static double DecodeTangent(double value,int encoding,bool outgoing)
    {
        _=outgoing;
        return encoding switch
        {
            0x0 or 0x4 or 0x8 => value,
            0x1 or 0x2 or 0x5 or 0x6 or 0x9 or 0xA => value * 0.0001,
            0xF => 0.0,
            _ => value
        };
    }
}
