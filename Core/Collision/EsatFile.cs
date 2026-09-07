using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

public enum EsatKind { Sat, Eat }

public sealed class EsatFile
{
    public required string SourcePath { get; init; }
    public required EsatKind Kind { get; init; }
    public byte ContainerMagic { get; init; }
    public ushort ContainerUnknown { get; set; }
    public List<EsatMesh> Meshes { get; } = new();
    public int FaceCount => Meshes.Sum(x => x.Faces.Count);
}

public sealed class EsatMesh
{
    public long FileOffset { get; init; }
    public long PositionsFileOffset { get; init; }
    public long NormalsFileOffset { get; init; }
    public long EdgeVectorsFileOffset { get; init; }
    public byte Magic { get; init; }
    public byte Unknown01 { get; init; }
    public ushort Unknown08 { get; init; }
    public ushort FloorCount { get; init; }
    public ushort SlopeCount { get; init; }
    public ushort WallCount { get; init; }
    public List<Vector3> Positions { get; } = new();
    public List<Vector3> OriginalPositions { get; } = new();
    public List<Vector3> Normals { get; } = new();
    public List<Vector3> EdgeVectors { get; } = new();
    public List<EsatFace> Faces { get; } = new();
    public List<EsatGroup> Groups { get; } = new();
    public bool IsModified => Positions.Count == OriginalPositions.Count && Positions.Where((p, i) => p != OriginalPositions[i]).Any();
}

public sealed record EsatFace(
    ushort Vertex0, ushort Vertex1, ushort Vertex2, ushort Normal,
    ushort Edge0, ushort Edge1, ushort Edge2, ushort Unknown,
    byte Blue, byte Green, byte Red, byte Connectivity);

public sealed class EsatGroup
{
    public long FileOffset { get; init; }
    public Vector3 Position { get; set; }
    public Vector3 Size { get; set; }
    public ushort FloorCount { get; init; }
    public ushort SlopeCount { get; init; }
    public ushort WallCount { get; init; }
    public ushort Flags { get; init; }
    public uint BrotherDistance { get; init; }
    public ushort[] FloorFaces { get; init; } = Array.Empty<ushort>();
    public ushort[] SlopeFaces { get; init; } = Array.Empty<ushort>();
    public ushort[] WallFaces { get; init; } = Array.Empty<ushort>();
}
