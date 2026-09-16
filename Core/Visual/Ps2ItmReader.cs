namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2ItmReader
{
    public static EtmCatalog Read(string path)
    {
        byte[] data=File.ReadAllBytes(path);
        if(data.Length<0x30)throw new InvalidDataException("ITM is too small.");
        if(data.Length==0x40)return new EtmCatalog{SourcePath=path}; // empty room ITM used by several PS2 scenarios
        uint sectionCount=BitConverter.ToUInt32(data,0);
        if(sectionCount<3||4L+sectionCount*4L>data.Length)throw new InvalidDataException("Invalid ITM section table.");
        int map=Offset(data,4),models=Offset(data,8),textures=Offset(data,12);
        if(!(map<models&&models<textures&&textures<data.Length))throw new InvalidDataException("Invalid ITM section offsets.");
        int count=checked((int)BitConverter.ToUInt32(data,map));
        int modelCount=checked((int)BitConverter.ToUInt32(data,models));
        int textureCount=checked((int)BitConverter.ToUInt32(data,textures));
        if(count<=0||count!=modelCount||count!=textureCount||map+8L+count*8L>models||models+8L+count*4L>textures||textures+8L+count*4L>data.Length)throw new InvalidDataException("Invalid ITM item/model/texture table.");

        var resources=new List<EtmResource>();
        var objects=new Dictionary<byte,EtmObjectDefinition>();
        var modelMap=new Dictionary<byte,IReadOnlyList<ScenarioTriangle>>();
        var partMap=new Dictionary<byte,IReadOnlyList<EtmModelPart>>();
        for(int i=0;i<count;i++)
        {
            uint rawId=BitConverter.ToUInt32(data,map+0x0C+i*8);
            if(rawId>byte.MaxValue)continue;
            byte id=(byte)rawId;
            int relative=Offset(data,models+8+i*4);int start=checked(models+relative);
            int next=i+1<count?checked(models+Offset(data,models+8+(i+1)*4)):textures;
            int end=next>start&&next<=textures?next:textures;
            if(start<models||start>=end||end>textures)continue;
            byte[] bin=data.AsSpan(start,end-start).ToArray();
            try
            {
                IReadOnlyList<ScenarioTriangle> source=Ps2ScenarioReader.ReadStandaloneBin(bin);
                // +0x04 is the texture-section header size; its offset table begins at +0x08.
                int textureRelative=Offset(data,textures+8+i*4);int textureStart=checked(textures+textureRelative);
                int textureNext=i+1<count?checked(textures+Offset(data,textures+8+(i+1)*4)):data.Length;
                int textureEnd=textureNext>textureStart&&textureNext<=data.Length?textureNext:data.Length;
                if(textureStart<textures||textureStart>=textureEnd)continue;
                byte[] tpl=data.AsSpan(textureStart,textureEnd-textureStart).ToArray();
                // Every ITM slot contains an independent one-texture TPL.
                ScenarioTriangle[] triangles=source.Select(t=>new ScenarioTriangle(t.A,t.B,t.C,t.UvA,t.UvB,t.UvC,0,t.SourceOffsetA,t.SourceOffsetB,t.SourceOffsetC,t.SourceFactor,t.SourceStripFlagOffset)).ToArray();
                var binResource=new EtmResource(i,2,$"itm{id:X2}.bin",bin);resources.Add(binResource);
                var textureResource=new EtmResource(100000+i,3,$"itm{id:X2}.tpl",tpl);resources.Add(textureResource);
                var definition=new EtmObjectDefinition(id,new EtmResource[]{binResource,textureResource});
                var part=new EtmModelPart(binResource,null,textureResource,triangles);
                objects[id]=definition;modelMap[id]=triangles;partMap[id]=new[]{part};
            }
            catch(InvalidDataException){ }
            catch(EndOfStreamException){ }
        }
        return new EtmCatalog{SourcePath=path,Resources=resources,Objects=objects,Models=modelMap,ModelParts=partMap};
    }

    private static int Offset(byte[] data,int offset)
    {
        uint value=BitConverter.ToUInt32(data,offset);
        if(value>int.MaxValue)throw new InvalidDataException("ITM offset is too large.");
        return(int)value;
    }
}
