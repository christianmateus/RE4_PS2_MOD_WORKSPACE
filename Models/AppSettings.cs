namespace RE4_PS2_MOD_WORKSPACE;

public sealed class AppSettings
{
    public string? TplManagerPath { get; set; }
    public string? Pcsx2Path { get; set; }
    public string? Ps2BinToolPath { get; set; }
    public string? LastWorkspace { get; set; }
    public string? LastMainPage { get; set; } = "Workspace";
    public bool SidebarCollapsed { get; set; }
    public bool VisualScenarioLayer { get; set; } = true;
    public bool VisualAevLayer { get; set; } = true;
    public bool VisualEnemiesLayer { get; set; } = false;
    public bool VisualObjectsLayer { get; set; } = false;
    public bool VisualCollisionLayer { get; set; } = false;
    public bool VisualLightingLayer { get; set; } = true;
    public bool VisualEffectsLayer { get; set; } = true;
    public bool VisualRtpLayer { get; set; } = true;
    public string? SelectedEnemyEslName { get; set; }
    public bool VisualEnemyLabels { get; set; } = false;
    public bool VisualEnemyModelParts { get; set; } = false;
    public bool VisualEnemySnap { get; set; } = false;
    public bool VisualEnemyAnimated { get; set; } = false;
    public bool VisualShowInactiveEnemies { get; set; } = false;
    public int VisualSelectedEntityTab { get; set; } = 0;
    public bool SmdProtectSpecialEntryIndices { get; set; } = true;
    public bool AutoSaveEnabled { get; set; } = false;
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool VisualShowFps { get; set; } = false;
    public bool VisualLayersPanelCollapsed { get; set; } = false;
    public bool VisualPropertiesPanelCollapsed { get; set; } = false;
    public string? LastCharacterDatPath { get; set; }
    public bool HasCharacterCamera { get; set; }
    public float CharacterCameraX { get; set; }
    public float CharacterCameraY { get; set; }
    public float CharacterCameraZ { get; set; }
    public float CharacterCameraYaw { get; set; }
    public float CharacterCameraPitch { get; set; }
    public List<CamPresetDefinition> CamPresets { get; set; } = new();
    public bool CamShowTimeline { get; set; } = true;
    public bool CamProtectMotionIndices { get; set; } = true;
}

public sealed class CamPresetDefinition
{
    public string Name { get; set; } = "New preset";
    public string Description { get; set; } = string.Empty;
    public byte CameraType { get; set; } = 8;
    public int VertexCount { get; set; } = 4;
    public int FrameCount { get; set; }
    public byte AreaAttributes { get; set; } = 3;
}
