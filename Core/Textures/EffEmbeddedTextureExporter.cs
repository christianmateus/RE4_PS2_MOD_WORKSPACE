namespace RE4_PS2_MOD_WORKSPACE.Core.Textures;

/// <summary>Exports individual TPL-compatible textures embedded in an ETM EFF payload.</summary>
public static class EffEmbeddedTextureExporter
{
    public static IReadOnlyList<TPLDefinition.TPL> Read(byte[] effData) => new EffTextureReader().ReadTextures(effData);

    public static Bitmap DecodePng(byte[] effData, int index)
    {
        TPLDefinition.TPL texture = At(Read(effData), index);
        using var stream = new MemoryStream(effData, false);
        using var reader = new BinaryReader(stream);
        return new TextureDecoder().Decode(texture, reader);
    }

    public static void ExportTpl(byte[] effData, int index, string outputPath)
    {
        TPLDefinition.TPL texture = At(Read(effData), index);
        // EFF records expose the main image. Export it as a self-contained one-image TPL.
        texture.mipmapCount = 0;
        new TplWriter(new TplReader()).RebuildFile(outputPath, new[] { texture });
        TPLDefinition.TPL verified = new TplReader().ReadTexture(outputPath, 0);
        if (verified.width != texture.width || verified.height != texture.height || verified.bitDepth != texture.bitDepth)
            throw new InvalidDataException("O TPL exportado não corresponde à textura EFF.");
    }

    public static byte[] Transform(byte[] source,int index,RotateFlipType transform)
    {
        var target=At(Read(source),index);
        if(target.mipmapCount!=0)throw new NotSupportedException("Esta textura EFF possui mipmaps; a transformação exige atualizar também todos os níveis.");
        string temporary=Path.Combine(Path.GetTempPath(),"eff_transform_"+Guid.NewGuid().ToString("N")+".tpl");
        try
        {
            ExportTpl(source,index,temporary);
            using Bitmap bitmap=DecodePng(source,index);bitmap.RotateFlip(transform);
            new TextureWorkspaceService().ReplaceFromBitmap(temporary,0,bitmap,false);
            var changed=new TplReader().ReadTexture(temporary,0);
            byte[] encodedHeader=changed.header;changed.header=(byte[])target.header.Clone();
            BitConverter.GetBytes(changed.width).CopyTo(changed.header,0);BitConverter.GetBytes(changed.height).CopyTo(changed.header,2);
            // Preserve effect priority/scale while updating GS sampling configuration for rotated dimensions.
            Array.Copy(encodedHeader,0x29,changed.header,0x29,6);
            byte[] output=EffTextureArchive.Replace(source,index,changed);
            return output;
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }

    private static TPLDefinition.TPL At(IReadOnlyList<TPLDefinition.TPL> textures, int index) =>
        (uint)index < (uint)textures.Count ? textures[index] : throw new ArgumentOutOfRangeException(nameof(index), "Textura EFF não encontrada.");
}
