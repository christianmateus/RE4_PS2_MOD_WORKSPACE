using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Text.Json;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm
{
    private sealed record PartAnnotation(string Name);
    private sealed record VisibilityPreset(string Name, int[] VisibleBins);

    private sealed class CharacterViewState
    {
        public Dictionary<int, PartAnnotation> Parts { get; set; } = new();
        public Dictionary<string, string> TextureNames { get; set; } = new();
        public List<VisibilityPreset> Presets { get; set; } = new();
        public string? ActivePreset { get; set; }
        public int[]? VisibleBins { get; set; }
    }

    private void LoadViewState(string path)
    {
        partAnnotations.Clear();
        textureNames.Clear();
        visibilityPresets.Clear();
        activePresetName = null;
        savedVisibleBins = null;
        string sidecar = characterWorkspace.MetadataPath(path) + ".character-view.json";
        if (File.Exists(sidecar))
        {
            try
            {
                CharacterViewState? saved = JsonSerializer.Deserialize<CharacterViewState>(File.ReadAllText(sidecar));
                if (saved != null)
                {
                    foreach (var pair in saved.Parts ?? new()) partAnnotations[pair.Key] = pair.Value;
                    foreach (var pair in saved.TextureNames ?? new()) textureNames[pair.Key] = pair.Value;
                    visibilityPresets.AddRange(saved.Presets ?? new());
                    activePresetName = saved.ActivePreset;
                    if (saved.VisibleBins != null) savedVisibleBins = saved.VisibleBins.ToHashSet();
                }
            }
            catch (Exception ex)
            {
                status.Text = "Nomes e presets não puderam ser lidos: " + ex.Message;
            }
        }
        RefreshPresetList();
    }

    private void SaveViewState()
    {
        if (datPath == null) return;
        var state = new CharacterViewState
        {
            Parts = new Dictionary<int, PartAnnotation>(partAnnotations),
            TextureNames = new Dictionary<string, string>(textureNames),
            Presets = new List<VisibilityPreset>(visibilityPresets),
            ActivePreset = activePresetName,
            VisibleBins = visibleBins.OrderBy(x => x).ToArray()
        };
        string path = characterWorkspace.MetadataPath(datPath) + ".character-view.json";
        string temp = path + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Salvar visualização", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveSelectedPartAnnotation()
    {
        if (syncingPartDetails || selectedPart == null || datPath == null) return;
        string name = txtPartName.Text.Trim();
        if (name.Length == 0) partAnnotations.Remove(selectedPart.BinIndex);
        else partAnnotations[selectedPart.BinIndex] = new PartAnnotation(name);
        SaveViewState();
        if (cmbMeshDonor.SelectedItem is DatItem donor && string.Equals(donor.Path, datPath == null ? null : characterWorkspace.Source(datPath), StringComparison.OrdinalIgnoreCase))
            foreach (ListViewItem row in lstMeshLibrary.Items)
                if (row.Tag is MeshLibraryItem item && item.Part.BinIndex == selectedPart.BinIndex)
                {
                    MeshLibraryItem updated = item with { Name = name };
                    row.Tag = updated;
                    row.Text = updated.ToString();
                }
        RefreshPartList(selectedPart.BinIndex);
    }

    private static Dictionary<int, string> ReadBinNames(string datFile)
    {
        string sidecar = datFile + ".character-view.json";
        if (!File.Exists(sidecar)) return new();
        try
        {
            CharacterViewState? state = JsonSerializer.Deserialize<CharacterViewState>(File.ReadAllText(sidecar));
            return state?.Parts?.ToDictionary(x => x.Key, x => x.Value.Name) ?? new();
        }
        catch { return new(); }
    }

    private void SaveSelectedTextureName()
    {
        if (syncingTextureName || editingTexture == null || datPath == null) return;
        var key = editingTexture.Value;
        string storageKey = $"{key.TplEntry}:{key.Index}";
        string name = txtTextureName.Text.Trim();
        string? old = textureNames.GetValueOrDefault(storageKey);
        if (string.Equals(old ?? "", name, StringComparison.Ordinal)) return;
        if (name.Length == 0) textureNames.Remove(storageKey);
        else textureNames[storageKey] = name;
        foreach (ListViewItem row in lstTextures.Items)
            if (row.Tag is TextureItem item && item.TplEntry == key.TplEntry && item.Index == key.Index)
            {
                TextureItem updated = item with { Name = name };
                row.Tag = updated;
                row.Text = updated.ToString();
                break;
            }
        SaveViewState();
    }

    private void UpdatePartDetails(EnemyModelPart? part)
    {
        syncingPartDetails = true;
        try
        {
            txtPartName.Enabled = part != null;
            if (part == null)
            {
                txtPartName.Clear(); lblPartDetails.Text = "Selecione um BIN para ver seus detalhes.";
                return;
            }
            partAnnotations.TryGetValue(part.BinIndex, out PartAnnotation? annotation);
            txtPartName.Text = annotation?.Name ?? "";
            int tpl = manualAssignments.GetValueOrDefault(part.BinIndex)?.TplEntry ?? part.TplEntryIndex;
            string maps = manualAssignments.TryGetValue(part.BinIndex, out ManualAssignment? assignment)
                ? assignment.TextureIndex.ToString() : string.Join(", ", part.DiffuseMaps);
            int vertices = part.Triangles.SelectMany(t => new[] { t.SourceVertexA, t.SourceVertexB, t.SourceVertexC })
                .Where(x => x >= 0).Distinct().Count();
            lblPartDetails.Text = $"DAT #{part.DatEntryIndex:D3}  •  TPL {(tpl < 0 ? "--" : $"#{tpl:D3}")}  •  mapas [{maps}]\n" +
                                  $"{vertices:N0} vértices  •  {part.Triangles.Count:N0} faces  •  {part.Size.X:0.##} × {part.Size.Y:0.##} × {part.Size.Z:0.##}";
        }
        finally { syncingPartDetails = false; }
    }

    private void RefreshPresetList()
    {
        syncingPreset = true;
        cmbViewPreset.Items.Clear();
        cmbViewPreset.Items.Add("Visualização livre");
        foreach (VisibilityPreset preset in visibilityPresets.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
            cmbViewPreset.Items.Add(preset.Name);
        int index = activePresetName == null ? -1 : cmbViewPreset.Items.IndexOf(activePresetName);
        cmbViewPreset.SelectedIndex = index >= 0 ? index : 0;
        syncingPreset = false;
    }

    private void ApplySelectedPreset()
    {
        if (syncingPreset || model == null) return;
        string? name = cmbViewPreset.SelectedIndex > 0 ? cmbViewPreset.SelectedItem as string : null;
        activePresetName = name;
        VisibilityPreset? preset = visibilityPresets.FirstOrDefault(x => x.Name == name);
        if (preset != null) SetVisibleBins(preset.VisibleBins);
        SaveViewState();
    }

    private void CreatePreset()
    {
        if (model == null) return;
        string? name = PromptForPresetName("Novo preset", "Nome da visualização:", "");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (visibilityPresets.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { MessageBox.Show(this, "Já existe um preset com esse nome."); return; }
        visibilityPresets.Add(new VisibilityPreset(name, visibleBins.OrderBy(x => x).ToArray()));
        activePresetName = name;
        SaveViewState(); RefreshPresetList();
    }

    private void RenamePreset()
    {
        if (activePresetName == null) return;
        string? name = PromptForPresetName("Renomear preset", "Novo nome:", activePresetName);
        if (string.IsNullOrWhiteSpace(name)) return;
        if (visibilityPresets.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.Name != activePresetName))
        { MessageBox.Show(this, "Já existe um preset com esse nome."); return; }
        int index = visibilityPresets.FindIndex(x => x.Name == activePresetName);
        if (index < 0) return;
        visibilityPresets[index] = visibilityPresets[index] with { Name = name };
        activePresetName = name;
        SaveViewState(); RefreshPresetList();
    }

    private void DuplicatePreset()
    {
        VisibilityPreset? source = visibilityPresets.FirstOrDefault(x => x.Name == activePresetName);
        if (source == null) { CreatePreset(); return; }
        string? name = PromptForPresetName("Duplicar preset", "Nome da cópia:", source.Name + " — cópia");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (visibilityPresets.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { MessageBox.Show(this, "Já existe um preset com esse nome."); return; }
        visibilityPresets.Add(new VisibilityPreset(name, (int[])source.VisibleBins.Clone()));
        activePresetName = name;
        SaveViewState(); RefreshPresetList();
    }

    private void DeletePreset()
    {
        if (activePresetName == null) return;
        if (MessageBox.Show(this, $"Excluir o preset '{activePresetName}'?", "Excluir preset", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        visibilityPresets.RemoveAll(x => x.Name == activePresetName);
        activePresetName = null;
        SaveViewState(); RefreshPresetList();
    }

    private void SaveActivePreset()
    {
        if (activePresetName == null) { CreatePreset(); return; }
        int index = visibilityPresets.FindIndex(x => x.Name == activePresetName);
        if (index < 0) return;
        visibilityPresets[index] = visibilityPresets[index] with { VisibleBins = visibleBins.OrderBy(x => x).ToArray() };
        SaveViewState();
        status.Text = $"Preset '{activePresetName}' atualizado.";
    }

    private static string? PromptForPresetName(string title, string label, string current)
    {
        using var dialog = new Form { Text = title, Width = 360, Height = 145, FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false };
        var prompt = new Label { Text = label, Dock = DockStyle.Top, Height = 28, Padding = new Padding(10, 8, 0, 0) };
        var input = new TextBox { Dock = DockStyle.Top, Text = current, Margin = new Padding(10) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(new Button { Text = "Salvar", DialogResult = DialogResult.OK });
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel });
        dialog.Controls.Add(input); dialog.Controls.Add(prompt); dialog.Controls.Add(buttons);
        dialog.AcceptButton = (Button)buttons.Controls[0]; dialog.CancelButton = (Button)buttons.Controls[1];
        return dialog.ShowDialog() == DialogResult.OK ? input.Text.Trim() : null;
    }
}


