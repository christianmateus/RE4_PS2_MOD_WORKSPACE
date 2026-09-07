using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class CharacterCustomizerForm : Form
{
    private const byte PreviewType = 0;
    private readonly ScenarioViewport viewport = new() { Dock = DockStyle.Fill, ScenarioVisible = true, AevVisible = false, EnemiesVisible = true, ObjectsVisible = false, ShowEnemyLabels = false, EnemyModelPartPickingEnabled = true };
    private readonly ComboBox cmbDat = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, FlatStyle = FlatStyle.Flat };
    private readonly CheckedListBox lstParts = new() { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(22, 25, 30), ForeColor = Color.Gainsboro, Font = new Font("Consolas", 8.5f) };
    private readonly ListView lstTextures = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = true, BackColor = Color.FromArgb(22, 25, 30), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly ComboBox cmbMeshDonor = new() { Dock = DockStyle.Top, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, FlatStyle = FlatStyle.Flat };
    private readonly ComboBox cmbRigMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, FlatStyle = FlatStyle.Flat };
    private readonly NumericUpDown moveX = TransformNumber(), moveY = TransformNumber(), moveZ = TransformNumber();
    private readonly NumericUpDown rotateX = TransformNumber(), rotateY = TransformNumber(), rotateZ = TransformNumber();
    private readonly NumericUpDown scaleX = TransformNumber(1), scaleY = TransformNumber(1), scaleZ = TransformNumber(1);
    private readonly ListView lstBones = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, BackColor = Color.FromArgb(22, 25, 30), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly CheckBox chkSkeleton = new() { Text = "MOSTRAR ESQUELETO", AutoSize = true, ForeColor = Color.Gainsboro };
    private readonly Label lblBoneWarning = new() { Dock = DockStyle.Bottom, Height = 42, ForeColor = Color.FromArgb(240, 185, 75), Padding = new Padding(6) };
    private readonly CheckBox chkFaceEdit = new() { Text = "SELECIONAR FACES", AutoSize = true, ForeColor = Color.Gainsboro };
    private readonly Label lblFaceSelection = new() { Text = "0 face(s)", Width = 72, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(155, 162, 174) };
    private readonly HashSet<int> selectedFaceFlags = new();
    private readonly HashSet<int> selectedFaceVertices = new();
    private int selectedFaceBin = -1;
    private string? externalConverterPath;
    private readonly Action<string>? externalConverterSelected;
    private readonly ListView lstMeshLibrary = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, BackColor = Color.FromArgb(22, 25, 30), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.None };
    private readonly PictureBox preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(16, 18, 22) };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 30, ForeColor = Color.FromArgb(155, 162, 174), Padding = new Padding(10, 7, 0, 0) };
    private EnemyModelScene? model;
    private EnemyModelScene? originalModel;
    private bool showingOriginal;
    private readonly TabControl rightTabs = new() { Dock = DockStyle.Fill };
    private TabPage? texturesTab;
    private readonly Button btnCompare = new();
    private readonly Button btnUndo = new();
    private readonly Button btnRedo = new();
    private readonly ComboBox cmbCurrentMap = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 92, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, FlatStyle = FlatStyle.Flat };
    private readonly ComboBox cmbNewMap = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 92, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, FlatStyle = FlatStyle.Flat };
    private readonly Label lblMaterialMap = new() { Dock = DockStyle.Top, Height = 26, Padding = new Padding(7, 6, 0, 0), ForeColor = Color.FromArgb(155, 162, 174), Text = "MATERIAL • selecione uma parte" };
    private EnemyModelPart? selectedPart;
    private readonly Stack<TextureEdit> undoHistory = new();
    private readonly Stack<TextureEdit> redoHistory = new();
    private readonly Dictionary<int, ManualAssignment> manualAssignments = new();
    private string? datPath;
    private bool syncingParts;
    private bool syncingDatList;
    private readonly string? initialPath;
    private readonly Action<string>? datOpened;
    private readonly string[] extractedRoots;
    private readonly ScenarioCameraState? initialCamera;
    private readonly Action<ScenarioCameraState>? cameraSaved;
    private bool cameraInitialized;
    private static readonly Regex SupportedDatName = new("^(?:pl|em)[0-9]{2}\\.dat$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private sealed record ManualAssignment(int TplEntry, int TextureIndex);
    private sealed record TextureEdit(string DatPath, int TplEntry, int TextureIndex, byte[] Before, byte[] After, string Description, bool IsBinMaterial = false, int SelectionTplEntry = -1, int BinIndex = -1, ManualAssignment? BeforeAssignment = null, ManualAssignment? AfterAssignment = null);

    private sealed record DatItem(string Path)
    {
        public override string ToString()
        {
            DirectoryInfo? parent = Directory.GetParent(Path);
            return parent == null ? System.IO.Path.GetFileName(Path) : $"{System.IO.Path.GetFileName(Path)}  •  {parent.Name}";
        }
    }

    private sealed record TextureItem(int TplEntry, int Index, int Width, int Height, string Format)
    {
        public override string ToString() => $"TPL #{TplEntry:D3} / textura {Index:D2}";
    }
    private sealed record MeshLibraryItem(string DatPath, EnemyModelScene DonorModel, EnemyModelPart Part)
    {
        public override string ToString() => $"BIN {Part.BinIndex:D2} • DAT #{Part.DatEntryIndex:D3}";
    }
    private sealed record PartItem(EnemyModelPart Part, ManualAssignment? Assignment)
    {
        public override string ToString()
        {
            int tplIndex = Assignment?.TplEntry ?? Part.TplEntryIndex;
            string tpl = tplIndex < 0 ? "TPL --" : $"TPL #{tplIndex:D3}";
            string maps = Assignment != null ? Assignment.TextureIndex.ToString() : Part.DiffuseMaps.Count == 0 ? "--" : string.Join(",", Part.DiffuseMaps);
            int vertices = Part.Triangles.SelectMany(triangle => new[] { triangle.SourceVertexA, triangle.SourceVertexB, triangle.SourceVertexC }).Where(x => x >= 0).Distinct().Count();
            return $"BIN {Part.BinIndex:D2} • DAT #{Part.DatEntryIndex:D3} • {tpl} • tex [{maps}] • V {vertices:N0} • F {Part.Triangles.Count:N0}{(Assignment != null ? " • MANUAL" : "")}{(Part.Triangles.Count == 0 ? " • REMOVIDO" : "")}";
        }
    }

    public CharacterCustomizerForm(string? initialPath = null, Action<string>? datOpened = null, IEnumerable<string>? extractedRoots = null, ScenarioCameraState? initialCamera = null, Action<ScenarioCameraState>? cameraSaved = null, string? externalConverterPath = null, Action<string>? externalConverterSelected = null)
    {
        this.initialPath = initialPath;
        this.datOpened = datOpened;
        this.extractedRoots = (extractedRoots ?? new[] { Path.Combine(Application.StartupPath, "Extracted") }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        this.initialCamera = initialCamera;
        this.cameraSaved = cameraSaved;
        this.externalConverterPath = externalConverterPath;
        this.externalConverterSelected = externalConverterSelected;
        Text = "Customização de Personagem • RE4 PS2";
        Width = 1320; Height = 820; MinimumSize = new Size(980, 640); StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(13, 15, 18); ForeColor = Color.Gainsboro; Font = new Font("Segoe UI", 9f);
        BuildUi();
        Shown += (_, _) => InitializeDatSelection();
    }

    private void BuildUi()
    {
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), ColumnCount = 5, BackColor = Color.FromArgb(22, 25, 30) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 1; i < 5; i++) top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        cmbDat.SelectedIndexChanged += (_, _) => { if (!syncingDatList && cmbDat.SelectedItem is DatItem item) LoadDat(item.Path); };
        top.Controls.Add(cmbDat, 0, 0);
        top.Controls.Add(Button("ATUALIZAR", (_, _) => RefreshDatDropdown()), 1, 0);
        top.Controls.Add(Button("ABRIR DAT", (_, _) => BrowseDat()), 2, 0);
        top.Controls.Add(Button("ENQUADRAR", (_, _) => viewport.FitScene()), 3, 0);
        top.Controls.Add(Button("TODAS", (_, _) => ShowAllParts()), 4, 0);

        lstTextures.Columns.Add("Textura", 170); lstTextures.Columns.Add("Dimensões", 85); lstTextures.Columns.Add("Formato", 70);
        lstTextures.SelectedIndexChanged += (_, _) => ShowSelectedTexture();
        lstTextures.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            ListViewItem? row = lstTextures.GetItemAt(e.X, e.Y);
            if (row != null && !row.Selected) { foreach (ListViewItem other in lstTextures.Items) other.Selected = false; row.Selected = true; row.Focused = true; }
        };
        lstTextures.ItemDrag += (_, e) =>
        {
            if (e.Item is ListViewItem row && row.Tag is TextureItem item) lstTextures.DoDragDrop(item, DragDropEffects.Copy);
        };
        lstParts.ItemCheck += PartsItemCheck;
        lstParts.SelectedIndexChanged += (_, _) =>
        {
            if (lstParts.SelectedItem is PartItem item) { selectedPart = item.Part; viewport.HighlightEnemyModelPart(PreviewType, item.Part.BinIndex); PopulateMaterialMapping(item.Part); PopulateBoneDiagnostics(item.Part); }
        };
        lstParts.MouseDown += (_, e) => { if (e.Button == MouseButtons.Right) { int index = lstParts.IndexFromPoint(e.Location); if (index >= 0) lstParts.SelectedIndex = index; } };
        var partsMenu = new ContextMenuStrip { BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro };
        var hidePart = new ToolStripMenuItem("Ocultar somente no viewer");
        hidePart.Click += (_, _) => ToggleSelectedPartVisibility();
        var removePart = new ToolStripMenuItem("Remover do modelo (jogo)...");
        removePart.Click += (_, _) => RemoveSelectedPartFromModel();
        var restorePart = new ToolStripMenuItem("Restaurar BIN original");
        restorePart.Click += (_, _) => RestoreSelectedPartFromBackup();
        partsMenu.Items.Add(hidePart); partsMenu.Items.Add(new ToolStripSeparator()); partsMenu.Items.Add(removePart); partsMenu.Items.Add(restorePart);
        partsMenu.Opening += (_, _) =>
        {
            bool selected = lstParts.SelectedItem is PartItem;
            hidePart.Enabled = selected; removePart.Enabled = selected && selectedPart?.Triangles.Count > 0;
            restorePart.Enabled = selected && datPath != null && File.Exists(datPath + ".bak");
            if (selected && lstParts.SelectedIndex >= 0) hidePart.Text = lstParts.GetItemChecked(lstParts.SelectedIndex) ? "Ocultar somente no viewer" : "Mostrar no viewer";
        };
        lstParts.ContextMenuStrip = partsMenu;
        viewport.EnemyModelPartClicked += SelectPartFromViewport;
        viewport.EnemyModelFaceClicked += SelectFaceFromViewport;
        viewport.EnemyModelFaceTranslationRequested += MoveSelectedFacesWithGizmo;
        viewport.EnemyModelPartTranslationRequested += MoveSelectedMeshWithGizmo;
        viewport.AllowDrop = true;
        viewport.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(typeof(TextureItem)) == true || e.Data?.GetDataPresent(typeof(MeshLibraryItem)) == true ? DragDropEffects.Copy : DragDropEffects.None;
        viewport.DragDrop += ViewportAssetDragDrop;
        var textureMenu = new ContextMenuStrip { BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro };
        ToolStripItem AddTextureAction(string label, EventHandler action)
        {
            var menuItem = new ToolStripMenuItem(label); menuItem.Click += action; textureMenu.Items.Add(menuItem); return menuItem;
        }
        AddTextureAction("Substituir por PNG...", (_, _) => ImportPng());
        AddTextureAction("Exportar para PNG...", (_, _) => ExportSelectedTexture());
        textureMenu.Items.Add(new ToolStripSeparator());
        AddTextureAction("Redimensionar...", (_, _) => ResizeSelectedTexture());
        var depthMenu = new ToolStripMenuItem("Profundidade de cor");
        var depth4 = new ToolStripMenuItem("Converter para 4-bit (16 cores)"); depth4.Click += (_, _) => ConvertSelectedTextureBitDepth(16);
        var depth8 = new ToolStripMenuItem("Converter para 8-bit (256 cores)"); depth8.Click += (_, _) => ConvertSelectedTextureBitDepth(256);
        depthMenu.DropDownItems.Add(depth4); depthMenu.DropDownItems.Add(depth8); textureMenu.Items.Add(depthMenu);
        textureMenu.Items.Add(new ToolStripSeparator());
        AddTextureAction("Rotacionar 90°", (_, _) => TransformSelectedTexture(Ps2CharacterDatEditor.TextureTransform.Rotate90));
        AddTextureAction("Inverter X", (_, _) => TransformSelectedTexture(Ps2CharacterDatEditor.TextureTransform.FlipX));
        AddTextureAction("Inverter Y", (_, _) => TransformSelectedTexture(Ps2CharacterDatEditor.TextureTransform.FlipY));
        textureMenu.Items.Add(new ToolStripSeparator());
        AddTextureAction("Restaurar textura original", (_, _) => RestoreSelectedTexture());
        textureMenu.Opening += (_, _) =>
        {
            TextureItem? selected = lstTextures.SelectedItems.Count > 0 ? lstTextures.SelectedItems[0].Tag as TextureItem : null;
            bool hasSelection = selected != null;
            foreach (ToolStripItem menuItem in textureMenu.Items) if (menuItem is not ToolStripSeparator) menuItem.Enabled = hasSelection;
            depth4.Enabled = selected?.Format == "8-bit";
            depth8.Enabled = selected?.Format == "4-bit";
        };
        lstTextures.ContextMenuStrip = textureMenu;

        var partsTab = new TabPage("PARTES / BIN") { BackColor = Color.FromArgb(22, 25, 30) };
        var materialPanel = new Panel { Dock = DockStyle.Bottom, Height = 96, BackColor = Color.FromArgb(28, 31, 37), Padding = new Padding(5) };
        var materialRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(2, 3, 2, 2), WrapContents = false };
        materialRow.Controls.Add(new Label { Text = "DE", Width = 24, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Silver });
        materialRow.Controls.Add(cmbCurrentMap);
        materialRow.Controls.Add(new Label { Text = "PARA", Width = 38, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Silver });
        materialRow.Controls.Add(cmbNewMap);
        materialRow.Controls.Add(TextureButton("APLICAR", (_, _) => ApplyMaterialTextureIndex(), 72));
        materialPanel.Controls.Add(materialRow); materialPanel.Controls.Add(lblMaterialMap);
        partsTab.Controls.Add(lstParts); partsTab.Controls.Add(materialPanel);
        texturesTab = new TabPage("TEXTURAS") { BackColor = Color.FromArgb(22, 25, 30) };
        var textureSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 330 };
        var historyTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4), WrapContents = false, BackColor = Color.FromArgb(24, 27, 32) };
        ConfigureTextureButton(btnCompare, "VER ORIGINAL", (_, _) => ToggleOriginal(), 104);
        ConfigureTextureButton(btnUndo, "DESFAZER", (_, _) => UndoTextureEdit(), 80);
        ConfigureTextureButton(btnRedo, "REFAZER", (_, _) => RedoTextureEdit(), 76);
        historyTools.Controls.Add(btnCompare); historyTools.Controls.Add(btnUndo); historyTools.Controls.Add(btnRedo);
        historyTools.Controls.Add(TextureButton("RESTAURAR", (_, _) => RestoreSelectedTexture(), 88));
        textureSplit.Panel1.Controls.Add(lstTextures); textureSplit.Panel1.Controls.Add(historyTools); textureSplit.Panel2.Controls.Add(preview); texturesTab.Controls.Add(textureSplit);
        var meshLibraryTab = new TabPage("MESHES DO JOGO") { BackColor = Color.FromArgb(22, 25, 30), Padding = new Padding(5) };
        lstMeshLibrary.Columns.Add("Mesh", 150); lstMeshLibrary.Columns.Add("Triângulos", 76); lstMeshLibrary.Columns.Add("Tamanho", 130);
        lstMeshLibrary.ItemDrag += (_, e) => { if (e.Item is ListViewItem row && row.Tag is MeshLibraryItem item) lstMeshLibrary.DoDragDrop(item, DragDropEffects.Copy); };
        cmbMeshDonor.SelectedIndexChanged += (_, _) => LoadMeshDonor();
        var rigModePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, WrapContents = true, Padding = new Padding(2, 4, 2, 2) };
        rigModePanel.Controls.Add(new Label { Text = "ENCAIXE", Width = 64, Height = 27, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Silver });
        cmbRigMode.Items.AddRange(new object[] { "Automático (recomendado)", "Rígido / acessório", "Distribuído / roupas", "Manter rig do doador" }); cmbRigMode.SelectedIndex = 0;
        rigModePanel.Controls.Add(cmbRigMode);
        rigModePanel.Controls.Add(TextureButton("IMPORTAR MODELO...", async (_, _) => await ImportExternalModelAsync(), 132));
        meshLibraryTab.Controls.Add(lstMeshLibrary); meshLibraryTab.Controls.Add(new Label { Text = "Arraste um BIN e solte sobre a malha que deseja substituir", Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(5, 8, 0, 0), ForeColor = Color.FromArgb(155, 162, 174) }); meshLibraryTab.Controls.Add(rigModePanel); meshLibraryTab.Controls.Add(cmbMeshDonor);

        TabPage rigTab = BuildRigAndDiagnosticsTab();
        rightTabs.TabPages.Add(partsTab); rightTabs.TabPages.Add(texturesTab); rightTabs.TabPages.Add(meshLibraryTab); rightTabs.TabPages.Add(rigTab);
        viewport.ExternalUndoRequested += UndoTextureEdit;
        viewport.ExternalRedoRequested += RedoTextureEdit;
        UpdateHistoryButtons();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 5,
            FixedPanel = FixedPanel.Panel2
        };
        split.Panel1.Controls.Add(viewport); split.Panel2.Controls.Add(rightTabs);
        void ApplyReadablePanelWidth()
        {
            int width = split.ClientSize.Width;
            if (width < 300) return;
            int rightWidth = Math.Clamp(380, 180, Math.Max(180, width / 2));
            int desired = Math.Max(100, width - rightWidth - split.SplitterWidth);
            int maximum = Math.Max(100, width - 105 - split.SplitterWidth);
            split.SplitterDistance = Math.Min(desired, maximum);
        }
        split.HandleCreated += (_, _) => BeginInvoke(ApplyReadablePanelWidth);
        Controls.Add(split); Controls.Add(status); Controls.Add(top);
    }

    private static Button Button(string text, EventHandler click)
    {
        var b = new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(42, 47, 56), ForeColor = Color.White, Margin = new Padding(4, 0, 0, 0) };
        b.FlatAppearance.BorderSize = 0; b.Click += click; return b;
    }

    private static NumericUpDown TransformNumber(decimal value = 0)
    {
        return new NumericUpDown { Width = 68, Height = 26, DecimalPlaces = 2, Increment = 0.1m, Minimum = -1000, Maximum = 1000, Value = value, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.FixedSingle };
    }

    private TabPage BuildRigAndDiagnosticsTab()
    {
        var tab = new TabPage("AJUSTES / OSSOS") { BackColor = Color.FromArgb(22, 25, 30), Padding = new Padding(6) };
        var transform = new GroupBox { Text = "TRANSFORMAR MESH SELECIONADO", Dock = DockStyle.Top, Height = 210, ForeColor = Color.Gainsboro, Padding = new Padding(8) };
        var rows = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        FlowLayoutPanel AxisRow(string label, NumericUpDown x, NumericUpDown y, NumericUpDown z)
        {
            var row = new FlowLayoutPanel { Width = 340, Height = 32, WrapContents = false };
            row.Controls.Add(new Label { Text = label, Width = 72, Height = 26, TextAlign = ContentAlignment.MiddleLeft });
            foreach (Control control in new Control[] { new Label { Text = "X", Width = 14, Height = 26, TextAlign = ContentAlignment.MiddleCenter }, x, new Label { Text = "Y", Width = 14, Height = 26, TextAlign = ContentAlignment.MiddleCenter }, y, new Label { Text = "Z", Width = 14, Height = 26, TextAlign = ContentAlignment.MiddleCenter }, z }) row.Controls.Add(control);
            return row;
        }
        rows.Controls.Add(AxisRow("POSIÇÃO", moveX, moveY, moveZ));
        rows.Controls.Add(AxisRow("ROTAÇÃO", rotateX, rotateY, rotateZ));
        rows.Controls.Add(AxisRow("ESCALA", scaleX, scaleY, scaleZ));
        var actions = new FlowLayoutPanel { Width = 350, Height = 36, WrapContents = false };
        actions.Controls.Add(TextureButton("APLICAR", (_, _) => ApplySelectedMeshTransform(), 86));
        actions.Controls.Add(TextureButton("CENTRALIZAR", (_, _) => CenterSelectedMesh(), 102));
        actions.Controls.Add(TextureButton("ZERAR", (_, _) => ResetTransformControls(), 72));
        rows.Controls.Add(actions); transform.Controls.Add(rows);
        var faceActions = new FlowLayoutPanel { Width = 350, Height = 32, WrapContents = false };
        chkFaceEdit.CheckedChanged += (_, _) => ToggleFaceEditMode();
        faceActions.Controls.Add(chkFaceEdit); faceActions.Controls.Add(lblFaceSelection);
        faceActions.Controls.Add(TextureButton("EXCLUIR", (_, _) => DeleteSelectedFaces(), 78));
        rows.Controls.Add(faceActions);

        var diagnostics = new GroupBox { Text = "DIAGNÓSTICO DE OSSOS E PESOS", Dock = DockStyle.Fill, ForeColor = Color.Gainsboro, Padding = new Padding(8) };
        lstBones.Columns.Add("Osso", 62); lstBones.Columns.Add("Vértices", 70); lstBones.Columns.Add("Peso máx.", 78); lstBones.Columns.Add("Diagnóstico", 105);
        lstBones.SelectedIndexChanged += (_, _) => UpdateBoneDiagnosticViewport();
        chkSkeleton.CheckedChanged += (_, _) => UpdateBoneDiagnosticViewport();
        var diagTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56, WrapContents = true };
        diagTools.Controls.Add(chkSkeleton);
        diagTools.Controls.Add(TextureButton("LIMPAR OSSO", (_, _) => { lstBones.SelectedItems.Clear(); UpdateBoneDiagnosticViewport(); }, 102));
        diagTools.Controls.Add(new Label { Text = "PESOS: azul baixo • amarelo médio • vermelho alto", Width = 330, Height = 20, ForeColor = Color.FromArgb(155, 162, 174) });
        diagnostics.Controls.Add(lstBones); diagnostics.Controls.Add(lblBoneWarning); diagnostics.Controls.Add(diagTools);
        tab.Controls.Add(diagnostics); tab.Controls.Add(transform);
        return tab;
    }

    private static Button TextureButton(string text, EventHandler click, int width)
    {
        Button button = Button(text, click); button.Dock = DockStyle.None; button.Width = width; button.Height = 28; button.Margin = new Padding(2, 1, 2, 1); return button;
    }

    private static void ConfigureTextureButton(Button button, string text, EventHandler click, int width)
    {
        button.Text = text; button.Dock = DockStyle.None; button.Width = width; button.Height = 28; button.Margin = new Padding(2, 1, 2, 1);
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = Color.FromArgb(42, 47, 56); button.ForeColor = Color.White; button.Click += click;
    }

    private void BrowseDat()
    {
        using var dialog = new OpenFileDialog { Filter = "RE4 character/enemy DAT (*.dat)|*.dat|Todos os arquivos (*.*)|*.*", Title = "Abrir personagem ou inimigo" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!SupportedDatName.IsMatch(Path.GetFileName(dialog.FileName)))
        { MessageBox.Show(this, "O nome precisa seguir o padrão plXX.dat ou emXX.dat, usando dois números.", "DAT não suportado", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        LoadDat(dialog.FileName); RefreshDatList(dialog.FileName);
    }

    private void InitializeDatSelection()
    {
        string? preferred = !string.IsNullOrWhiteSpace(initialPath) && File.Exists(initialPath) && SupportedDatName.IsMatch(Path.GetFileName(initialPath)) && !IsInsideOriginalDat(initialPath)
            ? initialPath : FindDefaultPl00();
        RefreshDatList(preferred);
        if (preferred != null && File.Exists(preferred)) LoadDat(preferred);
        else if (cmbDat.Items.Count > 0) cmbDat.SelectedIndex = 0;
        else status.Text = "Nenhum plXX.dat ou emXX.dat extraído foi encontrado.";
    }

    private void RefreshDatDropdown()
    {
        string? selectedPath = datPath ?? (cmbDat.SelectedItem as DatItem)?.Path;
        RefreshDatList(selectedPath);
        status.Text = cmbDat.Items.Count == 0
            ? "Nenhum plXX.dat ou emXX.dat extraído foi encontrado."
            : $"Lista atualizada • {cmbDat.Items.Count} personagem(ns)/inimigo(s) encontrado(s).";
    }

    private void RefreshDatList(string? selectPath = null)
    {
        if (!string.IsNullOrWhiteSpace(selectPath) && IsInsideOriginalDat(selectPath))
            selectPath = FindWorkingCopy(selectPath);
        var discoveredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in extractedRoots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (string path in Directory.EnumerateFiles(root, "*.dat", SearchOption.AllDirectories))
                    if (SupportedDatName.IsMatch(Path.GetFileName(path)) && !IsInsideOriginalDat(path)) discoveredPaths.Add(Path.GetFullPath(path));
            }
            catch { }
        }
        if (!string.IsNullOrWhiteSpace(selectPath) && File.Exists(selectPath) && SupportedDatName.IsMatch(Path.GetFileName(selectPath)) && !IsInsideOriginalDat(selectPath)) discoveredPaths.Add(Path.GetFullPath(selectPath));
        int PathPriority(string path)
        {
            string file = Path.GetFileName(path);
            string parent = Directory.GetParent(path)?.Name ?? "";
            if (file.StartsWith("em", StringComparison.OrdinalIgnoreCase) && parent.Equals("Enemies", StringComparison.OrdinalIgnoreCase)) return 0;
            if (parent.Equals("Content", StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }
        string[] paths = discoveredPaths.GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(PathPriority).ThenBy(x => x, StringComparer.OrdinalIgnoreCase).First())
            .ToArray();
        syncingDatList = true;
        cmbDat.Items.Clear();
        foreach (string path in paths.OrderBy(x => Path.GetFileName(x), StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.OrdinalIgnoreCase)) cmbDat.Items.Add(new DatItem(path));
        if (!string.IsNullOrWhiteSpace(selectPath))
            for (int i = 0; i < cmbDat.Items.Count; i++) if (cmbDat.Items[i] is DatItem item && string.Equals(item.Path, selectPath, StringComparison.OrdinalIgnoreCase)) { cmbDat.SelectedIndex = i; break; }
        syncingDatList = false;
        string? donorPath = (cmbMeshDonor.SelectedItem as DatItem)?.Path;
        cmbMeshDonor.Items.Clear();
        foreach (string path in paths.OrderBy(x => Path.GetFileName(x), StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.OrdinalIgnoreCase)) cmbMeshDonor.Items.Add(new DatItem(path));
        int donorIndex = -1;
        for (int i = 0; i < cmbMeshDonor.Items.Count; i++)
            if (cmbMeshDonor.Items[i] is DatItem donor && string.Equals(donor.Path, donorPath, StringComparison.OrdinalIgnoreCase)) { donorIndex = i; break; }
        if (donorIndex < 0)
            for (int i = 0; i < cmbMeshDonor.Items.Count; i++)
                if (cmbMeshDonor.Items[i] is DatItem donor && !string.Equals(donor.Path, selectPath, StringComparison.OrdinalIgnoreCase)) { donorIndex = i; break; }
        if (donorIndex >= 0) cmbMeshDonor.SelectedIndex = donorIndex;
    }

    private static bool IsInsideOriginalDat(string path)
    {
        for (DirectoryInfo? directory = new FileInfo(path).Directory; directory != null; directory = directory.Parent)
            if (directory.Name.Equals("OriginalDAT", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string? FindWorkingCopy(string originalPath)
    {
        DirectoryInfo? originalDirectory = new FileInfo(originalPath).Directory;
        string? packageRoot = originalDirectory?.Parent?.FullName;
        if (packageRoot == null) return null;
        string contentCopy = Path.Combine(packageRoot, "Content", Path.GetFileName(originalPath));
        return File.Exists(contentCopy) ? contentCopy : null;
    }

    private void LoadMeshDonor()
    {
        lstMeshLibrary.Items.Clear();
        if (cmbMeshDonor.SelectedItem is not DatItem donor || !File.Exists(donor.Path)) return;
        try
        {
            EnemyModelScene donorModel = Ps2EnemyDatReader.Read(donor.Path, PreviewType);
            foreach (EnemyModelPart part in donorModel.Parts.OrderBy(x => x.BinIndex))
            {
                var item = new MeshLibraryItem(donor.Path, donorModel, part);
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
            ScenarioCameraState? currentCamera = cameraInitialized ? viewport.GetCameraState() : null;
            bool changedDat = !string.Equals(datPath, path, StringComparison.OrdinalIgnoreCase);
            if (changedDat) { undoHistory.Clear(); redoHistory.Clear(); showingOriginal = false; }
            model = Ps2EnemyDatReader.Read(path, PreviewType); datPath = path;
            LoadManualAssignments(path);
            string backup = path + ".bak";
            originalModel = Ps2EnemyDatReader.Read(File.Exists(backup) ? backup : path, PreviewType);
            var enemy = new EslEnemyEntry { Index = 0, Active = 1, EnemyType = PreviewType };
            viewport.SetScene(new ScenarioScene { SourcePath = path, BoundsMin = model.BoundsMin, BoundsMax = model.BoundsMax });
            viewport.SetEnemyModels(new Dictionary<byte, EnemyModelScene> { [PreviewType] = showingOriginal ? originalModel : model });
            viewport.SetEnemyTextureAssignments(PreviewType, showingOriginal ? null : manualAssignments.ToDictionary(x => x.Key, x => (x.Value.TplEntry, x.Value.TextureIndex)));
            viewport.SetEslScene(new EslScene(path, new List<EslEnemyEntry> { enemy }));
            viewport.ShowAllEnemyModelParts(PreviewType); viewport.FitScene();
            if (!cameraInitialized && initialCamera.HasValue) viewport.SetCameraState(initialCamera.Value);
            else if (currentCamera.HasValue) viewport.SetCameraState(currentCamera.Value);
            cameraInitialized = true;
            PopulateParts(); PopulateTextures();
            UpdateCompareButton(); UpdateHistoryButtons();
            status.Text = $"{Path.GetFileName(path)} • {model.LoadedBinCount}/{model.BinCount} BINs • {model.TexturePackages.Count} TPLs • {model.Triangles.Count:N0} triângulos";
            datOpened?.Invoke(path);
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
            if (cmbDat.Items[i] is DatItem item && string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)) { cmbDat.SelectedIndex = i; syncingDatList = false; return; }
        syncingDatList = false;
    }

    private void PopulateParts()
    {
        syncingParts = true; lstParts.Items.Clear(); selectedPart = null; cmbCurrentMap.Items.Clear(); cmbNewMap.Items.Clear(); lstBones.Items.Clear(); lblBoneWarning.Text = "Selecione uma parte para analisar."; viewport.SetEnemyBoneDiagnostic(chkSkeleton.Checked, null); lblMaterialMap.Text = "MATERIAL • selecione uma parte";
        if (model != null) foreach (EnemyModelPart p in model.Parts.OrderBy(x => x.BinIndex))
            lstParts.Items.Add(new PartItem(p, manualAssignments.GetValueOrDefault(p.BinIndex)), true);
        syncingParts = false;
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
                    var item = new TextureItem(package.DatEntryIndex, i, tpl.width, tpl.height, tpl.bitDepth == 8 ? "4-bit" : tpl.bitDepth == 9 ? "8-bit" : tpl.bitDepth == 6 ? "32-bit" : $"0x{tpl.bitDepth:X}");
                    var row = new ListViewItem(item.ToString()) { Tag = item }; row.SubItems.Add($"{item.Width}×{item.Height}"); row.SubItems.Add(item.Format); lstTextures.Items.Add(row);
                }
            }
            catch { }
        }
        if (lstTextures.Items.Count > 0) lstTextures.Items[0].Selected = true;
    }

    private void PartsItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (syncingParts || e.Index < 0 || lstParts.Items[e.Index] is not PartItem item) return;
        BeginInvoke(() => viewport.SetEnemyModelPartVisible(PreviewType, item.Part.BinIndex, e.NewValue == CheckState.Checked));
    }

    private void ShowAllParts()
    {
        syncingParts = true; for (int i = 0; i < lstParts.Items.Count; i++) lstParts.SetItemChecked(i, true); syncingParts = false;
        viewport.ShowAllEnemyModelParts(PreviewType);
    }

    private void ToggleSelectedPartVisibility()
    {
        int index = lstParts.SelectedIndex;
        if (index < 0) return;
        lstParts.SetItemChecked(index, !lstParts.GetItemChecked(index));
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
        for (int i = 0; i < lstParts.Items.Count; i++)
        {
            if (lstParts.Items[i] is not PartItem item || item.Part.BinIndex != part.BinIndex) continue;
            lstParts.SelectedIndex = i;
            lstParts.TopIndex = Math.Max(0, i - 2);
            status.Text = $"Selecionado: BIN {part.BinIndex:D2} • entrada DAT #{part.DatEntryIndex:D3} • TPL {(part.TplEntryIndex < 0 ? "--" : $"#{part.TplEntryIndex:D3}")} • arraste o gizmo para mover o mesh";
            SelectTexturesForPart(part);
            break;
        }
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
        if (modelDialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ExternalModelStats stats = ExternalPs2ModelConverter.Analyze(modelDialog.FileName);
            string sizeWarning = stats.IsLarge
                ? $"\n\n⚠ MODELO GRANDE: o limite recomendado é {ExternalModelStats.SafeVertexLimit:N0} vértices e {ExternalModelStats.SafeFaceLimit:N0} faces. Modelos acima disso podem falhar na conversão, exceder segmentos do PS2 ou causar travamentos no jogo."
                : "\n\nO modelo está dentro do tamanho recomendado para personagens do PS2.";
            DialogResult confirmation = MessageBox.Show(this,
                $"Arquivo: {Path.GetFileName(modelDialog.FileName)}\nVértices: {stats.Vertices:N0}\nFaces trianguladas: {stats.Faces:N0}\nDestino: BIN {selectedPart.BinIndex:D2}\nEncaixe: {cmbRigMode.SelectedItem}" + sizeWarning +
                "\n\nA geometria será convertida para BIN e vinculada ao esqueleto do receptor. Texturas PNG devem ser importadas separadamente na aba Texturas. Deseja continuar?",
                stats.IsLarge ? "Confirmar modelo grande" : "Importar modelo externo", MessageBoxButtons.YesNo,
                stats.IsLarge ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
            if (confirmation != DialogResult.Yes) return;
            string? converter = ResolveExternalConverterPath();
            if (converter == null) return;

            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            byte[] receiverTemplate = before;
            if (File.Exists(datPath + ".bak")) try { receiverTemplate = Ps2CharacterDatEditor.CaptureDatEntry(datPath + ".bak", entry); } catch { }
            Cursor = Cursors.WaitCursor; status.Text = $"Convertendo {Path.GetFileName(modelDialog.FileName)} para BIN PS2...";
            bool autoWeightedObj = Path.GetExtension(modelDialog.FileName).Equals(".obj", StringComparison.OrdinalIgnoreCase);
            string generated = await ExternalPs2ModelConverter.ConvertAsync(converter, modelDialog.FileName, receiverTemplate, autoWeightedObj);
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

    private string? ResolveExternalConverterPath()
    {
        if (!string.IsNullOrWhiteSpace(externalConverterPath) && File.Exists(externalConverterPath)) return externalConverterPath;
        string[] names = { "RE4_PS2_BIN_TOOL.exe", "Re4Ps2BINrepack.exe" };
        foreach (string folder in new[] { Application.StartupPath, Path.Combine(Application.StartupPath, "Tools") })
            foreach (string name in names)
            {
                string candidate = Path.Combine(folder, name);
                if (File.Exists(candidate)) return externalConverterPath = candidate;
            }
        using var dialog = new OpenFileDialog
        {
            Filter = "RE4 PS2 BIN Tool (*.exe)|*.exe",
            Title = "Selecione a RE4 PS2 BIN Tool (necessário somente na primeira vez)"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        externalConverterPath = dialog.FileName;
        externalConverterSelected?.Invoke(dialog.FileName);
        return externalConverterPath;
    }

    private void ApplySelectedMeshTransform()
    {
        if (datPath == null || selectedPart == null) { MessageBox.Show(this, "Selecione uma parte do modelo primeiro."); return; }
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            var translation = new Vector3((float)moveX.Value, (float)moveY.Value, (float)moveZ.Value);
            var rotation = new Vector3((float)rotateX.Value, (float)rotateY.Value, (float)rotateZ.Value);
            var scale = new Vector3((float)scaleX.Value, (float)scaleY.Value, (float)scaleZ.Value);
            if (chkFaceEdit.Checked && selectedFaceFlags.Count > 0)
            {
                if (selectedFaceBin != binIndex) throw new InvalidOperationException("As faces selecionadas pertencem a outro BIN.");
                Ps2CharacterDatEditor.TransformBinVertices(datPath, entry, selectedFaceVertices, translation, rotation, scale);
            }
            else Ps2CharacterDatEditor.TransformBinEntry(datPath, entry, translation, rotation, scale);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Transformação do BIN {binIndex:D2}", true));
            ResetTransformControls(); showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = chkFaceEdit.Checked && selectedFaceFlags.Count > 0 ? $"{selectedFaceFlags.Count} face(s) transformada(s) no BIN {binIndex:D2}." : $"Transformação gravada no BIN {binIndex:D2}. Use Desfazer para reverter.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Transformar mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void CenterSelectedMesh()
    {
        if (datPath == null || selectedPart == null) { MessageBox.Show(this, "Selecione uma parte do modelo primeiro."); return; }
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.CenterBinOnOriginal(datPath, entry);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Centralização do BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} centralizado no encaixe original.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Centralizar mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ResetTransformControls()
    {
        moveX.Value = moveY.Value = moveZ.Value = rotateX.Value = rotateY.Value = rotateZ.Value = 0;
        scaleX.Value = scaleY.Value = scaleZ.Value = 1;
    }

    private void ToggleFaceEditMode()
    {
        viewport.EnemyModelFacePickingEnabled = chkFaceEdit.Checked;
        viewport.EnemyModelPartPickingEnabled = !chkFaceEdit.Checked;
        if (!chkFaceEdit.Checked) ClearFaceSelection();
        status.Text = chkFaceEdit.Checked ? "Modo Faces: clique para selecionar; Ctrl+clique adiciona ou remove faces." : "Modo de seleção de partes ativado.";
    }

    private void SelectFaceFromViewport(EnemyModelFaceHit? hit, bool additive)
    {
        if (!chkFaceEdit.Checked) return;
        if (!hit.HasValue) { if (!additive) ClearFaceSelection(); return; }
        EnemyModelFaceHit face = hit.Value;
        if (!additive || selectedFaceBin != face.Part.BinIndex) ClearFaceSelection();
        selectedFaceBin = face.Part.BinIndex;
        bool removing = additive && selectedFaceFlags.Contains(face.Triangle.StripFlagOffset);
        if (removing) selectedFaceFlags.Remove(face.Triangle.StripFlagOffset); else selectedFaceFlags.Add(face.Triangle.StripFlagOffset);
        RebuildSelectedFaceVertices(face.Part);
        selectedPart = face.Part; SelectPartByBinIndex(face.Part.BinIndex);
        viewport.SetSelectedEnemyModelFaces(selectedFaceBin, selectedFaceFlags);
        lblFaceSelection.Text = $"{selectedFaceFlags.Count} face(s)";
        status.Text = $"BIN {selectedFaceBin:D2}: {selectedFaceFlags.Count} face(s) selecionada(s). Ctrl+clique para seleção múltipla.";
    }

    private void RebuildSelectedFaceVertices(EnemyModelPart part)
    {
        selectedFaceVertices.Clear();
        foreach (EnemyModelTriangle triangle in part.Triangles.Where(x => selectedFaceFlags.Contains(x.StripFlagOffset)))
        {
            if (triangle.SourceVertexA >= 0) selectedFaceVertices.Add(triangle.SourceVertexA);
            if (triangle.SourceVertexB >= 0) selectedFaceVertices.Add(triangle.SourceVertexB);
            if (triangle.SourceVertexC >= 0) selectedFaceVertices.Add(triangle.SourceVertexC);
        }
    }

    private void ClearFaceSelection()
    {
        selectedFaceFlags.Clear(); selectedFaceVertices.Clear(); selectedFaceBin = -1; lblFaceSelection.Text = "0 face(s)";
        viewport.SetSelectedEnemyModelFaces(-1, null);
    }

    private void DeleteSelectedFaces()
    {
        if (datPath == null || selectedPart == null || selectedFaceFlags.Count == 0 || selectedFaceBin != selectedPart.BinIndex)
        { MessageBox.Show(this, "Ative Selecionar Faces e escolha uma ou mais faces no modelo."); return; }
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            int removed = Ps2CharacterDatEditor.DeleteBinFaces(datPath, entry, selectedFaceFlags);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Excluir {removed} face(s) do BIN {binIndex:D2}", true));
            ClearFaceSelection(); showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"{removed} face(s) removida(s) do BIN {binIndex:D2}. Use Desfazer para restaurar.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Excluir faces", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void MoveSelectedFacesWithGizmo(Vector3 delta)
    {
        if (datPath == null || selectedPart == null || selectedFaceFlags.Count == 0 || selectedFaceBin != selectedPart.BinIndex) return;
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.TransformBinVertices(datPath, entry, selectedFaceVertices, delta, Vector3.Zero, Vector3.One);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Mover {selectedFaceFlags.Count} face(s) do BIN {binIndex:D2}", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            viewport.SetSelectedEnemyModelFaces(binIndex, selectedFaceFlags);
            status.Text = $"{selectedFaceFlags.Count} face(s) movida(s) pelo gizmo • Δ {delta.X:0.##}, {delta.Y:0.##}, {delta.Z:0.##}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Mover faces", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void MoveSelectedMeshWithGizmo(Vector3 delta)
    {
        if (datPath == null || selectedPart == null || chkFaceEdit.Checked) return;
        try
        {
            int binIndex = selectedPart.BinIndex, entry = selectedPart.DatEntryIndex;
            byte[] before = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            Ps2CharacterDatEditor.TransformBinEntry(datPath, entry, delta, Vector3.Zero, Vector3.One);
            byte[] after = Ps2CharacterDatEditor.CaptureDatEntry(datPath, entry);
            PushTextureEdit(new TextureEdit(datPath, entry, -1, before, after, $"Mover mesh BIN {binIndex:D2} pelo gizmo", true));
            showingOriginal = false; LoadDat(datPath); SelectPartByBinIndex(binIndex);
            status.Text = $"BIN {binIndex:D2} movido pelo gizmo • Δ {delta.X:0.##}, {delta.Y:0.##}, {delta.Z:0.##}. Use Desfazer para reverter.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Mover mesh", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void PopulateBoneDiagnostics(EnemyModelPart part)
    {
        lstBones.BeginUpdate(); lstBones.Items.Clear();
        var influences = new Dictionary<byte, (int Count, float Max)>();
        void Add(EnemyVertexSkin skin)
        {
            foreach (EnemySkinInfluence influence in new[] { skin.A, skin.B, skin.C }.Take(Math.Clamp(skin.Count, 0, 3)))
            {
                if (influence.Weight <= 0) continue;
                influences.TryGetValue(influence.BoneId, out var current);
                influences[influence.BoneId] = (current.Count + 1, Math.Max(current.Max, influence.Weight));
            }
        }
        foreach (EnemyModelTriangle triangle in part.Triangles) { Add(triangle.SkinA); Add(triangle.SkinB); Add(triangle.SkinC); }
        foreach (var pair in influences.OrderBy(x => x.Key))
        {
            bool physicsCandidate = pair.Key >= 64;
            var row = new ListViewItem(pair.Key.ToString()) { Tag = pair.Key, ForeColor = physicsCandidate ? Color.FromArgb(245, 180, 70) : Color.Gainsboro };
            row.SubItems.Add(pair.Value.Count.ToString("N0")); row.SubItems.Add(pair.Value.Max.ToString("P0")); row.SubItems.Add(physicsCandidate ? "secundário/física" : "normal");
            lstBones.Items.Add(row);
        }
        int suspicious = influences.Count(x => x.Key >= 64);
        lblBoneWarning.Text = influences.Count == 0 ? "Esta malha não expõe pesos de skin." : suspicious > 0 ? $"⚠ {suspicious} osso(s) secundário(s) detectado(s). Se a peça balançar, use encaixe Rígido." : $"{influences.Count} osso(s) usados; nenhuma influência secundária suspeita.";
        lstBones.EndUpdate(); UpdateBoneDiagnosticViewport();
    }

    private void UpdateBoneDiagnosticViewport()
    {
        byte? boneId = lstBones.SelectedItems.Count > 0 && lstBones.SelectedItems[0].Tag is byte selected ? selected : null;
        viewport.SetEnemyBoneDiagnostic(chkSkeleton.Checked, boneId);
    }

    private void LoadManualAssignments(string path)
    {
        manualAssignments.Clear();
        string sidecar = path + ".texture-map.json";
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
        string sidecar = path + ".texture-map.json";
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
        for (int i = 0; i < lstParts.Items.Count; i++) if (lstParts.Items[i] is PartItem item && item.Part.BinIndex == binIndex) { lstParts.SelectedIndex = i; lstParts.TopIndex = Math.Max(0, i - 2); return; }
    }

    private void SelectTexturesForPart(EnemyModelPart part)
    {
        if (texturesTab != null) rightTabs.SelectedTab = texturesTab;
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

    private void ShowSelectedTexture()
    {
        preview.Image?.Dispose(); preview.Image = null;
        EnemyModelScene? displayModel = showingOriginal ? originalModel : model;
        if (displayModel == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item || !displayModel.TexturePackages.TryGetValue(item.TplEntry, out EnemyTexturePackage? package)) return;
        try
        {
            using var stream = new MemoryStream(package.Data, false); using var br = new BinaryReader(stream); var tpl = new TplReader().ReadTexture(br, item.Index); stream.Position = 0;
            preview.Image = new TextureDecoder().Decode(tpl, br);
        }
        catch { }
    }

    private void ImportPng()
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item) { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        using var dialog = new OpenFileDialog { Filter = "Imagem PNG (*.png)|*.png", Title = $"Substituir TPL #{item.TplEntry:D3}, textura {item.Index}" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.ReplaceTextureFromPng(datPath, item.TplEntry, item.Index, dialog.FileName);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, "Importar PNG"));
            ReloadModifiedTexture(item);
            status.Text = $"PNG aplicado. Backup preservado em {Path.GetFileName(datPath)}.bak";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Importar PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportSelectedTexture()
    {
        EnemyModelScene? displayModel = showingOriginal ? originalModel : model;
        if (displayModel == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        if (!displayModel.TexturePackages.TryGetValue(item.TplEntry, out EnemyTexturePackage? package))
        { MessageBox.Show(this, "O TPL da textura selecionada não está disponível."); return; }

        string datName = Path.GetFileNameWithoutExtension(datPath ?? "personagem");
        string version = showingOriginal ? "_original" : "";
        using var dialog = new SaveFileDialog
        {
            Filter = "Imagem PNG (*.png)|*.png",
            Title = $"Exportar {item}",
            FileName = $"{datName}_tpl{item.TplEntry:D3}_tex{item.Index:D2}{version}.png",
            AddExtension = true,
            DefaultExt = "png"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using var stream = new MemoryStream(package.Data, false);
            using var reader = new BinaryReader(stream);
            var tpl = new TplReader().ReadTexture(reader, item.Index);
            stream.Position = 0;
            using Bitmap bitmap = new TextureDecoder().Decode(tpl, reader);
            bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            status.Text = $"Textura exportada: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void TransformSelectedTexture(Ps2CharacterDatEditor.TextureTransform transform)
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        try
        {
            Cursor = Cursors.WaitCursor;
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.TransformTexture(datPath, item.TplEntry, item.Index, transform);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, transform.ToString()));
            ReloadModifiedTexture(item);
            status.Text = transform switch
            {
                Ps2CharacterDatEditor.TextureTransform.Rotate90 => "Textura rotacionada 90°.",
                Ps2CharacterDatEditor.TextureTransform.FlipX => "Textura invertida no eixo X.",
                _ => "Textura invertida no eixo Y."
            };
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Transformar textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

    private void ResizeSelectedTexture()
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        using var dialog = new TextureResizeDialog(item.Width, item.Height);
        if (dialog.ShowDialog(this) != DialogResult.OK || (dialog.TargetWidth == item.Width && dialog.TargetHeight == item.Height)) return;
        try
        {
            Cursor = Cursors.WaitCursor;
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.ResizeTexture(datPath, item.TplEntry, item.Index, dialog.TargetWidth, dialog.TargetHeight, dialog.ResamplingMode);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, $"Resize {item.Width}×{item.Height} → {dialog.TargetWidth}×{dialog.TargetHeight}"));
            ReloadModifiedTexture(item);
            status.Text = $"Textura redimensionada para {dialog.TargetWidth}×{dialog.TargetHeight}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Redimensionar textura", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

    private void ConvertSelectedTextureBitDepth(int colors)
    {
        if (datPath == null || lstTextures.SelectedItems.Count == 0 || lstTextures.SelectedItems[0].Tag is not TextureItem item)
        { MessageBox.Show(this, "Selecione uma textura primeiro."); return; }
        string target = colors == 16 ? "4-bit" : "8-bit";
        if (item.Format == target) return;
        try
        {
            Cursor = Cursors.WaitCursor;
            byte[] before = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            Ps2CharacterDatEditor.ConvertTextureBitDepth(datPath, item.TplEntry, item.Index, colors);
            byte[] after = Ps2CharacterDatEditor.CaptureTplEntry(datPath, item.TplEntry);
            PushTextureEdit(new TextureEdit(datPath, item.TplEntry, item.Index, before, after, $"Profundidade de cor → {target}"));
            ReloadModifiedTexture(item);
            status.Text = $"Textura convertida para {target}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Profundidade de cor", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

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
    }

    private void PushTextureEdit(TextureEdit edit)
    {
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

    private string? FindDefaultPl00()
    {
        string[] candidates = extractedRoots.Select(root => Path.Combine(root, "pl00", "Content", "pl00.dat")).ToArray();
        return candidates.FirstOrDefault(File.Exists);
    }
}
