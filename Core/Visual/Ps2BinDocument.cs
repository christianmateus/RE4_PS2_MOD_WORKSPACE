using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>A stable editable view of a PS2 BIN. Vertex identity is its physical
/// record offset, so shared strip vertices remain shared during edits.</summary>
public sealed class Ps2BinDocument
{
    public byte[] Source { get; private set; }
    public IReadOnlyList<Ps2BinVertex> Vertices { get; private set; }=Array.Empty<Ps2BinVertex>();
    public IReadOnlyList<Ps2BinFace> Faces { get; private set; }=Array.Empty<Ps2BinFace>();
    public IReadOnlyList<Ps2BinMaterial> Materials { get; private set; }=Array.Empty<Ps2BinMaterial>();
    public IReadOnlyList<Ps2BinEdge> Edges { get; private set; }=Array.Empty<Ps2BinEdge>();
    private Ps2BinDocument(byte[] source){Source=source;Reindex();}
    public static Ps2BinDocument Parse(byte[] source)=>new((byte[])(source??throw new ArgumentNullException(nameof(source))).Clone());
    public void Translate(IEnumerable<int> physicalVertexOffsets,Vector3 delta){Source=Ps2CharacterDatEditor.TransformStandaloneBinVertices(Source,physicalVertexOffsets.Distinct().ToArray(),delta,Vector3.Zero,Vector3.One);Reindex();}
    public void Rotate(IEnumerable<int> physicalVertexOffsets,Vector3 rotationDegrees){Source=Ps2CharacterDatEditor.TransformStandaloneBinVertices(Source,physicalVertexOffsets.Distinct().ToArray(),Vector3.Zero,rotationDegrees,Vector3.One);Reindex();}
    private void Reindex(){var triangles=Ps2ScenarioReader.ReadStandaloneBin(Source);var vertices=new Dictionary<int,Ps2BinVertex>();var faces=new List<Ps2BinFace>(triangles.Count);for(int i=0;i<triangles.Count;i++){ScenarioTriangle t=triangles[i];int[] offsets={t.SourceOffsetA,t.SourceOffsetB,t.SourceOffsetC};Vector3[] positions={t.A,t.B,t.C};Vector2[] uvs={t.UvA,t.UvB,t.UvC};for(int c=0;c<3;c++){if(offsets[c]<0||offsets[c]+24>Source.Length)throw new InvalidDataException($"Face {i} referencia um vértice fora do BIN.");if(!vertices.ContainsKey(offsets[c]))vertices[offsets[c]]=new(offsets[c],positions[c],uvs[c],t.SourceFactor);}faces.Add(new(i,t.TextureIndex,offsets[0],offsets[1],offsets[2],t.SourceStripFlagOffset));}Vertices=vertices.Values.OrderBy(x=>x.Offset).ToArray();Faces=faces;Edges=faces.SelectMany(f=>new[]{Ps2BinEdge.Create(f.A,f.B),Ps2BinEdge.Create(f.B,f.C),Ps2BinEdge.Create(f.C,f.A)}).Distinct().ToArray();Materials=faces.GroupBy(f=>f.MaterialIndex).Select(g=>new Ps2BinMaterial(g.Key,g.Count())).OrderBy(x=>x.Index).ToArray();}
}
public readonly record struct Ps2BinVertex(int Offset,Vector3 Position,Vector2 Uv,float Factor);
public readonly record struct Ps2BinFace(int Index,int MaterialIndex,int A,int B,int C,int StripFlagOffset);
public readonly record struct Ps2BinEdge(int A,int B){public static Ps2BinEdge Create(int a,int b)=>a<=b?new(a,b):new(b,a);}
public readonly record struct Ps2BinMaterial(int Index,int FaceCount);
