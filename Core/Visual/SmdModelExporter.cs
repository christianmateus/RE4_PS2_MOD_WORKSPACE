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
    {
        if(entry.LocalTriangles.Count==0)throw new InvalidOperationException("A entry selecionada não possui geometria exportável.");
        string directory=Path.GetDirectoryName(outputPath)??".";Directory.CreateDirectory(directory);string stem=Path.GetFileNameWithoutExtension(outputPath);string mtlPath=Path.Combine(directory,stem+".mtl");var obj=new StringBuilder();var mtl=new StringBuilder();obj.AppendLine("# RE4 PS2 MOD WORKSPACE - SMD BIN export");obj.AppendLine($"mtllib {Escape(stem+".mtl")}");obj.AppendLine($"o BIN_{entry.BinId:D3}");
        int vertex=1;int currentMaterial=int.MinValue;foreach(ScenarioTriangle triangle in entry.LocalTriangles)
        {
            if(triangle.TextureIndex!=currentMaterial){currentMaterial=triangle.TextureIndex;obj.AppendLine($"usemtl {MaterialName(currentMaterial)}");}
            WriteVertex(obj,triangle.A);WriteVertex(obj,triangle.B);WriteVertex(obj,triangle.C);WriteUv(obj,triangle.UvA);WriteUv(obj,triangle.UvB);WriteUv(obj,triangle.UvC);obj.AppendLine($"f {vertex}/{vertex} {vertex+1}/{vertex+1} {vertex+2}/{vertex+2}");vertex+=3;
        }
        var textureService=new TextureWorkspaceService();foreach(int index in entry.LocalTriangles.Select(x=>x.TextureIndex).Distinct().OrderBy(x=>x))
        {
            string material=MaterialName(index);mtl.AppendLine($"newmtl {material}");mtl.AppendLine("Ka 1.000000 1.000000 1.000000");mtl.AppendLine("Kd 1.000000 1.000000 1.000000");mtl.AppendLine("d 1.000000");if(index>=0){string png=$"{stem}_texture_{index:D3}.png";textureService.ExportPng(tplPath,index,Path.Combine(directory,png));mtl.AppendLine($"map_Kd {Escape(png)}");}mtl.AppendLine();
        }
        File.WriteAllText(outputPath,obj.ToString(),new UTF8Encoding(false));File.WriteAllText(mtlPath,mtl.ToString(),new UTF8Encoding(false));
    }
    private static void WriteVertex(StringBuilder text,System.Numerics.Vector3 value)=>text.AppendLine(FormattableString.Invariant($"v {value.X:R} {value.Y:R} {value.Z:R}"));
    private static void WriteUv(StringBuilder text,System.Numerics.Vector2 value)=>text.AppendLine(FormattableString.Invariant($"vt {value.X:R} {value.Y:R}"));
    private static string MaterialName(int index)=>index<0?"material_sem_textura":$"texture_{index:D3}";
    private static string Escape(string value)=>value.Contains(' ')?$"\"{value}\"":value;
}
