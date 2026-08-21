namespace RE4_PS2_MOD_WORKSPACE
{
    partial class Form1
    {
        private System.ComponentModel.IContainer? components = null;
        private Panel pnlSidebar = null!, pnlTop = null!, pnlContent = null!;
        private Panel pnlDashboard = null!, pnlWorkspace = null!, pnlAssets = null!, pnlTextures = null!, pnlVisualEditor = null!, pnlEnemies = null!, pnlAnimations = null!, pnlBuild = null!, pnlTools = null!, pnlLogs = null!;
        private Label lblLogo = null!, lblLogoSub = null!, lblVersion = null!, lblTopTitle = null!, lblWorkspaceCurrent = null!;
        private Button btnNavDashboard = null!, btnNavWorkspace = null!, btnNavAssets = null!, btnNavTextures = null!, btnNavVisualEditor = null!, btnNavEnemies = null!, btnNavAnimations = null!, btnNavBuild = null!, btnNavTools = null!, btnNavLogs = null!, btnTopBuild = null!;
        private ComboBox cmbEnemyFiles = null!, cmbEnemyTypeFriendly = null!, cmbEnemySubtypeFriendly = null!, cmbEnemyLocationFilter = null!, cmbVisualEnemyAttachBone = null!;
        private Button btnEnemyOpen = null!, btnEnemyReextract = null!, btnEnemySave = null!, btnEnemyRefresh = null!;
        private CheckBox chkEnemyActiveOnly = null!;
        private ListBox lstEnemyEntries = null!;
        private PropertyGrid pgEnemyProperties = null!;
        private Label lblEnemyFileInfo = null!, lblEnemyEntryCount = null!, lblEnemyStatus = null!;
        private ComboBox cmbAnimationFiles = null!;
        private Button btnAnimationBrowse = null!, btnAnimationRefresh = null!, btnAnimationBinBrowse = null!, btnAnimationAutoBin = null!, btnAnimationPlay = null!, btnAnimationStop = null!, btnAnimationFit = null!;
        private CheckBox chkAnimationBoneIds = null!, chkAnimationRestPose = null!;
        private ComboBox cmbAnimationBones = null!;
        private Label lblAnimationFile = null!, lblAnimationSummary = null!, lblAnimationStatus = null!, lblAnimationTrackDetail = null!, lblAnimationSkeleton = null!, lblAnimationFrame = null!, lblAnimationBoneDebug = null!;
        private DataGridView gridAnimationTracks = null!, gridAnimationKeys = null!;
        private TabControl tabAnimationAxis = null!, tabAnimationView = null!;
        private TrackBar trkAnimationFrame = null!;
        private AnimationSkeletonViewport animationSkeletonViewport = null!;
        private Label lblCardWorkspaceValue = null!, lblCardIsoValue = null!, lblCardDatValue = null!, lblCardStatusValue = null!;
        private TextBox txtWorkspacePath = null!, txtIsoPath = null!, txtDatPath = null!, txtIsoAfs = null!, txtDatTool = null!, txtTplManager = null!, txtPcsx2 = null!;
        private Button btnBrowseWorkspace = null!, btnCreateWorkspace = null!, btnOpenWorkspace = null!, btnBrowseIso = null!, btnBrowseDat = null!, btnDashboardWorkspace = null!;
        private Button btnBrowseIsoAfs = null!, btnBrowseDatTool = null!, btnBrowseTpl = null!, btnBrowsePcsx2 = null!, btnOpenIsoAfs = null!, btnOpenDatTool = null!, btnOpenTpl = null!, btnOpenPcsx2 = null!;
        private ComboBox cmbAfsEntries = null!, cmbDatEntries = null!, cmbAssetType = null!, cmbTextureDat = null!, cmbTextureSmd = null!;
        private Button btnScanIso = null!, btnExtractScenario = null!, btnRefreshContent = null!, btnOpenContentFolder = null!;
        private Label lblDatCurrentSize = null!, lblDatReservedSize = null!, lblDatFreeSpace = null!, lblAfsName = null!, lblContentSummary = null!;
        private TextBox txtAssetSearch = null!;
        private DataGridView gridAssets = null!;
        private RichTextBox rtbExtractLog = null!, rtbBuildLog = null!;
        private Button btnTextureLoad = null!, btnTextureReload = null!, btnTextureReplace = null!, btnTextureReplaceAll = null!, btnTextureExport = null!, btnTextureExportAll = null!, btnTextureOpenExternal = null!, btnTextureRotate = null!, btnTextureResize = null!, btnTextureFlipX = null!, btnTextureFlipY = null!, btnTextureIncreaseAll = null!, btnTextureDecreaseAll = null!;
        private Label lblTplStatus = null!, lblTextureTitle = null!, lblTextureMeta = null!, lblTextureCount = null!, lblTextureLoading = null!, lblTextureThumbSize = null!;
        private ListView lvTextures = null!, lvTrackedDats = null!;
        private AlphaPreviewBox picTexturePreview = null!;
        private ImageList textureImages = null!;
        private TrackBar trackTextureThumb = null!;
        private ContextMenuStrip ctxTexture = null!;
        private ToolStripMenuItem miTextureExport = null!, miTextureReplace = null!, miTextureIncrease = null!, miTextureDecrease = null!;
        private Button btnBuildOneClick = null!, btnBuildRefreshChanges = null!, btnBuildRepackDat = null!, btnBuildInjectIso = null!, btnBuildRecreateIso = null!, btnBuildOpenDat = null!, btnBuildOpenIsoAfs = null!, btnBuildOpenPcsx2 = null!, btnBuildFolder = null!, btnBuildAll = null!, btnBuildRefreshTracked = null!;
        private Label lblBuildActiveDat = null!, lblBuildDatStatus = null!, lblBuildIsoStatus = null!, lblBuildChangeStatus = null!, lblTrackedDatsSummary = null!;
        private Button btnVisualOpenScenario = null!, btnVisualOpenAev = null!, btnVisualFit = null!;
        private ComboBox cmbVisualDat = null!;
        private Button btnVisualSaveAev = null!, btnVisualSaveEsl = null!;
        private TrackBar trkVisualMoveSpeed = null!, trkVisualLookSpeed = null!;
        private Label lblVisualMoveSpeed = null!, lblVisualLookSpeed = null!;
        private CheckBox chkVisualAevLabels = null!, chkVisualEnemyLabels = null!, chkVisualEnemyInactive = null!, chkVisualEnemyModelParts = null!, chkVisualEnemySnap = null!, chkVisualEnemyAnimated = null!;
        private ComboBox cmbVisualRenderMode = null!;
        private Label lblVisualStage = null!, lblVisualStatus = null!;
        private CheckedListBox clbVisualLayers = null!;
        private ListBox lstVisualAevEntries = null!, lstVisualEnemyEntries = null!;
        private CheckedListBox clbVisualEnemyModelParts = null!;
        private Panel pnlVisualEnemyModelParts = null!;
        private Button btnVisualEnemyPartsSolo = null!, btnVisualEnemyPartsAll = null!, btnVisualEnemyPartsAuto = null!, btnVisualEnemyGizmoMove = null!, btnVisualEnemyGizmoRotate = null!;
        private Label lblVisualEnemyParts = null!, lblVisualEnemyAttachment = null!;
        private NumericUpDown nudVisualEnemyAttachX = null!, nudVisualEnemyAttachY = null!, nudVisualEnemyAttachZ = null!, nudVisualEnemyAttachRX = null!, nudVisualEnemyAttachRY = null!, nudVisualEnemyAttachRZ = null!;
        private TabControl tabVisualEntities = null!;
        private Label lblVisualPropertiesTitle = null!;
        private ComboBox cmbVisualAevTypeFilter = null!, cmbVisualEnemyLocationFilter = null!;
        private ContextMenuStrip ctxVisualAevEntries = null!, ctxVisualEnemyEntries = null!;
        private PropertyGrid pgVisualProperties = null!;
        private ScenarioViewport visualViewport = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            if (disposing && picTexturePreview?.Image != null) picTexturePreview.Image.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            textureImages = new ImageList(components) { ImageSize = new Size(104, 104), ColorDepth = ColorDepth.Depth32Bit };
            pnlSidebar = new Panel(); pnlTop = new Panel(); pnlContent = new Panel();
            pnlDashboard = new Panel(); pnlWorkspace = new Panel(); pnlAssets = new Panel(); pnlTextures = new Panel(); pnlVisualEditor = new Panel(); pnlEnemies = new Panel(); pnlAnimations = new Panel(); pnlBuild = new Panel(); pnlTools = new Panel(); pnlLogs = new Panel();
            lblLogo = new Label(); lblLogoSub = new Label(); lblVersion = new Label(); lblTopTitle = new Label();
            btnNavDashboard = new Button(); btnNavWorkspace = new Button(); btnNavAssets = new Button(); btnNavTextures = new Button(); btnNavVisualEditor = new Button(); btnNavEnemies = new Button(); btnNavAnimations = new Button(); btnNavBuild = new Button(); btnNavTools = new Button(); btnNavLogs = new Button(); btnTopBuild = new Button();
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Bg;
            ClientSize = new Size(1380, 820);
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1120, 690);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "RE4 PS2 Mod Workspace";
            KeyPreview = true;
            KeyDown += Form1_GlobalKeyDown;

            pnlSidebar.BackColor = Sidebar;
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 184;
            pnlSidebar.Padding = new Padding(12, 16, 12, 12);
            lblLogo.Text = "RE4 PS2"; lblLogo.Dock = DockStyle.Top; lblLogo.Height = 30; lblLogo.Font = new Font("Segoe UI Semibold", 16F); lblLogo.ForeColor = TextPrimary;
            lblLogoSub.Text = "MOD WORKSPACE"; lblLogoSub.Dock = DockStyle.Top; lblLogoSub.Height = 40; lblLogoSub.Font = new Font("Segoe UI Semibold", 8F); lblLogoSub.ForeColor = Accent;
            SetupNav(btnNavDashboard, "Dashboard", btnNavDashboard_Click);
            SetupNav(btnNavWorkspace, "Projeto", btnNavWorkspace_Click);
            SetupNav(btnNavAssets, "Arquivos", btnNavAssets_Click);
            SetupNav(btnNavTextures, "Texturas", btnNavTextures_Click);
            SetupNav(btnNavVisualEditor, "Visual Editor", btnNavVisualEditor_Click);
            SetupNav(btnNavEnemies, "Inimigos", btnNavEnemies_Click);
            SetupNav(btnNavAnimations, "Animações", btnNavAnimations_Click);
            SetupNav(btnNavBuild, "Build & Test", btnNavBuild_Click);
            SetupNav(btnNavTools, "Ferramentas", btnNavTools_Click);
            SetupNav(btnNavLogs, "Console", btnNavLogs_Click);
            lblVersion.Text = "v0.5.0"; lblVersion.Dock = DockStyle.Bottom; lblVersion.Height = 26; lblVersion.ForeColor = TextMuted; lblVersion.TextAlign = ContentAlignment.MiddleLeft;
            pnlSidebar.Controls.Add(btnNavLogs); pnlSidebar.Controls.Add(btnNavTools); pnlSidebar.Controls.Add(btnNavBuild); pnlSidebar.Controls.Add(btnNavAnimations); pnlSidebar.Controls.Add(btnNavEnemies); pnlSidebar.Controls.Add(btnNavVisualEditor); pnlSidebar.Controls.Add(btnNavTextures); pnlSidebar.Controls.Add(btnNavAssets); pnlSidebar.Controls.Add(btnNavWorkspace); pnlSidebar.Controls.Add(btnNavDashboard); pnlSidebar.Controls.Add(lblLogoSub); pnlSidebar.Controls.Add(lblLogo); pnlSidebar.Controls.Add(lblVersion);

            pnlTop.BackColor = Bg;
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 58;
            pnlTop.Padding = new Padding(24, 10, 24, 8);
            lblTopTitle.Text = "Dashboard"; lblTopTitle.Dock = DockStyle.Left; lblTopTitle.Width = 430; lblTopTitle.Font = new Font("Segoe UI Semibold", 14F); lblTopTitle.TextAlign = ContentAlignment.MiddleLeft;
            SetupButton(btnTopBuild, "BUILD & TEST", Accent, 130); btnTopBuild.Dock = DockStyle.Right; btnTopBuild.Click += btnTopBuild_Click;
            pnlTop.Controls.Add(btnTopBuild); pnlTop.Controls.Add(lblTopTitle);

            pnlContent.BackColor = Bg;
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(24, 10, 24, 20);
            foreach (Panel page in new[] { pnlDashboard, pnlWorkspace, pnlAssets, pnlTextures, pnlVisualEditor, pnlEnemies, pnlAnimations, pnlBuild, pnlTools, pnlLogs }) SetupPage(page);
            BuildDashboardDesigner(); BuildProjectDesigner(); BuildAssetsDesigner(); BuildTexturesDesigner(); BuildVisualEditorDesigner(); BuildEnemiesDesigner(); BuildAnimationsDesigner(); BuildBuildDesigner(); BuildToolsDesigner(); BuildLogsDesigner();
            pnlContent.Controls.Add(pnlLogs); pnlContent.Controls.Add(pnlTools); pnlContent.Controls.Add(pnlBuild); pnlContent.Controls.Add(pnlAnimations); pnlContent.Controls.Add(pnlEnemies); pnlContent.Controls.Add(pnlVisualEditor); pnlContent.Controls.Add(pnlTextures); pnlContent.Controls.Add(pnlAssets); pnlContent.Controls.Add(pnlWorkspace); pnlContent.Controls.Add(pnlDashboard);

            Controls.Add(pnlContent); Controls.Add(pnlTop); Controls.Add(pnlSidebar);
            ResumeLayout(false);
        }

        private static readonly Color Bg = Color.FromArgb(13, 15, 18);
        private static readonly Color Sidebar = Color.FromArgb(18, 20, 24);
        private static readonly Color Surface = Color.FromArgb(22, 25, 30);
        private static readonly Color Surface2 = Color.FromArgb(28, 31, 37);
        private static readonly Color Border = Color.FromArgb(47, 51, 60);
        private static readonly Color TextPrimary = Color.FromArgb(238, 240, 244);
        private static readonly Color TextMuted = Color.FromArgb(145, 151, 163);
        private static readonly Color Accent = Color.FromArgb(196, 56, 56);

        private void SetupPage(Panel panel) { panel.BackColor = Bg; panel.Dock = DockStyle.Fill; panel.AutoScroll = false; panel.Visible = false; }
        private void SetupNav(Button b, string text, EventHandler handler) { b.UseMnemonic = false; b.Text = "  " + text; b.Dock = DockStyle.Top; b.Height = 42; b.Margin = new Padding(0, 0, 0, 2); b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Surface2; b.BackColor = Sidebar; b.ForeColor = TextMuted; b.TextAlign = ContentAlignment.MiddleLeft; b.Cursor = Cursors.Hand; b.Font = new Font("Segoe UI Semibold", 9F); b.Click += handler; }
        private void SetupButton(Button b, string text, Color color, int width) { b.UseMnemonic = false; b.Text = text; b.Width = width; b.Height = 34; b.BackColor = color; b.ForeColor = TextPrimary; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand; b.Font = new Font("Segoe UI Semibold", 8.7F); }
        private void SetupSecondary(Button b, string text, int width) { SetupButton(b, text, Surface2, width); b.FlatAppearance.BorderColor = Border; b.FlatAppearance.BorderSize = 1; }
        private Label AddPageHeader(Panel page, string title, string subtitle) { var a = new Label { UseMnemonic = false, Text = title, Left = 0, Top = 0, Width = 640, Height = 30, Font = new Font("Segoe UI Semibold", 19F), ForeColor = TextPrimary }; var b = new Label { Text = subtitle, Left = 1, Top = 33, Width = 900, Height = 24, ForeColor = TextMuted }; page.Controls.Add(a); page.Controls.Add(b); return a; }
        private Panel Card(int x, int y, int w, int h) => new() { Left = x, Top = y, Width = w, Height = h, BackColor = Surface, Padding = new Padding(16) };
        private TextBox PathBox(Panel parent, string label, int y, int width, out Button browse) { parent.Controls.Add(new Label { Text = label, Left = 16, Top = y, Width = 240, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) }); var box = new TextBox { Left = 16, Top = y + 22, Width = width - 132, Height = 30, BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle }; browse = new Button { Left = width - 106, Top = y + 20 }; SetupSecondary(browse, "PROCURAR", 90); parent.Controls.Add(box); parent.Controls.Add(browse); return box; }
        private Label StatLabel(Control parent, string caption, int x, int width, out Label value) { var c = new Label { Text = caption, Left = x, Top = 12, Width = width, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) }; value = new Label { Text = "—", Left = x, Top = 33, Width = width, Height = 25, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 11F), AutoEllipsis = true }; parent.Controls.Add(c); parent.Controls.Add(value); return c; }

        private void BuildDashboardDesigner()
        {
            AddPageHeader(pnlDashboard, "Dashboard", "Visão rápida do projeto e atalhos para o fluxo de modding.");
            int y = 78;
            var cards = new[] { Card(0, y, 210, 88), Card(224, y, 210, 88), Card(448, y, 210, 88), Card(672, y, 210, 88) };
            string[] captions = { "WORKSPACE", "ISO BASE", "DAT ATIVO", "STATUS" };
            Label[] values = new Label[4];
            for (int i = 0; i < cards.Length; i++) { cards[i].Controls.Add(new Label { Text = captions[i], Dock = DockStyle.Top, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) }); values[i] = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 10.5F), AutoEllipsis = true }; cards[i].Controls.Add(values[i]); pnlDashboard.Controls.Add(cards[i]); }
            lblCardWorkspaceValue = values[0]; lblCardIsoValue = values[1]; lblCardDatValue = values[2]; lblCardStatusValue = values[3];
            var quick = Card(0, 184, 882, 112); pnlDashboard.Controls.Add(quick);
            quick.Controls.Add(new Label { Text = "Continuar trabalhando", Left = 16, Top = 14, Width = 300, Height = 24, Font = new Font("Segoe UI Semibold", 12F) });
            lblWorkspaceCurrent = new Label { Text = "Nenhum workspace selecionado", Left = 16, Top = 43, Width = 600, Height = 24, ForeColor = TextMuted, AutoEllipsis = true }; quick.Controls.Add(lblWorkspaceCurrent);
            btnDashboardWorkspace = new Button { Left = 16, Top = 70 }; SetupButton(btnDashboardWorkspace, "ABRIR PROJETO", Accent, 140); btnDashboardWorkspace.Click += btnDashboardWorkspace_Click; quick.Controls.Add(btnDashboardWorkspace);
            var flow = Card(0, 314, 882, 150); pnlDashboard.Controls.Add(flow);
            flow.Controls.Add(new Label { Text = "Fluxo rápido", Left = 16, Top = 14, Width = 240, Height = 24, Font = new Font("Segoe UI Semibold", 12F) });
            flow.Controls.Add(new Label { UseMnemonic = false, Text = "1  Extraia o DAT em Arquivos     →     2  Edite em Texturas     →     3  Use BUILD & TEST", Left = 16, Top = 53, Width = 820, Height = 28, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 10F) });
            flow.Controls.Add(new Label { Text = "O Workspace detecta alterações, repacka apenas o necessário e reutiliza a ISO de Build.", Left = 16, Top = 86, Width = 800, Height = 24, ForeColor = TextMuted });
        }

        private void BuildProjectDesigner()
        {
            AddPageHeader(pnlWorkspace, "Projeto", "Workspace, ISO base e caminhos principais do projeto.");
            var main = Card(0, 76, 882, 260); pnlWorkspace.Controls.Add(main);
            txtWorkspacePath = PathBox(main, "WORKSPACE", 16, 850, out btnBrowseWorkspace); btnBrowseWorkspace.Click += btnBrowseWorkspace_Click;
            btnCreateWorkspace = new Button { Left = 16, Top = 82 }; SetupButton(btnCreateWorkspace, "CRIAR NOVO", Accent, 125); btnCreateWorkspace.Click += btnCreateWorkspace_Click; main.Controls.Add(btnCreateWorkspace);
            btnOpenWorkspace = new Button { Left = 151, Top = 82 }; SetupSecondary(btnOpenWorkspace, "ABRIR PASTA", 125); btnOpenWorkspace.Click += btnOpenWorkspace_Click; main.Controls.Add(btnOpenWorkspace);
            txtIsoPath = PathBox(main, "ISO BASE", 128, 850, out btnBrowseIso); btnBrowseIso.Click += btnBrowseIso_Click; txtIsoPath.Leave += txtIsoPath_Leave;
            txtDatPath = PathBox(main, "DAT MANUAL / LEGADO", 194, 850, out btnBrowseDat); btnBrowseDat.Click += btnBrowseDat_Click; txtDatPath.Leave += txtDatPath_Leave;
            pnlWorkspace.Controls.Add(new Label { Text = "A seleção normal de DAT agora é feita na página Arquivos. O campo manual permanece apenas para compatibilidade com projetos antigos.", Left = 4, Top = 352, Width = 860, Height = 42, ForeColor = TextMuted });
        }

        private void BuildAssetsDesigner()
        {
            AddPageHeader(pnlAssets, "Arquivos", "Leia o BIO4DAT.AFS, extraia cenários e navegue pelo conteúdo do DAT.");
            var extract = Card(0, 70, 882, 142); pnlAssets.Controls.Add(extract);
            extract.Controls.Add(new Label { Text = "AFS", Left = 16, Top = 12, Width = 120, Height = 18, ForeColor = TextMuted });
            cmbAfsEntries = new ComboBox { Left = 16, Top = 34, Width = 360, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbAfsEntries.SelectedIndexChanged += cmbAfsEntries_SelectedIndexChanged; extract.Controls.Add(cmbAfsEntries);
            btnScanIso = new Button { Left = 388, Top = 32 }; SetupSecondary(btnScanIso, "LER ISO / AFS", 118); btnScanIso.Click += btnScanIso_Click; extract.Controls.Add(btnScanIso);
            extract.Controls.Add(new Label { Text = "DAT", Left = 16, Top = 72, Width = 120, Height = 18, ForeColor = TextMuted });
            cmbDatEntries = new ComboBox { Left = 16, Top = 94, Width = 360, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbDatEntries.SelectedIndexChanged += cmbDatEntries_SelectedIndexChanged; extract.Controls.Add(cmbDatEntries);
            btnExtractScenario = new Button { Left = 388, Top = 92 }; SetupButton(btnExtractScenario, "EXTRAIR CENÁRIO", Accent, 150); btnExtractScenario.Enabled = false; btnExtractScenario.Click += btnExtractScenario_Click; extract.Controls.Add(btnExtractScenario);
            lblAfsName = new Label { Text = "AFS ainda não carregado", Left = 524, Top = 12, Width = 338, Height = 20, ForeColor = TextMuted, AutoEllipsis = true }; extract.Controls.Add(lblAfsName);
            StatLabel(extract, "CURRENT", 524, 104, out lblDatCurrentSize); StatLabel(extract, "RESERVED", 636, 104, out lblDatReservedSize); StatLabel(extract, "FREE", 748, 104, out lblDatFreeSpace);

            var toolbar = new Panel { Left = 0, Top = 224, Width = 882, Height = 66, BackColor = Bg }; pnlAssets.Controls.Add(toolbar);
            txtAssetSearch = new TextBox { Left = 0, Top = 25, Width = 300, Height = 30, PlaceholderText = "Filtrar por nome ou caminho...", BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle }; txtAssetSearch.TextChanged += assetFilter_Changed; toolbar.Controls.Add(txtAssetSearch);
            cmbAssetType = new ComboBox { Left = 312, Top = 25, Width = 135, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbAssetType.Items.AddRange(new object[] { "Todos", "SMD", "TPL", "BIN", "AEV", "ESL", "SND", "SEQ", "Outros" }); cmbAssetType.SelectedIndex = 0; cmbAssetType.SelectedIndexChanged += assetFilter_Changed; toolbar.Controls.Add(cmbAssetType);
            btnRefreshContent = new Button { Left = 584, Top = 23 }; SetupSecondary(btnRefreshContent, "ATUALIZAR", 110); btnRefreshContent.Click += btnRefreshContent_Click; toolbar.Controls.Add(btnRefreshContent);
            btnOpenContentFolder = new Button { Left = 706, Top = 23 }; SetupSecondary(btnOpenContentFolder, "ABRIR PASTA", 126); btnOpenContentFolder.Click += btnOpenContentFolder_Click; toolbar.Controls.Add(btnOpenContentFolder);
            lblContentSummary = new Label { Text = "Nenhum cenário carregado.", Left = 0, Top = 2, Width = 540, Height = 20, ForeColor = TextMuted, AutoEllipsis = true }; toolbar.Controls.Add(lblContentSummary);

            var gridHost = new Panel { Left = 0, Top = 292, Width = 882, Height = 300, BackColor = Bg };
            void ResizeAssetsGrid()
            {
                int availableWidth = Math.Max(100, pnlAssets.ClientSize.Width);
                int availableHeight = Math.Max(120, pnlAssets.ClientSize.Height - gridHost.Top);
                gridHost.SetBounds(0, gridHost.Top, availableWidth, availableHeight);
            }
            pnlAssets.Resize += (_, _) => ResizeAssetsGrid();
            pnlAssets.Controls.Add(gridHost);
            ResizeAssetsGrid();
            gridAssets = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Surface, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoGenerateColumns = false, EnableHeadersVisualStyles = false, ColumnHeadersHeight = 36, RowTemplate = { Height = 30 }, GridColor = Border, ScrollBars = ScrollBars.Both, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None };
            gridAssets.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface2, ForeColor = TextPrimary, SelectionBackColor = Surface2, Font = new Font("Segoe UI Semibold", 8.5F), Padding = new Padding(6, 0, 0, 0) };
            gridAssets.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface, ForeColor = Color.FromArgb(214, 218, 225), SelectionBackColor = Color.FromArgb(55, 59, 68), SelectionForeColor = TextPrimary, Padding = new Padding(6, 0, 0, 0) };
            gridAssets.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(24, 27, 32) };
            gridAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Nome", DataPropertyName = "Name", Width = 260, SortMode = DataGridViewColumnSortMode.Programmatic });
            gridAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Tipo", DataPropertyName = "Type", Width = 90, SortMode = DataGridViewColumnSortMode.Programmatic });
            gridAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Tamanho", DataPropertyName = "SizeBytes", Width = 100, SortMode = DataGridViewColumnSortMode.Programmatic });
            gridAssets.Columns.Add(new DataGridViewTextBoxColumn { Name = "RelativePath", HeaderText = "Caminho", DataPropertyName = "RelativePath", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.Programmatic });
            gridAssets.SelectionChanged += gridAssets_SelectionChanged; gridAssets.CellDoubleClick += gridAssets_CellDoubleClick; gridAssets.CellFormatting += gridAssets_CellFormatting; gridAssets.ColumnHeaderMouseClick += gridAssets_ColumnHeaderMouseClick; gridHost.Controls.Add(gridAssets);
        }

        private void BuildTexturesDesigner()
        {
            AddPageHeader(pnlTextures, "Texturas", "Gerenciamento nativo de TPL, mipmaps, transformações e edição em lote.");
            var top = Card(0, 70, 882, 112); top.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlTextures.Controls.Add(top);
            top.Controls.Add(new Label { Text = "CENÁRIO / DAT", Left = 14, Top = 9, Width = 110, Height = 18, ForeColor = TextMuted });
            cmbTextureDat = new ComboBox { Left = 14, Top = 29, Width = 310, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbTextureDat.SelectedIndexChanged += cmbTextureDat_SelectedIndexChanged; top.Controls.Add(cmbTextureDat);
            cmbTextureSmd = new ComboBox { Visible = false, DropDownStyle = ComboBoxStyle.DropDownList }; cmbTextureSmd.SelectedIndexChanged += cmbTextureSmd_SelectedIndexChanged; top.Controls.Add(cmbTextureSmd);
            lblTextureCount = new Label { Text = "0 texturas", Left = 720, Top = 10, Width = 142, Height = 20, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right }; top.Controls.Add(lblTextureCount);
            btnTextureLoad = new Button { Left = 14, Top = 68 }; SetupButton(btnTextureLoad, "ATUALIZAR", Accent, 104); btnTextureLoad.Click += btnTextureLoad_Click; top.Controls.Add(btnTextureLoad);
            btnTextureReload = new Button { Left = 128, Top = 68 }; SetupSecondary(btnTextureReload, "RELER DO SMD", 116); btnTextureReload.Click += btnTextureReload_Click; top.Controls.Add(btnTextureReload);
            btnTextureOpenExternal = new Button { Left = 254, Top = 68 }; SetupSecondary(btnTextureOpenExternal, "TPL MANAGER", 112); btnTextureOpenExternal.Click += btnTextureOpenExternal_Click; top.Controls.Add(btnTextureOpenExternal);
            top.Controls.Add(new Label { Text = "THUMB", Left = 392, Top = 12, Width = 58, Height = 18, ForeColor = TextMuted });
            trackTextureThumb = new TrackBar { Left = 448, Top = 3, Width = 180, Height = 38, Minimum = 64, Maximum = 160, TickFrequency = 16, Value = 104, AutoSize = false }; trackTextureThumb.ValueChanged += trackTextureThumb_ValueChanged; top.Controls.Add(trackTextureThumb);
            lblTextureThumbSize = new Label { Text = "104px", Left = 632, Top = 12, Width = 55, Height = 18, ForeColor = TextMuted }; top.Controls.Add(lblTextureThumbSize);
            top.Controls.Add(new Label { Text = "ZOOM DO PREVIEW: use a roda do mouse sobre a imagem", Left = 392, Top = 54, Width = 320, Height = 18, ForeColor = TextMuted });

            var left = Card(0, 194, 552, 508); left.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; pnlTextures.Controls.Add(left);
            lvTextures = new ListView { Dock = DockStyle.Fill, View = View.LargeIcon, LargeImageList = textureImages, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, MultiSelect = false, HideSelection = false, TileSize = new Size(128, 142), AllowDrop = true }; lvTextures.SelectedIndexChanged += lvTextures_SelectedIndexChanged; lvTextures.DoubleClick += lvTextures_DoubleClick; lvTextures.DragEnter += lvTextures_DragEnter; lvTextures.DragDrop += lvTextures_DragDrop; lvTextures.MouseDown += lvTextures_MouseDown; left.Controls.Add(lvTextures);
            lblTextureLoading = new Label { Text = "Carregando texturas...", Width = 286, Height = 58, BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI Semibold", 11F), TextAlign = ContentAlignment.MiddleCenter, Visible = false }; left.Controls.Add(lblTextureLoading); left.Resize += (_, _) => { lblTextureLoading.Left = Math.Max(8, (left.ClientSize.Width - lblTextureLoading.Width) / 2); lblTextureLoading.Top = Math.Max(8, (left.ClientSize.Height - lblTextureLoading.Height) / 2); }; lblTextureLoading.BringToFront();

            ctxTexture = new ContextMenuStrip(components) { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            miTextureExport = new ToolStripMenuItem("Exportar PNG"); miTextureReplace = new ToolStripMenuItem("Substituir PNG..."); miTextureIncrease = new ToolStripMenuItem("Aumentar cores: 4-bit → 8-bit"); miTextureDecrease = new ToolStripMenuItem("Diminuir cores: 8-bit → 4-bit");
            miTextureExport.Click += miTextureExport_Click; miTextureReplace.Click += miTextureReplace_Click; miTextureIncrease.Click += miTextureIncrease_Click; miTextureDecrease.Click += miTextureDecrease_Click;
            ctxTexture.Items.AddRange(new ToolStripItem[] { miTextureExport, miTextureReplace, new ToolStripSeparator(), miTextureIncrease, miTextureDecrease }); ctxTexture.Opening += ctxTexture_Opening; lvTextures.ContextMenuStrip = ctxTexture;

            var right = Card(564, 194, 318, 508); right.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right; right.AutoScroll = true; pnlTextures.Controls.Add(right);
            picTexturePreview = new AlphaPreviewBox { Left = 14, Top = 14, Width = 274, Height = 226, BackColor = Color.FromArgb(12, 13, 16), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; right.Controls.Add(picTexturePreview);
            lblTextureTitle = new Label { Text = "Nenhuma textura selecionada", Left = 14, Top = 252, Width = 274, Height = 24, Font = new Font("Segoe UI Semibold", 10.5F), AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; right.Controls.Add(lblTextureTitle);
            lblTextureMeta = new Label { Text = "Selecione uma textura para ver os detalhes.", Left = 14, Top = 278, Width = 274, Height = 48, ForeColor = TextMuted, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; right.Controls.Add(lblTextureMeta);

            btnTextureReplace = new Button { Left = 14, Top = 334 }; SetupButton(btnTextureReplace, "SUBSTITUIR", Accent, 128); btnTextureReplace.Enabled = false; btnTextureReplace.Click += btnTextureReplace_Click; right.Controls.Add(btnTextureReplace);
            btnTextureExport = new Button { Left = 150, Top = 334 }; SetupSecondary(btnTextureExport, "EXPORT PNG", 138); btnTextureExport.Enabled = false; btnTextureExport.Click += btnTextureExport_Click; right.Controls.Add(btnTextureExport);
            btnTextureReplaceAll = new Button { Left = 14, Top = 376 }; SetupSecondary(btnTextureReplaceAll, "SUBSTITUIR TODAS", 128); btnTextureReplaceAll.Enabled = false; btnTextureReplaceAll.Click += btnTextureReplaceAll_Click; right.Controls.Add(btnTextureReplaceAll);
            btnTextureExportAll = new Button { Left = 150, Top = 376 }; SetupSecondary(btnTextureExportAll, "EXPORT ALL", 138); btnTextureExportAll.Enabled = false; btnTextureExportAll.Click += btnTextureExportAll_Click; right.Controls.Add(btnTextureExportAll);

            right.Controls.Add(new Label { Text = "AJUSTES RÁPIDOS", Left = 14, Top = 426, Width = 274, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            btnTextureRotate = new Button { Left = 14, Top = 452 }; SetupSecondary(btnTextureRotate, "ROTATE 90°", 128); btnTextureRotate.Click += btnTextureRotate_Click; right.Controls.Add(btnTextureRotate);
            btnTextureResize = new Button { Left = 150, Top = 452 }; SetupSecondary(btnTextureResize, "RESIZE", 138); btnTextureResize.Click += btnTextureResize_Click; right.Controls.Add(btnTextureResize);
            btnTextureFlipX = new Button { Left = 14, Top = 494 }; SetupSecondary(btnTextureFlipX, "FLIP X", 128); btnTextureFlipX.Click += btnTextureFlipX_Click; right.Controls.Add(btnTextureFlipX);
            btnTextureFlipY = new Button { Left = 150, Top = 494 }; SetupSecondary(btnTextureFlipY, "FLIP Y", 138); btnTextureFlipY.Click += btnTextureFlipY_Click; right.Controls.Add(btnTextureFlipY);

            right.Controls.Add(new Label { Text = "BIT DEPTH EM LOTE", Left = 14, Top = 544, Width = 274, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            btnTextureIncreaseAll = new Button { Left = 14, Top = 570 }; SetupSecondary(btnTextureIncreaseAll, "TODAS → 8-BIT", 128); btnTextureIncreaseAll.Click += btnTextureIncreaseAll_Click; right.Controls.Add(btnTextureIncreaseAll);
            btnTextureDecreaseAll = new Button { Left = 150, Top = 570 }; SetupSecondary(btnTextureDecreaseAll, "TODAS → 4-BIT", 138); btnTextureDecreaseAll.Click += btnTextureDecreaseAll_Click; right.Controls.Add(btnTextureDecreaseAll);
            lblTplStatus = new Label { Text = "Selecione um DAT para começar.", Left = 14, Top = 616, Width = 274, Height = 44, ForeColor = TextMuted, AutoEllipsis = true }; right.Controls.Add(lblTplStatus);

            void ResizeTextureLayout()
            {
                int width = Math.Max(700, pnlTextures.ClientSize.Width);
                int height = Math.Max(360, pnlTextures.ClientSize.Height - 194);
                int rightWidth = 318;
                int gap = 12;
                left.SetBounds(0, 194, Math.Max(330, width - rightWidth - gap), height);
                right.SetBounds(Math.Max(342, width - rightWidth), 194, rightWidth, height);
            }
            pnlTextures.Resize += (_, _) => ResizeTextureLayout();
            ResizeTextureLayout();
        }


        private void BuildEnemiesDesigner()
        {
            AddPageHeader(pnlEnemies, "Inimigos", "Gerenciamento dos arquivos emleon*.ESL diretamente do AFS ativo.");

            var top = Card(0, 70, 882, 116); top.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlEnemies.Controls.Add(top);
            top.Controls.Add(new Label { Text = "ARQUIVO ESL", Left = 14, Top = 10, Width = 110, Height = 18, ForeColor = TextMuted });
            cmbEnemyFiles = new ComboBox { Left = 14, Top = 31, Width = 300, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbEnemyFiles.SelectedIndexChanged += cmbEnemyFiles_SelectedIndexChanged; top.Controls.Add(cmbEnemyFiles);
            btnEnemyRefresh = new Button { Left = 326, Top = 29 }; SetupSecondary(btnEnemyRefresh, "ATUALIZAR", 104); btnEnemyRefresh.Click += btnEnemyRefresh_Click; top.Controls.Add(btnEnemyRefresh);
            btnEnemyOpen = new Button { Visible = false }; top.Controls.Add(btnEnemyOpen);
            btnEnemyReextract = new Button { Left = 442, Top = 29 }; SetupSecondary(btnEnemyReextract, "RE-EXTRAIR", 112); btnEnemyReextract.Click += btnEnemyReextract_Click; top.Controls.Add(btnEnemyReextract);
            btnEnemySave = new Button { Left = 566, Top = 29 }; SetupButton(btnEnemySave, "SALVAR", Accent, 96); btnEnemySave.Click += btnEnemySave_Click; btnEnemySave.Enabled = false; top.Controls.Add(btnEnemySave);
            lblEnemyFileInfo = new Label { Text = "Nenhum ESL selecionado", Left = 14, Top = 72, Width = 850, Height = 20, ForeColor = TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; top.Controls.Add(lblEnemyFileInfo);

            var toolbar = Card(0, 198, 882, 48); toolbar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlEnemies.Controls.Add(toolbar);
            chkEnemyActiveOnly = new CheckBox { Text = "Somente ativos", Checked = true, AutoSize = true, Left = 14, Top = 15, ForeColor = Color.FromArgb(210, 214, 221) }; chkEnemyActiveOnly.CheckedChanged += chkEnemyActiveOnly_CheckedChanged; toolbar.Controls.Add(chkEnemyActiveOnly);
            cmbEnemyLocationFilter = new ComboBox { Left = 142, Top = 9, Width = 150, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbEnemyLocationFilter.SelectedIndexChanged += cmbEnemyLocationFilter_SelectedIndexChanged; toolbar.Controls.Add(cmbEnemyLocationFilter);
            lblEnemyEntryCount = new Label { Text = "Nenhum ESL aberto", Left = 305, Top = 14, Width = 240, Height = 20, ForeColor = TextMuted }; toolbar.Controls.Add(lblEnemyEntryCount);
            lblEnemyStatus = new Label { Text = "Selecione um arquivo emleon*.ESL do AFS.", Left = 550, Top = 14, Width = 312, Height = 20, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Right }; toolbar.Controls.Add(lblEnemyStatus);

            var left = Card(0, 258, 356, 444); left.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left; pnlEnemies.Controls.Add(left);
            lstEnemyEntries = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Surface, ForeColor = TextPrimary, Font = new Font("Consolas", 9.5F), IntegralHeight = false, SelectionMode = SelectionMode.MultiExtended }; lstEnemyEntries.SelectedIndexChanged += lstEnemyEntries_SelectedIndexChanged; left.Controls.Add(lstEnemyEntries);
            var right = Card(368, 258, 514, 444); right.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; pnlEnemies.Controls.Add(right);
            var enemyFriendly = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = Surface2, Padding = new Padding(12, 10, 12, 8) };
            right.Controls.Add(enemyFriendly);
            var lblFriendlyType = new Label { Text = "ENEMY TYPE", Left = 12, Top = 10, Width = 120, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) }; enemyFriendly.Controls.Add(lblFriendlyType);
            cmbEnemyTypeFriendly = new ComboBox { Left = 12, Top = 31, Width = 320, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; cmbEnemyTypeFriendly.SelectedIndexChanged += cmbEnemyTypeFriendly_SelectedIndexChanged; enemyFriendly.Controls.Add(cmbEnemyTypeFriendly);
            var lblFriendlySubtype = new Label { Text = "SUBTYPE", Left = 12, Top = 63, Width = 120, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) }; enemyFriendly.Controls.Add(lblFriendlySubtype);
            cmbEnemySubtypeFriendly = new ComboBox { Left = 12, Top = 82, Width = 320, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; cmbEnemySubtypeFriendly.SelectedIndexChanged += cmbEnemySubtypeFriendly_SelectedIndexChanged; enemyFriendly.Controls.Add(cmbEnemySubtypeFriendly);
            pgEnemyProperties = new PropertyGrid { Dock = DockStyle.Fill, HelpVisible = true, ToolbarVisible = false, BackColor = Surface, ForeColor = TextPrimary }; pgEnemyProperties.PropertyValueChanged += pgEnemyProperties_PropertyValueChanged; right.Controls.Add(pgEnemyProperties); pgEnemyProperties.BringToFront(); enemyFriendly.BringToFront();

            void ResizeEnemyLayout()
            {
                int width = Math.Max(700, pnlEnemies.ClientSize.Width);
                int height = Math.Max(300, pnlEnemies.ClientSize.Height - 258);
                int leftWidth = Math.Min(390, Math.Max(300, width * 38 / 100));
                left.SetBounds(0, 258, leftWidth, height);
                right.SetBounds(leftWidth + 12, 258, Math.Max(300, width - leftWidth - 12), height);
            }
            pnlEnemies.Resize += (_, _) => ResizeEnemyLayout();
            ResizeEnemyLayout();
        }


        private void BuildAnimationsDesigner()
        {
            AddPageHeader(pnlAnimations, "Animações", "FCV Inspector + Skeleton Viewer experimental para Resident Evil 4 PS2.");
            var top = Card(0, 70, 882, 112); top.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlAnimations.Controls.Add(top);
            top.Controls.Add(new Label { Text = "ARQUIVO FCV", Left = 14, Top = 10, Width = 120, Height = 18, ForeColor = TextMuted });
            cmbAnimationFiles = new ComboBox { Left = 14, Top = 31, Width = 420, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbAnimationFiles.SelectedIndexChanged += cmbAnimationFiles_SelectedIndexChanged; top.Controls.Add(cmbAnimationFiles);
            btnAnimationRefresh = new Button { Left = 446, Top = 29 }; SetupSecondary(btnAnimationRefresh, "ATUALIZAR", 104); btnAnimationRefresh.Click += btnAnimationRefresh_Click; top.Controls.Add(btnAnimationRefresh);
            btnAnimationBrowse = new Button { Left = 562, Top = 29 }; SetupButton(btnAnimationBrowse, "ABRIR FCV", Accent, 112); btnAnimationBrowse.Click += btnAnimationBrowse_Click; top.Controls.Add(btnAnimationBrowse);
            lblAnimationFile = new Label { Text = "Nenhum FCV aberto", Left = 14, Top = 72, Width = 250, Height = 20, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 9F) }; top.Controls.Add(lblAnimationFile);
            lblAnimationSummary = new Label { Text = "Frames: —    Tracks: —", Left = 270, Top = 72, Width = 590, Height = 20, ForeColor = TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; top.Controls.Add(lblAnimationSummary);

            var left = Card(0, 194, 480, 508); left.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left; pnlAnimations.Controls.Add(left);
            left.Controls.Add(new Label { Text = "TRACKS", Left = 12, Top = 10, Width = 100, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            gridAnimationTracks = new DataGridView { Left = 12, Top = 36, Width = 456, Height = 458, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackgroundColor = Surface, ForeColor = TextPrimary, GridColor = Border, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None };
            gridAnimationTracks.ColumnHeadersDefaultCellStyle.BackColor = Surface2; gridAnimationTracks.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary; gridAnimationTracks.EnableHeadersVisualStyles = false; gridAnimationTracks.DefaultCellStyle.BackColor = Surface; gridAnimationTracks.DefaultCellStyle.ForeColor = TextPrimary; gridAnimationTracks.DefaultCellStyle.SelectionBackColor = Surface2; gridAnimationTracks.DefaultCellStyle.SelectionForeColor = TextPrimary;
            gridAnimationTracks.Columns.Add("Index", "#"); gridAnimationTracks.Columns.Add("Node", "Node"); gridAnimationTracks.Columns.Add("Type", "Type"); gridAnimationTracks.Columns.Add("Meaning", "Significado"); gridAnimationTracks.Columns.Add("Data", "Data"); gridAnimationTracks.Columns.Add("Offset", "Offset"); gridAnimationTracks.Columns.Add("Order", "Ord."); gridAnimationTracks.Columns.Add("X", "X"); gridAnimationTracks.Columns.Add("Y", "Y"); gridAnimationTracks.Columns.Add("Z", "Z");
            int[] widths = { 34, 52, 52, 135, 52, 82, 42, 34, 34, 34 }; for (int i = 0; i < widths.Length; i++) gridAnimationTracks.Columns[i].Width = widths[i];
            gridAnimationTracks.SelectionChanged += gridAnimationTracks_SelectionChanged; left.Controls.Add(gridAnimationTracks);

            var right = Card(492, 194, 390, 508); right.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; pnlAnimations.Controls.Add(right);
            tabAnimationView = new TabControl { Left=10, Top=10, Width=370, Height=488, Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right }; right.Controls.Add(tabAnimationView);
            var tabInspector = new TabPage("Inspector") { BackColor=Surface, ForeColor=TextPrimary };
            var tabSkeleton = new TabPage("Skeleton 3D") { BackColor=Surface, ForeColor=TextPrimary }; tabAnimationView.TabPages.Add(tabInspector); tabAnimationView.TabPages.Add(tabSkeleton);
            lblAnimationTrackDetail = new Label { Text = "Selecione um track", Left = 8, Top = 8, Width = 334, Height = 22, ForeColor = TextPrimary, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; tabInspector.Controls.Add(lblAnimationTrackDetail);
            tabAnimationAxis = new TabControl { Left = 8, Top = 36, Width = 334, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; tabAnimationAxis.TabPages.Add("X"); tabAnimationAxis.TabPages.Add("Y"); tabAnimationAxis.TabPages.Add("Z"); tabAnimationAxis.SelectedIndexChanged += tabAnimationAxis_SelectedIndexChanged; tabInspector.Controls.Add(tabAnimationAxis);
            gridAnimationKeys = new DataGridView { Left = 8, Top = 72, Width = 334, Height = 348, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackgroundColor = Surface, ForeColor = TextPrimary, GridColor = Border, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            gridAnimationKeys.ColumnHeadersDefaultCellStyle.BackColor = Surface2; gridAnimationKeys.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary; gridAnimationKeys.EnableHeadersVisualStyles = false; gridAnimationKeys.DefaultCellStyle.BackColor = Surface; gridAnimationKeys.DefaultCellStyle.ForeColor = TextPrimary; gridAnimationKeys.Columns.Add("Key", "#"); gridAnimationKeys.Columns.Add("Frame", "Frame"); gridAnimationKeys.Columns.Add("Value", "Value"); gridAnimationKeys.Columns.Add("In", "Tangent In"); gridAnimationKeys.Columns.Add("Out", "Tangent Out"); gridAnimationKeys.Columns.Add("Extra", "Extra"); tabInspector.Controls.Add(gridAnimationKeys);
            lblAnimationStatus = new Label { Text = "Abra um FCV para iniciar a análise.", Left = 8, Top = 428, Width = 334, Height = 28, ForeColor = TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right }; tabInspector.Controls.Add(lblAnimationStatus);

            btnAnimationBinBrowse = new Button { Left=8,Top=8 }; SetupSecondary(btnAnimationBinBrowse,"ABRIR BIN",96); btnAnimationBinBrowse.Click += btnAnimationBinBrowse_Click; tabSkeleton.Controls.Add(btnAnimationBinBrowse);
            btnAnimationAutoBin = new Button { Left=112,Top=8 }; SetupSecondary(btnAnimationAutoBin,"AUTO 440",96); btnAnimationAutoBin.Click += btnAnimationAutoBin_Click; tabSkeleton.Controls.Add(btnAnimationAutoBin);
            btnAnimationFit = new Button { Left=216,Top=8 }; SetupSecondary(btnAnimationFit,"ENQUADRAR",104); btnAnimationFit.Click += btnAnimationFit_Click; tabSkeleton.Controls.Add(btnAnimationFit);

            // Playback fica sempre visível no topo do viewer para facilitar os primeiros testes FCV.
            btnAnimationPlay = new Button { Left=8,Top=44 }; SetupButton(btnAnimationPlay,"PLAY",Accent,72); btnAnimationPlay.Click += btnAnimationPlay_Click; tabSkeleton.Controls.Add(btnAnimationPlay);
            btnAnimationStop = new Button { Left=86,Top=44 }; SetupSecondary(btnAnimationStop,"STOP",72); btnAnimationStop.Click += btnAnimationStop_Click; tabSkeleton.Controls.Add(btnAnimationStop);
            lblAnimationFrame = new Label { Text="Frame 0 / —",Left=168,Top=52,Width=174,Height=20,TextAlign=ContentAlignment.MiddleRight,ForeColor=TextPrimary,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; tabSkeleton.Controls.Add(lblAnimationFrame);
            trkAnimationFrame = new TrackBar { Left=8,Top=78,Width=334,Height=30,Minimum=0,Maximum=1,TickStyle=TickStyle.None,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; trkAnimationFrame.Scroll += trkAnimationFrame_Scroll; tabSkeleton.Controls.Add(trkAnimationFrame);

            lblAnimationSkeleton = new Label { Text="BIN: nenhum skeleton carregado",Left=8,Top=108,Width=334,Height=20,ForeColor=TextMuted,AutoEllipsis=true,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; tabSkeleton.Controls.Add(lblAnimationSkeleton);
            animationSkeletonViewport = new AnimationSkeletonViewport { Left=8,Top=130,Width=334,Height=198,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right }; animationSkeletonViewport.SelectedBoneChanged += animationSkeletonViewport_SelectedBoneChanged; tabSkeleton.Controls.Add(animationSkeletonViewport);

            var boneDebugPanel = new Panel { Left=8,Top=334,Width=334,Height=116,BackColor=Surface2,Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right }; tabSkeleton.Controls.Add(boneDebugPanel);
            chkAnimationBoneIds = new CheckBox { Text="IDs",Left=8,Top=6,Width=48,Height=22,ForeColor=TextPrimary,BackColor=Color.Transparent }; chkAnimationBoneIds.CheckedChanged += chkAnimationBoneIds_CheckedChanged; boneDebugPanel.Controls.Add(chkAnimationBoneIds);
            chkAnimationRestPose = new CheckBox { Text="Pose base",Left=58,Top=6,Width=82,Height=22,ForeColor=TextPrimary,BackColor=Color.Transparent }; chkAnimationRestPose.CheckedChanged += chkAnimationRestPose_CheckedChanged; boneDebugPanel.Controls.Add(chkAnimationRestPose);
            cmbAnimationBones = new ComboBox { Left=146,Top=4,Width=180,Height=25,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; cmbAnimationBones.SelectedIndexChanged += cmbAnimationBones_SelectedIndexChanged; boneDebugPanel.Controls.Add(cmbAnimationBones);
            lblAnimationBoneDebug = new Label { Text="Clique em um joint ou selecione um bone acima.",Left=8,Top=32,Width=318,Height=80,ForeColor=TextMuted,AutoEllipsis=true,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; boneDebugPanel.Controls.Add(lblAnimationBoneDebug);
            tabSkeleton.Controls.Add(new Label { Text="PLAY: 30 FPS • loop automático • LMB: bone • RMB: orbitar • Scroll: zoom",Left=8,Top=454,Width=334,Height=20,ForeColor=TextMuted,Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right });

            // O Skeleton 3D usa toda a largura disponível. A grade de tracks continua no layout
            // dividido apenas enquanto o Inspector está selecionado. Isso deixa o GL viewport
            // realmente grande, em vez de espremido na coluna da direita.
            void ResizeAnimationLayout()
            {
                int width = Math.Max(760, pnlAnimations.ClientSize.Width);
                int height = Math.Max(300, pnlAnimations.ClientSize.Height - 194);
                bool skeletonMode = tabAnimationView != null && tabAnimationView.SelectedIndex == 1;
                if (skeletonMode)
                {
                    left.Visible = false;
                    right.SetBounds(0, 194, width, height);
                }
                else
                {
                    left.Visible = true;
                    int leftWidth = Math.Min(500, Math.Max(360, width * 40 / 100));
                    left.SetBounds(0, 194, leftWidth, height);
                    right.SetBounds(leftWidth + 12, 194, Math.Max(480, width - leftWidth - 12), height);
                }
            }
            tabAnimationView.SelectedIndexChanged += (_, _) => ResizeAnimationLayout();
            pnlAnimations.Resize += (_, _) => ResizeAnimationLayout();
            ResizeAnimationLayout();
        }

        private void BuildBuildDesigner()
        {
            AddPageHeader(pnlBuild, "Build & Test", "Build automático do DAT, injeção na ISO e suporte a vários cenários modificados.");
            var hero = Card(0, 70, 882, 92); pnlBuild.Controls.Add(hero);
            lblBuildChangeStatus = new Label { Text = "Verificando alterações...", Left = 16, Top = 16, Width = 530, Height = 52, ForeColor = TextMuted, AutoEllipsis = true }; hero.Controls.Add(lblBuildChangeStatus);
            btnBuildRefreshChanges = new Button { Left = 564, Top = 28 }; SetupSecondary(btnBuildRefreshChanges, "ATUALIZAR", 112); btnBuildRefreshChanges.Click += btnBuildRefreshChanges_Click; hero.Controls.Add(btnBuildRefreshChanges);
            btnBuildOneClick = new Button { Left = 690, Top = 20 }; SetupButton(btnBuildOneClick, "BUILD & TEST", Accent, 168); btnBuildOneClick.Height = 50; btnBuildOneClick.Click += btnBuildOneClick_Click; hero.Controls.Add(btnBuildOneClick);

            lblTrackedDatsSummary = new Label { Text = "Carregando DATs...", Left = 0, Top = 178, Width = 570, Height = 24, ForeColor = TextMuted }; pnlBuild.Controls.Add(lblTrackedDatsSummary);
            btnBuildRefreshTracked = new Button { Left = 600, Top = 172 }; SetupSecondary(btnBuildRefreshTracked, "ATUALIZAR LISTA", 130); btnBuildRefreshTracked.Click += btnBuildRefreshTracked_Click; pnlBuild.Controls.Add(btnBuildRefreshTracked);
            btnBuildAll = new Button { Left = 742, Top = 172 }; SetupButton(btnBuildAll, "BUILD ALL", Accent, 140); btnBuildAll.Click += btnBuildAll_Click; pnlBuild.Controls.Add(btnBuildAll);
            lvTrackedDats = new ListView { Left = 0, Top = 212, Width = 882, Height = 190, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, View = View.Details, FullRowSelect = true, HideSelection = false, BackColor = Surface, ForeColor = Color.FromArgb(220, 223, 230), BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9F) };
            lvTrackedDats.Columns.Add("DAT", 175); lvTrackedDats.Columns.Add("Estado", 190); lvTrackedDats.Columns.Add("Arquivos", 90); lvTrackedDats.Columns.Add("TPLs", 70); lvTrackedDats.Columns.Add("Último build", 170); lvTrackedDats.DoubleClick += lvTrackedDats_DoubleClick; pnlBuild.Controls.Add(lvTrackedDats);

            var actions = Card(0, 414, 882, 126); pnlBuild.Controls.Add(actions);
            lblBuildActiveDat = new Label { Text = "DAT ativo: nenhum", Left = 14, Top = 12, Width = 420, Height = 22, Font = new Font("Segoe UI Semibold", 9.5F) }; actions.Controls.Add(lblBuildActiveDat);
            lblBuildDatStatus = new Label { Text = "Extraia e edite um cenário antes de reconstruir.", Left = 14, Top = 37, Width = 820, Height = 22, ForeColor = TextMuted, AutoEllipsis = true }; actions.Controls.Add(lblBuildDatStatus);
            btnBuildRepackDat = new Button { Visible = false };
            btnBuildInjectIso = new Button { Visible = false };
            btnBuildOpenPcsx2 = new Button { Left = 14, Top = 73 }; SetupSecondary(btnBuildOpenPcsx2, "ABRIR PCSX2", 126); btnBuildOpenPcsx2.Click += btnBuildOpenPcsx2_Click; actions.Controls.Add(btnBuildOpenPcsx2);
            btnBuildRecreateIso = new Button { Left = 150, Top = 73 }; SetupSecondary(btnBuildRecreateIso, "ISO LIMPA", 110); btnBuildRecreateIso.Click += btnBuildRecreateIso_Click; actions.Controls.Add(btnBuildRecreateIso);
            btnBuildFolder = new Button { Left = 270, Top = 73 }; SetupSecondary(btnBuildFolder, "PASTA BUILD", 120); btnBuildFolder.Click += btnBuildFolder_Click; actions.Controls.Add(btnBuildFolder);
            btnBuildOpenIsoAfs = new Button { Left = 400, Top = 73 }; SetupSecondary(btnBuildOpenIsoAfs, "ISOAFS", 100); btnBuildOpenIsoAfs.Click += btnBuildOpenIsoAfs_Click; actions.Controls.Add(btnBuildOpenIsoAfs);
            btnBuildOpenDat = new Button { Left = 510, Top = 73 }; SetupSecondary(btnBuildOpenDat, "DAT TOOL", 100); btnBuildOpenDat.Click += btnBuildOpenDat_Click; actions.Controls.Add(btnBuildOpenDat);
            actions.Controls.Add(new Label { Text = "Use BUILD & TEST ou BUILD ALL para reconstruir e injetar automaticamente.", Left = 624, Top = 68, Width = 238, Height = 42, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleRight });
            lblBuildIsoStatus = new Label { Text = "Fast Build reutiliza a ISO existente.", Left = 0, Top = 554, Width = 850, Height = 22, ForeColor = TextMuted }; pnlBuild.Controls.Add(lblBuildIsoStatus);
        }

        private void BuildVisualEditorDesigner()
        {
            AddPageHeader(pnlVisualEditor, "Visual Editor", "Base 3D compartilhada para cenário, eventos e outros dados espaciais do jogo.");

            var toolbar = Card(0, 76, 882, 78);
            toolbar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlVisualEditor.Controls.Add(toolbar);

            cmbVisualDat = new ComboBox { Left = 12, Top = 10, Width = 190, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbVisualDat.SelectedIndexChanged += cmbVisualDat_SelectedIndexChanged;
            toolbar.Controls.Add(cmbVisualDat);

            btnVisualFit = new Button { Left = 210, Top = 8 }; SetupSecondary(btnVisualFit, "FIT", 54);
            btnVisualFit.Click += (_, _) => visualViewport?.FitScene();
            toolbar.Controls.Add(btnVisualFit);

            cmbVisualRenderMode = new ComboBox { Left = 272, Top = 10, Width = 132, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbVisualRenderMode.Items.AddRange(new object[] { "Solid", "Solid + Wireframe", "Wireframe" });
            cmbVisualRenderMode.SelectedIndex = 0;
            cmbVisualRenderMode.SelectedIndexChanged += cmbVisualRenderMode_SelectedIndexChanged;
            toolbar.Controls.Add(cmbVisualRenderMode);

            btnVisualSaveAev = new Button { Left = 412, Top = 8 }; SetupButton(btnVisualSaveAev, "SAVE AEV", Accent, 100);
            btnVisualSaveAev.Enabled = false;
            btnVisualSaveAev.Click += btnVisualSaveAev_Click;
            toolbar.Controls.Add(btnVisualSaveAev);

            btnVisualSaveEsl = new Button { Left = 520, Top = 8 }; SetupButton(btnVisualSaveEsl, "SAVE ESL", Accent, 92);
            btnVisualSaveEsl.Enabled = false; btnVisualSaveEsl.Click += btnVisualSaveEsl_Click; toolbar.Controls.Add(btnVisualSaveEsl);

            lblVisualStage = new Label { Text = "Nenhum DAT ativo", Left = 620, Top = 13, Width = 150, Height = 20, ForeColor = TextMuted, AutoEllipsis = true };
            toolbar.Controls.Add(lblVisualStage);
            lblVisualStatus = new Label { Text = "v0.5.0 • Visual Editor", Left = 780, Top = 13, Width = 100, Height = 20, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            toolbar.Controls.Add(lblVisualStatus);

            lblVisualMoveSpeed = new Label { Text = "MOVE 1.00×", Left = 12, Top = 50, Width = 82, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) };
            toolbar.Controls.Add(lblVisualMoveSpeed);
            trkVisualMoveSpeed = new TrackBar { Left = 94, Top = 40, Width = 170, Height = 32, Minimum = 10, Maximum = 300, Value = 100, TickStyle = TickStyle.None, SmallChange = 5, LargeChange = 20 };
            trkVisualMoveSpeed.Scroll += visualMoveSpeed_Scroll;
            toolbar.Controls.Add(trkVisualMoveSpeed);

            lblVisualLookSpeed = new Label { Text = "LOOK 1.00×", Left = 278, Top = 50, Width = 82, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) };
            toolbar.Controls.Add(lblVisualLookSpeed);
            trkVisualLookSpeed = new TrackBar { Left = 360, Top = 40, Width = 170, Height = 32, Minimum = 10, Maximum = 250, Value = 100, TickStyle = TickStyle.None, SmallChange = 5, LargeChange = 20 };
            trkVisualLookSpeed.Scroll += visualLookSpeed_Scroll;
            toolbar.Controls.Add(trkVisualLookSpeed);

            chkVisualAevLabels = new CheckBox { Text = "AEV LABELS", Left = 548, Top = 48, Width = 106, Height = 20, Checked = true, ForeColor = TextMuted, BackColor = Surface, FlatStyle = FlatStyle.Flat };
            chkVisualAevLabels.CheckedChanged += (_, _) => { if (visualViewport != null) { visualViewport.ShowAevLabels = chkVisualAevLabels.Checked; visualViewport.Invalidate(); } };
            toolbar.Controls.Add(chkVisualAevLabels);

            chkVisualEnemyLabels = new CheckBox { Text = "ENEMY LABELS", Left = 660, Top = 48, Width = 126, Height = 20, Checked = false, ForeColor = TextMuted, BackColor = Surface, FlatStyle = FlatStyle.Flat };
            chkVisualEnemyLabels.CheckedChanged += (_, _) => { settings.VisualEnemyLabels = chkVisualEnemyLabels.Checked; if (!restoringSession) SaveSettings(); if (visualViewport != null) { visualViewport.ShowEnemyLabels = chkVisualEnemyLabels.Checked; visualViewport.Invalidate(); } };
            toolbar.Controls.Add(chkVisualEnemyLabels);

            var workspace = new SplitContainer { Left = 0, Top = 166, Width = Math.Max(1, pnlVisualEditor.ClientSize.Width), Height = Math.Max(1, pnlVisualEditor.ClientSize.Height - 166), SplitterDistance = 210, FixedPanel = FixedPanel.Panel1, BackColor = Border, BorderStyle = BorderStyle.None };
            workspace.Panel1.BackColor = Surface; workspace.Panel2.BackColor = Bg; pnlVisualEditor.Controls.Add(workspace);

            void ResizeVisualEditor()
            {
                int w = Math.Max(1, pnlVisualEditor.ClientSize.Width);
                int h = Math.Max(1, pnlVisualEditor.ClientSize.Height);
                toolbar.Width = w;
                workspace.SetBounds(0, 166, w, Math.Max(1, h - 166));
                workspace.PerformLayout();
                workspace.Panel2.PerformLayout();
                visualViewport?.PerformLayout();
                visualViewport?.Invalidate();
            }
            pnlVisualEditor.Resize += (_, _) => ResizeVisualEditor();

            var layersTitle = new Label { Text = "LAYERS", Dock = DockStyle.Top, Height = 34, Padding = new Padding(12, 10, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) };
            clbVisualLayers = new CheckedListBox { Dock = DockStyle.Top, Height = 118, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, CheckOnClick = true, Font = new Font("Segoe UI", 9.5F), Padding = new Padding(8) };
            clbVisualLayers.Items.Add("Scenario", true); clbVisualLayers.Items.Add("AEV Events", true); clbVisualLayers.Items.Add("Enemies", false); clbVisualLayers.Items.Add("Objects", false); clbVisualLayers.Items.Add("Collision", false); clbVisualLayers.ItemCheck += clbVisualLayers_ItemCheck;

            tabVisualEntities = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, BackColor = Surface, ForeColor = TextPrimary, Padding = new Point(10, 4) };
            var tabAev = new TabPage("AEV") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabEnemies = new TabPage("INIMIGOS") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            tabVisualEntities.TabPages.Add(tabAev); tabVisualEntities.TabPages.Add(tabEnemies);

            cmbVisualAevTypeFilter = new ComboBox { Dock = DockStyle.Top, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbVisualAevTypeFilter.SelectedIndexChanged += cmbVisualAevTypeFilter_SelectedIndexChanged;
            lstVisualAevEntries = new ListBox { Dock = DockStyle.Fill, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, IntegralHeight = false, Font = new Font("Segoe UI", 9F) };
            lstVisualAevEntries.SelectedIndexChanged += lstVisualAevEntries_SelectedIndexChanged; lstVisualAevEntries.KeyDown += lstVisualAevEntries_KeyDown;
            var aevHint = new Label { Text = "EVENTOS DA FASE", Dock = DockStyle.Top, Height = 26, Padding = new Padding(8, 7, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) };
            tabAev.Controls.Add(lstVisualAevEntries); tabAev.Controls.Add(cmbVisualAevTypeFilter); tabAev.Controls.Add(aevHint);

            cmbVisualEnemyLocationFilter = new ComboBox { Dock = DockStyle.Top, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbVisualEnemyLocationFilter.SelectedIndexChanged += cmbVisualEnemyLocationFilter_SelectedIndexChanged;
            chkVisualEnemyInactive = new CheckBox { Text = "Mostrar inativos", Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 0, 0, 0), BackColor = Surface, ForeColor = TextPrimary, Checked = false };
            chkVisualEnemyInactive.CheckedChanged += chkVisualEnemyInactive_CheckedChanged;
            lstVisualEnemyEntries = new ListBox { Dock = DockStyle.Fill, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, IntegralHeight = false, Font = new Font("Segoe UI", 9F), SelectionMode = SelectionMode.MultiExtended };
            lstVisualEnemyEntries.SelectedIndexChanged += lstVisualEnemyEntries_SelectedIndexChanged; lstVisualEnemyEntries.KeyDown += lstVisualEnemyEntries_KeyDown;
            var enemyHint = new Label { Text = "INIMIGOS • Ctrl/Shift = seleção múltipla", Dock = DockStyle.Top, Height = 26, Padding = new Padding(8, 7, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) };
            var transformHint = new Label { Text = "G = mover • R = rotacionar • F = focar", Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(8, 5, 8, 0), ForeColor = TextMuted, Font = new Font("Segoe UI", 8F) };
            var enemyGizmoBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, BackColor = Surface, Padding = new Padding(6, 3, 3, 2), FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            btnVisualEnemyGizmoMove = new Button(); SetupButton(btnVisualEnemyGizmoMove, "MOVE (G)", Accent, 82); btnVisualEnemyGizmoMove.Height = 28; btnVisualEnemyGizmoMove.Click += btnVisualEnemyGizmoMove_Click;
            btnVisualEnemyGizmoRotate = new Button(); SetupButton(btnVisualEnemyGizmoRotate, "ROTATE (R)", Surface2, 92); btnVisualEnemyGizmoRotate.Height = 28; btnVisualEnemyGizmoRotate.Click += btnVisualEnemyGizmoRotate_Click;
            chkVisualEnemySnap = new CheckBox { Text = "Snap", AutoSize = true, Margin = new Padding(8, 6, 0, 0), BackColor = Surface, ForeColor = TextPrimary, Checked = false }; chkVisualEnemySnap.CheckedChanged += chkVisualEnemySnap_CheckedChanged;
            chkVisualEnemyAnimated = new CheckBox { Text = "Animated", AutoSize = true, Margin = new Padding(10, 6, 0, 0), BackColor = Surface, ForeColor = TextPrimary, Checked = false }; chkVisualEnemyAnimated.CheckedChanged += chkVisualEnemyAnimated_CheckedChanged;
            enemyGizmoBar.Controls.Add(btnVisualEnemyGizmoMove); enemyGizmoBar.Controls.Add(btnVisualEnemyGizmoRotate); enemyGizmoBar.Controls.Add(chkVisualEnemySnap); enemyGizmoBar.Controls.Add(chkVisualEnemyAnimated);

            // MODEL PARTS is an optional reverse-engineering tool. Hidden by default in v0.4.7.
            chkVisualEnemyModelParts = new CheckBox { Text = "Mostrar Model Parts", Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 0, 0, 0), BackColor = Surface, ForeColor = TextMuted, Checked = false };
            chkVisualEnemyModelParts.CheckedChanged += chkVisualEnemyModelParts_CheckedChanged;
            var enemyListHost = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
            enemyListHost.Controls.Add(lstVisualEnemyEntries); enemyListHost.Controls.Add(enemyGizmoBar); enemyListHost.Controls.Add(transformHint);
            pnlVisualEnemyModelParts = new Panel { Dock = DockStyle.Bottom, Height = 330, BackColor = Surface2, Padding = new Padding(0, 1, 0, 0), Visible = false };
            lblVisualEnemyParts = new Label { Text = "MODEL PARTS • selecione um inimigo", Dock = DockStyle.Top, Height = 25, Padding = new Padding(8, 6, 0, 0), ForeColor = TextMuted, BackColor = Surface, Font = new Font("Segoe UI Semibold", 8F) };
            clbVisualEnemyModelParts = new CheckedListBox { Dock = DockStyle.Fill, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, CheckOnClick = true, IntegralHeight = false, Font = new Font("Consolas", 8.5F) };
            clbVisualEnemyModelParts.ItemCheck += clbVisualEnemyModelParts_ItemCheck;
            var enemyPartsButtons = new Panel { Dock = DockStyle.Bottom, Height = 39, BackColor = Surface };
            btnVisualEnemyPartsSolo = new Button { Left = 6, Top = 3 }; SetupButton(btnVisualEnemyPartsSolo, "SOLO", Surface2, 62); btnVisualEnemyPartsSolo.Click += btnVisualEnemyPartsSolo_Click;
            btnVisualEnemyPartsAll = new Button { Left = 74, Top = 3 }; SetupButton(btnVisualEnemyPartsAll, "SHOW ALL", Surface2, 82); btnVisualEnemyPartsAll.Click += btnVisualEnemyPartsAll_Click;
            btnVisualEnemyPartsAuto = new Button { Left = 162, Top = 3 }; SetupButton(btnVisualEnemyPartsAuto, "AUTO", Accent, 64); btnVisualEnemyPartsAuto.Click += btnVisualEnemyPartsAuto_Click;
            enemyPartsButtons.Controls.Add(btnVisualEnemyPartsSolo); enemyPartsButtons.Controls.Add(btnVisualEnemyPartsAll); enemyPartsButtons.Controls.Add(btnVisualEnemyPartsAuto);

            var enemyAttachPanel = new Panel { Dock = DockStyle.Bottom, Height = 82, BackColor = Surface, Padding = new Padding(6, 2, 6, 2) };
            lblVisualEnemyAttachment = new Label { Text = "ATTACHMENT DEBUG • em12 Axe #616", Left = 7, Top = 3, Width = 270, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 7.8F) };
            cmbVisualEnemyAttachBone = new ComboBox { Left = 7, Top = 22, Width = 180, Height = 25, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbVisualEnemyAttachBone.SelectedIndexChanged += cmbVisualEnemyAttachBone_SelectedIndexChanged;
            NumericUpDown MakeAttachNud(int left, int top, decimal min, decimal max) { var n = new NumericUpDown { Left = left, Top = top, Width = 55, Height = 23, Minimum = min, Maximum = max, DecimalPlaces = 1, Increment = 1, BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle }; n.ValueChanged += visualEnemyAttachment_ValueChanged; return n; }
            nudVisualEnemyAttachX = MakeAttachNud(195, 22, -500, 500); nudVisualEnemyAttachY = MakeAttachNud(254, 22, -500, 500); nudVisualEnemyAttachZ = MakeAttachNud(313, 22, -500, 500);
            nudVisualEnemyAttachRX = MakeAttachNud(195, 50, -360, 360); nudVisualEnemyAttachRY = MakeAttachNud(254, 50, -360, 360); nudVisualEnemyAttachRZ = MakeAttachNud(313, 50, -360, 360);
            var lblAttachPos = new Label { Text = "Bone", Left = 7, Top = 51, Width = 36, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 7.5F) };
            var lblAttachXYZ = new Label { Text = "X       Y       Z", Left = 204, Top = 4, Width = 160, Height = 16, ForeColor = TextMuted, Font = new Font("Consolas", 7F) };
            var lblAttachRot = new Label { Text = "Rot", Left = 164, Top = 54, Width = 28, Height = 16, ForeColor = TextMuted, Font = new Font("Segoe UI", 7F) };
            enemyAttachPanel.Controls.Add(lblVisualEnemyAttachment); enemyAttachPanel.Controls.Add(cmbVisualEnemyAttachBone); enemyAttachPanel.Controls.Add(lblAttachPos); enemyAttachPanel.Controls.Add(lblAttachXYZ); enemyAttachPanel.Controls.Add(lblAttachRot);
            enemyAttachPanel.Controls.Add(nudVisualEnemyAttachX); enemyAttachPanel.Controls.Add(nudVisualEnemyAttachY); enemyAttachPanel.Controls.Add(nudVisualEnemyAttachZ); enemyAttachPanel.Controls.Add(nudVisualEnemyAttachRX); enemyAttachPanel.Controls.Add(nudVisualEnemyAttachRY); enemyAttachPanel.Controls.Add(nudVisualEnemyAttachRZ);

            pnlVisualEnemyModelParts.Controls.Add(clbVisualEnemyModelParts); pnlVisualEnemyModelParts.Controls.Add(enemyAttachPanel); pnlVisualEnemyModelParts.Controls.Add(enemyPartsButtons); pnlVisualEnemyModelParts.Controls.Add(lblVisualEnemyParts);
            tabEnemies.Controls.Add(enemyListHost); tabEnemies.Controls.Add(pnlVisualEnemyModelParts); tabEnemies.Controls.Add(chkVisualEnemyModelParts); tabEnemies.Controls.Add(chkVisualEnemyInactive); tabEnemies.Controls.Add(cmbVisualEnemyLocationFilter); tabEnemies.Controls.Add(enemyHint);

            ctxVisualAevEntries = new ContextMenuStrip { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var duplicateAevItem = new ToolStripMenuItem("Duplicate   Ctrl+D"); duplicateAevItem.Click += (_, _) => DuplicateSelectedAev();
            var deleteAevItem = new ToolStripMenuItem("Delete   Del"); deleteAevItem.Click += (_, _) => DeleteSelectedAev();
            ctxVisualAevEntries.Items.Add(duplicateAevItem); ctxVisualAevEntries.Items.Add(deleteAevItem);
            ctxVisualAevEntries.Opening += (_, _) => { bool hasSelection = lstVisualAevEntries.SelectedItem != null; duplicateAevItem.Enabled = hasSelection; deleteAevItem.Enabled = hasSelection; };
            lstVisualAevEntries.ContextMenuStrip = ctxVisualAevEntries;

            ctxVisualEnemyEntries = new ContextMenuStrip { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var focusEnemyItem = new ToolStripMenuItem("Focar na viewport   F"); focusEnemyItem.Click += (_, _) => FocusSelectedEnemy();
            ctxVisualEnemyEntries.Items.Add(focusEnemyItem);
            ctxVisualEnemyEntries.Opening += (_, _) => { focusEnemyItem.Enabled = lstVisualEnemyEntries.SelectedItems.Count > 0; };
            lstVisualEnemyEntries.ContextMenuStrip = ctxVisualEnemyEntries;

            workspace.Panel1.Controls.Add(tabVisualEntities); workspace.Panel1.Controls.Add(clbVisualLayers); workspace.Panel1.Controls.Add(layersTitle);

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Border, Margin = Padding.Empty, Padding = Padding.Empty, ColumnCount = 2, RowCount = 1 };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workspace.Panel2.Controls.Add(right);

            var viewportHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 10, 13), Margin = Padding.Empty, Padding = Padding.Empty };
            visualViewport = new ScenarioViewport { Dock = DockStyle.Fill, Margin = Padding.Empty };
            viewportHost.Controls.Add(visualViewport);
            right.Controls.Add(viewportHost, 0, 0);

            var propertiesHost = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(1, 0, 0, 0), Padding = Padding.Empty };
            lblVisualPropertiesTitle = new Label { Text = "PROPERTIES • SELECTION", Dock = DockStyle.Top, Height = 34, Padding = new Padding(12, 10, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) };
            pgVisualProperties = new PropertyGrid { Dock = DockStyle.Fill, HelpVisible = false, ToolbarVisible = false, PropertySort = PropertySort.Categorized, BackColor = Surface, ViewBackColor = Surface, ViewForeColor = TextPrimary, ViewBorderColor = Border, LineColor = Border, CategoryForeColor = TextMuted };
            pgVisualProperties.PropertyValueChanged += pgVisualProperties_PropertyValueChanged;
            propertiesHost.Controls.Add(pgVisualProperties);
            propertiesHost.Controls.Add(lblVisualPropertiesTitle);
            right.Controls.Add(propertiesHost, 1, 0);

            right.SizeChanged += (_, _) =>
            {
                float inspector = right.ClientSize.Width < 850 ? 260F : 310F;
                if (right.ColumnStyles[1].Width != inspector) right.ColumnStyles[1].Width = inspector;
            };

            visualMoveSpeed_Scroll(null, EventArgs.Empty);
            visualLookSpeed_Scroll(null, EventArgs.Empty);
            ResizeVisualEditor();
        }

        private void BuildToolsDesigner()
        {
            AddPageHeader(pnlTools, "Ferramentas", "Executáveis externos usados pelo pipeline. Texturas agora podem ser editadas nativamente.");
            var box = Card(0, 76, 882, 360); pnlTools.Controls.Add(box);
            txtIsoAfs = ToolRow(box, "ISOAFS", 16, out btnBrowseIsoAfs, out btnOpenIsoAfs); btnBrowseIsoAfs.Click += btnBrowseIsoAfs_Click; btnOpenIsoAfs.Click += btnOpenIsoAfs_Click;
            txtDatTool = ToolRow(box, "DAT Tool", 94, out btnBrowseDatTool, out btnOpenDatTool); btnBrowseDatTool.Click += btnBrowseDatTool_Click; btnOpenDatTool.Click += btnOpenDatTool_Click;
            txtTplManager = ToolRow(box, "TPL Manager (opcional)", 172, out btnBrowseTpl, out btnOpenTpl); btnBrowseTpl.Click += btnBrowseTpl_Click; btnOpenTpl.Click += btnOpenTpl_Click;
            txtPcsx2 = ToolRow(box, "PCSX2", 250, out btnBrowsePcsx2, out btnOpenPcsx2); btnBrowsePcsx2.Click += btnBrowsePcsx2_Click; btnOpenPcsx2.Click += btnOpenPcsx2_Click;
        }

        private TextBox ToolRow(Panel parent, string label, int y, out Button browse, out Button open)
        {
            parent.Controls.Add(new Label { Text = label, Left = 16, Top = y, Width = 220, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            var t = new TextBox { Left = 16, Top = y + 22, Width = 640, Height = 30, BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            browse = new Button { Left = 670, Top = y + 20 }; SetupSecondary(browse, "...", 46);
            open = new Button { Left = 728, Top = y + 20 }; SetupSecondary(open, "ABRIR", 106);
            parent.Controls.Add(t); parent.Controls.Add(browse); parent.Controls.Add(open); return t;
        }

        private void BuildLogsDesigner()
        {
            AddPageHeader(pnlLogs, "Console", "Logs de extração e build separados do restante da interface.");
            var split = new SplitContainer { Left = 0, Top = 70, Width = 882, Height = 632, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, Orientation = Orientation.Horizontal, SplitterDistance = 300, BackColor = Border, BorderStyle = BorderStyle.None };
            split.Panel1.BackColor = Surface; split.Panel2.BackColor = Surface;
            rtbExtractLog = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 11, 14), ForeColor = Color.FromArgb(183, 190, 200), BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Consolas", 9F), Text = "[Workspace] Pronto.\n" }; split.Panel1.Controls.Add(rtbExtractLog);
            var lblExtractLog = new Label { Text = "EXTRAÇÃO / WORKSPACE", Dock = DockStyle.Top, Height = 28, Padding = new Padding(10, 7, 0, 0), ForeColor = TextMuted, BackColor = Surface, Font = new Font("Segoe UI Semibold", 8F) }; split.Panel1.Controls.Add(lblExtractLog); lblExtractLog.BringToFront();
            rtbBuildLog = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 11, 14), ForeColor = Color.FromArgb(183, 190, 200), BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Consolas", 9F), Text = "[Workspace] Build pronto.\n" }; split.Panel2.Controls.Add(rtbBuildLog);
            var lblBuildLog = new Label { Text = "BUILD", Dock = DockStyle.Top, Height = 28, Padding = new Padding(10, 7, 0, 0), ForeColor = TextMuted, BackColor = Surface, Font = new Font("Segoe UI Semibold", 8F) }; split.Panel2.Controls.Add(lblBuildLog); lblBuildLog.BringToFront();
            pnlLogs.Controls.Add(split);
        }
    }
}
