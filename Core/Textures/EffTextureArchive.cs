using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures;

/// <summary>EFF is a relative-offset archive. Its embedded TPLs can share a table with BINs.</summary>
internal static class EffTextureArchive
{
    internal sealed record Entry(int ArchiveOffset,int ArchiveEnd,int Index,int HeaderOffset,TplModel Texture);
    private sealed record Node(int Start,int End,int[] Offsets,List<Node> Children);

    private static Node Parse(byte[] data,int start,int end,int depth)
    {
        var leaf=new Node(start,end,Array.Empty<int>(),new());
        if(depth>3||end-start<4)return leaf;
        uint count=BitConverter.ToUInt32(data,start);
        if(count==0||count>256||4L+count*4>end-start)return leaf;
        int header=checked(4+(int)count*4);var offsets=new int[count];
        for(int i=0;i<offsets.Length;i++)
        {
            uint offset=BitConverter.ToUInt32(data,start+4+i*4);
            if(offset!=0&&(offset<header||offset>(uint)(end-start)||(offset&15)!=0))return leaf;
            offsets[i]=(int)offset;
        }
        var starts=offsets.Where(x=>x>0&&start+x<end).Distinct().Order().Select(x=>start+x).ToArray();
        if(starts.Length==0)return leaf;
        var children=new List<Node>();
        for(int i=0;i<starts.Length;i++)children.Add(Parse(data,starts[i],i+1<starts.Length?starts[i+1]:end,depth+1));
        return new(start,end,offsets,children);
    }

    internal static IReadOnlyList<Entry> Entries(byte[] data)
    {
        var result=new List<Entry>();
        void Visit(Node node)
        {
            if(node.End-node.Start>=0x40&&BitConverter.ToUInt32(data,node.Start)==0x1000)
            {
                using var stream=new MemoryStream(data.AsSpan(node.Start,node.End-node.Start).ToArray(),false);
                using var reader=new BinaryReader(stream);var tplReader=new TplReader();
                uint count=BitConverter.ToUInt32(data,node.Start+4);
                if(count==0||count>4096||0x10L+count*0x30>stream.Length)throw new InvalidDataException("Tabela TPL inválida no EFF.");
                for(int i=0;i<count;i++)
                {
                    var t=tplReader.ReadTexture(reader,i);
                    t.pixelsOffset=checked(t.pixelsOffset+(uint)node.Start);
                    if(t.paletteOffset!=0)t.paletteOffset=checked(t.paletteOffset+(uint)node.Start);
                    if(t.mipmapOffset1!=0)t.mipmapOffset1=checked(t.mipmapOffset1+(uint)node.Start);
                    if(t.mipmapOffset2!=0)t.mipmapOffset2=checked(t.mipmapOffset2+(uint)node.Start);
                    result.Add(new(node.Start,node.End,i,node.Start+0x10+i*0x30,t));
                }
            }
            else foreach(var child in node.Children)Visit(child);
        }
        Visit(Parse(data,0,data.Length,0));return result;
    }

    internal static byte[] Replace(byte[] data,int index,TplModel replacement)
    {
        if(replacement.width==0||replacement.height==0||replacement.width>512||replacement.height>512)
            throw new InvalidDataException("A textura deve ter no máximo 512 pixels de largura e altura.");
        if(replacement.mipmapCount!=0)throw new InvalidDataException("Importe um TPL sem mipmaps.");
        if(replacement.bitDepth is not (6 or 8 or 9)||replacement.interlace>3||replacement.header?.Length!=0x30||replacement.pixels.Length!=TplReader.GetPixelDataLength(replacement.width,replacement.height,replacement.bitDepth)||replacement.palette.Length!=TplReader.GetPaletteLength(replacement.bitDepth))
            throw new InvalidDataException("Formato ou tamanho dos dados TPL inválido.");
        var entries=Entries(data);
        if((uint)index>=(uint)entries.Count)throw new InvalidDataException("Textura EFF não encontrada na estrutura do arquivo.");
        var entry=entries[index];var old=entry.Texture;
        // Keep an exact patch for equal-sized records; no unrelated bytes need to move.
        if(old.mipmapCount==0&&old.pixels.Length==replacement.pixels.Length&&old.palette.Length==replacement.palette.Length)
        {
            byte[] patched=(byte[])data.Clone();byte[] header=(byte[])replacement.header.Clone();
            Array.Clear(header,0x10,8);
            BitConverter.GetBytes(old.pixelsOffset-(uint)entry.ArchiveOffset).CopyTo(header,0x20);
            BitConverter.GetBytes(old.paletteOffset==0?0:old.paletteOffset-(uint)entry.ArchiveOffset).CopyTo(header,0x24);
            header.CopyTo(patched,entry.HeaderOffset);replacement.pixels.CopyTo(patched,(int)old.pixelsOffset);
            if(replacement.palette.Length>0)replacement.palette.CopyTo(patched,(int)old.paletteOffset);
            return patched;
        }
        var textures=entries.Where(e=>e.ArchiveOffset==entry.ArchiveOffset).Select(e=>e.Texture).ToArray();textures[entry.Index]=replacement;
        string temp=Path.Combine(Path.GetTempPath(),"eff_tpl_rebuild_"+Guid.NewGuid().ToString("N")+".tpl");byte[] changed;
        try{new TplWriter(new TplReader()).RebuildFile(temp,textures);changed=File.ReadAllBytes(temp);Array.Resize(ref changed,checked((changed.Length+15)&~15));}
        finally{if(File.Exists(temp))File.Delete(temp);}
        byte[] Rebuild(Node node)
        {
            if(node.Start==entry.ArchiveOffset)return changed;
            if(node.Children.Count==0||entry.ArchiveOffset<node.Start||entry.ArchiveOffset>=node.End)return data.AsSpan(node.Start,node.End-node.Start).ToArray();
            using var output=new MemoryStream();output.Write(data.AsSpan(node.Start,node.Children[0].Start-node.Start));
            var relocated=new Dictionary<int,int>();
            foreach(var child in node.Children){relocated[child.Start-node.Start]=checked((int)output.Position);output.Write(Rebuild(child));}
            relocated[node.End-node.Start]=checked((int)output.Length);byte[] result=output.ToArray();
            for(int i=0;i<node.Offsets.Length;i++)if(node.Offsets[i]!=0)BitConverter.GetBytes(checked((uint)relocated[node.Offsets[i]])).CopyTo(result,4+i*4);
            return result;
        }
        var rebuilt=Rebuild(Parse(data,0,data.Length,0));var verified=Entries(rebuilt);
        if(verified.Count!=entries.Count)throw new InvalidDataException("A reconstrução alterou a quantidade de texturas EFF.");
        for(int i=0;i<entries.Count;i++)
        {
            var expected=i==index?replacement:entries[i].Texture;var actual=verified[i].Texture;
            if(actual.width!=expected.width||actual.height!=expected.height||actual.bitDepth!=expected.bitDepth||actual.interlace!=expected.interlace||!actual.pixels.SequenceEqual(expected.pixels)||!actual.palette.SequenceEqual(expected.palette))throw new InvalidDataException("Textura divergente após reconstruir o EFF.");
        }
        return rebuilt;
    }
}
