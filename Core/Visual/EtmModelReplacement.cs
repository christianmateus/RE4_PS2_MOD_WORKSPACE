using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed record EtmReplacementRequest(EtmResource Resource, int? EmbeddedTextureIndex, string SourcePath, int SourceTextureIndex = 0);

public static class EtmModelReplacement
{
    public static IReadOnlyDictionary<int, byte[]> Prepare(IReadOnlyList<EtmReplacementRequest> requests)
    {
        var replacements = new Dictionary<int, byte[]>();
        var effReader = new EffTextureReader();
        var tplReader = new TplReader();
        foreach (EtmReplacementRequest request in requests.Where(x => x.EmbeddedTextureIndex == null))
        {
            string extension = Path.GetExtension(request.Resource.Name).ToLowerInvariant();
            if (!Path.GetExtension(request.SourcePath).Equals(extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"{request.Resource.Name}: selecione um arquivo {extension}.");
            byte[] data = File.ReadAllBytes(request.SourcePath);
            if (extension == ".bin")
            {
                if (Ps2ScenarioReader.ReadStandaloneBin(data).Count == 0)
                    throw new InvalidDataException($"{request.Resource.Name}: o BIN não contém faces renderizáveis.");
            }
            else if (extension == ".tpl") ValidateTpl(data, tplReader);
            else if (extension == ".eff" && effReader.ReadTextures(request.Resource.Data).Count > 0 && effReader.ReadTextures(data).Count == 0)
                throw new InvalidDataException($"{request.Resource.Name}: o EFF escolhido não contém texturas reconhecidas.");
            replacements.Add(request.Resource.FileOrder, data);
        }

        foreach (IGrouping<int, EtmReplacementRequest> group in requests.Where(x => x.EmbeddedTextureIndex != null).GroupBy(x => x.Resource.FileOrder))
        {
            if (replacements.ContainsKey(group.Key))
                throw new InvalidOperationException("Escolha substituir o EFF inteiro ou as texturas internas, não ambos.");
            byte[] data = (byte[])group.First().Resource.Data.Clone();
            var changedIndices = new HashSet<int>();
            foreach (EtmReplacementRequest request in group)
            {
                int index = request.EmbeddedTextureIndex!.Value;
                if (!changedIndices.Add(index)) throw new InvalidOperationException("A mesma textura interna foi selecionada duas vezes.");
                var target = effReader.ReadTextures(data);
                if ((uint)index >= (uint)target.Count) throw new InvalidDataException("Índice de textura EFF inválido.");
                var replacement = tplReader.ReadTexture(request.SourcePath, request.SourceTextureIndex);
                data = EffTextureArchive.Replace(data,index,replacement);
            }
            replacements.Add(group.Key, data);
        }
        return replacements;
    }

    private static void ValidateTpl(byte[] data, TplReader reader)
    {
        using var stream = new MemoryStream(data, false);
        using var binary = new BinaryReader(stream);
        if (data.Length < 8) throw new InvalidDataException("TPL truncado.");
        stream.Position = 4;
        uint count = binary.ReadUInt32();
        if (count == 0 || count > 4096) throw new InvalidDataException("TPL sem texturas válidas.");
        for (int i = 0; i < count; i++) reader.ReadTexture(binary, i);
    }
}
