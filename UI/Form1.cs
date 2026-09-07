using RE4_PS2_MOD_WORKSPACE.Core.Afs;
using RE4_PS2_MOD_WORKSPACE.Core.Iso;
using System.Diagnostics;
using System.Text.Json;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1 : Form
{

    private readonly string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RE4_PS2_MOD_WORKSPACE", "settings.json");
    private AppSettings settings = new();
    private WorkspaceProject project = new();
    private AfsImage? loadedAfs;
    private bool restoringSession;
    private CharacterCustomizerForm? characterCustomizer;
    private Panel? pnlStartupLoading;
    private Label? lblStartupLoading;
    private readonly System.Windows.Forms.Timer autoSaveTimer = new();
    private readonly System.Windows.Forms.Timer visualSpeedSaveTimer = new();
    private bool autoSaveRunning;

    public Form1()
    {
        InitializeComponent();
        LoadSettings();
        ApplySidebarState(settings.SidebarCollapsed);
        ApplyVisualSidePanelState();
        InitializeAutoSave();
        InitializeVisualSpeedPersistence();
        ApplyVisualLayerSettings();
        if (chkVisualEnemyLabels != null) chkVisualEnemyLabels.Checked = settings.VisualEnemyLabels;
        if (visualViewport != null) visualViewport.ShowEnemyLabels = settings.VisualEnemyLabels;
        if (chkVisualEnemyModelParts != null) chkVisualEnemyModelParts.Checked = settings.VisualEnemyModelParts;
        if (pnlVisualEnemyModelParts != null) pnlVisualEnemyModelParts.Visible = settings.VisualEnemyModelParts;
        if (chkVisualEnemyAnimated != null) chkVisualEnemyAnimated.Checked = settings.VisualEnemyAnimated;
        if (chkVisualEnemyInactive != null) chkVisualEnemyInactive.Checked = settings.VisualShowInactiveEnemies;
        if (tabVisualEntities != null && tabVisualEntities.TabCount > 0)
            tabVisualEntities.SelectedIndex = Math.Clamp(settings.VisualSelectedEntityTab, 0, tabVisualEntities.TabCount - 1);
        // A leitura pesada do workspace é feita depois que a janela aparece, para que
        // o usuário veja o andamento da restauração em vez de uma janela congelada.
        ApplyDataToUi(refreshContent: false, refreshBuild: false);
        ShowPage(pnlDashboard, btnNavDashboard, "Dashboard");
        CreateStartupLoadingOverlay();
        Shown += Form1_Shown;
        FormClosing += Form1_FormClosing;
    }
}
