using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

/// <summary>Small authored FCVs used to validate the experimental animation pipeline.</summary>
public static class FcvCustomAnimationFactory
{
    public static FcvAnimation CreateCrossedArmsBreathing(Ps2BinSkeleton skeleton,ushort frameCount=90)
    {
        var result=new FcvAnimation { FilePath="em12_900.FCV",FrameCount=frameCount,TrackCount=10 };
        result.Tracks.Add(CreateBreathingRotation(0x01,0,0,0,0.012,frameCount));
        result.Tracks.Add(CreateBreathingRotation(0x02,0,0,0,-0.006,frameCount));
        AddCrossedArm(skeleton,result,right:true,frameCount);
        AddCrossedArm(skeleton,result,right:false,frameCount);
        return result;
    }

    public static FcvAnimation CreateSeatedKneesBent(Ps2BinSkeleton skeleton,ushort frameCount=90)
    {
        var result=new FcvAnimation { FilePath="em12_901.FCV",FrameCount=frameCount,TrackCount=15 };
        // Lower the bind root until the shoes meet the floor while the hips remain seated.
        result.Tracks.Add(CreateStaticTranslation(0x00,new Vector3(0,-900f,0),frameCount));
        result.Tracks.Add(CreateBreathingRotation(0x01,0,0,0,0.006,frameCount));
        result.Tracks.Add(CreateBreathingRotation(0x02,0,0,0,-0.003,frameCount));
        AddSeatedLeg(skeleton,result,right:true,frameCount);
        AddSeatedLeg(skeleton,result,right:false,frameCount);
        AddArmOnKnee(skeleton,result,right:true,frameCount);
        AddArmOnKnee(skeleton,result,right:false,frameCount);
        return result;
    }

    private static void AddSeatedLeg(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount)
    {
        byte thighId=right ? (byte)0x12 : (byte)0x16;
        byte calfId=right ? (byte)0x13 : (byte)0x17;
        byte footId=right ? (byte)0x14 : (byte)0x18;
        int thigh=FindBoneIndex(skeleton,thighId),calf=FindBoneIndex(skeleton,calfId),foot=FindBoneIndex(skeleton,footId);
        if(thigh<0 || calf<0 || foot<0) throw new InvalidDataException("Skeleton em12 sem a cadeia completa das pernas.");

        float side=right ? -1f : 1f;
        Vector3 hip=GetBindWorldPosition(skeleton,thigh);
        Vector3 kneeGuide=hip+new Vector3(side*45f,320f,280f);
        Vector3 knee=hip+Vector3.Normalize(kneeGuide-hip)*skeleton.Bones[calf].LocalPosition.Length();
        Vector3 ankleGuide=hip+new Vector3(side*130f,-20f,600f);
        Vector3 ankle=knee+Vector3.Normalize(ankleGuide-knee)*skeleton.Bones[foot].LocalPosition.Length();

        Quaternion thighWorld=RotationBetween(skeleton.Bones[calf].LocalPosition,knee-hip);
        Quaternion calfWorld=RotationBetween(skeleton.Bones[foot].LocalPosition,ankle-knee);
        Quaternion calfLocal=Quaternion.Normalize(Quaternion.Inverse(thighWorld)*calfWorld);
        // An identity world foot is the em12 bind orientation: sole down and toe forward.
        Quaternion footLocal=Quaternion.Normalize(Quaternion.Inverse(calfWorld));
        animation.Tracks.Add(CreateQuaternionRotation(thighId,thighWorld,frameCount));
        animation.Tracks.Add(CreateQuaternionRotation(calfId,calfLocal,frameCount));
        animation.Tracks.Add(CreateQuaternionRotation(footId,footLocal,frameCount));
    }

    private static void AddArmOnKnee(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount)
    {
        byte upperId=right ? (byte)0x07 : (byte)0x0D;
        byte elbowId=right ? (byte)0x08 : (byte)0x0E;
        byte wristId=right ? (byte)0x09 : (byte)0x0F;
        byte handId=right ? (byte)0x0A : (byte)0x10;
        int upper=FindBoneIndex(skeleton,upperId),elbow=FindBoneIndex(skeleton,elbowId);
        int wrist=FindBoneIndex(skeleton,wristId),hand=FindBoneIndex(skeleton,handId),chest=FindBoneIndex(skeleton,0x03);
        if(upper<0 || elbow<0 || wrist<0 || hand<0 || chest<0) throw new InvalidDataException("Skeleton em12 sem a cadeia completa dos braços.");

        float side=right ? -1f : 1f;
        Vector3 chestPosition=GetBindWorldPosition(skeleton,chest);
        Vector3 shoulder=GetBindWorldPosition(skeleton,upper);
        Vector3 elbowGuide=chestPosition+new Vector3(side*210f,-150f,230f);
        Vector3 elbowTarget=shoulder+Vector3.Normalize(elbowGuide-shoulder)*skeleton.Bones[elbow].LocalPosition.Length();
        Vector3 lowerBind=skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition;
        Vector3 handGuide=chestPosition+new Vector3(side*110f,-240f,430f);
        Vector3 handTarget=elbowTarget+Vector3.Normalize(handGuide-elbowTarget)*lowerBind.Length();

        Quaternion upperWorld=RotationBetween(skeleton.Bones[elbow].LocalPosition,elbowTarget-shoulder);
        Quaternion elbowWorld=RotationBetween(lowerBind,handTarget-elbowTarget);
        Quaternion elbowLocal=Quaternion.Normalize(Quaternion.Inverse(upperWorld)*elbowWorld);
        animation.Tracks.Add(CreateQuaternionRotation(upperId,upperWorld,frameCount));
        animation.Tracks.Add(CreateQuaternionRotation(elbowId,elbowLocal,frameCount));
        animation.Tracks.Add(CreateQuaternionRotation(handId,Quaternion.Identity,frameCount));
    }

    private static void AddCrossedArm(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount)
    {
        byte upperId=right ? (byte)0x07 : (byte)0x0D;
        byte elbowId=right ? (byte)0x08 : (byte)0x0E;
        byte wristId=right ? (byte)0x09 : (byte)0x0F;
        byte handId=right ? (byte)0x0A : (byte)0x10;
        int upper=FindBoneIndex(skeleton,upperId),elbow=FindBoneIndex(skeleton,elbowId);
        int wrist=FindBoneIndex(skeleton,wristId),hand=FindBoneIndex(skeleton,handId);
        int chest=FindBoneIndex(skeleton,0x03);
        if(upper<0 || elbow<0 || wrist<0 || hand<0 || chest<0) throw new InvalidDataException("Skeleton em12 sem a cadeia completa dos braços.");

        Vector3 chestPosition=GetBindWorldPosition(skeleton,chest);
        Vector3 shoulder=GetBindWorldPosition(skeleton,upper);
        float side=right ? -1f : 1f;
        // A natural folded-arms pose is layered rather than symmetrical: the right forearm is
        // slightly higher/front, while the left passes underneath. Keeping both arms on the
        // same Y/Z plane made them intersect at the sternum and look braided.
        Vector3 elbowGuide=chestPosition+(right
            ? new Vector3(-190f,-250f,210f)
            : new Vector3(150f,-340f,170f));
        Vector3 upperDirection=Vector3.Normalize(elbowGuide-shoulder);
        Vector3 elbowTarget=shoulder+upperDirection*skeleton.Bones[elbow].LocalPosition.Length();
        Vector3 lowerBind=skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition;
        Vector3 handGuide=chestPosition+(right
            ? new Vector3(100f,-230f,300f)
            : new Vector3(-120f,-245f,280f));
        Vector3 acrossDirection=Vector3.Normalize(handGuide-elbowTarget);
        Vector3 handTarget=elbowTarget+acrossDirection*lowerBind.Length();

        Quaternion upperWorld=RotationBetween(skeleton.Bones[elbow].LocalPosition,elbowTarget-shoulder);
        Quaternion elbowWorld=RotationBetween(lowerBind,handTarget-elbowTarget);
        Quaternion elbowLocal=Quaternion.Normalize(Quaternion.Inverse(upperWorld)*elbowWorld);
        // Roll around the longitudinal forearm axis. The hand offset is collinear with local X,
        // so this rotates wrist and palm together by 90 degrees without moving the hand pivot.
        Quaternion wristLocal=Quaternion.CreateFromAxisAngle(Vector3.UnitX,-MathF.PI/2f);
        // The mirrored meshes do not need equal wrist angles: the upper/right hand required less
        // lift, while the lower/left hand needs a little more to sit against the opposite arm.
        Quaternion handLocal=Quaternion.CreateFromAxisAngle(Vector3.UnitZ,right ? -0.18f : 0.52f);

        animation.Tracks.Add(CreateQuaternionRotation(upperId,upperWorld,frameCount));
        animation.Tracks.Add(CreateQuaternionRotation(elbowId,elbowLocal,frameCount));
        animation.Tracks.Add(CreateQuaternionRotation(wristId,wristLocal,frameCount));
        // Bend the wrists in mirrored directions so the fingers follow the opposite biceps
        // instead of continuing outward from the forearm and appearing to hang in the air.
        animation.Tracks.Add(CreateQuaternionRotation(handId,handLocal,frameCount));
    }

    private static FcvTrack CreateQuaternionRotation(byte nodeId,Quaternion rotation,ushort frameCount)
    {
        Vector3 euler=FindEvaluatorEuler(nodeId,rotation);
        return CreateStaticRotation(nodeId,euler.X,euler.Y,euler.Z,frameCount);
    }

    private static Vector3 FindEvaluatorEuler(byte nodeId,Quaternion target)
    {
        // Mirrored Z-Y-X arms have several distant Euler representations. Search coarse seeds
        // first, then refine the best basin; a single zero seed can converge to the wrong mirror.
        float[] seeds={-MathF.PI,-MathF.PI/2f,0f,MathF.PI/2f,MathF.PI};
        Vector3 bestAngles=Vector3.Zero;
        float bestError=float.PositiveInfinity;
        foreach(float x in seeds) foreach(float y in seeds) foreach(float z in seeds)
        {
            Vector3 angles=new(x,y,z);
            float error=QuaternionError(Compose(nodeId,angles),target);
            float step=MathF.PI/2f;
            for(int iteration=0;iteration<28;iteration++)
            {
                for(int axis=0;axis<3;axis++)
                {
                    Vector3 plus=angles,minus=angles;
                    if(axis==0){plus.X+=step;minus.X-=step;}
                    else if(axis==1){plus.Y+=step;minus.Y-=step;}
                    else {plus.Z+=step;minus.Z-=step;}
                    float plusError=QuaternionError(Compose(nodeId,plus),target);
                    float minusError=QuaternionError(Compose(nodeId,minus),target);
                    if(plusError<error && plusError<=minusError){angles=plus;error=plusError;}
                    else if(minusError<error){angles=minus;error=minusError;}
                }
                step*=0.5f;
            }
            if(error<bestError){bestError=error;bestAngles=angles;}
        }
        return bestAngles;
    }

    private static Quaternion Compose(byte nodeId,Vector3 euler)
    {
        if(nodeId>0x10) return Quaternion.CreateFromYawPitchRoll(euler.Y,euler.X,euler.Z);
        Quaternion qz=Quaternion.CreateFromAxisAngle(Vector3.UnitZ,euler.Z);
        Quaternion qx=Quaternion.CreateFromAxisAngle(Vector3.UnitX,euler.X);
        Quaternion qy=Quaternion.CreateFromAxisAngle(Vector3.UnitY,euler.Y);
        return Quaternion.Normalize(nodeId is >=0x0B and <=0x10 ? qz*qy*qx : qz*qx*qy);
    }

    private static float QuaternionError(Quaternion value,Quaternion target)
        => 1f-MathF.Abs(Quaternion.Dot(Quaternion.Normalize(value),Quaternion.Normalize(target)));

    private static Quaternion RotationBetween(Vector3 from,Vector3 to)
    {
        from=Vector3.Normalize(from);to=Vector3.Normalize(to);
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

    private static int FindBoneIndex(Ps2BinSkeleton skeleton,byte id)
        => skeleton.FirstIndexById.TryGetValue(id,out int index) ? index : -1;

    private static Vector3 GetBindWorldPosition(Ps2BinSkeleton skeleton,int index)
    {
        Vector3 result=Vector3.Zero;
        int current=index,guard=0;
        while(current>=0 && current<skeleton.Bones.Count && guard++<skeleton.Bones.Count)
        {
            result+=skeleton.Bones[current].LocalPosition;
            current=skeleton.Bones[current].ParentIndex;
        }
        return result;
    }

    private static FcvTrack CreateStaticRotation(byte nodeId,double x,double y,double z,ushort frameCount)
    {
        var track=new FcvTrack { NodeId=nodeId,Type=0x02,DataType=0x00 };
        AddStatic(track.X,x,frameCount);AddStatic(track.Y,y,frameCount);AddStatic(track.Z,z,frameCount);
        return track;
    }

    private static FcvTrack CreateStaticTranslation(byte nodeId,Vector3 value,ushort frameCount)
    {
        var track=new FcvTrack { NodeId=nodeId,Type=0x04,DataType=0x00 };
        AddStatic(track.X,value.X,frameCount);AddStatic(track.Y,value.Y,frameCount);AddStatic(track.Z,value.Z,frameCount);
        return track;
    }

    private static FcvTrack CreateBreathingRotation(byte nodeId,double baseX,double baseY,double baseZ,double amplitude,ushort frameCount)
    {
        var track=new FcvTrack { NodeId=nodeId,Type=0x02,DataType=0x00 };
        ushort[] frames={0,(ushort)(frameCount/4),(ushort)(frameCount/2),(ushort)(frameCount*3/4),frameCount};
        for(int i=0;i<frames.Length;i++)
        {
            double phase=Math.PI*2.0*frames[i]/frameCount;
            double value=baseX+amplitude*Math.Sin(phase);
            double segment=i==frames.Length-1 ? frames[i]-frames[i-1] : frames[Math.Min(i+1,frames.Length-1)]-frames[i];
            double tangent=amplitude*Math.Cos(phase)*(Math.PI*2.0/frameCount)*segment;
            track.X.Keys.Add(new FcvKey(frames[i],value,tangent,tangent,0));
        }
        AddStatic(track.Y,baseY,frameCount);AddStatic(track.Z,baseZ,frameCount);
        return track;
    }

    private static void AddStatic(FcvAxis axis,double value,ushort frameCount)
    {
        axis.Keys.Add(new FcvKey(0,value,0,0,0));
        axis.Keys.Add(new FcvKey(frameCount,value,0,0,0));
    }
}
