namespace RE4_PS2_MOD_WORKSPACE
{
    partial class Form1
    {
        private System.ComponentModel.IContainer? components = null;
        private Panel pnlSidebar = null!, pnlTop = null!, pnlContent = null!;
        private Panel pnlDashboard = null!, pnlWorkspace = null!, pnlAssets = null!, pnlTextures = null!, pnlBuild = null!, pnlTools = null!, pnlLogs = null!;
        private Label lblLogo = null!, lblLogoSub = null!, lblVersion = null!, lblTopTitle = null!, lblWorkspaceCurrent = null!;
        private Button btnNavDashboard = null!, btnNavWorkspace = null!, btnNavAssets = null!, btnNavTextures = null!, btnNavBuild = null!, btnNavTools = null!, btnNavLogs = null!, btnTopBuild = null!;
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
            pnlDashboard = new Panel(); pnlWorkspace = new Panel(); pnlAssets = new Panel(); pnlTextures = new Panel(); pnlBuild = new Panel(); pnlTools = new Panel(); pnlLogs = new Panel();
            lblLogo = new Label(); lblLogoSub = new Label(); lblVersion = new Label(); lblTopTitle = new Label();
            btnNavDashboard = new Button(); btnNavWorkspace = new Button(); btnNavAssets = new Button(); btnNavTextures = new Button(); btnNavBuild = new Button(); btnNavTools = new Button(); btnNavLogs = new Button(); btnTopBuild = new Button();
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Bg;
            ClientSize = new Size(1380, 820);
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1120, 690);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "RE4 PS2 Mod Workspace";

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
            SetupNav(btnNavBuild, "Build & Test", btnNavBuild_Click);
            SetupNav(btnNavTools, "Ferramentas", btnNavTools_Click);
            SetupNav(btnNavLogs, "Console", btnNavLogs_Click);
            lblVersion.Text = "v0.2.2"; lblVersion.Dock = DockStyle.Bottom; lblVersion.Height = 26; lblVersion.ForeColor = TextMuted; lblVersion.TextAlign = ContentAlignment.MiddleLeft;
            pnlSidebar.Controls.Add(btnNavLogs); pnlSidebar.Controls.Add(btnNavTools); pnlSidebar.Controls.Add(btnNavBuild); pnlSidebar.Controls.Add(btnNavTextures); pnlSidebar.Controls.Add(btnNavAssets); pnlSidebar.Controls.Add(btnNavWorkspace); pnlSidebar.Controls.Add(btnNavDashboard); pnlSidebar.Controls.Add(lblLogoSub); pnlSidebar.Controls.Add(lblLogo); pnlSidebar.Controls.Add(lblVersion);

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
            foreach (Panel page in new[] { pnlDashboard, pnlWorkspace, pnlAssets, pnlTextures, pnlBuild, pnlTools, pnlLogs }) SetupPage(page);
            BuildDashboardDesigner(); BuildProjectDesigner(); BuildAssetsDesigner(); BuildTexturesDesigner(); BuildBuildDesigner(); BuildToolsDesigner(); BuildLogsDesigner();
            pnlContent.Controls.Add(pnlLogs); pnlContent.Controls.Add(pnlTools); pnlContent.Controls.Add(pnlBuild); pnlContent.Controls.Add(pnlTextures); pnlContent.Controls.Add(pnlAssets); pnlContent.Controls.Add(pnlWorkspace); pnlContent.Controls.Add(pnlDashboard);

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
