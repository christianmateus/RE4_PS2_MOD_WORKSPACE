using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

public sealed class FcvSkeletonPose
{
    public Vector3[] LocalPositions { get; init; } = Array.Empty<Vector3>();
    public Quaternion[] LocalRotations { get; init; } = Array.Empty<Quaternion>();
    public Vector3[] WorldPositions { get; init; } = Array.Empty<Vector3>();
    public Quaternion[] WorldRotations { get; init; } = Array.Empty<Quaternion>();
}

public static class FcvSkeletonEvaluator
{
    public static FcvSkeletonPose Evaluate(Ps2BinSkeleton skeleton, FcvAnimation? animation, float frame)
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
                // Conservative FCV playback while the PS2 transform semantics are being validated:
                // - 0x02 is the only rotation type the reference MaxScript actually imports.
                // - encodings 0x00/0x10/0x50/0x60 are the rotation encodings handled by that importer.
                // - 0x10 (documented as "absolute?") is intentionally NOT treated as normal FK rotation yet.
                if(t.Type==0x02 && IsSupportedRotationEncoding(enc))
                {
                    // FCV rotation tracks use several storage precisions. The old evaluator only
                    // accepted float/int16 encodings (00/10/50/60), so compressed 8-bit tracks
                    // such as A0 were silently ignored. em12 uses A0 rotations extensively on
                    // the lower-body chain (hips/legs/feet), which made those bones inherit the
                    // parent's rotation and visually "follow the hip".
                    float rx=RotationValue(EvalOrZero(t.X,frame),enc), ry=RotationValue(EvalOrZero(t.Y,frame),enc), rz=RotationValue(EvalOrZero(t.Z,frame),enc);
                    localRot[bi]=Quaternion.CreateFromYawPitchRoll(ry,rx,rz);
                }
                // Type 0x04 is documented/used as RELATIVE translation. The BIN bone position is the
                // rest/base position, so the FCV values must be added as a delta. Replacing the BIN
                // position with the FCV value made the root jump from Y ~= 1140 to Y ~= 0 and pushed
                // the whole skeleton to the bottom/outside of the viewport during PLAY.
                // Keep this conservative for now: only apply the root float translation.
                else if(t.Type==0x04 && skeleton.Bones[bi].ParentIndex<0 && (enc==0x0 || enc==0x1 || enc==0x2))
                {
                    // Keep only root translation active. Non-root 0x04 / 0x10 tracks are exposed
                    // by the diagnostic UI but are not applied until their exact PS2 semantics
                    // are confirmed. The previous guessed two-bone IK could stretch a leg across
                    // the entire scene.
                    Vector3 value = new((float)EvalOrZero(t.X,frame),(float)EvalOrZero(t.Y,frame),(float)EvalOrZero(t.Z,frame));
                    localPos[bi]=skeleton.Bones[bi].LocalPosition+value;
                }
                // 0x01 movement, 0x08 scale, 0x20 toe IK, 0x40 root rotation and 0x80/0xA0
                // special tracks remain conservative until their exact semantics are validated.
            }
        }
        for(int i=0;i<n;i++) ResolveWorld(i,skeleton,localPos,localRot,wp,wr,new byte[n]);
        return new FcvSkeletonPose{LocalPositions=localPos,LocalRotations=localRot,WorldPositions=wp,WorldRotations=wr};
    }

    private static int FindBoneIndex(Ps2BinSkeleton s, byte id){ if(s.FirstIndexById.TryGetValue(id,out int i)) return i; return -1; }
    private static void ResolveWorld(int i, Ps2BinSkeleton s, Vector3[] lp, Quaternion[] lr, Vector3[] wp, Quaternion[] wr, byte[] state)
    {
        if(state[i]==2)return; if(state[i]==1){wp[i]=lp[i];wr[i]=lr[i];state[i]=2;return;} state[i]=1;
        int p=s.Bones[i].ParentIndex;
        if(p<0){wp[i]=lp[i];wr[i]=lr[i];}
        else {ResolveWorld(p,s,lp,lr,wp,wr,state); wr[i]=Quaternion.Normalize(wr[p]*lr[i]); wp[i]=wp[p]+Vector3.Transform(lp[i],wr[p]);}
        state[i]=2;
    }
    private static bool IsSupportedRotationEncoding(int encoding)
        => encoding is 0x0 or 0x1 or 0x5 or 0x6 or 0x8 or 0x9 or 0xA;

    private static float RotationValue(double value,int encoding)
    {
        // 00/10 store the angle directly as float radians.
        // 50/60 store the value in signed 16-bit normalized angular space.
        // 80/90/A0 use the same angular range quantized to signed 8-bit.
        // Supporting A0 is the important lower-body fix for em12 FCV 001.
        return encoding switch
        {
            0x0 or 0x1 => (float)value,
            0x5 or 0x6 => (float)(value / 32767.0 * Math.PI),
            0x8 or 0x9 or 0xA => (float)(value / 127.0 * Math.PI),
            _ => 0f
        };
    }
    public static double SampleAxis(FcvAxis axis, float frame) => axis.Keys.Count == 0 ? 0.0 : Eval(axis, frame);
    public static Vector3 SampleTrackRaw(FcvTrack track, float frame)
        => new((float)SampleAxis(track.X, frame), (float)SampleAxis(track.Y, frame), (float)SampleAxis(track.Z, frame));

    private static float EvalOr(FcvAxis a,float frame,float fallback) => a.Keys.Count==0 ? fallback : (float)Eval(a,frame);
    private static double EvalOrZero(FcvAxis a,float frame) => a.Keys.Count==0 ? 0.0 : Eval(a,frame);
    private static double Eval(FcvAxis a,float frame)
    {
        if(a.Keys.Count==0)return 0; if(a.Keys.Count==1)return a.Keys[0].Value;
        if(frame<=a.Keys[0].Frame)return a.Keys[0].Value; if(frame>=a.Keys[^1].Frame)return a.Keys[^1].Value;
        int hi=1; while(hi<a.Keys.Count && a.Keys[hi].Frame<frame)hi++; var k0=a.Keys[hi-1]; var k1=a.Keys[hi];
        float span=Math.Max(1,k1.Frame-k0.Frame); float u=(frame-k0.Frame)/span;
        return k0.Value+(k1.Value-k0.Value)*u;
    }
}
