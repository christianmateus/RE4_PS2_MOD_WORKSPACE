using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class SmdModelExporter
{
    public static void ExportBin(string smdPath,ScenarioEntry entry,int binCount,string outputPath)
    {
        File.WriteAllBytes(outputPath,SmdEmbeddedBinService.Extract(smdPath,entry.BinId,binCount));
    }

    public static void ExportObj(ScenarioEntry entry,string tplPath,string outputPath)
        => ExportObj(new[]{entry},tplPath,outputPath,false);

    public static void ExportCombinedObj(IReadOnlyList<ScenarioEntry> entries,string tplPath,string outputPath)
        => ExportObj(entries,tplPath,outputPath,true);

    public static void ExportSeparateObjs(IReadOnlyList<ScenarioEntry> entries,string tplPath,string outputDirectory)
    {
        if(entries.Count==0)throw new InvalidOperationException("Nenhuma entry foi selecionada.");
        Directory.CreateDirectory(outputDirectory);
        foreach(ScenarioEntry entry in entries)
            ExportObj(new[]{entry},tplPath,Path.Combine(outputDirectory,$"entry_{entry.FileOrder:D3}_bin_{entry.BinId:D3}.obj"),false);
    }

    public static void ExportSeparateBins(string smdPath,IReadOnlyList<ScenarioEntry> entries,int binCount,string outputDirectory)
    {
        if(entries.Count==0)throw new InvalidOperationException("Nenhuma entry foi selecionada.");
        Directory.CreateDirectory(outputDirectory);
        foreach(ScenarioEntry entry in entries)
            ExportBin(smdPath,entry,binCount,Path.Combine(outputDirectory,$"entry_{entry.FileOrder:D3}_bin_{entry.BinId:D3}.bin"));
    }

    public static async Task<byte[]> JoinAsBinAsync(string converterPath,string smdPath,IReadOnlyList<ScenarioEntry> entries,int binCount,CancellationToken cancellationToken=default)
    {
        if(entries.Count<2)throw new InvalidOperationException("Selecione pelo menos duas entries para fazer o join.");
        ScenarioEntry? template=entries.FirstOrDefault(x=>x.LocalTriangles.Count>0);if(template==null)throw new InvalidOperationException("A seleção não possui geometria exportável.");
        byte[] receiver=SmdEmbeddedBinService.Extract(smdPath,template.BinId,binCount);
        string temporary=Path.Combine(Path.GetTempPath(),"re4_smd_join_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
        string objPath=Path.Combine(temporary,"joined.obj");
        try
        {
            WriteObjFiles(entries,objPath,true,null);
            string generated=await ExternalPs2ModelConverter.ConvertAsync(converterPath,objPath,receiver,false,cancellationToken,new ExternalModelImportOptions(AutoFit:false));
            try
            {
                byte[] joined=File.ReadAllBytes(generated);int materialCount=SmdEmbeddedBinService.ReadBinMaterialTextures(joined).Count;
                int[] textures=entries.SelectMany(x=>x.LocalTriangles).Select(x=>x.TextureIndex).Distinct().ToArray();
                for(int i=0;i<materialCount;i++)SmdEmbeddedBinService.SetBinMaterialTextureAt(joined,i,i<textures.Length&&textures[i]>=0?(byte)Math.Min(byte.MaxValue,textures[i]):(byte)0xFF);
                return joined;
            }
            finally{try{File.Delete(generated);}catch{}}
        }
        finally{try{Directory.Delete(temporary,true);}catch{}}
    }

    private static void ExportObj(IReadOnlyList<ScenarioEntry> entries,string tplPath,string outputPath,bool applyEntryTransforms)
    {
        WriteObjFiles(entries,outputPath,applyEntryTransforms,tplPath);
    }

    private static void WriteObjFiles(IReadOnlyList<ScenarioEntry> entries,string outputPath,bool applyEntryTransforms,string? tplPath)
    {
        if(entries.Count==0||entries.All(x=>x.LocalTriangles.Count==0))throw new InvalidOperationException("A seleção não possui geometria exportável.");
        string directory=Path.GetDirectoryName(outputPath)??".";Directory.CreateDirectory(directory);string stem=Path.GetFileNameWithoutExtension(outputPath);string mtlPath=Path.Combine(directory,stem+".mtl");var obj=new StringBuilder();var mtl=new StringBuilder();obj.AppendLine("# RE4 PS2 MOD WORKSPACE - SMD export");obj.AppendLine($"mtllib {Escape(stem+".mtl")}");
        int vertex=1;int currentMaterial=int.MinValue;foreach(ScenarioEntry entry in entries)
        {
            obj.AppendLine($"o ENTRY_{entry.FileOrder:D3}_BIN_{entry.BinId:D3}");
            foreach(ScenarioTriangle triangle in entry.LocalTriangles)
            {
                if(triangle.TextureIndex!=currentMaterial){currentMaterial=triangle.TextureIndex;obj.AppendLine($"usemtl {MaterialName(currentMaterial)}");}
                System.Numerics.Vector3 a=triangle.A,b=triangle.B,c=triangle.C;System.Numerics.Vector2 uvA=triangle.UvA,uvB=triangle.UvB,uvC=triangle.UvC;
                if(applyEntryTransforms){a=Transform(a,entry);b=Transform(b,entry);c=Transform(c,entry);if(entry.ScaleX*entry.ScaleY*entry.ScaleZ<0f){(b,c)=(c,b);(uvB,uvC)=(uvC,uvB);}}
                WriteVertex(obj,a);WriteVertex(obj,b);WriteVertex(obj,c);WriteUv(obj,uvA);WriteUv(obj,uvB);WriteUv(obj,uvC);obj.AppendLine($"f {vertex}/{vertex} {vertex+1}/{vertex+1} {vertex+2}/{vertex+2}");vertex+=3;
            }
        }
        TextureWorkspaceService? textureService=tplPath==null?null:new TextureWorkspaceService();foreach(int index in entries.SelectMany(x=>x.LocalTriangles).Select(x=>x.TextureIndex).Distinct())
        {
            string material=MaterialName(index);mtl.AppendLine($"newmtl {material}");mtl.AppendLine("Ka 1.000000 1.000000 1.000000");mtl.AppendLine("Kd 1.000000 1.000000 1.000000");mtl.AppendLine("d 1.000000");if(index>=0&&textureService!=null){string png=$"{stem}_texture_{index:D3}.png";textureService.ExportPng(tplPath!,index,Path.Combine(directory,png));mtl.AppendLine($"map_Kd {Escape(png)}");}mtl.AppendLine();
        }
        File.WriteAllText(outputPath,obj.ToString(),new UTF8Encoding(false));File.WriteAllText(mtlPath,mtl.ToString(),new UTF8Encoding(false));
    }
    private static System.Numerics.Vector3 Transform(System.Numerics.Vector3 value,ScenarioEntry entry)
    {
        value*=entry.Scale;float c=MathF.Cos(entry.RotationX),s=MathF.Sin(entry.RotationX);value=new(value.X,value.Y*c-value.Z*s,value.Y*s+value.Z*c);c=MathF.Cos(entry.RotationY);s=MathF.Sin(entry.RotationY);value=new(value.X*c+value.Z*s,value.Y,-value.X*s+value.Z*c);c=MathF.Cos(entry.RotationZ);s=MathF.Sin(entry.RotationZ);value=new(value.X*c-value.Y*s,value.X*s+value.Y*c,value.Z);return value+entry.Position;
    }
    private static void WriteVertex(StringBuilder text,System.Numerics.Vector3 value)=>text.AppendLine(FormattableString.Invariant($"v {value.X:R} {value.Y:R} {value.Z:R}"));
    private static void WriteUv(StringBuilder text,System.Numerics.Vector2 value)=>text.AppendLine(FormattableString.Invariant($"vt {value.X:R} {value.Y:R}"));
    private static string MaterialName(int index)=>index<0?"material_sem_textura":$"texture_{index:D3}";
    private static string Escape(string value)=>value.Contains(' ')?$"\"{value}\"":value;
}
