using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public readonly record struct ExternalModelStats(int Vertices, int Faces)
{
    public const int SafeVertexLimit = 6000;
    public const int SafeFaceLimit = 8000;
    public bool IsLarge => Vertices > SafeVertexLimit || Faces > SafeFaceLimit;
}

public static class ExternalPs2ModelConverter
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

    private static ExternalModelStats AnalyzeObj(string path)
    {
        int vertices = 0, faces = 0;
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.TrimStart();
            if (line.StartsWith("v ", StringComparison.Ordinal)) vertices++;
            else if (line.StartsWith("f ", StringComparison.Ordinal))
            {
                int corners = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length - 1;
                if (corners >= 3) faces += corners - 2;
            }
        }
        if (vertices == 0 || faces == 0) throw new InvalidDataException("O OBJ não contém vértices e faces utilizáveis.");
        return new ExternalModelStats(vertices, faces);
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
        return new ExternalModelStats(vertexRecords, faces);
    }

    public static async Task<string> ConvertAsync(string converterPath, string modelPath, byte[] receiverTemplateBin, bool autoWeightObj = false, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(converterPath)) throw new FileNotFoundException("RE4 PS2 BIN Tool não encontrada.", converterPath);
        string temporary = Path.Combine(Path.GetTempPath(), "re4_external_model_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string stagedModel = Path.Combine(temporary, Path.GetFileName(modelPath));
            File.Copy(modelPath, stagedModel, true);
            if (Path.GetExtension(stagedModel).Equals(".obj", StringComparison.OrdinalIgnoreCase))
            {
                FitObjToReceiver(stagedModel, receiverTemplateBin);
                if (autoWeightObj)
                {
                    string weighted = Path.Combine(temporary, Path.GetFileNameWithoutExtension(stagedModel) + "_weighted.smd");
                    ObjToWeightedSmd(stagedModel, receiverTemplateBin, weighted);
                    stagedModel = weighted;
                }
            }
            string templateBin = Path.Combine(temporary, "receiver_template.BIN");
            File.WriteAllBytes(templateBin, receiverTemplateBin);
            async Task<(string Output, string Error)> RunToolAsync(params string[] arguments)
            {
                var start = new ProcessStartInfo
                {
                    FileName = converterPath,
                    WorkingDirectory = temporary,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                foreach (string argument in arguments) start.ArgumentList.Add(argument);
                using Process process = Process.Start(start) ?? throw new InvalidOperationException("Não foi possível iniciar a RE4 PS2 BIN Tool.");
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                string output = await outputTask, error = await errorTask;
                if (process.ExitCode != 0) throw new InvalidOperationException($"A ferramenta terminou com código {process.ExitCode}.\n{error}\n{output}".Trim());
                return (output, error);
            }

            // The PS2 tool repacks OBJ/SMD from a companion idxbin. Generate that
            // structural description from the selected receiver BIN automatically.
            (string templateOutput, string templateError) = await RunToolAsync(templateBin);
            string? templateIdxBin = Directory.EnumerateFiles(temporary, "*.idxbin", SearchOption.AllDirectories).FirstOrDefault();
            if (templateIdxBin == null)
                throw new InvalidOperationException($"A ferramenta não conseguiu extrair a estrutura .idxbin do BIN receptor.\n{templateError}\n{templateOutput}".Trim());
            string modelIdxBin = Path.ChangeExtension(stagedModel, ".idxbin");
            File.Copy(templateIdxBin, modelIdxBin, true);

            string sourceMtl = Path.ChangeExtension(modelPath, ".mtl");
            string? stagedMaterial = null;
            string sourceIdxMaterial = Path.ChangeExtension(modelPath, ".idxmaterial");
            if (File.Exists(sourceIdxMaterial)) { stagedMaterial = Path.Combine(temporary, Path.GetFileName(sourceIdxMaterial)); File.Copy(sourceIdxMaterial, stagedMaterial, true); }
            else if (File.Exists(sourceMtl)) { stagedMaterial = Path.Combine(temporary, Path.GetFileName(sourceMtl)); File.Copy(sourceMtl, stagedMaterial, true); }
            else
            {
                string? templateIdxMaterial = Directory.EnumerateFiles(temporary, "*.idxmaterial", SearchOption.AllDirectories).FirstOrDefault();
                if (templateIdxMaterial != null) { stagedMaterial = Path.ChangeExtension(stagedModel, ".idxmaterial"); File.Copy(templateIdxMaterial, stagedMaterial, true); }
            }
            string expectedBin = Path.ChangeExtension(stagedModel, ".BIN");
            if (File.Exists(expectedBin)) File.Delete(expectedBin);
            (string output, string error) = stagedMaterial == null ? await RunToolAsync(stagedModel) : await RunToolAsync(stagedModel, stagedMaterial);
            string? generated = File.Exists(expectedBin) ? expectedBin : Directory.EnumerateFiles(temporary, "*.bin", SearchOption.AllDirectories)
                .Where(path => !string.Equals(path, templateBin, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (generated == null) throw new InvalidOperationException($"A ferramenta terminou sem gerar um arquivo BIN.\n{error}\n{output}".Trim());
            string retained = Path.Combine(Path.GetTempPath(), "re4_generated_" + Guid.NewGuid().ToString("N") + ".bin");
            File.Copy(generated, retained, true); return retained;
        }
        finally { try { Directory.Delete(temporary, true); } catch { } }
    }

    private static void ObjToWeightedSmd(string objPath, byte[] receiverBin, string smdPath)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var faces = new List<(int A, int B, int C, int Ua, int Ub, int Uc)>();
        foreach (string raw in File.ReadLines(objPath))
        {
            string line = raw.Trim();
            string[] f = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length == 0) continue;
            if (f[0] == "v" && f.Length >= 4 && float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) && float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) && float.TryParse(f[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) vertices.Add(new Vector3(x, y, z));
            else if (f[0] == "vt" && f.Length >= 3 && float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float u) && float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) uvs.Add(new Vector2(u, 1f - v));
            else if (f[0] == "f" && f.Length >= 4)
            {
                var corners = new List<(int V, int U)>();
                for (int i = 1; i < f.Length; i++) { string[] p = f[i].Split('/'); if (!int.TryParse(p[0], out int vi)) continue; int ui = p.Length > 1 && int.TryParse(p[1], out int parsed) ? parsed : 0; corners.Add((vi < 0 ? vertices.Count + vi : vi - 1, ui < 0 ? uvs.Count + ui : ui - 1)); }
                for (int i = 1; i + 1 < corners.Count; i++) faces.Add((corners[0].V, corners[i].V, corners[i + 1].V, corners[0].U, corners[i].U, corners[i + 1].U));
            }
        }
        Ps2BinSkeleton skeleton = Ps2BinSkeletonReader.Read(receiverBin, "receiver");
        Vector3[] globals = new Vector3[skeleton.Bones.Count];
        for (int i = 0; i < skeleton.Bones.Count; i++) globals[i] = skeleton.Bones[i].LocalPosition + (skeleton.Bones[i].ParentIndex >= 0 ? globals[skeleton.Bones[i].ParentIndex] : Vector3.Zero);
        int BoneFor(Vector3 p) => Enumerable.Range(0, globals.Length).OrderBy(i => Vector3.DistanceSquared(p, globals[i])).First();
        using var sw = new StreamWriter(smdPath, false, Encoding.ASCII);
        sw.WriteLine("version 1\nnodes");
        foreach (var b in skeleton.Bones) sw.WriteLine($"{b.Id} \"bone_{b.Id}\" {(b.ParentId == 0xFF ? -1 : b.ParentId)}");
        sw.WriteLine("end\nskeleton\ntime 0");
        foreach (var b in skeleton.Bones) sw.WriteLine($"{b.Id} {b.LocalPosition.X.ToString("R", CultureInfo.InvariantCulture)} {b.LocalPosition.Y.ToString("R", CultureInfo.InvariantCulture)} {b.LocalPosition.Z.ToString("R", CultureInfo.InvariantCulture)} 0 0 0");
        sw.WriteLine("end\ntriangles");
        foreach (var face in faces)
        {
            Vector3 a = vertices[face.A], b = vertices[face.B], c = vertices[face.C];
            Vector3 n = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            sw.WriteLine("material");
            foreach ((Vector3 p, int uvIndex) in new[] { (a, face.Ua), (b, face.Ub), (c, face.Uc) })
            {
                int nearest = BoneFor(p); float d = MathF.Sqrt(Vector3.DistanceSquared(p, globals[nearest]));
                int second = Enumerable.Range(0, globals.Length).Where(i => i != nearest).OrderBy(i => Vector3.DistanceSquared(p, globals[i])).FirstOrDefault(nearest);
                float w = Math.Clamp(1f - d / 80f, 0.55f, 0.9f); Vector2 uv = uvIndex >= 0 && uvIndex < uvs.Count ? uvs[uvIndex] : Vector2.Zero;
                sw.WriteLine(string.Join(" ", skeleton.Bones[nearest].Id, p.X.ToString("R", CultureInfo.InvariantCulture), p.Y.ToString("R", CultureInfo.InvariantCulture), p.Z.ToString("R", CultureInfo.InvariantCulture), n.X.ToString("R", CultureInfo.InvariantCulture), n.Y.ToString("R", CultureInfo.InvariantCulture), n.Z.ToString("R", CultureInfo.InvariantCulture), uv.X.ToString("R", CultureInfo.InvariantCulture), uv.Y.ToString("R", CultureInfo.InvariantCulture), "2", skeleton.Bones[nearest].Id, w.ToString("R", CultureInfo.InvariantCulture), skeleton.Bones[second].Id, (1f - w).ToString("R", CultureInfo.InvariantCulture)));
            }
        }
        sw.WriteLine("end");
    }

    private static void FitObjToReceiver(string objPath, byte[] receiverTemplateBin)
    {
        string[] lines = File.ReadAllLines(objPath);
        var positions = new List<Vector3>();
        foreach (string raw in lines)
        {
            string line = raw.TrimStart();
            if (!line.StartsWith("v ", StringComparison.Ordinal)) continue;
            string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length >= 4 && float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) && float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) && float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) positions.Add(new Vector3(x, y, z));
        }
        if (positions.Count == 0) return;
        Vector3 sourceMin = positions.Aggregate(new Vector3(float.PositiveInfinity), Vector3.Min), sourceMax = positions.Aggregate(new Vector3(float.NegativeInfinity), Vector3.Max);
        (Vector3 targetMin, Vector3 targetMax) = Ps2CharacterDatEditor.GetBinBounds(receiverTemplateBin);
        Vector3 sourceSize = sourceMax - sourceMin, targetSize = targetMax - targetMin;
        float sourceExtent = Math.Max(sourceSize.X, Math.Max(sourceSize.Y, sourceSize.Z));
        float targetExtent = Math.Max(targetSize.X, Math.Max(targetSize.Y, targetSize.Z));
        if (sourceExtent < 0.000001f || targetExtent < 0.000001f) return;
        float scale = targetExtent / sourceExtent;
        Vector3 sourceCenter = (sourceMin + sourceMax) * 0.5f, targetCenter = (targetMin + targetMax) * 0.5f;
        for (int i = 0; i < lines.Length; i++)
        {
            string prefixTrimmed = lines[i].TrimStart();
            if (!prefixTrimmed.StartsWith("v ", StringComparison.Ordinal)) continue;
            string[] fields = prefixTrimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4 || !float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) || !float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) || !float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;
            Vector3 p = (new Vector3(x, y, z) - sourceCenter) * scale + targetCenter;
            lines[i] = $"v {p.X.ToString("R", CultureInfo.InvariantCulture)} {p.Y.ToString("R", CultureInfo.InvariantCulture)} {p.Z.ToString("R", CultureInfo.InvariantCulture)}" + (fields.Length > 4 ? " " + string.Join(' ', fields.Skip(4)) : "");
        }
        File.WriteAllLines(objPath, lines);
    }
}
