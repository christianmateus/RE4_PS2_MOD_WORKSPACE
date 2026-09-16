using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;
namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;
public sealed record FcvSmdImportResult(FcvAnimation Animation,int FramesRead,int RotationTracksUpdated,bool RootTranslationUpdated);
public static class FcvSmdRoundTrip
{
 static readonly CultureInfo C=CultureInfo.InvariantCulture;
 const int MetadataVersion=2;const float SmdScale=0.01f;
 static readonly Quaternion Ps2ToSmdBasis=Quaternion.CreateFromAxisAngle(Vector3.UnitX,MathF.PI/2f);
 public static void Export(string path,Ps2BinSkeleton sk,FcvAnimation anim,EnemyModelScene? model=null)
 {
  if(sk.Bones.Count==0||anim.FrameCount==0)throw new InvalidDataException("Skeleton or animation is empty.");
  using(var w=new StreamWriter(path,false,Encoding.ASCII)){w.NewLine="\n";WriteNodes(w,sk);w.WriteLine("skeleton");for(int f=0;f<anim.FrameCount;f++){w.WriteLine($"time {f}");var p=FcvSkeletonEvaluator.Evaluate(sk,anim,f,false);for(int i=0;i<sk.Bones.Count;i++){var(pos,rot)=Local(sk,p,i);pos=ToSmdPosition(pos);rot=ToSmdRotation(rot);var e=SmdEuler(rot);w.WriteLine($"{i} {N(pos.X)} {N(pos.Y)} {N(pos.Z)} {N(e.X)} {N(e.Y)} {N(e.Z)}");}}w.WriteLine("end");}
  if(model!=null)WriteReferenceModel(ModelPath(path),model,sk,anim);
  var meta=new Meta(MetadataVersion,30,anim.FrameCount,Path.GetFileName(anim.FilePath),sk.Bones.Select(b=>new MetaBone(b.Index,b.Id,b.ParentIndex)).ToArray());File.WriteAllText(path+".fcv.json",JsonSerializer.Serialize(meta,new JsonSerializerOptions{WriteIndented=true}));
 }
 public static FcvSmdImportResult Import(string path,Ps2BinSkeleton sk,FcvAnimation template)
 {
  var frames=Read(path,sk);if(frames.Count==0||frames.Keys.First()!=0)throw new InvalidDataException("SMD timeline must start at frame 0.");int count=frames.Keys.Last()+1;if(count>ushort.MaxValue)throw new InvalidDataException("Too many SMD frames.");for(int f=0;f<count;f++)if(!frames.ContainsKey(f))throw new InvalidDataException($"Missing SMD frame {f}.");Validate(path,sk);
  var result=Clone(template,(ushort)count);var world=new Quaternion[count,sk.Bones.Count];for(int f=0;f<count;f++)for(int i=0;i<sk.Bones.Count;i++){if(!frames[f].TryGetValue(i,out var p))throw new InvalidDataException($"Frame {f}: missing bone {i}.");var q=FromSmdRotation(Q(p.Rot));int parent=sk.Bones[i].ParentIndex;world[f,i]=parent<0?q:Quaternion.Normalize(world[f,parent]*q);}
  int changed=0;bool root=false;foreach(var t in result.Tracks){if(!sk.FirstIndexById.TryGetValue(t.NodeId,out int bi))continue;int enc=t.DataType>>4;if(t.Type is 0x02 or 0x10 or 0x40&&RotEnc(enc)){var v=Enumerable.Range(0,count).Select(f=>FcvEuler(t.Type==0x10?world[f,bi]:FromSmdRotation(Q(frames[f][bi].Rot)))).ToArray();Set(t.X,v.Select(x=>x.X),enc,true);Set(t.Y,v.Select(x=>x.Y),enc,true);Set(t.Z,v.Select(x=>x.Z),enc,true);changed++;}else if(sk.Bones[bi].ParentIndex<0&&t.Type is 0x01 or 0x04&&enc==0){var bind=sk.Bones[bi].LocalPosition;var v=Enumerable.Range(0,count).Select(f=>FromSmdPosition(frames[f][bi].Pos)).ToArray();Set(t.X,v.Select(x=>t.Type==1?x.X-bind.X:x.X),0,false);Set(t.Y,v.Select(x=>t.Type==1?x.Y-bind.Y:x.Y),0,false);Set(t.Z,v.Select(x=>t.Type==1?x.Z-bind.Z:x.Z),0,false);root=true;}}
  return new(result,count,changed,root);
 }
 static (Vector3,Quaternion) Local(Ps2BinSkeleton s,FcvSkeletonPose p,int i){int parent=s.Bones[i].ParentIndex;if(parent<0)return(p.WorldPositions[i],p.WorldRotations[i]);var inv=Quaternion.Inverse(p.WorldRotations[parent]);return(Vector3.Transform(p.WorldPositions[i]-p.WorldPositions[parent],inv),Quaternion.Normalize(inv*p.WorldRotations[i]));}
 static Quaternion Q(Vector3 e)=>Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitZ,e.Z)*Quaternion.CreateFromAxisAngle(Vector3.UnitY,e.Y)*Quaternion.CreateFromAxisAngle(Vector3.UnitX,e.X));
 static Vector3 ToSmdPosition(Vector3 p)=>new Vector3(p.X,-p.Z,p.Y)*SmdScale;
 static Vector3 ToSmdMeshPosition(Vector3 p)=>new Vector3(p.X,-p.Z,p.Y);
 static Vector3 FromSmdPosition(Vector3 p)=>new Vector3(p.X,p.Z,-p.Y)/SmdScale;
 static Quaternion ToSmdRotation(Quaternion q)=>Quaternion.Normalize(Ps2ToSmdBasis*q*Quaternion.Inverse(Ps2ToSmdBasis));
 static Quaternion FromSmdRotation(Quaternion q)=>Quaternion.Normalize(Quaternion.Inverse(Ps2ToSmdBasis)*q*Ps2ToSmdBasis);
 static void WriteNodes(StreamWriter w,Ps2BinSkeleton sk){w.WriteLine("version 1");w.WriteLine("nodes");foreach(var b in sk.Bones)w.WriteLine($"{b.Index} \"bone_{b.Id:X2}_{b.Index:D2}\" {(b.ParentIndex<0?-1:b.ParentIndex)}");w.WriteLine("end");}
 static string ModelPath(string animationPath)=>Path.Combine(Path.GetDirectoryName(Path.GetFullPath(animationPath))!,Path.GetFileNameWithoutExtension(animationPath)+"_model.smd");
 static void WriteReferenceModel(string path,EnemyModelScene model,Ps2BinSkeleton sk,FcvAnimation anim)
 {
  using var w=new StreamWriter(path,false,Encoding.ASCII);w.NewLine="\n";WriteNodes(w,sk);w.WriteLine("skeleton");w.WriteLine("time 0");FcvSkeletonPose pose=FcvSkeletonEvaluator.Evaluate(sk,anim,0,false);
  for(int i=0;i<sk.Bones.Count;i++){var(pos,rot)=Local(sk,pose,i);pos=ToSmdPosition(pos);rot=ToSmdRotation(rot);Vector3 e=SmdEuler(rot);w.WriteLine($"{i} {N(pos.X)} {N(pos.Y)} {N(pos.Z)} {N(e.X)} {N(e.Y)} {N(e.Z)}");}
  w.WriteLine("end");WriteMesh(w,model,sk,pose,FcvSkeletonEvaluator.Evaluate(sk,null,0,false));
 }
 static void WriteMesh(StreamWriter w,EnemyModelScene model,Ps2BinSkeleton sk,FcvSkeletonPose pose,FcvSkeletonPose bind)
 {
  IEnumerable<EnemyModelPart> parts=model.Parts;
  if(Path.GetFileNameWithoutExtension(model.SourcePath).Equals("pl00",StringComparison.OrdinalIgnoreCase))
  {
   HashSet<int> leonCore=new(){0,1,2,3,4,5};EnemyModelPart? left=model.Parts.Where(p=>p.BinIndex is >=10 and <=17&&p.BoundsMax.X<0).OrderBy(p=>p.BinIndex).FirstOrDefault();EnemyModelPart? right=model.Parts.Where(p=>p.BinIndex is >=10 and <=17&&p.BoundsMin.X>0).OrderBy(p=>p.BinIndex).FirstOrDefault();if(left!=null)leonCore.Add(left.BinIndex);if(right!=null)leonCore.Add(right.BinIndex);parts=parts.Where(p=>leonCore.Contains(p.BinIndex));
  }
  else
  {
   IReadOnlySet<int>? core=EnemyModelPartCatalog.GetAutomaticCoreParts(model.EnemyType,0);if(core!=null&&core.Count>0&&core.All(id=>model.Parts.Any(p=>p.DatEntryIndex==id)))parts=parts.Where(p=>core.Contains(p.DatEntryIndex));
  }
  EnemyModelPart[] visible=parts.ToArray();if(visible.Sum(p=>p.Triangles.Count)==0)return;w.WriteLine("triangles");
  foreach(var part in visible)foreach(var t in part.Triangles){Vector3 a=Skin(t.A,t.SkinA,sk,pose,bind),b=Skin(t.B,t.SkinB,sk,pose,bind),c=Skin(t.C,t.SkinC,sk,pose,bind);Vector3 n=Vector3.Cross(b-a,c-a);if(n.LengthSquared()<0.0000001f)continue;n=Vector3.Normalize(new Vector3(n.X,-n.Z,n.Y));w.WriteLine($"re4_part_{part.BinIndex:D2}_dat_{part.DatEntryIndex:D3}_tex_{t.TextureIndex:D2}");WriteVertex(w,a,n,t.UvA,t.SkinA,sk);WriteVertex(w,b,n,t.UvB,t.SkinB,sk);WriteVertex(w,c,n,t.UvC,t.SkinC,sk);}
  w.WriteLine("end");
 }
 static Vector3 Skin(Vector3 v,EnemyVertexSkin skin,Ps2BinSkeleton sk,FcvSkeletonPose pose,FcvSkeletonPose bind)
 {
  if(skin.Count<=0)return v;Vector3 result=Vector3.Zero;float used=0;
  void Add(EnemySkinInfluence x){if(x.Weight<=0||!sk.FirstIndexById.TryGetValue(x.BoneId,out int i))return;Vector3 bindPosition=bind.WorldPositions[i]*SmdScale,posePosition=pose.WorldPositions[i]*SmdScale;Vector3 local=Vector3.Transform(v-bindPosition,Quaternion.Inverse(bind.WorldRotations[i]));result+=(Vector3.Transform(local,pose.WorldRotations[i])+posePosition)*x.Weight;used+=x.Weight;}
  Add(skin.A);if(skin.Count>1)Add(skin.B);if(skin.Count>2)Add(skin.C);return used>0.000001f?result/used:v;
 }
 static void WriteVertex(StreamWriter w,Vector3 source,Vector3 normal,Vector2 uv,EnemyVertexSkin skin,Ps2BinSkeleton sk)
 {
  Vector3 p=ToSmdMeshPosition(source);var inf=new List<(int Bone,float Weight)>();void Add(EnemySkinInfluence i){if(i.Weight>0&&sk.FirstIndexById.TryGetValue(i.BoneId,out int bone))inf.Add((bone,i.Weight));}
  if(skin.Count>0)Add(skin.A);if(skin.Count>1)Add(skin.B);if(skin.Count>2)Add(skin.C);if(inf.Count==0)inf.Add((0,1));float total=inf.Sum(i=>i.Weight);int primary=inf.OrderByDescending(i=>i.Weight).First().Bone;
  var fields=new List<string>{primary.ToString(C),N(p.X),N(p.Y),N(p.Z),N(normal.X),N(normal.Y),N(normal.Z),N(uv.X),N(uv.Y),inf.Count.ToString(C)};foreach(var i in inf){fields.Add(i.Bone.ToString(C));fields.Add(N(i.Weight/total));}w.WriteLine(string.Join(" ",fields));
 }
 static Vector3 SmdEuler(Quaternion value){var q=Quaternion.Normalize(value);float x=MathF.Atan2(2*(q.W*q.X+q.Y*q.Z),1-2*(q.X*q.X+q.Y*q.Y));float y=MathF.Asin(Math.Clamp(2*(q.W*q.Y-q.Z*q.X),-1,1));float z=MathF.Atan2(2*(q.W*q.Z+q.X*q.Y),1-2*(q.Y*q.Y+q.Z*q.Z));return new(x,y,z);}
 static Vector3 FcvEuler(Quaternion value){var q=Quaternion.Normalize(value);float x=MathF.Asin(Math.Clamp(2*(q.W*q.X-q.Y*q.Z),-1,1));float y=MathF.Atan2(2*(q.W*q.Y+q.X*q.Z),1-2*(q.X*q.X+q.Y*q.Y));float z=MathF.Atan2(2*(q.W*q.Z+q.X*q.Y),1-2*(q.X*q.X+q.Z*q.Z));return new(x,y,z);}
 static void Set(FcvAxis a,IEnumerable<float> values,int enc,bool rotation){a.Keys.Clear();int f=0;foreach(float v in values)a.Keys.Add(new((ushort)f++,rotation?Encode(v,enc):v,0,0,0));}
 static double Encode(float r,int e)=>e switch{0 or 1=>r,4 or 5 or 6=>Math.Clamp(Math.Round(r/Math.PI*32767),short.MinValue,short.MaxValue),8 or 9 or 10=>Math.Clamp(Math.Round(r/Math.PI*127),sbyte.MinValue,sbyte.MaxValue),_=>0};
 static bool RotEnc(int e)=>e is 0 or 1 or 4 or 5 or 6 or 8 or 9 or 10;static string N(float v)=>v.ToString("R",C);
 static SortedDictionary<int,Dictionary<int,Pose>> Read(string path,Ps2BinSkeleton sk)
 {
  var r=new SortedDictionary<int,Dictionary<int,Pose>>();var nodeMap=ReadNodeMap(path,sk);bool section=false;int time=-1;
  foreach(string raw in File.ReadLines(path))
  {
   string l=raw.Trim();if(l.Length==0)continue;if(!section){if(l.Equals("skeleton",StringComparison.OrdinalIgnoreCase))section=true;continue;}if(l.Equals("end",StringComparison.OrdinalIgnoreCase))break;
   if(l.StartsWith("time ",StringComparison.OrdinalIgnoreCase)){if(!int.TryParse(l.AsSpan(5).Trim(),out time)||time<0)throw new InvalidDataException("Invalid SMD time.");r.TryAdd(time,new());continue;}if(time<0)continue;
   var x=l.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(x.Length<7||!int.TryParse(x[0],out int smdNode)||!nodeMap.TryGetValue(smdNode,out int node))throw new InvalidDataException("Invalid or unknown SMD bone in pose.");var v=x.Skip(1).Take(6).Select(n=>float.Parse(n,NumberStyles.Float,C)).ToArray();if(!r[time].TryAdd(node,new(new(v[0],v[1],v[2]),new(v[3],v[4],v[5]))))throw new InvalidDataException($"Frame {time}: duplicate bone {node} after Blender remapping.");
  }
  return r;
 }
 static Dictionary<int,int> ReadNodeMap(string path,Ps2BinSkeleton sk)
 {
  var map=new Dictionary<int,int>();var used=new HashSet<int>();bool section=false;
  foreach(string raw in File.ReadLines(path))
  {
   string l=raw.Trim();if(!section){if(l.Equals("nodes",StringComparison.OrdinalIgnoreCase))section=true;continue;}if(l.Equals("end",StringComparison.OrdinalIgnoreCase))break;
   int quote1=l.IndexOf('"'),quote2=quote1<0?-1:l.IndexOf('"',quote1+1);if(quote1<=0||quote2<=quote1||!int.TryParse(l.AsSpan(0,quote1).Trim(),out int smdNode))throw new InvalidDataException("Invalid SMD node declaration.");string name=l[(quote1+1)..quote2];int split=name.LastIndexOf('_');int original=-1;if(split>=0)int.TryParse(name.AsSpan(split+1),NumberStyles.None,C,out original);
   if(original<0||original>=sk.Bones.Count||!name.StartsWith($"bone_{sk.Bones[original].Id:X2}_",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException($"SMD bone '{name}' does not belong to the loaded skeleton.");if(!map.TryAdd(smdNode,original)||!used.Add(original))throw new InvalidDataException($"Duplicate SMD bone mapping for '{name}'.");
  }
  if(map.Count!=sk.Bones.Count)throw new InvalidDataException($"SMD has {map.Count} bones, but the loaded skeleton has {sk.Bones.Count}.");return map;
 }
 static void Validate(string path,Ps2BinSkeleton sk){string p=path+".fcv.json";if(!File.Exists(p))return;var m=JsonSerializer.Deserialize<Meta>(File.ReadAllText(p))??throw new InvalidDataException("Invalid FCV metadata.");if(m.Version!=MetadataVersion)throw new InvalidDataException("SMD antigo: exporte novamente para aplicar escala e eixos do Blender.");if(m.Bones.Length!=sk.Bones.Count||m.Bones.Any(b=>b.SmdNode<0||b.SmdNode>=sk.Bones.Count||sk.Bones[b.SmdNode].Id!=b.FcvBoneId))throw new InvalidDataException("SMD belongs to another skeleton.");}
 static FcvAnimation Clone(FcvAnimation s,ushort frames){var a=new FcvAnimation{FilePath=s.FilePath,FrameCount=frames,TrackCount=s.TrackCount};foreach(var x in s.Tracks){var t=new FcvTrack{Index=x.Index,NodeId=x.NodeId,Type=x.Type,DataType=x.DataType,Offset=x.Offset,PhysicalOrder=x.PhysicalOrder};Copy(x.X,t.X);Copy(x.Y,t.Y);Copy(x.Z,t.Z);a.Tracks.Add(t);}return a;}static void Copy(FcvAxis x,FcvAxis y){foreach(var k in x.Keys)y.Keys.Add(k);}
 readonly record struct Pose(Vector3 Pos,Vector3 Rot);sealed record Meta(int Version,int Fps,int FrameCount,string SourceFcv,MetaBone[] Bones);sealed record MetaBone(int SmdNode,byte FcvBoneId,int ParentSmdNode);
}
