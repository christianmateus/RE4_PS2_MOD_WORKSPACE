using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2CamWriter
{
    public static bool Save(CamScene scene,string? firstBackupPath)
    {
        if(scene==null)throw new ArgumentNullException(nameof(scene));
        if(!File.Exists(scene.SourcePath))throw new FileNotFoundException("CAM original não encontrado.",scene.SourcePath);
        bool structural=scene.StructureModified||scene.Entries.Count!=scene.OriginalEntryCount||scene.CameraRecords.Count!=scene.OriginalCameraCount;byte[] data=structural?Rebuild(scene):(byte[])scene.OriginalData.Clone();if(!structural)Patch(scene,data);
        bool backup=false;
        if(!string.IsNullOrWhiteSpace(firstBackupPath)&&!File.Exists(firstBackupPath)){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(firstBackupPath))!);File.Copy(scene.SourcePath,firstBackupPath,false);backup=true;}
        string temp=scene.SourcePath+".workspace_save_tmp";try{File.WriteAllBytes(temp,data);File.Move(temp,scene.SourcePath,true);}finally{if(File.Exists(temp))File.Delete(temp);}
        scene.OriginalData=data;scene.IsModified=false;return backup;
    }

    public static bool HasEditableChanges(CamScene scene){if(scene.IsModified||scene.StructureModified||scene.Entries.Count!=scene.OriginalEntryCount||scene.CameraRecords.Count!=scene.OriginalCameraCount)return true;byte[] test=(byte[])scene.OriginalData.Clone();Patch(scene,test);return !test.AsSpan().SequenceEqual(scene.OriginalData);}

    private static byte[] Rebuild(CamScene scene)
    {
        int entryCount=scene.Entries.Count,cameraCount=scene.CameraRecords.Count;if(entryCount>255||cameraCount>255)throw new InvalidDataException("CAM aceita no máximo 255 zonas e 255 câmeras.");
        int header=Ps2CamReader.HeaderSize,entriesStart=header,areasStart=entriesStart+entryCount*Ps2CamReader.EntrySize,camerasStart=areasStart+entryCount*Ps2CamReader.AreaSize,verticesStart=camerasStart+cameraCount*Ps2CamReader.CameraSize+scene.PreVertexData.Length;
        int cursor=verticesStart+scene.Entries.Sum(x=>x.Vertices.Count*12);
        var pos=new int[cameraCount];var target=new int[cameraCount];var roll=new int[cameraCount];var fov=new int[cameraCount];var times=new int[cameraCount];
        for(int i=0;i<cameraCount;i++){int n=scene.CameraRecords[i].Frames.Count;pos[i]=cursor;cursor+=n*16;target[i]=cursor;cursor+=n*16;roll[i]=cursor;cursor+=n*4;fov[i]=cursor;cursor+=n*4;}
        for(int i=0;i<cameraCount;i++)if((byte)scene.CameraRecords[i].Type==6&&scene.CameraRecords[i].Frames.Count>0){times[i]=cursor;cursor+=scene.CameraRecords[i].Frames.Count*2;}
        int tailPadding=(0x40-(cursor&0x3F))&0x3F;
        byte[] d=new byte[checked(cursor+tailPadding)];Buffer.BlockCopy(scene.Header,0,d,0,Math.Min(header,scene.Header.Length));d[4]=(byte)cameraCount;d[5]=(byte)entryCount;
        if(scene.PreVertexData.Length>0)Buffer.BlockCopy(scene.PreVertexData,0,d,camerasStart+cameraCount*Ps2CamReader.CameraSize,scene.PreVertexData.Length);
        int vertexCursor=verticesStart;
        for(int i=0;i<entryCount;i++)
        {
            CamEntry e=scene.Entries[i];e.FileOrder=i;int eo=entriesStart+i*Ps2CamReader.EntrySize,ao=areasStart+i*Ps2CamReader.AreaSize;
            CopySized(e.EntryRaw,d,eo,Ps2CamReader.EntrySize);CopySized(e.AreaRaw,d,ao,Ps2CamReader.AreaSize);
            d[eo]=(byte)e.AreaAttributes;WriteU32(d,eo+8,(uint)ao);WriteU32(d,eo+12,e.Camera==null?0u:(uint)(camerasStart+e.Camera.FileOrder*Ps2CamReader.CameraSize));
            d[ao]=e.AreaEnabled;d[ao+1]=e.Index;d[ao+2]=e.SubIndex;d[ao+3]=(byte)e.AreaAttributes;WriteF32(d,ao+4,e.FacingRadians);WriteF32(d,ao+0x20,e.UpperY);WriteF32(d,ao+0x24,e.LowerY);WriteU32(d,ao+0x28,(uint)e.Vertices.Count);WriteU32(d,ao+0x2C,(uint)vertexCursor);
            foreach(CamVertex v in e.Vertices){WriteV3(d,vertexCursor,v.Position);vertexCursor+=12;}
        }
        for(int i=0;i<cameraCount;i++){CamCameraRecord c=scene.CameraRecords[i];c.FileOrder=i;int co=camerasStart+i*Ps2CamReader.CameraSize;CopySized(c.Raw,d,co,Ps2CamReader.CameraSize);d[co]=c.Enabled;d[co+1]=c.Index;d[co+2]=(byte)c.Type;d[co+3]=(byte)c.Attributes;WriteV3(d,co+4,c.OffsetVector);WriteU32(d,co+0x20,(uint)c.Frames.Count);WriteU32(d,co+0x24,(uint)pos[i]);WriteU32(d,co+0x28,(uint)target[i]);WriteU32(d,co+0x2C,(uint)roll[i]);WriteU32(d,co+0x30,(uint)fov[i]);if(times[i]>0)WriteU32(d,co+0x10,(uint)times[i]);for(int f=0;f<c.Frames.Count;f++){CamFrame frame=c.Frames[f];WriteV3(d,pos[i]+f*16,frame.Position);WriteU32(d,pos[i]+f*16+12,frame.PositionSuffix);WriteV3(d,target[i]+f*16,frame.Target);WriteU32(d,target[i]+f*16+12,frame.TargetSuffix);WriteF32(d,roll[i]+f*4,frame.Roll);WriteF32(d,fov[i]+f*4,frame.Fov);if(times[i]>0)WriteU16(d,times[i]+f*2,frame.Time);}}
        if(tailPadding>0)d.AsSpan(cursor,tailPadding).Fill(0xCD);return d;
    }

    private static void CopySized(byte[] source,byte[] target,int offset,int size){if(source.Length>0)Buffer.BlockCopy(source,0,target,offset,Math.Min(size,source.Length));}

    private static void Patch(CamScene scene,byte[] d)
    {
        foreach(CamEntry e in scene.Entries)
        {
            if(e.AreaRaw.Length>3&&e.AreaRaw[3]!=(byte)e.AreaAttributes)d[e.EntryOffset]=(byte)e.AreaAttributes;
            d[e.AreaOffset+3]=(byte)e.AreaAttributes;
            d[e.AreaOffset]=e.AreaEnabled;d[e.AreaOffset+1]=e.Index;d[e.AreaOffset+2]=e.SubIndex;WriteF32(d,e.AreaOffset+4,e.FacingRadians);WriteF32(d,e.AreaOffset+0x20,e.UpperY);WriteF32(d,e.AreaOffset+0x24,e.LowerY);
            foreach(CamVertex v in e.Vertices)WriteV3(d,v.FileOffset,v.Position);
        }
        foreach(CamCameraRecord c in scene.CameraRecords){d[c.FileOffset]=c.Enabled;d[c.FileOffset+1]=c.Index;d[c.FileOffset+2]=(byte)c.Type;d[c.FileOffset+3]=(byte)c.Attributes;WriteV3(d,c.FileOffset+4,c.OffsetVector);foreach(CamFrame f in c.Frames){WriteV3(d,f.PositionOffset,f.Position);WriteV3(d,f.TargetOffset,f.Target);WriteF32(d,f.RollOffset,f.Roll);WriteF32(d,f.FovOffset,f.Fov);if(f.TimeOffset is int t)WriteU16(d,t,f.Time);}}
    }
    private static void WriteV3(byte[] d,int o,Vector3 v){WriteF32(d,o,v.X);WriteF32(d,o+4,v.Y);WriteF32(d,o+8,v.Z);}
    private static void WriteF32(byte[] d,int o,float v){if(!float.IsFinite(v))throw new InvalidDataException($"Valor CAM não finito em 0x{o:X}.");byte[] raw=BitConverter.GetBytes(v);Buffer.BlockCopy(raw,0,d,o,4);}
    private static void WriteU32(byte[] d,int o,uint v)=>Buffer.BlockCopy(BitConverter.GetBytes(v),0,d,o,4);
    private static void WriteU16(byte[] d,int o,ushort v)=>Buffer.BlockCopy(BitConverter.GetBytes(v),0,d,o,2);
}
