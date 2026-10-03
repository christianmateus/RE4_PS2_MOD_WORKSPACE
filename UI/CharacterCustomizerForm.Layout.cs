using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private void BuildUi()
    {
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), ColumnCount = 8, BackColor = Color.FromArgb(22, 25, 30) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 1; i < 8; i++) top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,i>=6?128:92));
        cmbDat.SelectedIndexChanged += (_, _) => { if (!syncingDatList && cmbDat.SelectedItem is DatItem item) LoadDat(item.Path); };
        top.Controls.Add(cmbDat, 0, 0);
        top.Controls.Add(Button("ATUALIZAR", (_, _) => RefreshDatDropdown()), 1, 0);
        top.Controls.Add(Button("ABRIR IDX", (_, _) => BrowseDat()), 2, 0);
        top.Controls.Add(Button("ENQUADRAR", (_, _) => viewport.FitScene()), 3, 0);
        top.Controls.Add(Button("TODAS", (_, _) => ShowAllParts()), 4, 0);
        top.Controls.Add(Button("NENHUMA", (_, _) => HideAllParts()), 5, 0);

        top.Controls.Add(Button("EXPORTAR SMD", async (_, _) => await ExportCharacterPartsSmdAsync()),6,0);
        top.Controls.Add(Button("REIMPORTAR SMD", async (_, _) => await ReimportCharacterPartsSmdAsync()),7,0);

        lstTextures.Columns.Add("Textura", 190); lstTextures.Columns.Add("Índice", 40); lstTextures.Columns.Add("Dimensões", 75); lstTextures.Columns.Add("Formato", 60);
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
            selectedPart = (lstParts.SelectedItem as PartItem)?.Part;
            viewport.HighlightEnemyModelPart(PreviewType, selectedPart?.BinIndex);
            UpdatePartDetails(selectedPart);
            if (selectedPart != null) { PopulateMaterialMapping(selectedPart); PopulateBoneDiagnostics(selectedPart); }
        };
        lstParts.MouseDown += (_, e) => { if (e.Button == MouseButtons.Right) { int index = lstParts.IndexFromPoint(e.Location); if (index >= 0) lstParts.SelectedIndex = index; } };
        var partsMenu = new ContextMenuStrip { BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro };
        var hidePart = new ToolStripMenuItem("Ocultar somente no viewer");
        hidePart.Click += (_, _) => ToggleSelectedPartVisibility();
        var removePart = new ToolStripMenuItem("Remover do modelo (jogo)...");
        removePart.Click += (_, _) => RemoveSelectedPartFromModel();
        var restorePart = new ToolStripMenuItem("Restaurar BIN original");
        restorePart.Click += (_, _) => RestoreSelectedPartFromBackup();
        var soloPart = new ToolStripMenuItem("Isolar no viewer");
        soloPart.Click += (_, _) => SoloSelectedPart();
        partsMenu.Items.Add(hidePart); partsMenu.Items.Add(soloPart); partsMenu.Items.Add(new ToolStripSeparator()); partsMenu.Items.Add(removePart); partsMenu.Items.Add(restorePart);
        partsMenu.Opening += (_, _) =>
        {
            bool selected = lstParts.SelectedItem is PartItem;
            hidePart.Enabled = selected; removePart.Enabled = selected && selectedPart?.Triangles.Count > 0;
            restorePart.Enabled = selected && datPath != null && File.Exists(datPath + ".bak");
            if (selected && lstParts.SelectedIndex >= 0) hidePart.Text = lstParts.GetItemChecked(lstParts.SelectedIndex) ? "Ocultar somente no viewer" : "Mostrar no viewer";
        };
        partsMenu.Items.Add(new ToolStripSeparator());var exportSmd=new ToolStripMenuItem("Exportar partes para SMD...");exportSmd.Click+=async (_,_)=>await ExportCharacterPartsSmdAsync();partsMenu.Items.Add(exportSmd);var importSmd=new ToolStripMenuItem("Reimportar partes SMD do Blender...");importSmd.Click+=async (_,_)=>await ReimportCharacterPartsSmdAsync();partsMenu.Items.Add(importSmd);
        lstParts.ContextMenuStrip = partsMenu;
        viewport.EnemyModelPartClicked += SelectPartFromViewport;
        viewport.EnemyModelFaceClicked += SelectFaceFromViewport;
        viewport.EnemyModelFaceTranslationRequested += MoveSelectedFacesWithGizmo;
        viewport.EnemyModelPartTranslationRequested += MoveSelectedMeshWithGizmo;
        viewport.EnemyModelPartRotationRequested += RotateSelectedMeshWithGizmo;
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
        txtPartSearch.TextChanged += (_, _) => RefreshPartList(selectedPart?.BinIndex);
        cmbPartFilter.Items.AddRange(new object[] { "Todas", "Visíveis", "Ocultas", "Sem geometria", "Anotadas" });
        cmbPartFilter.SelectedIndex = 0;
        cmbPartFilter.SelectedIndexChanged += (_, _) => RefreshPartList(selectedPart?.BinIndex);
        var filterPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 2 };
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        filterPanel.Controls.Add(txtPartSearch, 0, 0); filterPanel.Controls.Add(cmbPartFilter, 1, 0);
        cmbViewPreset.SelectedIndexChanged += (_, _) => ApplySelectedPreset();
        var presetPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, WrapContents = true, Padding = new Padding(2) };
        presetPanel.Controls.Add(cmbViewPreset);
        presetPanel.Controls.Add(TextureButton("NOVO", (_, _) => CreatePreset(), 55));
        presetPanel.Controls.Add(TextureButton("DUPLICAR", (_, _) => DuplicatePreset(), 78));
        presetPanel.Controls.Add(TextureButton("SALVAR", (_, _) => SaveActivePreset(), 65));
        presetPanel.Controls.Add(TextureButton("RENOMEAR", (_, _) => RenamePreset(), 80));
        presetPanel.Controls.Add(TextureButton("EXCLUIR", (_, _) => DeletePreset(), 70));
        var materialPanel = new Panel { Dock = DockStyle.Bottom, Height = 74, BackColor = Color.FromArgb(28, 31, 37), Padding = new Padding(5) };
        var materialRow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(2, 3, 2, 2), WrapContents = false };
        materialRow.Controls.Add(new Label { Text = "DE", Width = 24, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Silver });
        materialRow.Controls.Add(cmbCurrentMap);
        materialRow.Controls.Add(new Label { Text = "PARA", Width = 38, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Silver });
        materialRow.Controls.Add(cmbNewMap);
        materialRow.Controls.Add(TextureButton("APLICAR", (_, _) => ApplyMaterialTextureIndex(), 72));
        materialPanel.Controls.Add(materialRow); materialPanel.Controls.Add(lblMaterialMap);
        var detailsPanel = new Panel { Dock = DockStyle.Bottom, Height = 170, BackColor = Color.FromArgb(28, 31, 37), Padding = new Padding(5) };
        txtPartName.Leave += (_, _) => SaveSelectedPartAnnotation();
        txtPartName.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { SaveSelectedPartAnnotation(); e.Handled = true; e.SuppressKeyPress = true; } };
        var nameLabel = new Label { Text = "NOME DO BIN", Dock = DockStyle.Top, Height = 23, ForeColor = Color.Silver };
        detailsPanel.Controls.Add(lblPartDetails); detailsPanel.Controls.Add(txtPartName); detailsPanel.Controls.Add(nameLabel);
        detailsPanel.Controls.Add(materialPanel);
        partsTab.Controls.Add(lstParts); partsTab.Controls.Add(detailsPanel); partsTab.Controls.Add(filterPanel); partsTab.Controls.Add(presetPanel);
        texturesTab = new TabPage("TEXTURAS") { BackColor = Color.FromArgb(22, 25, 30) };
        var textureSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 330 };
        var historyTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4), WrapContents = false, BackColor = Color.FromArgb(24, 27, 32) };
        ConfigureTextureButton(btnCompare, "VER ORIGINAL", (_, _) => ToggleOriginal(), 104);
        ConfigureTextureButton(btnUndo, "DESFAZER", (_, _) => UndoTextureEdit(), 80);
        ConfigureTextureButton(btnRedo, "REFAZER", (_, _) => RedoTextureEdit(), 76);
        historyTools.Controls.Add(btnCompare); historyTools.Controls.Add(btnUndo); historyTools.Controls.Add(btnRedo);
        historyTools.Controls.Add(TextureButton("RESTAURAR", (_, _) => RestoreSelectedTexture(), 88));
        txtTextureName.Leave += (_, _) => SaveSelectedTextureName();
        txtTextureName.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { SaveSelectedTextureName(); e.Handled = true; e.SuppressKeyPress = true; } };
        textureSplit.Panel1.Controls.Add(lstTextures); textureSplit.Panel1.Controls.Add(historyTools);
        textureSplit.Panel2.Controls.Add(preview); textureSplit.Panel2.Controls.Add(txtTextureName); texturesTab.Controls.Add(textureSplit);
        var meshLibraryTab = new TabPage("MESHES DO JOGO") { BackColor = Color.FromArgb(22, 25, 30), Padding = new Padding(5) };
        lstMeshLibrary.Columns.Add("Mesh", 205); lstMeshLibrary.Columns.Add("Triângulos", 70); lstMeshLibrary.Columns.Add("Tamanho", 95);
        lstMeshLibrary.ItemDrag += (_, e) => { if (e.Item is ListViewItem row && row.Tag is MeshLibraryItem item) lstMeshLibrary.DoDragDrop(item, DragDropEffects.Copy); };
        cmbMeshDonor.SelectedIndexChanged += (_, _) => LoadMeshDonor();
        var rigModePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 102, WrapContents = true, Padding = new Padding(2, 4, 2, 2) };
        rigModePanel.Controls.Add(new Label { Text = "ENCAIXE", Width = 64, Height = 27, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Silver });
        cmbRigMode.Items.AddRange(new object[] { "Automático (recomendado)", "Rígido / acessório", "Distribuído / roupas", "Manter rig do doador" }); cmbRigMode.SelectedIndex = 0;
        rigModePanel.Controls.Add(cmbRigMode);
        rigModePanel.Controls.Add(TextureButton("IMPORTAR MODELO...", async (_, _) => await ImportExternalModelAsync(), 132));
        rigModePanel.Controls.Add(TextureButton("SUBSTITUIR BIN...", (_, _) => ReplaceSelectedMeshFromLibrary(), 125));
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
        var viewportTools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, BackColor = Color.FromArgb(22, 25, 30) };
        chkFaceEdit.CheckedChanged += (_, _) => ToggleFaceEditMode();
        viewportTools.Controls.Add(chkFaceEdit);
        viewportTools.Controls.Add(lblFaceSelection);
        ConfigureTextureButton(btnMoveMesh, "MOVER", (_, _) => SetMeshGizmoMode(EnemyMeshGizmoMode.Move), 67);
        ConfigureTextureButton(btnRotateMesh, "ROTACIONAR", (_, _) => SetMeshGizmoMode(EnemyMeshGizmoMode.Rotate), 92);
        viewportTools.Controls.Add(btnMoveMesh);
        viewportTools.Controls.Add(btnRotateMesh);
        UpdateMeshGizmoButtons();
        viewportTools.Controls.Add(TextureButton("ISOLAR", (_, _) => SoloSelectedPart(), 66));
        viewportTools.Controls.Add(TextureButton("MOSTRAR TODAS", (_, _) => ShowAllParts(), 112));
        viewportTools.Controls.Add(TextureButton("OCULTAR TODAS", (_, _) => HideAllParts(), 112));
        viewportTools.Controls.Add(TextureButton("INVERTER", (_, _) => InvertPartVisibility(), 76));
        ConfigureTextureButton(btnDeleteFaces, "EXCLUIR FACES", (_, _) => DeleteSelectedFaces(), 105);
        viewportTools.Controls.Add(btnDeleteFaces);
        viewportTools.Controls.Add(chkAnimationTimeline);
        BuildAnimationTimeline();
        split.Panel1.Controls.Add(viewport); split.Panel1.Controls.Add(viewportTools);
        split.Panel1.Controls.Add(animationTimeline);
        split.Panel2.Controls.Add(rightTabs);
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
        AttachHelpTips();
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
        meshTransformGroup = transform;
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

}

