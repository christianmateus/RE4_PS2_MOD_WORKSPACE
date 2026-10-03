using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Collision;
namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class SmdCatalogCollisionFace
{
    public EsatKind Kind {get;set;}
    public int Category {get;set;}
    public float[] A {get;set;}=new float[3];
    public float[] B {get;set;}=new float[3];
    public float[] C {get;set;}=new float[3];
    public byte Blue {get;set;} public byte Green {get;set;} public byte Red {get;set;} public byte Connectivity {get;set;}
    public ushort Unknown {get;set;}
    public float NormalSign {get;set;}=1;
    public static Matrix4x4 Transform(ScenarioEntry e)=>Matrix4x4.CreateScale(e.ScaleX,e.ScaleY,e.ScaleZ)*Matrix4x4.CreateRotationX(e.RotationX)*Matrix4x4.CreateRotationY(e.RotationY)*Matrix4x4.CreateRotationZ(e.RotationZ)*Matrix4x4.CreateTranslation(e.Position);
    public static SmdCatalogCollisionFace Capture(EsatFaceInspection selection,ScenarioEntry anchor)
    {
        if(!Matrix4x4.Invert(Transform(anchor),out var inverse))throw new InvalidOperationException("A escala do objeto não pode ser zero ao associar colisões.");
        var f=selection.Mesh.Faces[selection.FaceIndex];
        float[] Point(int index){var v=Vector3.Transform(selection.Mesh.Positions[index]/100f,inverse);return new[]{v.X,v.Y,v.Z};}
        return new(){Kind=selection.File.Kind,Category=(int)selection.Mesh.GetFaceCategory(selection.FaceIndex),A=Point(f.Vertex0),B=Point(f.Vertex1),C=Point(f.Vertex2),Blue=f.Blue,Green=f.Green,Red=f.Red,Connectivity=f.Connectivity,Unknown=f.Unknown,NormalSign=Vector3.Dot(Vector3.Cross(selection.Mesh.Positions[f.Vertex1]-selection.Mesh.Positions[f.Vertex0],selection.Mesh.Positions[f.Vertex2]-selection.Mesh.Positions[f.Vertex0]),selection.Mesh.Normals[f.Normal])<0?-1:1};
    }
    public Vector3 Point(float[] value,ScenarioEntry anchor)
    {
        if(value.Length!=3||value.Any(v=>!float.IsFinite(v)))throw new InvalidDataException("Coordenada inválida na colisão do catálogo.");
        return Vector3.Transform(new Vector3(value[0],value[1],value[2]),Transform(anchor))*100f;
    }
}
