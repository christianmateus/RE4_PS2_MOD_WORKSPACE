namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class SmdEmbeddedBinService
{
    public static void RemoveEntries(string smdPath,IReadOnlyCollection<int> fileOrders,int entryCount,int binCount)
    {
        if(fileOrders.Count==0)return;var remove=fileOrders.ToHashSet();if(remove.Any(x=>x<0||x>=entryCount))throw new ArgumentOutOfRangeException(nameof(fileOrders));if(remove.Count>=entryCount)throw new InvalidOperationException("O SMD precisa manter pelo menos uma entry.");
        byte[] data=File.ReadAllBytes(smdPath);int table=checked((int)BitConverter.ToUInt32(data,4));int tplOffset=checked((int)BitConverter.ToUInt32(data,8));int oldEntriesEnd=checked(0x10+entryCount*0x40);if(oldEntriesEnd>table)throw new InvalidDataException("Tabela de entries SMD inválida.");
        int kept=entryCount-remove.Count,delta=remove.Count*0x40,newTable=table-delta,newTplOffset=tplOffset-delta;byte[] output=new byte[data.Length-delta];Buffer.BlockCopy(data,0,output,0,0x10);int cursor=0x10;
        for(int i=0;i<entryCount;i++)if(!remove.Contains(i)){Buffer.BlockCopy(data,0x10+i*0x40,output,cursor,0x40);cursor+=0x40;}
        Buffer.BlockCopy(data,oldEntriesEnd,output,cursor,data.Length-oldEntriesEnd);BitConverter.GetBytes((ushort)kept).CopyTo(output,2);BitConverter.GetBytes((uint)newTable).CopyTo(output,4);BitConverter.GetBytes((uint)newTplOffset).CopyTo(output,8);AtomicWrite(smdPath,output);
    }
    public static int AppendEntry(string smdPath, ScenarioEntry template, int entryCount, int binCount, byte[]? newBin = null)
    {
        byte[] data=File.ReadAllBytes(smdPath);int table=checked((int)BitConverter.ToUInt32(data,4));int tplOffset=checked((int)BitConverter.ToUInt32(data,8));
        int oldEntriesEnd=checked(0x10+entryCount*0x40);if(oldEntriesEnd>table||table+binCount*4>data.Length)throw new InvalidDataException("Estrutura SMD inválida.");
        var bins=new List<byte[]>(binCount+(newBin==null?0:1));var offsets=new uint[binCount];for(int i=0;i<binCount;i++)offsets[i]=BitConverter.ToUInt32(data,table+i*4);
        for(int i=0;i<binCount;i++)
        {
            if(offsets[i]==0){bins.Add(Array.Empty<byte>());continue;}int start=checked(table+(int)offsets[i]);int end=tplOffset;for(int j=i+1;j<binCount;j++)if(offsets[j]!=0){end=checked(table+(int)offsets[j]);break;}bins.Add(data.AsSpan(start,end-start).ToArray());
        }
        if(newBin!=null){int aligned=(newBin.Length+15)&~15;byte[] padded=Enumerable.Repeat((byte)0xCD,aligned).ToArray();Buffer.BlockCopy(newBin,0,padded,0,newBin.Length);bins.Add(padded);}
        int newBinId=newBin==null?template.BinId:binCount,newEntryCount=entryCount+1,newBinCount=bins.Count;
        if(newBinId>byte.MaxValue)throw new InvalidDataException("O SMD atingiu o limite de 256 BINs.");
        int newTable=checked(table+0x40);int oldFirst=offsets.Where(x=>x!=0).Select(x=>(int)x).DefaultIfEmpty(binCount*4).Min();int oldGap=Math.Max(0,oldFirst-binCount*4);int cursor=Align16(newBinCount*4+oldGap);
        var newOffsets=new uint[newBinCount];for(int i=0;i<bins.Count;i++){if(bins[i].Length==0)continue;newOffsets[i]=(uint)cursor;cursor=checked(cursor+bins[i].Length);}
        int newTplOffset=checked(newTable+cursor);byte[] tail=data.AsSpan(tplOffset).ToArray();byte[] output=Enumerable.Repeat((byte)0xCD,checked(newTplOffset+tail.Length)).ToArray();
        Buffer.BlockCopy(data,0,output,0,oldEntriesEnd);byte[] raw=template.RawData.Length==0x40?(byte[])template.RawData.Clone():new byte[0x40];WriteEntry(raw,template,(byte)newBinId);Buffer.BlockCopy(raw,0,output,oldEntriesEnd,0x40);
        if(table>oldEntriesEnd)Buffer.BlockCopy(data,oldEntriesEnd,output,oldEntriesEnd+0x40,table-oldEntriesEnd);
        BitConverter.GetBytes((ushort)newEntryCount).CopyTo(output,2);BitConverter.GetBytes((uint)newTable).CopyTo(output,4);BitConverter.GetBytes((uint)newTplOffset).CopyTo(output,8);
        for(int i=0;i<newOffsets.Length;i++)BitConverter.GetBytes(newOffsets[i]).CopyTo(output,newTable+i*4);
        for(int i=0;i<bins.Count;i++)if(newOffsets[i]!=0)Buffer.BlockCopy(bins[i],0,output,newTable+(int)newOffsets[i],bins[i].Length);
        Buffer.BlockCopy(tail,0,output,newTplOffset,tail.Length);AtomicWrite(smdPath,output);return newEntryCount-1;
    }

    public static void SetMaterialTexture(string smdPath,int binId,int binCount,byte textureIndex)
    {
        byte[] bin=Extract(smdPath,binId,binCount);if(bin.Length<0x20)throw new InvalidDataException("BIN inválido.");ushort count=BitConverter.ToUInt16(bin,0x0A);uint materialOffset=BitConverter.ToUInt32(bin,0x0C);
        if(count==0||materialOffset==0||materialOffset+count*16L>bin.Length)throw new InvalidDataException($"BIN {binId} não possui materiais editáveis.");
        for(int i=0;i<count;i++)bin[checked((int)materialOffset+i*16+1)]=textureIndex;Replace(smdPath,binId,binCount,bin);
    }
    public static void SetBinMaterialTexture(byte[] bin,byte textureIndex)
    {
        if(bin.Length<0x20)throw new InvalidDataException("BIN inválido.");ushort count=BitConverter.ToUInt16(bin,0x0A);uint materialOffset=BitConverter.ToUInt32(bin,0x0C);if(count==0||materialOffset==0||materialOffset+count*16L>bin.Length)throw new InvalidDataException("O BIN convertido não possui materiais editáveis.");for(int i=0;i<count;i++)bin[checked((int)materialOffset+i*16+1)]=textureIndex;
    }
    public static void SetBinVertexPositions(string smdPath,byte binId,int binCount,IReadOnlyDictionary<int,(System.Numerics.Vector3 Position,float Factor)> edits)
    {
        byte[] bin=Extract(smdPath,binId,binCount);foreach(var pair in edits){int o=pair.Key;if(o<0||o+6>bin.Length)throw new InvalidDataException("Offset de vértice BIN inválido.");float factor=MathF.Abs(pair.Value.Factor)<.0000001f?1f:pair.Value.Factor;short V(float value)=>checked((short)Math.Clamp((int)MathF.Round(value*100f/factor),short.MinValue,short.MaxValue));BitConverter.GetBytes(V(pair.Value.Position.X)).CopyTo(bin,o);BitConverter.GetBytes(V(pair.Value.Position.Y)).CopyTo(bin,o+2);BitConverter.GetBytes(V(pair.Value.Position.Z)).CopyTo(bin,o+4);}Replace(smdPath,binId,binCount,bin);
    }
    public static void DeleteBinFaces(string smdPath,byte binId,int binCount,IReadOnlyCollection<int> stripFlagOffsets)
    {
        byte[] bin=Extract(smdPath,binId,binCount);int changed=0;foreach(int offset in stripFlagOffsets.Distinct()){if(offset<0||offset+2>bin.Length)continue;if(BitConverter.ToUInt16(bin,offset)!=0)continue;bin[offset]=1;bin[offset+1]=0;changed++;}if(changed==0)throw new InvalidOperationException("Nenhuma face válida foi encontrada para excluir.");Replace(smdPath,binId,binCount,bin);
    }
    public static byte[] Extract(string smdPath, int binId, int binCount)
    {
        byte[] data=File.ReadAllBytes(smdPath);(int start,int end,_,_)=Locate(data,binId,binCount);
        return data.AsSpan(start,end-start).ToArray();
    }

    public static void Replace(string smdPath, int binId, int binCount, byte[] replacement)
    {
        if(replacement==null||replacement.Length<0x50)throw new InvalidDataException("O BIN convertido é inválido ou está vazio.");
        byte[] data=File.ReadAllBytes(smdPath);(int start,int end,int table,int tplOffset)=Locate(data,binId,binCount);
        int oldLength=end-start;int aligned=(replacement.Length+15)&~15;int delta=aligned-oldLength;
        byte[] output=new byte[checked(data.Length+delta)];
        Buffer.BlockCopy(data,0,output,0,start);Buffer.BlockCopy(replacement,0,output,start,replacement.Length);
        for(int i=replacement.Length;i<aligned;i++)output[start+i]=0xCD;
        Buffer.BlockCopy(data,end,output,start+aligned,data.Length-end);
        for(int i=binId+1;i<binCount;i++)
        {
            int o=table+i*4;uint value=BitConverter.ToUInt32(data,o);if(value!=0)BitConverter.GetBytes(checked((uint)(value+delta))).CopyTo(output,o);
        }
        BitConverter.GetBytes(checked((uint)(tplOffset+delta))).CopyTo(output,8);
        AtomicWrite(smdPath,output);
    }

    private static (int Start,int End,int Table,int TplOffset) Locate(byte[] data,int binId,int binCount)
    {
        if(data.Length<0x20)throw new InvalidDataException("SMD muito pequeno.");
        int table=checked((int)BitConverter.ToUInt32(data,4));int tplOffset=checked((int)BitConverter.ToUInt32(data,8));
        if(binId<0||binId>=binCount||table<0x10||table+binCount*4>data.Length||tplOffset<=table||tplOffset>data.Length)throw new InvalidDataException("Tabela BIN do SMD inválida.");
        uint relative=BitConverter.ToUInt32(data,table+binId*4);if(relative==0)throw new InvalidDataException($"BIN {binId} não possui geometria incorporada.");
        int start=checked(table+(int)relative),end=tplOffset;
        for(int i=binId+1;i<binCount;i++){uint next=BitConverter.ToUInt32(data,table+i*4);if(next!=0){end=Math.Min(end,checked(table+(int)next));break;}}
        if(start<table+binCount*4||start+0x50>end||end>data.Length)throw new InvalidDataException($"Intervalo do BIN {binId} inválido.");
        return(start,end,table,tplOffset);
    }
    private static int Align16(int value)=>(value+15)&~15;
    private static void WriteEntry(byte[] raw,ScenarioEntry e,byte binId)
    {Write(raw,0,e.PositionX*100f,e.PositionY*100f,e.PositionZ*100f);Write(raw,0x10,e.RotationX,e.RotationY,e.RotationZ);Write(raw,0x20,e.ScaleX,e.ScaleY,e.ScaleZ);raw[0x30]=binId;}
    private static void Write(byte[] b,int o,float x,float y,float z){BitConverter.GetBytes(x).CopyTo(b,o);BitConverter.GetBytes(y).CopyTo(b,o+4);BitConverter.GetBytes(z).CopyTo(b,o+8);}
    private static void AtomicWrite(string path,byte[] data){string temp=path+".smd_rebuild_tmp";try{File.WriteAllBytes(temp,data);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
