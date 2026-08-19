using RE4_PS2_MOD_WORKSPACE.Core.Workspace;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class WorkspaceProject
{
    public string? RootPath { get; set; }
    public string? IsoPath { get; set; }
    public string? ActiveDatPath { get; set; }
    public string? ActiveDatName { get; set; }
    public string? ActiveContentPath { get; set; }
    public string? ActiveBuildDatPath { get; set; }
    public string? ActiveBuildIsoPath { get; set; }
    public string? ActiveAfsPath { get; set; }
    public string? BuildIsoSourcePath { get; set; }
    public DateTime? LastBuildUtc { get; set; }
    public int ActiveWorkspaceTabIndex { get; set; }
    public string? SelectedContentRelativePath { get; set; }
    public int BuildIsoGeneration { get; set; }
    public List<DatProjectState> DatStates { get; set; } = new();
}

public sealed class DatProjectState
{
    public string DatName { get; set; } = "";
    public string? OriginalDatPath { get; set; }
    public string? ContentPath { get; set; }
    public string? BuildDatPath { get; set; }
    public string? AfsPath { get; set; }
    public DateTime? LastBuildUtc { get; set; }
    public int InjectedGeneration { get; set; }

    public bool HasVisualCamera { get; set; }
    public float VisualCameraX { get; set; }
    public float VisualCameraY { get; set; }
    public float VisualCameraZ { get; set; }
    public float VisualCameraYaw { get; set; }
    public float VisualCameraPitch { get; set; }
    public int VisualMoveSpeedSlider { get; set; } = 100;
    public int VisualLookSpeedSlider { get; set; } = 100;
}

public sealed record TrackedDatStatus(DatProjectState State, SnapshotDiff Diff, int PendingTpl, bool NeedsRepack, bool NeedsInject);
