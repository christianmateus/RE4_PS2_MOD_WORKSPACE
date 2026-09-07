using System.Numerics;

using System.ComponentModel;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public readonly struct ScenarioTriangle
{
    public readonly Vector3 A;
    public readonly Vector3 B;
    public readonly Vector3 C;
    public readonly Vector2 UvA;
    public readonly Vector2 UvB;
    public readonly Vector2 UvC;
    public readonly int TextureIndex;
    public readonly int SourceOffsetA, SourceOffsetB, SourceOffsetC;
    public readonly int SourceStripFlagOffset;
    public readonly float SourceFactor;

    public ScenarioTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, int textureIndex, int sourceOffsetA=-1, int sourceOffsetB=-1, int sourceOffsetC=-1, float sourceFactor=1f, int sourceStripFlagOffset=-1)
    {
        A = a; B = b; C = c;
        UvA = uvA; UvB = uvB; UvC = uvC;
        TextureIndex = textureIndex;
        SourceOffsetA=sourceOffsetA;SourceOffsetB=sourceOffsetB;SourceOffsetC=sourceOffsetC;SourceFactor=sourceFactor;
        SourceStripFlagOffset=sourceStripFlagOffset;
    }
}

public sealed class ScenarioScene
{
    public string SourcePath { get; init; } = string.Empty;
    public int EntryCount { get; init; }
    public int BinCount { get; init; }
    public int LoadedBinCount { get; init; }
    public int SkippedBinCount { get; init; }
    public List<string> Warnings { get; init; } = new();
    public List<ScenarioTriangle> Triangles { get; init; } = new();
    public List<ScenarioEntry> Entries { get; init; } = new();
    public bool IsModified { get; set; }
    public Dictionary<(byte BinId,int VertexOffset),(Vector3 Position,float Factor)> PendingVertexEdits { get; } = new();
    public HashSet<(byte BinId,int StripFlagOffset)> PendingFaceDeletes { get; } = new();
    public Vector3 BoundsMin { get; set; }
    public Vector3 BoundsMax { get; set; }

    public Vector3 Center => (BoundsMin + BoundsMax) * 0.5f;
    public Vector3 Size => BoundsMax - BoundsMin;
    public float Radius => Math.Max(1f, Size.Length() * 0.5f);
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class ScenarioEntry
{
    internal byte[] RawData { get; set; } = new byte[0x40];
    [Browsable(false)] public int FileOrder { get; set; }
    [Browsable(false)] public IReadOnlyList<ScenarioTriangle> LocalTriangles { get; set; } = Array.Empty<ScenarioTriangle>();
    [Category("Object"), DisplayName("Entry")] public int Index => FileOrder;
    [Category("Object"), DisplayName("BIN ID"), ReadOnly(true)] public byte BinId { get; set; }
    [Category("Position")] public float PositionX { get; set; }
    [Category("Position")] public float PositionY { get; set; }
    [Category("Position")] public float PositionZ { get; set; }
    [Category("Rotation (radians)")] public float RotationX { get; set; }
    [Category("Rotation (radians)")] public float RotationY { get; set; }
    [Category("Rotation (radians)")] public float RotationZ { get; set; }
    [Category("Scale")] public float ScaleX { get; set; } = 1f;
    [Category("Scale")] public float ScaleY { get; set; } = 1f;
    [Category("Scale")] public float ScaleZ { get; set; } = 1f;
    [Category("Materials"), DisplayName("Texture Index")]
    public int TextureIndex { get; set; } = -1;
    [Browsable(false)] public Vector3 Position => new(PositionX, PositionY, PositionZ);
    [Browsable(false)] public Vector3 Rotation => new(RotationX, RotationY, RotationZ);
    [Browsable(false)] public Vector3 Scale => new(ScaleX, ScaleY, ScaleZ);
    public override string ToString() => $"#{FileOrder:D3} • BIN {BinId:D3}";
}
