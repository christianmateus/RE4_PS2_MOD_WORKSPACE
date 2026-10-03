using System.Security.Cryptography;
namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed record EtmReplicationTarget(string Path,bool Compatible,string Detail,byte[] Hash);
public static class EtmObjectReplication
{
    public static EtmReplicationTarget? Inspect(string path,byte id,IReadOnlyList<EtmResource> source)
    {
        try
        {
            byte[] bytes=File.ReadAllBytes(path);var target=Ps2EtmReader.Read(path);
            if(!target.Objects.TryGetValue(id,out var definition))return null;
            var own=source.Where(r=>r.Name.StartsWith($"et{id:X2}",StringComparison.OrdinalIgnoreCase)).Select(r=>r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var targetOwn=definition.Resources.Select(r=>r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing=source.Where(s=>!target.Resources.Any(t=>t.Name.Equals(s.Name,StringComparison.OrdinalIgnoreCase)&&t.Type==s.Type)).Select(r=>r.Name).ToArray();
            bool compatible=missing.Length==0&&own.SetEquals(targetOwn);
            string detail=missing.Length>0?"Ausentes: "+string.Join(", ",missing):!own.SetEquals(targetOwn)?"Variantes do objeto diferentes":"Compatível • conjunto completo";
            return new(path,compatible,detail,SHA256.HashData(bytes));
        }
        catch(Exception ex){return new(path,false,"Não foi possível ler: "+ex.Message,Array.Empty<byte>());}
    }

    public static IReadOnlyList<string> Apply(byte id,IReadOnlyList<EtmResource> source,IReadOnlyList<EtmReplicationTarget> targets)
    {
        if(targets.Count==0)throw new InvalidOperationException("Selecione ao menos um cenário compatível.");
        string token=DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")[..8];
        var prepared=new List<(EtmReplicationTarget Target,string Stage,string Backup)>();var committed=new List<(string Path,string Backup)>();
        try
        {
            foreach(var target in targets.DistinctBy(t=>System.IO.Path.GetFullPath(t.Path),StringComparer.OrdinalIgnoreCase))
            {
                var fresh=Inspect(target.Path,id,source);
                if(fresh==null||!fresh.Compatible||!fresh.Hash.SequenceEqual(target.Hash))throw new InvalidOperationException($"{System.IO.Path.GetFileName(target.Path)} mudou após a análise. Atualize a lista antes de replicar.");
                var catalog=Ps2EtmReader.Read(target.Path);
                var byName=source.ToDictionary(r=>r.Name,StringComparer.OrdinalIgnoreCase);
                var replacements=catalog.Resources.Where(r=>byName.ContainsKey(r.Name)).ToDictionary(r=>r.FileOrder,r=>byName[r.Name].Data);
                string stage=target.Path+".replica_"+token+".tmp",backup=target.Path+".replica_"+token+".bak";
                prepared.Add((target,stage,backup));Ps2AssetPackageEditor.Save(target.Path,stage,replacements);
                var verified=Ps2EtmReader.Read(stage);
                if(verified.Resources.Count!=catalog.Resources.Count)throw new InvalidDataException("A validação alterou a quantidade de recursos.");
                foreach(var resource in verified.Resources)
                {
                    byte[] expected=byName.TryGetValue(resource.Name,out var supplied)?supplied.Data:catalog.Resources[resource.FileOrder].Data;
                    if(!resource.Data.SequenceEqual(expected))throw new InvalidDataException("Recurso divergente: "+resource.Name);
                }
                if(!verified.ModelParts.TryGetValue(id,out var models)||models.Count==0)throw new InvalidDataException("Objeto replicado sem geometria renderizável.");
            }
            foreach(var item in prepared)
            {
                if(!SHA256.HashData(File.ReadAllBytes(item.Target.Path)).SequenceEqual(item.Target.Hash))throw new IOException("ETM alterado durante a preparação: "+item.Target.Path);
                File.Copy(item.Target.Path,item.Backup,false);
            }
            foreach(var item in prepared)
            {
                committed.Add((item.Target.Path,item.Backup));File.Copy(item.Stage,item.Target.Path,true);
            }
            return prepared.Select(p=>p.Backup).ToArray();
        }
        catch(Exception failure)
        {
            var rollbackErrors=new List<Exception>();foreach(var item in committed.AsEnumerable().Reverse())try{File.Copy(item.Backup,item.Path,true);}catch(Exception ex){rollbackErrors.Add(ex);}
            if(rollbackErrors.Count>0)throw new AggregateException("O lote falhou e parte da restauração requer os backups indicados na pasta dos ETMs.",new[]{failure}.Concat(rollbackErrors));
            throw;
        }
        finally{foreach(var item in prepared)if(File.Exists(item.Stage))File.Delete(item.Stage);}
    }
}
