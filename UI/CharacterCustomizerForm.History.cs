using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private void ToggleOriginal()
    {
        if (model == null || originalModel == null) return;
        showingOriginal = !showingOriginal;
        viewport.SetEnemyModels(new Dictionary<byte, EnemyModelScene> { [PreviewType] = showingOriginal ? originalModel : model });
        viewport.SetEnemyTextureAssignments(PreviewType, showingOriginal ? null : manualAssignments.ToDictionary(x => x.Key, x => (x.Value.TplEntry, x.Value.TextureIndex)));
        PopulateTextures();
        UpdateCompareButton();
        status.Text = showingOriginal ? "Visualizando o DAT original. As edições permanecem preservadas." : "Visualizando o DAT modificado.";
    }

    private void UpdateCompareButton()
    {
        btnCompare.Text = showingOriginal ? "VER MODIFICADA" : "VER ORIGINAL";
        btnCompare.BackColor = showingOriginal ? Color.FromArgb(145, 92, 35) : Color.FromArgb(42, 47, 56);
        UpdatePartDetails(selectedPart);
    }

    private void PushTextureEdit(TextureEdit edit)
    {
        characterWorkspace.SaveChanges(edit.DatPath);
        undoHistory.Push(edit); redoHistory.Clear(); UpdateHistoryButtons();
    }

    private void UndoTextureEdit()
    {
        if (undoHistory.Count == 0) return;
        TextureEdit edit = undoHistory.Pop();
        try
        {
            if (edit.IsBinMaterial) Ps2CharacterDatEditor.RestoreDatEntry(edit.DatPath, edit.TplEntry, edit.Before);
            else Ps2CharacterDatEditor.RestoreTplEntry(edit.DatPath, edit.TplEntry, edit.Before);
            characterWorkspace.SaveChanges(edit.DatPath);
            if (edit.BinIndex >= 0) { SetManualAssignment(edit.BinIndex, edit.BeforeAssignment); SaveManualAssignments(edit.DatPath); }
            redoHistory.Push(edit); ReloadHistoryEdit(edit); status.Text = $"Desfeito: {edit.Description}.";
        }
        catch (Exception ex) { undoHistory.Push(edit); MessageBox.Show(this, ex.Message, "Desfazer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        UpdateHistoryButtons();
    }

    private void RedoTextureEdit()
    {
        if (redoHistory.Count == 0) return;
        TextureEdit edit = redoHistory.Pop();
        try
        {
            if (edit.IsBinMaterial) Ps2CharacterDatEditor.RestoreDatEntry(edit.DatPath, edit.TplEntry, edit.After);
            else Ps2CharacterDatEditor.RestoreTplEntry(edit.DatPath, edit.TplEntry, edit.After);
            characterWorkspace.SaveChanges(edit.DatPath);
            if (edit.BinIndex >= 0) { SetManualAssignment(edit.BinIndex, edit.AfterAssignment); SaveManualAssignments(edit.DatPath); }
            undoHistory.Push(edit); ReloadHistoryEdit(edit); status.Text = $"Refeito: {edit.Description}.";
        }
        catch (Exception ex) { redoHistory.Push(edit); MessageBox.Show(this, ex.Message, "Refazer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        UpdateHistoryButtons();
    }

    private void RestoreSelectedTexture()
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.RestoreTextureFromBackup(datPath, item.TplEntry, item.Index);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, "Restaurar textura original"));
            ReloadModifiedTexture(item); status.Text = "A textura selecionada foi restaurada do backup original.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restaurar textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ReloadModifiedTexture(TextureItem item)
    {
        showingOriginal = false; LoadDat(datPath!); SelectTexture(item.TplEntry, item.Index);
    }

    private void ReloadHistoryEdit(TextureEdit edit)
    {
        showingOriginal = false; LoadDat(edit.DatPath);
        if (edit.IsBinMaterial)
        {
            EnemyModelPart? part = model?.Parts.FirstOrDefault(x => x.DatEntryIndex == edit.TplEntry);
            if (part != null) SelectPartByBinIndex(part.BinIndex);
        }
        else SelectTexture(edit.TplEntry, edit.TextureIndex);
    }

    private void UpdateHistoryButtons()
    {
        btnUndo.Enabled = undoHistory.Count > 0; btnRedo.Enabled = redoHistory.Count > 0;
        btnUndo.Text = undoHistory.Count > 0 ? $"DESFAZER ({undoHistory.Count})" : "DESFAZER";
        btnRedo.Text = redoHistory.Count > 0 ? $"REFAZER ({redoHistory.Count})" : "REFAZER";
    }

    private void SelectTexture(int tplEntry, int textureIndex)
    {
        // PopulateTextures selects the first row as a sensible default. When a
        // texture edit reloads the DAT, remove that temporary selection before
        // restoring the texture the user was actually editing.
        foreach (ListViewItem row in lstTextures.Items) row.Selected = false;
        foreach (ListViewItem row in lstTextures.Items)
            if (row.Tag is TextureItem item && item.TplEntry == tplEntry && item.Index == textureIndex)
            {
                row.Selected = true;
                row.Focused = true;
                row.EnsureVisible();
                ShowSelectedTexture();
                return;
            }
    }

}

