using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class CharacterSmdDocument
{
    private static readonly CultureInfo C=CultureInfo.InvariantCulture;
    public sealed record Node(int Index,string Name,int Parent);
    public sealed record BonePose(Vector3 Position,Vector3 Rotation);
    public sealed record Vertex(int Parent,Vector3 Position,Vector3 Normal,Vector2 Uv,List<(int Bone,float Weight)> Links);
    public sealed record Triangle(string Material,Vertex A,Vertex B,Vertex C);
    public List<Node> Nodes {get;}=new();public Dictionary<int,BonePose> Bind {get;}=new();public List<Triangle> Triangles {get;}=new();
    private static string N(float f)=>f.ToString("0.################",C);
    private static float F(string s){float v=float.Parse(s,C);if(!float.IsFinite(v))throw new InvalidDataException("O SMD contém coordenadas ou pesos não finitos.");return v;}
    public static CharacterSmdDocument Read(string path)
    {
        var result=new CharacterSmdDocument();string[] lines=File.ReadAllLines(path).Select(l=>l.Trim()).ToArray();string section="";int frame=-1;
        for(int i=0;i<lines.Length;i++){
            string line=lines[i];if(line.Length==0||line.StartsWith("//"))continue;if(line is "nodes" or "skeleton" or "triangles"){section=line;continue;}if(line=="end"){section="";continue;}
            string[] Fields(string value)=>value.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
            if(section=="nodes"){var match=Regex.Match(line,"^(?<id>-?[0-9]+)\\s+\"(?<name>[^\"]+)\"\\s+(?<parent>-?[0-9]+)$");if(!match.Success)throw new InvalidDataException("Linha de osso inválida no SMD.");result.Nodes.Add(new(int.Parse(match.Groups["id"].Value,C),match.Groups["name"].Value,int.Parse(match.Groups["parent"].Value,C)));}
            else if(section=="skeleton"){var f=Fields(line);if(f[0]=="time"){frame=int.Parse(f[1],C);continue;}if(frame==0){if(f.Length<7)throw new InvalidDataException("Pose inválida no SMD.");result.Bind.Add(int.Parse(f[0],C),new(new(F(f[1]),F(f[2]),F(f[3])),new(F(f[4]),F(f[5]),F(f[6]))));}}
            else if(section=="triangles"){
                Vertex ReadVertex(string value){var f=Fields(value);if(f.Length<9)throw new InvalidDataException("Vértice inválido no SMD.");int parent=int.Parse(f[0],C),count=f.Length>9?int.Parse(f[9],C):0;if(count<0||count>3||f.Length<10+count*2&&count>0)throw new InvalidDataException("O PS2 suporta no máximo 3 influências por vértice. Ajuste os pesos no Blender.");var links=new List<(int,float)>();for(int j=0;j<count;j++){float weight=F(f[11+j*2]);if(weight<0)throw new InvalidDataException("Peso negativo no SMD.");if(weight>0)links.Add((int.Parse(f[10+j*2],C),weight));}if(links.Count==0)links.Add((parent,1));if(Math.Abs(links.Sum(l=>l.Item2)-1)>0.02f)throw new InvalidDataException("Normalize os pesos dos vértices no Blender.");return new(parent,new(F(f[1]),F(f[2]),F(f[3])),new(F(f[4]),F(f[5]),F(f[6])),new(F(f[7]),F(f[8])),links);}
                if(i+3>=lines.Length)throw new InvalidDataException("Triângulo incompleto no SMD.");result.Triangles.Add(new(line,ReadVertex(lines[++i]),ReadVertex(lines[++i]),ReadVertex(lines[++i])));
            }
        }
        if(result.Nodes.Count==0||result.Nodes.Select(n=>n.Index).Distinct().Count()!=result.Nodes.Count||result.Nodes.Select(n=>n.Name).Distinct(StringComparer.Ordinal).Count()!=result.Nodes.Count||result.Triangles.Count==0)throw new InvalidDataException("Use o SMD de modelo exportado, com esqueleto e triângulos. O SMD de animação não substitui uma malha.");return result;
    }
    public void Write(string path,IReadOnlyDictionary<int,int> ids,IReadOnlyDictionary<int,string> names,IReadOnlyDictionary<string,string>? materials=null)
    {
        using var w=new StreamWriter(path);w.WriteLine("version 1\nnodes");foreach(var n in Nodes){if(!ids.ContainsKey(n.Index)||n.Parent>=0&&!ids.ContainsKey(n.Parent))throw new InvalidDataException("O SMD referencia um osso desconhecido.");w.WriteLine($"{ids[n.Index]} \"{names[n.Index]}\" {(n.Parent<0?-1:ids[n.Parent])}");}w.WriteLine("end\nskeleton\ntime 0");foreach(var n in Nodes){if(!Bind.TryGetValue(n.Index,out var p))throw new InvalidDataException("O SMD não possui pose de referência para todos os ossos.");w.WriteLine($"{ids[n.Index]} {N(p.Position.X)} {N(p.Position.Y)} {N(p.Position.Z)} {N(p.Rotation.X)} {N(p.Rotation.Y)} {N(p.Rotation.Z)}");}w.WriteLine("end\ntriangles");
        foreach(var t in Triangles){w.WriteLine(materials?.GetValueOrDefault(t.Material)??t.Material);foreach(var v in new[]{t.A,t.B,t.C}){foreach(var link in v.Links)if(!ids.ContainsKey(link.Bone))throw new InvalidDataException("Há um peso ligado a um osso desconhecido.");int primary=v.Links.OrderByDescending(l=>l.Weight).First().Bone;var values=new List<string>{ids[primary].ToString(C),N(v.Position.X),N(v.Position.Y),N(v.Position.Z),N(v.Normal.X),N(v.Normal.Y),N(v.Normal.Z),N(v.Uv.X),N(v.Uv.Y),v.Links.Count.ToString(C)};foreach(var link in v.Links){values.Add(ids[link.Bone].ToString(C));values.Add(N(link.Weight));}w.WriteLine(string.Join(" ",values));}}w.WriteLine("end");
    }
}

