using System.Data;
using System.Diagnostics;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private readonly DataTable assetTable = CreateAssetTable();
    private readonly BindingSource assetBinding = new();
    private SortOrder assetSortOrder = SortOrder.None;
    private int assetSortColumn = -1;

    private static DataTable CreateAssetTable()
    {
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Type", typeof(string));
        table.Columns.Add("SizeBytes", typeof(long));
        table.Columns.Add("RelativePath", typeof(string));
        table.Columns.Add("FullPath", typeof(string));
        return table;
    }

    private void btnRefreshContent_Click(object? sender, EventArgs e) => RefreshExtractedContent();
    private void btnOpenContentFolder_Click(object? sender, EventArgs e) => OpenFolder(GetActiveContentPath());

    private void gridAssets_SelectionChanged(object? sender, EventArgs e)
    {
        string? selected = GetSelectedContentFile();
        if (string.IsNullOrWhiteSpace(selected)) return;
        string? content = GetActiveContentPath();
        if (!string.IsNullOrWhiteSpace(content))
        {
            project.SelectedContentRelativePath = Path.GetRelativePath(content, selected).Replace('\\', '/');
            if (!restoringSession) SaveProject();
        }
    }

    private void gridAssets_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        string? path = GetSelectedContentFile();
        if (path == null || !File.Exists(path)) return;
        if (Path.GetExtension(path).Equals(".SMD", StringComparison.OrdinalIgnoreCase))
        {
            btnNavTextures_Click(null, EventArgs.Empty);
            for (int i = 0; i < cmbTextureSmd.Items.Count; i++)
            {
                if (cmbTextureSmd.Items[i] is TextureSmdItem item && item.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase)) { cmbTextureSmd.SelectedIndex = i; break; }
            }
            return;
        }
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
    }



    private void gridAssets_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0) return;
        DataGridViewColumn column = gridAssets.Columns[e.ColumnIndex];
        if (string.IsNullOrWhiteSpace(column.DataPropertyName)) return;
        assetSortOrder = assetSortColumn == e.ColumnIndex && assetSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        assetSortColumn = e.ColumnIndex;
        foreach (DataGridViewColumn c in gridAssets.Columns) c.HeaderCell.SortGlyphDirection = SortOrder.None;
        assetBinding.Sort = $"{column.DataPropertyName} {(assetSortOrder == SortOrder.Ascending ? "ASC" : "DESC")}";
        column.HeaderCell.SortGlyphDirection = assetSortOrder;
    }

    private void gridAssets_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (gridAssets.Columns[e.ColumnIndex].Name == "Size" && e.Value is long bytes)
        {
            e.Value = FormatBytes(bytes);
            e.FormattingApplied = true;
        }
    }

    private void assetFilter_Changed(object? sender, EventArgs e) => ApplyAssetFilter();

    private void ApplyAssetFilter()
    {
        if (assetBinding.DataSource is not DataTable) return;
        var filters = new List<string>();
        string search = txtAssetSearch?.Text.Trim() ?? "";
        if (search.Length > 0)
        {
            string value = search.Replace("'", "''").Replace("[", "[[]");
            filters.Add($"(Name LIKE '%{value}%' OR RelativePath LIKE '%{value}%')");
        }
        string type = cmbAssetType?.SelectedItem?.ToString() ?? "Todos";
        if (type == "Outros") filters.Add("NOT (Type IN ('SMD','TPL','BIN','AEV','ESL','SND','SEQ'))");
        else if (type != "Todos")
        {
            string safeType = type.Replace("'", "''");
            filters.Add($"Type = '{safeType}'");
        }
        assetBinding.Filter = string.Join(" AND ", filters);
        UpdateAssetSummary();
    }

    private void RefreshExtractedContentPreserveSelection(string fullPath)
    {
        RefreshExtractedContent();
        SelectAsset(fullPath);
    }

    private void RefreshExtractedContent()
    {
        if (gridAssets == null || gridAssets.IsDisposed) return;
        string? selected = GetSelectedContentFile();
        assetTable.BeginLoadData();
        try
        {
            assetTable.Rows.Clear();
            string? content = GetActiveContentPath();
            if (string.IsNullOrWhiteSpace(content) || !Directory.Exists(content))
            {
                lblContentSummary.Text = "Nenhum cenário extraído carregado.";
                btnOpenContentFolder.Enabled = false;
                assetBinding.DataSource = assetTable;
                gridAssets.DataSource = assetBinding;
                return;
            }

            btnOpenContentFolder.Enabled = true;
            string filesRoot = GetScenarioFilesRoot(content);
            foreach (FileInfo fi in Directory.GetFiles(filesRoot, "*", SearchOption.AllDirectories).Select(x => new FileInfo(x)).OrderBy(x => FileTypePriority(x.Extension)).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                assetTable.Rows.Add(fi.Name, GetFriendlyFileType(fi.Extension), fi.Length, Path.GetRelativePath(content, fi.FullName), fi.FullName);
            }
            assetBinding.DataSource = assetTable;
            gridAssets.DataSource = assetBinding;
            if (gridAssets.Columns["Size"] != null) gridAssets.Columns["Size"]!.DefaultCellStyle.Format = "#";
            ApplyAssetFilter();
            RefreshTextureSmdList();
        }
        catch (Exception ex)
        {
            lblContentSummary.Text = "Erro ao ler o conteúdo extraído.";
            ExtractLog("ERRO AO ATUALIZAR CONTEÚDO: " + ex.Message);
        }
        finally { assetTable.EndLoadData(); }

        if (!string.IsNullOrWhiteSpace(selected)) SelectAsset(selected);
        else RestoreSelectedContentFile();
    }

    private void UpdateAssetSummary()
    {
        string? content = GetActiveContentPath();
        int total = assetBinding.Count;
        int smds = 0;
        foreach (DataRowView row in assetBinding) if (string.Equals(row["Type"]?.ToString(), "SMD", StringComparison.OrdinalIgnoreCase)) smds++;
        lblContentSummary.Text = string.IsNullOrWhiteSpace(content) ? "Nenhum cenário carregado." : $"{total:N0} arquivo(s) visíveis • {smds:N0} SMD • {Path.GetFileName(Path.GetDirectoryName(content))}";
    }

    private void SelectAsset(string fullPath)
    {
        foreach (DataGridViewRow row in gridAssets.Rows)
        {
            if (row.DataBoundItem is DataRowView view && string.Equals(view["FullPath"]?.ToString(), fullPath, StringComparison.OrdinalIgnoreCase))
            {
                row.Selected = true;
                gridAssets.CurrentCell = row.Cells[0];
                if (row.Index >= 0) gridAssets.FirstDisplayedScrollingRowIndex = row.Index;
                return;
            }
        }
    }


    private string GetScenarioFilesRoot(string contentPath)
    {
        if (string.IsNullOrWhiteSpace(project.ActiveDatName)) return contentPath;
        string scenario = Path.GetFileNameWithoutExtension(project.ActiveDatName);
        string scenarioPath = Path.Combine(contentPath, scenario);
        return Directory.Exists(scenarioPath) ? scenarioPath : contentPath;
    }

    private string? GetActiveContentPath()
    {
        if (!string.IsNullOrWhiteSpace(project.ActiveContentPath) && Directory.Exists(project.ActiveContentPath)) return project.ActiveContentPath;
        if (string.IsNullOrWhiteSpace(project.RootPath) || string.IsNullOrWhiteSpace(project.ActiveDatName)) return null;
        string scenario = Path.GetFileNameWithoutExtension(project.ActiveDatName);
        return Path.Combine(project.RootPath, "Extracted", scenario, "Content");
    }

    private string? GetSelectedContentFile()
    {
        if (gridAssets?.CurrentRow?.DataBoundItem is DataRowView view) return view["FullPath"]?.ToString();
        return null;
    }

    private static int FileTypePriority(string extension) => extension.ToUpperInvariant() switch
    {
        ".SMD" => 0, ".TPL" => 1, ".BIN" => 2, ".AEV" => 3, ".ESL" => 4, ".SND" => 5, ".SEQ" => 6, ".DAT" => 98, _ => 50
    };

    private static string GetFriendlyFileType(string extension) => extension.ToUpperInvariant() switch
    {
        ".SMD" => "SMD", ".TPL" => "TPL", ".BIN" => "BIN", ".AEV" => "AEV", ".ESL" => "ESL", ".SND" => "SND", ".SEQ" => "SEQ", ".DAT" => "DAT", "" => "Arquivo", _ => extension.TrimStart('.').ToUpperInvariant()
    };

    private static string FormatBytes(long bytes)
    {
        const double kb = 1024, mb = kb * 1024, gb = mb * 1024;
        if (bytes >= gb) return $"{bytes / gb:N2} GB";
        if (bytes >= mb) return $"{bytes / mb:N2} MB";
        if (bytes >= kb) return $"{bytes / kb:N2} KB";
        return $"{bytes:N0} B";
    }

    private void RestoreSelectedContentFile()
    {
        if (string.IsNullOrWhiteSpace(project.SelectedContentRelativePath)) return;
        string? content = GetActiveContentPath();
        if (string.IsNullOrWhiteSpace(content)) return;
        string full = Path.Combine(content, project.SelectedContentRelativePath.Replace('/', Path.DirectorySeparatorChar));
        SelectAsset(full);
    }
}
