using RE4_PS2_MOD_WORKSPACE.Core.Executable;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ExecutableEditorForm
{
    private DataGridView? cheatsGrid;
    private Label? cheatsDetail;
    private Button? applyCheats, removeCheats;
    private bool refreshingCheats;

    private void Cheats()
    {
        var page = new TabPage("TRAPAÇAS") { BackColor = Bg, Padding = new Padding(12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Bg };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        cheatsDetail = new Label { Dock = DockStyle.Fill, ForeColor = Muted, Padding = new Padding(4), AutoEllipsis = true };
        layout.Controls.Add(cheatsDetail, 0, 0);
        cheatsGrid = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = Bg, BorderStyle = BorderStyle.None,
            GridColor = Border, RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            EnableHeadersVisualStyles = false, EditMode = DataGridViewEditMode.EditOnEnter
        };
        cheatsGrid.ColumnHeadersDefaultCellStyle = new() { BackColor = Surface2, ForeColor = TextColor, SelectionBackColor = Surface2, Font = new Font("Segoe UI Semibold", 9F) };
        cheatsGrid.DefaultCellStyle = new() { BackColor = Surface, ForeColor = TextColor, SelectionBackColor = Color.FromArgb(92, 42, 45), SelectionForeColor = Color.White, Padding = new Padding(4) };
        cheatsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(27, 30, 37);
        cheatsGrid.RowTemplate.Height = 32;
        cheatsGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ativar", FillWeight = 48, SortMode = DataGridViewColumnSortMode.NotSortable });
        cheatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trapaça", FillWeight = 330, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        cheatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Instalação", FillWeight = 125, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        cheatsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", FillWeight = 125, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
        cheatsGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (cheatsGrid.IsCurrentCellDirty) cheatsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        cheatsGrid.CellValueChanged += (_, e) =>
        {
            if (refreshingCheats || e.RowIndex < 0 || e.ColumnIndex != 0) return;
            var row = cheatsGrid.Rows[e.RowIndex];
            if (row.Tag is not SlusCheatStatus option) return;
            if (option.Id is 19 or 20 or 21 && row.Cells[0].Value is true)
            {
                refreshingCheats = true;
                foreach (DataGridViewRow other in cheatsGrid.Rows)
                    if (other != row && other.Tag is SlusCheatStatus { Id: 19 or 20 or 21 }) other.Cells[0].Value = false;
                refreshingCheats = false;
            }
            UpdateCheatButtons();
        };
        layout.Controls.Add(cheatsGrid, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), WrapContents = false };
        applyCheats = new Button();
        Style(applyCheats, "APLICAR SELEÇÃO", Accent, 160);
        applyCheats.Click += (_, _) => ChangeCheats(false);
        removeCheats = new Button();
        Style(removeCheats, "REMOVER TODAS", Surface2, 150);
        removeCheats.Click += (_, _) => ChangeCheats(true);
        actions.Controls.Add(applyCheats);
        actions.Controls.Add(removeCheats);
        actions.Controls.Add(new Label { AutoSize = true, ForeColor = Muted, Margin = new Padding(14, 9, 0, 0), Text = "Desmarque uma opção e aplique para removê-la." });
        layout.Controls.Add(actions, 0, 2);
        page.Controls.Add(layout);
        tabs.TabPages.Add(page);
        RefreshCheatsUi();
    }

    private void RefreshCheatsUi()
    {
        if (doc == null || cheatsGrid == null || cheatsDetail == null) return;
        refreshingCheats = true;
        try
        {
            SlusCheatInspection inspection = SlusCheatPatches.Inspect(doc);
            cheatsDetail.Text = inspection.Detail + Environment.NewLine + "Códigos CGH para SLUS-21134. Os efeitos ainda não foram testados em emulador.";
            cheatsGrid.Rows.Clear();
            foreach (SlusCheatStatus option in inspection.Options)
            {
                string state = option.State switch
                {
                    SlusCheatState.Ready => "Disponível", SlusCheatState.Applied => "Instalada",
                    SlusCheatState.Unavailable => "Runtime pendente", _ => "Incompatível"
                };
                int index = cheatsGrid.Rows.Add(option.State == SlusCheatState.Applied, option.Name, option.Type, state);
                var row = cheatsGrid.Rows[index];
                row.Tag = option;
                row.Cells[0].ReadOnly = option.State is SlusCheatState.Incompatible or SlusCheatState.Unavailable;
                if (row.Cells[0].ReadOnly) row.DefaultCellStyle.ForeColor = Muted;
                foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = option.Detail;
            }
        }
        finally { refreshingCheats = false; }
        UpdateCheatButtons();
    }

    private void UpdateCheatButtons()
    {
        if (cheatsGrid == null || applyCheats == null || removeCheats == null) return;
        bool pending = cheatsGrid.Rows.Cast<DataGridViewRow>().Any(row => row.Tag is SlusCheatStatus option &&
            (row.Cells[0].Value is true) != (option.State == SlusCheatState.Applied));
        bool installed = cheatsGrid.Rows.Cast<DataGridViewRow>().Any(row => row.Tag is SlusCheatStatus { State: SlusCheatState.Applied });
        applyCheats.Enabled = pending;
        removeCheats.Enabled = installed;
    }

    private void ChangeCheats(bool removeAll)
    {
        if (doc == null || cheatsGrid == null) return;
        int[] ids = removeAll ? Array.Empty<int>() : cheatsGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => row.Cells[0].Value is true).Select(row => ((SlusCheatStatus)row.Tag!).Id).ToArray();
        byte[] snapshot = doc.ReadBytes(0, doc.Length);
        try
        {
            foreach (var grid in grids.Values) grid.EndEdit();
            CommitCapacity(); CommitFloat("PODER DE FOGO", false); CommitFloat("RECARGA", true);
            CommitFloat("DISPARO", true); CommitUpgrades(); CommitLimits();
            SlusCheatPatches.SetSelection(doc, ids);
            BuildTabs();
            tabs.SelectedTab = tabs.TabPages.Cast<TabPage>().Single(page => page.Text == "TRAPAÇAS");
            save.Enabled = revert.Enabled = doc.IsModified;
            status.Text = ids.Length == 0 ? "Remoção preparada. Salve para gravar." : $"{ids.Length} trapaça(s) preparada(s). Salve para gravar.";
        }
        catch (Exception ex)
        {
            doc.WriteBytes(0, snapshot);
            MessageBox.Show(ex.Message, "Trapaças do executável", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
