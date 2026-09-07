using System.Buffers.Binary;
using System.Drawing;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Effects;

public sealed class EffTextureResource
{
    public ushort Id { get; init; }
    public string SourceEffPath { get; init; } = string.Empty;
    public int PackageIndex { get; init; }
    public byte[] TplData { get; init; } = Array.Empty<byte>();
    public int FrameCount { get; init; }
    public string SourceName => Path.GetFileName(SourceEffPath);
    public Bitmap DecodeFrame(int frame)
    {
        using var stream=new MemoryStream(TplData,false);using var reader=new BinaryReader(stream);
        var texture=new TplReader().ReadTexture(reader,Math.Clamp(frame,0,Math.Max(0,FrameCount-1)));
        stream.Position=0;return new TextureDecoder().Decode(texture,reader);
    }
}

public sealed class EffTextureCatalog
{
    private readonly Dictionary<ushort,List<EffTextureResource>> resources=new();
    public IReadOnlyList<EffTextureResource> Resolve(ushort id)=>resources.TryGetValue(id,out List<EffTextureResource>? found)?found:Array.Empty<EffTextureResource>();
    public static EffTextureCatalog Build(string localEffPath,IEnumerable<string> coreEffPaths)
    {
        var catalog=new EffTextureCatalog();catalog.Add(localEffPath);
        foreach(string path in coreEffPaths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>Path.GetFileName(x),StringComparer.OrdinalIgnoreCase))
            if(!Path.GetFullPath(path).Equals(Path.GetFullPath(localEffPath),StringComparison.OrdinalIgnoreCase))catalog.Add(path);
        return catalog;
    }
    private void Add(string path)
    {
        byte[] d=File.ReadAllBytes(path);if(d.Length<0x30||U32(d,0)!=11)return;
        int ids=checked((int)U32(d,4)),tpl=checked((int)U32(d,0x18)),meta=checked((int)U32(d,0x1C));
        if(ids==0||tpl==0||meta<=tpl||meta>d.Length)return;
        int count=checked((int)U32(d,ids));if(count<0||count>4096||ids+4L+count*8L>d.Length||U32(d,tpl)!=(uint)count)return;
        for(int i=0;i<count;i++)
        {
            ushort id=U16(d,ids+4+i*8);int rel=checked((int)U32(d,tpl+4+i*4));int next=i+1<count?checked((int)U32(d,tpl+8+i*4)):meta-tpl;
            int start=tpl+rel,end=tpl+next;if(start<tpl||end<=start||end>meta)continue;
            byte[] package=d.AsSpan(start,end-start).ToArray();if(package.Length<0x10)continue;
            int frames=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(package.AsSpan(4,4)));if(frames<=0||frames>4096)continue;
            var item=new EffTextureResource{Id=id,SourceEffPath=path,PackageIndex=i,TplData=package,FrameCount=frames};
            if(!resources.TryGetValue(id,out List<EffTextureResource>? list)){list=new();resources[id]=list;}list.Add(item);
        }
    }
    private static ushort U16(byte[]d,int p)=>BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(p,2));
    private static uint U32(byte[]d,int p)=>BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p,4));
}
