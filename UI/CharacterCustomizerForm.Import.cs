using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private void ViewportAssetDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(MeshLibraryItem)) is MeshLibraryItem mesh) { ViewportMeshDragDrop(e, mesh); return; }
        ViewportTextureDragDrop(sender, e);
    }

    private void ViewportTextureDragDrop(object? sender, DragEventArgs e)
    {
        if (datPath == null || e.Data?.GetData(typeof(TextureItem)) is not TextureItem texture) return;
        Point client = viewport.PointToClient(new Point(e.X, e.Y));
        EnemyModelPart? part = viewport.PickEnemyModelPartAt(client);
        if (part == null) { status.Text = "Solte a textura diretamente sobre uma parte visível do modelo."; return; }
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, part.DatEntryIndex);
            ManualAssignment? previousAssignment = manualAssignments.GetValueOrDefault(part.BinIndex);
            int changed = 0;
            foreach (int oldIndex in part.DiffuseMaps.Distinct())
            {
                if (oldIndex == texture.Index) continue;
                changed += Ps2CharacterDatEditor.ReplaceBinTextureIndex(datPath, part.DatEntryIndex, oldIndex, texture.Index);
            }
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, part.DatEntryIndex);
            var newAssignment = new ManualAssignment(texture.TplEntry, texture.Index);
            SetManualAssignment(part.BinIndex, newAssignment); SaveManualAssignments(datPath);
            PushTextureEdit(new TextureEdit(datPath, part.DatEntryIndex, texture.Index, before, after,
                $"BIN {part.BinIndex:D2} → TPL #{texture.TplEntry:D3} / textura {texture.Index}", true, texture.TplEntry,
                part.BinIndex, previousAssignment, newAssignment));
            int binIndex = part.BinIndex;
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex); SelectTexture(texture.TplEntry, texture.Index);
            status.Text = $"BIN {binIndex:D2} atribuído ao TPL #{texture.TplEntry:D3}, textura {texture.Index}. {changed} material(is) atualizado(s).";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Atribuir textura à malha", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ViewportMeshDragDrop(DragEventArgs e, MeshLibraryItem donor)
    {
        if (datPath == null || model == null) return;
        Point client = viewport.PointToClient(new Point(e.X, e.Y));
        EnemyModelPart? target = viewport.PickEnemyModelPartAt(client);
        if (target == null) { status.Text = "Solte o BIN diretamente sobre a malha que deseja substituir."; return; }
        ReplaceWithDonor(target, donor);
    }

    private void ReplaceSelectedMeshFromLibrary()
    {
        if (selectedPart == null || lstMeshLibrary.SelectedItems.Count == 0 || lstMeshLibrary.SelectedItems[0].Tag is not MeshLibraryItem donor)
        {
            MessageBox.Show(this, "Selecione um BIN de destino e um mesh da biblioteca.", "Substituir BIN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ReplaceWithDonor(selectedPart, donor);
    }

    private void ReplaceWithDonor(EnemyModelPart target, MeshLibraryItem donor)
    {
        if (datPath == null) return;
        DialogResult choice = MessageBox.Show(this,
            $"Substituir BIN {target.BinIndex:D2} de {Path.GetFileName(datPath)} pelo BIN {donor.Part.BinIndex:D2} de {Path.GetFileName(donor.DatPath)}?\n\n" +
            "SIM: encaixar o mesh e copiar suas texturas originais.\nNÃO: encaixar somente o mesh.\nCANCELAR: não alterar.\n\n" +
            $"Modo de encaixe: {cmbRigMode.SelectedItem ?? "Automático"}.",
            "Encaixar mesh", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (choice == DialogResult.Cancel) return;
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, target.DatEntryIndex);
            Ps2CharacterDatEditor.ReplaceBinEntry(datPath, target.DatEntryIndex, donor.DatPath, donor.Part.DatEntryIndex, SelectedRigMode());
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, target.DatEntryIndex);
            PushTextureEdit(new TextureEdit(datPath, target.DatEntryIndex, -1, before, after,
                $"Mesh BIN {target.BinIndex:D2} ← {Path.GetFileName(donor.DatPath)} / BIN {donor.Part.BinIndex:D2}", true));
            int copiedTextures = 0;
            int targetTpl = manualAssignments.TryGetValue(target.BinIndex, out ManualAssignment? targetAssignment) ? targetAssignment.TplEntry : target.TplEntryIndex;
            if (choice == DialogResult.Yes && donor.Part.TplEntryIndex >= 0 && targetTpl >= 0)
            {
                foreach (int textureIndex in donor.Part.DiffuseMaps.Where(x => x >= 0).Distinct())
                {
                    byte[] textureBefore = Ps2CharacterDatEditor.CaptureTplEntry(datPath, targetTpl);
                    Ps2CharacterDatEditor.CopyTextureFromDat(datPath, targetTpl, textureIndex, donor.DatPath, donor.Part.TplEntryIndex, textureIndex);
                    byte[] textureAfter = Ps2CharacterDatEditor.CaptureTplEntry(datPath, targetTpl);
                    PushTextureEdit(new TextureEdit(datPath, targetTpl, textureIndex, textureBefore, textureAfter,
                        $"Textura do mesh: {Path.GetFileName(donor.DatPath)} / TPL #{donor.Part.TplEntryIndex:D3} / {textureIndex}"));
                    copiedTextures++;
                }
            }
            int targetBin = target.BinIndex;
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(targetBin);
            status.Text = $"Mesh encaixado no BIN {targetBin:D2}. {copiedTextures} textura(s) original(is) copiada(s); rig do destino preservado.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Encaixar mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private Ps2CharacterDatEditor.SkeletonAdaptMode SelectedRigMode() => cmbRigMode.SelectedIndex switch
    {
        1 => Ps2CharacterDatEditor.SkeletonAdaptMode.Rigid,
        2 => Ps2CharacterDatEditor.SkeletonAdaptMode.Distributed,
        3 => Ps2CharacterDatEditor.SkeletonAdaptMode.KeepDonor,
        _ => Ps2CharacterDatEditor.SkeletonAdaptMode.Automatic
    };

    private async Task ImportExternalModelAsync()
    {
        if (datPath == null || selectedPart == null)
        { MessageBox.Show(this, "Selecione primeiro o BIN que receberá o modelo externo, na lista ou no viewer.", "Importar modelo", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        using var modelDialog = new OpenFileDialog { Filter = "Modelos compatíveis (*.obj;*.smd)|*.obj;*.smd|Wavefront OBJ (*.obj)|*.obj|StudioModelData (*.smd)|*.smd", Title = "Importar modelo externo" };
        if (modelDialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        if(File.Exists(modelDialog.FileName+".character.json")){await ReimportCharacterPartsSmdAsync(new[]{modelDialog.FileName});return;}
        try
        {
            ExternalModelStats stats = Ps2ModelConversionService.Analyze(modelDialog.FileName);
            string sizeWarning = stats.IsLarge
                ? $"\n\n⚠ MODELO GRANDE: o limite recomendado é {ExternalModelStats.SafeVertexLimit:N0} vértices e {ExternalModelStats.SafeFaceLimit:N0} faces. Modelos acima disso podem falhar na conversão, exceder segmentos do PS2 ou causar travamentos no jogo."
                : "\n\nO modelo está dentro do tamanho recomendado para personagens do PS2.";
            DialogResult confirmation = MessageBox.Show(this,
                $"Arquivo: {Path.GetFileName(modelDialog.FileName)}\nVértices: {stats.Vertices:N0}\nFaces trianguladas: {stats.Faces:N0}\nDestino: BIN {selectedPart.BinIndex:D2}\nEncaixe: {cmbRigMode.SelectedItem}" + sizeWarning +
                "\n\nA geometria será convertida para BIN e vinculada ao esqueleto do receptor. Texturas PNG devem ser importadas separadamente na aba Texturas. Deseja continuar?",
                stats.IsLarge ? "Confirmar modelo grande" : "Importar modelo externo", MessageBoxButtons.YesNo,
                stats.IsLarge ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
            if (confirmation != DialogResult.Yes) return;
            

            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            byte[] receiverTemplate = before;
            if (File.Exists(datPath + ".bak")) try { receiverTemplate = Ps2CharacterDatEditor.CaptureDatEntry(datPath + ".bak", entry); } catch { }
            Cursor = Cursors.WaitCursor; status.Text = $"Convertendo {Path.GetFileName(modelDialog.FileName)} para BIN PS2...";
            bool autoWeightedObj = Path.GetExtension(modelDialog.FileName).Equals(".obj", StringComparison.OrdinalIgnoreCase);
            string generated = await Ps2ModelConversionService.ConvertAsync(modelDialog.FileName, receiverTemplate, autoWeightedObj);
            (bool autoFitted, float fittedScale) = Ps2CharacterDatEditor.AutoFitBinFileToTemplate(generated, receiverTemplate);
            Ps2CharacterDatEditor.SkeletonAdaptMode importMode = autoWeightedObj
                ? SelectedRigMode()
                : SelectedRigMode();
            try
            {
                Ps2CharacterDatEditor.ReplaceBinEntryFromFile(datPath, entry, generated, importMode);
            }
            finally { try { File.Delete(generated); } catch { } }
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Modelo externo {Path.GetFileName(modelDialog.FileName)} → BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"Modelo externo importado • {stats.Vertices:N0} vértices • {stats.Faces:N0} faces • BIN {binIndex:D2}" + (autoFitted ? $" • encaixe automático ×{fittedScale:0.##}" : "") + (autoWeightedObj ? " • pesos automáticos." : (importMode == Ps2CharacterDatEditor.SkeletonAdaptMode.Rigid ? " • vínculo rígido." : "."));
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Importar modelo externo", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }



}
