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

    public Form1()
    {
        InitializeComponent();
        LoadSettings();
        ApplyDataToUi();
        ShowPage(pnlDashboard, btnNavDashboard, "Dashboard");
        Shown += Form1_Shown;
        FormClosing += Form1_FormClosing;
    }
}
