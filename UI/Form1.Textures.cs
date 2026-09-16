using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private readonly TextureWorkspaceService textureService = new();
    private string? activeTextureTplPath;
    private string? activeTextureSmdPath;
    private TextureSmdItem? activeTextureSource;
    private readonly Dictionary<TextureInfo, TextureSmdItem> textureOwners = new(ReferenceEqualityComparer.Instance);
    private bool loadingTextures;
    private bool syncingTextureDat;
    private int previewRequestId;
    private int textureThumbnailSize = 104;
    private bool textureDropBusy;
    private int thumbnailRequestId;


    private void RefreshTextureDatList()
    {
        if (cmbTextureDat == null || cmbTextureDat.IsDisposed) return;
        string? active = project.ActiveDatName;
        syncingTextureDat = true;
        cmbTextureDat.BeginUpdate();
        try
        {
            cmbTextureDat.Items.Clear();
            foreach (DatProjectState state in project.DatStates.Where(x => !string.IsNullOrWhiteSpace(x.ContentPath) && Directory.Exists(x.ContentPath)).OrderBy(x => x.DatName, StringComparer.OrdinalIgnoreCase))
                cmbTextureDat.Items.Add(new TextureDatItem(state.DatName, state.ContentPath!));
            int index = -1;
            if (!string.IsNullOrWhiteSpace(active))
            {
                for (int i = 0; i < cmbTextureDat.Items.Count; i++)
                    if (cmbTextureDat.Items[i] is TextureDatItem item && item.DatName.Equals(active, StringComparison.OrdinalIgnoreCase)) { index = i; break; }
            }
            if (cmbTextureDat.Items.Count > 0) cmbTextureDat.SelectedIndex = index >= 0 ? index : 0;
        }
        finally { cmbTextureDat.EndUpdate(); syncingTextureDat = false; }
    }

    private async void cmbTextureDat_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (syncingTextureDat || cmbTextureDat.SelectedItem is not TextureDatItem item) return;
        bool changedDat = !item.DatName.Equals(project.ActiveDatName, StringComparison.OrdinalIgnoreCase);
        if (changedDat)
        {
            ResetTextureUi("Trocando cenário...");
            ActivateTextureDat(item);
        }
        else RefreshTextureSmdList();
        while (loadingTextures) await Task.Delay(25);
        await LoadNativeTexturesAsync(false);
    }

    private void ActivateTextureDat(TextureDatItem item)
    {
        DatProjectState? state = GetDatState(item.DatName, false);
        if (state == null) return;
        project.ActiveDatName = state.DatName;
        project.ActiveDatPath = state.OriginalDatPath;
        project.ActiveContentPath = state.ContentPath;
        project.ActiveBuildDatPath = state.BuildDatPath;
        project.ActiveAfsPath = state.AfsPath ?? project.ActiveAfsPath;
        project.LastBuildUtc = state.LastBuildUtc;
        if (!restoringSession) SaveProject();
        for (int i = 0; i < cmbDatEntries.Items.Count; i++)
        {
            if (cmbDatEntries.Items[i] is AfsEntry entry && entry.FileName.Equals(state.DatName, StringComparison.OrdinalIgnoreCase))
            {
                if (cmbDatEntries.SelectedIndex != i) cmbDatEntries.SelectedIndex = i;
                break;
            }
        }
        RefreshDashboard();
        RefreshExtractedContent();
        RefreshTextureSmdList();
        UpdateBuildUi();
        _ = RefreshTrackedDatsAsync();
    }

    private void RefreshTextureSmdList()
    {
        if (cmbTextureSmd == null || cmbTextureSmd.IsDisposed) return;
        string? content = GetActiveContentPath();
        cmbTextureSmd.BeginUpdate();
        try
        {
            cmbTextureSmd.Items.Clear();
            if (string.IsNullOrWhiteSpace(content) || !Directory.Exists(content))
            {
                ResetTextureUi("Extraia um cenario para listar as texturas.");
                return;
            }
            foreach (string smd in Directory.GetFiles(content, "*.SMD", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                cmbTextureSmd.Items.Add(new TextureSmdItem(smd, Path.GetRelativePath(content, smd)));
            foreach (string eff in Directory.GetFiles(content, "*.EFF", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                try
                {
                    foreach (EffTplPackageInfo package in EffTextureService.ReadPackages(eff))
                        cmbTextureSmd.Items.Add(new TextureSmdItem(eff, Path.GetRelativePath(content, eff), package.PackageIndex, package.TextureCount));
                }
                catch { }
            if (cmbTextureSmd.Items.Count > 0) cmbTextureSmd.SelectedIndex = 0;
            else ResetTextureUi("Nenhum SMD ou EFF com texturas encontrado no Content atual.");
        }
        finally { cmbTextureSmd.EndUpdate(); }
    }

    private void cmbTextureSmd_SelectedIndexChanged(object? sender, EventArgs e) { }

    private async void btnTextureLoad_Click(object? sender, EventArgs e)
    {
        RefreshTextureSmdList();
        await LoadNativeTexturesAsync(false);
    }

    private async void btnTextureReload_Click(object? sender, EventArgs e)
    {
        if (cmbTextureSmd.Items.Count == 0) return;
        if (MessageBox.Show("Reler todas as texturas dos SMDs e EFFs substituir� os TPLs de trabalho atuais. Continuar?", "Reler fontes", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        await LoadNativeTexturesAsync(true);
    }

    private async Task LoadNativeTexturesAsync(bool forceExtract)
    {
        if (loadingTextures) return;
        TextureSmdItem[] sources = cmbTextureSmd.Items.Cast<object>().OfType<TextureSmdItem>().ToArray();
        if (sources.Length == 0) return;
        loadingTextures = true;
        lblTextureLoading.Text = "Carregando texturas...";
        lblTextureLoading.Visible = true;
        lblTextureLoading.BringToFront();
        btnTextureLoad.Enabled = btnTextureReload.Enabled = false;
        try
        {
            ClearTexturePreview();
            lvTextures.Items.Clear();
            textureImages.Images.Clear();
            textureOwners.Clear();
            var catalogs = new List<(TextureSmdItem Source, string TplPath, IReadOnlyList<TextureInfo> Textures)>();
            foreach (TextureSmdItem source in sources)
            {
                string tplPath = GetTplWorkPath(source);
                if (forceExtract || !File.Exists(tplPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tplPath)!);
                    if (source.IsEff) await Task.Run(() => EffTextureService.ExtractTpl(source.FullPath, source.PackageIndex, tplPath));
                    else await Task.Run(() => SmdTextureService.ExtractTpl(source.FullPath, tplPath));
                }
                IReadOnlyList<TextureInfo> catalog = await Task.Run(() => textureService.ReadCatalog(tplPath));
                catalogs.Add((source, tplPath, catalog));
            }
            int total = catalogs.Sum(x => x.Textures.Count), loaded = 0;
            foreach (var catalog in catalogs)
                foreach (TextureInfo info in catalog.Textures)
                {
                    Bitmap thumb;
                    try { thumb = await Task.Run(() => textureService.CreateThumbnail(catalog.TplPath, info.Index, textureThumbnailSize)); }
                    catch { thumb = new Bitmap(104, 104); using Graphics g = Graphics.FromImage(thumb); g.Clear(Color.FromArgb(35, 38, 44)); }
                    string key = catalog.Source.Key + "|" + info.Index;
                    textureImages.Images.Add(key, thumb);
                    string sourceLabel = catalog.Source.IsEff ? $"EFF TPL #{catalog.Source.PackageIndex:D3}" : "SMD";
                    var listItem = new ListViewItem($"#{info.Index:D3} - {sourceLabel}\n{info.Width}x{info.Height}") { ImageKey = key, Tag = info };
                    textureOwners[info] = catalog.Source;
                    lvTextures.Items.Add(listItem);
                    lblTextureLoading.Text = $"Carregando texturas...\n{++loaded:N0} / {total:N0}";
                    await Task.Yield();
                }
            lblTextureCount.Text = $"{total:N0} texturas";
            btnTextureExportAll.Enabled = total > 0;
            btnTextureReplaceAll.Enabled = total > 0;
            lblTplStatus.Text = $"{sources.Count(x => !x.IsEff)} SMD - {sources.Count(x => x.IsEff)} pacote(s) TPL em EFF";
            if (lvTextures.Items.Count > 0) lvTextures.Items[0].Selected = true;
        }
        catch (Exception ex)
        {
            ResetTextureUi("Falha ao carregar TPL: " + ex.Message);
            MessageBox.Show(ex.Message, "Texturas", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadingTextures = false;
            lblTextureLoading.Visible = false;
            btnTextureLoad.Enabled = btnTextureReload.Enabled = true;
        }
    }

    private async Task OpenTextureModuleForSmdAsync(string smdPath,int textureIndex)
    {
        SaveVisualCameraIfLeaving();
        RefreshTextureDatList();RefreshTextureSmdList();
        for(int i=0;i<cmbTextureSmd.Items.Count;i++)if(cmbTextureSmd.Items[i] is TextureSmdItem item&&item.FullPath.Equals(smdPath,StringComparison.OrdinalIgnoreCase)){cmbTextureSmd.SelectedIndex=i;break;}
        ShowPage(pnlTextures,btnNavTextures,"Texturas");RememberMainPage("Textures");
        await LoadNativeTexturesAsync(false);
        ListViewItem? target=lvTextures.Items.Cast<ListViewItem>().FirstOrDefault(x=>x.Tag is TextureInfo info&&info.Index==textureIndex&&textureOwners.TryGetValue(info,out TextureSmdItem? owner)&&owner.FullPath.Equals(smdPath,StringComparison.OrdinalIgnoreCase));
        if(target!=null){foreach(ListViewItem item in lvTextures.Items)item.Selected=false;target.Selected=true;target.Focused=true;target.EnsureVisible();lvTextures.Focus();}
    }

    private async void lvTextures_SelectedIndexChanged(object? sender, EventArgs e)
    {
        int request = ++previewRequestId;
        Image? previous = picTexturePreview.Image;
        picTexturePreview.Image = null;
        previous?.Dispose();
        picTexturePreview.Zoom = 1f;
        if (lvTextures.SelectedItems.Count != 1 || lvTextures.SelectedItems[0].Tag is not TextureInfo info || !textureOwners.TryGetValue(info, out TextureSmdItem? source))
        {
            btnTextureReplace.Enabled = false; btnTextureExport.Enabled = false; return;
        }
        string tplPath = GetTplWorkPath(source);
        int textureIndex = info.Index;
        activeTextureSource = source;
        activeTextureSmdPath = source.FullPath;
        activeTextureTplPath = tplPath;
        btnTextureReplace.Enabled = true;
        btnTextureExport.Enabled = true;
        lblTextureTitle.Text = $"Texture #{textureIndex:D3}";
        string origin = source.IsEff ? $"EFF - TPL #{source.PackageIndex:D3}" : "SMD";
        lblTextureMeta.Text = $"{origin} - {Path.GetFileName(source.FullPath)}\n{info.Width} x {info.Height} - {info.BitDepthName}\n{info.InterlaceName} - Mipmaps: {info.MipmapCount}";
        lblTplStatus.Text = origin;
        try
        {
            Bitmap bitmap = await Task.Run(() => textureService.Decode(tplPath, textureIndex));
            if (request != previewRequestId) { bitmap.Dispose(); return; }
            picTexturePreview.Image = bitmap;
        }
        catch (Exception ex)
        {
            if (request == previewRequestId) lblTextureMeta.Text += "\nPreview indisponivel: " + ex.Message;
        }
    }

    private async void lvTextures_DoubleClick(object? sender, EventArgs e)
    {
        if (lvTextures.SelectedItems.Count != 1 || lvTextures.SelectedItems[0].Tag is not TextureInfo info || string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        using var dialog = new TextureMipmapDialog(activeTextureTplPath, info.Index, textureService);
        dialog.ShowLocalizedDialog(this);
        if (!dialog.Modified) return;
        await SyncTextureTplToSmdAsync($"Mipmaps da textura #{info.Index:D3} atualizados.");
        await ReloadTextureCatalogKeepingSelectionAsync(info.Index);
    }

    private async void btnTextureReplace_Click(object? sender, EventArgs e)
    {
        if (lvTextures.SelectedItems.Count != 1 || lvTextures.SelectedItems[0].Tag is not TextureInfo info || string.IsNullOrWhiteSpace(activeTextureTplPath) || string.IsNullOrWhiteSpace(activeTextureSmdPath)) return;
        using var dialog = new OpenFileDialog { Filter = "Imagens (*.png;*.bmp;*.jpg;*.jpeg)|*.png;*.bmp;*.jpg;*.jpeg|Todos os arquivos (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            btnTextureReplace.Enabled = false;
            TextureInfo updated = await Task.Run(() => textureService.ReplaceFromImage(activeTextureTplPath, info.Index, dialog.FileName));
            await SyncTextureTplToSmdAsync("TPL atualizado na fonte selecionada.");
            RefreshVisualEditorTexturesFromTextureManager();
            await RefreshTextureItemAsync(updated);
            await RefreshChangeStatusAsync();
            _ = RefreshTrackedDatsAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Substituir textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { btnTextureReplace.Enabled = lvTextures.SelectedItems.Count == 1; }
    }


    private async void btnTextureReplaceAll_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(activeTextureTplPath) || string.IsNullOrWhiteSpace(activeTextureSmdPath) || !File.Exists(activeTextureTplPath)) return;
        using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta com os PNGs nomeados pelos índices das texturas" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;

        TextureInfo[] textures = lvTextures.Items.Cast<ListViewItem>().Select(x => x.Tag).OfType<TextureInfo>().Where(x => activeTextureSource != null && textureOwners.TryGetValue(x, out TextureSmdItem? owner) && owner.Key == activeTextureSource.Key).OrderBy(x => x.Index).ToArray();
        if (textures.Length == 0) return;
        string[] pngs = Directory.GetFiles(dialog.SelectedPath, "*.png", SearchOption.TopDirectoryOnly);
        var matches = new List<(TextureInfo Texture, string Path)>();
        foreach (TextureInfo texture in textures)
        {
            string? path = FindPngForTextureIndex(pngs, texture.Index);
            if (!string.IsNullOrWhiteSpace(path)) matches.Add((texture, path));
        }
        if (matches.Count == 0)
        {
            MessageBox.Show("Nenhum PNG correspondente aos índices foi encontrado.\n\nExemplos aceitos: texture_000.png, texture_000_128x128.png ou 000.png.", "Substituir todas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show($"Foram encontrados {matches.Count} PNG(s) para {textures.Length} textura(s).\n\nSubstituir todas as correspondências no pacote selecionado?", "Substituir todas", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        if (answer != DialogResult.OK) return;

        btnTextureReplaceAll.Enabled = false;
        btnTextureReplace.Enabled = false;
        lblTextureLoading.Visible = true;
        lblTextureLoading.BringToFront();
        int replaced = 0;
        var failures = new List<string>();
        try
        {
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                lblTextureLoading.Text = $"Substituindo texturas...\n{i + 1:N0} / {matches.Count:N0}";
                try
                {
                    await Task.Run(() => textureService.ReplaceFromImage(activeTextureTplPath, match.Texture.Index, match.Path));
                    replaced++;
                }
                catch (Exception ex)
                {
                    failures.Add($"#{match.Texture.Index:D3}: {ex.Message}");
                }
                await Task.Yield();
            }

            if (replaced > 0)
            {
            await SyncTextureTplToSmdAsync("TPL atualizado na fonte selecionada.");
                RefreshVisualEditorTexturesFromTextureManager();
                await LoadNativeTexturesAsync(false);
                await RefreshChangeStatusAsync();
                _ = RefreshTrackedDatsAsync();
            }

            string message = $"{replaced} textura(s) substituída(s).";
            if (failures.Count > 0) message += $"\n\n{failures.Count} falha(s):\n" + string.Join("\n", failures.Take(8)) + (failures.Count > 8 ? "\n..." : "");
            MessageBox.Show(message, "Substituir todas", MessageBoxButtons.OK, failures.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        finally
        {
            lblTextureLoading.Visible = false;
            btnTextureReplaceAll.Enabled = lvTextures.Items.Count > 0;
            btnTextureReplace.Enabled = lvTextures.SelectedItems.Count == 1;
        }
    }

    private static string? FindPngForTextureIndex(IEnumerable<string> pngs, int index)
    {
        string token = index.ToString("D3");
        string[] files = pngs.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
        string? exact = files.FirstOrDefault(x => Path.GetFileName(x).Equals($"texture_{token}.png", StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        exact = files.FirstOrDefault(x => Path.GetFileName(x).Equals($"{token}.png", StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        string prefix = $"texture_{token}_";
        exact = files.FirstOrDefault(x => Path.GetFileName(x).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        return files.FirstOrDefault(x =>
        {
            string name = Path.GetFileNameWithoutExtension(x);
            return name.StartsWith(token + "_", StringComparison.OrdinalIgnoreCase) || name.StartsWith(token + "-", StringComparison.OrdinalIgnoreCase);
        });
    }

    private async Task RefreshTextureItemAsync(TextureInfo info)
    {
        if (string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        Bitmap thumb = await Task.Run(() => textureService.CreateThumbnail(activeTextureTplPath, info.Index, textureThumbnailSize));
        string key = info.Index.ToString();
        if (textureImages.Images.ContainsKey(key)) textureImages.Images.RemoveByKey(key);
        textureImages.Images.Add(key, thumb);
        foreach (ListViewItem item in lvTextures.Items)
        {
            if (item.Tag is TextureInfo old && old.Index == info.Index)
            {
                item.Tag = info; item.ImageKey = key; item.Text = $"#{info.Index:D3}\n{info.Width}x{info.Height}"; item.Selected = true; break;
            }
        }
        lvTextures_SelectedIndexChanged(null, EventArgs.Empty);
    }

    private void btnTextureExport_Click(object? sender, EventArgs e)
    {
        if (lvTextures.SelectedItems.Count != 1 || lvTextures.SelectedItems[0].Tag is not TextureInfo info || string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        using var dialog = new SaveFileDialog { Filter = "PNG (*.png)|*.png", FileName = $"texture_{info.Index:D3}.png" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { textureService.ExportPng(activeTextureTplPath, info.Index, dialog.FileName); ExtractLog("Textura exportada: " + dialog.FileName); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Exportar textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnTextureExportAll_Click(object? sender, EventArgs e)
    {
        if (lvTextures.Items.Count == 0) return;
        using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta para exportar as texturas" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            btnTextureExportAll.Enabled = false;
            var items = lvTextures.Items.Cast<ListViewItem>().Select(x => x.Tag).OfType<TextureInfo>().Where(textureOwners.ContainsKey).ToArray();
            await Task.Run(() =>
            {
                foreach (TextureInfo info in items)
                {
                    TextureSmdItem source = textureOwners[info];
                    string prefix = source.IsEff ? $"{Path.GetFileNameWithoutExtension(source.FullPath)}_tpl_{source.PackageIndex:D3}" : Path.GetFileNameWithoutExtension(source.FullPath);
                    textureService.ExportPng(GetTplWorkPath(source), info.Index, Path.Combine(dialog.SelectedPath, $"{prefix}_texture_{info.Index:D3}_{info.Width}x{info.Height}.png"));
                }
            });
            ExtractLog($"{items.Length} textura(s) exportada(s) para {dialog.SelectedPath}");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Exportar texturas", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { btnTextureExportAll.Enabled = lvTextures.Items.Count > 0; }
    }

    private void btnTextureOpenExternal_Click(object? sender, EventArgs e)
    {
        if (activeTextureSource == null || string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        try { LaunchTplManager(activeTextureTplPath); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "TPL Manager", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LaunchTplManager(string tplPath)
    {
        if (string.IsNullOrWhiteSpace(settings.TplManagerPath) || !File.Exists(settings.TplManagerPath))
        {
            MessageBox.Show("Configure o TPL Manager em Ferramentas para usar o editor externo.", "TPL Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var psi = new System.Diagnostics.ProcessStartInfo(settings.TplManagerPath) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(settings.TplManagerPath)! };
        psi.ArgumentList.Add(tplPath);
        System.Diagnostics.Process.Start(psi);
        ExtractLog("TPL aberto no TPL Manager: " + tplPath);
    }

    private string GetTplWorkPath(string smdPath, string? datName = null)
    {
        string root = project.RootPath ?? Path.GetDirectoryName(smdPath)!;
        datName ??= project.ActiveDatName;
        string scenario = !string.IsNullOrWhiteSpace(datName) ? Path.GetFileNameWithoutExtension(datName) : "Scenario";
        return Path.Combine(root, "Mods", scenario, "Textures", Path.GetFileNameWithoutExtension(smdPath) + ".tpl");
    }

    private string GetTplWorkPath(TextureSmdItem item)
    {
        string path=GetTplWorkPath(item.FullPath);
        return item.IsEff?Path.Combine(Path.GetDirectoryName(path)!,Path.GetFileNameWithoutExtension(path)+$"_eff_{item.PackageIndex:D3}.tpl"):path;
    }

    private string GetSmdBackupPath(string smdPath, string? datName = null)
    {
        string root = project.RootPath ?? Path.GetDirectoryName(smdPath)!;
        datName ??= project.ActiveDatName;
        string scenario = !string.IsNullOrWhiteSpace(datName) ? Path.GetFileNameWithoutExtension(datName) : "Scenario";
        string name = Path.GetFileNameWithoutExtension(smdPath) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(smdPath);
        return Path.Combine(root, "Mods", scenario, "Backups", name);
    }

    private void UpdateTplSelectionInfo(TextureSmdItem item)
    {
        if (!File.Exists(item.FullPath)) { lblTplStatus.Text = "Selecione um DAT para comecar."; return; }
        try
        {
            string tplPath = GetTplWorkPath(item);
            long size = item.IsEff ? EffTextureService.ReadPackages(item.FullPath)[item.PackageIndex].Length : SmdTextureService.ReadInfo(item.FullPath).TplSize;
            string source = item.IsEff ? $"EFF - TPL #{item.PackageIndex:D3}" : "TPL interno";
            lblTplStatus.Text = $"{source}: {FormatBytes(size)} - {(File.Exists(tplPath) ? "TPL de trabalho pronto" : "ainda nao carregado")}";
        }
        catch (Exception ex) { lblTplStatus.Text = "Nao foi possivel ler o TPL: " + ex.Message; }
    }

    private void ResetTextureUi(string message)
    {
        activeTextureTplPath = null; activeTextureSmdPath = null; activeTextureSource = null; textureOwners.Clear();
        if (lblTextureLoading != null) lblTextureLoading.Visible = false;
        lvTextures?.Items.Clear(); textureImages?.Images.Clear();
        lblTextureCount.Text = "0 texturas"; lblTplStatus.Text = message;
        ClearTexturePreview();
        btnTextureReplace.Enabled = false; btnTextureExport.Enabled = false; btnTextureExportAll.Enabled = false; btnTextureReplaceAll.Enabled = false;
    }

    private void ClearTexturePreview()
    {
        if (picTexturePreview == null) return;
        Image? old = picTexturePreview.Image; picTexturePreview.Image = null; old?.Dispose();
        lblTextureTitle.Text = "Nenhuma textura selecionada";
        lblTextureMeta.Text = "Selecione uma textura para ver os detalhes.";
    }


    private void trackTextureThumb_ValueChanged(object? sender, EventArgs e)
    {
        textureThumbnailSize = trackTextureThumb.Value;
        lblTextureThumbSize.Text = $"{textureThumbnailSize}px";
        textureImages.ImageSize = new Size(textureThumbnailSize, textureThumbnailSize);
        lvTextures.TileSize = new Size(textureThumbnailSize + 24, textureThumbnailSize + 38);
        _ = ReloadTextureThumbnailsAsync();
    }

    private async Task ReloadTextureThumbnailsAsync()
    {
        if (loadingTextures || lvTextures.Items.Count == 0) return;
        int request = ++thumbnailRequestId;
        var generated = new List<(ListViewItem Item, string Key, Bitmap Bitmap)>();
        foreach (ListViewItem item in lvTextures.Items)
        {
            if (item.Tag is not TextureInfo info || !textureOwners.TryGetValue(info, out TextureSmdItem? source)) continue;
            try
            {
                string key = source.Key + "|" + info.Index;
                Bitmap thumb = await Task.Run(() => textureService.CreateThumbnail(GetTplWorkPath(source), info.Index, textureThumbnailSize));
                if (request != thumbnailRequestId) { thumb.Dispose(); foreach (var entry in generated) entry.Bitmap.Dispose(); return; }
                generated.Add((item, key, thumb));
            }
            catch { }
        }
        if (request != thumbnailRequestId) { foreach (var entry in generated) entry.Bitmap.Dispose(); return; }
        textureImages.Images.Clear();
        foreach (var entry in generated) { textureImages.Images.Add(entry.Key, entry.Bitmap); entry.Item.ImageKey = entry.Key; }
    }

    private void lvTextures_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        ListViewItem? item = lvTextures.GetItemAt(e.X, e.Y);
        if (item != null) item.Selected = true;
    }

    private void lvTextures_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) { e.Effect = DragDropEffects.None; return; }
        string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
        e.Effect = files?.Length == 1 && Path.GetExtension(files[0]).Equals(".png", StringComparison.OrdinalIgnoreCase) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void lvTextures_DragDrop(object? sender, DragEventArgs e)
    {
        if (textureDropBusy || e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length != 1 || !Path.GetExtension(files[0]).Equals(".png", StringComparison.OrdinalIgnoreCase)) return;
        Point point = lvTextures.PointToClient(new Point(e.X, e.Y));
        ListViewItem? target = lvTextures.GetItemAt(point.X, point.Y) ?? (lvTextures.SelectedItems.Count == 1 ? lvTextures.SelectedItems[0] : null);
        if (target?.Tag is not TextureInfo info) return;
        textureDropBusy = true;
        try { await ReplaceTextureFromFileAsync(info, files[0]); }
        finally { textureDropBusy = false; }
    }

    private async Task ReplaceTextureFromFileAsync(TextureInfo info, string path)
    {
        if (string.IsNullOrWhiteSpace(activeTextureTplPath) || string.IsNullOrWhiteSpace(activeTextureSmdPath)) return;
        TextureInfo updated = await Task.Run(() => textureService.ReplaceFromImage(activeTextureTplPath, info.Index, path));
        await SyncTextureTplToSmdAsync($"Textura #{info.Index:D3} substituída por drag & drop.");
        await RefreshTextureItemAsync(updated);
    }

    private void ctxTexture_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        bool selected = lvTextures.SelectedItems.Count == 1 && lvTextures.SelectedItems[0].Tag is TextureInfo;
        miTextureExport.Enabled = selected; miTextureReplace.Enabled = selected; miTextureIncrease.Enabled = selected; miTextureDecrease.Enabled = selected;
        if (selected && lvTextures.SelectedItems[0].Tag is TextureInfo info)
        {
            miTextureIncrease.Enabled = info.BitDepth == 0x08;
            miTextureDecrease.Enabled = info.BitDepth == 0x09;
        }
    }

    private void miTextureExport_Click(object? sender, EventArgs e) => btnTextureExport_Click(sender, e);
    private void miTextureReplace_Click(object? sender, EventArgs e) => btnTextureReplace_Click(sender, e);
    private async void miTextureIncrease_Click(object? sender, EventArgs e) => await ConvertSelectedTextureBitDepthAsync(256);
    private async void miTextureDecrease_Click(object? sender, EventArgs e) => await ConvertSelectedTextureBitDepthAsync(16);

    private async Task ConvertSelectedTextureBitDepthAsync(int colors)
    {
        if (lvTextures.SelectedItems.Count != 1 || lvTextures.SelectedItems[0].Tag is not TextureInfo info || string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        try
        {
            TextureInfo updated = await Task.Run(() => textureService.ConvertBitDepth(activeTextureTplPath, info.Index, colors));
            await SyncTextureTplToSmdAsync($"Textura #{info.Index:D3} convertida para {(colors == 16 ? "4-bit" : "8-bit")}.");
            await RefreshTextureItemAsync(updated);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Bit depth", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnTextureRotate_Click(object? sender, EventArgs e) => await TransformSelectedTextureAsync(b => b.RotateFlip(RotateFlipType.Rotate90FlipNone), "Rotacionada 90°");
    private async void btnTextureFlipX_Click(object? sender, EventArgs e) => await TransformSelectedTextureAsync(b => b.RotateFlip(RotateFlipType.RotateNoneFlipX), "Flip X aplicado");
    private async void btnTextureFlipY_Click(object? sender, EventArgs e) => await TransformSelectedTextureAsync(b => b.RotateFlip(RotateFlipType.RotateNoneFlipY), "Flip Y aplicado");

    private async void btnTextureResize_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedTexture(out TextureInfo info) || string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        using var dialog = new TextureResizeDialog(info.Width, info.Height);
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            using Bitmap source = textureService.Decode(activeTextureTplPath, info.Index);
            using var resized = new Bitmap(dialog.TargetWidth, dialog.TargetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = dialog.ResamplingMode switch
                {
                    TextureResizeResampling.NearestNeighbor => System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor,
                    TextureResizeResampling.Bilinear => System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear,
                    _ => System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                };
                g.PixelOffsetMode = dialog.ResamplingMode == TextureResizeResampling.NearestNeighbor
                    ? System.Drawing.Drawing2D.PixelOffsetMode.Half
                    : System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.DrawImage(source, new Rectangle(0, 0, resized.Width, resized.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
            }
            TextureInfo updated = await Task.Run(() => textureService.ReplaceFromBitmap(activeTextureTplPath, info.Index, resized, false));
            await SyncTextureTplToSmdAsync($"Textura #{info.Index:D3} redimensionada para {dialog.TargetWidth}x{dialog.TargetHeight}.");
            await RefreshTextureItemAsync(updated);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Resize", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task TransformSelectedTextureAsync(Action<Bitmap> transform, string actionName)
    {
        if (!TryGetSelectedTexture(out TextureInfo info) || string.IsNullOrWhiteSpace(activeTextureTplPath)) return;
        try
        {
            using Bitmap source = textureService.Decode(activeTextureTplPath, info.Index);
            using var transformed = new Bitmap(source); transform(transformed);
            TextureInfo updated = await Task.Run(() => textureService.ReplaceFromBitmap(activeTextureTplPath, info.Index, transformed, false));
            await SyncTextureTplToSmdAsync($"Textura #{info.Index:D3}: {actionName}.");
            await RefreshTextureItemAsync(updated);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ajuste rápido", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnTextureIncreaseAll_Click(object? sender, EventArgs e) => await ConvertAllTextureBitDepthAsync(256);
    private async void btnTextureDecreaseAll_Click(object? sender, EventArgs e) => await ConvertAllTextureBitDepthAsync(16);

    private async Task ConvertAllTextureBitDepthAsync(int colors)
    {
        if (string.IsNullOrWhiteSpace(activeTextureTplPath) || string.IsNullOrWhiteSpace(activeTextureSmdPath)) return;
        string label = colors == 16 ? "4-bit" : "8-bit";
        if (MessageBox.Show($"Converter todas as texturas compatíveis para {label}?", "Bit depth em lote", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        TextureInfo[] items = lvTextures.Items.Cast<ListViewItem>().Select(x => x.Tag).OfType<TextureInfo>().Where(x => activeTextureSource != null && textureOwners.TryGetValue(x, out TextureSmdItem? owner) && owner.Key == activeTextureSource.Key).ToArray();
        int changed = 0; var failures = new List<string>(); lblTextureLoading.Visible = true; lblTextureLoading.BringToFront();
        try
        {
            for (int i = 0; i < items.Length; i++)
            {
                TextureInfo info = items[i]; lblTextureLoading.Text = $"Convertendo para {label}...\n{i + 1} / {items.Length}";
                if (info.BitDepth != 0x08 && info.BitDepth != 0x09) continue;
                if ((colors == 16 && info.BitDepth == 0x08) || (colors == 256 && info.BitDepth == 0x09)) continue;
                try { await Task.Run(() => textureService.ConvertBitDepth(activeTextureTplPath, info.Index, colors)); changed++; }
                catch (Exception ex) { failures.Add($"#{info.Index:D3}: {ex.Message}"); }
            }
            if (changed > 0) await SyncTextureTplToSmdAsync($"{changed} textura(s) convertida(s) para {label}.");
            await ReloadTextureCatalogKeepingSelectionAsync(lvTextures.SelectedItems.Count == 1 && lvTextures.SelectedItems[0].Tag is TextureInfo selected ? selected.Index : 0);
            if (failures.Count > 0) MessageBox.Show($"{changed} convertidas. {failures.Count} falha(s).\n\n" + string.Join("\n", failures.Take(10)), "Bit depth em lote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { lblTextureLoading.Visible = false; }
    }

    private bool TryGetSelectedTexture(out TextureInfo info)
    {
        if (lvTextures.SelectedItems.Count == 1 && lvTextures.SelectedItems[0].Tag is TextureInfo selected) { info = selected; return true; }
        info = null!; return false;
    }

    private async Task SyncTextureTplToSmdAsync(string log)
    {
        if (string.IsNullOrWhiteSpace(activeTextureTplPath) || activeTextureSource == null) return;
        string backup = GetSmdBackupPath(activeTextureSource.FullPath);
        if (activeTextureSource.IsEff)
            await Task.Run(() => EffTextureService.InjectTpl(activeTextureSource.FullPath, activeTextureSource.PackageIndex, activeTextureTplPath, backup));
        else
            await Task.Run(() => SmdTextureService.InjectTpl(activeTextureSource.FullPath, activeTextureTplPath, backup));
        ExtractLog(log);
        RefreshVisualEditorTexturesFromTextureManager();
        await RefreshChangeStatusAsync(); _ = RefreshTrackedDatsAsync();
    }

    private async Task ReloadTextureCatalogKeepingSelectionAsync(int index)
    {
        string? sourceKey = activeTextureSource?.Key;
        await LoadNativeTexturesAsync(false);
        foreach (ListViewItem item in lvTextures.Items)
            if (item.Tag is TextureInfo info && info.Index == index && textureOwners.TryGetValue(info, out TextureSmdItem? owner) && owner.Key == sourceKey) { item.Selected = true; item.EnsureVisible(); break; }
    }

    private sealed record TextureDatItem(string DatName, string ContentPath)
    {
        public override string ToString() => DatName;
    }

    private sealed record TextureSmdItem(string FullPath, string RelativePath, int PackageIndex = -1, int TextureCount = 0)
    {
        public bool IsEff => PackageIndex >= 0;
        public string Key => FullPath + "|" + PackageIndex;
        public override string ToString() => IsEff ? $"{RelativePath} - TPL #{PackageIndex:D3} ({TextureCount} texturas)" : RelativePath;
    }
}
