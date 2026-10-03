using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public readonly record struct ExternalModelStats(int Vertices, int Faces, int Uvs=0, int Materials=0, bool HasSkeleton=false)
{
    public const int SafeVertexLimit = 6000;
    public const int SafeFaceLimit = 8000;
    public bool IsLarge => Vertices > SafeVertexLimit || Faces > SafeFaceLimit;
}

public static class Ps2ModelConversionService
{
    private static readonly Regex SmdVertex = new(@"^\s*-?\d+\s+[-+0-9.eE]+\s+[-+0-9.eE]+\s+[-+0-9.eE]+\s+", RegexOptions.Compiled);

    public static ExternalModelStats Analyze(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".obj" => AnalyzeObj(path),
            ".smd" => AnalyzeSmd(path),
            _ => throw new NotSupportedException("Use um modelo Wavefront OBJ ou StudioModelData SMD.")
        };
    }

    public static IReadOnlyList<ScenarioTriangle> ReadPreview(string path)
    {
        string extension=Path.GetExtension(path).ToLowerInvariant();
        return extension==".obj"?ReadObjPreview(path):extension==".smd"?ReadSmdPreview(path):Array.Empty<ScenarioTriangle>();
    }

    private static IReadOnlyList<ScenarioTriangle> ReadObjPreview(string path)
    {
        var vertices=new List<Vector3>();var result=new List<ScenarioTriangle>();
        foreach(string raw in File.ReadLines(path)){string[] f=raw.Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(f.Length==0)continue;if(f[0]=="v"&&f.Length>=4&&float.TryParse(f[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float x)&&float.TryParse(f[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float y)&&float.TryParse(f[3],NumberStyles.Float,CultureInfo.InvariantCulture,out float z))vertices.Add(new(x,y,z));else if(f[0]=="f"&&f.Length>=4){var ids=new List<int>();for(int i=1;i<f.Length;i++){string token=f[i].Split('/')[0];if(int.TryParse(token,out int id))ids.Add(id<0?vertices.Count+id:id-1);}for(int i=1;i+1<ids.Count;i++)if(ids[0]>=0&&ids[0]<vertices.Count&&ids[i]>=0&&ids[i]<vertices.Count&&ids[i+1]>=0&&ids[i+1]<vertices.Count)result.Add(new ScenarioTriangle(vertices[ids[0]],vertices[ids[i]],vertices[ids[i+1]],Vector2.Zero,Vector2.Zero,Vector2.Zero,0));}}
        return result;
    }
    private static IReadOnlyList<ScenarioTriangle> ReadSmdPreview(string path)
    {
        var points=new List<Vector3>();bool triangles=false;foreach(string raw in File.ReadLines(path)){string line=raw.Trim();if(!triangles){if(line.Equals("triangles",StringComparison.OrdinalIgnoreCase))triangles=true;continue;}if(line.Equals("end",StringComparison.OrdinalIgnoreCase))break;string[] f=line.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(f.Length>=4&&int.TryParse(f[0],out _)&&float.TryParse(f[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float x)&&float.TryParse(f[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float y)&&float.TryParse(f[3],NumberStyles.Float,CultureInfo.InvariantCulture,out float z))points.Add(new(x,y,z));}var result=new List<ScenarioTriangle>();for(int i=0;i+2<points.Count;i+=3)result.Add(new ScenarioTriangle(points[i],points[i+1],points[i+2],Vector2.Zero,Vector2.Zero,Vector2.Zero,0));return result;
    }

    public static Vector3 TransformPreviewPoint(Vector3 point,IReadOnlyList<ScenarioTriangle> source,IReadOnlyList<ScenarioTriangle> receiver,ExternalModelImportOptions options)
    {
        Vector3 Convert(Vector3 p)=>options.BlenderZUp?new(p.X,p.Z,-p.Y):p;
        point=Convert(point);
        Quaternion rotation=Quaternion.CreateFromYawPitchRoll(options.RotationDegrees.Y*MathF.PI/180f,options.RotationDegrees.X*MathF.PI/180f,options.RotationDegrees.Z*MathF.PI/180f);
        if(!options.AutoFit)return Vector3.Transform(point*options.Scale,rotation)+options.Translation;
        Vector3[] sp=source.SelectMany(t=>new[]{t.A,t.B,t.C}).Select(Convert).ToArray(),tp=receiver.SelectMany(t=>new[]{t.A,t.B,t.C}).ToArray();
        if(sp.Length==0||tp.Length==0)return point;
        Vector3 smin=sp.Aggregate(new Vector3(float.PositiveInfinity),Vector3.Min),smax=sp.Aggregate(new Vector3(float.NegativeInfinity),Vector3.Max);
        Vector3 tmin=tp.Aggregate(new Vector3(float.PositiveInfinity),Vector3.Min),tmax=tp.Aggregate(new Vector3(float.NegativeInfinity),Vector3.Max);
        float se=Math.Max((smax-smin).X,Math.Max((smax-smin).Y,(smax-smin).Z)),te=Math.Max((tmax-tmin).X,Math.Max((tmax-tmin).Y,(tmax-tmin).Z));
        float fit=se>1e-6f?te/se:1;
        return Vector3.Transform((point-(smin+smax)*.5f)*(fit*options.Scale),rotation)+(tmin+tmax)*.5f+options.Translation;
    }

    private static ExternalModelStats AnalyzeObj(string path)
    {
        int vertices = 0, faces = 0,uvs=0;var materials=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.TrimStart();
            if (line.StartsWith("v ", StringComparison.Ordinal)) vertices++;
            else if(line.StartsWith("vt ",StringComparison.Ordinal))uvs++;
            else if(line.StartsWith("usemtl ",StringComparison.Ordinal))materials.Add(line[7..].Trim());
            else if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                int corners = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length - 1;
                if (corners >= 3) faces += corners - 2;
            }
        }
        if (vertices == 0 || faces == 0) throw new InvalidDataException("O OBJ não contém vértices e faces utilizáveis.");
        return new ExternalModelStats(vertices, faces,uvs,materials.Count,false);
    }

    private static ExternalModelStats AnalyzeSmd(string path)
    {
        bool triangles = false; int vertexRecords = 0;
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Trim();
            if (!triangles) { if (line.Equals("triangles", StringComparison.OrdinalIgnoreCase)) triangles = true; continue; }
            if (line.Equals("end", StringComparison.OrdinalIgnoreCase)) break;
            if (SmdVertex.IsMatch(line)) vertexRecords++;
        }
        int faces = vertexRecords / 3;
        if (faces == 0) throw new InvalidDataException("O SMD não contém triângulos utilizáveis.");
        return new ExternalModelStats(vertexRecords, faces,vertexRecords,0,true);
    }

    public static Task<string> ConvertAsync(string modelPath, byte[] receiverTemplateBin, bool autoWeightObj = false, CancellationToken cancellationToken = default, ExternalModelImportOptions? options = null)
        => Task.Run(() => CompileModel(modelPath, receiverTemplateBin, autoWeightObj, cancellationToken, options), cancellationToken);

    private static string CompileModel(string modelPath, byte[] receiverTemplateBin, bool autoWeightObj, CancellationToken cancellationToken, ExternalModelImportOptions? options)
    {
        string temporary = Path.Combine(Path.GetTempPath(), "re4_native_model_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Analyze(modelPath);
            string stagedModel = Path.Combine(temporary, Path.GetFileName(modelPath));
            File.Copy(modelPath, stagedModel, true);
            if (Path.GetExtension(stagedModel).Equals(".obj", StringComparison.OrdinalIgnoreCase))
            {
                ModelImportPreparation.AlignObj(stagedModel, receiverTemplateBin, options ?? new ExternalModelImportOptions());
                if (autoWeightObj)
                {
                    string weighted = Path.Combine(temporary, "weighted.smd");
                    ModelImportPreparation.WriteWeightedSmd(stagedModel, receiverTemplateBin, weighted);
                    stagedModel = weighted;
                }
            }
            string? material = null;
            string sidecar = Path.ChangeExtension(modelPath, ".idxmaterial");
            string mtl = Path.ChangeExtension(modelPath, ".mtl");
            if (File.Exists(sidecar)) material = sidecar;
            else if (File.Exists(mtl))
            {
                material = Path.Combine(temporary, "materials.mtl");
                string[] lines = File.ReadAllLines(mtl);
                if (autoWeightObj)
                {
                    int textureIndex = 0;
                    for (int i = 0; i < lines.Length; i++)
                        if (lines[i].TrimStart().StartsWith("map_Kd ", StringComparison.Ordinal))
                            lines[i] = $"map_Kd {textureIndex++:D3}.png";
                }
                File.WriteAllLines(material, lines);
            }
            byte[] bin = BinConversionService.Compile(stagedModel, receiverTemplateBin, material, cancellationToken);
            string retained = Path.Combine(Path.GetTempPath(), "re4_generated_" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(retained, bin);
            return retained;
        }
        finally { try { Directory.Delete(temporary, true); } catch { } }
    }

    public static Task ExtractCharacterSmdAsync(byte[] bin, string outputBinPath, CancellationToken cancellationToken = default)
        => Task.Run(() => BinConversionService.ExportCharacter(bin, outputBinPath, cancellationToken), cancellationToken);


}

public sealed record ExternalModelImportOptions(bool AutoFit=true,float Scale=1f,Vector3 Translation=default,Vector3 RotationDegrees=default,bool BlenderZUp=false);

