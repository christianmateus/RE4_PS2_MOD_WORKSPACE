using System.Numerics;

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

    public ScenarioTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, int textureIndex)
    {
        A = a; B = b; C = c;
        UvA = uvA; UvB = uvB; UvC = uvC;
        TextureIndex = textureIndex;
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
    public Vector3 BoundsMin { get; set; }
    public Vector3 BoundsMax { get; set; }

    public Vector3 Center => (BoundsMin + BoundsMax) * 0.5f;
    public Vector3 Size => BoundsMax - BoundsMin;
    public float Radius => Math.Max(1f, Size.Length() * 0.5f);
}
