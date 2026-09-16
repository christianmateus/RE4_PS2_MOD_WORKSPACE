namespace RE4_PS2_MOD_WORKSPACE
{
    partial class Form1
    {
        private System.ComponentModel.IContainer? components = null;
        private Panel pnlSidebar = null!, pnlTop = null!, pnlContent = null!;
        private Panel pnlDashboard = null!, pnlWorkspace = null!, pnlAssets = null!, pnlTextures = null!, pnlMessages = null!, pnlVisualEditor = null!, pnlCharacters = null!, pnlEnemies = null!, pnlAnimations = null!, pnlSounds = null!, pnlBuild = null!, pnlTools = null!, pnlSettings = null!, pnlLogs = null!;
        private Label lblLogo = null!, lblLogoSub = null!, lblVersion = null!, lblTopTitle = null!, lblWorkspaceCurrent = null!, lblTopVisualModified = null!;
        private Button btnNavDashboard = null!, btnNavWorkspace = null!, btnNavAssets = null!, btnNavTextures = null!, btnNavMessages = null!, btnNavVisualEditor = null!, btnNavCharacters = null!, btnNavEnemies = null!, btnNavAnimations = null!, btnNavSounds = null!, btnNavBuild = null!, btnNavTools = null!, btnNavSettings = null!, btnNavLogs = null!, btnSidebarToggle = null!, btnTopBuild = null!, btnTopSaveScenario = null!;
        private ComboBox cmbSoundFiles = null!, cmbSoundGroups = null!;
        private DataGridView gridSoundSamples = null!;
        private Button btnSoundBrowse = null!, btnSoundRefresh = null!, btnSoundPlay = null!, btnSoundStop = null!, btnSoundExportVag = null!, btnSoundExportWav = null!, btnSoundReplace = null!, btnSoundSave = null!, btnSoundRestore = null!, btnSoundExportAllWav = null!, btnSoundExportAllVag = null!, btnSoundImportFolder = null!, btnSoundZeroSelected = null!;
        private Label lblSoundFile = null!, lblSoundSummary = null!, lblSoundSelection = null!, lblSoundStatus = null!;
        private TrackBar trkSoundPosition = null!;
        private CheckBox chkSettingsSmdProtectIndices = null!, chkSettingsAutoSave = null!, chkSettingsShowFps = null!, chkSettingsStartMaximized = null!, chkSettingsCamTimeline = null!, chkSettingsCamProtectMotion = null!, chkSettingsCreateIsoBackup = null!;
        private ComboBox cmbSettingsAutoSaveInterval = null!, cmbSettingsLanguage = null!;
        private ComboBox cmbMessageLanguage = null!;
        private TextBox txtMessageSearch = null!;
        private CheckBox chkMessageFriendly = null!;
        private ListBox lstMessages = null!;
        private RichTextBox txtMessageEditor = null!;
        private Button btnMessageReload = null!, btnMessageSave = null!, btnMessagePrevious = null!, btnMessageNext = null!, btnMessageFindNext = null!;
        private Button btnHudPagePrevious = null!, btnHudPageNext = null!;
        private Label lblMessageFile = null!, lblMessageStatus = null!, lblMessageSelection = null!, lblMessageValidation = null!, lblMessageSize = null!, lblHudPage = null!;
        private HudMessagePreview messageHudPreview = null!;
        private ComboBox cmbEnemyFiles = null!, cmbEnemyTypeFriendly = null!, cmbEnemySubtypeFriendly = null!, cmbEnemyLocationFilter = null!, cmbVisualEnemyAttachBone = null!;
        private Button btnEnemyOpen = null!, btnEnemyReextract = null!, btnEnemySave = null!, btnEnemyRefresh = null!;
        private CheckBox chkEnemyActiveOnly = null!;
        private ListBox lstEnemyEntries = null!;
        private PropertyGrid pgEnemyProperties = null!;
        private Label lblEnemyFileInfo = null!, lblEnemyEntryCount = null!, lblEnemyStatus = null!;
        private ComboBox cmbAnimationFiles = null!;
        private Button btnAnimationBrowse = null!, btnAnimationRefresh = null!, btnAnimationExportSmd = null!, btnAnimationImportSmd = null!;
        private Button btnAnimationLabOpenDat = null!, btnAnimationLabAutoDat = null!, btnAnimationLabFit = null!, btnAnimationLabPlay = null!, btnAnimationLabStop = null!;
        private Button btnAnimationCatalogSave = null!;
        private CheckBox chkAnimationLabSkeleton = null!, chkAnimationLabRestPose = null!;
        private Label lblAnimationFile = null!, lblAnimationSummary = null!, lblAnimationStatus = null!, lblAnimationTrackDetail = null!, lblAnimationLabModel = null!, lblAnimationLabFrame = null!;
        private DataGridView gridAnimationTracks = null!, gridAnimationKeys = null!;
        private TabControl tabAnimationAxis = null!, tabAnimationView = null!;
        private TrackBar trkAnimationLabFrame = null!, trkAnimationLabBackground = null!;
        private TextBox txtAnimationCatalogName = null!;
        private ScenarioViewport animationLabViewport = null!;
        private Label lblCardWorkspaceValue = null!, lblCardIsoValue = null!, lblCardDatValue = null!, lblCardStatusValue = null!;
        private TextBox txtWorkspacePath = null!, txtIsoPath = null!, txtTplManager = null!, txtPcsx2 = null!;
        private Button btnBrowseWorkspace = null!, btnCreateWorkspace = null!, btnOpenWorkspace = null!, btnBrowseIso = null!, btnDashboardWorkspace = null!;
        private Button btnBrowseTpl = null!, btnBrowsePcsx2 = null!, btnOpenTpl = null!, btnOpenPcsx2 = null!, btnSettingsRunSetup = null!;
        private ComboBox cmbAfsEntries = null!, cmbDatEntries = null!, cmbAssetType = null!, cmbTextureDat = null!, cmbTextureSmd = null!;
        private Button btnScanIso = null!, btnExtractScenario = null!, btnExtractAllScenarios = null!, btnRestoreDat = null!, btnRefreshContent = null!, btnOpenContentFolder = null!;
        private CheckBox chkShowAllAfsFiles = null!;
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
        private ContextMenuStrip ctxAssets = null!;
        private ToolStripMenuItem miTextureExport = null!, miTextureReplace = null!, miTextureIncrease = null!, miTextureDecrease = null!;
        private Button btnBuildOneClick = null!, btnBuildRefreshChanges = null!, btnBuildRepackDat = null!, btnBuildInjectIso = null!, btnBuildRecreateIso = null!, btnBuildClean = null!, btnBuildResetWorkspace = null!, btnBuildOpenPcsx2 = null!, btnBuildFolder = null!, btnBuildAll = null!, btnBuildRefreshTracked = null!;
        private Label lblBuildActiveDat = null!, lblBuildDatStatus = null!, lblBuildIsoStatus = null!, lblBuildChangeStatus = null!, lblTrackedDatsSummary = null!, lblBuildBusy = null!;
        private Panel pnlBuildBusy = null!;
        private ProgressBar progressBuildBusy = null!;
        private Button btnVisualOpenScenario = null!, btnVisualOpenAev = null!, btnVisualFit = null!, btnVisualEditCns = null!;
        private Button btnVisualLayersToggle = null!, btnVisualPropertiesToggle = null!;
        private ComboBox cmbVisualDat = null!, cmbVisualEnemyAnimation = null!;
        private Button btnVisualSaveAev = null!, btnVisualSaveEsl = null!, btnVisualSaveEts = null!, btnVisualSaveSmd = null!, btnVisualSaveLit = null!, btnVisualExtractCore = null!, btnVisualAddCam = null!, btnVisualCamMove = null!, btnVisualCamScale = null!, btnVisualCamVertex = null!, btnVisualCamFace = null!, btnVisualCamFrame = null!, btnVisualCamPreview = null!, btnVisualCamCapture = null!, btnVisualCamPosition = null!, btnVisualCamTarget = null!;
        private Button btnVisualCollisionMoveXZ = null!, btnVisualCollisionMoveY = null!, btnVisualCollisionEdge = null!, btnVisualCollisionMoveFace = null!, btnVisualCollisionMoveSide = null!, btnVisualCollisionObject = null!, btnVisualCollisionRotateLeft = null!, btnVisualCollisionRotateRight = null!, btnVisualCollisionDuplicate = null!, btnVisualCollisionRemove = null!, btnVisualCollisionFlip = null!, btnVisualCollisionCube = null!, btnVisualCollisionSave = null!;
        private TrackBar trkVisualMoveSpeed = null!, trkVisualLookSpeed = null!;
        private Label lblVisualMoveSpeed = null!, lblVisualLookSpeed = null!;
        private CheckBox chkVisualAevLabels = null!, chkVisualEnemyLabels = null!, chkVisualEnemyInactive = null!, chkVisualEnemyModelParts = null!, chkVisualEnemySnap = null!, chkVisualEnemyAnimated = null!;
        private CheckBox chkVisualCollisionSat = null!, chkVisualCollisionEat = null!, chkVisualCollisionFloor = null!, chkVisualCollisionSlope = null!, chkVisualCollisionWall = null!;
        private ComboBox cmbVisualRenderMode = null!, cmbVisualCollisionStyle = null!, cmbVisualCollisionMesh = null!, cmbVisualCollisionVertex = null!;
        private TrackBar trkVisualCollisionOpacity = null!;
        private Panel pnlVisualContextActions = null!;
        private readonly Dictionary<int, Control> visualContextActionBars = new();
        private Label lblVisualCollisionInfo = null!;
        private Label lblVisualStage = null!, lblVisualStatus = null!, lblVisualFps = null!;
        private CheckedListBox clbVisualLayers = null!;
        private ListBox lstVisualAevEntries = null!, lstVisualEnemyEntries = null!, lstVisualObjectEntries = null!, lstVisualItaEntries = null!, lstVisualEseEntries = null!, lstVisualFseEntries = null!, lstVisualDseEntries = null!, lstVisualSmdEntries = null!, lstVisualLitEntries = null!, lstVisualEffEntries = null!, lstVisualRtpNodes = null!, lstVisualCamEntries = null!, lstVisualCamParts = null!;
        private PictureBox picVisualEffTexture = null!;
        private Label lblVisualEffTexture = null!;
        private NumericUpDown nudVisualEffFrame = null!;
        private ComboBox cmbVisualLitFile = null!, cmbVisualLitGroup = null!;
        private CheckedListBox clbVisualEnemyModelParts = null!;
        private Panel pnlVisualEnemyModelParts = null!;
        private Button btnVisualEnemyPartsSolo = null!, btnVisualEnemyPartsAll = null!, btnVisualEnemyPartsAuto = null!, btnVisualEnemyGizmoMove = null!, btnVisualEnemyGizmoRotate = null!;
        private Button btnVisualObjectMove = null!, btnVisualObjectRotate = null!;
        private Button btnVisualFseMove = null!, btnVisualFseVertex = null!, btnVisualFseFace = null!;
        private Button btnVisualSmdMove = null!, btnVisualSmdRotate = null!, btnVisualSmdScale = null!, btnVisualSmdImport = null!, btnVisualSmdDuplicate = null!, btnVisualSmdCatalog = null!;
        private CheckBox chkVisualSmdSnap = null!, chkVisualSmdEditMode = null!;
        private ComboBox cmbVisualSmdTransformSpeed = null!, cmbVisualSmdTransformSpace = null!;
        private CheckBox chkVisualObjectSnap = null!;
        private FlowLayoutPanel flpVisualObjectTextures = null!;
        private Label lblVisualObjectTextureDebug = null!;
        private Panel pnlVisualSmdTexturePreview = null!;
        private FlowLayoutPanel flpVisualSmdTextures = null!;
        private Label lblVisualSmdTextures = null!;
        private Label lblVisualEnemyParts = null!, lblVisualEnemyAttachment = null!;
        private NumericUpDown nudVisualEnemyAttachX = null!, nudVisualEnemyAttachY = null!, nudVisualEnemyAttachZ = null!, nudVisualEnemyAttachRX = null!, nudVisualEnemyAttachRY = null!, nudVisualEnemyAttachRZ = null!;
        private TabControl tabVisualEntities = null!, tabVisualSound = null!;
        private TabPage tabVisualRtp = null!;
        private CamTimelineControl visualCamTimeline = null!;
        private Label lblVisualPropertiesTitle = null!;
        private ComboBox cmbVisualAevTypeFilter = null!, cmbVisualEnemyLocationFilter = null!;
        private ContextMenuStrip ctxVisualAevEntries = null!, ctxVisualEnemyEntries = null!;
        private PropertyGrid pgVisualProperties = null!;
        private ScenarioViewport visualViewport = null!;
        private SplitContainer splitVisualWorkspace = null!;
        private TableLayoutPanel tblVisualWorkspaceRight = null!;
        private Panel pnlVisualPropertiesHost = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            if (disposing) autoSaveTimer.Dispose();
            if (disposing) visualSpeedSaveTimer.Dispose();
            if (disposing && picTexturePreview?.Image != null) picTexturePreview.Image.Dispose();
            if (disposing) activeHudFont?.Dispose();
            if (disposing) characterCustomizer?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            textureImages = new ImageList(components) { ImageSize = new Size(104, 104), ColorDepth = ColorDepth.Depth32Bit };
            pnlSidebar = new Panel(); pnlTop = new Panel(); pnlContent = new Panel();
            pnlDashboard = new Panel(); pnlWorkspace = new Panel(); pnlAssets = new Panel(); pnlTextures = new Panel(); pnlMessages = new Panel(); pnlVisualEditor = new Panel(); pnlCharacters = new Panel(); pnlEnemies = new Panel(); pnlAnimations = new Panel(); pnlSounds = new Panel(); pnlBuild = new Panel(); pnlTools = new Panel(); pnlSettings = new Panel(); pnlLogs = new Panel();
            lblLogo = new Label(); lblLogoSub = new Label(); lblVersion = new Label(); lblTopTitle = new Label();
            btnNavDashboard = new Button(); btnNavWorkspace = new Button(); btnNavAssets = new Button(); btnNavTextures = new Button(); btnNavMessages = new Button(); btnNavVisualEditor = new Button(); btnNavCharacters = new Button(); btnNavEnemies = new Button(); btnNavAnimations = new Button(); btnNavSounds = new Button(); btnNavBuild = new Button(); btnNavTools = new Button(); btnNavSettings = new Button(); btnNavLogs = new Button(); btnSidebarToggle = new Button(); btnTopBuild = new Button(); btnTopSaveScenario = new Button();
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
            SetupNav(btnNavMessages, "Mensagens", btnNavMessages_Click);
            SetupNav(btnNavVisualEditor, "Visual Editor", btnNavVisualEditor_Click);
            SetupNav(btnNavCharacters, "Personagens", btnNavCharacters_Click);
            SetupNav(btnNavEnemies, "Inimigos", btnNavEnemies_Click);
            SetupNav(btnNavAnimations, "Animações", btnNavAnimations_Click);
            SetupNav(btnNavSounds, "Sons", btnNavSounds_Click);
            SetupNav(btnNavBuild, "Build & Test", btnNavBuild_Click);
            SetupNav(btnNavTools, "Ferramentas", btnNavTools_Click);
            SetupNav(btnNavSettings, "Configurações", btnNavSettings_Click);
            SetupNav(btnNavLogs, "Console", btnNavLogs_Click);
            btnSidebarToggle.Text = "RETRAIR  ‹"; btnSidebarToggle.Dock = DockStyle.Top; btnSidebarToggle.Height = 34; btnSidebarToggle.FlatStyle = FlatStyle.Flat; btnSidebarToggle.FlatAppearance.BorderSize = 0; btnSidebarToggle.BackColor = Sidebar; btnSidebarToggle.ForeColor = TextMuted; btnSidebarToggle.Font = new Font("Segoe UI Semibold", 8F); btnSidebarToggle.TextAlign = ContentAlignment.MiddleRight; btnSidebarToggle.Cursor = Cursors.Hand; btnSidebarToggle.Click += btnSidebarToggle_Click;
            lblVersion.Text = "v0.7.0"; lblVersion.Dock = DockStyle.Bottom; lblVersion.Height = 26; lblVersion.ForeColor = TextMuted; lblVersion.TextAlign = ContentAlignment.MiddleLeft;
            pnlSidebar.Controls.Add(btnNavLogs); pnlSidebar.Controls.Add(btnNavSettings); pnlSidebar.Controls.Add(btnNavTools); pnlSidebar.Controls.Add(btnNavBuild); pnlSidebar.Controls.Add(btnNavSounds); pnlSidebar.Controls.Add(btnNavAnimations); pnlSidebar.Controls.Add(btnNavEnemies); pnlSidebar.Controls.Add(btnNavCharacters); pnlSidebar.Controls.Add(btnNavVisualEditor); pnlSidebar.Controls.Add(btnNavMessages); pnlSidebar.Controls.Add(btnNavTextures); pnlSidebar.Controls.Add(btnNavAssets); pnlSidebar.Controls.Add(btnNavWorkspace); pnlSidebar.Controls.Add(btnNavDashboard); pnlSidebar.Controls.Add(btnSidebarToggle); pnlSidebar.Controls.Add(lblLogoSub); pnlSidebar.Controls.Add(lblLogo); pnlSidebar.Controls.Add(lblVersion);

            pnlTop.BackColor = Bg;
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 58;
            pnlTop.Padding = new Padding(24, 10, 24, 8);
            lblTopTitle.UseMnemonic = false; lblTopTitle.Text = "Dashboard"; lblTopTitle.Dock = DockStyle.Left; lblTopTitle.Width = 430; lblTopTitle.Font = new Font("Segoe UI Semibold", 14F); lblTopTitle.TextAlign = ContentAlignment.MiddleLeft;
            var topActions = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 570, Height = 40, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Bg, Padding = Padding.Empty };
            SetupButton(btnTopBuild, "BUILD & TEST", Accent, 130); btnTopBuild.Height = 36; btnTopBuild.Margin = new Padding(8, 0, 0, 0); btnTopBuild.Click += btnTopBuild_Click;
            SetupButton(btnTopSaveScenario, "SALVAR CENÁRIO", Surface2, 142); btnTopSaveScenario.Height = 36; btnTopSaveScenario.Margin = new Padding(8, 0, 0, 0); btnTopSaveScenario.Visible = false; btnTopSaveScenario.Click += btnTopSaveScenario_Click;
            lblTopVisualModified = new Label { Text = "ARQUIVO MODIFICADO", AutoSize = false, Width = 158, Height = 36, TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.FromArgb(236, 180, 92), Font = new Font("Segoe UI Semibold", 8.5F), Visible = false, Margin = new Padding(0) };
            topActions.Controls.Add(btnTopBuild); topActions.Controls.Add(btnTopSaveScenario); topActions.Controls.Add(lblTopVisualModified);
            pnlTop.Controls.Add(topActions); pnlTop.Controls.Add(lblTopTitle);

            pnlContent.BackColor = Bg;
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(24, 10, 24, 20);
            foreach (Panel page in new[] { pnlDashboard, pnlWorkspace, pnlAssets, pnlTextures, pnlMessages, pnlVisualEditor, pnlCharacters, pnlEnemies, pnlAnimations, pnlSounds, pnlBuild, pnlTools, pnlSettings, pnlLogs }) SetupPage(page);
            BuildDashboardDesigner(); BuildProjectDesigner(); BuildAssetsDesigner(); BuildTexturesDesigner(); BuildMessagesDesigner(); BuildVisualEditorDesigner(); BuildEnemiesDesigner(); BuildAnimationsDesigner(); BuildSoundsDesigner(); BuildBuildDesigner(); BuildToolsDesigner(); BuildSettingsDesigner(); BuildLogsDesigner();
            pnlContent.Controls.Add(pnlLogs); pnlContent.Controls.Add(pnlSettings); pnlContent.Controls.Add(pnlTools); pnlContent.Controls.Add(pnlBuild); pnlContent.Controls.Add(pnlSounds); pnlContent.Controls.Add(pnlAnimations); pnlContent.Controls.Add(pnlEnemies); pnlContent.Controls.Add(pnlCharacters); pnlContent.Controls.Add(pnlVisualEditor); pnlContent.Controls.Add(pnlMessages); pnlContent.Controls.Add(pnlTextures); pnlContent.Controls.Add(pnlAssets); pnlContent.Controls.Add(pnlWorkspace); pnlContent.Controls.Add(pnlDashboard);

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
        private void SetupNav(Button b, string text, EventHandler handler) { b.UseMnemonic = false; b.Tag = text; b.Text = "  " + text; b.Dock = DockStyle.Top; b.Height = 42; b.Margin = new Padding(0, 0, 0, 2); b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Surface2; b.BackColor = Sidebar; b.ForeColor = TextMuted; b.TextAlign = ContentAlignment.MiddleLeft; b.Cursor = Cursors.Hand; b.Font = new Font("Segoe UI Semibold", 9F); b.Click += handler; }
        private void SetupButton(Button b, string text, Color color, int width) { b.UseMnemonic = false; b.Text = text; b.Width = width; b.Height = 34; b.BackColor = color; b.ForeColor = TextPrimary; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand; b.Font = new Font("Segoe UI Semibold", 8.7F); }
        private void SetupSecondary(Button b, string text, int width) { SetupButton(b, text, Surface2, width); b.FlatAppearance.BorderColor = Border; b.FlatAppearance.BorderSize = 1; }
        private Label AddPageHeader(Panel page, string title, string subtitle) { var a = new Label { UseMnemonic = false, Text = title, Left = 0, Top = 0, Width = 640, Height = 30, Font = new Font("Segoe UI Semibold", 19F), ForeColor = TextPrimary }; var b = new Label { UseMnemonic = false, Text = subtitle, Left = 1, Top = 33, Width = 900, Height = 24, ForeColor = TextMuted }; page.Controls.Add(a); page.Controls.Add(b); return a; }
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
            var main = Card(0, 76, 882, 198); pnlWorkspace.Controls.Add(main);
            txtWorkspacePath = PathBox(main, "WORKSPACE", 16, 850, out btnBrowseWorkspace); btnBrowseWorkspace.Click += btnBrowseWorkspace_Click;
            btnCreateWorkspace = new Button { Left = 16, Top = 82 }; SetupButton(btnCreateWorkspace, "CRIAR NOVO", Accent, 125); btnCreateWorkspace.Click += btnCreateWorkspace_Click; main.Controls.Add(btnCreateWorkspace);
            btnOpenWorkspace = new Button { Left = 151, Top = 82 }; SetupSecondary(btnOpenWorkspace, "ABRIR PASTA", 125); btnOpenWorkspace.Click += btnOpenWorkspace_Click; main.Controls.Add(btnOpenWorkspace);
            txtIsoPath = PathBox(main, "ISO BASE", 128, 850, out btnBrowseIso); btnBrowseIso.Click += btnBrowseIso_Click; txtIsoPath.Leave += txtIsoPath_Leave;
        }

        private void BuildAssetsDesigner()
        {
            AddPageHeader(pnlAssets, "Arquivos", "Explore todos os arquivos do AFS, extraia itens individuais ou abra o conteúdo de pacotes DAT.");
            var extract = Card(0, 70, 882, 164); pnlAssets.Controls.Add(extract);
            extract.Controls.Add(new Label { Text = "ARQUIVO AFS", Left = 16, Top = 8, Width = 160, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
            cmbAfsEntries = new ComboBox { Left = 16, Top = 34, Width = 360, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbAfsEntries.SelectedIndexChanged += cmbAfsEntries_SelectedIndexChanged; extract.Controls.Add(cmbAfsEntries);
            btnScanIso = new Button { Left = 388, Top = 32 }; SetupSecondary(btnScanIso, "LER ISO / AFS", 118); btnScanIso.Click += btnScanIso_Click; extract.Controls.Add(btnScanIso);
            extract.Controls.Add(new Label { Text = "ARQUIVO NO AFS", Left = 16, Top = 72, Width = 160, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
            cmbDatEntries = new ComboBox { Left = 16, Top = 94, Width = 360, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbDatEntries.SelectedIndexChanged += cmbDatEntries_SelectedIndexChanged; extract.Controls.Add(cmbDatEntries);
            btnExtractScenario = new Button { Left = 388, Top = 92 }; SetupButton(btnExtractScenario, "EXTRAIR DAT", Accent, 150); btnExtractScenario.Enabled = false; btnExtractScenario.Click += btnExtractScenario_Click; extract.Controls.Add(btnExtractScenario);
            btnExtractAllScenarios = new Button { Left = 548, Top = 92 }; SetupSecondary(btnExtractAllScenarios, "EXTRAIR CENÁRIOS", 168); btnExtractAllScenarios.Enabled = false; btnExtractAllScenarios.Click += btnExtractAllScenarios_Click; extract.Controls.Add(btnExtractAllScenarios);
            chkShowAllAfsFiles = new CheckBox { Text = "Mostrar todos", Left = 730, Top = 98, Width = 126, Height = 24, ForeColor = TextPrimary, BackColor = Surface, Checked = false }; chkShowAllAfsFiles.CheckedChanged += chkShowAllAfsFiles_CheckedChanged; extract.Controls.Add(chkShowAllAfsFiles);
            btnRestoreDat = new Button { Left = 730, Top = 126 }; SetupSecondary(btnRestoreDat, "RESTAURAR DAT", 126); btnRestoreDat.Height = 28; btnRestoreDat.Enabled = false; btnRestoreDat.Click += btnRestoreDat_Click; extract.Controls.Add(btnRestoreDat);
            extract.Controls.Add(new Label { Text = "Extrai o arquivo selecionado", Left = 388, Top = 128, Width = 150, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 7.8F), TextAlign = ContentAlignment.TopCenter });
            extract.Controls.Add(new Label { Text = "Lote r100.dat–r534.dat", Left = 548, Top = 128, Width = 168, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 7.8F), TextAlign = ContentAlignment.TopCenter });
            lblAfsName = new Label { Text = "AFS ainda não carregado", Left = 524, Top = 12, Width = 338, Height = 20, ForeColor = TextMuted, AutoEllipsis = true }; extract.Controls.Add(lblAfsName);
            StatLabel(extract, "TAMANHO", 524, 104, out lblDatCurrentSize); StatLabel(extract, "RESERVADO", 636, 104, out lblDatReservedSize); StatLabel(extract, "LIVRE", 748, 104, out lblDatFreeSpace);

            var toolbar = new Panel { Left = 0, Top = 246, Width = 882, Height = 66, BackColor = Bg }; pnlAssets.Controls.Add(toolbar);
            txtAssetSearch = new TextBox { Left = 0, Top = 25, Width = 300, Height = 30, PlaceholderText = "Filtrar por nome ou caminho...", BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle }; txtAssetSearch.TextChanged += assetFilter_Changed; toolbar.Controls.Add(txtAssetSearch);
            cmbAssetType = new ComboBox { Left = 312, Top = 25, Width = 135, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbAssetType.Items.AddRange(new object[] { "Todos", "SMD", "TPL", "BIN", "AEV", "ESL", "SND", "SEQ", "Outros" }); cmbAssetType.SelectedIndex = 0; cmbAssetType.SelectedIndexChanged += assetFilter_Changed; toolbar.Controls.Add(cmbAssetType);
            btnRefreshContent = new Button { Left = 584, Top = 23 }; SetupSecondary(btnRefreshContent, "ATUALIZAR", 110); btnRefreshContent.Click += btnRefreshContent_Click; toolbar.Controls.Add(btnRefreshContent);
            btnOpenContentFolder = new Button { Left = 706, Top = 23 }; SetupSecondary(btnOpenContentFolder, "ABRIR PASTA", 126); btnOpenContentFolder.Click += btnOpenContentFolder_Click; toolbar.Controls.Add(btnOpenContentFolder);
            lblContentSummary = new Label { Text = "Nenhum pacote DAT carregado.", Left = 0, Top = 2, Width = 540, Height = 20, ForeColor = TextMuted, AutoEllipsis = true }; toolbar.Controls.Add(lblContentSummary);

            var gridHost = new Panel { Left = 0, Top = 314, Width = 882, Height = 300, BackColor = Bg };
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
            gridAssets.SelectionChanged += gridAssets_SelectionChanged; gridAssets.CellDoubleClick += gridAssets_CellDoubleClick; gridAssets.CellFormatting += gridAssets_CellFormatting; gridAssets.ColumnHeaderMouseClick += gridAssets_ColumnHeaderMouseClick; gridAssets.CellMouseDown += gridAssets_CellMouseDown; gridHost.Controls.Add(gridAssets);
            ctxAssets = new ContextMenuStrip { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var replaceAsset = new ToolStripMenuItem("Substituir..."); replaceAsset.Click += replaceAsset_Click; ctxAssets.Items.Add(replaceAsset);
            var restoreAsset = new ToolStripMenuItem("Restaurar original"); restoreAsset.Click += restoreAsset_Click; ctxAssets.Items.Add(restoreAsset);
            ctxAssets.Opening += (_, _) =>
            {
                string selected = GetSelectedContentFile() ?? string.Empty;
                replaceAsset.Enabled = !string.IsNullOrWhiteSpace(selected) && File.Exists(selected);
                var originalDat = GetActiveOriginalDatPath(); restoreAsset.Enabled = !string.IsNullOrWhiteSpace(selected) && !string.IsNullOrWhiteSpace(originalDat) && File.Exists(originalDat);
            };
            gridAssets.ContextMenuStrip = ctxAssets;
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
            btnTextureReload = new Button { Left = 128, Top = 68 }; SetupSecondary(btnTextureReload, "RELER FONTE", 116); btnTextureReload.Click += btnTextureReload_Click; top.Controls.Add(btnTextureReload);
            btnTextureOpenExternal = new Button { Left = 254, Top = 68 }; SetupSecondary(btnTextureOpenExternal, "TPL MANAGER", 112); btnTextureOpenExternal.Click += btnTextureOpenExternal_Click; top.Controls.Add(btnTextureOpenExternal);
            top.Controls.Add(new Label { Text = "THUMB", Left = 392, Top = 12, Width = 58, Height = 18, ForeColor = TextMuted });
            trackTextureThumb = new TrackBar { Left = 448, Top = 3, Width = 180, Height = 38, Minimum = 64, Maximum = 160, TickFrequency = 16, Value = 104, AutoSize = false }; trackTextureThumb.ValueChanged += trackTextureThumb_ValueChanged; top.Controls.Add(trackTextureThumb);
            lblTextureThumbSize = new Label { Text = "104px", Left = 632, Top = 12, Width = 55, Height = 18, ForeColor = TextMuted }; top.Controls.Add(lblTextureThumbSize);
            top.Controls.Add(new Label { Text = "Todas as texturas SMD e EFF são exibidas juntas", Left = 392, Top = 54, Width = 330, Height = 18, ForeColor = TextMuted });

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


        private void BuildMessagesDesigner()
        {
            AddPageHeader(pnlMessages, "Mensagens", "Edite os textos do HUD contidos no MDT do DAT ativo.");

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 70, 0, 0), BackColor = Bg };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Surface, Padding = new Padding(12) };
            cmbMessageLanguage = new ComboBox { Left = 12, Top = 17, Width = 184, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbMessageLanguage.SelectedIndexChanged += cmbMessageLanguage_SelectedIndexChanged;
            chkMessageFriendly = new CheckBox { Left = 208, Top = 20, Width = 112, Height = 25, Text = "Modo amigável", Checked = true, ForeColor = TextPrimary, BackColor = Surface };
            chkMessageFriendly.CheckedChanged += chkMessageFriendly_CheckedChanged;
            txtMessageSearch = new TextBox { Left = 330, Top = 17, Width = 170, Height = 30, PlaceholderText = "Localizar texto...", BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtMessageSearch.TextChanged += txtMessageSearch_TextChanged;
            txtMessageSearch.KeyDown += txtMessageSearch_KeyDown;
            btnMessageFindNext = new Button { Top = 15, Width = 38, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupSecondary(btnMessageFindNext, "↓", 38); btnMessageFindNext.Click += (_, _) => FindNextMessage();
            btnMessageReload = new Button { Top = 15, Width = 98, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupSecondary(btnMessageReload, "RECARREGAR", 98); btnMessageReload.Click += btnMessageReload_Click;
            btnMessageSave = new Button { Top = 15, Width = 116, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupButton(btnMessageSave, "SALVAR MDT", Accent, 116); btnMessageSave.Click += btnMessageSave_Click;
            btnMessageSave.Left = 754; btnMessageReload.Left = 644; btnMessageFindNext.Left = 598;
            toolbar.Resize += (_, _) => { btnMessageSave.Left = toolbar.ClientSize.Width - 128; btnMessageReload.Left = btnMessageSave.Left - 110; btnMessageFindNext.Left = btnMessageReload.Left - 46; txtMessageSearch.Width = Math.Max(90, btnMessageFindNext.Left - txtMessageSearch.Left - 8); };
            toolbar.Controls.Add(cmbMessageLanguage); toolbar.Controls.Add(chkMessageFriendly); toolbar.Controls.Add(txtMessageSearch); toolbar.Controls.Add(btnMessageFindNext); toolbar.Controls.Add(btnMessageReload); toolbar.Controls.Add(btnMessageSave);

            var workspace = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(882, 516), SplitterDistance = 330, Panel1MinSize = 240, Panel2MinSize = 300, FixedPanel = FixedPanel.Panel1, BackColor = Border, BorderStyle = BorderStyle.None };
            workspace.Panel1.BackColor = Surface; workspace.Panel2.BackColor = Surface;
            lblMessageFile = new Label { Text = "MDT • nenhum arquivo carregado", Dock = DockStyle.Top, Height = 34, Padding = new Padding(10, 9, 6, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.3F), AutoEllipsis = true };
            lstMessages = new ListBox { Dock = DockStyle.Fill, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, IntegralHeight = false, Font = new Font("Segoe UI", 9F), HorizontalScrollbar = false };
            lstMessages.SelectedIndexChanged += lstMessages_SelectedIndexChanged;
            lblMessageStatus = new Label { Text = "Selecione um DAT extraído para começar.", Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(10, 7, 8, 0), ForeColor = TextMuted, BackColor = Surface2, Font = new Font("Segoe UI", 8F), AutoEllipsis = true };
            workspace.Panel1.Controls.Add(lstMessages); workspace.Panel1.Controls.Add(lblMessageStatus); workspace.Panel1.Controls.Add(lblMessageFile);

            var editorHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Surface };
            lblMessageSelection = new Label { Text = "SELECIONE UMA MENSAGEM", Left = 12, Top = 12, Width = 260, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) };
            btnMessagePrevious = new Button { Top = 6, Width = 38, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupSecondary(btnMessagePrevious, "‹", 38); btnMessagePrevious.Click += (_, _) => MoveMessageSelection(-1);
            btnMessageNext = new Button { Top = 6, Width = 38, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupSecondary(btnMessageNext, "›", 38); btnMessageNext.Click += (_, _) => MoveMessageSelection(1);
            btnMessageNext.Left = 498; btnMessagePrevious.Left = 454;
            editorHeader.Resize += (_, _) => { btnMessageNext.Left = editorHeader.ClientSize.Width - 46; btnMessagePrevious.Left = btnMessageNext.Left - 44; };
            editorHeader.Controls.Add(lblMessageSelection); editorHeader.Controls.Add(btnMessagePrevious); editorHeader.Controls.Add(btnMessageNext);
            txtMessageEditor = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 17, 21), ForeColor = TextPrimary, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12F), AcceptsTab = false, DetectUrls = false, EnableAutoDragDrop = false, WordWrap = true, Padding = new Padding(14), Enabled = false };
            txtMessageEditor.TextChanged += txtMessageEditor_TextChanged; txtMessageEditor.KeyDown += txtMessageEditor_KeyDown;
            var commandBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, BackColor = Surface2, Padding = new Padding(8, 7, 4, 4), WrapContents = true };
            foreach (var command in new[] { ("QUEBRA", "[line-break]"), ("NOVA PÁGINA", "[new-page]"), ("PAUSA", "[pause]"), ("OPÇÃO", "[option]"), ("INÍCIO", "[message-start]"), ("FIM", "[message-end]") })
            {
                var button = new Button { Height = 26, Margin = new Padding(2), Tag = command.Item2 }; SetupSecondary(button, command.Item1, command.Item1.Length > 8 ? 96 : 68); button.Font = new Font("Segoe UI Semibold", 7.5F); button.Click += messageCommand_Click; commandBar.Controls.Add(button);
            }
            var diagnostics = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Surface };
            lblMessageValidation = new Label { Text = "Aguardando mensagem", Left = 12, Top = 5, Width = 340, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F), AutoEllipsis = true };
            lblMessageSize = new Label { Text = "Tamanho: —", Left = 12, Top = 25, Width = 430, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 8F), AutoEllipsis = true };
            diagnostics.Controls.Add(lblMessageValidation); diagnostics.Controls.Add(lblMessageSize);
            var previewHost = new Panel { Dock = DockStyle.Bottom, Height = 270, BackColor = Color.FromArgb(8, 10, 13) };
            var previewHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Surface };
            previewHeader.Controls.Add(new Label { Text = "PRÉVIA DO HUD • common_p.fnt", Left = 10, Top = 7, Width = 230, Height = 17, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 7.8F) });
            btnHudPagePrevious = new Button { Top = 2, Width = 30, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupSecondary(btnHudPagePrevious, "‹", 30); btnHudPagePrevious.Click += (_, _) => messageHudPreview.MovePage(-1);
            btnHudPageNext = new Button { Top = 2, Width = 30, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right }; SetupSecondary(btnHudPageNext, "›", 30); btnHudPageNext.Click += (_, _) => messageHudPreview.MovePage(1);
            lblHudPage = new Label { Text = "1 / 1", Top = 6, Width = 58, Height = 18, TextAlign = ContentAlignment.MiddleCenter, ForeColor = TextMuted, Anchor = AnchorStyles.Top | AnchorStyles.Right, Font = new Font("Segoe UI", 8F) };
            btnHudPageNext.Left = 516; lblHudPage.Left = 454; btnHudPagePrevious.Left = 422;
            previewHeader.Resize += (_, _) => { btnHudPageNext.Left = previewHeader.ClientSize.Width - 36; lblHudPage.Left = btnHudPageNext.Left - 62; btnHudPagePrevious.Left = lblHudPage.Left - 32; };
            messageHudPreview = new HudMessagePreview { Dock = DockStyle.Fill }; messageHudPreview.PageChanged += (_, _) => UpdateHudPageUi(); messageHudPreview.ExtractFontRequested += btnExtractHudFont_Click;
            previewHeader.Controls.Add(btnHudPagePrevious); previewHeader.Controls.Add(lblHudPage); previewHeader.Controls.Add(btnHudPageNext);
            // Add Fill first and Top last so WinForms reserves the header instead of overlaying it.
            previewHost.Controls.Add(messageHudPreview); previewHost.Controls.Add(previewHeader);
            workspace.Panel2.Controls.Add(txtMessageEditor); workspace.Panel2.Controls.Add(previewHost); workspace.Panel2.Controls.Add(diagnostics); workspace.Panel2.Controls.Add(commandBar); workspace.Panel2.Controls.Add(editorHeader);
            body.Controls.Add(workspace); body.Controls.Add(toolbar); pnlMessages.Controls.Add(body); body.SendToBack();
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
            AddPageHeader(pnlAnimations, "Animações", "Inspector FCV e Laboratório 3D para animações de inimigos do Resident Evil 4 PS2.");
            var top = Card(0, 70, 882, 112); top.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlAnimations.Controls.Add(top);
            top.Controls.Add(new Label { Text = "ARQUIVO FCV", Left = 14, Top = 10, Width = 120, Height = 18, ForeColor = TextMuted });
            cmbAnimationFiles = new ComboBox { Left = 14, Top = 31, Width = 420, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbAnimationFiles.SelectedIndexChanged += cmbAnimationFiles_SelectedIndexChanged; top.Controls.Add(cmbAnimationFiles);
            btnAnimationRefresh = new Button { Left = 446, Top = 29 }; SetupSecondary(btnAnimationRefresh, "ATUALIZAR", 104); btnAnimationRefresh.Click += btnAnimationRefresh_Click; top.Controls.Add(btnAnimationRefresh);
            btnAnimationBrowse = new Button { Left = 562, Top = 29 }; SetupButton(btnAnimationBrowse, "ABRIR FCV", Accent, 112); btnAnimationBrowse.Click += btnAnimationBrowse_Click; top.Controls.Add(btnAnimationBrowse);
            btnAnimationExportSmd = new Button { Left = 686, Top = 29 }; SetupSecondary(btnAnimationExportSmd, "EXPORTAR SMD", 92); btnAnimationExportSmd.Click += btnAnimationExportSmd_Click; top.Controls.Add(btnAnimationExportSmd);
            btnAnimationImportSmd = new Button { Left = 786, Top = 29 }; SetupButton(btnAnimationImportSmd, "IMPORTAR SMD", Accent, 92); btnAnimationImportSmd.Click += btnAnimationImportSmd_Click; top.Controls.Add(btnAnimationImportSmd);
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
            var tabLaboratory = new TabPage("Laboratório 3D") { BackColor=Surface, ForeColor=TextPrimary };
            tabAnimationView.TabPages.Add(tabInspector); tabAnimationView.TabPages.Add(tabLaboratory);
            lblAnimationTrackDetail = new Label { Text = "Selecione um track", Left = 8, Top = 8, Width = 334, Height = 22, ForeColor = TextPrimary, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; tabInspector.Controls.Add(lblAnimationTrackDetail);
            tabAnimationAxis = new TabControl { Left = 8, Top = 36, Width = 334, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; tabAnimationAxis.TabPages.Add("X"); tabAnimationAxis.TabPages.Add("Y"); tabAnimationAxis.TabPages.Add("Z"); tabAnimationAxis.SelectedIndexChanged += tabAnimationAxis_SelectedIndexChanged; tabInspector.Controls.Add(tabAnimationAxis);
            gridAnimationKeys = new DataGridView { Left = 8, Top = 72, Width = 334, Height = 348, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackgroundColor = Surface, ForeColor = TextPrimary, GridColor = Border, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            gridAnimationKeys.ColumnHeadersDefaultCellStyle.BackColor = Surface2; gridAnimationKeys.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary; gridAnimationKeys.EnableHeadersVisualStyles = false; gridAnimationKeys.DefaultCellStyle.BackColor = Surface; gridAnimationKeys.DefaultCellStyle.ForeColor = TextPrimary; gridAnimationKeys.Columns.Add("Key", "#"); gridAnimationKeys.Columns.Add("Frame", "Frame"); gridAnimationKeys.Columns.Add("Value", "Value"); gridAnimationKeys.Columns.Add("In", "Tangent In"); gridAnimationKeys.Columns.Add("Out", "Tangent Out"); gridAnimationKeys.Columns.Add("Extra", "Extra"); tabInspector.Controls.Add(gridAnimationKeys);
            lblAnimationStatus = new Label { Text = "Abra um FCV para iniciar a análise.", Left = 8, Top = 428, Width = 334, Height = 28, ForeColor = TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right }; tabInspector.Controls.Add(lblAnimationStatus);


            var labToolbar = new FlowLayoutPanel { Dock=DockStyle.Top,Height=39,Padding=new Padding(7,4,4,3),BackColor=Surface,WrapContents=false };
            btnAnimationLabOpenDat = new Button(); SetupButton(btnAnimationLabOpenDat,"ABRIR DAT",Accent,104); btnAnimationLabOpenDat.Click += btnAnimationLabOpenDat_Click; labToolbar.Controls.Add(btnAnimationLabOpenDat);
            btnAnimationLabAutoDat = new Button(); SetupSecondary(btnAnimationLabAutoDat,"ÚLTIMO / AUTO",116); btnAnimationLabAutoDat.Click += btnAnimationLabAutoDat_Click; labToolbar.Controls.Add(btnAnimationLabAutoDat);
            btnAnimationLabFit = new Button(); SetupSecondary(btnAnimationLabFit,"ENQUADRAR",106); btnAnimationLabFit.Click += btnAnimationLabFit_Click; labToolbar.Controls.Add(btnAnimationLabFit);
            chkAnimationLabSkeleton = new CheckBox { Text="Esqueleto + bones",Checked=true,AutoSize=true,Margin=new Padding(12,5,3,0),ForeColor=TextPrimary,BackColor=Color.Transparent }; chkAnimationLabSkeleton.CheckedChanged += chkAnimationLabSkeleton_CheckedChanged; labToolbar.Controls.Add(chkAnimationLabSkeleton);
            chkAnimationLabRestPose = new CheckBox { Text="Pose base",Checked=false,AutoSize=true,Margin=new Padding(12,5,3,0),ForeColor=TextPrimary,BackColor=Color.Transparent }; chkAnimationLabRestPose.CheckedChanged += chkAnimationLabRestPose_CheckedChanged; labToolbar.Controls.Add(chkAnimationLabRestPose);
            labToolbar.Controls.Add(new Label { Text="FUNDO",AutoSize=true,Margin=new Padding(14,8,2,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",7.5F) });
            trkAnimationLabBackground = new TrackBar { Width=110,Height=28,Minimum=0,Maximum=100,Value=24,TickStyle=TickStyle.None,Margin=new Padding(0,1,3,0) }; trkAnimationLabBackground.Scroll += trkAnimationLabBackground_Scroll; labToolbar.Controls.Add(trkAnimationLabBackground);
            tabLaboratory.Controls.Add(labToolbar);

            lblAnimationLabModel = new Label { Text="Modelo: carregue o em22.dat do Colmillo",Dock=DockStyle.Top,Height=25,Padding=new Padding(9,4,4,0),ForeColor=TextMuted,AutoEllipsis=true }; tabLaboratory.Controls.Add(lblAnimationLabModel); lblAnimationLabModel.BringToFront(); labToolbar.BringToFront();
            var catalogBar = new Panel { Dock=DockStyle.Top,Height=38,BackColor=Surface2,Padding=new Padding(8,5,8,5) };
            catalogBar.Controls.Add(new Label { Text="NOME",Left=8,Top=10,Width=48,Height=18,ForeColor=TextMuted });
            txtAnimationCatalogName = new TextBox { Left=58,Top=6,Width=190,Height=25,BackColor=Surface,ForeColor=TextPrimary,BorderStyle=BorderStyle.FixedSingle,PlaceholderText="Ex.: Caminhando" }; txtAnimationCatalogName.KeyDown += txtAnimationCatalogName_KeyDown; catalogBar.Controls.Add(txtAnimationCatalogName);
            btnAnimationCatalogSave = new Button { Left=254,Top=4 }; SetupButton(btnAnimationCatalogSave,"SALVAR NOME",Accent,108); btnAnimationCatalogSave.Click += btnAnimationCatalogSave_Click; catalogBar.Controls.Add(btnAnimationCatalogSave);
            void ResizeAnimationCatalogBar(){int buttonWidth=108;btnAnimationCatalogSave.SetBounds(Math.Max(154,catalogBar.ClientSize.Width-buttonWidth-8),4,buttonWidth,29);txtAnimationCatalogName.Width=Math.Max(80,btnAnimationCatalogSave.Left-txtAnimationCatalogName.Left-7);}
            catalogBar.Resize += (_,_) => ResizeAnimationCatalogBar(); ResizeAnimationCatalogBar();
            tabLaboratory.Controls.Add(catalogBar);
            var labPlayback = new Panel { Dock=DockStyle.Bottom,Height=72,BackColor=Surface2,Padding=new Padding(8,5,8,4) }; tabLaboratory.Controls.Add(labPlayback);
            btnAnimationLabPlay = new Button { Left=8,Top=5 }; SetupButton(btnAnimationLabPlay,"PLAY",Accent,72); btnAnimationLabPlay.Click += btnAnimationPlay_Click; labPlayback.Controls.Add(btnAnimationLabPlay);
            btnAnimationLabStop = new Button { Left=86,Top=5 }; SetupSecondary(btnAnimationLabStop,"STOP",72); btnAnimationLabStop.Click += btnAnimationStop_Click; labPlayback.Controls.Add(btnAnimationLabStop);
            lblAnimationLabFrame = new Label { Text="Frame 0 / —",Left=168,Top=12,Width=174,Height=20,TextAlign=ContentAlignment.MiddleRight,ForeColor=TextPrimary,Anchor=AnchorStyles.Top|AnchorStyles.Right }; labPlayback.Controls.Add(lblAnimationLabFrame);
            trkAnimationLabFrame = new TrackBar { Left=8,Top=36,Width=334,Height=30,Minimum=0,Maximum=1,TickStyle=TickStyle.None,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; trkAnimationLabFrame.Scroll += trkAnimationLabFrame_Scroll; labPlayback.Controls.Add(trkAnimationLabFrame);
            animationLabViewport = new ScenarioViewport { Dock=DockStyle.Fill,ScenarioVisible=false,AevVisible=false,ObjectsVisible=false,EnemiesVisible=true,ShowEnemyLabels=false,RenderMode=ScenarioRenderMode.Solid,BackgroundBrightness=24 }; tabLaboratory.Controls.Add(animationLabViewport); animationLabViewport.BringToFront(); catalogBar.BringToFront(); lblAnimationLabModel.BringToFront(); labToolbar.BringToFront(); labPlayback.BringToFront();

            // O Laboratório 3D usa toda a largura disponível. A grade de tracks continua no layout
            // dividido apenas enquanto o Inspector está selecionado. Isso deixa o GL viewport
            // realmente grande, em vez de espremido na coluna da direita.
            void ResizeAnimationLayout()
            {
                int width = Math.Max(760, pnlAnimations.ClientSize.Width);
                int height = Math.Max(300, pnlAnimations.ClientSize.Height - 194);
                bool viewerMode = tabAnimationView != null && tabAnimationView.SelectedIndex != 0;
                if (viewerMode)
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

        private void BuildSoundsDesigner()
        {
            AddPageHeader(pnlSounds, "Sons", "Explore, ouça, exporte e substitua bancos SND e áudio VAG do PlayStation 2.");
            var source = Card(0, 70, 980, 112); source.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; pnlSounds.Controls.Add(source);
            source.Controls.Add(new Label { Text = "BANCO SND", Left = 14, Top = 10, Width = 120, Height = 18, ForeColor = TextMuted });
            cmbSoundFiles = new ComboBox { Left = 14, Top = 31, Width = 438, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbSoundFiles.SelectedIndexChanged += cmbSoundFiles_SelectedIndexChanged; source.Controls.Add(cmbSoundFiles);
            btnSoundRefresh = new Button { Left = 464, Top = 29 }; SetupSecondary(btnSoundRefresh, "ATUALIZAR", 104); btnSoundRefresh.Click += btnSoundRefresh_Click; source.Controls.Add(btnSoundRefresh);
            btnSoundBrowse = new Button { Left = 580, Top = 29 }; SetupButton(btnSoundBrowse, "ABRIR SND", Accent, 112); btnSoundBrowse.Click += btnSoundBrowse_Click; source.Controls.Add(btnSoundBrowse);
            btnSoundSave = new Button { Left = 704, Top = 29, Enabled = false }; SetupButton(btnSoundSave, "SALVAR SND", Color.FromArgb(36, 130, 92), 120); btnSoundSave.Click += btnSoundSave_Click; source.Controls.Add(btnSoundSave);
            btnSoundRestore = new Button { Left = 836, Top = 29, Enabled = false }; SetupSecondary(btnSoundRestore, "RESTAURAR ORIGINAL", 144); btnSoundRestore.Click += btnSoundRestore_Click; source.Controls.Add(btnSoundRestore);
            lblSoundFile = new Label { Text = "Nenhum banco aberto", Left = 14, Top = 72, Width = 260, Height = 20, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 9F) }; source.Controls.Add(lblSoundFile);
            lblSoundSummary = new Label { Text = "Grupos: —    Áudios: —", Left = 280, Top = 72, Width = 680, Height = 20, ForeColor = TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; source.Controls.Add(lblSoundSummary);

            var body = new SplitContainer { Left = 0, Top = 194, Width = 980, Height = 508, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, SplitterDistance = 625, SplitterWidth = 12, BackColor = Bg, FixedPanel = FixedPanel.Panel2 }; pnlSounds.Controls.Add(body);
            body.Panel1.BackColor = Surface; body.Panel1.Padding = new Padding(14); body.Panel2.BackColor = Surface; body.Panel2.Padding = new Padding(14);
            var groupBar = new Panel { Left = 14, Top = 14, Width = 583, Height = 82, BackColor = Surface, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; body.Panel1.Controls.Add(groupBar);
            groupBar.Controls.Add(new Label { Text = "GRUPO", Left = 0, Top = 9, Width = 52, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
            cmbSoundGroups = new ComboBox { Left = 58, Top = 5, Width = 220, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat }; cmbSoundGroups.SelectedIndexChanged += cmbSoundGroups_SelectedIndexChanged; groupBar.Controls.Add(cmbSoundGroups);
            btnSoundExportAllWav = new Button { Left = 0, Top = 43 }; SetupSecondary(btnSoundExportAllWav, "TODAS WAV", 102); btnSoundExportAllWav.Height = 30; btnSoundExportAllWav.Click += btnSoundExportAllWav_Click; groupBar.Controls.Add(btnSoundExportAllWav);
            btnSoundExportAllVag = new Button { Left = 110, Top = 43 }; SetupSecondary(btnSoundExportAllVag, "TODAS VAG", 102); btnSoundExportAllVag.Height = 30; btnSoundExportAllVag.Click += btnSoundExportAllVag_Click; groupBar.Controls.Add(btnSoundExportAllVag);
            btnSoundImportFolder = new Button { Left = 220, Top = 43 }; SetupSecondary(btnSoundImportFolder, "IMPORTAR PASTA", 134); btnSoundImportFolder.Height = 30; btnSoundImportFolder.Click += btnSoundImportFolder_Click; groupBar.Controls.Add(btnSoundImportFolder);
            btnSoundZeroSelected = new Button { Left = 362, Top = 43 }; SetupSecondary(btnSoundZeroSelected, "ZERAR SELEÇÃO", 126); btnSoundZeroSelected.Height = 30; btnSoundZeroSelected.Click += soundZeroMenu_Click; groupBar.Controls.Add(btnSoundZeroSelected);
            gridSoundSamples = new DataGridView { Left = 14, Top = 96, Width = 583, Height = 384, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BackgroundColor = Surface, ForeColor = TextPrimary, GridColor = Border, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            gridSoundSamples.ColumnHeadersDefaultCellStyle.BackColor = Surface2; gridSoundSamples.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary; gridSoundSamples.EnableHeadersVisualStyles = false; gridSoundSamples.DefaultCellStyle.BackColor = Surface; gridSoundSamples.DefaultCellStyle.ForeColor = TextPrimary; gridSoundSamples.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 38, 41); gridSoundSamples.DefaultCellStyle.SelectionForeColor = TextPrimary;
            var favoriteColumn = new DataGridViewButtonColumn { Name = "Favorite", HeaderText = "★", Width = 38, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Ordenar por favoritos" }; gridSoundSamples.Columns.Add(favoriteColumn);
            var playColumn = new DataGridViewButtonColumn { Name = "Play", HeaderText = "", Text = "▶", UseColumnTextForButtonValue = true, Width = 38, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat }; gridSoundSamples.Columns.Add(playColumn);
            gridSoundSamples.Columns.Add("Index", "Faixa"); gridSoundSamples.Columns.Add("Duration", "Duração"); gridSoundSamples.Columns.Add("Rate", "Frequência"); gridSoundSamples.Columns.Add("Size", "Tamanho"); gridSoundSamples.Columns.Add("Loop", "Loop"); gridSoundSamples.Columns.Add("Note", "Anotação"); gridSoundSamples.Columns.Add("State", "Estado"); gridSoundSamples.SelectionChanged += gridSoundSamples_SelectionChanged; gridSoundSamples.CellContentClick += gridSoundSamples_CellContentClick; gridSoundSamples.CellDoubleClick += gridSoundSamples_CellDoubleClick; gridSoundSamples.SortCompare += gridSoundSamples_SortCompare; body.Panel1.Controls.Add(gridSoundSamples);
            var soundMenu = new ContextMenuStrip(components) { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var soundReplaceMenu = new ToolStripMenuItem("Substituir por WAV/VAG") { ForeColor = TextPrimary }; soundReplaceMenu.Click += btnSoundReplace_Click;
            var soundExportWavMenu = new ToolStripMenuItem("Exportar WAV") { ForeColor = TextPrimary }; soundExportWavMenu.Click += btnSoundExportWav_Click;
            var soundExportVagMenu = new ToolStripMenuItem("Exportar VAG") { ForeColor = TextPrimary }; soundExportVagMenu.Click += btnSoundExportVag_Click;
            var soundFavoriteMenu = new ToolStripMenuItem("Alternar favorito") { ForeColor = TextPrimary }; soundFavoriteMenu.Click += soundFavoriteMenu_Click;
            var soundNoteMenu = new ToolStripMenuItem("Editar anotação...") { ForeColor = TextPrimary }; soundNoteMenu.Click += soundNoteMenu_Click;
            var soundZeroMenu = new ToolStripMenuItem("Zerar") { ForeColor = TextPrimary }; soundZeroMenu.Click += soundZeroMenu_Click;
            soundMenu.Items.Add(soundReplaceMenu); soundMenu.Items.Add(new ToolStripSeparator()); soundMenu.Items.Add(soundExportWavMenu); soundMenu.Items.Add(soundExportVagMenu); soundMenu.Items.Add(new ToolStripSeparator()); soundMenu.Items.Add(soundFavoriteMenu); soundMenu.Items.Add(soundNoteMenu); soundMenu.Items.Add(new ToolStripSeparator()); soundMenu.Items.Add(soundZeroMenu); gridSoundSamples.ContextMenuStrip = soundMenu;
            soundMenu.Opening += (_, e) => e.Cancel = gridSoundSamples.SelectedRows.Count == 0;
            gridSoundSamples.MouseDown += gridSoundSamples_MouseDown;

            var right = body.Panel2;
            right.Controls.Add(new Label { Text = "FAIXA SELECIONADA", Left = 14, Top = 14, Width = 220, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            lblSoundSelection = new Label { Text = "Selecione um áudio à esquerda", Left = 14, Top = 41, Width = 292, Height = 54, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 11F), AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; right.Controls.Add(lblSoundSelection);
            var preview = new Panel { Left = 14, Top = 102, Width = 292, Height = 132, BackColor = Surface2, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; right.Controls.Add(preview);
            preview.Controls.Add(new Label { Text = "PRÉVIA PCM", Left = 12, Top = 10, Width = 100, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
            trkSoundPosition = new TrackBar { Left = 8, Top = 32, Width = 276, Height = 45, Minimum = 0, Maximum = 1000, TickStyle = TickStyle.None, Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; preview.Controls.Add(trkSoundPosition);
            btnSoundPlay = new Button { Left = 12, Top = 86 }; SetupButton(btnSoundPlay, "PLAY", Accent, 88); btnSoundPlay.Click += btnSoundPlay_Click; preview.Controls.Add(btnSoundPlay);
            btnSoundStop = new Button { Left = 108, Top = 86 }; SetupSecondary(btnSoundStop, "STOP", 88); btnSoundStop.Click += btnSoundStop_Click; preview.Controls.Add(btnSoundStop);
            right.Controls.Add(new Label { Text = "EXPORTAR", Left = 14, Top = 256, Width = 100, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            btnSoundExportWav = new Button { Left = 14, Top = 282 }; SetupSecondary(btnSoundExportWav, "EXPORTAR WAV", 141); btnSoundExportWav.Click += btnSoundExportWav_Click; right.Controls.Add(btnSoundExportWav);
            btnSoundExportVag = new Button { Left = 165, Top = 282 }; SetupSecondary(btnSoundExportVag, "EXPORTAR VAG", 141); btnSoundExportVag.Click += btnSoundExportVag_Click; right.Controls.Add(btnSoundExportVag);
            right.Controls.Add(new Label { Text = "EDIÇÃO", Left = 14, Top = 342, Width = 100, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) });
            btnSoundReplace = new Button { Left = 14, Top = 368 }; SetupButton(btnSoundReplace, "SUBSTITUIR POR WAV / VAG", Accent, 292); btnSoundReplace.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; btnSoundReplace.Click += btnSoundReplace_Click; right.Controls.Add(btnSoundReplace);
            lblSoundStatus = new Label { Text = "Abra um SND para começar.", Left = 14, Top = 424, Width = 292, Height = 64, ForeColor = TextMuted, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }; right.Controls.Add(lblSoundStatus);
            void ResizeSoundLayout() { source.Width = Math.Max(760, pnlSounds.ClientSize.Width); body.Width = source.Width; body.Height = Math.Max(300, pnlSounds.ClientSize.Height - 194); body.SplitterDistance = Math.Max(430, body.Width - 355); }
            pnlSounds.Resize += (_, _) => ResizeSoundLayout(); ResizeSoundLayout();
        }

        private void BuildBuildDesigner()
        {
            AddPageHeader(pnlBuild, "Build & Test", "Compile e injete DATs ou qualquer arquivo modificado do AFS na ISO de teste.");
            var hero = Card(0, 70, 882, 92); pnlBuild.Controls.Add(hero);
            lblBuildChangeStatus = new Label { UseMnemonic = false, Text = "Verificando alterações...", Left = 16, Top = 12, Width = 505, Height = 34, ForeColor = TextMuted, AutoEllipsis = true }; hero.Controls.Add(lblBuildChangeStatus);
            hero.Controls.Add(new Label { UseMnemonic = false, Text = "Selecione arquivos na tabela ou use o arquivo ativo; ao concluir, o PCSX2 será aberto.", Left = 16, Top = 52, Width = 505, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) });
            btnBuildRefreshChanges = new Button { Left = 530, Top = 28 }; SetupSecondary(btnBuildRefreshChanges, "ATUALIZAR", 112); btnBuildRefreshChanges.Click += btnBuildRefreshChanges_Click; hero.Controls.Add(btnBuildRefreshChanges);
            btnBuildOneClick = new Button { Left = 654, Top = 15 }; SetupButton(btnBuildOneClick, "BUILD & TEST\r\nSELEÇÃO / ATIVO", Accent, 204); btnBuildOneClick.Height = 60; btnBuildOneClick.Click += btnBuildOneClick_Click; hero.Controls.Add(btnBuildOneClick);

            pnlBuildBusy = new Panel { Left = 0, Top = 70, Width = 882, Height = 92, BackColor = Surface2, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lblBuildBusy = new Label { UseMnemonic = false, Text = "Processando...", Left = 18, Top = 17, Width = 840, Height = 25, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 11F), AutoEllipsis = true };
            progressBuildBusy = new ProgressBar { Left = 18, Top = 53, Width = 840, Height = 12, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 24 };
            pnlBuildBusy.Controls.Add(lblBuildBusy); pnlBuildBusy.Controls.Add(progressBuildBusy); pnlBuild.Controls.Add(pnlBuildBusy);

            lblTrackedDatsSummary = new Label { UseMnemonic = false, Text = "Carregando arquivos...", Left = 0, Top = 178, Width = 570, Height = 24, ForeColor = TextMuted }; pnlBuild.Controls.Add(lblTrackedDatsSummary);
            btnBuildRefreshTracked = new Button { Left = 600, Top = 172 }; SetupSecondary(btnBuildRefreshTracked, "ATUALIZAR LISTA", 130); btnBuildRefreshTracked.Click += btnBuildRefreshTracked_Click; pnlBuild.Controls.Add(btnBuildRefreshTracked);
            btnBuildAll = new Button { Left = 742, Top = 166 }; SetupButton(btnBuildAll, "BUILD ALL\r\nTODOS", Accent, 140); btnBuildAll.Height = 42; btnBuildAll.Click += btnBuildAll_Click; pnlBuild.Controls.Add(btnBuildAll);
            lvTrackedDats = new ListView { Left = 0, Top = 212, Width = 882, Height = 190, Anchor = AnchorStyles.Top | AnchorStyles.Left, View = View.Details, FullRowSelect = true, MultiSelect = true, HideSelection = false, BackColor = Surface, ForeColor = Color.FromArgb(220, 223, 230), BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9F) };
            lvTrackedDats.Columns.Add("Arquivo", 185); lvTrackedDats.Columns.Add("Tipo / origem", 165); lvTrackedDats.Columns.Add("Estado", 185); lvTrackedDats.Columns.Add("Alterações", 100); lvTrackedDats.Columns.Add("Último build", 165); lvTrackedDats.SelectedIndexChanged += lvTrackedDats_SelectedIndexChanged; lvTrackedDats.DoubleClick += lvTrackedDats_DoubleClick; pnlBuild.Controls.Add(lvTrackedDats);

            var actions = Card(0, 414, 882, 126); pnlBuild.Controls.Add(actions);
            lblBuildActiveDat = new Label { Text = "DAT ativo: nenhum", Left = 14, Top = 12, Width = 420, Height = 22, Font = new Font("Segoe UI Semibold", 9.5F) }; actions.Controls.Add(lblBuildActiveDat);
            lblBuildDatStatus = new Label { Text = "Extraia e edite um cenário antes de reconstruir.", Left = 14, Top = 37, Width = 820, Height = 22, ForeColor = TextMuted, AutoEllipsis = true }; actions.Controls.Add(lblBuildDatStatus);
            btnBuildRepackDat = new Button { Visible = false };
            btnBuildInjectIso = new Button { Visible = false };
            btnBuildOpenPcsx2 = new Button { Left = 14, Top = 73 }; SetupSecondary(btnBuildOpenPcsx2, "ABRIR PCSX2", 126); btnBuildOpenPcsx2.Click += btnBuildOpenPcsx2_Click; actions.Controls.Add(btnBuildOpenPcsx2);
            btnBuildRecreateIso = new Button { Left = 150, Top = 73 }; SetupSecondary(btnBuildRecreateIso, "RECRIAR LIMPA", 126); btnBuildRecreateIso.Click += btnBuildRecreateIso_Click; actions.Controls.Add(btnBuildRecreateIso);
            btnBuildClean = new Button { Left = 286, Top = 73 }; SetupSecondary(btnBuildClean, "LIMPAR BUILD", 120); btnBuildClean.Click += btnBuildClean_Click; actions.Controls.Add(btnBuildClean);
            btnBuildFolder = new Button { Left = 416, Top = 73 }; SetupSecondary(btnBuildFolder, "PASTA BUILD", 112); btnBuildFolder.Click += btnBuildFolder_Click; actions.Controls.Add(btnBuildFolder);
            btnBuildResetWorkspace = new Button { Left = 538, Top = 73 }; SetupButton(btnBuildResetWorkspace, "LIMPAR TUDO", Accent, 220); btnBuildResetWorkspace.Click += btnBuildResetWorkspace_Click; actions.Controls.Add(btnBuildResetWorkspace);
            lblBuildIsoStatus = new Label { UseMnemonic = false, Text = "Fast Build reutiliza a ISO existente.", Left = 0, Top = 554, Width = 850, Height = 22, ForeColor = TextMuted }; pnlBuild.Controls.Add(lblBuildIsoStatus);
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

            cmbVisualRenderMode = new ComboBox { Left = 210, Top = 10, Width = 150, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            cmbVisualRenderMode.Items.AddRange(new object[] { "Solid", "Solid + Wireframe", "Wireframe" });
            cmbVisualRenderMode.SelectedIndex = 0;
            cmbVisualRenderMode.SelectedIndexChanged += cmbVisualRenderMode_SelectedIndexChanged;
            toolbar.Controls.Add(cmbVisualRenderMode);

            btnVisualEditCns = new Button { Left = 370, Top = 10, Height = 24, Enabled = false };
            SetupButton(btnVisualEditCns, "EDITAR CNS", Accent, 116);
            btnVisualEditCns.Height = 24;
            btnVisualEditCns.Click += btnVisualEditCns_Click;
            toolbar.Controls.Add(btnVisualEditCns);

            // Kept as non-visual state holders because the save routines also use
            // their Enabled/Text state. Saving is exposed exclusively through Ctrl+S.
            btnVisualFit = new Button();
            btnVisualSaveAev = new Button { Enabled=false, Text="SAVE AEV" };
            btnVisualSaveEsl = new Button { Enabled=false, Text="SAVE ESL" };
            btnVisualSaveEts = new Button { Enabled=false, Text="SAVE ETS" };

            lblVisualStage = new Label { Text = "Nenhum DAT ativo", Left = 496, Top = 13, Width = 126, Height = 20, ForeColor = TextMuted, AutoEllipsis = true };
            toolbar.Controls.Add(lblVisualStage);
            lblVisualStatus = new Label { UseMnemonic = false, Text = "v0.7.0 • Visual Editor", Left = 628, Top = 10, Width = 242, Height = 27, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right, AutoEllipsis = false };
            toolbar.Controls.Add(lblVisualStatus);

            lblVisualMoveSpeed = new Label(); trkVisualMoveSpeed = new TrackBar { Minimum=10, Maximum=300, Value=100 };
            lblVisualLookSpeed = new Label { Text = "LOOK 1.00×", Left = 12, Top = 50, Width = 82, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) };
            toolbar.Controls.Add(lblVisualLookSpeed);
            trkVisualLookSpeed = new TrackBar { Left = 94, Top = 46, Width = 170, Height = 28, Minimum = 10, Maximum = 250, Value = 100, TickStyle = TickStyle.None, SmallChange = 5, LargeChange = 20 };
            trkVisualLookSpeed.Scroll += visualLookSpeed_Scroll;
            toolbar.Controls.Add(trkVisualLookSpeed);

            chkVisualAevLabels = new CheckBox { Text = "AEV LABELS", Left = 286, Top = 47, Width = 106, Height = 22, Checked = true, ForeColor = TextPrimary, BackColor = Surface, FlatStyle = FlatStyle.Standard, UseVisualStyleBackColor = false };
            chkVisualAevLabels.CheckedChanged += (_, _) => { if (visualViewport != null) { visualViewport.ShowAevLabels = chkVisualAevLabels.Checked; visualViewport.Invalidate(); } };
            toolbar.Controls.Add(chkVisualAevLabels);

            chkVisualEnemyLabels = new CheckBox { Text = "ENEMY LABELS", Left = 398, Top = 47, Width = 126, Height = 22, Checked = false, ForeColor = TextPrimary, BackColor = Surface, FlatStyle = FlatStyle.Standard, UseVisualStyleBackColor = false };
            chkVisualEnemyLabels.CheckedChanged += (_, _) => { settings.VisualEnemyLabels = chkVisualEnemyLabels.Checked; if (!restoringSession) SaveSettings(); if (visualViewport != null) { visualViewport.ShowEnemyLabels = chkVisualEnemyLabels.Checked; visualViewport.Invalidate(); } };
            toolbar.Controls.Add(chkVisualEnemyLabels);
            btnVisualLayersToggle=new Button{Left=536,Top=43,Height=27};SetupButton(btnVisualLayersToggle,"OCULTAR LAYERS",Surface2,132);btnVisualLayersToggle.Height=27;btnVisualLayersToggle.Click+=(_,_)=>ToggleVisualLayersPanel();toolbar.Controls.Add(btnVisualLayersToggle);
            btnVisualPropertiesToggle=new Button{Left=676,Top=43,Height=27};SetupButton(btnVisualPropertiesToggle,"OCULTAR PROPS",Surface2,132);btnVisualPropertiesToggle.Height=27;btnVisualPropertiesToggle.Click+=(_,_)=>ToggleVisualPropertiesPanel();toolbar.Controls.Add(btnVisualPropertiesToggle);
            var visualPanelTip=new ToolTip();visualPanelTip.SetToolTip(btnVisualLayersToggle,"Recolher ou mostrar Layers e abas");visualPanelTip.SetToolTip(btnVisualPropertiesToggle,"Recolher ou mostrar Propriedades");
            splitVisualWorkspace = new SplitContainer { Left = 0, Top = 166, Width = Math.Max(1, pnlVisualEditor.ClientSize.Width), Height = Math.Max(1, pnlVisualEditor.ClientSize.Height - 166), SplitterDistance = 210, FixedPanel = FixedPanel.Panel1, BackColor = Border, BorderStyle = BorderStyle.None };
            splitVisualWorkspace.Panel1.BackColor = Surface; splitVisualWorkspace.Panel2.BackColor = Bg; pnlVisualEditor.Controls.Add(splitVisualWorkspace);

            void ResizeVisualEditor()
            {
                int w = Math.Max(1, pnlVisualEditor.ClientSize.Width);
                int h = Math.Max(1, pnlVisualEditor.ClientSize.Height);
                toolbar.Width = w;
                splitVisualWorkspace.SetBounds(0, 166, w, Math.Max(1, h - 166));
                splitVisualWorkspace.PerformLayout();
                splitVisualWorkspace.Panel2.PerformLayout();
                visualViewport?.PerformLayout();
                visualViewport?.Invalidate();
            }
            pnlVisualEditor.Resize += (_, _) => ResizeVisualEditor();

            var layersTitle = new Label { Text = "LAYERS", Dock = DockStyle.Top, Height = 34, Padding = new Padding(12, 10, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) };
            clbVisualLayers = new CheckedListBox { Dock = DockStyle.Top, Height = 165, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, CheckOnClick = true, Font = new Font("Segoe UI", 9.5F), Padding = new Padding(8) };
            clbVisualLayers.Items.Add("Scenario", true); clbVisualLayers.Items.Add("AEV Events", true); clbVisualLayers.Items.Add("Enemies", false); clbVisualLayers.Items.Add("Objects", false); clbVisualLayers.Items.Add("Collision", false); clbVisualLayers.Items.Add("Lighting", true); clbVisualLayers.Items.Add("Effects", true); clbVisualLayers.Items.Add("RTP Routes", true); clbVisualLayers.Items.Add("CAM Cameras", true); clbVisualLayers.Items.Add("ITA Items", true); clbVisualLayers.Items.Add("Sound ESE/FSE", false); clbVisualLayers.ItemCheck += clbVisualLayers_ItemCheck;

            tabVisualEntities = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal, BackColor = Surface, ForeColor = TextPrimary, Padding = new Point(10, 4) };
            var tabAev = new TabPage("AEV") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabEnemies = new TabPage("INIMIGOS") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabObjects = new TabPage("OBJETOS") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabCollision = new TabPage("COLISÃO") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(7) };
            var tabSmd = new TabPage("SMD") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabLit = new TabPage("ILUMINAÇÃO") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabEff = new TabPage("EFEITOS") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            tabVisualRtp = new TabPage("RTP") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabCam = new TabPage("CAM") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabIta = new TabPage("ITENS") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            var tabSound = new TabPage("SOM") { BackColor = Surface, ForeColor = TextPrimary, Padding = new Padding(0) };
            tabVisualEntities.TabPages.Add(tabAev); tabVisualEntities.TabPages.Add(tabEnemies); tabVisualEntities.TabPages.Add(tabObjects); tabVisualEntities.TabPages.Add(tabCollision); tabVisualEntities.TabPages.Add(tabSmd); tabVisualEntities.TabPages.Add(tabLit); tabVisualEntities.TabPages.Add(tabEff); tabVisualEntities.TabPages.Add(tabVisualRtp); tabVisualEntities.TabPages.Add(tabCam); tabVisualEntities.TabPages.Add(tabIta); tabVisualEntities.TabPages.Add(tabSound);
            tabVisualEntities.SelectedIndexChanged += tabVisualEntities_SelectedIndexChanged;

            var rtpHint=new Label{Text="WAYPOINTS • Ctrl+clique seleciona 2 • Ctrl+L conecta",Dock=DockStyle.Top,Height=44,Padding=new Padding(8,7,5,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)};
            lstVisualRtpNodes=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface,ForeColor=TextPrimary,Font=new Font("Consolas",8.5F),IntegralHeight=false,SelectionMode=SelectionMode.MultiExtended};
            lstVisualRtpNodes.SelectedIndexChanged+=lstVisualRtpNodes_SelectedIndexChanged;lstVisualRtpNodes.KeyDown+=lstVisualRtpNodes_KeyDown;
            tabVisualRtp.Controls.Add(lstVisualRtpNodes);tabVisualRtp.Controls.Add(rtpHint);
            var rtpBar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=40,BackColor=Surface,Padding=new Padding(6,5,3,3),WrapContents=false};
            var btnRtpDuplicate=new Button();SetupButton(btnRtpDuplicate,"DUPLICAR",Surface2,82);btnRtpDuplicate.Click+=(_,_)=>DuplicateSelectedRtpNode();
            var btnRtpChild=new Button();SetupButton(btnRtpChild,"+ FILHO",Surface2,72);btnRtpChild.Click+=(_,_)=>CreateChildRtpNode();
            var btnRtpConnect=new Button();SetupButton(btnRtpConnect,"CONECTAR",Accent,82);btnRtpConnect.Click+=(_,_)=>ConnectSelectedRtpNodes();
            var btnRtpDelete=new Button();SetupButton(btnRtpDelete,"EXCLUIR",Surface2,72);btnRtpDelete.Click+=(_,_)=>DeleteSelectedRtpNode();rtpBar.Controls.Add(btnRtpConnect);rtpBar.Controls.Add(btnRtpDuplicate);rtpBar.Controls.Add(btnRtpChild);rtpBar.Controls.Add(btnRtpDelete);tabVisualRtp.Controls.Add(rtpBar);

            var camHint=new Label{Text="CAM • F foca • 1 mover • 2 escala • 3 vértice • 4 face • 5 frame",Dock=DockStyle.Top,Height=42,Padding=new Padding(8,7,5,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)};
            var camSplit=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=190,BackColor=Border};
            lstVisualCamEntries=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface,ForeColor=TextPrimary,Font=new Font("Consolas",8.5F),IntegralHeight=false};lstVisualCamEntries.SelectedIndexChanged+=lstVisualCamEntries_SelectedIndexChanged;lstVisualCamEntries.KeyDown+=lstVisualCamEntries_KeyDown;
            lstVisualCamParts=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface2,ForeColor=TextPrimary,Font=new Font("Consolas",8F),IntegralHeight=false};lstVisualCamParts.SelectedIndexChanged+=lstVisualCamParts_SelectedIndexChanged;
            var camBar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=136,BackColor=Surface,Padding=new Padding(5,5,2,2),WrapContents=true};btnVisualAddCam=new Button();SetupButton(btnVisualAddCam,"+ ADD CAMERA",Accent,116);btnVisualAddCam.Height=28;btnVisualAddCam.Enabled=false;btnVisualAddCam.Click+=btnVisualAddCam_Click;btnVisualCamMove=new Button();SetupButton(btnVisualCamMove,"MOVE (1)",Accent,72);btnVisualCamMove.Height=28;btnVisualCamMove.Click+=(_,_)=>SetCamGizmoMode(CamGizmoMode.Move);btnVisualCamScale=new Button();SetupButton(btnVisualCamScale,"SCALE (2)",Surface2,76);btnVisualCamScale.Height=28;btnVisualCamScale.Click+=(_,_)=>SetCamGizmoMode(CamGizmoMode.Scale);btnVisualCamVertex=new Button();SetupButton(btnVisualCamVertex,"VERTEX (3)",Surface2,82);btnVisualCamVertex.Height=28;btnVisualCamVertex.Click+=(_,_)=>SetCamGizmoMode(CamGizmoMode.Vertex);btnVisualCamFace=new Button();SetupButton(btnVisualCamFace,"FACE (4)",Surface2,72);btnVisualCamFace.Height=28;btnVisualCamFace.Click+=(_,_)=>SetCamGizmoMode(CamGizmoMode.Face);btnVisualCamFrame=new Button();SetupButton(btnVisualCamFrame,"FRAME (5)",Surface2,84);btnVisualCamFrame.Height=28;btnVisualCamFrame.Click+=(_,_)=>SetCamGizmoMode(CamGizmoMode.Frame);btnVisualCamPreview=new Button();SetupButton(btnVisualCamPreview,"PREVIEW CAMERA",Color.FromArgb(34,110,150),126);btnVisualCamPreview.Height=28;btnVisualCamPreview.Click+=btnVisualCamPreview_Click;btnVisualCamCapture=new Button();SetupButton(btnVisualCamCapture,"FRAME ← VIEW",Color.FromArgb(36,130,92),116);btnVisualCamCapture.Height=28;btnVisualCamCapture.Click+=btnVisualCamCapture_Click;btnVisualCamPosition=new Button();SetupButton(btnVisualCamPosition,"EDIT POSITION",Surface2,104);btnVisualCamPosition.Height=28;btnVisualCamPosition.Click+=(_,_)=>SelectVisualCamFramePoint(false);btnVisualCamTarget=new Button();SetupButton(btnVisualCamTarget,"EDIT TARGET",Surface2,96);btnVisualCamTarget.Height=28;btnVisualCamTarget.Click+=(_,_)=>SelectVisualCamFramePoint(true);camBar.Controls.Add(btnVisualAddCam);camBar.Controls.Add(btnVisualCamMove);camBar.Controls.Add(btnVisualCamScale);camBar.Controls.Add(btnVisualCamVertex);camBar.Controls.Add(btnVisualCamFace);camBar.Controls.Add(btnVisualCamFrame);camBar.Controls.Add(btnVisualCamPreview);camBar.Controls.Add(btnVisualCamCapture);camBar.Controls.Add(btnVisualCamPosition);camBar.Controls.Add(btnVisualCamTarget);
            camSplit.Panel1.Controls.Add(lstVisualCamEntries);camSplit.Panel2.Controls.Add(lstVisualCamParts);tabCam.Controls.Add(camSplit);tabCam.Controls.Add(camHint);
            var camMenu=new ContextMenuStrip{BackColor=Surface2,ForeColor=TextPrimary,ShowImageMargin=false};var duplicateCam=new ToolStripMenuItem("Duplicar camera");duplicateCam.Click+=(_,_)=>DuplicateSelectedCam();var deleteCam=new ToolStripMenuItem("Deletar camera");deleteCam.Click+=(_,_)=>DeleteSelectedCam();camMenu.Items.Add(duplicateCam);camMenu.Items.Add(deleteCam);camMenu.Opening+=(_,_)=>{bool selected=lstVisualCamEntries.SelectedItem!=null;duplicateCam.Enabled=selected;deleteCam.Enabled=selected&&lstVisualCamEntries.Items.Count>1;};lstVisualCamEntries.ContextMenuStrip=camMenu;

            var itaHint=new Label{Text="ITA • F foca • G move • R gira • Ctrl+D duplica • Del exclui",Dock=DockStyle.Top,Height=42,Padding=new Padding(8,7,5,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)};
            lstVisualItaEntries=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface,ForeColor=TextPrimary,Font=new Font("Consolas",8.5F),IntegralHeight=false};
            lstVisualItaEntries.SelectedIndexChanged+=lstVisualItaEntries_SelectedIndexChanged;lstVisualItaEntries.KeyDown+=lstVisualItaEntries_KeyDown;
            tabIta.Controls.Add(lstVisualItaEntries);tabIta.Controls.Add(itaHint);
            var itaBar=new FlowLayoutPanel{BackColor=Surface,Padding=new Padding(6,5,3,3),WrapContents=false};
            var btnItaMove=new Button();SetupButton(btnItaMove,"MOVE (G)",Accent,78);btnItaMove.Click+=(_,_)=>{visualViewport.ItaTransformMode=EtsGizmoMode.Move;visualViewport.RefreshItaGeometry();};
            var btnItaRotate=new Button();SetupButton(btnItaRotate,"ROTATE (R)",Surface2,88);btnItaRotate.Click+=(_,_)=>{visualViewport.ItaTransformMode=EtsGizmoMode.Rotate;visualViewport.RefreshItaGeometry();};
            var btnItaDuplicate=new Button();SetupButton(btnItaDuplicate,"DUPLICAR",Surface2,82);btnItaDuplicate.Click+=(_,_)=>DuplicateSelectedIta();
            var btnItaDelete=new Button();SetupButton(btnItaDelete,"EXCLUIR",Surface2,72);btnItaDelete.Click+=(_,_)=>DeleteSelectedIta();
            itaBar.Controls.Add(btnItaMove);itaBar.Controls.Add(btnItaRotate);itaBar.Controls.Add(btnItaDuplicate);itaBar.Controls.Add(btnItaDelete);

            tabVisualSound=new TabControl{Dock=DockStyle.Fill,BackColor=Surface,ForeColor=TextPrimary};tabVisualSound.SelectedIndexChanged+=(_,_)=>{bool fse=tabVisualSound.SelectedIndex==1;if(visualViewport!=null){visualViewport.FseEditingEnabled=fse;visualViewport.RefreshSoundGeometry();}if(btnVisualFseMove!=null){btnVisualFseMove.Visible=fse;btnVisualFseVertex.Visible=fse;btnVisualFseFace.Visible=fse;}};
            var eseTab=new TabPage("ESE • FONTES"){BackColor=Surface,ForeColor=TextPrimary};var fseTab=new TabPage("FSE • ZONAS"){BackColor=Surface,ForeColor=TextPrimary};var dseTab=new TabPage("DSE • PORTAS"){BackColor=Surface,ForeColor=TextPrimary};tabVisualSound.TabPages.Add(eseTab);tabVisualSound.TabPages.Add(fseTab);tabVisualSound.TabPages.Add(dseTab);
            lstVisualEseEntries=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface,ForeColor=TextPrimary,Font=new Font("Consolas",8.5F),IntegralHeight=false};lstVisualEseEntries.SelectedIndexChanged+=lstVisualEseEntries_SelectedIndexChanged;lstVisualEseEntries.KeyDown+=lstVisualEseEntries_KeyDown;
            lstVisualFseEntries=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface,ForeColor=TextPrimary,Font=new Font("Consolas",8.5F),IntegralHeight=false};lstVisualFseEntries.SelectedIndexChanged+=lstVisualFseEntries_SelectedIndexChanged;lstVisualFseEntries.KeyDown+=lstVisualFseEntries_KeyDown;
            lstVisualDseEntries=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Surface,ForeColor=TextPrimary,Font=new Font("Consolas",8.5F),IntegralHeight=false};lstVisualDseEntries.SelectedIndexChanged+=lstVisualDseEntries_SelectedIndexChanged;lstVisualDseEntries.KeyDown+=lstVisualDseEntries_KeyDown;
            var soundBar=new FlowLayoutPanel{BackColor=Surface,Padding=new Padding(6,5,3,3),WrapContents=false};
            btnVisualFseMove=new Button{Visible=false};SetupButton(btnVisualFseMove,"MOVE (1)",Accent,76);btnVisualFseMove.Click+=(_,_)=>SetFseGizmoMode(FseGizmoMode.Move);
            btnVisualFseVertex=new Button{Visible=false};SetupButton(btnVisualFseVertex,"VERTEX (2)",Surface2,86);btnVisualFseVertex.Click+=(_,_)=>SetFseGizmoMode(FseGizmoMode.Vertex);
            btnVisualFseFace=new Button{Visible=false};SetupButton(btnVisualFseFace,"FACE (3)",Surface2,74);btnVisualFseFace.Click+=(_,_)=>SetFseGizmoMode(FseGizmoMode.Face);
            var btnSoundAdd=new Button();SetupButton(btnSoundAdd,"+ NOVO",Accent,70);btnSoundAdd.Click+=(_,_)=>AddVisualSoundEntry(tabVisualSound.SelectedIndex);
            var btnSoundDuplicate=new Button();SetupButton(btnSoundDuplicate,"DUPLICAR",Surface2,82);btnSoundDuplicate.Click+=(_,_)=>DuplicateVisualSoundEntry(tabVisualSound.SelectedIndex);
            var btnSoundDelete=new Button();SetupButton(btnSoundDelete,"EXCLUIR",Surface2,72);btnSoundDelete.Click+=(_,_)=>DeleteVisualSoundEntry(tabVisualSound.SelectedIndex);
            soundBar.Controls.Add(btnVisualFseMove);soundBar.Controls.Add(btnVisualFseVertex);soundBar.Controls.Add(btnVisualFseFace);soundBar.Controls.Add(btnSoundAdd);soundBar.Controls.Add(btnSoundDuplicate);soundBar.Controls.Add(btnSoundDelete);
            eseTab.Controls.Add(lstVisualEseEntries);eseTab.Controls.Add(new Label{Text="FONTES SONORAS PONTUAIS • Ctrl+D duplica • Del exclui • Ctrl+S salva tudo",Dock=DockStyle.Top,Height=39,Padding=new Padding(7,7,2,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)});
            fseTab.Controls.Add(lstVisualFseEntries);fseTab.Controls.Add(new Label{Text="ZONAS DE AMBIENTE • arraste o centro • F foca • Ctrl+S salva tudo",Dock=DockStyle.Top,Height=39,Padding=new Padding(7,7,2,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)});
            dseTab.Controls.Add(lstVisualDseEntries);dseTab.Controls.Add(new Label{Text="SONS DE TRANSIÇÃO POR PORTA • edite a sala como rXXX e o DoorSE ID nas propriedades • Ctrl+S salva",Dock=DockStyle.Top,Height=52,Padding=new Padding(7,7,2,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)});tabSound.Controls.Add(tabVisualSound);

            var collisionTitle = new Label { Text="SAT / EAT INSPECTOR", Dock=DockStyle.Top, Height=27, ForeColor=TextMuted, Font=new Font("Segoe UI Semibold",8.5F), Padding=new Padding(2,6,0,0) };
            chkVisualCollisionSat = new CheckBox { Text="SAT • personagens", Dock=DockStyle.Top, Height=27, Checked=true, ForeColor=Color.FromArgb(255,128,58), BackColor=Surface };
            chkVisualCollisionEat = new CheckBox { Text="EAT • projéteis", Dock=DockStyle.Top, Height=27, Checked=true, ForeColor=Color.FromArgb(64,200,255), BackColor=Surface };
            var categoryTitle = new Label { Text="CATEGORIAS", Dock=DockStyle.Top, Height=25, ForeColor=TextMuted, Font=new Font("Segoe UI Semibold",8F), Padding=new Padding(2,7,0,0) };
            chkVisualCollisionFloor = new CheckBox { Text="Floor", Dock=DockStyle.Top, Height=25, Checked=true, ForeColor=TextPrimary, BackColor=Surface };
            chkVisualCollisionSlope = new CheckBox { Text="Slope", Dock=DockStyle.Top, Height=25, Checked=true, ForeColor=TextPrimary, BackColor=Surface };
            chkVisualCollisionWall = new CheckBox { Text="Wall", Dock=DockStyle.Top, Height=25, Checked=true, ForeColor=TextPrimary, BackColor=Surface };
            cmbVisualCollisionStyle = new ComboBox { Dock=DockStyle.Top, Height=28, DropDownStyle=ComboBoxStyle.DropDownList, BackColor=Surface2, ForeColor=TextPrimary, FlatStyle=FlatStyle.Flat };
            cmbVisualCollisionStyle.Items.AddRange(new object[]{"Solid + Wireframe","Solid","Wireframe"}); cmbVisualCollisionStyle.SelectedIndex=0;
            cmbVisualCollisionMesh = new ComboBox { Dock=DockStyle.Top, Height=28, DropDownStyle=ComboBoxStyle.DropDownList, BackColor=Surface2, ForeColor=TextPrimary, FlatStyle=FlatStyle.Flat };
            cmbVisualCollisionVertex = new ComboBox { Dock=DockStyle.Top, Height=28, DropDownStyle=ComboBoxStyle.DropDownList, BackColor=Surface2, ForeColor=TextPrimary, FlatStyle=FlatStyle.Flat, Enabled=false };
            cmbVisualCollisionVertex.Items.AddRange(new object[]{"V0","V1","V2"}); cmbVisualCollisionVertex.SelectedIndexChanged += cmbVisualCollisionVertex_SelectedIndexChanged;
            var collisionMoveBar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=140,BackColor=Surface,Padding=new Padding(0,3,0,2),WrapContents=true};
            btnVisualCollisionMoveXZ=new Button();SetupButton(btnVisualCollisionMoveXZ,"VÉRTICE",Accent,78);btnVisualCollisionMoveXZ.Height=28;btnVisualCollisionMoveXZ.Click+=(_,_)=>{if(visualCollisionMoveEdge)ToggleCollisionEdgeMove();if(visualCollisionMoveWholeFace)ToggleCollisionFaceMove();};
            btnVisualCollisionMoveY=new Button();SetupButton(btnVisualCollisionMoveY,"FACE",Surface2,72);btnVisualCollisionMoveY.Height=28;btnVisualCollisionMoveY.Click+=(_,_)=>{if(visualCollisionMoveEdge)ToggleCollisionEdgeMove();if(!visualCollisionMoveWholeFace)ToggleCollisionFaceMove();};
            btnVisualCollisionEdge=new Button();SetupButton(btnVisualCollisionEdge,"ARESTA",Surface2,68);btnVisualCollisionEdge.Height=28;btnVisualCollisionEdge.Click+=(_,_)=>ToggleCollisionEdgeMove();
            btnVisualCollisionMoveFace=new Button();SetupButton(btnVisualCollisionMoveFace,"REGIÃO",Surface2,72);btnVisualCollisionMoveFace.Height=28;btnVisualCollisionMoveFace.Visible=false;btnVisualCollisionMoveFace.Click+=(_,_)=>ToggleCollisionFaceMove();
            btnVisualCollisionMoveSide=new Button();SetupButton(btnVisualCollisionMoveSide,"LADO",Surface2,58);btnVisualCollisionMoveSide.Height=28;btnVisualCollisionMoveSide.Click+=(_,_)=>ToggleCollisionSideMove();
            btnVisualCollisionObject=new Button();SetupButton(btnVisualCollisionObject,"OBJETO",Surface2,68);btnVisualCollisionObject.Height=28;btnVisualCollisionObject.Click+=(_,_)=>ToggleCollisionObjectMove();
            btnVisualCollisionRotateLeft=new Button();SetupButton(btnVisualCollisionRotateLeft,"MOVER",Accent,70);btnVisualCollisionRotateLeft.Height=28;btnVisualCollisionRotateLeft.Click+=(_,_)=>SetCollisionGizmoMode(CollisionGizmoMode.Move);
            btnVisualCollisionRotateRight=new Button();SetupButton(btnVisualCollisionRotateRight,"ROTACIONAR",Surface2,94);btnVisualCollisionRotateRight.Height=28;btnVisualCollisionRotateRight.Click+=(_,_)=>SetCollisionGizmoMode(CollisionGizmoMode.Rotate);
            btnVisualCollisionDuplicate=new Button();SetupButton(btnVisualCollisionDuplicate,"DUPLICAR",Surface2,82);btnVisualCollisionDuplicate.Height=28;btnVisualCollisionDuplicate.Click+=btnVisualCollisionDuplicate_Click;
            btnVisualCollisionRemove=new Button();SetupButton(btnVisualCollisionRemove,"EXCLUIR",Surface2,78);btnVisualCollisionRemove.Height=28;btnVisualCollisionRemove.Click+=btnVisualCollisionRemove_Click;
            btnVisualCollisionFlip=new Button();SetupButton(btnVisualCollisionFlip,"INVERTER",Surface2,78);btnVisualCollisionFlip.Height=28;btnVisualCollisionFlip.Click+=(_,_)=>TransformCollisionObject(0,invert:true);
            btnVisualCollisionCube=new Button();SetupButton(btnVisualCollisionCube,"+ CRIAR",Accent,88);btnVisualCollisionCube.Height=28;btnVisualCollisionCube.Click+=btnVisualCollisionCube_Click;
            btnVisualCollisionSave=new Button();SetupButton(btnVisualCollisionSave,"SAVE",Surface2,60);btnVisualCollisionSave.Height=28;btnVisualCollisionSave.Click+=btnVisualCollisionSave_Click;btnVisualCollisionSave.Enabled=false;
            collisionMoveBar.Controls.Add(btnVisualCollisionMoveXZ);collisionMoveBar.Controls.Add(btnVisualCollisionMoveY);collisionMoveBar.Controls.Add(btnVisualCollisionEdge);collisionMoveBar.Controls.Add(btnVisualCollisionMoveFace);collisionMoveBar.Controls.Add(btnVisualCollisionMoveSide);collisionMoveBar.Controls.Add(btnVisualCollisionObject);collisionMoveBar.Controls.Add(btnVisualCollisionRotateLeft);collisionMoveBar.Controls.Add(btnVisualCollisionRotateRight);collisionMoveBar.Controls.Add(btnVisualCollisionDuplicate);collisionMoveBar.Controls.Add(btnVisualCollisionRemove);collisionMoveBar.Controls.Add(btnVisualCollisionFlip);collisionMoveBar.Controls.Add(btnVisualCollisionCube);
            trkVisualCollisionOpacity = new TrackBar { Dock=DockStyle.Top, Height=32, Minimum=5, Maximum=80, Value=22, TickStyle=TickStyle.None };
            lblVisualCollisionInfo = new Label { Text="Clique em uma face para inspecionar flags.", Dock=DockStyle.Fill, ForeColor=TextMuted, Padding=new Padding(2,8,2,0), AutoEllipsis=true };
            chkVisualCollisionSat.CheckedChanged += visualCollisionDisplay_Changed; chkVisualCollisionEat.CheckedChanged += visualCollisionDisplay_Changed; chkVisualCollisionFloor.CheckedChanged += visualCollisionDisplay_Changed; chkVisualCollisionSlope.CheckedChanged += visualCollisionDisplay_Changed; chkVisualCollisionWall.CheckedChanged += visualCollisionDisplay_Changed;
            cmbVisualCollisionStyle.SelectedIndexChanged += visualCollisionDisplay_Changed; cmbVisualCollisionMesh.SelectedIndexChanged += visualCollisionDisplay_Changed; trkVisualCollisionOpacity.Scroll += visualCollisionDisplay_Changed;
            tabCollision.Controls.Add(lblVisualCollisionInfo); tabCollision.Controls.Add(trkVisualCollisionOpacity); tabCollision.Controls.Add(cmbVisualCollisionVertex); tabCollision.Controls.Add(cmbVisualCollisionMesh); tabCollision.Controls.Add(cmbVisualCollisionStyle); tabCollision.Controls.Add(chkVisualCollisionWall); tabCollision.Controls.Add(chkVisualCollisionSlope); tabCollision.Controls.Add(chkVisualCollisionFloor); tabCollision.Controls.Add(categoryTitle); tabCollision.Controls.Add(chkVisualCollisionEat); tabCollision.Controls.Add(chkVisualCollisionSat); tabCollision.Controls.Add(collisionTitle);

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
            var enemyAnimationBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, BackColor = Surface, Padding = new Padding(7, 3, 3, 2), FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            enemyAnimationBar.Controls.Add(new Label { Text = "FCV debug", AutoSize = true, Margin = new Padding(0, 7, 7, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
            cmbVisualEnemyAnimation = new ComboBox { Width = 184, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Enabled = false };
            cmbVisualEnemyAnimation.SelectedIndexChanged += cmbVisualEnemyAnimation_SelectedIndexChanged;
            enemyAnimationBar.Controls.Add(cmbVisualEnemyAnimation);

            // MODEL PARTS is an optional reverse-engineering tool. Hidden by default in v0.4.7.
            chkVisualEnemyModelParts = new CheckBox { Text = "Mostrar Model Parts", Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 0, 0, 0), BackColor = Surface, ForeColor = TextMuted, Checked = false };
            chkVisualEnemyModelParts.CheckedChanged += chkVisualEnemyModelParts_CheckedChanged;
            var enemyListHost = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
            enemyListHost.Controls.Add(lstVisualEnemyEntries); enemyListHost.Controls.Add(enemyAnimationBar); enemyListHost.Controls.Add(transformHint);
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

            lstVisualObjectEntries = new ListBox { Dock = DockStyle.Fill, BackColor = Surface, ForeColor = TextPrimary, BorderStyle = BorderStyle.None, IntegralHeight = false, Font = new Font("Segoe UI", 9F), SelectionMode = SelectionMode.MultiExtended };
            lstVisualObjectEntries.SelectedIndexChanged += lstVisualObjectEntries_SelectedIndexChanged;
            lstVisualObjectEntries.KeyDown += lstVisualObjectEntries_KeyDown;
            var objectHint = new Label { Text = "1 mover • 2 rotacionar • F focar", Dock = DockStyle.Bottom, Height = 38, Padding = new Padding(8, 7, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) };
            var objectBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 68, BackColor = Surface, Padding = new Padding(5, 4, 2, 2), WrapContents = true };
            btnVisualObjectMove = new Button(); SetupButton(btnVisualObjectMove,"MOVER",Accent,78); btnVisualObjectMove.Height=27; btnVisualObjectMove.Click += (_,_)=>SetEtsGizmoMode(EtsGizmoMode.Move);
            btnVisualObjectRotate = new Button(); SetupButton(btnVisualObjectRotate,"ROTACIONAR",Surface2,96); btnVisualObjectRotate.Height=27; btnVisualObjectRotate.Click += (_,_)=>SetEtsGizmoMode(EtsGizmoMode.Rotate);
            chkVisualObjectSnap = new CheckBox { Text="Snap",AutoSize=true,Margin=new Padding(8,6,0,0),ForeColor=TextPrimary,BackColor=Surface }; chkVisualObjectSnap.CheckedChanged += (_,_)=>{if(visualViewport!=null)visualViewport.EtsSnapEnabled=chkVisualObjectSnap.Checked;};
            objectBar.Controls.Add(btnVisualObjectMove);objectBar.Controls.Add(btnVisualObjectRotate);objectBar.Controls.Add(chkVisualObjectSnap);
            var objectTextureDebugHost=new Panel{Dock=DockStyle.Bottom,Height=190,BackColor=Surface2,Padding=new Padding(5)};
            lblVisualObjectTextureDebug=new Label{Text="TEXTURE DEBUG • selecione um objeto",Dock=DockStyle.Top,Height=24,ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F),Padding=new Padding(3,3,0,0)};
            flpVisualObjectTextures=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Surface2,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Padding=new Padding(2)};
            objectTextureDebugHost.Controls.Add(flpVisualObjectTextures);objectTextureDebugHost.Controls.Add(lblVisualObjectTextureDebug);
            tabObjects.Controls.Add(lstVisualObjectEntries); tabObjects.Controls.Add(objectHint);tabObjects.Controls.Add(objectTextureDebugHost);

            lstVisualSmdEntries = new ListBox { Dock=DockStyle.Fill, BackColor=Surface, ForeColor=TextPrimary, BorderStyle=BorderStyle.None, IntegralHeight=false, Font=new Font("Segoe UI",9F), SelectionMode=SelectionMode.MultiExtended };
            lstVisualSmdEntries.SelectedIndexChanged += lstVisualSmdEntries_SelectedIndexChanged;
            lstVisualSmdEntries.KeyDown += lstVisualSmdEntries_KeyDown;
            var smdHint = new Label { Text="Selecione a malha • Edit Mode: Del exclui faces", Dock=DockStyle.Bottom, Height=38, Padding=new Padding(8,7,0,0), ForeColor=TextMuted, Font=new Font("Segoe UI Semibold",8F) };
            var smdBar = new FlowLayoutPanel { Dock=DockStyle.Top, Height=68, BackColor=Surface, Padding=new Padding(5,4,2,2), WrapContents=true };
            btnVisualSmdMove=new Button();SetupButton(btnVisualSmdMove,"MOVE",Accent,62);btnVisualSmdMove.Height=27;btnVisualSmdMove.Click+=(_,_)=>SetSmdGizmoMode(SmdGizmoMode.Move);
            btnVisualSmdRotate=new Button();SetupButton(btnVisualSmdRotate,"ROTATE",Surface2,68);btnVisualSmdRotate.Height=27;btnVisualSmdRotate.Click+=(_,_)=>SetSmdGizmoMode(SmdGizmoMode.Rotate);
            btnVisualSmdScale=new Button();SetupButton(btnVisualSmdScale,"SCALE",Surface2,62);btnVisualSmdScale.Height=27;btnVisualSmdScale.Click+=(_,_)=>SetSmdGizmoMode(SmdGizmoMode.Scale);
            chkVisualSmdSnap=new CheckBox{Text="Snap",AutoSize=true,Margin=new Padding(8,6,0,0),ForeColor=TextPrimary,BackColor=Surface};chkVisualSmdSnap.CheckedChanged+=(_,_)=>{if(visualViewport!=null)visualViewport.SmdSnapEnabled=chkVisualSmdSnap.Checked;};
            chkVisualSmdEditMode=new CheckBox{Text="EDIT MODE",AutoSize=true,Margin=new Padding(8,6,0,0),ForeColor=Color.FromArgb(255,170,55),BackColor=Surface};chkVisualSmdEditMode.CheckedChanged+=(_,_)=>{if(visualViewport!=null){visualViewport.SetSmdFaceEditMode(chkVisualSmdEditMode.Checked);if(chkVisualSmdEditMode.Checked)SetSmdGizmoMode(SmdGizmoMode.Move);}btnVisualSmdRotate.Enabled=!chkVisualSmdEditMode.Checked;btnVisualSmdScale.Enabled=!chkVisualSmdEditMode.Checked;};
            cmbVisualSmdTransformSpeed=new ComboBox{Width=72,Height=27,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface2,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat,Margin=new Padding(5,1,0,0)};cmbVisualSmdTransformSpeed.Items.AddRange(new object[]{"0.5×","1×","2×","4×","8×","16×"});cmbVisualSmdTransformSpeed.SelectedIndex=3;cmbVisualSmdTransformSpeed.SelectedIndexChanged+=(_,_)=>{if(visualViewport!=null)visualViewport.SmdTransformSpeed=cmbVisualSmdTransformSpeed.SelectedIndex switch{0=>.5f,1=>1f,2=>2f,3=>4f,4=>8f,5=>16f,_=>1f};};
            cmbVisualSmdTransformSpace=new ComboBox{Width=78,Height=27,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface2,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat,Margin=new Padding(5,1,0,0)};cmbVisualSmdTransformSpace.Items.AddRange(new object[]{"WORLD","LOCAL"});cmbVisualSmdTransformSpace.SelectedIndex=0;cmbVisualSmdTransformSpace.SelectedIndexChanged+=(_,_)=>{if(visualViewport!=null)visualViewport.SetSmdTransformSpace(cmbVisualSmdTransformSpace.SelectedIndex==1?SmdTransformSpace.Local:SmdTransformSpace.World);};
            btnVisualSaveSmd=new Button();SetupButton(btnVisualSaveSmd,"SAVE SMD",Surface2,92);btnVisualSaveSmd.Height=27;btnVisualSaveSmd.Enabled=false;btnVisualSaveSmd.Click+=btnVisualSaveSmd_Click;
            btnVisualSmdImport=new Button();SetupButton(btnVisualSmdImport,"IMPORT MODEL",Surface2,112);btnVisualSmdImport.Height=27;btnVisualSmdImport.Enabled=false;btnVisualSmdImport.Click+=btnVisualSmdImport_Click;
            btnVisualSmdDuplicate=new Button();SetupButton(btnVisualSmdDuplicate,"DUPLICATE",Surface2,88);btnVisualSmdDuplicate.Height=27;btnVisualSmdDuplicate.Enabled=false;btnVisualSmdDuplicate.Click+=(_,_)=>DuplicateSelectedSmdEntries();
            btnVisualSmdCatalog=new Button();SetupButton(btnVisualSmdCatalog,"CATÁLOGO",Color.FromArgb(77,67,122),86);btnVisualSmdCatalog.Height=27;btnVisualSmdCatalog.Click+=btnVisualSmdCatalog_Click;
            smdBar.Controls.Add(btnVisualSmdMove);smdBar.Controls.Add(btnVisualSmdRotate);smdBar.Controls.Add(btnVisualSmdScale);smdBar.Controls.Add(chkVisualSmdSnap);smdBar.Controls.Add(chkVisualSmdEditMode);smdBar.Controls.Add(cmbVisualSmdTransformSpace);smdBar.Controls.Add(cmbVisualSmdTransformSpeed);smdBar.Controls.Add(btnVisualSmdDuplicate);smdBar.Controls.Add(btnVisualSmdImport);smdBar.Controls.Add(btnVisualSmdCatalog);
            tabSmd.Controls.Add(lstVisualSmdEntries);tabSmd.Controls.Add(smdHint);
            var smdMenu=new ContextMenuStrip{BackColor=Surface2,ForeColor=TextPrimary,ShowImageMargin=false};var smdDuplicate=new ToolStripMenuItem("Duplicar   Ctrl+D");smdDuplicate.Click+=(_,_)=>DuplicateSelectedSmdEntries();var smdDelete=new ToolStripMenuItem("Excluir   Del");smdDelete.Click+=(_,_)=>DeleteSelectedSmdEntries();var smdTextures=new ToolStripMenuItem("Editar texturas deste modelo...");smdTextures.Click+=(_,_)=>EditSelectedSmdTextures();var smdToCatalog=new ToolStripMenuItem("Adicionar seleção ao catálogo...");smdToCatalog.Click+=(_,_)=>AddSelectedSmdToCatalog();var smdExportBin=new ToolStripMenuItem("Exportar modelo BIN...");smdExportBin.Click+=(_,_)=>ExportSelectedSmdBin();var smdExportObj=new ToolStripMenuItem("Exportar modelo OBJ + texturas...");smdExportObj.Click+=(_,_)=>ExportSelectedSmdObj();smdMenu.Items.Add(smdTextures);smdMenu.Items.Add(new ToolStripSeparator());smdMenu.Items.Add(smdToCatalog);smdMenu.Items.Add(smdExportBin);smdMenu.Items.Add(smdExportObj);smdMenu.Items.Add(new ToolStripSeparator());smdMenu.Items.Add(smdDuplicate);smdMenu.Items.Add(smdDelete);smdMenu.Opening+=(_,_)=>{bool any=lstVisualSmdEntries.SelectedItems.Count>0;bool one=lstVisualSmdEntries.SelectedItems.Count==1;smdTextures.Enabled=one;smdToCatalog.Enabled=any;smdExportBin.Enabled=one;smdExportObj.Enabled=one;smdDuplicate.Enabled=any;smdDelete.Enabled=any;};lstVisualSmdEntries.ContextMenuStrip=smdMenu;

            var litHint=new Label{Text="LIT • selecione um grupo e uma luz",Dock=DockStyle.Top,Height=27,Padding=new Padding(8,7,0,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)};
            cmbVisualLitFile=new ComboBox{Dock=DockStyle.Top,Height=30,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface2,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat};
            cmbVisualLitFile.SelectedIndexChanged+=cmbVisualLitFile_SelectedIndexChanged;
            cmbVisualLitGroup=new ComboBox{Dock=DockStyle.Top,Height=30,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface2,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat};
            cmbVisualLitGroup.SelectedIndexChanged+=cmbVisualLitGroup_SelectedIndexChanged;
            lstVisualLitEntries=new ListBox{Dock=DockStyle.Fill,BackColor=Surface,ForeColor=TextPrimary,BorderStyle=BorderStyle.None,IntegralHeight=false,Font=new Font("Segoe UI",9F)};
            lstVisualLitEntries.SelectedIndexChanged+=lstVisualLitEntries_SelectedIndexChanged;
            var litBar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=40,BackColor=Surface,Padding=new Padding(5,5,2,2),WrapContents=false};
            btnVisualSaveLit=new Button();SetupButton(btnVisualSaveLit,"SAVE LIT",Accent,92);btnVisualSaveLit.Height=28;btnVisualSaveLit.Enabled=false;btnVisualSaveLit.Click+=btnVisualSaveLit_Click;
            litBar.Controls.Add(btnVisualSaveLit);
            tabLit.Controls.Add(lstVisualLitEntries);tabLit.Controls.Add(cmbVisualLitGroup);tabLit.Controls.Add(cmbVisualLitFile);tabLit.Controls.Add(litHint);
            var effHint=new Label{Text="EFF • grupos Effect 0 / Effect 1",Dock=DockStyle.Top,Height=27,Padding=new Padding(8,7,0,0),ForeColor=TextMuted,Font=new Font("Segoe UI Semibold",8F)};
            lstVisualEffEntries=new ListBox{Dock=DockStyle.Fill,BackColor=Surface,ForeColor=TextPrimary,BorderStyle=BorderStyle.None,IntegralHeight=false,Font=new Font("Consolas",8.5F)};
            lstVisualEffEntries.SelectedIndexChanged+=lstVisualEffEntries_SelectedIndexChanged;
            var effBar=new FlowLayoutPanel{BackColor=Surface,Padding=new Padding(6,5,3,3),WrapContents=false};
            btnVisualExtractCore=new Button();SetupButton(btnVisualExtractCore,"EXTRAIR CORE.DAT",Surface2,132);btnVisualExtractCore.Height=28;btnVisualExtractCore.Click+=btnVisualExtractCore_Click;effBar.Controls.Add(btnVisualExtractCore);
            var effPreview=new Panel{Dock=DockStyle.Bottom,Height=196,BackColor=Surface2,Padding=new Padding(7)};
            picVisualEffTexture=new PictureBox{Left=7,Top=7,Width=150,Height=142,BackColor=Color.Black,SizeMode=PictureBoxSizeMode.Zoom};
            lblVisualEffTexture=new Label{Left=7,Top=151,Width=196,Height=38,ForeColor=TextMuted,Font=new Font("Segoe UI",7.5F),Text="Selecione um efeito",AutoEllipsis=true};
            nudVisualEffFrame=new NumericUpDown{Left=161,Top=7,Width=42,Height=26,Minimum=0,Maximum=0,BackColor=Surface,ForeColor=TextPrimary,Enabled=false};nudVisualEffFrame.ValueChanged+=nudVisualEffFrame_ValueChanged;
            effPreview.Controls.Add(picVisualEffTexture);effPreview.Controls.Add(lblVisualEffTexture);effPreview.Controls.Add(nudVisualEffFrame);
            tabEff.Controls.Add(lstVisualEffEntries);tabEff.Controls.Add(effPreview);tabEff.Controls.Add(effHint);
            var objectMenu = new ContextMenuStrip { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var duplicateObject = new ToolStripMenuItem("Duplicate   Ctrl+D"); duplicateObject.Click += (_, _) => DuplicateSelectedEtsObject();
            var deleteObject = new ToolStripMenuItem("Delete   Del"); deleteObject.Click += (_, _) => DeleteSelectedEtsObject();
            objectMenu.Items.Add(duplicateObject); objectMenu.Items.Add(deleteObject);
            objectMenu.Opening += (_, _) => { bool selected = lstVisualObjectEntries.SelectedItem != null; duplicateObject.Enabled = selected; deleteObject.Enabled = selected; };
            lstVisualObjectEntries.ContextMenuStrip = objectMenu;

            ctxVisualAevEntries = new ContextMenuStrip { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var duplicateAevItem = new ToolStripMenuItem("Duplicate   Ctrl+D"); duplicateAevItem.Click += (_, _) => DuplicateSelectedAev();
            var deleteAevItem = new ToolStripMenuItem("Delete   Del"); deleteAevItem.Click += (_, _) => DeleteSelectedAev();
            ctxVisualAevEntries.Items.Add(duplicateAevItem); ctxVisualAevEntries.Items.Add(deleteAevItem);
            ctxVisualAevEntries.Opening += (_, _) => { bool hasSelection = lstVisualAevEntries.SelectedItem != null; duplicateAevItem.Enabled = hasSelection; deleteAevItem.Enabled = hasSelection; };
            lstVisualAevEntries.ContextMenuStrip = ctxVisualAevEntries;
            var aevBar=new FlowLayoutPanel{BackColor=Surface,Padding=new Padding(6,5,3,3),WrapContents=false};
            var btnAevDuplicate=new Button();SetupButton(btnAevDuplicate,"DUPLICAR",Surface2,82);btnAevDuplicate.Click+=(_,_)=>DuplicateSelectedAev();
            var btnAevDelete=new Button();SetupButton(btnAevDelete,"EXCLUIR",Surface2,72);btnAevDelete.Click+=(_,_)=>DeleteSelectedAev();aevBar.Controls.Add(btnAevDuplicate);aevBar.Controls.Add(btnAevDelete);

            ctxVisualEnemyEntries = new ContextMenuStrip { BackColor = Surface2, ForeColor = TextPrimary, ShowImageMargin = false };
            var focusEnemyItem = new ToolStripMenuItem("Focar na viewport   F"); focusEnemyItem.Click += (_, _) => FocusSelectedEnemy();
            ctxVisualEnemyEntries.Items.Add(focusEnemyItem);
            ctxVisualEnemyEntries.Opening += (_, _) => { focusEnemyItem.Enabled = lstVisualEnemyEntries.SelectedItems.Count > 0; };
            lstVisualEnemyEntries.ContextMenuStrip = ctxVisualEnemyEntries;

            var rtpMenu=new ContextMenuStrip{BackColor=Surface2,ForeColor=TextPrimary,ShowImageMargin=false};
            var duplicateRtpItem=new ToolStripMenuItem("Duplicar waypoint   Ctrl+D");duplicateRtpItem.Click+=(_,_)=>DuplicateSelectedRtpNode();
            var childRtpItem=new ToolStripMenuItem("Criar waypoint filho   Ctrl+Shift+D");childRtpItem.Click+=(_,_)=>CreateChildRtpNode();
            var connectRtpItem=new ToolStripMenuItem("Conectar 2 waypoints   Ctrl+L");connectRtpItem.Click+=(_,_)=>ConnectSelectedRtpNodes();
            var deleteRtpItem=new ToolStripMenuItem("Deletar waypoint   Del");deleteRtpItem.Click+=(_,_)=>DeleteSelectedRtpNode();
            rtpMenu.Items.Add(connectRtpItem);rtpMenu.Items.Add(new ToolStripSeparator());rtpMenu.Items.Add(duplicateRtpItem);rtpMenu.Items.Add(childRtpItem);rtpMenu.Items.Add(new ToolStripSeparator());rtpMenu.Items.Add(deleteRtpItem);
            rtpMenu.Opening+=(_,_)=>{bool selected=lstVisualRtpNodes.SelectedItem!=null;connectRtpItem.Enabled=lstVisualRtpNodes.SelectedItems.Count==2;duplicateRtpItem.Enabled=selected;childRtpItem.Enabled=selected;deleteRtpItem.Enabled=selected;};
            lstVisualRtpNodes.ContextMenuStrip=rtpMenu;

            splitVisualWorkspace.Panel1.Controls.Add(tabVisualEntities); splitVisualWorkspace.Panel1.Controls.Add(clbVisualLayers); splitVisualWorkspace.Panel1.Controls.Add(layersTitle);

            tblVisualWorkspaceRight = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Border, Margin = Padding.Empty, Padding = Padding.Empty, ColumnCount = 2, RowCount = 1 };
            tblVisualWorkspaceRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblVisualWorkspaceRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310F));
            tblVisualWorkspaceRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            splitVisualWorkspace.Panel2.Controls.Add(tblVisualWorkspaceRight);

            var viewportHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 10, 13), Margin = Padding.Empty, Padding = Padding.Empty };
            visualViewport = new ScenarioViewport { Dock = DockStyle.Fill, Margin = Padding.Empty };
            viewportHost.Controls.Add(visualViewport);
            visualCamTimeline=new CamTimelineControl{Visible=true};visualCamTimeline.SelectionChanged+=SelectVisualCamTimelineFrames;visualCamTimeline.AddRequested+=AddVisualCamFrame;visualCamTimeline.RemoveRequested+=RemoveVisualCamFrame;viewportHost.Controls.Add(visualCamTimeline);visualCamTimeline.BringToFront();
            pnlVisualContextActions=new Panel{Dock=DockStyle.Top,Height=44,BackColor=Surface,Padding=Padding.Empty,Visible=false};
            viewportHost.Controls.Add(pnlVisualContextActions);
            lblVisualFps = new Label { Text="0 FPS", AutoSize=false, Width=68, Height=25, Top=52, Left=Math.Max(8, viewportHost.ClientSize.Width-76), Anchor=AnchorStyles.Top|AnchorStyles.Right, TextAlign=ContentAlignment.MiddleCenter, ForeColor=Color.White, BackColor=Color.FromArgb(210,12,17,23), Font=new Font("Segoe UI Semibold",9F), Visible=false, UseMnemonic=false };
            visualViewport.FpsUpdated += fps => { if (!lblVisualFps.IsDisposed) lblVisualFps.Text = $"{fps} FPS"; };
            viewportHost.Controls.Add(lblVisualFps);
            lblVisualFps.BringToFront();
            visualContextActionBars.Clear();visualContextActionBars[0]=aevBar;visualContextActionBars[1]=enemyGizmoBar;visualContextActionBars[2]=objectBar;visualContextActionBars[3]=collisionMoveBar;visualContextActionBars[4]=smdBar;visualContextActionBars[6]=effBar;visualContextActionBars[7]=rtpBar;visualContextActionBars[8]=camBar;visualContextActionBars[9]=itaBar;visualContextActionBars[10]=soundBar;
            UpdateVisualContextActions();
            tblVisualWorkspaceRight.Controls.Add(viewportHost, 0, 0);

            pnlVisualPropertiesHost = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(1, 0, 0, 0), Padding = Padding.Empty };
            lblVisualPropertiesTitle = new Label { Text = "PROPERTIES • SELECTION", Dock = DockStyle.Top, Height = 34, Padding = new Padding(12, 10, 0, 0), ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8.5F) };
            pgVisualProperties = new PropertyGrid { Dock = DockStyle.Fill, HelpVisible = false, ToolbarVisible = false, PropertySort = PropertySort.Categorized, BackColor = Surface, ViewBackColor = Surface, ViewForeColor = TextPrimary, ViewBorderColor = Border, LineColor = Border, CategoryForeColor = TextMuted };
            pgVisualProperties.PropertyValueChanged += pgVisualProperties_PropertyValueChanged;
            pnlVisualSmdTexturePreview = new Panel { Dock = DockStyle.Bottom, Height = 190, BackColor = Surface2, Padding = new Padding(5), Visible = false };
            lblVisualSmdTextures = new Label { Text = "TEXTURAS USADAS • selecione uma entry", Dock = DockStyle.Top, Height = 25, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F), Padding = new Padding(4, 4, 0, 0) };
            flpVisualSmdTextures = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Surface2, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(2) };
            pnlVisualSmdTexturePreview.Controls.Add(flpVisualSmdTextures); pnlVisualSmdTexturePreview.Controls.Add(lblVisualSmdTextures);
            pnlVisualPropertiesHost.Controls.Add(pgVisualProperties);
            pnlVisualPropertiesHost.Controls.Add(pnlVisualSmdTexturePreview);
            pnlVisualPropertiesHost.Controls.Add(lblVisualPropertiesTitle);
            tblVisualWorkspaceRight.Controls.Add(pnlVisualPropertiesHost, 1, 0);

            tblVisualWorkspaceRight.SizeChanged += (_, _) =>
            {
                if(settings.VisualPropertiesPanelCollapsed)return;
                float inspector = tblVisualWorkspaceRight.ClientSize.Width < 850 ? 260F : 310F;
                if (tblVisualWorkspaceRight.ColumnStyles[1].Width != inspector) tblVisualWorkspaceRight.ColumnStyles[1].Width = inspector;
            };

            visualLookSpeed_Scroll(null, EventArgs.Empty);
            ResizeVisualEditor();
        }

        private void BuildToolsDesigner()
        {
            AddPageHeader(pnlTools, "Ferramentas", "DAT, AFS e ISO são manipulados nativamente. Configure apenas as ferramentas opcionais.");
            var box = Card(0, 76, 882, 204); pnlTools.Controls.Add(box);
            txtTplManager = ToolRow(box, "TPL Manager (opcional)", 16, out btnBrowseTpl, out btnOpenTpl); btnBrowseTpl.Click += btnBrowseTpl_Click; btnOpenTpl.Click += btnOpenTpl_Click;
            txtPcsx2 = ToolRow(box, "PCSX2", 94, out btnBrowsePcsx2, out btnOpenPcsx2); btnBrowsePcsx2.Click += btnBrowsePcsx2_Click; btnOpenPcsx2.Click += btnOpenPcsx2_Click;
            var assetCard = Card(0, 298, 882, 112); pnlTools.Controls.Add(assetCard);
            assetCard.Controls.Add(new Label { Text = "EDITOR DE MODELOS DE OBJETOS", Left = 16, Top = 14, Width = 420, Height = 22, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 10F) });
            assetCard.Controls.Add(new Label { Text = "Edite modelos e texturas de armas, itens (ITM) e objetos interativos (ETM). Também abre com duplo clique na lista de Arquivos.", Left = 16, Top = 42, Width = 650, Height = 45, ForeColor = TextMuted });
            var openAssetEditor = new Button { Left = 690, Top = 34 }; SetupButton(openAssetEditor, "ABRIR EDITOR 3D", Accent, 160); openAssetEditor.Click += (_, _) => OpenAssetModelEditor(); assetCard.Controls.Add(openAssetEditor);
            var executableCard = Card(0, 428, 882, 126); pnlTools.Controls.Add(executableCard);
            executableCard.Controls.Add(new Label { Text = "EDITOR DE EXECUTÁVEL", Left = 16, Top = 14, Width = 420, Height = 22, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 10F) });
            executableCard.Controls.Add(new Label { Text = "Edite armas e limites no ELF do RE4 PS2 e injete o HUD de vida dos inimigos no SLPS debug. Compatível com SLUS, SLES e SLPS debug.", Left = 16, Top = 42, Width = 650, Height = 58, ForeColor = TextMuted });
            var openExecutableEditor = new Button { Left = 690, Top = 43 }; SetupButton(openExecutableEditor, "ABRIR EDITOR", Accent, 160); openExecutableEditor.Click += (_, _) => OpenExecutableEditor(); executableCard.Controls.Add(openExecutableEditor);
        }

        private void BuildSettingsDesigner()
        {
            AddPageHeader(pnlSettings,"Configurações","Preferências de segurança e comportamento dos editores.");
            var tabs=new TabControl{Left=0,Top=70,Width=882,Height=632,Anchor=AnchorStyles.Top|AnchorStyles.Left};
            void ResizeSettingsTabs()
            {
                // pnlSettings and the top action bar share the same 24 px page
                // margin. Leaving two pixels inside the page keeps the complete
                // native border visible while aligning with Build & Test.
                tabs.Width=Math.Max(320,pnlSettings.ClientSize.Width-2);
                tabs.Height=Math.Max(320,pnlSettings.ClientSize.Height-tabs.Top-2);
            }
            pnlSettings.SizeChanged+=(_,_)=>ResizeSettingsTabs();
            ResizeSettingsTabs();
            var generalTab=new TabPage("GERAL"){BackColor=Surface,ForeColor=TextPrimary,Padding=new Padding(18)};
            var generalCard=Card(18,18,790,330);generalCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            generalCard.Controls.Add(new Label{Text="SALVAMENTO AUTOMÁTICO",Left=18,Top=15,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});
            chkSettingsAutoSave=new CheckBox{Text="Ativar auto-save do cenário no Editor Visual",Left=18,Top=48,Width=420,Height=26,ForeColor=TextPrimary,BackColor=Surface2};
            chkSettingsAutoSave.CheckedChanged+=chkSettingsAutoSave_CheckedChanged;
            generalCard.Controls.Add(chkSettingsAutoSave);
            generalCard.Controls.Add(new Label{Text="Salvar a cada",Left=38,Top=91,Width=105,Height=26,TextAlign=ContentAlignment.MiddleLeft,ForeColor=TextMuted,BackColor=Surface2});
            cmbSettingsAutoSaveInterval=new ComboBox{Left=145,Top=90,Width=120,Height=28,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface2,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat,Enabled=false};
            cmbSettingsAutoSaveInterval.Items.AddRange(new object[]{"1 minuto","3 minutos","5 minutos","10 minutos"});
            cmbSettingsAutoSaveInterval.SelectedIndexChanged+=cmbSettingsAutoSaveInterval_SelectedIndexChanged;
            generalCard.Controls.Add(cmbSettingsAutoSaveInterval);
            generalCard.Controls.Add(new Label{Text="Quando houver um cenário carregado, todos os componentes modificados serão salvos no intervalo escolhido. Backups originais continuam preservados.",Left=38,Top=132,Width=710,Height=42,ForeColor=TextMuted,Font=new Font("Segoe UI",8.5F)});
            chkSettingsShowFps=new CheckBox{Text="Mostrar FPS no canto superior direito do viewport",Left=18,Top=190,Width=430,Height=26,ForeColor=TextPrimary,BackColor=Surface2};
            chkSettingsShowFps.CheckedChanged+=chkSettingsShowFps_CheckedChanged;
            generalCard.Controls.Add(chkSettingsShowFps);
            chkSettingsStartMaximized=new CheckBox{Text="Iniciar em tela maximizada",Left=470,Top=190,Width=285,Height=26,ForeColor=TextPrimary,BackColor=Surface2};
            chkSettingsStartMaximized.CheckedChanged+=chkSettingsStartMaximized_CheckedChanged;
            generalCard.Controls.Add(chkSettingsStartMaximized);
            generalCard.Controls.Add(new Label{Text="IDIOMA",Left=18,Top=232,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});
            generalCard.Controls.Add(new Label{Text="Idioma da interface",Left=38,Top=272,Width=180,Height=26,TextAlign=ContentAlignment.MiddleLeft,ForeColor=TextMuted,BackColor=Surface2});
            cmbSettingsLanguage=new ComboBox{Left=220,Top=270,Width=250,Height=28,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Surface2,ForeColor=TextPrimary,FlatStyle=FlatStyle.Flat};
            cmbSettingsLanguage.SelectedIndexChanged+=cmbSettingsLanguage_SelectedIndexChanged;
            generalCard.Controls.Add(cmbSettingsLanguage);
            generalTab.Controls.Add(generalCard);
            var setupCard=Card(18,366,790,126);setupCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            setupCard.Controls.Add(new Label{Text="CONFIGURAÇÃO INICIAL",Left=18,Top=15,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});
            setupCard.Controls.Add(new Label{Text="Troque a ISO, mova o workspace ou revise as ferramentas usando o assistente guiado.",Left=18,Top=48,Width=540,Height=42,ForeColor=TextMuted,Font=new Font("Segoe UI",8.5F)});
            btnSettingsRunSetup=new Button{Left=598,Top=43};SetupSecondary(btnSettingsRunSetup,"ABRIR ASSISTENTE",164);btnSettingsRunSetup.Click+=btnSettingsRunSetup_Click;setupCard.Controls.Add(btnSettingsRunSetup);
            generalTab.Controls.Add(setupCard);tabs.TabPages.Add(generalTab);
            var smdTab=new TabPage("SMD"){BackColor=Surface,ForeColor=TextPrimary,Padding=new Padding(18)};
            var card=Card(18,18,790,150);card.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            card.Controls.Add(new Label{Text="PROTEÇÃO DE ÍNDICES",Left=18,Top=15,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});
            chkSettingsSmdProtectIndices=new CheckBox{Text="Preservar índices de entries especiais (byte +0x38 diferente de 0x08)",Left=18,Top=48,Width=650,Height=26,ForeColor=TextPrimary,BackColor=Surface2,Checked=true};
            chkSettingsSmdProtectIndices.CheckedChanged+=chkSettingsSmdProtectIndices_CheckedChanged;
            card.Controls.Add(chkSettingsSmdProtectIndices);
            card.Controls.Add(new Label{Text="Quando necessário, a exclusão mantém um slot invisível com escala zero para não deslocar eventos vinculados pelo executável.",Left=38,Top=80,Width=710,Height=46,ForeColor=TextMuted,Font=new Font("Segoe UI",8.5F)});
            smdTab.Controls.Add(card);tabs.TabPages.Add(smdTab);
            var toolsTab=new TabPage("FERRAMENTAS"){BackColor=Surface,ForeColor=TextPrimary,Padding=new Padding(18)};
            var toolsCard=Card(18,18,790,160);toolsCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            toolsCard.Controls.Add(new Label{Text="EDITOR DE EXECUTÁVEL",Left=18,Top=15,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});
            chkSettingsCreateIsoBackup=new CheckBox{Text="Criar backup da ISO antes de editar o ELF interno",Left=18,Top=48,Width=560,Height=26,ForeColor=TextPrimary,BackColor=Surface2,Checked=true};
            chkSettingsCreateIsoBackup.CheckedChanged+=chkSettingsCreateIsoBackup_CheckedChanged;toolsCard.Controls.Add(chkSettingsCreateIsoBackup);
            toolsCard.Controls.Add(new Label{Text="Quando ativado, a primeira gravação cria uma cópia .iso.bak completa. Desative para economizar espaço e tempo; o backup de arquivos ELF independentes não é afetado.",Left=38,Top=80,Width=710,Height=54,ForeColor=TextMuted,Font=new Font("Segoe UI",8.5F)});
            toolsTab.Controls.Add(toolsCard);tabs.TabPages.Add(toolsTab);
            var camTab=new TabPage("CAM"){BackColor=Surface,ForeColor=TextPrimary,Padding=new Padding(18)};var camCard=Card(18,18,790,260);camCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;camCard.Controls.Add(new Label{Text="TIMELINE DE FRAMES",Left=18,Top=15,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});chkSettingsCamTimeline=new CheckBox{Text="Mostrar timeline CAM na parte inferior do viewport",Left=18,Top=48,Width=520,Height=26,ForeColor=TextPrimary,BackColor=Surface2,Checked=true};chkSettingsCamTimeline.CheckedChanged+=chkSettingsCamTimeline_CheckedChanged;camCard.Controls.Add(chkSettingsCamTimeline);camCard.Controls.Add(new Label{Text="A lista tradicional de frames permanece disponível. A timeline permite selecionar, adicionar e remover datasets rapidamente.",Left=38,Top=80,Width=710,Height=40,ForeColor=TextMuted,Font=new Font("Segoe UI",8.5F)});camCard.Controls.Add(new Label{Text="PROTEÇÃO DE ÍNDICES MOTION",Left=18,Top=132,Width=350,Height=22,ForeColor=TextPrimary,Font=new Font("Segoe UI Semibold",10F)});chkSettingsCamProtectMotion=new CheckBox{Text="Preservar os índices físicos das câmeras Motion",Left=18,Top=165,Width=520,Height=26,ForeColor=TextPrimary,BackColor=Surface2,Checked=true};chkSettingsCamProtectMotion.CheckedChanged+=chkSettingsCamProtectMotion_CheckedChanged;camCard.Controls.Add(chkSettingsCamProtectMotion);camCard.Controls.Add(new Label{Text="Ao excluir câmeras anteriores, o registro necessário é mantido como slot reservado para não deslocar referências dinâmicas usadas pelo jogo.",Left=38,Top=197,Width=710,Height=46,ForeColor=TextMuted,Font=new Font("Segoe UI",8.5F)});camTab.Controls.Add(camCard);tabs.TabPages.Add(camTab);pnlSettings.Controls.Add(tabs);
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
