namespace RE4_PS2_MOD_WORKSPACE;

public sealed class AppSettings
{
    public string? IsoAfsPath { get; set; }
    public string? DatToolPath { get; set; }
    public string? TplManagerPath { get; set; }
    public string? Pcsx2Path { get; set; }
    public string? LastWorkspace { get; set; }
    public string? LastMainPage { get; set; } = "Workspace";
    public bool VisualScenarioLayer { get; set; } = true;
    public bool VisualAevLayer { get; set; } = true;
    public bool VisualEnemiesLayer { get; set; } = false;
    public bool VisualObjectsLayer { get; set; } = false;
    public bool VisualCollisionLayer { get; set; } = false;
    public string? SelectedEnemyEslName { get; set; }
    public bool VisualEnemyLabels { get; set; } = false;
    public bool VisualEnemyModelParts { get; set; } = false;
    public bool VisualEnemySnap { get; set; } = false;
    public bool VisualEnemyAnimated { get; set; } = false;
}
