using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using RE4_PS2_MOD_WORKSPACE.Core.Dat;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class CharacterSmdMetadata
{
    public int Version {get;set;}=1;public string SourceDat {get;set;}="";public int DatEntry {get;set;}public int BinIndex {get;set;}public int TplEntry {get;set;}=-1;
    public List<CharacterSmdBone> Bones {get;set;}=new();public List<CharacterSmdMaterial> Materials {get;set;}=new();
}
public sealed class CharacterSmdBone {public string Name {get;set;}="";public int Id {get;set;}public int GameId {get;set;}public int RawParentId {get;set;}public int ParentId {get;set;}public float[] Local {get;set;}=new float[3];}
public sealed class CharacterSmdMaterial {public string Name {get;set;}="";public string OriginalName {get;set;}="";public int TextureIndex {get;set;}}
public sealed record CharacterSmdChange(int DatEntry,int BinIndex,byte[] Before,byte[] After);

public static class CharacterSmdRoundTrip
{
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public static CharacterSmdMetadata ReadMetadata(string smd)
    {
        string path=smd+".character.json";if(!File.Exists(path))throw new InvalidDataException($"{Path.GetFileName(smd)}: mantenha o arquivo .character.json ao lado do SMD de modelo exportado. Use IMPORTAR MODELO para modelos externos.");
        var meta=JsonSerializer.Deserialize<CharacterSmdMetadata>(File.ReadAllText(path),Json)??throw new InvalidDataException("Identificação do SMD inválida.");if(meta.Version!=1||meta.Bones.Count==0||meta.Bones.Select(b=>b.Id).Distinct().Count()!=meta.Bones.Count)throw new InvalidDataException("Versão ou esqueleto da identificação SMD inválido.");return meta;
    }
    public static async Task ExportAsync(string dat,IReadOnlyList<EnemyModelPart> parts,string folder,FcvAnimation? animation,IProgress<string>? progress=null,IReadOnlyDictionary<int,int>? tplAssignments=null)
    {
        Directory.CreateDirectory(folder);string work=Path.Combine(Path.GetTempPath(),"re4_character_export_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
        try{foreach(var part in parts){progress?.Report($"Exportando BIN {part.BinIndex:D2} • DAT #{part.DatEntryIndex:D3}...");byte[] bin=Ps2CharacterDatEditor.CaptureDatEntry(dat,part.DatEntryIndex);var skeleton=Ps2BinSkeletonReader.Read(bin);
            string stem=Path.GetFileNameWithoutExtension(dat)+$"_bin_{part.BinIndex:D2}_dat_{part.DatEntryIndex:D3}_model";string nativeBin=Path.Combine(work,stem+".BIN");await Ps2ModelConversionService.ExtractCharacterSmdAsync(bin,nativeBin);var document=CharacterSmdDocument.Read(Path.ChangeExtension(nativeBin,".smd"));
            if(document.Nodes.Count!=skeleton.Bones.Count)throw new InvalidDataException("Os ossos extraídos não correspondem ao BIN de origem.");
            // The native tool uses ID+256 for alternate duplicate-ID bones. Keep this
            // encoding in the sidecar and expose stable, unique indices to Blender.
            var ids=document.Nodes.Select((n,i)=>(n,i)).ToDictionary(x=>x.n.Index,x=>x.i);
            var names=document.Nodes.Select((n,i)=>(n,i)).ToDictionary(x=>x.n.Index,x=>$"bone_{skeleton.Bones[x.i].Id:X2}_{x.i:D2}");
            int tpl=tplAssignments?.GetValueOrDefault(part.BinIndex,part.TplEntryIndex)??part.TplEntryIndex;
            var meta=new CharacterSmdMetadata{SourceDat=Path.GetFileName(dat),DatEntry=part.DatEntryIndex,BinIndex=part.BinIndex,TplEntry=tpl,Bones=skeleton.Bones.Select((b,i)=>new CharacterSmdBone{Name=names[document.Nodes[i].Index],Id=document.Nodes[i].Index,GameId=b.Id,RawParentId=b.ParentId,ParentId=document.Nodes[i].Parent,Local=new[]{b.LocalPosition.X,b.LocalPosition.Y,b.LocalPosition.Z}}).ToList()};
            string materialFile=Path.ChangeExtension(nativeBin,".idxmaterial");string[] materialLines=File.ReadAllLines(materialFile);string? name=null;int nameLine=-1;var materialMap=new Dictionary<string,string>(StringComparer.Ordinal);
            for(int i=0;i<materialLines.Length;i++){if(materialLines[i].StartsWith("UseMaterial:",StringComparison.Ordinal)){name=materialLines[i][12..].Trim();nameLine=i;}else if(name!=null&&materialLines[i].StartsWith("diffuse_map:",StringComparison.Ordinal)){int texture=int.Parse(materialLines[i][12..].Trim(),CultureInfo.InvariantCulture);string exported=texture==255?$"{stem}_material_{meta.Materials.Count:D3}":$"{stem}_texture_{texture:D3}.png";meta.Materials.Add(new(){Name=exported,OriginalName=name,TextureIndex=texture});materialMap[name]=exported;materialLines[nameLine]="UseMaterial:"+exported;name=null;}}
            string output=Path.Combine(folder,stem+".smd");document.Write(output,ids,names,materialMap);File.WriteAllLines(Path.ChangeExtension(output,".idxmaterial"),materialLines);File.WriteAllText(output+".character.json",JsonSerializer.Serialize(meta,Json));
            if(tpl>=0){string tempTpl=Path.Combine(work,"textures.TPL");File.WriteAllBytes(tempTpl,Ps2CharacterDatEditor.CaptureTplEntry(dat,tpl));var textures=new TextureWorkspaceService();foreach(int index in meta.Materials.Select(m=>m.TextureIndex).Where(i=>i>=0&&i<255).Distinct())textures.ExportPng(tempTpl,index,Path.Combine(folder,$"{stem}_texture_{index:D3}.png"));}
            if(animation!=null){string clip=Path.Combine(folder,stem.Replace("_model","_animation")+".smd");FcvSmdRoundTrip.Export(clip,skeleton,animation);}
        }
        File.WriteAllText(Path.Combine(folder,"COMO_USAR.txt"),"1. Importe os arquivos _model.smd no Blender (Blender Source Tools). Cada arquivo é uma parte independente, em pose de referência.\r\n2. Opcionalmente importe o _animation.smd correspondente para conferir a animação, usando 30 FPS.\r\n3. Mantenha os nomes/hierarquia dos ossos e a pose de referência. Edite as malhas; normalize os pesos e limite a 3 influências por vértice.\r\n4. Exporte como Reference SMD, sem aplicar a pose da animação à malha. Preserve os nomes dos materiais.\r\n5. Deixe .character.json e .idxmaterial ao lado do SMD editado; mantenha o nome do arquivo.\r\n6. Em Personagens, use REIMPORTAR SMD e selecione um ou mais _model.smd. Somente esses BINs serão substituídos. Texturas PNG editadas podem ser importadas pela aba Texturas.\r\n");
        }finally{try{Directory.Delete(work,true);}catch{}}
    }
    private static void ValidateRig(CharacterSmdMetadata meta,Ps2BinSkeleton skeleton)
    {
        if(skeleton.Bones.Count!=meta.Bones.Count)throw new InvalidDataException("A quantidade de ossos do BIN de destino mudou desde a exportação.");for(int i=0;i<meta.Bones.Count;i++){var bone=meta.Bones[i];var actual=skeleton.Bones[i];if(actual.Id!=bone.GameId||bone.Local.Length!=3||actual.ParentId!=bone.RawParentId||Vector3.Distance(actual.LocalPosition,new(bone.Local[0],bone.Local[1],bone.Local[2]))>.1f)throw new InvalidDataException($"O esqueleto do BIN não corresponde à parte exportada (osso {i}). Exporte novamente a parte de destino.");}
    }
    public static void NormalizeForImport(string smd,CharacterSmdMetadata meta,string output)
    {
        var document=CharacterSmdDocument.Read(smd);var byName=meta.Bones.ToDictionary(b=>b.Name,StringComparer.Ordinal);if(document.Nodes.Count!=byName.Count||document.Nodes.Any(n=>!byName.ContainsKey(n.Name)))throw new InvalidDataException("Mantenha todos os ossos e seus nomes originais. O SMD não corresponde à parte exportada.");var ids=document.Nodes.ToDictionary(n=>n.Index,n=>byName[n.Name].Id);var names=document.Nodes.ToDictionary(n=>n.Index,n=>n.Name);
        foreach(var node in document.Nodes){var bone=byName[node.Name];if((node.Parent<0?-1:ids.GetValueOrDefault(node.Parent,-2))!=bone.ParentId)throw new InvalidDataException("A hierarquia dos ossos foi alterada no Blender.");if(!document.Bind.TryGetValue(node.Index,out var pose)||pose.Rotation.LengthSquared()>.000001f||Vector3.Distance(pose.Position,new Vector3(bone.Local[0],-bone.Local[2],bone.Local[1])/100f)>.001f)throw new InvalidDataException("A pose de referência foi alterada. Exporte o modelo em Reference/Rest Pose, sem incorporar a animação.");}
        var materials=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var material in meta.Materials){materials[material.Name]=material.Name;materials[Path.GetFileNameWithoutExtension(material.Name)]=material.Name;materials[material.OriginalName]=material.Name;}
        var map=new Dictionary<string,string>();foreach(string name in document.Triangles.Select(t=>t.Material).Distinct()){if(!materials.TryGetValue(Path.GetFileName(name),out var mapped))throw new InvalidDataException($"Material desconhecido: {name}. Preserve os nomes exportados para manter os índices TPL.");map[name]=mapped;}
        // Blender can reorder and renumber nodes without changing the rig. The
        // compiler writes bones in node order, so restore the original BIN order
        // from the sidecar while retaining the maps for parents, poses and weights.
        var nodesByName=document.Nodes.ToDictionary(n=>n.Name,StringComparer.Ordinal);
        document.Nodes.Clear();
        document.Nodes.AddRange(meta.Bones.Select(b=>nodesByName[b.Name]));
        document.Write(output,ids,names,map);string companion=Path.ChangeExtension(smd,".idxmaterial");if(!File.Exists(companion))throw new InvalidDataException("Mantenha o .idxmaterial exportado junto do SMD editado.");File.Copy(companion,Path.ChangeExtension(output,".idxmaterial"),true);
    }
    public static async Task<IReadOnlyList<CharacterSmdChange>> ImportAsync(string dat,IReadOnlyList<string> files,IProgress<string>? progress=null)
    {
        if(files.Count==0)throw new InvalidOperationException("Selecione pelo menos um SMD de modelo.");var imports=files.Select(f=>(File:f,Meta:ReadMetadata(f))).ToArray();if(imports.Select(i=>i.Meta.DatEntry).Distinct().Count()!=imports.Length)throw new InvalidOperationException("Selecione somente um SMD por BIN de destino.");if(imports.Any(i=>!i.Meta.SourceDat.Equals(Path.GetFileName(dat),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Os SMDs selecionados pertencem a outro DAT. Abra o DAT indicado na exportação.");
        string work=Path.Combine(Path.GetTempPath(),"re4_character_import_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);byte[] original=File.ReadAllBytes(dat);var changes=new List<CharacterSmdChange>();
        try{string stage=Path.Combine(work,Path.GetFileName(dat));File.WriteAllBytes(stage,original);
            foreach(var import in imports){progress?.Report($"Reimportando BIN {import.Meta.BinIndex:D2} • DAT #{import.Meta.DatEntry:D3}...");byte[] template=Ps2CharacterDatEditor.CaptureDatEntry(stage,import.Meta.DatEntry);var target=Ps2BinSkeletonReader.Read(template);ValidateRig(import.Meta,target);string normalized=Path.Combine(work,$"part_{import.Meta.DatEntry:D3}.smd");NormalizeForImport(import.File,import.Meta,normalized);string converted=await Ps2ModelConversionService.ConvertAsync(normalized,template,false);
                try{byte[] bin=File.ReadAllBytes(converted);var actual=Ps2BinSkeletonReader.Read(bin);ValidateRig(import.Meta,actual);Buffer.BlockCopy(template,checked((int)BitConverter.ToUInt32(template,4)),bin,checked((int)BitConverter.ToUInt32(bin,4)),target.Bones.Count*16);if(!actual.JointBlends.SequenceEqual(target.JointBlends))throw new InvalidDataException("O conversor alterou a tabela de articulações do esqueleto.");if(Ps2ScenarioReader.ReadStandaloneBin(bin).Count==0)throw new InvalidDataException("O BIN convertido não possui geometria.");Ps2CharacterDatEditor.RestoreDatEntry(stage,import.Meta.DatEntry,bin);changes.Add(new(import.Meta.DatEntry,import.Meta.BinIndex,template,Ps2CharacterDatEditor.CaptureDatEntry(stage,import.Meta.DatEntry)));}finally{File.Delete(converted);}
            }
            var before=NativeDatService.Read(dat);var after=NativeDatService.Read(stage);var touched=changes.Select(c=>c.DatEntry).ToHashSet();if(before.Entries.Count!=after.Entries.Count||before.Entries.Any(e=>!touched.Contains(e.Index)&&!e.Data.SequenceEqual(after.Entries.Single(a=>a.Index==e.Index).Data)))throw new InvalidDataException("A validação detectou alteração em uma entrada fora da seleção.");if(!SHA256.HashData(original).SequenceEqual(SHA256.HashData(File.ReadAllBytes(dat))))throw new InvalidOperationException("O DAT mudou durante a conversão. Nenhum arquivo foi substituído.");
            if(!File.Exists(dat+".bak"))File.WriteAllBytes(dat+".bak",original);string commit=dat+".character-import.tmp";try{File.Copy(stage,commit,true);File.Move(commit,dat,true);}finally{if(File.Exists(commit))File.Delete(commit);}return changes;
        }finally{try{Directory.Delete(work,true);}catch{}}
    }
}


