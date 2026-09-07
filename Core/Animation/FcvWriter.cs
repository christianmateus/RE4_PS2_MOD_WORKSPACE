using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

/// <summary>Writes the FCV layout currently understood by <see cref="FcvReader"/>.</summary>
public static class FcvWriter
{
    public static void Write(string path,FcvAnimation animation)
    {
        byte[] bytes=Write(animation);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path,bytes);
    }

    public static byte[] Write(FcvAnimation animation)
    {
        if(animation.Tracks.Count==0 || animation.Tracks.Count>byte.MaxValue)
            throw new InvalidDataException("FCV precisa conter entre 1 e 255 tracks.");

        using var stream=new MemoryStream();
        using var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,leaveOpen:true);
        writer.Write(animation.FrameCount);
        writer.Write((byte)animation.Tracks.Count);
        foreach(FcvTrack track in animation.Tracks){writer.Write(track.Type);writer.Write(track.DataType);}
        foreach(FcvTrack track in animation.Tracks) writer.Write(track.NodeId);
        while((stream.Position&3)!=0) writer.Write((byte)0);

        long sizePosition=stream.Position;
        writer.Write(0u);
        long offsetsPosition=stream.Position;
        for(int i=0;i<animation.Tracks.Count;i++) writer.Write(0u);

        var offsets=new uint[animation.Tracks.Count];
        for(int i=0;i<animation.Tracks.Count;i++)
        {
            offsets[i]=checked((uint)stream.Position);
            FcvTrack track=animation.Tracks[i];
            int encoding=track.DataType>>4;
            WriteAxis(writer,track.X,encoding);
            WriteAxis(writer,track.Y,encoding);
            WriteAxis(writer,track.Z,encoding);
        }

        byte[] result=stream.ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(checked((int)sizePosition),4),checked((uint)result.Length));
        for(int i=0;i<offsets.Length;i++)
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(checked((int)offsetsPosition+i*4),4),offsets[i]);
        return result;
    }

    private static void WriteAxis(BinaryWriter writer,FcvAxis axis,int encoding)
    {
        if(axis.Keys.Count==0 || axis.Keys.Count>ushort.MaxValue) throw new InvalidDataException("Eixo FCV sem keys ou grande demais.");
        writer.Write((ushort)axis.Keys.Count);
        foreach(FcvKey key in axis.Keys) writer.Write(key.Frame);
        foreach(FcvKey key in axis.Keys)
        {
            switch(encoding)
            {
                case 0x0: writer.Write((float)key.Value);writer.Write((float)key.TangentIn);writer.Write((float)key.TangentOut);break;
                case 0x1: writer.Write((float)key.Value);writer.Write(checked((short)key.TangentIn));writer.Write(checked((short)key.TangentOut));break;
                case 0x2: writer.Write(checked((short)key.Value));writer.Write(checked((short)key.TangentIn));writer.Write(checked((short)key.TangentOut));break;
                case 0x4: writer.Write(checked((short)key.Value));writer.Write((float)key.TangentIn);writer.Write((float)key.TangentOut);break;
                case 0x5: writer.Write(checked((short)key.Value));writer.Write(checked((short)key.TangentIn));writer.Write(unchecked((ushort)(short)key.TangentOut));break;
                case 0x6: writer.Write(checked((short)key.Value));writer.Write(checked((sbyte)key.TangentIn));writer.Write(checked((sbyte)key.TangentOut));break;
                case 0x8: writer.Write(checked((sbyte)key.Value));writer.Write((float)key.TangentIn);writer.Write((float)key.TangentOut);break;
                case 0x9: writer.Write(checked((sbyte)key.Value));writer.Write(checked((short)key.TangentIn));writer.Write(checked((short)key.TangentOut));break;
                case 0xA: writer.Write(checked((sbyte)key.Value));writer.Write(checked((sbyte)key.TangentIn));writer.Write(checked((sbyte)key.TangentOut));break;
                case 0xF: writer.Write(checked((sbyte)key.Value));writer.Write(checked((byte)key.TangentIn));writer.Write(checked((byte)key.TangentOut));writer.Write(checked((byte)key.Extra));break;
                default: throw new InvalidDataException($"Encoding FCV desconhecido: 0x{encoding:X1}0");
            }
        }
    }
}
