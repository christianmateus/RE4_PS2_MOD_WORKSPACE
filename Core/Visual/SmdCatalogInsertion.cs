using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Collision;
namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;
public static class SmdCatalogInsertion
{
    public static int Insert(SmdObjectCatalogService service,SmdCatalogItem item,string smd,string tpl,int entries,int bins,EsatFile? sat,EsatFile? eat,string backupRoot,Vector3? spawnPosition=null)
    {
        foreach(var kind in item.Collisions.Select(f=>f.Kind).Distinct())if((kind==EsatKind.Sat?sat:eat)==null)throw new InvalidOperationException($"O cenário de destino não possui {kind.ToString().ToUpperInvariant()} para receber a colisão deste item.");
        string token=Guid.NewGuid().ToString("N");var stages=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var recoveries=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var committed=new List<string>();
        try
        {
            string Stage(string path){string temp=path+".catalog-"+token+".tmp";File.Copy(path,temp);stages.Add(path,temp);return temp;}
            string smdStage=Stage(smd),tplStage=Stage(tpl);int index=service.Import(item,smdStage,tplStage,entries,bins);var scene=Ps2ScenarioReader.Read(smdStage);if(scene.EntryCount!=entries+item.Entries.Count)throw new InvalidDataException("O número de entries inseridas não confere.");
            if(spawnPosition is Vector3 position)
            {
                if(!float.IsFinite(position.X)||!float.IsFinite(position.Y)||!float.IsFinite(position.Z))throw new InvalidDataException("Posição de inserção inválida.");
                var added=scene.Entries.Where(e=>e.FileOrder>=entries).ToArray();
                var points=added.SelectMany(e=>e.LocalTriangles.SelectMany(t=>new[]{t.A,t.B,t.C}).Select(p=>Vector3.Transform(p,SmdCatalogCollisionFace.Transform(e)))).ToArray();
                Vector3 center=points.Length==0?added[0].Position:(points.Aggregate(Vector3.Min)+points.Aggregate(Vector3.Max))/2f;
                Vector3 delta=position-center;
                using var stream=new FileStream(smdStage,FileMode.Open,FileAccess.Write,FileShare.None);using var writer=new BinaryWriter(stream);
                foreach(var entry in added){entry.PositionX+=delta.X;entry.PositionY+=delta.Y;entry.PositionZ+=delta.Z;stream.Position=0x10+entry.FileOrder*0x40;writer.Write(entry.PositionX*100f);writer.Write(entry.PositionY*100f);writer.Write(entry.PositionZ*100f);}
            }
            var links=new List<SmdCatalogCollisionLink>();
            foreach(var file in new[]{sat,eat}.Where(f=>f!=null&&item.Collisions.Any(c=>c.Kind==f.Kind)).Cast<EsatFile>())
            {
                string stage=Stage(file.SourcePath);var anchor=scene.Entries.First(e=>e.FileOrder==entries);int mesh=file.Meshes.FindIndex(m=>m.Groups.Any(g=>g.Flags==2));int start=mesh>=0?file.Meshes[mesh].Positions.Count:0;
                var faces=item.Collisions.Where(c=>c.Kind==file.Kind).ToArray();Ps2EsatPrimitiveWriter.AppendCatalogFaces(file,faces,anchor,stage);
                links.Add(new(){Kind=file.Kind,Entry=entries,BinId=anchor.BinId,Mesh=mesh,FirstVertex=start,LocalPoints=faces.SelectMany(f=>new[]{f.A,f.B,f.C}).Select(v=>v.ToArray()).ToList(),LastPoints=faces.SelectMany(f=>new[]{f.Point(f.A,anchor),f.Point(f.B,anchor),f.Point(f.C,anchor)}).Select(v=>new[]{v.X,v.Y,v.Z}).ToList()});
            }
            if(links.Count>0){string metadata=SmdCatalogCollisionLinks.PathFor(smd);string stage=metadata+".catalog-"+token+".tmp";var existing=SmdCatalogCollisionLinks.Load(smd);existing.AddRange(links);SmdCatalogCollisionLinks.Write(stage,existing);stages.Add(metadata,stage);}
            Directory.CreateDirectory(backupRoot);
            foreach(var path in stages.Keys){if(!File.Exists(path))continue;string backup=System.IO.Path.Combine(backupRoot,System.IO.Path.GetFileName(path)+".catalog-"+token+".bak");File.Copy(path,backup);recoveries.Add(path,backup);}
            foreach(var pair in stages){File.Move(pair.Value,pair.Key,true);committed.Add(pair.Key);}return index;
        }
        catch{foreach(var path in committed.AsEnumerable().Reverse()){if(recoveries.TryGetValue(path,out var recovery))File.Copy(recovery,path,true);else File.Delete(path);}throw;}
        finally{foreach(var path in stages.Values)if(File.Exists(path))File.Delete(path);}
    }
}
