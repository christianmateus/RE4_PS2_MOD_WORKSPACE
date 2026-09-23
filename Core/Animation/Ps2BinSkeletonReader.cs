using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

public sealed class Ps2BinSkeleton
{
    public string FilePath { get; init; } = "";
    public uint ModelVersion { get; init; }
    public List<Ps2BinBone> Bones { get; } = new();
    public List<Ps2BinJointBlend> JointBlends { get; } = new();
    public IReadOnlyDictionary<byte, int> FirstIndexById => _firstIndexById;
    private readonly Dictionary<byte, int> _firstIndexById = new();
    internal void BuildLookup() { _firstIndexById.Clear(); for (int i=0;i<Bones.Count;i++) if (!_firstIndexById.ContainsKey(Bones[i].Id)) _firstIndexById[Bones[i].Id]=i; }
}

public readonly record struct Ps2BinJointBlend(ushort Destination,ushort A,ushort C,ushort Percent);

public sealed class Ps2BinBone
{
    public int Index { get; init; }
    public byte Id { get; init; }
    public byte ParentId { get; init; }
    public int ParentIndex { get; set; } = -1;
    public Vector3 LocalPosition { get; init; }
}

public static class Ps2BinSkeletonReader
{
    public static Ps2BinSkeleton Read(string path)
    {
        using var fs = File.OpenRead(path);
        Ps2BinSkeleton skel = Read(fs, path);
        return skel;
    }

    public static Ps2BinSkeleton Read(byte[] data, string sourceName = "embedded.bin")
    {
        using var ms = new MemoryStream(data, writable: false);
        return Read(ms, sourceName);
    }

    public static Ps2BinSkeleton Read(Stream fs, string sourceName = "embedded.bin")
    {
        using var br = new BinaryReader(fs, System.Text.Encoding.Default, leaveOpen: true);
        if (fs.Length < 0x50) throw new InvalidDataException("BIN muito pequeno.");
        ushort magic=br.ReadUInt16(); if (magic!=0x0030) throw new InvalidDataException($"BIN inválido: magic 0x{magic:X4} (esperado 0x0030).");
        _=br.ReadUInt16(); uint bonesPoint=br.ReadUInt32(); _=br.ReadByte(); byte boneCount=br.ReadByte();
        if (boneCount==0) throw new InvalidDataException("BIN não possui bones.");
        if (bonesPoint==0 || bonesPoint + boneCount*16L > fs.Length) throw new InvalidDataException("Tabela de bones fora do arquivo.");
        uint version=0;
        if(fs.Length>=0x20)
        {
            fs.Position=0x18;
            version=br.ReadUInt32();
        }
        var skel=new Ps2BinSkeleton{FilePath=sourceName,ModelVersion=version}; fs.Position=bonesPoint;
        for(int i=0;i<boneCount;i++)
        {
            byte id=br.ReadByte(); byte parent=br.ReadByte(); br.ReadUInt16(); float x=br.ReadSingle(), y=br.ReadSingle(), z=br.ReadSingle();
            skel.Bones.Add(new Ps2BinBone{Index=i,Id=id,ParentId=parent,LocalPosition=new Vector3(x,y,z)});
        }
        skel.BuildLookup();
        for(int i=0;i<skel.Bones.Count;i++)
        {
            var b=skel.Bones[i];
            if (b.ParentId==0xFF) { b.ParentIndex=-1; continue; }
            // The format references parents by bone ID. Prefer the nearest earlier matching ID.
            int parent=-1; for(int p=i-1;p>=0;p--) if(skel.Bones[p].Id==b.ParentId){parent=p;break;}
            if(parent<0 && skel.FirstIndexById.TryGetValue(b.ParentId,out int first)) parent=first;
            b.ParentIndex=parent;
        }
        // PS2 ModelData places version/blendTbl at 0x18/0x1C. Version 0x20030818
        // stores the same table used by MotionMove after IK: destination rotation is
        // slerp(C, A, percent/100). Player models rely on these double joints heavily.
        if(version==0x20030818 && fs.Length>=0x20)
        {
            fs.Position=0x1C;
            uint blendOffset=br.ReadUInt32();
            if(blendOffset!=0 && blendOffset+4L<=fs.Length)
            {
                fs.Position=blendOffset;
                int blendCount=br.ReadInt32();
                if(blendCount>=0 && blendCount<=4096 && blendOffset+4L+blendCount*8L<=fs.Length)
                {
                    for(int i=0;i<blendCount;i++)
                    {
                        ushort destination=br.ReadUInt16(),a=br.ReadUInt16(),c=br.ReadUInt16(),percent=br.ReadUInt16();
                        if(destination<boneCount && a<boneCount && c<boneCount)
                            skel.JointBlends.Add(new Ps2BinJointBlend(destination,a,c,percent));
                    }
                }
            }
        }
        return skel;
    }
}
