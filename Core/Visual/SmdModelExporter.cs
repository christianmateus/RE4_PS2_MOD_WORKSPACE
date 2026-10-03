using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class SmdModelExporter
{
    public static void ExportStandaloneObj(string binPath, string? tplPath, string outputPath)
    {
        ScenarioEntry entry = StandaloneEntry(binPath);
        if (tplPath != null) ValidateTextureIndices(entry.LocalTriangles, tplPath);
        WriteObjFiles(new[] { entry }, outputPath, false, tplPath);
    }

    public static void ExportStandaloneSmd(string binPath, string? tplPath, string outputPath)
    {
        ScenarioEntry entry = StandaloneEntry(binPath);
        string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        Directory.CreateDirectory(directory);
        string stem = Path.GetFileNameWithoutExtension(outputPath);
        if (tplPath != null) ValidateTextureIndices(entry.LocalTriangles, tplPath);
        var text = new StringBuilder("version 1\nnodes\n0 \"root\" -1\nend\nskeleton\ntime 0\n0 0 0 0 0 0 0\nend\ntriangles\n");
        foreach (ScenarioTriangle triangle in entry.LocalTriangles)
        {
            int index = triangle.TextureIndex;
            string material = index < 0 ? "material_sem_textura" : tplPath == null ? $"texture_{index:D3}" : $"{stem}_texture_{index:D3}.png";
            text.AppendLine(material);
            System.Numerics.Vector3 normal = System.Numerics.Vector3.Cross(triangle.B - triangle.A, triangle.C - triangle.A);
            normal = normal.LengthSquared() > 0.0000001f ? System.Numerics.Vector3.Normalize(normal) : System.Numerics.Vector3.UnitY;
            WriteSmdVertex(text, triangle.A, normal, triangle.UvA);
            WriteSmdVertex(text, triangle.B, normal, triangle.UvB);
            WriteSmdVertex(text, triangle.C, normal, triangle.UvC);
        }
        text.AppendLine("end");
        File.WriteAllText(outputPath, text.ToString(), new UTF8Encoding(false));
        if (tplPath != null) ExportStandaloneTextures(entry.LocalTriangles, tplPath, directory, stem);
    }

    private static ScenarioEntry StandaloneEntry(string binPath)
    {
        IReadOnlyList<ScenarioTriangle> triangles = Ps2ScenarioReader.ReadStandaloneBin(File.ReadAllBytes(binPath));
        if (triangles.Count == 0) throw new InvalidDataException("O BIN não contém faces renderizáveis.");
        return new ScenarioEntry { LocalTriangles = triangles };
    }

    private static void ValidateTextureIndices(IReadOnlyList<ScenarioTriangle> triangles, string tplPath)
    {
        uint count = new TplReader().ReadTextureCount(tplPath);
        int required = triangles.Select(triangle => triangle.TextureIndex).DefaultIfEmpty(-1).Max();
        if (required >= count) throw new InvalidDataException($"O BIN usa a textura #{required}, mas o TPL contém somente {count} textura(s).");
    }

    private static void ExportStandaloneTextures(IReadOnlyList<ScenarioTriangle> triangles, string tplPath, string directory, string stem)
    {
        var service = new TextureWorkspaceService();
        foreach (int index in triangles.Select(triangle => triangle.TextureIndex).Where(index => index >= 0).Distinct())
            service.ExportPng(tplPath, index, Path.Combine(directory, $"{stem}_texture_{index:D3}.png"));
    }

    private static void WriteSmdVertex(StringBuilder text, System.Numerics.Vector3 position, System.Numerics.Vector3 normal, System.Numerics.Vector2 uv) =>
        text.AppendLine(FormattableString.Invariant($"0 {position.X:R} {-position.Z:R} {position.Y:R} {normal.X:R} {-normal.Z:R} {normal.Y:R} {uv.X:R} {uv.Y:R}"));

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

    public static async Task<byte[]> JoinAsBinAsync(string smdPath,IReadOnlyList<ScenarioEntry> entries,int binCount,CancellationToken cancellationToken=default)
    {
        if(entries.Count<2)throw new InvalidOperationException("Selecione pelo menos duas entries para fazer o join.");
        ScenarioEntry? template=entries.FirstOrDefault(x=>x.LocalTriangles.Count>0);if(template==null)throw new InvalidOperationException("A seleção não possui geometria exportável.");
        byte[] receiver=SmdEmbeddedBinService.Extract(smdPath,template.BinId,binCount);
        string temporary=Path.Combine(Path.GetTempPath(),"re4_smd_join_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
        string objPath=Path.Combine(temporary,"joined.obj");
        try
        {
            WriteObjFiles(entries,objPath,true,null,invertTextureV:BinConversionService.UsesScenarioVertexLayout(receiver));
            string generated=await Ps2ModelConversionService.ConvertAsync(objPath,receiver,false,cancellationToken,new ExternalModelImportOptions(AutoFit:false));
            try
            {
                return File.ReadAllBytes(generated);
            }
            finally{try{File.Delete(generated);}catch{}}
        }
        finally{try{Directory.Delete(temporary,true);}catch{}}
    }

    private static void ExportObj(IReadOnlyList<ScenarioEntry> entries,string tplPath,string outputPath,bool applyEntryTransforms)
    {
        WriteObjFiles(entries,outputPath,applyEntryTransforms,tplPath);
    }

    private static void WriteObjFiles(IReadOnlyList<ScenarioEntry> entries,string outputPath,bool applyEntryTransforms,string? tplPath,bool invertTextureV=false)
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
                if(invertTextureV){uvA.Y=1f-uvA.Y;uvB.Y=1f-uvB.Y;uvC.Y=1f-uvC.Y;}
                if(applyEntryTransforms){a=Transform(a,entry);b=Transform(b,entry);c=Transform(c,entry);if(entry.ScaleX*entry.ScaleY*entry.ScaleZ<0f){(b,c)=(c,b);(uvB,uvC)=(uvC,uvB);}}
                WriteVertex(obj,a);WriteVertex(obj,b);WriteVertex(obj,c);WriteUv(obj,uvA);WriteUv(obj,uvB);WriteUv(obj,uvC);obj.AppendLine($"f {vertex}/{vertex} {vertex+1}/{vertex+1} {vertex+2}/{vertex+2}");vertex+=3;
            }
        }
        TextureWorkspaceService? textureService=tplPath==null?null:new TextureWorkspaceService();foreach(int index in entries.SelectMany(x=>x.LocalTriangles).Select(x=>x.TextureIndex).Distinct())
        {
            string material=MaterialName(index);mtl.AppendLine($"newmtl {material}");mtl.AppendLine("Ka 1.000000 1.000000 1.000000");mtl.AppendLine("Kd 1.000000 1.000000 1.000000");mtl.AppendLine("d 1.000000");if(index>=0){string png=textureService==null?$"{index:D3}.png":$"{stem}_texture_{index:D3}.png";if(textureService!=null)textureService.ExportPng(tplPath!,index,Path.Combine(directory,png));mtl.AppendLine($"map_Kd {Escape(png)}");}mtl.AppendLine();
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
