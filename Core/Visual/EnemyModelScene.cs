using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public readonly record struct EnemySkinInfluence(byte BoneId, float Weight);

public readonly record struct EnemyVertexSkin(EnemySkinInfluence A, EnemySkinInfluence B, EnemySkinInfluence C, int Count)
{
    public static EnemyVertexSkin None => default;
}

public readonly record struct EnemyModelTriangle(
    Vector3 A, Vector3 B, Vector3 C,
    Vector2 UvA, Vector2 UvB, Vector2 UvC,
    int TextureIndex,
    int TplEntryIndex,
    EnemyVertexSkin SkinA, EnemyVertexSkin SkinB, EnemyVertexSkin SkinC);

/// <summary>One embedded TPL package inside an enemy emXX.dat.</summary>
public sealed class EnemyTexturePackage
{
    public int DatEntryIndex { get; init; }
    public byte[] Data { get; init; } = Array.Empty<byte>();
}

/// <summary>One renderable BIN inside an enemy emXX.dat.</summary>
public enum EnemyTplResolutionKind { None, DirectNext, SharedPrevious }

public sealed class EnemyModelPart
{
    /// <summary>Zero-based BIN ordinal among BIN entries in the DAT.</summary>
    public int BinIndex { get; init; }
    /// <summary>Original DAT table entry index. Useful while reverse engineering the package.</summary>
    public int DatEntryIndex { get; init; }
    /// <summary>Embedded TPL entry selected for this BIN. -1 means unknown/untextured.</summary>
    public int TplEntryIndex { get; init; } = -1;
    public EnemyTplResolutionKind TplResolution { get; init; } = EnemyTplResolutionKind.None;
    public IReadOnlyList<int> DiffuseMaps { get; init; } = Array.Empty<int>();
    public IReadOnlyList<EnemyModelTriangle> Triangles { get; init; } = Array.Empty<EnemyModelTriangle>();
    public Vector3 BoundsMin { get; init; }
    public Vector3 BoundsMax { get; init; }
    public Vector3 Size => BoundsMax - BoundsMin;
}

public sealed class EnemyModelScene
{
    /// <summary>Skeleton extracted from a representative body BIN when available.</summary>
    public Ps2BinSkeleton? Skeleton { get; init; }
    public int SkeletonSourceDatEntryIndex { get; init; } = -1;
    /// <summary>Idle FCV embedded in the enemy DAT. For em12 the known idle is DAT entry/FCV 001.</summary>
    public FcvAnimation? IdleAnimation { get; init; }
    public int IdleAnimationDatEntryIndex { get; init; } = -1;
    public byte EnemyType { get; init; }
    public string SourcePath { get; init; } = string.Empty;
    public int DatEntryCount { get; init; }
    public int BinCount { get; init; }
    public int LoadedBinCount { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    /// <summary>Individual BIN parts. MODEL PARTS debugger can toggle these independently.</summary>
    public IReadOnlyList<EnemyModelPart> Parts { get; init; } = Array.Empty<EnemyModelPart>();
    /// <summary>Embedded enemy texture packages, keyed by original DAT entry index.</summary>
    public IReadOnlyDictionary<int, EnemyTexturePackage> TexturePackages { get; init; } = new Dictionary<int, EnemyTexturePackage>();
    /// <summary>Combined geometry retained for diagnostics/summary.</summary>
    public IReadOnlyList<EnemyModelTriangle> Triangles { get; init; } = Array.Empty<EnemyModelTriangle>();
    public Vector3 BoundsMin { get; init; }
    public Vector3 BoundsMax { get; init; }
    public Vector3 Size => BoundsMax - BoundsMin;
}
