using System.Drawing.Imaging;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class AssetModelEditorForm
{
    private Button? exportTplButton, exportAllPngButton, exportAllTplButton;
    private Button? replacePngButton, rotateTextureButton, flipXTextureButton, flipYTextureButton;
    private bool refreshingTextureIndices;

    private static bool IsTpl(EtmResource resource) => resource.Name.EndsWith(".tpl", StringComparison.OrdinalIgnoreCase);
    private static bool IsEff(EtmResource resource) => resource.Name.EndsWith(".eff", StringComparison.OrdinalIgnoreCase);
    private bool HasEffTextures(EtmResource resource)
    {
        try { return EffEmbeddedTextureExporter.Read(Data(resource)).Count > 0; }
        catch { return false; }
    }

    private void SelectPreferredTextureResource(int previousFileOrder)
    {
        RefreshTextureSources(previousFileOrder);
    }

    private IReadOnlyList<TPLDefinition.TPL> ReadSelectedTextures(EtmResource resource)
    {
        byte[] data = Data(resource);
        if (IsEff(resource)) return EffEmbeddedTextureExporter.Read(data);
        if (!IsTpl(resource)) return Array.Empty<TPLDefinition.TPL>();
        using var stream = new MemoryStream(data, false);
        using var reader = new BinaryReader(stream);
        if (stream.Length < 8) throw new InvalidDataException("TPL truncado.");
        stream.Position = 4;
        uint count = reader.ReadUInt32();
        if (count == 0 || count > 4096) throw new InvalidDataException("TPL sem texturas válidas.");
        var textures = new List<TPLDefinition.TPL>(checked((int)count));
        for (int index = 0; index < count; index++) textures.Add(new TplReader().ReadTexture(reader, index));
        return textures;
    }

    private Bitmap DecodeSelectedTexture(EtmResource resource, int index)
    {
        byte[] data = Data(resource);
        if (IsEff(resource)) return EffEmbeddedTextureExporter.DecodePng(data, index);
        using var stream = new MemoryStream(data, false);
        using var reader = new BinaryReader(stream);
        var info = new TplReader().ReadTexture(reader, index);
        return new TextureDecoder().Decode(info, reader);
    }

    private void PreviewSelectedTexture(bool rebuildIndices)
    {
        if (refreshingTextureIndices) return;
        Image? previous = texture.Image;
        texture.Image = null;
        previous?.Dispose();
        EtmResource? resource = TextureResource();
        try
        {
            if (rebuildIndices)
            {
                refreshingTextureIndices = true;
                try
                {
                    textureIndex.Items.Clear();
                    if (resource != null && (IsEff(resource) || IsTpl(resource)))
                    {
                        IReadOnlyList<TPLDefinition.TPL> found = ReadSelectedTextures(resource);
                        for (int index = 0; index < found.Count; index++)
                            textureIndex.Items.Add(new TextureIndexItem(index, $"#{index:D2} • {found[index].width}×{found[index].height}"));
                        if (textureIndex.Items.Count > 0) textureIndex.SelectedIndex = 0;
                    }
                }
                finally { refreshingTextureIndices = false; }
            }
            if (resource != null && textureIndex.SelectedIndex >= 0 && (IsEff(resource) || IsTpl(resource)))
                texture.Image = DecodeSelectedTexture(resource, CurrentTextureIndex);
        }
        catch (Exception ex) { status.Text = $"Textura de {resource?.Name}: {ex.Message}"; }
        textureEmpty.Visible=texture.Image==null;
        textureDetails.Text=resource==null?"Nenhuma textura associada":$"{resource.Name} • {textureIndex.Items.Count} textura(s)";
        UpdateTextureActions();
    }

    private void UpdateTextureActions()
    {
        EtmResource? resource = TextureResource();
        bool hasTexture = resource != null && textureIndex.SelectedIndex >= 0;
        bool editable = hasTexture && (IsTpl(resource!) || IsEff(resource!));
        if (replacePngButton != null) replacePngButton.Enabled = hasTexture;
        if (exportTplButton != null) exportTplButton.Enabled = hasTexture;
        if (exportAllPngButton != null) exportAllPngButton.Enabled = hasTexture;
        if (exportAllTplButton != null) exportAllTplButton.Enabled = hasTexture;
        foreach (Button? button in new[] { rotateTextureButton, flipXTextureButton, flipYTextureButton })
            if (button != null) button.Enabled = editable;
    }

    private void ExportSelectedPng()
    {
        EtmResource? resource = TextureResource();
        if (resource == null || textureIndex.SelectedIndex < 0) return;
        using var dialog = new SaveFileDialog { FileName = $"{Path.GetFileNameWithoutExtension(resource.Name)}_{CurrentTextureIndex:D2}.png", Filter = "Imagem PNG (*.png)|*.png" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            using Bitmap image = DecodeSelectedTexture(resource, CurrentTextureIndex);
            image.Save(dialog.FileName, ImageFormat.Png);
            status.Text = $"PNG exportado: {dialog.FileName}";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportTextureTpl()
    {
        EtmResource? resource = TextureResource();
        if (resource == null || textureIndex.SelectedIndex < 0) return;
        using var dialog = new SaveFileDialog { FileName = $"{Path.GetFileNameWithoutExtension(resource.Name)}_{CurrentTextureIndex:D2}.tpl", Filter = "Pacote TPL (*.tpl)|*.tpl" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            ExportSingleTpl(resource, CurrentTextureIndex, dialog.FileName);
            status.Text = $"TPL exportado: {dialog.FileName}";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar TPL", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportSingleTpl(EtmResource resource, int index, string outputPath)
    {
        if (IsEff(resource)) EffEmbeddedTextureExporter.ExportTpl(Data(resource), index, outputPath);
        else
        {
            TPLDefinition.TPL model = ReadSelectedTextures(resource)[index];
            new TplWriter(new TplReader()).RebuildFile(outputPath, new[] { model });
            _ = new TplReader().ReadTexture(outputPath, 0);
        }
    }

    private void ExportAllTextures(bool tpl)
    {
        EtmResource? resource = TextureResource();
        if (resource == null || textureIndex.Items.Count == 0) return;
        using var dialog = new FolderBrowserDialog { Description = "Escolha a pasta para as texturas extraídas", UseDescriptionForTitle = true };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        string stem = Path.GetFileNameWithoutExtension(resource.Name);
        string extension = tpl ? ".tpl" : ".png";
        string[] targets = Enumerable.Range(0, textureIndex.Items.Count).Select(index => Path.Combine(dialog.SelectedPath, $"{stem}_{index:D2}{extension}")).ToArray();
        if (targets.Any(File.Exists) && MessageBox.Show(this, "Alguns arquivos já existem na pasta. Sobrescrever?", "Exportar texturas", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            for (int index = 0; index < targets.Length; index++)
            {
                if (tpl) ExportSingleTpl(resource, index, targets[index]);
                else { using Bitmap image = DecodeSelectedTexture(resource, index); image.Save(targets[index], ImageFormat.Png); }
            }
            status.Text = $"{targets.Length} textura(s) exportada(s) para {dialog.SelectedPath}";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar texturas", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
