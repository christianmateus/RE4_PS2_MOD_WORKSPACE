using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures;

/// <summary>Reads the main textures of the TPL archives nested inside an EFF.</summary>
public sealed class EffTextureReader
{
    public IReadOnlyList<TplModel> ReadTextures(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return EffTextureArchive.Entries(data).Select(e=>e.Texture).ToArray();
    }
}
