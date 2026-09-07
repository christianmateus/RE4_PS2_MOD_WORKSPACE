using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

public enum EsatFaceCategory { Floor, Slope, Wall }

public sealed class EsatFaceInspection
{
    [Browsable(false)] public required EsatFile File { get; init; }
    [Browsable(false)] public required EsatMesh Mesh { get; init; }
    [Browsable(false)] public required EsatFace Face { get; init; }

    [Category("Face"), DisplayName("Arquivo")] public string FileType => File.Kind.ToString().ToUpperInvariant();
    [Category("Face"), DisplayName("Submesh")] public int MeshIndex { get; init; }
    [Category("Face"), DisplayName("Índice")] public int FaceIndex { get; init; }
    [Category("Face"), DisplayName("Categoria")] public EsatFaceCategory Category { get; init; }
    [Category("Face"), DisplayName("Vértices")] public string VertexIndices => $"{Face.Vertex0}, {Face.Vertex1}, {Face.Vertex2}";
    [Category("Face"), DisplayName("Normal")] public ushort NormalIndex => Face.Normal;
    [Category("Face"), DisplayName("Arestas")] public string EdgeIndices => $"{Face.Edge0}, {Face.Edge1}, {Face.Edge2}";

    [Category("Posições"), DisplayName("V0")] public Vector3 Vertex0 => Mesh.Positions[Face.Vertex0] / 100f;
    [Category("Posições"), DisplayName("V1")] public Vector3 Vertex1 => Mesh.Positions[Face.Vertex1] / 100f;
    [Category("Posições"), DisplayName("V2")] public Vector3 Vertex2 => Mesh.Positions[Face.Vertex2] / 100f;
    [Category("Posições"), DisplayName("Normal da face")] public Vector3 Normal => Mesh.Normals[Face.Normal];

    [Category("Flags brutas"), DisplayName("BB (Blue)")] public string Blue => $"0x{Face.Blue:X2}";
    [Category("Flags brutas"), DisplayName("GG (Green)")] public string Green => $"0x{Face.Green:X2}";
    [Category("Flags brutas"), DisplayName("RR (Red)")] public string Red => $"0x{Face.Red:X2}";
    [Category("Flags brutas"), DisplayName("YY (Connectivity)")] public string Connectivity => $"0x{Face.Connectivity:X2}";
    [Category("Flags interpretadas"), DisplayName("Ativas")] public string KnownFlags => EsatFlagCatalog.Describe(File.Kind, Face);
    [Category("Grupos espaciais"), DisplayName("Referências")] public string GroupReferences => FindGroups();

    private string FindGroups()
    {
        var groups = new List<int>();
        for (int i = 0; i < Mesh.Groups.Count; i++)
        {
            EsatGroup g = Mesh.Groups[i];
            if (g.FloorFaces.Contains((ushort)FaceIndex) || g.SlopeFaces.Contains((ushort)FaceIndex) || g.WallFaces.Contains((ushort)FaceIndex)) groups.Add(i);
        }
        return groups.Count == 0 ? "Nenhum" : string.Join(", ", groups);
    }

    public override string ToString() => $"{FileType} • Mesh {MeshIndex} • Face {FaceIndex} • {Category}";
}

public static class EsatFlagCatalog
{
    public static string Describe(EsatKind kind, EsatFace face)
    {
        var names = new List<string>();
        if (kind == EsatKind.Sat)
        {
            Add(face.Red, 0x04, "Fall Fence", names); Add(face.Red, 0x08, "Jump Over", names); Add(face.Red, 0x10, "Fall", names); Add(face.Red, 0x20, "Up", names); Add(face.Red, 0x40, "Player No Hit", names); Add(face.Red, 0x80, "Camera No Hit", names);
            Add(face.Green, 0x04, "Only Camera Hit", names); Add(face.Green, 0x08, "Cliff", names); Add(face.Green, 0x10, "Up 2", names); Add(face.Green, 0x20, "Down", names); Add(face.Green, 0x40, "Enemy No Hit", names); Add(face.Green, 0x80, "Small No Hit", names);
            Add(face.Blue, 0x04, "No Effect Set", names); Add(face.Blue, 0x08, "Hide", names); Add(face.Blue, 0x10, "Down 2", names); Add(face.Blue, 0x20, "Fence", names); Add(face.Blue, 0x40, "Route No Hit", names); Add(face.Blue, 0x80, "Steps", names);
        }
        else
        {
            Add(face.Red, 0x40, "Small No Hit", names); Add(face.Red, 0x80, "Effect Bit 0", names);
            Add(face.Green, 0x40, "Middle No Hit", names); Add(face.Green, 0x80, "Effect Bit 1", names);
            Add(face.Blue, 0x40, "No Effect Set", names); Add(face.Blue, 0x80, "Effect Bit 2", names);
        }
        Add(face.Connectivity, 0x20, "Edge 0 Shared", names); Add(face.Connectivity, 0x40, "Edge 1 Shared", names); Add(face.Connectivity, 0x80, "Edge 2 Shared", names);
        return names.Count == 0 ? "Nenhuma flag conhecida" : string.Join(" • ", names);
    }

    private static void Add(byte value, byte mask, string name, List<string> output) { if ((value & mask) != 0) output.Add(name); }
}
