using System.Globalization;
using System.Numerics;
using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec;

/// <summary>Aligns OBJ geometry and optionally assigns receiver bones before compilation.</summary>
internal static class ModelImportPreparation
{
    public static void WriteWeightedSmd(string objPath, byte[] receiverBin, string smdPath)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var faces = new List<(int A, int B, int C, int Ua, int Ub, int Uc,string Material)>();
        string material="material";
        foreach (string raw in File.ReadLines(objPath))
        {
            string line = raw.Trim();
            string[] f = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length == 0) continue;
            if (f[0] == "v" && f.Length >= 4 && float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) && float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) && float.TryParse(f[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) vertices.Add(new Vector3(x, y, z));
            else if (f[0] == "vt" && f.Length >= 3 && float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float u) && float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) uvs.Add(new Vector2(u, v));
            else if(f[0]=="usemtl"&&f.Length>=2)material=string.Join(" ",f.Skip(1));
            else if (f[0] == "f" && f.Length >= 4)
            {
                var corners = new List<(int V, int U)>();
                for (int i = 1; i < f.Length; i++) { string[] p = f[i].Split('/'); if (!int.TryParse(p[0], out int vi)) continue; int ui = p.Length > 1 && int.TryParse(p[1], out int parsed) ? parsed : 0; corners.Add((vi < 0 ? vertices.Count + vi : vi - 1, ui < 0 ? uvs.Count + ui : ui - 1)); }
                for (int i = 1; i + 1 < corners.Count; i++) faces.Add((corners[0].V, corners[i].V, corners[i + 1].V, corners[0].U, corners[i].U, corners[i + 1].U,material));
            }
        }
        Ps2BinSkeleton skeleton = Ps2BinSkeletonReader.Read(receiverBin, "receiver");
        Vector3[] globals = new Vector3[skeleton.Bones.Count];
        for (int i = 0; i < skeleton.Bones.Count; i++) globals[i] = skeleton.Bones[i].LocalPosition + (skeleton.Bones[i].ParentIndex >= 0 ? globals[skeleton.Bones[i].ParentIndex] : Vector3.Zero);
        int BoneFor(Vector3 p) => Enumerable.Range(0, globals.Length).OrderBy(i => Vector3.DistanceSquared(p, globals[i])).First();
        using var sw = new StreamWriter(smdPath, false, System.Text.Encoding.ASCII);
        sw.WriteLine("version 1\nnodes");
        foreach (var b in skeleton.Bones) sw.WriteLine($"{b.Id} \"bone_{b.Id}\" {(b.ParentId == 0xFF ? -1 : b.ParentId)}");
        sw.WriteLine("end\nskeleton\ntime 0");
        // The SMD reader converts Z-up to game Y-up. Serialize the inverse basis.
        foreach (var b in skeleton.Bones) sw.WriteLine(FormattableString.Invariant($"{b.Id} {b.LocalPosition.X:R} {-b.LocalPosition.Z:R} {b.LocalPosition.Y:R} 0 0 0"));
        sw.WriteLine("end\ntriangles");
        foreach (var face in faces)
        {
            Vector3 a = vertices[face.A], b = vertices[face.B], c = vertices[face.C];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            Vector3 n = cross.LengthSquared() > 1e-10f ? Vector3.Normalize(cross) : Vector3.UnitY;
            sw.WriteLine(face.Material);
            foreach ((Vector3 p, int uvIndex) in new[] { (a, face.Ua), (b, face.Ub), (c, face.Uc) })
            {
                int nearest = BoneFor(p); float d = MathF.Sqrt(Vector3.DistanceSquared(p, globals[nearest]));
                int second = Enumerable.Range(0, globals.Length).Where(i => i != nearest).OrderBy(i => Vector3.DistanceSquared(p, globals[i])).FirstOrDefault(nearest);
                float w = Math.Clamp(1f - d / 80f, 0.55f, 0.9f); Vector2 uv = uvIndex >= 0 && uvIndex < uvs.Count ? uvs[uvIndex] : Vector2.Zero;
                sw.WriteLine(string.Join(" ", skeleton.Bones[nearest].Id, p.X.ToString("R", CultureInfo.InvariantCulture), (-p.Z).ToString("R", CultureInfo.InvariantCulture), p.Y.ToString("R", CultureInfo.InvariantCulture), n.X.ToString("R", CultureInfo.InvariantCulture), (-n.Z).ToString("R", CultureInfo.InvariantCulture), n.Y.ToString("R", CultureInfo.InvariantCulture), uv.X.ToString("R", CultureInfo.InvariantCulture), uv.Y.ToString("R", CultureInfo.InvariantCulture), "2", skeleton.Bones[nearest].Id, w.ToString("R", CultureInfo.InvariantCulture), skeleton.Bones[second].Id, (1f - w).ToString("R", CultureInfo.InvariantCulture)));
            }
        }
        sw.WriteLine("end");
    }

    public static void AlignObj(string objPath,byte[] receiverTemplateBin,ExternalModelImportOptions options)
    {
        NormalizeFaceReferences(objPath);
        string[] lines=File.ReadAllLines(objPath);
        var source=Ps2ModelConversionService.ReadPreview(objPath);var receiver=Ps2ScenarioReader.ReadStandaloneBin(receiverTemplateBin);
        var q=Quaternion.CreateFromYawPitchRoll(options.RotationDegrees.Y*MathF.PI/180f,options.RotationDegrees.X*MathF.PI/180f,options.RotationDegrees.Z*MathF.PI/180f);
        for(int i=0;i<lines.Length;i++)
        {
            string[] fields=lines[i].Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
            if(fields.Length<4||fields[0] is not ("v" or "vn"))continue;
            if(!float.TryParse(fields[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float x)||!float.TryParse(fields[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float y)||!float.TryParse(fields[3],NumberStyles.Float,CultureInfo.InvariantCulture,out float z))continue;
            Vector3 p=new(x,y,z);
            if(fields[0]=="v")p=Ps2ModelConversionService.TransformPreviewPoint(p,source,receiver,options);
            else{if(options.BlenderZUp)p=new(p.X,p.Z,-p.Y);p=Vector3.Transform(p,q);}
            lines[i]=FormattableString.Invariant($"{fields[0]} {p.X:R} {p.Y:R} {p.Z:R}")+(fields.Length>4?" "+string.Join(' ',fields.Skip(4)):"");
        }
        File.WriteAllLines(objPath,lines);
    }

    private static void NormalizeFaceReferences(string path)
    {
        string[] lines = File.ReadAllLines(path);
        int positions = 0, textures = 0, normals = 0;
        for (int line = 0; line < lines.Length; line++)
        {
            string[] fields = lines[line].Split('#')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 0) continue;
            if (fields[0] == "v") positions++;
            else if (fields[0] == "vt") textures++;
            else if (fields[0] == "vn") normals++;
            else if (fields[0] == "f")
            {
                if (fields.Length < 4) throw new InvalidDataException($"Face incompleta na linha {line + 1}.");
                for (int corner = 1; corner < fields.Length; corner++)
                {
                    string[] indices = fields[corner].Split('/');
                    if (indices.Length > 3) throw new InvalidDataException("Referência OBJ inválida.");
                    for (int component = 0; component < indices.Length; component++)
                    {
                        if (component > 0 && indices[component].Length == 0) continue;
                        if (!int.TryParse(indices[component], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                            throw new InvalidDataException($"Índice OBJ inválido na linha {line + 1}.");
                        int count = component == 0 ? positions : component == 1 ? textures : normals;
                        if (index < 0) index = count + index + 1;
                        if (index <= 0 || index > count) throw new InvalidDataException($"Índice OBJ fora da faixa na linha {line + 1}.");
                        indices[component] = index.ToString(CultureInfo.InvariantCulture);
                    }
                    fields[corner] = string.Join('/', indices);
                }
                lines[line] = string.Join(' ', fields);
            }
            if (fields[0] is "v" or "vt" or "vn")
            {
                foreach (string value in fields.Skip(1))
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) || !float.IsFinite(number))
                        throw new InvalidDataException($"Coordenada OBJ inválida na linha {line + 1}.");
            }
        }
        File.WriteAllLines(path, lines);
    }
}
