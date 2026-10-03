using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private static bool SupportedSource(string path) => SupportedDatName.IsMatch(Path.ChangeExtension(Path.GetFileName(path), ".dat"));

    private string ResolveCharacterSource(string path)
    {
        path = characterWorkspace.Source(path);
        if (Path.GetExtension(path).Equals(".idx", StringComparison.OrdinalIgnoreCase)) return path;
        string stem = Path.GetFileNameWithoutExtension(path);
        var candidates = new[] { Path.ChangeExtension(path, ".idx") }
            .Concat(extractedRoots.Select(root => Path.Combine(root, stem, "Content", stem + ".idx")));
        return candidates.FirstOrDefault(File.Exists) ?? path;
    }

    private void BrowseDat()
    {
        using var dialog = new OpenFileDialog { Filter = "Personagem extraído (*.idx)|*.idx|Personagem DAT (*.dat)|*.dat", Title = "Abrir personagem extraído — selecione o IDX da pasta Content" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        if (!SupportedSource(dialog.FileName))
        { MessageBox.Show(this, "Selecione plXX.idx, emXX.idx ou wepXX.idx na pasta Content."); return; }
        string source = ResolveCharacterSource(dialog.FileName);
        RefreshDatList(source); LoadDat(source);
    }

    private void InitializeDatSelection()
    {
        string? preferred = !string.IsNullOrWhiteSpace(initialPath) ? ResolveCharacterSource(initialPath) : FindDefaultPl00();
        RefreshDatList(preferred);
        if (preferred != null && File.Exists(preferred)) LoadDat(preferred);
        else if (cmbDat.Items.Count > 0) cmbDat.SelectedIndex = 0;
        else status.Text = "Nenhum personagem extraído foi encontrado. Extraia o DAT para gerar a pasta Content e o IDX.";
    }

    private void RefreshDatDropdown()
    {
        string? selectedPath = datPath == null ? null : characterWorkspace.Source(datPath);
        RefreshDatList(selectedPath);
        if (selectedPath != null) LoadDat(selectedPath);
        status.Text = $"Lista atualizada • {cmbDat.Items.Count} personagem(ns) extraído(s).";
    }

    private void RefreshDatList(string? selectPath = null)
    {
        if (selectPath != null) selectPath = ResolveCharacterSource(selectPath);
        var paths = new List<string>();
        foreach (string root in extractedRoots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                paths.AddRange(Directory.EnumerateFiles(root, "*.idx", SearchOption.AllDirectories)
                    .Where(p => SupportedSource(p) && !IsInsideOriginalDat(p)));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        if (selectPath != null && File.Exists(selectPath) && SupportedSource(selectPath) && !IsInsideOriginalDat(selectPath)) paths.Insert(0, selectPath);
        var sources = paths.Distinct(StringComparer.OrdinalIgnoreCase)
            .GroupBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(p => Path.GetExtension(p).Equals(".idx", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First())
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
        syncingDatList = true;
        cmbDat.Items.Clear();
        foreach (string path in sources) cmbDat.Items.Add(new DatItem(path));
        for (int i = 0; i < cmbDat.Items.Count; i++)
            if (cmbDat.Items[i] is DatItem item && string.Equals(item.Path, selectPath, StringComparison.OrdinalIgnoreCase)) { cmbDat.SelectedIndex = i; break; }
        syncingDatList = false;
        string? donorPath = (cmbMeshDonor.SelectedItem as DatItem)?.Path;
        cmbMeshDonor.Items.Clear();
        foreach (string path in sources) cmbMeshDonor.Items.Add(new DatItem(path));
        int donorIndex = Array.FindIndex(sources, p => string.Equals(p, donorPath, StringComparison.OrdinalIgnoreCase));
        if (donorIndex < 0) donorIndex = Array.FindIndex(sources, p => !string.Equals(p, selectPath, StringComparison.OrdinalIgnoreCase));
        if (donorIndex >= 0) cmbMeshDonor.SelectedIndex = donorIndex;
    }
    private static bool IsInsideOriginalDat(string path)
    {
        for (DirectoryInfo? directory = new FileInfo(path).Directory; directory != null; directory = directory.Parent)
            if (directory.Name.Equals("OriginalDAT", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void LoadMeshDonor()
    {
        lstMeshLibrary.Items.Clear();
        if (cmbMeshDonor.SelectedItem is not DatItem donor || !File.Exists(donor.Path)) return;
        try
        {
            string donorDat = characterWorkspace.Open(donor.Path);
            EnemyModelScene donorModel = Ps2EnemyDatReader.Read(donorDat, PreviewType);
            Dictionary<int, string> donorNames = ReadBinNames(characterWorkspace.MetadataPath(donorDat));
            foreach (EnemyModelPart part in donorModel.Parts.OrderBy(x => x.BinIndex))
            {
                var item = new MeshLibraryItem(donorDat, donorModel, part, donorNames.GetValueOrDefault(part.BinIndex));
                var row = new ListViewItem(item.ToString()) { Tag = item };
                row.SubItems.Add(part.Triangles.Count.ToString("N0"));
                row.SubItems.Add($"{part.Size.X:0.##} × {part.Size.Y:0.##} × {part.Size.Z:0.##}");
                lstMeshLibrary.Items.Add(row);
            }
            status.Text = $"Biblioteca: {Path.GetFileName(donor.Path)} • {donorModel.Parts.Count} mesh(es). Arraste uma linha sobre o modelo.";
        }
        catch (Exception ex) { status.Text = "Não foi possível carregar a biblioteca: " + ex.Message; }
    }

    private void LoadDat(string path)
    {
        try
        {
            Cursor = Cursors.WaitCursor;
            path = characterWorkspace.Open(ResolveCharacterSource(path));
            ScenarioCameraState? currentCamera = cameraInitialized ? viewport.GetCameraState() : null;
            bool changedDat = !string.Equals(datPath, path, StringComparison.OrdinalIgnoreCase);
            int selectedBin = changedDat ? -1 : selectedPart?.BinIndex ?? -1;
            if (datPath != null) SaveSelectedTextureName();
            if (changedDat && datPath != null) { SaveSelectedPartAnnotation(); SaveViewState(); }
            if (changedDat) editingTexture = null;
            if (changedDat) { undoHistory.Clear(); redoHistory.Clear(); showingOriginal = false; }
            bool weapon = IsWeaponDat(path);
            string? leonPath = weapon ? FindLeonDat(path) : null;
            if (weapon && leonPath == null) throw new FileNotFoundException("Não encontrei pl00.dat extraído. Extraia o pacote do Leon para visualizar esta arma.");
            EnemyModelScene ReadPreview(string source) => weapon
                ? WeaponPreviewModel.MergeLeonAndFirstWeaponModel(Ps2EnemyDatReader.Read(leonPath!, PreviewType), Ps2EnemyDatReader.Read(source, PreviewType), source, out _)
                : Ps2EnemyDatReader.Read(source, PreviewType);
            model = ReadPreview(path); datPath = path;
            LoadManualAssignments(path);
            if (changedDat) LoadViewState(path);
            string backup = path + ".bak";
            originalModel = ReadPreview(File.Exists(backup) ? backup : path);
            var enemy = new EslEnemyEntry { Index = 0, Active = 1, EnemyType = PreviewType };
            var previewScene = new ScenarioScene { SourcePath = path, BoundsMin = model.BoundsMin, BoundsMax = model.BoundsMax };
            if (changedDat) viewport.GridRadiusOverride = previewScene.Radius;
            viewport.SetScene(previewScene);
            viewport.SetEnemyModels(new Dictionary<byte, EnemyModelScene> { [PreviewType] = showingOriginal ? originalModel : model });
            int? weaponBin = weapon ? model.Parts.Last().BinIndex : null;
            viewport.SetEnemyForcedHandHeldPart(PreviewType, weaponBin);
            viewport.SetEnemyAttachmentBone(weapon && model.Skeleton?.FirstIndexById.TryGetValue(0x0A, out int handBone) == true ? handBone : -1);
            viewport.SetEnemyTextureAssignments(PreviewType, showingOriginal ? null : manualAssignments.ToDictionary(x => x.Key, x => (x.Value.TplEntry, x.Value.TextureIndex)));
            viewport.SetEslScene(new EslScene(path, new List<EslEnemyEntry> { enemy }));
            if (changedDat) RefreshCharacterAnimations();
            if (changedDat)
            {
                visibleBins.Clear();
                VisibilityPreset? preset = visibilityPresets.FirstOrDefault(x => x.Name == activePresetName);
                IEnumerable<int> defaultBins = weapon ? GetWeaponPreviewBins(model, weaponBin!.Value) : model.Parts.Select(x => x.BinIndex);
                foreach (int bin in savedVisibleBins ?? (IEnumerable<int>?)preset?.VisibleBins ?? defaultBins) visibleBins.Add(bin);
            }
            viewport.SetVisibleEnemyModelParts(PreviewType, visibleBins);
            viewport.FitScene();
            if (!cameraInitialized && initialCamera.HasValue) viewport.SetCameraState(initialCamera.Value);
            else if (currentCamera.HasValue) viewport.SetCameraState(currentCamera.Value);
            cameraInitialized = true;
            PopulateParts(); PopulateTextures();
            if (selectedBin >= 0) SelectPartByBinIndex(selectedBin);
            UpdateCompareButton(); UpdateHistoryButtons();
            status.Text = $"{Path.GetFileName(characterWorkspace.Source(path))} • {Path.GetDirectoryName(characterWorkspace.Source(path))} • {model.LoadedBinCount}/{model.BinCount} BINs • {model.TexturePackages.Count} TPLs • {model.Triangles.Count:N0} triângulos";
            datOpened?.Invoke(characterWorkspace.Source(path));
            SelectDatInDropdown(path);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Não foi possível abrir o DAT", MessageBoxButtons.OK, MessageBoxIcon.Error); status.Text = "Falha ao carregar o DAT."; }
        finally { Cursor = Cursors.Default; }
    }

    public void SaveCameraState()
    {
        if (cameraInitialized) cameraSaved?.Invoke(viewport.GetCameraState());
    }

    private void SelectDatInDropdown(string path)
    {
        syncingDatList = true;
        for (int i = 0; i < cmbDat.Items.Count; i++)
            if (cmbDat.Items[i] is DatItem item && string.Equals(item.Path, characterWorkspace.Source(path), StringComparison.OrdinalIgnoreCase)) { cmbDat.SelectedIndex = i; syncingDatList = false; return; }
        syncingDatList = false;
    }

    private string? FindDefaultPl00()
    {
        string[] candidates = extractedRoots.Select(root => Path.Combine(root, "pl00", "Content", "pl00.idx")).ToArray();
        return candidates.FirstOrDefault(File.Exists);
    }

    public void RefreshAvailableDats()
    {
        if (datPath != null) { RefreshDatList(datPath); LoadDat(datPath); }
    }

    private static bool IsWeaponDat(string path) => Path.GetFileName(path).StartsWith("wep", StringComparison.OrdinalIgnoreCase);

    private string? FindLeonDat(string weaponPath)
    {
        var roots = extractedRoots.Prepend(Path.GetDirectoryName(weaponPath)!).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots)
        {
            try
            {
                string? found = Directory.EnumerateFiles(root, "pl00.idx", SearchOption.AllDirectories)
                    .Where(x => !IsInsideOriginalDat(x))
                    .OrderByDescending(x => Directory.GetParent(x)?.Name.Equals("Content", StringComparison.OrdinalIgnoreCase) == true)
                    .FirstOrDefault();
                if (found != null) return characterWorkspace.Open(ResolveCharacterSource(found));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return null;
    }

    private static IEnumerable<int> GetWeaponPreviewBins(EnemyModelScene model, int weaponBin)
    {
        var visible = new HashSet<int> { 0, 1, 2, 3, 4, 5, weaponBin };
        EnemyModelPart[] hands = model.Parts.Where(p => p.BinIndex is >= 10 and <= 17).OrderBy(p => p.BinIndex).ToArray();
        EnemyModelPart? left = hands.FirstOrDefault(p => p.BoundsMax.X < 0), right = hands.FirstOrDefault(p => p.BoundsMin.X > 0);
        if (left != null) visible.Add(left.BinIndex);
        if (right != null) visible.Add(right.BinIndex);
        if (left == null || right == null) foreach (EnemyModelPart hand in hands.Take(2)) visible.Add(hand.BinIndex);
        return visible;
    }
}



