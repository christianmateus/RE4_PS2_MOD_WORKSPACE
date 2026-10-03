using RE4_PS2_MOD_WORKSPACE.Core.Afs;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private EslScene? selectedEnemyScene;
    private string? currentEnemyEslPath;
    private AfsEntry? currentEnemyAfsEntry;
    private bool syncingEnemyFriendlyUi;
    private bool syncingEnemyFileSelection;
    private bool enemySceneModified;
    private readonly Dictionary<byte, EnemyModelScene> visualEnemyModelCache = new();
    private readonly HashSet<byte> visualEnemyModelFailed = new();
    private readonly Dictionary<byte, int> visualEnemyModelReloadVersions = new();
    private readonly Dictionary<byte, (string Path, long Length, long WriteTicks)> visualEnemyDatSignatures = new();
    private readonly HashSet<byte> visualEnemyDatReloadPending = new();
    private readonly System.Windows.Forms.Timer visualEnemyDatWatchTimer = new() { Interval = 1000 };
    private bool visualEnemyDatWatcherInitialized;
    private bool loadingVisualEnemyModels;
    private bool visualEnemyModelsEnsurePending;


    private sealed record EnemyLocationFilter(byte? StageId, byte? RoomId, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed class EnemyChoice
    {
        public byte Id { get; }
        public string Label { get; }
        public EnemyChoice(byte id, string label) { Id = id; Label = label; }
        public override string ToString() => Label;
    }

    private List<EslEnemyEntry> GetSelectedEnemyEntries() => lstEnemyEntries?.SelectedItems.Cast<object>().OfType<EslEnemyEntry>().ToList() ?? new List<EslEnemyEntry>();

    private void RefreshEnemyEntriesPreserveSelection(IEnumerable<int>? preferredIndices = null)
    {
        HashSet<int> selectedIndices = preferredIndices != null ? preferredIndices.ToHashSet() : GetSelectedEnemyEntries().Select(x => x.Index).ToHashSet();
        lstEnemyEntries.BeginUpdate();
        lstEnemyEntries.Items.Clear();
        if (selectedEnemyScene != null)
            foreach (EslEnemyEntry entry in selectedEnemyScene.Entries.Where(EnemyEntryPassesManagerFilter)) lstEnemyEntries.Items.Add(entry);
        lstEnemyEntries.ClearSelected();
        for (int i = 0; i < lstEnemyEntries.Items.Count; i++)
            if (lstEnemyEntries.Items[i] is EslEnemyEntry e && selectedIndices.Contains(e.Index)) lstEnemyEntries.SetSelected(i, true);
        if (lstEnemyEntries.SelectedItems.Count == 0 && lstEnemyEntries.Items.Count > 0) lstEnemyEntries.SelectedIndex = 0;
        lstEnemyEntries.EndUpdate();
        UpdateEnemySelectionUi();
        UpdateEnemyEntryCount();
    }

    private void UpdateEnemySelectionUi()
    {
        List<EslEnemyEntry> entries = GetSelectedEnemyEntries();
        if (entries.Count == 0)
        {
            pgEnemyProperties.SelectedObject = null;
            SyncEnemyFriendlyEditors(null);
            visualViewport?.SelectEnemyEntry(null);
            return;
        }
        if (entries.Count == 1) pgEnemyProperties.SelectedObject = entries[0];
        else pgEnemyProperties.SelectedObjects = entries.Cast<object>().ToArray();
        SyncEnemyFriendlyEditorsForSelection(entries);
        visualViewport?.SelectEnemyEntry(entries[0]);
    }

    private void NotifyEnemyEntriesChanged(IEnumerable<int>? selectedIndices = null)
    {
        enemySceneModified = true;
        RefreshEnemyEntriesPreserveSelection(selectedIndices);
        PopulateEnemyLocationFilter();
        PopulateVisualEnemyLocationFilter(project.ActiveDatName);
        RefreshVisualEnemyEntryList(selectedIndices);
        if (visualViewport != null && !visualViewport.IsDisposed) visualViewport.RefreshEnemyGeometry(GetSelectedEnemyEntries().FirstOrDefault());
        UpdateVisualStatus();
    }

    private async void btnNavEnemies_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving();
        ShowPage(pnlEnemies, btnNavEnemies, "Inimigos");
        RememberMainPage("Enemies");
        lblEnemyStatus.Text = "Lendo ESLs do AFS...";
        await EnsureEnemyAfsAndRefreshAsync();
    }

    private async Task EnsureEnemyAfsAndRefreshAsync()
    {
        if (!RequireWorkspace()) { lblEnemyStatus.Text = "Workspace necessário."; return; }
        if (loadedAfs == null)
        {
            if (string.IsNullOrWhiteSpace(project.IsoPath) || !File.Exists(project.IsoPath))
            {
                lblEnemyStatus.Text = "Selecione uma ISO no módulo Projeto.";
                cmbEnemyFiles.Items.Clear();
                UpdateEnemyButtons();
                return;
            }
            await LoadIsoAfsAsync(project.ActiveAfsPath, project.ActiveDatName);
        }
        RefreshEnemyFiles();
    }

    private void RefreshEnemyFiles()
    {
        string? previous = (cmbEnemyFiles.SelectedItem as AfsEntry)?.FileName;
        string? preferred = previous ?? settings.SelectedEnemyEslName;
        syncingEnemyFileSelection = true;
        cmbEnemyFiles.BeginUpdate();
        cmbEnemyFiles.Items.Clear();
        AfsEntry? selected = null;
        if (loadedAfs != null)
        {
            var entries = AfsService.GetEmleonEslEntries(loadedAfs).ToArray();
            cmbEnemyFiles.Items.AddRange(entries.Cast<object>().ToArray());
            if (entries.Length > 0)
            {
                int index = !string.IsNullOrWhiteSpace(preferred) ? Array.FindIndex(entries, x => x.FileName.Equals(preferred, StringComparison.OrdinalIgnoreCase)) : -1;
                cmbEnemyFiles.SelectedIndex = index >= 0 ? index : 0;
                selected = cmbEnemyFiles.SelectedItem as AfsEntry;
            }
            lblEnemyStatus.Text = entries.Length == 0 ? "Nenhum emleon*.ESL encontrado neste AFS." : $"{entries.Length:N0} ESL(s) encontrado(s) no AFS ativo.";
            ExtractLog($"Enemy Manager: {entries.Length:N0} arquivo(s) emleon*.ESL encontrado(s) em {loadedAfs.IsoAfsEntry.FullPath}.");
        }
        else lblEnemyStatus.Text = "AFS não carregado.";
        cmbEnemyFiles.EndUpdate();
        syncingEnemyFileSelection = false;
        UpdateEnemyFileInfo();
        UpdateEnemyButtons();
        if (selected != null) OpenSelectedEnemyEsl(false);
    }

    private async void btnEnemyRefresh_Click(object? sender, EventArgs e) => await EnsureEnemyAfsAndRefreshAsync();
    private void cmbEnemyFiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateEnemyFileInfo(); UpdateEnemyButtons();
        if (syncingEnemyFileSelection || cmbEnemyFiles.SelectedItem is not AfsEntry entry) return;
        settings.SelectedEnemyEslName = entry.FileName;
        SaveSettings();
        OpenSelectedEnemyEsl(false);
    }
    private void btnEnemyOpen_Click(object? sender, EventArgs e) => OpenSelectedEnemyEsl(false);
    private void btnEnemyReextract_Click(object? sender, EventArgs e) => OpenSelectedEnemyEsl(true);
    private void btnEnemySave_Click(object? sender, EventArgs e) => SaveCurrentEnemyEsl();
    private void btnEnemyQuickAdjustments_Click(object? sender,EventArgs e)
    {
        if(selectedEnemyScene==null||loadedAfs==null||string.IsNullOrWhiteSpace(project.RootPath)){MessageBox.Show(this,"Abra um arquivo ESL antes de usar os ajustes rápidos.","Ajustes rápidos",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        try
        {
            Dictionary<string,EslScene> scenes=LoadEnemyQuickAdjustmentScenes();
            var locations=scenes.ToDictionary(x=>x.Key,x=>(IReadOnlyList<(byte Stage,byte Room,byte Active)>)x.Value.Entries.Select(e=>(e.StageID,e.RoomID,e.Active)).Where(e=>e.StageID!=0||e.RoomID!=0).ToArray(),StringComparer.OrdinalIgnoreCase);
            using var dialog=new EnemyQuickAdjustmentsDialog(Path.GetFileName(currentEnemyEslPath!),scenes.Keys.ToArray(),locations);
            if(dialog.ShowLocalizedDialog(this)!=DialogResult.OK||dialog.Options==null)return;
            if(dialog.SelectedFiles.Any(path=>Path.GetFullPath(path).Equals(Path.GetFullPath(currentEnemyEslPath!),StringComparison.OrdinalIgnoreCase))&&enemySceneModified&&!SaveCurrentEnemyEsl(false))return;
            EnemyQuickAdjustmentResult total=default;
            if(dialog.SelectedFiles.Count>1||dialog.AllFiles)
            {
                foreach(string selectedPath in dialog.SelectedFiles)
                {
                    if(!scenes.TryGetValue(selectedPath,out EslScene? selectedScene))continue;
                    var pair=new KeyValuePair<string,EslScene>(selectedPath,selectedScene);
                    EslScene scene=pair.Value;EnemyQuickAdjustmentResult result=EnemyQuickAdjustments.Apply(scene,dialog.Options,Path.GetFileName(pair.Key));
                    total=new(total.Matched+result.Matched,total.Changed+result.Changed,total.Weapons+result.Weapons,total.Equipment+result.Equipment,total.Scales+result.Scales);
                    if(result.Changed>0)Ps2EslWriter.Save(scene);
                }
                if(dialog.SelectedFiles.Any(path=>Path.GetFullPath(path).Equals(Path.GetFullPath(currentEnemyEslPath!),StringComparison.OrdinalIgnoreCase)))
                {if(!string.IsNullOrWhiteSpace(currentEnemyEslPath)&&File.Exists(currentEnemyEslPath))selectedEnemyScene=Ps2EslReader.Read(currentEnemyEslPath);enemySceneModified=false;RefreshEnemyEntries();OnEnemySceneLoaded(selectedEnemyScene!);}
            }
            else
            {
                total=EnemyQuickAdjustments.Apply(selectedEnemyScene,dialog.Options,Path.GetFileName(currentEnemyEslPath!));
                NotifyEnemyEntriesChanged();
            }
            lblEnemyStatus.Text=$"Ajustes rápidos: {total.Changed:N0} de {total.Matched:N0} inimigos alterados.";
            MessageBox.Show(this,$"Ajustes concluídos.\n\nInimigos encontrados: {total.Matched:N0}\nInimigos alterados: {total.Changed:N0}\nArmas sorteadas: {total.Weapons:N0}\nEquipamentos sorteados: {total.Equipment:N0}\nTamanhos sorteados: {total.Scales:N0}"+(dialog.AllFiles?"\n\nOs ESLs foram salvos e os backups .bak foram preservados.":"\n\nClique em SALVAR para gravar o ESL atual."),"Ajustes rápidos",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Não foi possível aplicar os ajustes",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }

    private Dictionary<string,EslScene> LoadEnemyQuickAdjustmentScenes()
    {
        var result=new Dictionary<string,EslScene>(StringComparer.OrdinalIgnoreCase);string dir=Path.Combine(project.RootPath!,"Extracted","ESL");Directory.CreateDirectory(dir);
        foreach(AfsEntry entry in AfsService.GetEmleonEslEntries(loadedAfs!))
        {
            string path=Path.Combine(dir,entry.FileName);
            if(!File.Exists(path))
            {
                AfsService.ExtractEntry(loadedAfs!,entry,path);
                ExtractLog($"Enemy Manager: {entry.FileName} extraído automaticamente para indexar os cenários disponíveis.");
            }
            result[path]=Path.GetFullPath(path).Equals(Path.GetFullPath(currentEnemyEslPath!),StringComparison.OrdinalIgnoreCase)?selectedEnemyScene!:Ps2EslReader.Read(path);
        }
        return result;
    }
    private void chkEnemyActiveOnly_CheckedChanged(object? sender, EventArgs e)
    {
        RefreshEnemyEntries();
        if (chkVisualEnemyInactive != null && chkVisualEnemyInactive.Checked == chkEnemyActiveOnly.Checked) chkVisualEnemyInactive.Checked = !chkEnemyActiveOnly.Checked;
        if (visualViewport != null && !visualViewport.IsDisposed) visualViewport.ShowInactiveEnemies = !chkEnemyActiveOnly.Checked;
    }
    private void cmbEnemyLocationFilter_SelectedIndexChanged(object? sender, EventArgs e) => RefreshEnemyEntries();
    private void lstEnemyEntries_SelectedIndexChanged(object? sender, EventArgs e) => UpdateEnemySelectionUi();

    private void pgEnemyProperties_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
    {
        List<EslEnemyEntry> entries = GetSelectedEnemyEntries();
        int[] selected = entries.Select(x => x.Index).ToArray();
        var descriptor = e.ChangedItem.PropertyDescriptor;
        object? oldValue = e.OldValue;
        if (descriptor != null && entries.Count > 0 && visualViewport != null)
        {
            EslEnemyEntry[] targets = entries.ToArray();
            visualViewport.RegisterEnemyUndo(() =>
            {
                foreach (EslEnemyEntry entry in targets) try { descriptor.SetValue(entry, oldValue); } catch { }
                NotifyEnemyEntriesChanged(targets.Select(x => x.Index));
                pgEnemyProperties.Refresh();
            });
        }
        NotifyEnemyEntriesChanged(selected);
    }

    private void SyncEnemyFriendlyEditors(EslEnemyEntry? entry) => SyncEnemyFriendlyEditorsForSelection(entry == null ? new List<EslEnemyEntry>() : new List<EslEnemyEntry> { entry });

    private void SyncEnemyFriendlyEditorsForSelection(IReadOnlyList<EslEnemyEntry> entries)
    {
        if (cmbEnemyTypeFriendly == null || cmbEnemySubtypeFriendly == null) return;
        syncingEnemyFriendlyUi = true;
        try
        {
            cmbEnemyTypeFriendly.BeginUpdate();
            cmbEnemyTypeFriendly.Items.Clear();
            foreach (var enemy in EslEnemyCatalog.All) cmbEnemyTypeFriendly.Items.Add(new EnemyChoice(enemy.Id, $"em{enemy.Id:X2}: {enemy.Name}"));
            cmbEnemyTypeFriendly.EndUpdate();

            if (entries.Count == 0)
            {
                cmbEnemyTypeFriendly.SelectedIndex = -1;
                cmbEnemySubtypeFriendly.Items.Clear();
                cmbEnemyTypeFriendly.Enabled = cmbEnemySubtypeFriendly.Enabled = false;
                return;
            }

            cmbEnemyTypeFriendly.Enabled = true;
            byte firstType = entries[0].EnemyType;
            bool sameType = entries.All(x => x.EnemyType == firstType);
            int typeIndex = -1;
            if (sameType)
            {
                for (int i = 0; i < cmbEnemyTypeFriendly.Items.Count; i++) if (cmbEnemyTypeFriendly.Items[i] is EnemyChoice x && x.Id == firstType) { typeIndex = i; break; }
                if (typeIndex < 0) { cmbEnemyTypeFriendly.Items.Add(new EnemyChoice(firstType, $"em{firstType:X2}: Unknown")); typeIndex = cmbEnemyTypeFriendly.Items.Count - 1; }
            }
            cmbEnemyTypeFriendly.SelectedIndex = typeIndex;

            if (!sameType)
            {
                cmbEnemySubtypeFriendly.Items.Clear();
                cmbEnemySubtypeFriendly.SelectedIndex = -1;
                cmbEnemySubtypeFriendly.Enabled = false;
                return;
            }

            cmbEnemySubtypeFriendly.Enabled = true;
            byte firstSubtype = entries[0].Subtype;
            bool sameSubtype = entries.All(x => x.Subtype == firstSubtype);
            FillEnemySubtypeChoices(firstType, firstSubtype, sameSubtype);
        }
        finally { syncingEnemyFriendlyUi = false; }
    }

    private void FillEnemySubtypeChoices(byte enemyType, byte selectedSubtype, bool selectCurrent = true)
    {
        cmbEnemySubtypeFriendly.BeginUpdate();
        cmbEnemySubtypeFriendly.Items.Clear();
        foreach (var subtype in EslEnemyCatalog.GetSubtypes(enemyType).OrderBy(x => x.Key)) cmbEnemySubtypeFriendly.Items.Add(new EnemyChoice(subtype.Key, $"0x{subtype.Key:X2}: {subtype.Value}"));
        int subtypeIndex = -1;
        for (int i = 0; i < cmbEnemySubtypeFriendly.Items.Count; i++) if (cmbEnemySubtypeFriendly.Items[i] is EnemyChoice x && x.Id == selectedSubtype) { subtypeIndex = i; break; }
        if (subtypeIndex < 0)
        {
            cmbEnemySubtypeFriendly.Items.Add(new EnemyChoice(selectedSubtype, $"0x{selectedSubtype:X2}: Unknown"));
            subtypeIndex = cmbEnemySubtypeFriendly.Items.Count - 1;
        }
        cmbEnemySubtypeFriendly.SelectedIndex = selectCurrent ? subtypeIndex : -1;
        cmbEnemySubtypeFriendly.EndUpdate();
    }

    private void cmbEnemyTypeFriendly_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (syncingEnemyFriendlyUi || cmbEnemyTypeFriendly.SelectedItem is not EnemyChoice choice) return;
        List<EslEnemyEntry> entries = GetSelectedEnemyEntries();
        if (entries.Count == 0) return;
        int[] selected = entries.Select(x => x.Index).ToArray();
        var oldValues = entries.Select(x => (Entry: x, Type: x.EnemyType, Subtype: x.Subtype)).ToArray();
        visualViewport?.RegisterEnemyUndo(() =>
        {
            foreach (var old in oldValues) { old.Entry.EnemyType = old.Type; old.Entry.Subtype = old.Subtype; }
            NotifyEnemyEntriesChanged(oldValues.Select(x => x.Entry.Index));
        });
        syncingEnemyFriendlyUi = true;
        try
        {
            var subs = EslEnemyCatalog.GetSubtypes(choice.Id);
            byte subtype = subs.Count > 0 ? subs.Keys.OrderBy(x => x).First() : (byte)0;
            foreach (EslEnemyEntry entry in entries) { entry.EnemyType = choice.Id; entry.Subtype = subtype; }
            FillEnemySubtypeChoices(choice.Id, subtype);
            pgEnemyProperties.Refresh();
        }
        finally { syncingEnemyFriendlyUi = false; }
        NotifyEnemyEntriesChanged(selected);
    }

    private void cmbEnemySubtypeFriendly_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (syncingEnemyFriendlyUi || cmbEnemySubtypeFriendly.SelectedItem is not EnemyChoice choice) return;
        List<EslEnemyEntry> entries = GetSelectedEnemyEntries();
        if (entries.Count == 0) return;
        int[] selected = entries.Select(x => x.Index).ToArray();
        var oldValues = entries.Select(x => (Entry: x, Subtype: x.Subtype)).ToArray();
        visualViewport?.RegisterEnemyUndo(() =>
        {
            foreach (var old in oldValues) old.Entry.Subtype = old.Subtype;
            NotifyEnemyEntriesChanged(oldValues.Select(x => x.Entry.Index));
        });
        foreach (EslEnemyEntry entry in entries) entry.Subtype = choice.Id;
        pgEnemyProperties.Refresh();
        NotifyEnemyEntriesChanged(selected);
    }

    private void UpdateEnemyFileInfo()
    {
        if (cmbEnemyFiles.SelectedItem is not AfsEntry entry)
        {
            lblEnemyFileInfo.Text = "Nenhum ESL selecionado";
            return;
        }
        lblEnemyFileInfo.Text = $"AFS #{entry.Index:D4}  •  atual {FormatBytes(entry.CurrentSize)}  •  reservado {FormatBytes(entry.AllocatedSize)}  •  livre {FormatBytes(entry.FreeSpace)}";
    }

    private void UpdateEnemyButtons()
    {
        bool hasFile = cmbEnemyFiles.SelectedItem is AfsEntry;
        btnEnemyOpen.Enabled = hasFile;
        btnEnemyReextract.Enabled = hasFile;
        btnEnemySave.Enabled = selectedEnemyScene != null && !string.IsNullOrWhiteSpace(currentEnemyEslPath);
        if(btnEnemyQuickAdjustments!=null)btnEnemyQuickAdjustments.Enabled=selectedEnemyScene!=null;
    }

    private void OpenSelectedEnemyEsl(bool forceExtract)
    {
        if (loadedAfs == null || cmbEnemyFiles.SelectedItem is not AfsEntry entry) return;
        LoadEnemyEslEntry(entry, forceExtract, true);
    }

    private bool LoadEnemyEslEntry(AfsEntry entry, bool forceExtract, bool interactive)
    {
        if (loadedAfs == null || string.IsNullOrWhiteSpace(project.RootPath)) return false;
        try
        {
            string dir = Path.Combine(project.RootPath, "Extracted", "ESL");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, entry.FileName);
            if (forceExtract && File.Exists(path) && interactive && MessageBox.Show(this, "Isso substituirá a cópia extraída atual pelo ESL original do AFS. Continuar?", "Re-extrair ESL", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;
            if (forceExtract || !File.Exists(path))
            {
                AfsService.ExtractEntry(loadedAfs, entry, path);
                ExtractLog($"Enemy Manager: {entry.FileName} extraído do AFS para {path}.");
            }
            else ExtractLog($"Enemy Manager: usando cópia já extraída de {entry.FileName}.");

            selectedEnemyScene = Ps2EslReader.Read(path);
            enemySceneModified = false;
            currentEnemyEslPath = path;
            currentEnemyAfsEntry = entry;
            PopulateEnemyLocationFilter();
            PopulateVisualEnemyLocationFilter(project.ActiveDatName);
            RefreshEnemyEntries();
            UpdateEnemyButtons();
            if (lblEnemyStatus != null) lblEnemyStatus.Text = $"{entry.FileName} carregado.";
            OnEnemySceneLoaded(selectedEnemyScene);
            ExtractLog($"Enemy Manager: {entry.FileName} carregado • {selectedEnemyScene.ActiveCount:N0}/{selectedEnemyScene.Entries.Count:N0} entries ativas.");
            return true;
        }
        catch (Exception ex)
        {
            if (lblEnemyStatus != null) lblEnemyStatus.Text = "Erro ao abrir ESL.";
            if (interactive) MessageBox.Show(this, ex.Message, "Enemy Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExtractLog("Enemy Manager: ERRO: " + ex.Message);
            return false;
        }
    }

    private async Task EnsureDefaultVisualEnemyEslAsync()
    {
        if (!RequireWorkspace()) return;
        if (loadedAfs == null)
        {
            if (string.IsNullOrWhiteSpace(project.IsoPath) || !File.Exists(project.IsoPath)) return;
            await LoadIsoAfsAsync(project.ActiveAfsPath, project.ActiveDatName);
        }
        if (loadedAfs == null) return;
        if (EnsureVisualEnemyEslForScenario(project.ActiveDatName)) return;
        if (selectedEnemyScene != null && currentEnemyAfsEntry != null && currentEnemyAfsEntry.FileName.Equals("emleon00.esl", StringComparison.OrdinalIgnoreCase)) return;
        AfsEntry? entry = AfsService.GetEmleonEslEntries(loadedAfs).FirstOrDefault(x => x.FileName.Equals("emleon00.esl", StringComparison.OrdinalIgnoreCase));
        if (entry == null) { ExtractLog("Visual Editor: emleon00.esl não encontrado no AFS ativo."); return; }
        LoadEnemyEslEntry(entry, false, false);
    }

    private bool EnsureVisualEnemyEslForScenario(string? datName)
    {
        if (loadedAfs == null || string.IsNullOrWhiteSpace(project.RootPath) ||
            !TryParseScenarioLocation(datName, out byte stage, out byte room)) return false;

        string directory = Path.Combine(project.RootPath, "Extracted", "ESL");
        Directory.CreateDirectory(directory);
        AfsEntry? matchingEntry = null;
        EslScene? matchingScene = null;
        foreach (AfsEntry entry in AfsService.GetEmleonEslEntries(loadedAfs))
        {
            string path = Path.Combine(directory, entry.FileName);
            try
            {
                if (!File.Exists(path))
                {
                    AfsService.ExtractEntry(loadedAfs, entry, path);
                    ExtractLog($"Visual Editor: {entry.FileName} extraído automaticamente para localizar os inimigos de {Path.GetFileNameWithoutExtension(datName)}.");
                }

                EslScene scene = !string.IsNullOrWhiteSpace(currentEnemyEslPath) && Path.GetFullPath(path).Equals(Path.GetFullPath(currentEnemyEslPath), StringComparison.OrdinalIgnoreCase)
                    ? selectedEnemyScene ?? Ps2EslReader.Read(path)
                    : Ps2EslReader.Read(path);
                if (!scene.Entries.Any(x => x.StageID == stage && x.RoomID == room)) continue;
                matchingEntry = entry;
                matchingScene = scene;
                break;
            }
            catch (Exception ex)
            {
                ExtractLog($"Visual Editor: não foi possível verificar {entry.FileName}: {ex.Message}");
            }
        }

        if (matchingEntry == null || matchingScene == null)
        {
            ExtractLog($"Visual Editor: nenhum emleon*.ESL contém entries para {Path.GetFileNameWithoutExtension(datName)}.");
            return false;
        }

        if (currentEnemyAfsEntry?.FileName.Equals(matchingEntry.FileName, StringComparison.OrdinalIgnoreCase) == true && selectedEnemyScene != null)
            return true;

        if (enemySceneModified && !string.IsNullOrWhiteSpace(currentEnemyEslPath))
        {
            DialogResult answer = MessageBox.Show(this,
                $"O arquivo {Path.GetFileName(currentEnemyEslPath)} tem alterações não salvas. Salvar antes de carregar {matchingEntry.FileName}?\n\nSim: salvar • Não: descartar • Cancelar: manter o ESL atual",
                "Alterações no ESL", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (answer == DialogResult.Cancel) return false;
            if (answer == DialogResult.Yes && !SaveCurrentEnemyEsl(false)) return false;
        }

        int selection = -1;
        for (int i = 0; i < cmbEnemyFiles.Items.Count; i++)
            if (cmbEnemyFiles.Items[i] is AfsEntry item && item.FileName.Equals(matchingEntry.FileName, StringComparison.OrdinalIgnoreCase)) { selection = i; break; }
        if (selection >= 0)
        {
            syncingEnemyFileSelection = true;
            try { cmbEnemyFiles.SelectedIndex = selection; }
            finally { syncingEnemyFileSelection = false; }
        }
        if (!LoadEnemyEslEntry(matchingEntry, false, false)) return false;
        ExtractLog($"Visual Editor: {matchingEntry.FileName} selecionado automaticamente para {Path.GetFileNameWithoutExtension(datName)} ({matchingScene.Entries.Count(x => x.StageID == stage && x.RoomID == room):N0} inimigos no cenário).");
        return true;
    }

    private void PopulateEnemyLocationFilter()
    {
        if (cmbEnemyLocationFilter == null) return;
        var previous = cmbEnemyLocationFilter.SelectedItem as EnemyLocationFilter;
        cmbEnemyLocationFilter.BeginUpdate();
        cmbEnemyLocationFilter.Items.Clear();
        cmbEnemyLocationFilter.Items.Add(new EnemyLocationFilter(null, null, "Todas as fases"));
        if (selectedEnemyScene != null)
            foreach (var x in selectedEnemyScene.Entries.Select(e => (e.StageID, e.RoomID)).Distinct().OrderBy(x => x.StageID).ThenBy(x => x.RoomID))
                cmbEnemyLocationFilter.Items.Add(new EnemyLocationFilter(x.StageID, x.RoomID, $"r{x.StageID:X1}{x.RoomID:X2}"));
        int selected = 0;
        if (previous?.StageId != null && previous.RoomId != null)
            for (int i=1;i<cmbEnemyLocationFilter.Items.Count;i++) if (cmbEnemyLocationFilter.Items[i] is EnemyLocationFilter f && f.StageId==previous.StageId && f.RoomId==previous.RoomId) { selected=i; break; }
        cmbEnemyLocationFilter.SelectedIndex = selected;
        cmbEnemyLocationFilter.EndUpdate();
    }

    private bool EnemyEntryPassesManagerFilter(EslEnemyEntry e)
    {
        if (chkEnemyActiveOnly.Checked && e.Active == 0) return false;
        if (cmbEnemyLocationFilter.SelectedItem is EnemyLocationFilter { StageId: byte stageId, RoomId: byte roomId } &&
            (e.StageID != stageId || e.RoomID != roomId)) return false;
        return true;
    }

    private void RefreshEnemyEntries() => RefreshEnemyEntriesPreserveSelection();

    private void UpdateEnemyEntryCount()
    {
        lblEnemyEntryCount.Text = selectedEnemyScene == null ? "Nenhum ESL aberto" : $"{lstEnemyEntries.Items.Count:N0} exibidos • {selectedEnemyScene.ActiveCount:N0} ativos • {selectedEnemyScene.Entries.Count:N0} totais";
    }

    private bool SaveCurrentEnemyEsl(bool showSuccess = true)
    {
        if (selectedEnemyScene == null || string.IsNullOrWhiteSpace(currentEnemyEslPath)) return false;
        try
        {
            Ps2EslWriter.Save(selectedEnemyScene);
            enemySceneModified = false;
            lstEnemyEntries.Refresh();
            OnEnemySceneLoaded(selectedEnemyScene);
            lblEnemyStatus.Text = $"{Path.GetFileName(currentEnemyEslPath)} salvo.";
            ExtractLog($"Enemy Manager: ESL salvo: {Path.GetFileName(currentEnemyEslPath)}.");
            if (showSuccess) MessageBox.Show(this, "ESL salvo com sucesso. O backup .bak inicial foi preservado.", "Enemy Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Enemy Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExtractLog("Enemy Manager: erro ao salvar ESL: " + ex.Message);
            return false;
        }
    }

    private async Task EnsureVisualEnemyModelsAsync()
    {
        if (loadingVisualEnemyModels) { visualEnemyModelsEnsurePending = true; return; }
        if (selectedEnemyScene == null || visualViewport == null || visualViewport.IsDisposed || loadedAfs == null || string.IsNullOrWhiteSpace(project.RootPath)) return;
        byte? stage = null, room = null;
        if (cmbVisualEnemyLocationFilter?.SelectedItem is VisualEnemyLocationFilterItem filter) { stage = filter.StageId; room = filter.RoomId; }
        IEnumerable<EslEnemyEntry> source = selectedEnemyScene.Entries;
        if (stage.HasValue && room.HasValue) source = source.Where(x => x.StageID == stage.Value && x.RoomID == room.Value);
        byte[] needed = source.Select(x => x.EnemyType).Distinct().Where(x => !visualEnemyModelCache.ContainsKey(x) && !visualEnemyModelFailed.Contains(x)).OrderBy(x => x).ToArray();
        if (needed.Length == 0) { visualViewport.SetEnemyModels(visualEnemyModelCache); visualViewport.SetEnemyAttachmentAnimation(null, 0f); RefreshVisualEnemyAnimationChoices(); return; }

        loadingVisualEnemyModels = true;
        try
        {
            string dir = Path.Combine(project.RootPath, "Extracted", "Enemies");
            Directory.CreateDirectory(dir);
            foreach (byte type in needed)
            {
                string datName = $"em{type:X2}.dat";
                AfsEntry? afsEntry = AfsService.FindFirstValidEntryByName(loadedAfs, datName);
                if (afsEntry == null) { visualEnemyModelFailed.Add(type); ExtractLog($"Visual Editor: {datName} não encontrado no AFS; marcador mantido."); continue; }
                string path = Path.Combine(dir, datName);
                try
                {
                    if (!File.Exists(path) || new FileInfo(path).Length != afsEntry.CurrentSize)
                    {
                        AfsService.ExtractEntry(loadedAfs, afsEntry, path);
                        ExtractLog($"Visual Editor: {datName} extraído para cache de modelos.");
                    }
                    EnemyModelScene model = await Task.Run(() => Ps2EnemyDatReader.Read(path, type));
                    visualEnemyModelCache[type] = model;
                    TrackVisualEnemyDat(type, path);
                    ExtractLog($"Visual Editor: {datName} • {model.LoadedBinCount}/{model.BinCount} BINs • {model.Triangles.Count:N0} tris • {model.TexturePackages.Count} TPL(s).");
                }
                catch (Exception ex)
                {
                    visualEnemyModelFailed.Add(type);
                    ExtractLog($"Visual Editor: falha ao carregar {datName}: {ex.Message}");
                }
            }
            visualViewport.SetEnemyModels(visualEnemyModelCache);
            visualViewport.SetEnemyAttachmentAnimation(null, 0f);
            RefreshVisualEnemyAnimationChoices();
            UpdateVisualStatus();
        }
        finally
        {
            loadingVisualEnemyModels = false;
            if (visualEnemyModelsEnsurePending)
            {
                visualEnemyModelsEnsurePending = false;
                if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => _ = EnsureVisualEnemyModelsAsync()));
            }
        }
    }

    private void ResetVisualEnemyModelCache()
    {
        visualEnemyModelCache.Clear();
        visualEnemyModelFailed.Clear();
        visualEnemyModelReloadVersions.Clear();
        visualEnemyDatSignatures.Clear();
        visualViewport?.SetEnemyModels(visualEnemyModelCache);
        RefreshVisualEnemyAnimationChoices();
    }

    private void TrackVisualEnemyDat(byte enemyType, string path)
    {
        try
        {
            var file = new FileInfo(path);
            visualEnemyDatSignatures[enemyType] = (Path.GetFullPath(path), file.Length, file.LastWriteTimeUtc.Ticks);
        }
        catch { return; }
        if (visualEnemyDatWatcherInitialized) return;
        visualEnemyDatWatcherInitialized = true;
        visualEnemyDatWatchTimer.Tick += VisualEnemyDatWatchTimer_Tick;
        visualEnemyDatWatchTimer.Start();
    }

    private async void VisualEnemyDatWatchTimer_Tick(object? sender, EventArgs e)
    {
        foreach (var pair in visualEnemyDatSignatures.ToArray())
        {
            byte enemyType = pair.Key;
            string path = pair.Value.Path;
            if (visualEnemyDatReloadPending.Contains(enemyType) || !File.Exists(path)) continue;
            var file = new FileInfo(path);
            if (file.Length == pair.Value.Length && file.LastWriteTimeUtc.Ticks == pair.Value.WriteTicks) continue;
            visualEnemyDatReloadPending.Add(enemyType);
            try
            {
                // Repack tools commonly replace the DAT in more than one write. Only open it
                // after its size and timestamp have remained unchanged for a short interval.
                long length = file.Length, ticks = file.LastWriteTimeUtc.Ticks;
                await Task.Delay(450);
                if (!File.Exists(path)) continue;
                file.Refresh();
                if (file.Length != length || file.LastWriteTimeUtc.Ticks != ticks) continue;
                await RefreshVisualEnemyModelFromDatAsync(path);
            }
            finally { visualEnemyDatReloadPending.Remove(enemyType); }
        }
    }

    private async Task<bool> RefreshVisualEnemyModelFromDatAsync(string path)
    {
        string stem = Path.GetFileNameWithoutExtension(path);
        if (stem.Length != 4 || !stem.StartsWith("em", StringComparison.OrdinalIgnoreCase) ||
            !byte.TryParse(stem.AsSpan(2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out byte enemyType)) return false;

        // Do not eagerly load models which are not being displayed. If they become necessary,
        // EnsureVisualEnemyModelsAsync will load their current DAT in the normal way.
        bool isDisplayed = selectedEnemyScene?.Entries.Any(x => x.EnemyType == enemyType) == true;
        if (!visualEnemyModelCache.ContainsKey(enemyType) && !isDisplayed) return false;

        int version = visualEnemyModelReloadVersions.GetValueOrDefault(enemyType) + 1;
        visualEnemyModelReloadVersions[enemyType] = version;
        visualEnemyModelFailed.Remove(enemyType);
        try
        {
            EnemyModelScene refreshed = await Task.Run(() => Ps2EnemyDatReader.Read(path, enemyType));
            if (visualEnemyModelReloadVersions.GetValueOrDefault(enemyType) != version) return false;
            visualEnemyModelCache[enemyType] = refreshed;
            TrackVisualEnemyDat(enemyType, path);
            if (visualViewport == null || visualViewport.IsDisposed) return true;
            visualViewport.SetEnemyModels(visualEnemyModelCache);
            visualViewport.SetEnemyAttachmentAnimation(null, 0f);
            RefreshVisualEnemyAnimationChoices();
            visualViewport.RefreshEnemyGeometry(selectedEnemyScene?.Entries.FirstOrDefault(x => x.EnemyType == enemyType));
            UpdateVisualStatus();
            ExtractLog($"Visual Editor: texturas e modelo de {stem}.dat recarregados após edição.");
            return true;
        }
        catch (Exception ex)
        {
            if (visualEnemyModelReloadVersions.GetValueOrDefault(enemyType) == version)
                ExtractLog($"Visual Editor: não foi possível atualizar {stem}.dat: {ex.Message}");
            return false;
        }
    }

    private void OnEnemySceneLoaded(EslScene? scene)
    {
        selectedEnemyScene = scene;
        if (visualViewport == null || visualViewport.IsDisposed) return;
        if (btnVisualSaveEsl != null) btnVisualSaveEsl.Enabled = scene != null && !string.IsNullOrWhiteSpace(currentEnemyEslPath);
        visualViewport.SetEslScene(scene);
        WireVisualEnemyEvents();
        visualViewport.ShowInactiveEnemies = chkEnemyActiveOnly != null && !chkEnemyActiveOnly.Checked;
        if (chkVisualEnemyInactive != null) chkVisualEnemyInactive.Checked = visualViewport.ShowInactiveEnemies;
        PopulateVisualEnemyLocationFilter(project.ActiveDatName);
        RefreshVisualEnemyEntryList();
        _ = EnsureVisualEnemyModelsAsync();
        UpdateVisualStatus();
    }
}
