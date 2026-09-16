using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using RE4_PS2_MOD_WORKSPACE.Core.Smd;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class SmdCatalogItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Objeto SMD";
    public string Category { get; set; } = "Sem categoria";
    public string Notes { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<SmdCatalogEntry> Entries { get; set; } = new();
    public List<SmdCatalogBin> Bins { get; set; } = new();
    public List<SmdCatalogTexture> Textures { get; set; } = new();
    public string Folder { get; set; } = "";
    public string PreviewPath => Path.Combine(Folder,"preview.png");
    public override string ToString()=>Name;
}
public sealed class SmdCatalogEntry { public int BinKey { get; set; } public byte[] RawData { get; set; }=Array.Empty<byte>(); public float X { get;set;} public float Y {get;set;} public float Z {get;set;} public float Rx {get;set;} public float Ry {get;set;} public float Rz {get;set;} public float Sx {get;set;}=1; public float Sy {get;set;}=1; public float Sz {get;set;}=1; }
public sealed class SmdCatalogBin { public int Key {get;set;} public string File {get;set;}=""; }
public sealed class SmdCatalogTexture { public int SourceIndex {get;set;} public string File {get;set;}=""; public string PixelHash {get;set;}=""; }

public sealed class SmdObjectCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public string RootPath {get;}
    public SmdObjectCatalogService(string? root=null){RootPath=root??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"RE4_PS2_MOD_WORKSPACE","SmdCatalog");}
    public IReadOnlyList<SmdCatalogItem> LoadAll()
    {
        Directory.CreateDirectory(RootPath);var result=new List<SmdCatalogItem>();foreach(string file in Directory.EnumerateFiles(RootPath,"object.json",SearchOption.AllDirectories)){try{var item=JsonSerializer.Deserialize<SmdCatalogItem>(File.ReadAllText(file),JsonOptions);if(item!=null){item.Folder=Path.GetDirectoryName(file)!;result.Add(item);}}catch{}}return result.OrderBy(x=>x.Category).ThenBy(x=>x.Name).ToArray();
    }
    public SmdCatalogItem Export(string smdPath,string tplPath,IReadOnlyList<ScenarioEntry> entries,int binCount,string name,string category,string notes,Bitmap? preview)
    {
        if(entries.Count==0)throw new InvalidOperationException("Selecione pelo menos uma entry SMD.");Directory.CreateDirectory(RootPath);var item=new SmdCatalogItem{Name=name.Trim(),Category=string.IsNullOrWhiteSpace(category)?"Sem categoria":category.Trim(),Notes=notes.Trim()};item.Folder=Path.Combine(RootPath,item.Id);Directory.CreateDirectory(item.Folder);Directory.CreateDirectory(Path.Combine(item.Folder,"textures"));Directory.CreateDirectory(Path.Combine(item.Folder,"bins"));
        var binKeys=entries.Select(x=>(int)x.BinId).Distinct().ToArray();foreach(int key in binKeys){string file=$"bins/bin_{key:D3}.bin";File.WriteAllBytes(Path.Combine(item.Folder,file.Replace('/',Path.DirectorySeparatorChar)),SmdEmbeddedBinService.Extract(smdPath,key,binCount));item.Bins.Add(new(){Key=key,File=file});}
        foreach(ScenarioEntry e in entries)item.Entries.Add(new(){BinKey=e.BinId,RawData=(byte[])e.RawData.Clone(),X=e.PositionX,Y=e.PositionY,Z=e.PositionZ,Rx=e.RotationX,Ry=e.RotationY,Rz=e.RotationZ,Sx=e.ScaleX,Sy=e.ScaleY,Sz=e.ScaleZ});
        int[] textures=entries.SelectMany(e=>e.LocalTriangles).Select(t=>t.TextureIndex).Where(i=>i>=0&&i<=255).Distinct().OrderBy(i=>i).ToArray();var textureService=new TextureWorkspaceService();foreach(int index in textures){string file=$"textures/texture_{index:D3}.png";string path=Path.Combine(item.Folder,file.Replace('/',Path.DirectorySeparatorChar));textureService.ExportPng(tplPath,index,path);item.Textures.Add(new(){SourceIndex=index,File=file,PixelHash=ComputePixelHash(path)});}
        if(preview!=null)preview.Save(item.PreviewPath,ImageFormat.Png);else if(item.Textures.Count>0)File.Copy(Path.Combine(item.Folder,item.Textures[0].File.Replace('/',Path.DirectorySeparatorChar)),item.PreviewPath,true);
        Save(item);return item;
    }
    public int Import(SmdCatalogItem item,string smdPath,string tplPath,int entryCount,int binCount)
    {
        var textureService=new TextureWorkspaceService();var existing=new Dictionary<string,byte>(StringComparer.OrdinalIgnoreCase);uint count=new TplReader().ReadTextureCount(tplPath);string temp=Path.Combine(Path.GetTempPath(),"re4_smd_catalog_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
        try
        {
            for(int i=0;i<count&&i<=255;i++){string p=Path.Combine(temp,$"{i}.png");textureService.ExportPng(tplPath,i,p);existing.TryAdd(ComputePixelHash(p),(byte)i);}
            var map=new Dictionary<byte,byte>();foreach(var texture in item.Textures){if(existing.TryGetValue(texture.PixelHash,out byte found)){map[(byte)texture.SourceIndex]=found;continue;}int added=textureService.AppendFromImage(tplPath,Path.Combine(item.Folder,texture.File.Replace('/',Path.DirectorySeparatorChar)));if(added>255)throw new InvalidDataException("O TPL atingiu o limite de 256 texturas.");map[(byte)texture.SourceIndex]=(byte)added;existing[texture.PixelHash]=(byte)added;}
            var destinationBins=new Dictionary<int,byte>();int entries=entryCount,bins=binCount,last=-1;
            foreach(SmdCatalogEntry source in item.Entries)
            {
                byte[] raw=(byte[])source.RawData.Clone();if(raw.Length>0x38)raw[0x38]=0x08;
                var template=new ScenarioEntry{RawData=raw,PositionX=source.X,PositionY=source.Y,PositionZ=source.Z,RotationX=source.Rx,RotationY=source.Ry,RotationZ=source.Rz,ScaleX=source.Sx,ScaleY=source.Sy,ScaleZ=source.Sz};
                if(!destinationBins.TryGetValue(source.BinKey,out byte destination)){SmdCatalogBin descriptor=item.Bins.First(x=>x.Key==source.BinKey);byte[] bin=File.ReadAllBytes(Path.Combine(item.Folder,descriptor.File.Replace('/',Path.DirectorySeparatorChar)));SmdEmbeddedBinService.RemapBinMaterialTextures(bin,map);last=SmdEmbeddedBinService.AppendEntry(smdPath,template,entries++,bins,bin);destination=checked((byte)bins++);destinationBins[source.BinKey]=destination;}
                else{template.BinId=destination;last=SmdEmbeddedBinService.AppendEntry(smdPath,template,entries++,bins);}
            }
            SmdTextureService.InjectTpl(smdPath,tplPath);return last;
        }
        finally{try{Directory.Delete(temp,true);}catch{}}
    }
    public void Delete(SmdCatalogItem item){if(!Path.GetFullPath(item.Folder).StartsWith(Path.GetFullPath(RootPath),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Item fora do catálogo.");Directory.Delete(item.Folder,true);}
    private static void Save(SmdCatalogItem item){File.WriteAllText(Path.Combine(item.Folder,"object.json"),JsonSerializer.Serialize(item,JsonOptions));}
    private static string ComputePixelHash(string imagePath){using var image=new Bitmap(imagePath);using var sha=SHA256.Create();using var stream=new MemoryStream();for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){Color c=image.GetPixel(x,y);stream.WriteByte(c.R);stream.WriteByte(c.G);stream.WriteByte(c.B);stream.WriteByte(c.A);}return Convert.ToHexString(sha.ComputeHash(stream.ToArray()));}
}
