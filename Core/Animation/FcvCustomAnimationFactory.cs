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

    /// <summary>
    /// Creates a self-contained seated breathing test using only FK rotations and relative root
    /// motion. The limb IDs are resolved from the standard humanoid branches in the supplied BIN;
    /// no source FCV is copied, so this also validates the writer independently of the game clips.
    /// </summary>
    public static FcvAnimation CreateGroundSeatedBreathing(Ps2BinSkeleton skeleton,string fileName,ushort frameCount=90)
    {
        var result=new FcvAnimation { FilePath=fileName,FrameCount=frameCount,TrackCount=15 };
        // Put the pelvis just above the floor. Extended legs are deliberately used for this
        // diagnostic pose because they remain readable from every camera angle and cannot be
        // mistaken for a standing squat.
        result.Tracks.Add(CreateRelativeMovement(0x00,new Vector3(0,-1050f,0),frameCount));
        result.Tracks.Add(CreateBreathingRotation(0x01,-0.035,0,0,0.014,frameCount));
        result.Tracks.Add(CreateBreathingRotation(0x02,-0.10,0,0,-0.022,frameCount));
        AddGroundSeatedLeg(skeleton,result,right:true,frameCount);
        AddGroundSeatedLeg(skeleton,result,right:false,frameCount);
        AddGroundSeatedArm(skeleton,result,right:true,frameCount);
        AddGroundSeatedArm(skeleton,result,right:false,frameCount);
        return result;
    }

    /// <summary>Creates an unmistakable in-game test pose with both hands raised.</summary>
    public static FcvAnimation CreateRaisedHandsBreathing(Ps2BinSkeleton skeleton,string fileName,ushort frameCount)
    {
        var result=new FcvAnimation { FilePath=fileName,FrameCount=frameCount,TrackCount=5 };
        result.Tracks.Add(CreateBreathingRotation(0x02,-0.035,0,0,0.012,frameCount));
        AddRaisedArm(skeleton,result,right:true,frameCount);
        AddRaisedArm(skeleton,result,right:false,frameCount);
        return result;
    }

    /// <summary>
    /// Reposes the main arm tracks in an existing clip while retaining its complete track/key
    /// topology. This is intended for strict in-game tests where resource size and encodings must
    /// remain compatible with the animation slot being replaced.
    /// </summary>
    public static FcvAnimation ApplyRaisedHandsToTemplate(Ps2BinSkeleton skeleton,FcvAnimation template)
    {
        var authored=new FcvAnimation { FilePath=template.FilePath,FrameCount=template.FrameCount,TrackCount=4 };
        AddRaisedArm(skeleton,authored,right:true,template.FrameCount);
        AddRaisedArm(skeleton,authored,right:false,template.FrameCount);
        foreach(FcvTrack source in authored.Tracks)
        {
            FcvTrack? destination=template.Tracks.FirstOrDefault(t=>t.NodeId==source.NodeId&&t.Type==0x02);
            if(destination==null) throw new InvalidDataException($"FCV molde sem rotation track para o bone 0x{source.NodeId:X2}.");
            ReplaceRotationAxis(destination.X,source.X.Keys[0].Value,destination.DataType>>4);
            ReplaceRotationAxis(destination.Y,source.Y.Keys[0].Value,destination.DataType>>4);
            ReplaceRotationAxis(destination.Z,source.Z.Keys[0].Value,destination.DataType>>4);
        }
        return template;
    }

    public static FcvAnimation ApplyTwoHandedKnifeLoop(Ps2BinSkeleton skeleton,FcvAnimation template)
    {
        var authored=new FcvAnimation { FilePath=template.FilePath,FrameCount=template.FrameCount,TrackCount=8 };
        FcvSkeletonPose basePose=FcvSkeletonEvaluator.Evaluate(skeleton,template,0f,false);
        AddKnifeGuardArm(skeleton,basePose,authored,right:true,template.FrameCount);
        AddKnifeGuardArm(skeleton,basePose,authored,right:false,template.FrameCount);
        foreach(FcvTrack source in authored.Tracks)
        {
            FcvTrack? destination=template.Tracks.FirstOrDefault(t=>t.NodeId==source.NodeId&&t.Type==0x02);
            if(destination==null)throw new InvalidDataException($"FCV molde sem rotation track para o bone 0x{source.NodeId:X2}.");
            float side=source.NodeId is >=0x0D and <=0x10?-1f:1f;
            float strength=source.NodeId switch{0x07 or 0x0D=>1f,0x08 or 0x0E=>0.65f,0x09 or 0x0F=>0.25f,_=>0.12f};
            int encoding=destination.DataType>>4;
            ReplaceRotationAxisLoop(destination.X,source.X.Keys[0].Value,encoding,0.012f*strength);
            ReplaceRotationAxisLoop(destination.Y,source.Y.Keys[0].Value,encoding,0.007f*strength*side);
            ReplaceRotationAxisLoop(destination.Z,source.Z.Keys[0].Value,encoding,-0.009f*strength*side);
        }
        return template;
    }

    public static FcvAnimation ApplyTwoHandedKnifeStart(FcvAnimation startTemplate,FcvAnimation completedLoop)
    {
        byte[] armIds={0x07,0x08,0x09,0x0A,0x0D,0x0E,0x0F,0x10};
        foreach(byte nodeId in armIds)
        {
            FcvTrack? source=completedLoop.Tracks.FirstOrDefault(t=>t.NodeId==nodeId&&t.Type==0x02);
            FcvTrack? destination=startTemplate.Tracks.FirstOrDefault(t=>t.NodeId==nodeId&&t.Type==0x02);
            if(source==null||destination==null)continue;int sourceEncoding=source.DataType>>4;
            double x=DecodeRotationValue(source.X.Keys[0].Value,sourceEncoding),y=DecodeRotationValue(source.Y.Keys[0].Value,sourceEncoding),z=DecodeRotationValue(source.Z.Keys[0].Value,sourceEncoding);int encoding=destination.DataType>>4;
            if(nodeId is 0x09 or 0x0A){ReplaceRotationAxis(destination.X,x,encoding);ReplaceRotationAxis(destination.Y,y,encoding);ReplaceRotationAxis(destination.Z,z,encoding);}
            else{ReplaceRotationAxisTransition(destination.X,x,encoding);ReplaceRotationAxisTransition(destination.Y,y,encoding);ReplaceRotationAxisTransition(destination.Z,z,encoding);}
        }
        return startTemplate;
    }

    public static FcvAnimation ApplyKnifeAttack(FcvAnimation attackTemplate,FcvAnimation completedLoop)
    {
        throw new InvalidOperationException("Use the skeleton overload for the knife attack.");
    }

    public static FcvAnimation ApplyKnifeAttack(Ps2BinSkeleton skeleton,FcvAnimation attackTemplate,FcvAnimation completedLoop)
    {
        // Keep the stock 126 shoulder/elbow arc. It is already a clean authored knife attack and
        // preserves the elbow bend; solving a new end point and interpolating its Euler axes made
        // the arm cross the head and introduced a visible twist. Only blend the ends into our 125
        // guard and stabilise the wrist/hand in the corrected grip.
        byte[] armIds={0x07,0x08,0x09,0x0A,0x0D,0x0E,0x0F,0x10};
        foreach(byte nodeId in armIds)
        {
            FcvTrack? guard=completedLoop.Tracks.FirstOrDefault(t=>t.NodeId==nodeId&&t.Type==0x02);
            FcvTrack? attack=attackTemplate.Tracks.FirstOrDefault(t=>t.NodeId==nodeId&&t.Type==0x02);
            if(guard==null||attack==null)continue;int guardEncoding=guard.DataType>>4,attackEncoding=attack.DataType>>4;
            double gx=DecodeRotationValue(guard.X.Keys[0].Value,guardEncoding),gy=DecodeRotationValue(guard.Y.Keys[0].Value,guardEncoding),gz=DecodeRotationValue(guard.Z.Keys[0].Value,guardEncoding);
            if(nodeId is 0x07 or 0x08)
            {
                BlendRotationAxisEnds(attack.X,gx,attackEncoding);
                BlendRotationAxisEnds(attack.Y,gy,attackEncoding);
                BlendRotationAxisEnds(attack.Z,gz,attackEncoding);
            }
            else if(nodeId is 0x09 or 0x0A)
            {
                // Correct only the grip. Rotating the complete arm to turn the palm over caused
                // the previous elbow deformation and sent the knife through Leon's head.
                ReplaceRotationAxis(attack.X,gx,attackEncoding);
                ReplaceRotationAxis(attack.Y,gy,attackEncoding);
                ReplaceRotationAxis(attack.Z,gz,attackEncoding);
            }
            else
            {
                float recoil=nodeId switch{0x0D=>0.012f,0x0E=>-0.008f,_=>0f};
                ReplaceRotationAxisLoop(attack.X,gx,attackEncoding,recoil);
                ReplaceRotationAxisLoop(attack.Y,gy,attackEncoding,recoil*0.45f);
                ReplaceRotationAxisLoop(attack.Z,gz,attackEncoding,-recoil*0.35f);
            }
        }
        StabilizeKnifeHandWorld(skeleton,attackTemplate,completedLoop);
        // Keep the 125 combat stance throughout the strike. The stock 126 closes both legs,
        // causing a visible pop on 125 -> 126 and another one when returning to the loop.
        for(byte nodeId=0x12;nodeId<=0x19;nodeId++)
        {
            foreach(FcvTrack attackLeg in attackTemplate.Tracks.Where(t=>t.NodeId==nodeId&&t.Type is 0x02 or 0x10 or 0x20 or 0x04))
            {
                FcvTrack? guardLeg=completedLoop.Tracks.FirstOrDefault(t=>t.NodeId==nodeId&&t.Type==attackLeg.Type);
                if(guardLeg==null)continue;int sourceEncoding=guardLeg.DataType>>4,destinationEncoding=attackLeg.DataType>>4;
                if(attackLeg.Type is 0x02 or 0x10)
                {
                    ReplaceRotationAxis(attackLeg.X,DecodeRotationValue(guardLeg.X.Keys[0].Value,sourceEncoding),destinationEncoding);
                    ReplaceRotationAxis(attackLeg.Y,DecodeRotationValue(guardLeg.Y.Keys[0].Value,sourceEncoding),destinationEncoding);
                    ReplaceRotationAxis(attackLeg.Z,DecodeRotationValue(guardLeg.Z.Keys[0].Value,sourceEncoding),destinationEncoding);
                }
                else
                {
                    ReplaceRawAxis(attackLeg.X,guardLeg.X.Keys[0].Value,destinationEncoding);
                    ReplaceRawAxis(attackLeg.Y,guardLeg.Y.Keys[0].Value,destinationEncoding);
                    ReplaceRawAxis(attackLeg.Z,guardLeg.Z.Keys[0].Value,destinationEncoding);
                }
            }
        }
        return attackTemplate;
    }

    private static void StabilizeKnifeHandWorld(Ps2BinSkeleton skeleton,FcvAnimation attack,FcvAnimation guard)
    {
        const byte wristId=0x09,handId=0x0A;
        FcvTrack? wristTrack=attack.Tracks.FirstOrDefault(t=>t.NodeId==wristId&&t.Type==0x02);
        if(wristTrack==null)return;
        int wrist=FindBoneIndex(skeleton,wristId),hand=FindBoneIndex(skeleton,handId);
        if(wrist<0||hand<0)return;
        int wristParent=skeleton.Bones[wrist].ParentIndex;
        if(wristParent<0)return;

        FcvSkeletonPose guardPose=FcvSkeletonEvaluator.Evaluate(skeleton,guard,0f,false);
        Quaternion desiredHandWorld=guardPose.WorldRotations[hand];
        Quaternion handLocal=guardPose.LocalRotations[hand];
        int encoding=wristTrack.DataType>>4;
        ushort[] frames=wristTrack.X.Keys.Select(k=>k.Frame)
            .Concat(wristTrack.Y.Keys.Select(k=>k.Frame))
            .Concat(wristTrack.Z.Keys.Select(k=>k.Frame))
            .Distinct().OrderBy(f=>f).ToArray();
        var rotations=new Dictionary<ushort,Vector3>();
        foreach(ushort frame in frames)
        {
            FcvSkeletonPose pose=FcvSkeletonEvaluator.Evaluate(skeleton,attack,frame,false);
            Quaternion wristLocal=Quaternion.Normalize(
                Quaternion.Inverse(pose.WorldRotations[wristParent])
                *desiredHandWorld
                *Quaternion.Inverse(handLocal));
            rotations[frame]=FindEvaluatorEuler(wristId,wristLocal,false);
        }
        ReplaceRotationAxisByFrame(wristTrack.X,encoding,rotations,0);
        ReplaceRotationAxisByFrame(wristTrack.Y,encoding,rotations,1);
        ReplaceRotationAxisByFrame(wristTrack.Z,encoding,rotations,2);
    }

    private static void ReplaceRotationAxisByFrame(FcvAxis axis,int encoding,IReadOnlyDictionary<ushort,Vector3> rotations,int component)
    {
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];
            Vector3 value=rotations[key.Frame];
            double radians=component==0?value.X:component==1?value.Y:value.Z;
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(radians,encoding),0,0,key.Extra);
        }
    }

    private static void AddKnifeGuardArm(Ps2BinSkeleton skeleton,FcvSkeletonPose basePose,FcvAnimation animation,bool right,ushort frameCount,Vector3? targetOffset=null,Vector3? poleHint=null)
    {
        byte upperId=right?(byte)0x07:(byte)0x0D;
        byte elbowId=right?(byte)0x08:(byte)0x0E;
        byte wristId=right?(byte)0x09:(byte)0x0F;
        byte handId=right?(byte)0x0A:(byte)0x10;
        int upper=FindBoneIndex(skeleton,upperId),elbow=FindBoneIndex(skeleton,elbowId);
        int wrist=FindBoneIndex(skeleton,wristId),hand=FindBoneIndex(skeleton,handId),chest=FindBoneIndex(skeleton,0x03);
        if(upper<0||elbow<0||wrist<0||hand<0||chest<0)throw new InvalidDataException("Skeleton sem as cadeias necessárias para a guarda de faca.");

        float side=right?-1f:1f;
        Vector3 shoulder=basePose.WorldPositions[upper];
        Vector3 chestPosition=basePose.WorldPositions[chest];
        // Bring both hand pivots together in front of the sternum. A small separation leaves
        // room for the supporting palm around the knife wrist.
        // In the pl00 coordinate system positive Z is in front of Leon. The previous negative-Z
        // target produced a mathematically valid pose on the wrong side of his torso.
        Vector3 handTarget=chestPosition+(targetOffset??(right?new Vector3(-55f,95f,285f):new Vector3(35f,-55f,255f)));
        float upperLength=skeleton.Bones[elbow].LocalPosition.Length();
        Vector3 lowerBind=skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition;
        float lowerLength=lowerBind.Length();
        Vector3 toTarget=handTarget-shoulder;
        float distance=toTarget.Length();
        if(distance<0.001f||upperLength<0.001f||lowerLength<0.001f)return;
        Vector3 direction=toTarget/distance;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        handTarget=shoulder+direction*reachable;
        Vector3 pole=poleHint??new Vector3(side,-0.40f,0.28f);
        pole-=direction*Vector3.Dot(pole,direction);
        if(pole.LengthSquared()<0.0001f)pole=Vector3.UnitY-direction*Vector3.Dot(Vector3.UnitY,direction);
        pole=Vector3.Normalize(pole);
        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 elbowTarget=shoulder+direction*along+pole*bend;
        Quaternion upperWorld=RotationBetween(skeleton.Bones[elbow].LocalPosition,elbowTarget-shoulder);
        Quaternion elbowWorld=RotationBetween(lowerBind,handTarget-elbowTarget);
        int parent=skeleton.Bones[upper].ParentIndex;
        Quaternion parentWorld=parent>=0?basePose.WorldRotations[parent]:Quaternion.Identity;
        Quaternion upperLocal=Quaternion.Normalize(Quaternion.Inverse(parentWorld)*upperWorld);
        Quaternion elbowLocal=Quaternion.Normalize(Quaternion.Inverse(upperWorld)*elbowWorld);
        animation.Tracks.Add(CreateQuaternionRotation(upperId,upperLocal,frameCount,false));
        animation.Tracks.Add(CreateQuaternionRotation(elbowId,elbowLocal,frameCount,false));
        // The endpoint solve assumes the wrist and hand continue along the forearm. Author them
        // explicitly so the old one-handed knife clip cannot rotate the weapon back behind the
        // shoulder after the upper-arm pose has been replaced.
        animation.Tracks.Add(CreateQuaternionRotation(wristId,Quaternion.Identity,frameCount,false));
        Quaternion handGrip=right?Quaternion.CreateFromAxisAngle(Vector3.UnitX,MathF.PI):Quaternion.Identity;
        animation.Tracks.Add(CreateQuaternionRotation(handId,handGrip,frameCount,false));
    }

    private static void ReplaceRotationAxis(FcvAxis axis,double radians,int encoding)
    {
        double stored=EncodeRotationValue(radians,encoding);
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];
            axis.Keys[i]=new FcvKey(key.Frame,stored,0,0,key.Extra);
        }
    }

    private static void ReplaceRawAxis(FcvAxis axis,double value,int encoding)
    {
        double stored=encoding switch{0 or 1=>value,2 or 4 or 5 or 6=>Math.Clamp(Math.Round(value),short.MinValue,short.MaxValue),8 or 9 or 10=>Math.Clamp(Math.Round(value),sbyte.MinValue,sbyte.MaxValue),_=>value};
        for(int i=0;i<axis.Keys.Count;i++){FcvKey key=axis.Keys[i];axis.Keys[i]=new FcvKey(key.Frame,stored,0,0,key.Extra);}
    }

    private static void ReplaceRotationAxisTransition(FcvAxis axis,double targetRadians,int encoding)
    {
        if(axis.Keys.Count==0)return;
        double startRadians=DecodeRotationValue(axis.Keys[0].Value,encoding);
        ushort first=axis.Keys[0].Frame,last=axis.Keys[^1].Frame;float span=Math.Max(1,last-first);
        double delta=ShortestAngle(targetRadians-startRadians);
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];float u=Math.Clamp((key.Frame-first)/span,0f,1f);
            float eased=u*u*(3f-2f*u);double settle=Math.Sin(Math.PI*u)*0.035*delta;
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(startRadians+delta*eased+settle,encoding),0,0,key.Extra);
        }
    }

    private static void ReplaceRotationAxisLoop(FcvAxis axis,double centerRadians,int encoding,float amplitude)
    {
        if(axis.Keys.Count==0)return;ushort first=axis.Keys[0].Frame,last=axis.Keys[^1].Frame;float span=Math.Max(1,last-first);
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];double phase=Math.PI*2.0*(key.Frame-first)/span;
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(centerRadians+amplitude*Math.Sin(phase),encoding),0,0,key.Extra);
        }
    }

    private static void AddRotationAxisLoop(FcvAxis axis,int encoding,float amplitude)
    {
        if(axis.Keys.Count<2||MathF.Abs(amplitude)<0.000001f)return;
        ushort first=axis.Keys[0].Frame,last=axis.Keys[^1].Frame;if(last<=first)return;float span=last-first;
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];double radians=DecodeRotationValue(key.Value,encoding);
            double phase=Math.PI*2.0*(key.Frame-first)/span;
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(radians+amplitude*Math.Sin(phase),encoding),key.TangentIn,key.TangentOut,key.Extra);
        }
    }

    private static void BlendRotationAxisEnds(FcvAxis axis,double guardRadians,int encoding)
    {
        if(axis.Keys.Count==0)return;ushort first=axis.Keys[0].Frame,last=axis.Keys[^1].Frame;float span=Math.Max(1,last-first);
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];float u=Math.Clamp((key.Frame-first)/span,0f,1f);
            float weight=u<0.24f?1f-Smooth(u/0.24f):u>0.76f?Smooth((u-0.76f)/0.24f):0f;
            double original=DecodeRotationValue(key.Value,encoding);
            double blended=original+ShortestAngle(guardRadians-original)*weight;
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(blended,encoding),0,0,key.Extra);
        }
    }

    private static void ReplaceRotationAxisAttack(FcvAxis axis,double guardRadians,double strikeRadians,int encoding)
    {
        if(axis.Keys.Count==0)return;ushort first=axis.Keys[0].Frame,last=axis.Keys[^1].Frame;float span=Math.Max(1,last-first);
        double delta=ShortestAngle(strikeRadians-guardRadians);
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];float u=Math.Clamp((key.Frame-first)/span,0f,1f);
            float amount=u<0.18f?-0.10f*Smooth(u/0.18f):u<0.55f?-0.10f+1.10f*Smooth((u-0.18f)/0.37f):1f-Smooth((u-0.55f)/0.45f);
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(guardRadians+delta*amount,encoding),0,0,key.Extra);
        }
    }

    private static void OffsetRotationAxis(FcvAxis axis,double radians,int encoding)
    {
        for(int i=0;i<axis.Keys.Count;i++)
        {
            FcvKey key=axis.Keys[i];double value=DecodeRotationValue(key.Value,encoding);
            axis.Keys[i]=new FcvKey(key.Frame,EncodeRotationValue(NormalizeAngle((float)(value+radians)),encoding),key.TangentIn,key.TangentOut,key.Extra);
        }
    }

    private static float Smooth(float value){value=Math.Clamp(value,0f,1f);return value*value*(3f-2f*value);}

    private static double ShortestAngle(double value)=>Math.IEEERemainder(value,Math.PI*2.0);

    private static double EncodeRotationValue(double radians,int encoding)=>encoding switch
    {
        0x0 or 0x1 or 0x2 or 0xF=>radians,
        0x4 or 0x5 or 0x6=>Math.Clamp(Math.Round(radians*10000.0),short.MinValue,short.MaxValue),
        0x8 or 0x9 or 0xA=>Math.Clamp(Math.Round(radians*10000.0),sbyte.MinValue,sbyte.MaxValue),
        _=>throw new InvalidDataException($"Encoding de rotação não suportado no molde: 0x{encoding:X1}0")
    };

    private static double DecodeRotationValue(double value,int encoding)=>encoding switch
    {
        0x0 or 0x1 or 0x2 or 0xF=>value,
        0x4 or 0x5 or 0x6 or 0x8 or 0x9 or 0xA=>value*0.0001,
        _=>throw new InvalidDataException($"Encoding de rotação não suportado no molde: 0x{encoding:X1}0")
    };

    private static void AddRaisedArm(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount)
    {
        byte upperId=right ? (byte)0x07 : (byte)0x0D;
        byte elbowId=right ? (byte)0x08 : (byte)0x0E;
        byte wristId=right ? (byte)0x09 : (byte)0x0F;
        byte handId=right ? (byte)0x0A : (byte)0x10;
        int upper=FindBoneIndex(skeleton,upperId),elbow=FindBoneIndex(skeleton,elbowId);
        int wrist=FindBoneIndex(skeleton,wristId),hand=FindBoneIndex(skeleton,handId);
        if(upper<0 || elbow<0 || wrist<0 || hand<0 || skeleton.Bones[elbow].ParentIndex!=upper)
            throw new InvalidDataException("Skeleton sem uma cadeia humanoide completa nos braços.");

        float side=right ? -1f : 1f;
        Vector3 shoulder=GetBindWorldPosition(skeleton,upper);
        float upperLength=skeleton.Bones[elbow].LocalPosition.Length();
        Vector3 lowerBind=skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition;
        float lowerLength=lowerBind.Length();
        Vector3 handTarget=shoulder+new Vector3(side*125f,445f,105f);
        Vector3 toTarget=handTarget-shoulder;
        float distance=toTarget.Length();
        if(distance<0.001f || upperLength<0.001f || lowerLength<0.001f)return;
        Vector3 direction=toTarget/distance;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        handTarget=shoulder+direction*reachable;

        Vector3 pole=new Vector3(side,0.15f,-0.45f);
        pole-=direction*Vector3.Dot(pole,direction);
        if(pole.LengthSquared()<0.0001f) pole=Vector3.UnitZ-direction*Vector3.Dot(Vector3.UnitZ,direction);
        pole=Vector3.Normalize(pole);
        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 elbowTarget=shoulder+direction*along+pole*bend;

        Quaternion upperWorld=RotationBetween(skeleton.Bones[elbow].LocalPosition,elbowTarget-shoulder);
        Quaternion elbowWorld=RotationBetween(lowerBind,handTarget-elbowTarget);
        Quaternion elbowLocal=Quaternion.Normalize(Quaternion.Inverse(upperWorld)*elbowWorld);
        animation.Tracks.Add(CreateQuaternionRotation(upperId,upperWorld,frameCount,false));
        animation.Tracks.Add(CreateQuaternionRotation(elbowId,elbowLocal,frameCount,false));
    }

    private static void AddGroundSeatedLeg(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount)
    {
        byte thighId=right ? (byte)0x12 : (byte)0x16;
        byte calfId=right ? (byte)0x13 : (byte)0x17;
        byte footId=right ? (byte)0x14 : (byte)0x18;
        int thigh=FindBoneIndex(skeleton,thighId),calf=FindBoneIndex(skeleton,calfId),foot=FindBoneIndex(skeleton,footId);
        if(thigh<0 || calf<0 || foot<0 || skeleton.Bones[calf].ParentIndex!=thigh || skeleton.Bones[foot].ParentIndex!=calf)
            throw new InvalidDataException("Skeleton sem uma cadeia humanoide completa nas pernas.");

        float side=right ? -1f : 1f;
        Vector3 hip=GetBindWorldPosition(skeleton,thigh);
        Vector3 kneeDirection=Vector3.Normalize(new Vector3(side*75f,80f,470f));
        Vector3 knee=hip+kneeDirection*skeleton.Bones[calf].LocalPosition.Length();
        Vector3 ankleDirection=Vector3.Normalize(new Vector3(side*25f,-120f,450f));
        Vector3 ankle=knee+ankleDirection*skeleton.Bones[foot].LocalPosition.Length();

        Quaternion thighWorld=RotationBetween(skeleton.Bones[calf].LocalPosition,knee-hip);
        Quaternion calfWorld=RotationBetween(skeleton.Bones[foot].LocalPosition,ankle-knee);
        Quaternion calfLocal=Quaternion.Normalize(Quaternion.Inverse(thighWorld)*calfWorld);
        Quaternion footLocal=Quaternion.Normalize(Quaternion.Inverse(calfWorld));
        animation.Tracks.Add(CreateQuaternionRotation(thighId,thighWorld,frameCount,false));
        animation.Tracks.Add(CreateQuaternionRotation(calfId,calfLocal,frameCount,false));
        animation.Tracks.Add(CreateQuaternionRotation(footId,footLocal,frameCount,false));
    }

    private static void AddGroundSeatedArm(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount)
    {
        byte upperId=right ? (byte)0x07 : (byte)0x0D;
        byte elbowId=right ? (byte)0x08 : (byte)0x0E;
        byte wristId=right ? (byte)0x09 : (byte)0x0F;
        byte handId=right ? (byte)0x0A : (byte)0x10;
        byte thighId=right ? (byte)0x12 : (byte)0x16;
        byte calfId=right ? (byte)0x13 : (byte)0x17;
        int upper=FindBoneIndex(skeleton,upperId),elbow=FindBoneIndex(skeleton,elbowId);
        int wrist=FindBoneIndex(skeleton,wristId),hand=FindBoneIndex(skeleton,handId);
        int thigh=FindBoneIndex(skeleton,thighId),calf=FindBoneIndex(skeleton,calfId);
        if(upper<0 || elbow<0 || wrist<0 || hand<0 || thigh<0 || calf<0)
            throw new InvalidDataException("Skeleton sem as cadeias humanoides necessárias para apoiar os braços nos joelhos.");

        float side=right ? -1f : 1f;
        Vector3 shoulder=GetBindWorldPosition(skeleton,upper);
        Vector3 hip=GetBindWorldPosition(skeleton,thigh);
        // Place each hand beside its own hip, close to the ground after the root is lowered.
        // em36 has unusually long wrist/hand segments, so a target on the thigh makes the mesh
        // continue inward and overlap at the center even when the hand pivot itself is correct.
        Vector3 handTarget=hip+new Vector3(side*250f,-30f,80f);

        float upperLength=skeleton.Bones[elbow].LocalPosition.Length();
        Vector3 lowerBind=skeleton.Bones[wrist].LocalPosition+skeleton.Bones[hand].LocalPosition;
        float lowerLength=lowerBind.Length();
        Vector3 toTarget=handTarget-shoulder;
        float distance=toTarget.Length();
        if(distance<0.001f || upperLength<0.001f || lowerLength<0.001f)return;
        Vector3 direction=toTarget/distance;
        float reachable=Math.Clamp(distance,MathF.Abs(upperLength-lowerLength)+0.001f,upperLength+lowerLength-0.001f);
        handTarget=shoulder+direction*reachable;

        // Keep the elbows slightly outside and behind the torso for a relaxed supported seat.
        Vector3 pole=new Vector3(side,0.10f,-0.35f);
        pole-=direction*Vector3.Dot(pole,direction);
        if(pole.LengthSquared()<0.0001f)pole=Vector3.UnitZ-direction*Vector3.Dot(Vector3.UnitZ,direction);
        pole=Vector3.Normalize(pole);
        float along=(upperLength*upperLength-lowerLength*lowerLength+reachable*reachable)/(2f*reachable);
        float bend=MathF.Sqrt(MathF.Max(0f,upperLength*upperLength-along*along));
        Vector3 elbowTarget=shoulder+direction*along+pole*bend;

        Quaternion upperWorld=RotationBetween(skeleton.Bones[elbow].LocalPosition,elbowTarget-shoulder);
        Quaternion elbowWorld=RotationBetween(lowerBind,handTarget-elbowTarget);
        Quaternion elbowLocal=Quaternion.Normalize(Quaternion.Inverse(upperWorld)*elbowWorld);
        animation.Tracks.Add(CreateQuaternionRotation(upperId,upperWorld,frameCount,false));
        animation.Tracks.Add(CreateQuaternionRotation(elbowId,elbowLocal,frameCount,false));
        animation.Tracks.Add(CreateQuaternionRotation(handId,Quaternion.Identity,frameCount,false));
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

    private static void AddArmOnKnee(Ps2BinSkeleton skeleton,FcvAnimation animation,bool right,ushort frameCount,bool useLegacyEm12Profile=true)
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
        animation.Tracks.Add(CreateQuaternionRotation(upperId,upperWorld,frameCount,useLegacyEm12Profile));
        animation.Tracks.Add(CreateQuaternionRotation(elbowId,elbowLocal,frameCount,useLegacyEm12Profile));
        animation.Tracks.Add(CreateQuaternionRotation(handId,Quaternion.Identity,frameCount,useLegacyEm12Profile));
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

    private static FcvTrack CreateQuaternionRotation(byte nodeId,Quaternion rotation,ushort frameCount,bool useLegacyEm12Profile=true)
    {
        Vector3 euler=FindEvaluatorEuler(nodeId,rotation,useLegacyEm12Profile);
        return CreateStaticRotation(nodeId,euler.X,euler.Y,euler.Z,frameCount);
    }

    private static Vector3 FindEvaluatorEuler(byte nodeId,Quaternion target,bool useLegacyEm12Profile)
    {
        // Mirrored Z-Y-X arms have several distant Euler representations. Search coarse seeds
        // first, then refine the best basin; a single zero seed can converge to the wrong mirror.
        float[] seeds={-MathF.PI,-MathF.PI/2f,0f,MathF.PI/2f,MathF.PI};
        Vector3 bestAngles=Vector3.Zero;
        float bestError=float.PositiveInfinity;
        foreach(float x in seeds) foreach(float y in seeds) foreach(float z in seeds)
        {
            Vector3 angles=new(x,y,z);
            float error=QuaternionError(Compose(nodeId,angles,useLegacyEm12Profile),target);
            float step=MathF.PI/2f;
            for(int iteration=0;iteration<28;iteration++)
            {
                for(int axis=0;axis<3;axis++)
                {
                    Vector3 plus=angles,minus=angles;
                    if(axis==0){plus.X+=step;minus.X-=step;}
                    else if(axis==1){plus.Y+=step;minus.Y-=step;}
                    else {plus.Z+=step;minus.Z-=step;}
                    float plusError=QuaternionError(Compose(nodeId,plus,useLegacyEm12Profile),target);
                    float minusError=QuaternionError(Compose(nodeId,minus,useLegacyEm12Profile),target);
                    if(plusError<error && plusError<=minusError){angles=plus;error=plusError;}
                    else if(minusError<error){angles=minus;error=minusError;}
                }
                step*=0.5f;
            }
            angles=new Vector3(NormalizeAngle(angles.X),NormalizeAngle(angles.Y),NormalizeAngle(angles.Z));
            error=QuaternionError(Compose(nodeId,angles,useLegacyEm12Profile),target);
            float magnitude=angles.LengthSquared();
            float bestMagnitude=bestAngles.LengthSquared();
            if(error<bestError-0.0000001f || (MathF.Abs(error-bestError)<=0.0000001f && magnitude<bestMagnitude))
            {
                bestError=error;
                bestAngles=angles;
            }
        }
        return bestAngles;
    }

    private static float NormalizeAngle(float value)
    {
        value=MathF.IEEERemainder(value,MathF.Tau);
        if(value<=-MathF.PI)value+=MathF.Tau;
        if(value>MathF.PI)value-=MathF.Tau;
        return MathF.Abs(value)<0.000001f?0f:value;
    }

    private static Quaternion Compose(byte nodeId,Vector3 euler,bool useLegacyEm12Profile)
    {
        if(!useLegacyEm12Profile || nodeId>0x10) return Quaternion.CreateFromYawPitchRoll(euler.Y,euler.X,euler.Z);
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

    private static FcvTrack CreateRelativeMovement(byte nodeId,Vector3 value,ushort frameCount)
    {
        var track=new FcvTrack { NodeId=nodeId,Type=0x01,DataType=0x00 };
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
