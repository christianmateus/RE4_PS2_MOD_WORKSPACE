// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Common;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding.Structures;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding
{
    public static class ObjMeshCompiler
    {
        public static void CompileObj(Stream objFile, bool optimizeStrips, int rigidBoneIndex, out PackedMesh packedMesh, out float quantizationScale, out BoundingBox boundingBox)
        {
            // load .obj file
            var objLoaderFactory = new RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders.ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            StreamReader streamReader = null;
            RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders.LoadResult objDocument = null;

            try
            {
                streamReader = new StreamReader(objFile, System.Text.Encoding.ASCII);
                objDocument = objLoader.Load(streamReader);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                streamReader?.Close();
            }

            // valor que representa a maior distancia do modelo, tanto para X, Y ou Z
            float maximumCoordinate = 0;

            //--- crio a primeira estrutura:

            ImportedMesh importedMesh = new ImportedMesh();

            StartWeightMap weightMap = new StartWeightMap(1, rigidBoneIndex, 1, 0, 0, 0, 0);

            for (int iG = 0; iG < objDocument.Groups.Count; iG++)
            {
                string materialNameInvariant = objDocument.Groups[iG].MaterialName.ToUpperInvariant().Trim();

                List<List<StartVertex>> facesList = new List<List<StartVertex>>();

                for (int iF = 0; iF < objDocument.Groups[iG].Faces.Count; iF++)
                {
                    List<StartVertex> verticeListInObjFace = new List<StartVertex>();

                    for (int iI = 0; iI < objDocument.Groups[iG].Faces[iF].Count; iI++)
                    {
                        StartVertex vertice = new StartVertex();

                        if (objDocument.Groups[iG].Faces[iF][iI].VertexIndex <= 0 || objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1 >= objDocument.Vertices.Count)
                        {
                            throw new ApplicationException("Vertex Position Index is invalid! Value: " + objDocument.Groups[iG].Faces[iF][iI].VertexIndex);
                        }

                        Vector3 position = new Vector3(
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].X,
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].Y,
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].Z
                            );

                        vertice.Position = position;

                      
                        if (objDocument.Groups[iG].Faces[iF][iI].TextureIndex <= 0 || objDocument.Groups[iG].Faces[iF][iI].TextureIndex - 1 >= objDocument.Textures.Count)
                        {
                            vertice.Texture = new Vector2(0, 0);
                        }
                        else
                        {
                            Vector2 texture = new Vector2(
                            objDocument.Textures[objDocument.Groups[iG].Faces[iF][iI].TextureIndex - 1].U,
                            objDocument.Textures[objDocument.Groups[iG].Faces[iF][iI].TextureIndex - 1].V
                            );

                            vertice.Texture = texture;
                        }

        
                        if (objDocument.Groups[iG].Faces[iF][iI].NormalIndex <= 0 || objDocument.Groups[iG].Faces[iF][iI].NormalIndex - 1 >= objDocument.Normals.Count)
                        {
                            vertice.Normal = new Vector3(0, 0, 0);
                        }
                        else 
                        {
                            float nx = objDocument.Normals[objDocument.Groups[iG].Faces[iF][iI].NormalIndex - 1].X;
                            float ny = objDocument.Normals[objDocument.Groups[iG].Faces[iF][iI].NormalIndex - 1].Y;
                            float nz = objDocument.Normals[objDocument.Groups[iG].Faces[iF][iI].NormalIndex - 1].Z;
                            float NORMAL_FIX = (float)Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
                            NORMAL_FIX = (NORMAL_FIX == 0) ? 1 : NORMAL_FIX;
                            nx /= NORMAL_FIX;
                            ny /= NORMAL_FIX;
                            nz /= NORMAL_FIX;

                            vertice.Normal = new Vector3(nx, ny, nz);
                        }


                        Vector4 color = new Vector4(
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].R,
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].G,
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].B,
                            objDocument.Vertices[objDocument.Groups[iG].Faces[iF][iI].VertexIndex - 1].A);

                        vertice.Color = color;
                        vertice.WeightMap = weightMap;

                        verticeListInObjFace.Add(vertice);


                        // --- verifica o vertice mais distante

                        float temp = position.X;
                        if (temp < 0)
                        {
                            temp *= -1;
                        }
                        if (temp > maximumCoordinate)
                        {
                            maximumCoordinate = temp;
                        }

                        temp = position.Y;
                        if (temp < 0)
                        {
                            temp *= -1;
                        }
                        if (temp > maximumCoordinate)
                        {
                            maximumCoordinate = temp;
                        }

                        temp = position.Z;
                        if (temp < 0)
                        {
                            temp *= -1;
                        }
                        if (temp > maximumCoordinate)
                        {
                            maximumCoordinate = temp;
                        }

                    }

                    if (verticeListInObjFace.Count >= 3)
                    {
                        for (int i = 2; i < verticeListInObjFace.Count; i++)
                        {
                            List<StartVertex> face = new List<StartVertex>();
                            face.Add(verticeListInObjFace[0]);
                            face.Add(verticeListInObjFace[i - 1]);
                            face.Add(verticeListInObjFace[i]);
                            facesList.Add(face);
                        }
                    }

                }

                if (importedMesh.FacesByMaterial.ContainsKey(materialNameInvariant))
                {
                    importedMesh.FacesByMaterial[materialNameInvariant].Faces.AddRange(facesList);
                }
                else
                {
                    importedMesh.FacesByMaterial.Add(materialNameInvariant, new StartFacesGroup(facesList));
                }

            }


            // faz a compressão das vertives
            if (optimizeStrips == true)
            {
                importedMesh.OptimizeStrips();
            }


            // calcula o fator de conversão
            quantizationScale = (maximumCoordinate * ModelEncodingConstants.GLOBAL_POSITION_SCALE) / short.MaxValue;

            // estrutura intermediaria
            StripMesh stripMesh = MeshTopologyBuilder.BuildStrips(importedMesh, out boundingBox);

            // estrutura final
            packedMesh = VifSegmentBuilder.PackSegments(stripMesh, quantizationScale);
        }

        public static EncodedBone[] EncodeBones((int ID, int parent, float x, float y, float z)[] Bones)
        {
            List<EncodedBone> bones = new List<EncodedBone>();

            for (int i = 0; i < Bones.Length; i++)
            {

                (float X, float Y, float Z) bonePos = (0, 0, 0);

                bonePos.X = Bones[i].x * ModelEncodingConstants.GLOBAL_POSITION_SCALE;
                bonePos.Y = Bones[i].z * ModelEncodingConstants.GLOBAL_POSITION_SCALE;
                bonePos.Z = Bones[i].y * -1 * ModelEncodingConstants.GLOBAL_POSITION_SCALE;

                if (bonePos.Z == 0f * -1f) { bonePos.Z = 0; }

                byte ParentID = (byte)Bones[i].parent;
                if (Bones[i].parent < 0)
                {
                    ParentID = 0xFF;
                }

                bones.Add(new EncodedBone((byte)(ushort)Bones[i].ID, ParentID, bonePos.X, bonePos.Y, bonePos.Z));
            }

            return bones.ToArray();
        }

    }
}
