using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2CamReader
{
    public const int HeaderSize=0x10, EntrySize=0x10, AreaSize=0x30, CameraSize=0x34;

    public static CamScene Read(string path)
    {
        byte[] data=File.ReadAllBytes(path);
        if(data.Length<HeaderSize)throw new InvalidDataException("CAM muito pequeno.");
        if(data[0]!=(byte)'B'||data[1]!=(byte)'4'||data[2]!=(byte)'0')throw new InvalidDataException("Assinatura CAM PS2 inválida.");
        int cameraCount=data[4],entryCount=data[5];
        int entriesStart=HeaderSize,areasStart=entriesStart+entryCount*EntrySize,camerasStart=areasStart+entryCount*AreaSize,cameraTableEnd=camerasStart+cameraCount*CameraSize;
        if(cameraTableEnd>data.Length)throw new InvalidDataException("Tabelas CAM truncadas.");
        var cameras=new List<CamCameraRecord>();var camerasByOffset=new Dictionary<int,CamCameraRecord>();
        for(int i=0;i<cameraCount;i++){int offset=camerasStart+i*CameraSize;CamCameraRecord camera=ReadCamera(data,i,offset);cameras.Add(camera);camerasByOffset[offset]=camera;}
        var entries=new List<CamEntry>();for(int i=0;i<entryCount;i++)entries.Add(ReadEntry(data,i,camerasByOffset));
        int firstVertex=entries.SelectMany(x=>x.Vertices).Select(x=>x.FileOffset).DefaultIfEmpty(cameraTableEnd).Min();
        int tailStart=data.Length;while(tailStart>0&&data[tailStart-1]==0xCD)tailStart--;
        var scene=new CamScene{SourcePath=path,OriginalData=(byte[])data.Clone(),Header=data[..HeaderSize],OriginalEntryCount=entryCount,OriginalCameraCount=cameraCount,PreVertexData=firstVertex>=cameraTableEnd?data.AsSpan(cameraTableEnd,firstVertex-cameraTableEnd).ToArray():Array.Empty<byte>(),TailPadding=tailStart<data.Length?data[tailStart..]:Array.Empty<byte>(),Entries=entries,CameraRecords=cameras};
        return scene;
    }

    private static CamEntry ReadEntry(byte[] d,int index,Dictionary<int,CamCameraRecord> cameras)
    {
        int eo=HeaderSize+index*EntrySize,ao=CheckedOffset(d,ReadU32(d,eo+8),AreaSize,"Area Properties");uint cameraPointer=ReadU32(d,eo+12);CamCameraRecord? camera=null;if(cameraPointer!=0&&!cameras.TryGetValue((int)cameraPointer,out camera))throw new InvalidDataException($"CAM entry {index+1}: referência de câmera inválida 0x{cameraPointer:X}.");
        int vertexCount=checked((int)ReadU32(d,ao+0x28)),vertexOffset=CheckedOffset(d,ReadU32(d,ao+0x2C),checked(vertexCount*12),"vértices");
        if(vertexCount>32)throw new InvalidDataException($"CAM entry {index+1}: contagem fora do limite seguro.");
        var e=new CamEntry{FileOrder=index,EntryOffset=eo,AreaOffset=ao,EntryRaw=d.AsSpan(eo,EntrySize).ToArray(),AreaRaw=d.AsSpan(ao,AreaSize).ToArray(),AreaEnabled=d[ao],Index=d[ao+1],SubIndex=d[ao+2],AreaAttributes=(CamAreaAttributes)d[ao+3],FacingRadians=ReadF32(d,ao+4),UpperY=ReadF32(d,ao+0x20),LowerY=ReadF32(d,ao+0x24),Camera=camera};
        for(int v=0;v<vertexCount;v++)e.Vertices.Add(new CamVertex{Index=v,FileOffset=vertexOffset+v*12,Position=ReadV3(d,vertexOffset+v*12)});
        return e;
    }

    private static CamCameraRecord ReadCamera(byte[] d,int index,int co)
    {
        int frameCount=checked((int)ReadU32(d,co+0x20));if(frameCount>1024)throw new InvalidDataException($"CAM camera {index+1}: contagem fora do limite seguro.");
        var camera=new CamCameraRecord{FileOrder=index,FileOffset=co,Raw=d.AsSpan(co,CameraSize).ToArray(),Enabled=d[co],Index=d[co+1],Type=(CamType)d[co+2],Attributes=(CamCameraAttributes)d[co+3],OffsetVector=ReadV3(d,co+4)};
        if(frameCount==0)return camera;
        int pp=CheckedOffset(d,ReadU32(d,co+0x24),checked(frameCount*16),"posições"),tp=CheckedOffset(d,ReadU32(d,co+0x28),checked(frameCount*16),"alvos"),rp=CheckedOffset(d,ReadU32(d,co+0x2C),checked(frameCount*4),"roll"),fp=CheckedOffset(d,ReadU32(d,co+0x30),checked(frameCount*4),"FOV");
        int aux=(int)ReadU32(d,co+0x10);bool hasTimes=(byte)camera.Type==6&&aux>=0&&aux+frameCount*2<=d.Length;
        for(int f=0;f<frameCount;f++)camera.Frames.Add(new CamFrame{Index=f,PositionOffset=pp+f*16,TargetOffset=tp+f*16,RollOffset=rp+f*4,FovOffset=fp+f*4,TimeOffset=hasTimes?aux+f*2:null,PositionSuffix=ReadU32(d,pp+f*16+12),TargetSuffix=ReadU32(d,tp+f*16+12),Position=ReadV3(d,pp+f*16),Target=ReadV3(d,tp+f*16),Roll=ReadF32(d,rp+f*4),Fov=ReadF32(d,fp+f*4),Time=hasTimes?BitConverter.ToUInt16(d,aux+f*2):(ushort)0});
        return camera;
    }

    private static int CheckedOffset(byte[] data,uint value,int size,string name){if(value>int.MaxValue||value+size>data.Length)throw new InvalidDataException($"Offset de {name} fora do arquivo: 0x{value:X}.");return (int)value;}
    private static uint ReadU32(byte[] d,int o)=>BitConverter.ToUInt32(d,o);
    private static float ReadF32(byte[] d,int o){float v=BitConverter.ToSingle(d,o);if(!float.IsFinite(v))throw new InvalidDataException($"Float CAM inválido em 0x{o:X}.");return v;}
    private static Vector3 ReadV3(byte[] d,int o)=>new(ReadF32(d,o),ReadF32(d,o+4),ReadF32(d,o+8));
}
