// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Common;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding.Structures;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding
{
    public static class VifSegmentBuilder
    {
        public static PackedMesh PackSegments(StripMesh stripMesh, float quantizationScale)
        {
            PackedMesh packedMesh = new PackedMesh();

            foreach (var item in stripMesh.Groups.OrderBy(a => a.Key).ToArray())
            {
                List<IntermediaryFace> remainingStrips = new List<IntermediaryFace>();
                remainingStrips.AddRange(item.Value.Faces);
                remainingStrips = remainingStrips.OrderByDescending(o => o.Vertexs.Count).ToList();

                List<FinalSegment> segments = new List<FinalSegment>();
                List<byte> usedBones = new List<byte>();

                int vertexCount = 0;

                FinalSegment currentSegment = null;
                List<IntermediaryWeightMap> IntermediaryWeightMapList = new List<IntermediaryWeightMap>();


                while (remainingStrips.Count != 0)
                {
                    if (currentSegment == null)
                    {
                        currentSegment = new FinalSegment();
                        IntermediaryWeightMapList.Clear();
                        segments.Add(currentSegment);
                    }

                    if (vertexCount <= 44)
                    {
                        IntermediaryFace intermediaryFace = null;
                        if (vertexCount + remainingStrips[0].Vertexs.Count <= 44 && CreateNewIntermediaryWeightMapList(IntermediaryWeightMapList, remainingStrips[0].WeightMapOnFace).Count() <= 15)
                        {
                            intermediaryFace = remainingStrips[0];
                            remainingStrips.RemoveAt(0);
                            vertexCount += intermediaryFace.Vertexs.Count;
                            IntermediaryWeightMapList = CreateNewIntermediaryWeightMapList(IntermediaryWeightMapList, intermediaryFace.WeightMapOnFace).ToList();
                        }
                        else
                        {
                            bool ok = false;
                            int iI = 1;
                            if (remainingStrips.Count > 1)
                            {
                                while (!ok)
                                {
                                    if (iI < remainingStrips.Count)
                                    {
                                        if (vertexCount + remainingStrips[iI].Vertexs.Count <= 44 && CreateNewIntermediaryWeightMapList(IntermediaryWeightMapList, remainingStrips[iI].WeightMapOnFace).Count() <= 15)
                                        {
                                            intermediaryFace = remainingStrips[iI];
                                            remainingStrips.RemoveAt(iI);
                                            vertexCount += intermediaryFace.Vertexs.Count;
                                            IntermediaryWeightMapList = CreateNewIntermediaryWeightMapList(IntermediaryWeightMapList, intermediaryFace.WeightMapOnFace).ToList();
                                            ok = true;
                                        }
                                        iI++;
                                    }
                                    else
                                    {
                                        vertexCount = int.MaxValue;
                                        ok = true;
                                        continue;
                                    }

                                }

                                if (intermediaryFace == null)
                                {
                                    vertexCount = int.MaxValue;
                                    continue;
                                }
                            }
                            else
                            {
                                vertexCount = int.MaxValue;
                                continue;
                            }

                        }

                        if (intermediaryFace != null)
                        {

                            ushort mountStatus = 0;
                            for (int v = 0; v < intermediaryFace.Vertexs.Count; v++)
                            {
                                IntermediaryVertex intermediaryVertex = intermediaryFace.Vertexs[v];
                                FinalVertex finalVertex = new FinalVertex();

                                finalVertex.PosX = Utils.ParseFloatToShort(intermediaryVertex.PosX / quantizationScale);
                                finalVertex.PosY = Utils.ParseFloatToShort(intermediaryVertex.PosY / quantizationScale);
                                finalVertex.PosZ = Utils.ParseFloatToShort(intermediaryVertex.PosZ / quantizationScale);

                                finalVertex.NormalX = Utils.ParseFloatToShort(intermediaryVertex.NormalX * 127f);
                                finalVertex.NormalY = Utils.ParseFloatToShort(intermediaryVertex.NormalY * 127f);
                                finalVertex.NormalZ = Utils.ParseFloatToShort(intermediaryVertex.NormalZ * 127f);

                                finalVertex.TextureU = Utils.ParseFloatToShort(intermediaryVertex.TextureU * 255f);
                                finalVertex.TextureV = Utils.ParseFloatToShort(intermediaryVertex.TextureV * 255f);

                                finalVertex.ColorR = Utils.ParseFloatToShort(intermediaryVertex.ColorR * 0x80);
                                finalVertex.ColorG = Utils.ParseFloatToShort(intermediaryVertex.ColorG * 0x80);
                                finalVertex.ColorB = Utils.ParseFloatToShort(intermediaryVertex.ColorB * 0x80);
                                finalVertex.ColorA = Utils.ParseFloatToShort(intermediaryVertex.ColorA * 0x80);

                                if (v >= 2)
                                {
                                    if (mountStatus == 0x0000 || mountStatus == 0xFFFF)
                                    {
                                        mountStatus = 0x0001;
                                        finalVertex.IndexMount = 0x0001;
                                    }
                                    else if (mountStatus == 0x0001)
                                    {
                                        mountStatus = 0xFFFF;
                                        finalVertex.IndexMount = 0xFFFF;
                                    }

                                }

                                int IndexBoneID1 = 0;
                                int IndexBoneID2 = 0;
                                int IndexBoneID3 = 0;


                                if (intermediaryVertex.Links >= 1)
                                {
                                    if (!usedBones.Contains((byte)intermediaryVertex.BoneID1))
                                    {
                                        usedBones.Add((byte)intermediaryVertex.BoneID1);
                                    }
                                    IndexBoneID1 = usedBones.IndexOf((byte)intermediaryVertex.BoneID1);
                                }

                                if (intermediaryVertex.Links >= 2)
                                {
                                    if (!usedBones.Contains((byte)intermediaryVertex.BoneID2))
                                    {
                                        usedBones.Add((byte)intermediaryVertex.BoneID2);
                                    }
                                    IndexBoneID2 = usedBones.IndexOf((byte)intermediaryVertex.BoneID2);
                                }

                                if (intermediaryVertex.Links >= 3)
                                {
                                    if (!usedBones.Contains((byte)intermediaryVertex.BoneID3))
                                    {
                                        usedBones.Add((byte)intermediaryVertex.BoneID3);
                                    }
                                    IndexBoneID3 = usedBones.IndexOf((byte)intermediaryVertex.BoneID3);
                                }


                                FinalWeightMap map = new FinalWeightMap();
                                map.Links = intermediaryVertex.Links;
                                map.BoneID1 = IndexBoneID1 * 4;
                                map.Weight1 = intermediaryVertex.Weight1;
                                map.BoneID2 = IndexBoneID2 * 4;
                                map.Weight2 = intermediaryVertex.Weight2;
                                map.BoneID3 = IndexBoneID3 * 4;
                                map.Weight3 = intermediaryVertex.Weight3;

                                if (!currentSegment.WeightMapList.Contains(map))
                                {
                                    currentSegment.WeightMapList.Add(map);
                                }

                                int index = currentSegment.WeightMapList.IndexOf(map);
                                finalVertex.WeightMapReference = (ushort)(uint)(index * 2);

                                currentSegment.VertexList.Add(finalVertex);

                            }

                        }


                    }
                    else
                    {
                        currentSegment = null;
                        IntermediaryWeightMapList.Clear();
                        vertexCount = 0;
                    }

                }


                for (int i = 0; i < segments.Count; i++)
                {
                    for (int l = 2; l < segments[i].VertexList.Count; l++)
                    {
                        if (segments[i].VertexList[l].IndexMount == 0x0000)
                        {
                            var vertex = segments[i].VertexList[l];
                            vertex.IndexComplement = 0x0001;
                            segments[i].VertexList[l] = vertex;
                        }

                    }
                }


                //calc
                const int maxSegs = 255;
                int Parts = segments.Count / maxSegs;
                int _rest = segments.Count % maxSegs;
                Parts += _rest != 0 ? 1 : 0;

                if (Parts == 1)
                {
                    FinalNode node = new FinalNode();
                    node.MaterialName = item.Key;
                    node.BonesIDs = usedBones.ToArray();
                    node.Segments = segments.ToArray();
                    packedMesh.Nodes.Add(node);
                }
                else if (Parts > 1)
                {
                    for (int i = 0; i < Parts; i++)
                    {
                        var segs = segments.Skip(i * maxSegs).Take(maxSegs).ToArray();

                        FinalNode node = new FinalNode();
                        node.MaterialName = item.Key;
                        node.BonesIDs = usedBones.ToArray();
                        node.Segments = segs;
                        packedMesh.Nodes.Add(node);
                    }
                }
              
            }

            return packedMesh;
        }

        private static IEnumerable<IntermediaryWeightMap> CreateNewIntermediaryWeightMapList(IEnumerable<IntermediaryWeightMap> List, IEnumerable<IntermediaryWeightMap> ToAdd)
        {
            List<IntermediaryWeightMap> res = new List<IntermediaryWeightMap>();
            res.AddRange(List);
            foreach (var item in ToAdd)
            {
                if (!res.Contains(item))
                {
                    res.Add(item);
                }
            }
            return res;
        }

    }
}
