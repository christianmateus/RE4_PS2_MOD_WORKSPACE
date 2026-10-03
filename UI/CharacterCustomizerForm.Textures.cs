using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private void ShowSelectedTexture()
    {
        SaveSelectedTextureName();
        syncingTextureName = true;
        TextureItem? selected = lstTextures.SelectedItems.Count > 0 ? lstTextures.SelectedItems[0].Tag as TextureItem : null;
        editingTexture = selected == null ? null : (selected.TplEntry, selected.Index);
        txtTextureName.Enabled = selected != null;
        txtTextureName.Text = selected == null ? "" : textureNames.GetValueOrDefault($"{selected.TplEntry}:{selected.Index}") ?? "";
        syncingTextureName = false;
        preview.Image?.Dispose(); preview.Image = null;
        EnemyModelScene? displayModel = showingOriginal ? originalModel : model;
        if (displayModel == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item || !displayModel.TexturePackages.TryGetValue(item.TplEntry, out EnemyTexturePackage? package)) return;
        try
        {
            using var stream = new MemoryStream(package.Data, false); using var br = new BinaryReader(stream); var tpl = new TplReader().ReadTexture(br, item.Index); stream.Position = 0;
            preview.Image = new TextureDecoder().Decode(tpl, br);
        }
        catch { }
    }

    private void ImportPng()
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item) { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        using var dialog = new OpenFileDialog { Filter = "Imagem PNG (*.png)|*.png", Title = $"Substituir TPL #{item.TplEntry:D3}, textura {item.Index}" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.ReplaceTextureFromPng(datPath, item.TplEntry, item.Index, dialog.FileName);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, "Importar PNG"));
            ReloadModifiedTexture(item);
            status.Text = characterWorkspace.Source(datPath) != datPath ? "PNG aplicado ao TPL extraído. Backup preservado em CharacterBackups." : $"PNG aplicado. Backup preservado em {Path.GetFileName(datPath)}.bak";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Importar PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportSelectedTexture()
    {
        EnemyModelScene? displayModel = showingOriginal ? originalModel : model;
        if (displayModel == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        if (!displayModel.TexturePackages.TryGetValue(item.TplEntry, out EnemyTexturePackage? package))
        { MessageBox.Show(this, "O TPL da textura selecionada não está disponível."); return; }

        string datName = Path.GetFileNameWithoutExtension(datPath ?? "personagem");
        string version = showingOriginal ? "_original" : "";
        using var dialog = new SaveFileDialog
        {
            Filter = "Imagem PNG (*.png)|*.png",
            Title = $"Exportar {item}",
            FileName = $"{datName}_tpl{item.TplEntry:D3}_tex{item.Index:D2}{version}.png",
            AddExtension = true,
            DefaultExt = "png"
        };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;

        try
        {
            using var stream = new MemoryStream(package.Data, false);
            using var reader = new BinaryReader(stream);
            var tpl = new TplReader().ReadTexture(reader, item.Index);
            stream.Position = 0;
            using Bitmap bitmap = new TextureDecoder().Decode(tpl, reader);
            bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            status.Text = $"Textura exportada: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void TransformSelectedTexture(Ps2CharacterDatEditor.TextureTransform transform)
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        try
        {
            Cursor = Cursors.WaitCursor;
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.TransformTexture(datPath, item.TplEntry, item.Index, transform);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, transform.ToString()));
            ReloadModifiedTexture(item);
            status.Text = transform switch
            {
                Ps2CharacterDatEditor.TextureTransform.Rotate90 => "Textura rotacionada 90°.",
                Ps2CharacterDatEditor.TextureTransform.FlipX => "Textura invertida no eixo X.",
                _ => "Textura invertida no eixo Y."
            };
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Transformar textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

    private void ResizeSelectedTexture()
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        using var dialog = new TextureResizeDialog(item.Width, item.Height);
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK || (dialog.TargetWidth == item.Width && dialog.TargetHeight == item.Height)) return;
        try
        {
            Cursor = Cursors.WaitCursor;
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.ResizeTexture(datPath, item.TplEntry, item.Index, dialog.TargetWidth, dialog.TargetHeight, dialog.ResamplingMode);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, $"Resize {item.Width}×{item.Height} → {dialog.TargetWidth}×{dialog.TargetHeight}"));
            ReloadModifiedTexture(item);
            status.Text = $"Textura redimensionada para {dialog.TargetWidth}×{dialog.TargetHeight}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Redimensionar textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

    private void ConvertSelectedTextureBitDepth(int colors)
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        string target = colors == 16 ? "4-bit" : "8-bit";
        if (item.Format == target) return;
        try
        {
            Cursor = Cursors.WaitCursor;
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.ConvertTextureBitDepth(datPath, item.TplEntry, item.Index, colors);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, $"Profundidade de cor → {target}"));
            ReloadModifiedTexture(item);
            status.Text = $"Textura convertida para {target}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Profundidade de cor", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

}

