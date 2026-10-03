using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private void PopulateParts()
    {
        selectedPart = null; cmbCurrentMap.Items.Clear(); cmbNewMap.Items.Clear(); lstBones.Items.Clear();
        lblBoneWarning.Text = "Selecione uma parte para analisar.";
        viewport.SetEnemyBoneDiagnostic(chkSkeleton.Checked, null);
        lblMaterialMap.Text = "MATERIAL • selecione uma parte";
        RefreshPartList();
    }

    private void RefreshPartList(int? selectBin = null)
    {
        int? target = selectBin ?? selectedPart?.BinIndex;
        syncingParts = true;
        lstParts.BeginUpdate();
        try
        {
            lstParts.Items.Clear();
            if (model != null)
            {
                string query = txtPartSearch.Text.Trim();
                foreach (EnemyModelPart part in model.Parts.OrderBy(x => x.BinIndex))
                {
                    string name = partAnnotations.GetValueOrDefault(part.BinIndex)?.Name ?? "";
                    string searchable = $"BIN {part.BinIndex:D2} {name} DAT {part.DatEntryIndex:D3} TPL {part.TplEntryIndex:D3}";
                    if (query.Length > 0 && !searchable.Contains(query, StringComparison.CurrentCultureIgnoreCase)) continue;
                    bool visible = visibleBins.Contains(part.BinIndex);
                    bool include = cmbPartFilter.SelectedIndex switch
                    {
                        1 => visible,
                        2 => !visible,
                        3 => part.Triangles.Count == 0,
                        4 => partAnnotations.ContainsKey(part.BinIndex),
                        _ => true
                    };
                    if (!include) continue;
                    int index = lstParts.Items.Add(new PartItem(part, name));
                    lstParts.SetItemChecked(index, visible);
                    if (part.BinIndex == target) lstParts.SelectedIndex = index;
                }
            }
        }
        finally { lstParts.EndUpdate(); syncingParts = false; }
        if (lstParts.SelectedItem is not PartItem)
        {
            selectedPart = target.HasValue ? model?.Parts.FirstOrDefault(x => x.BinIndex == target.Value) : null;
            UpdatePartDetails(selectedPart);
        }
    }

    private void PopulateTextures()
    {
        lstTextures.Items.Clear(); EnemyModelScene? catalogModel = showingOriginal ? originalModel : model; if (catalogModel == null) return;
        var reader = new TplReader();
        foreach (EnemyTexturePackage package in catalogModel.TexturePackages.Values.OrderBy(x => x.DatEntryIndex))
        {
            try
            {
                using var stream = new MemoryStream(package.Data, false); using var br = new BinaryReader(stream);
                stream.Position = 4; int count = checked((int)br.ReadUInt32());
                for (int i = 0; i < count; i++)
                {
                    stream.Position = 0; var tpl = reader.ReadTexture(br, i);
                    string? name = textureNames.GetValueOrDefault($"{package.DatEntryIndex}:{i}");
                    var item = new TextureItem(package.DatEntryIndex, i, tpl.width, tpl.height, tpl.bitDepth == 8 ? "4-bit" : tpl.bitDepth == 9 ? "8-bit" : tpl.bitDepth == 6 ? "32-bit" : $"0x{tpl.bitDepth:X}", name);
                    var row = new ListViewItem(item.ToString()) { Tag = item };
                    row.SubItems.Add(item.Index.ToString("D2")); row.SubItems.Add($"{item.Width}×{item.Height}"); row.SubItems.Add(item.Format);
                    lstTextures.Items.Add(row);
                }
            }
            catch { }
        }
        if (lstTextures.Items.Count > 0) lstTextures.Items[0].Selected = true;
    }

    private void PartsItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (syncingParts || e.Index < 0 || lstParts.Items[e.Index] is not PartItem item) return;
        bool visible = e.NewValue == CheckState.Checked;
        if (visible) visibleBins.Add(item.Part.BinIndex); else visibleBins.Remove(item.Part.BinIndex);
        SaveViewState();
        if (activePresetName != null) status.Text = $"Preset '{activePresetName}' alterado. Clique em SALVAR para atualizar.";
        BeginInvoke(() =>
        {
            viewport.SetEnemyModelPartVisible(PreviewType, item.Part.BinIndex, visible);
            if (cmbPartFilter.SelectedIndex is 1 or 2) RefreshPartList(item.Part.BinIndex);
        });
    }

    private void ShowAllParts()
    {
        if (model == null) return;
        SetVisibleBins(model.Parts.Select(x => x.BinIndex));
        if (activePresetName != null) status.Text = $"Preset '{activePresetName}' alterado. Clique em SALVAR para atualizar.";
    }

    private void HideAllParts()
    {
        if (model == null) return;
        SetVisibleBins(Array.Empty<int>());
        if (activePresetName != null) status.Text = $"Preset '{activePresetName}' alterado. Clique em SALVAR para atualizar.";
    }

    private void SetVisibleBins(IEnumerable<int> bins)
    {
        visibleBins.Clear();
        foreach (int bin in bins) visibleBins.Add(bin);
        viewport.SetVisibleEnemyModelParts(PreviewType, visibleBins);
        RefreshPartList();
        SaveViewState();
    }

    private void SoloSelectedPart()
    {
        if (selectedPart == null) return;
        int bin = selectedPart.BinIndex;
        SetVisibleBins(new[] { bin });
        if (activePresetName != null) status.Text = $"Preset '{activePresetName}' alterado. Clique em SALVAR para atualizar.";
    }

    private void InvertPartVisibility()
    {
        if (model == null) return;
        SetVisibleBins(model.Parts.Select(x => x.BinIndex).Where(x => !visibleBins.Contains(x)).ToArray());
        if (activePresetName != null) status.Text = $"Preset '{activePresetName}' alterado. Clique em SALVAR para atualizar.";
    }

    private void ToggleSelectedPartVisibility()
    {
        if (selectedPart == null) return;
        int bin = selectedPart.BinIndex;
        HashSet<int> next = new(visibleBins);
        if (!next.Add(bin)) next.Remove(bin);
        SetVisibleBins(next);
        if (activePresetName != null) status.Text = $"Preset '{activePresetName}' alterado. Clique em SALVAR para atualizar.";
    }

    private void RemoveSelectedPartFromModel()
    {
        if (datPath == null || selectedPart == null || selectedPart.Triangles.Count == 0) return;
        int binIndex = selectedPart.BinIndex, entryIndex = selectedPart.DatEntryIndex;
        if (MessageBox.Show(this,
            $"Remover a geometria do BIN {binIndex:D2}?\n\nA entrada e o esqueleto serão preservados, mas este mesh não será desenhado no jogo.",
            "Remover mesh", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entryIndex);
            Ps2CharacterDatEditor.DisableBinGeometry(datPath, entryIndex);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entryIndex);
            PushTextureEdit(new TextureEdit(datPath, entryIndex, -1, before, after, $"Remover mesh BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} removido do modelo. Use Desfazer ou Restaurar BIN original para recuperá-lo.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Remover mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RestoreSelectedPartFromBackup()
    {
        if (datPath == null || selectedPart == null) return;
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) return;
        int binIndex = selectedPart.BinIndex, entryIndex = selectedPart.DatEntryIndex;
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entryIndex);
            byte[] original = Ps2CharacterDatEditor.CaptureDatEntry(backup, entryIndex);
            Ps2CharacterDatEditor.RestoreDatEntry(datPath, entryIndex, original);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entryIndex);
            PushTextureEdit(new TextureEdit(datPath, entryIndex, -1, before, after, $"Restaurar BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} restaurado a partir do DAT original.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restaurar BIN", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SelectPartFromViewport(EnemyModelPart? part)
    {
        lstParts.ClearSelected();
        viewport.HighlightEnemyModelPart(PreviewType, part?.BinIndex);
        if (part == null) { status.Text = model == null ? "Nenhum DAT carregado." : "Nenhuma parte selecionada."; return; }
        SelectPartByBinIndex(part.BinIndex);
        status.Text = $"Selecionado: BIN {part.BinIndex:D2} • entrada DAT #{part.DatEntryIndex:D3} • TPL {(part.TplEntryIndex < 0 ? "--" : $"#{part.TplEntryIndex:D3}")} • arraste o gizmo para mover o mesh";
        SelectTexturesForPart(part);
    }

    private void PopulateMaterialMapping(EnemyModelPart part)
    {
        cmbCurrentMap.Items.Clear(); cmbNewMap.Items.Clear();
        foreach (int map in part.DiffuseMaps.Distinct().OrderBy(x => x)) cmbCurrentMap.Items.Add(map);
        int textureCount = 0;
        int effectiveTpl = manualAssignments.TryGetValue(part.BinIndex, out ManualAssignment? assignment) ? assignment.TplEntry : part.TplEntryIndex;
        if (model != null && effectiveTpl >= 0 && model.TexturePackages.TryGetValue(effectiveTpl, out EnemyTexturePackage? package) && package.Data.Length >= 8)
            textureCount = checked((int)Math.Min(255u, BitConverter.ToUInt32(package.Data, 4)));
        cmbNewMap.Items.Add(-1);
        for (int i = 0; i < textureCount; i++) cmbNewMap.Items.Add(i);
        if (cmbCurrentMap.Items.Count > 0) cmbCurrentMap.SelectedIndex = 0;
        if (cmbNewMap.Items.Count > 1) cmbNewMap.SelectedIndex = 1; else if (cmbNewMap.Items.Count > 0) cmbNewMap.SelectedIndex = 0;
        lblMaterialMap.Text = $"MATERIAL • BIN {part.BinIndex:D2} • TPL #{effectiveTpl:D3} • {textureCount} textura(s)";
    }

    private void ApplyMaterialTextureIndex()
    {
        if (datPath == null || selectedPart == null || cmbCurrentMap.SelectedItem is not int oldIndex || cmbNewMap.SelectedItem is not int newIndex)
        { MessageBox.Show(this, "Selecione uma parte e os índices de origem e destino."); return; }
        if (oldIndex == newIndex) return;
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, selectedPart.DatEntryIndex);
            int changed = Ps2CharacterDatEditor.ReplaceBinTextureIndex(datPath, selectedPart.DatEntryIndex, oldIndex, newIndex);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, selectedPart.DatEntryIndex);
            int binIndex = selectedPart.BinIndex, datEntry = selectedPart.DatEntryIndex, tplEntry = selectedPart.TplEntryIndex;
            PushTextureEdit(new TextureEdit(datPath, datEntry, newIndex, before, after, $"BIN {binIndex:D2}: textura {oldIndex} → {newIndex}", true, tplEntry));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"{changed} material(is) do BIN {binIndex:D2} agora usam a textura {newIndex}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Alterar índice de textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadManualAssignments(string path)
    {
        manualAssignments.Clear();
        string sidecar = characterWorkspace.MetadataPath(path) + ".texture-map.json";
        if (!File.Exists(sidecar)) return;
        try
        {
            Dictionary<string, int[]>? stored = JsonSerializer.Deserialize<Dictionary<string, int[]>>(File.ReadAllText(sidecar));
            if (stored == null) return;
            foreach (var pair in stored)
                if (int.TryParse(pair.Key, out int binIndex) && pair.Value.Length >= 2)
                    manualAssignments[binIndex] = new ManualAssignment(pair.Value[0], pair.Value[1]);
        }
        catch { status.Text = "O mapa manual de texturas não pôde ser lido."; }
    }

    private void SaveManualAssignments(string path)
    {
        string sidecar = characterWorkspace.MetadataPath(path) + ".texture-map.json";
        var stored = manualAssignments.ToDictionary(x => x.Key.ToString(), x => new[] { x.Value.TplEntry, x.Value.TextureIndex });
        if (stored.Count == 0) { if (File.Exists(sidecar)) File.Delete(sidecar); return; }
        File.WriteAllText(sidecar, JsonSerializer.Serialize(stored, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void SetManualAssignment(int binIndex, ManualAssignment? assignment)
    {
        if (assignment == null) manualAssignments.Remove(binIndex); else manualAssignments[binIndex] = assignment;
    }

    private void SelectPartByBinIndex(int binIndex)
    {
        if (!lstParts.Items.Cast<PartItem>().Any(x => x.Part.BinIndex == binIndex))
        {
            txtPartSearch.Clear(); cmbPartFilter.SelectedIndex = 0; RefreshPartList(binIndex);
        }
        for (int i = 0; i < lstParts.Items.Count; i++) if (lstParts.Items[i] is PartItem item && item.Part.BinIndex == binIndex) { lstParts.SelectedIndex = i; lstParts.TopIndex = Math.Max(0, i - 2); return; }
    }

    private void SelectTexturesForPart(EnemyModelPart part)
    {
        foreach (ListViewItem row in lstTextures.Items) row.Selected = false;
        ListViewItem? first = null;
        ManualAssignment? assignment = manualAssignments.GetValueOrDefault(part.BinIndex);
        foreach (ListViewItem row in lstTextures.Items)
        {
            if (row.Tag is not TextureItem texture) continue;
            bool matches = assignment != null
                ? texture.TplEntry == assignment.TplEntry && texture.Index == assignment.TextureIndex
                : texture.TplEntry == part.TplEntryIndex && part.DiffuseMaps.Contains(texture.Index);
            if (!matches) continue;
            row.Selected = true; first ??= row;
        }
        first?.EnsureVisible();
    }

}

