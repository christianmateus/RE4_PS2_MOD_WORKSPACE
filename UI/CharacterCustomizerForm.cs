using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm : AppForm
{
    private const byte PreviewType = 0;
    private readonly ExtractedCharacterWorkspace characterWorkspace = new();
    public string? WorkingDatPath => datPath;
    private readonly ScenarioViewport viewport = new() { Dock = DockStyle.Fill, ScenarioVisible = true, AevVisible = false, EnemiesVisible = true, ObjectsVisible = false, ShowEnemyLabels = false, EnemyModelPartPickingEnabled = true, GridAtWorldOrigin = true };
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
    private readonly CheckBox chkFaceEdit = new() { Text = "EDIT MODE", AutoSize = true, ForeColor = Color.Gainsboro };
    private readonly Label lblFaceSelection = new() { Text = "0 face(s)", Width = 72, Height = 26, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(155, 162, 174) };
    private readonly Button btnDeleteFaces = new() { Enabled = false };
    private readonly Button btnMoveMesh = new();
    private readonly Button btnRotateMesh = new();
    private GroupBox? meshTransformGroup;
    private readonly ThemedToolTip helpTips = new();
    private readonly HashSet<int> selectedFaceFlags = new();
    private readonly HashSet<int> selectedFaceVertices = new();
    private int selectedFaceBin = -1;
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
    private readonly string? animationCatalogRoot;
    private readonly Action<bool>? characterLockRootChanged;
    private bool cameraInitialized;
    private readonly TextBox txtPartSearch = new() { PlaceholderText = "Buscar BIN, nome, DAT ou TPL", Dock = DockStyle.Fill };
    private readonly ComboBox cmbPartFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 112 };
    private readonly TextBox txtPartName = new() { Dock = DockStyle.Top, PlaceholderText = "Nome da parte" };
    private readonly TextBox txtTextureName = new() { Dock = DockStyle.Top, PlaceholderText = "Nome da textura" };
    private (int TplEntry, int Index)? editingTexture;
    private bool syncingTextureName;
    private readonly Label lblPartDetails = new() { Dock = DockStyle.Top, Height = 56, ForeColor = Color.Gainsboro };
    private readonly ComboBox cmbViewPreset = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly Dictionary<int, PartAnnotation> partAnnotations = new();
    private readonly Dictionary<string, string> textureNames = new();
    private readonly List<VisibilityPreset> visibilityPresets = new();
    private readonly HashSet<int> visibleBins = new();
    private HashSet<int>? savedVisibleBins;
    private string? activePresetName;
    private bool syncingPartDetails;
    private bool syncingPreset;
    private static readonly Regex SupportedDatName = new("^(?:pl|em|wep)[0-9a-f]{2}\\.dat$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
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

    private sealed record TextureItem(int TplEntry, int Index, int Width, int Height, string Format, string? Name = null)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(Name)
            ? $"TPL #{TplEntry:D3} / textura {Index:D2}"
            : $"TPL #{TplEntry:D3} • {Name}";
    }
    private sealed record MeshLibraryItem(string DatPath, EnemyModelScene DonorModel, EnemyModelPart Part, string? Name)
    {
        public override string ToString() => $"BIN {Part.BinIndex:D2}{(string.IsNullOrWhiteSpace(Name) ? "" : $" — {Name}")}";
    }
    private sealed record PartItem(EnemyModelPart Part, string? Name)
    {
        public override string ToString() => $"BIN {Part.BinIndex:D2}{(string.IsNullOrWhiteSpace(Name) ? "" : $" — {Name}")}{(Part.Triangles.Count == 0 ? " • REMOVIDO" : "")}";
    }

    public CharacterCustomizerForm(string? initialPath = null, Action<string>? datOpened = null, IEnumerable<string>? extractedRoots = null, ScenarioCameraState? initialCamera = null, Action<ScenarioCameraState>? cameraSaved = null, string? animationCatalogRoot = null, bool characterLockRoot = true, Action<bool>? characterLockRootChanged = null)
    {
        this.initialPath = initialPath;
        this.datOpened = datOpened;
        this.extractedRoots = (extractedRoots ?? new[] { Path.Combine(Application.StartupPath, "Extracted") }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        this.initialCamera = initialCamera;
        this.cameraSaved = cameraSaved;
        this.animationCatalogRoot = animationCatalogRoot;
        this.characterLockRootChanged = characterLockRootChanged;
        chkCharacterLockRoot.Checked = characterLockRoot;
        viewport.SetEnemyAnimationIgnoreRootMotion(characterLockRoot);
        Text = "Customização de Personagem • RE4 PS2";
        Width = 1320; Height = 820; MinimumSize = new Size(980, 640); StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(13, 15, 18); ForeColor = Color.Gainsboro; Font = new Font("Segoe UI", 9f);
        KeyPreview = true;
        BuildUi();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.H || txtPartName.ContainsFocus || txtPartSearch.ContainsFocus || txtTextureName.ContainsFocus || txtCharacterAnimationName.ContainsFocus) return;
            ToggleSelectedPartVisibility();
            e.Handled = true; e.SuppressKeyPress = true;
        };
        Shown += (_, _) => InitializeDatSelection();
        FormClosing += (_, _) => { PauseCharacterPlayback(); SaveSelectedPartAnnotation(); };
        FormClosed += (_, _) => { animationTimer.Dispose(); helpTips.Dispose(); };
        Disposed += (_, _) => characterWorkspace.Dispose();
    }

}



