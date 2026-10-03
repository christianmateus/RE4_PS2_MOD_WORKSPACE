using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private async void AlterarModeloEtmSelecionado()
    {
        if (tabVisualEntities?.SelectedIndex != 2 || lstVisualObjectEntries.SelectedItems.Count != 1 ||
            lstVisualObjectEntries.SelectedItem is not EtsEntry entry || visualEtmCatalog == null ||
            string.IsNullOrWhiteSpace(visualEtmPath) || !visualEtmCatalog.Objects.TryGetValue(entry.ObjectId, out EtmObjectDefinition? definition)) return;

        int instances = visualEtsScene?.Entries.Count(item => item.ObjectId == entry.ObjectId) ?? 0;
        using var dialog = new EtmModelReplacementDialog(definition, instances);
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK || dialog.Replacements.Count == 0) return;

        string source = visualEtmPath;
        string staged = source + ".workspace_import_tmp";
        try
        {
            UseWaitCursor = true;
            Ps2AssetPackageEditor.Save(source, staged, dialog.Replacements);
            EtmCatalog verified = Ps2EtmReader.Read(staged);
            if (verified.Resources.Count != visualEtmCatalog.Resources.Count ||
                !verified.ModelParts.TryGetValue(entry.ObjectId, out IReadOnlyList<EtmModelPart>? parts) || parts.Count == 0)
                throw new InvalidDataException("O ETM resultante não contém um modelo renderizável para o objeto selecionado.");

            string backup = GetVisualAevBackupPath(source);
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            if (!File.Exists(backup)) File.Copy(source, backup);
            File.Move(staged, source, true);
            visualEtmCatalog = Ps2EtmReader.Read(source);
            visualViewport.SetEtsScene(visualEtsScene, visualEtmCatalog);
            EtsEntry[] selection = lstVisualObjectEntries.SelectedItems.Cast<EtsEntry>().ToArray();
            visualViewport.SelectEtsEntries(selection, entry);
            UpdateEtsTextureDebugger(entry);
            lblVisualPropertiesTitle.Text = GetEtmObjectTitle(entry);
            lblVisualStatus.Text = $"Modelo ETM atualizado • {definition.DisplayName} • {dialog.Replacements.Count} recurso(s)";
            ExtractLog($"Visual Editor: {Path.GetFileName(source)} atualizado para {definition.DisplayName} (0x{entry.ObjectId:X2}) • {dialog.Replacements.Count} recurso(s) • backup preservado.");
            await RefreshChangeStatusAsync();
            _ = RefreshTrackedDatsAsync();
            await UpdateVisualModifiedStateAsync();
        }
        catch (Exception ex)
        {
            ExtractLog("Visual Editor: falha ao alterar modelo ETM: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Alterar modelo ETM", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try { if (File.Exists(staged)) File.Delete(staged); } catch { }
            UseWaitCursor = false;
        }
    }
}
