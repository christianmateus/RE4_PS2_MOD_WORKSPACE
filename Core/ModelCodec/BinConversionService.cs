using System.Globalization;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Decoding;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding.Structures;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec;

/// <summary>Compiles interchange meshes using the receiver's PS2 layout and rig flags.</summary>
public static class BinConversionService
{
    internal static bool UsesScenarioVertexLayout(byte[] receiver)
        => ReadReceiver(receiver).binType == BinType.ScenarioWithColors;

    public static byte[] Compile(string meshPath, byte[] receiver, string? materialPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            DecodedModel template = ReadReceiver(receiver);
            using var input = File.OpenRead(meshPath);
            PackedMesh mesh;
            EncodedBone[] bones;
            float scale;
            BoundingBox bounds;
            if (Path.GetExtension(meshPath).Equals(".smd", StringComparison.OrdinalIgnoreCase))
            {
                // Validate before entering the compiler, which otherwise truncates excess influences.
                var document = CharacterSmdDocument.Read(meshPath);
                var nodeIds = document.Nodes.Select(n => n.Index).ToHashSet();
                if (document.Nodes.Any(n => n.Parent >= 0 && !nodeIds.Contains(n.Parent)) ||
                    document.Triangles.SelectMany(t => new[] { t.A, t.B, t.C }).SelectMany(v => v.Links).Any(l => !nodeIds.Contains(l.Bone)))
                    throw new InvalidDataException("O SMD referencia um osso desconhecido.");
                SmdMeshCompiler.CompileSmd(input, out mesh, out bones, true, out scale, out bounds);
            }
            else
            {
                int rootBone = template.Bones.Length > 0 ? template.Bones[0].BoneID : 0;
                ObjMeshCompiler.CompileObj(input, true, rootBone, out mesh, out scale, out bounds);
                bones = template.Bones.Select(b => new EncodedBone(b.boneLine)).ToArray();
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (mesh.Nodes.Count == 0) throw new InvalidDataException("A malha não contém segmentos utilizáveis.");
            if (!float.IsFinite(scale) || scale <= 0) throw new InvalidDataException("A malha não possui uma escala representável no BIN.");
            var materials = ReadMaterials(materialPath, template);
            var options = new BinWriteOptions
            {
                IsScenarioBin = template.binType == BinType.ScenarioWithColors,
                EnableBonepairTag = template.ReturnsHasEnableBonepairTag(),
                EnableAdjacentBoneTag = template.ReturnsHasEnableAdjacentBoneTag(),
                EnableUnkFlag1 = template.ReturnsHasEnableUnkFlag1(),
                EnableUnkFlag2 = template.ReturnsHasEnableUnkFlag2(),
                EnableUnkFlag4 = template.ReturnsHasEnableUnkFlag4(),
                BonePairs = template.BonePairs?.Select(p => (p.Bone1, p.Bone2, p.Bone3, p.Bone4)).ToArray() ?? [],
                BoundingBox = bounds
            };
            using var output = new MemoryStream();
            BinMeshWriter.WriteModel(output, 0, out _, mesh, options, bones, scale, materials);
            cancellationToken.ThrowIfCancellationRequested();
            byte[] bin = output.ToArray();
            if (Ps2ScenarioReader.ReadStandaloneBin(bin).Count == 0)
                throw new InvalidDataException("O BIN gerado não contém faces renderizáveis.");
            return bin;
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    private static DecodedModel ReadReceiver(byte[] receiver)
    {
        if (receiver.Length < 0x50 || BitConverter.ToUInt16(receiver, 0) != 0x30)
            throw new InvalidDataException("BIN receptor inválido.");
        using var input = new MemoryStream(receiver, writable: false);
        return BinMeshDecoder.Decode(input, 0, out _);
    }

    private static MaterialTable ReadMaterials(string? path, DecodedModel template)
    {
        if (path == null) return DecodedMaterialMapper.Parser(template);
        using var input = File.OpenRead(path);
        if (Path.GetExtension(path).Equals(".idxmaterial", StringComparison.OrdinalIgnoreCase))
            return MaterialTableReader.Load(input);
        WavefrontMaterialReader.Load(input, out var wavefront);
        WavefrontMaterialConverter.Convert(wavefront, out var materials);
        if (materials.MaterialDic.TryGetValue("MATERIAL_SEM_TEXTURA", out var untextured)) untextured.diffuse_map = 255;
        return materials;
    }

    public static void ExportCharacter(byte[] receiver, string outputBinPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            DecodedModel model = ReadReceiver(receiver);
            string folder = Path.GetDirectoryName(Path.GetFullPath(outputBinPath))!;
            Directory.CreateDirectory(folder);
            string stem = Path.GetFileNameWithoutExtension(outputBinPath);
            ModelTextExporter.WriteSmd(model, folder, stem);
            cancellationToken.ThrowIfCancellationRequested();
            MaterialTextExporter.WriteMaterialTable(DecodedMaterialMapper.Parser(model), folder, stem);
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }
}
