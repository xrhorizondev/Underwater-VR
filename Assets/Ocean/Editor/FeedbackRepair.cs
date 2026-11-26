using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class FeedbackRepair {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/FeedbackRepair/";
 static MeshCollider[] floors;
 static float Floor(Vector3 p){float y=float.NegativeInfinity;foreach(var f in floors)if(f.Raycast(new Ray(new Vector3(p.x,40,p.z),Vector3.down),out var h,180))y=Mathf.Max(y,h.point.y);return y;}
 public static void Apply(){
  Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var report=new StringBuilder();
  floors=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.sharedMesh&&c.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  foreach(var anchor in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Weathered supplied anchor - "))){
   var start=anchor.position;var best=start;var rotation=anchor.rotation;float score=float.PositiveInfinity;
   var points=anchor.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>anchor.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
   var sample=points.Where((p,i)=>i%Mathf.Max(1,points.Length/240)==0).ToArray();
   for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++){
    var p=start+new Vector3(x*.5f,0,z*.5f);float dx=Floor(p+Vector3.right*.5f)-Floor(p-Vector3.right*.5f),dz=Floor(p+Vector3.forward*.5f)-Floor(p-Vector3.forward*.5f);
    var r=Quaternion.FromToRotation(Vector3.up,new Vector3(-dx,1,-dz).normalized)*Quaternion.Euler(90,anchor.eulerAngles.y,0);
    var matrix=Matrix4x4.TRS(p,r,anchor.lossyScale);var gaps=sample.Select(v=>{var w=matrix.MultiplyPoint3x4(v);return w.y-Floor(w);}).OrderBy(v=>v).ToArray();
    float cost=gaps.Take(Mathf.Max(1,gaps.Length/3)).Average()-gaps[0]+new Vector2(x,z).magnitude*.002f;
    if(cost<score){score=cost;best=p;rotation=r;}
   }
   anchor.SetPositionAndRotation(best,rotation);float lift=points.Max(v=>{var w=anchor.TransformPoint(v);return Floor(w)-w.y;});anchor.position+=Vector3.up*(lift-.01f);
   report.AppendLine(anchor.name+" moved="+Vector3.Distance(start,anchor.position)+" contactGap=-0.01 lowerSurfaceSpread="+score);Physics.SyncTransforms();
  }
  int groups=0;foreach(var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None)){
   ReefStableLod.ExtendViewingRange(group);groups++;
  }
  var octopus=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(a=>a.kind=="Octopus");var ground=octopus.GetComponent<OctopusGroundClearance>();
  var home=octopus.transform.position;var selected=home;float bestHeight=float.PositiveInfinity;
  // Prefer nearby level sand so the animation footprint can sit down naturally.
  for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++){
   var p=home+new Vector3(x*.4f,0,z*.4f);float height=ground.RequiredHeight(p);p.y=height;
   float cost=height-Floor(p)+new Vector2(x,z).magnitude*.003f;
   if(!float.IsInfinity(cost)&&cost<bestHeight&&octopus.GetComponent<ReefSphereBody>().Clear(p)){bestHeight=cost;selected=p;}
  }
  octopus.transform.position=selected;ground.terrainCell=.08f;ground.footCell=.08f;
  report.AppendLine("Octopus moved="+Vector3.Distance(home,selected)+" refined support cells=0.08m");report.AppendLine("LOD groups="+groups+" first detail retained to >=12m, final mesh retained to >=90m at 90 degree FOV");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"Placement.txt",report.ToString());
  OctopusGroundValidation.Run();Directory.CreateDirectory("Renders/CullingAnchorReview");CullingAnchorReview.Capture();
  FitShark();FinishPlacement();Debug.Log("FEEDBACK_REPAIR_APPLIED");
 }
 public static void Validate(){
  Directory.CreateDirectory(Output);Directory.CreateDirectory("Renders/CullingAnchorReview");CullingAnchorReview.Capture();EditorSceneManager.OpenScene(Scene);var report=new StringBuilder();
  foreach(var shark in UnityEngine.Object.FindObjectsByType<OpenWaterShark>(FindObjectsSortMode.None)){
   float minSpeed=100,maxStep=0,maxTurn=0,travel=0;int stalled=0;var start=shark.transform.position;
   for(int i=0;i<7200;i++){var p=shark.transform.position;var f=shark.transform.forward;shark.Step(1f/60,start+Vector3.up*30,Vector3.zero,false);float step=Vector3.Distance(p,shark.transform.position);if(step>.04f)report.AppendLine("Shark jump frame="+i+" from="+p.ToString("F4")+" to="+shark.transform.position.ToString("F4"));travel+=step;maxStep=Mathf.Max(maxStep,step);maxTurn=Mathf.Max(maxTurn,Vector3.Angle(f,shark.transform.forward)*60);if(i>300){minSpeed=Mathf.Min(minSpeed,step*60);if(step*60<.1f)stalled++;}if(float.IsNaN(step)||!shark.GetComponent<ReefSphereBody>().Clear(shark.transform.position))throw new Exception("Unsafe shark path at "+i);}
   report.AppendLine("Shark seconds=120 travel="+travel+" minSpeed="+minSpeed+" maxStep="+maxStep+" maxTurn="+maxTurn+" stalledFrames="+stalled);
   File.WriteAllText(Output+"Paths.txt",report.ToString());if(stalled>60||maxStep>.04f||maxTurn>21)throw new Exception("Shark motion failed: "+report);
  }
  EditorSceneManager.OpenScene(Scene);
  var types=new[]{typeof(TurtleSwimmer),typeof(StingraySwimmer),typeof(MarineSwimmer),typeof(SquidSwimmer),typeof(ReefCruiser),typeof(GrouperSwimController),typeof(DolphinSwimController),typeof(WreckCreature),typeof(CurledMorayEncounter)};
  foreach(var type in types){
   var actors=UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Cast<MonoBehaviour>().Where(a=>a.enabled).ToArray();
   foreach(var actor in actors){
    var method=type.GetMethods().First(m=>m.Name=="Step"&&m.GetParameters().Skip(1).All(p=>p.IsOptional));
    var args=method.GetParameters().Select((p,i)=>i==0?(object)(1f/60):p.DefaultValue).ToArray();
    float travel=0,maxStep=0,maxTurn=0;int stopped=0;
    for(int i=0;i<3600;i++){var p=actor.transform.position;var f=actor.transform.forward;method.Invoke(actor,args);var guard=actor.GetComponent<ReefAnimalClearance>();if(guard)guard.Enforce();float step=Vector3.Distance(p,actor.transform.position);if(i>0){travel+=step;maxStep=Mathf.Max(maxStep,step);maxTurn=Mathf.Max(maxTurn,Vector3.Angle(f,actor.transform.forward)*60);if(step<.00001f)stopped++;}if(float.IsNaN(step))throw new Exception("Invalid animal path: "+actor.name);}
    report.AppendLine(type.Name+" "+actor.name+" seconds=60 travel="+travel+" maxStep="+maxStep+" maxTurn="+maxTurn+" stoppedFrames="+stopped);File.WriteAllText(Output+"Paths.txt",report.ToString());
   }
   EditorSceneManager.OpenScene(Scene);
  }
  FinishValidation();
  report.AppendLine("School fish, octopus crawl/escape and diver collision validations passed.");File.WriteAllText(Output+"Paths.txt",report.ToString());Debug.Log("FEEDBACK_PATHS_VALIDATED");
 }
 public static void FinishValidation(){
  EditorSceneManager.OpenScene(Scene);FishMotionValidation.Run();OctopusGroundValidation.Validate();ReefCollisionValidation.Run();
  File.AppendAllText(Output+"Paths.txt","School fish, octopus crawl/escape and diver collision validations passed.\n");Debug.Log("FEEDBACK_PATHS_VALIDATED");
 }
 public static void FitShark(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var report=new StringBuilder();
  foreach(var shark in UnityEngine.Object.FindObjectsByType<OpenWaterShark>(FindObjectsSortMode.None)){
   var actor=shark.transform;var start=actor.position;var center=start+actor.right*shark.patrolRadius;var radial=start-center;float y=start.y;
   var guard=shark.GetComponent<ReefAnimalClearance>();
   for(int i=0;i<180;i++){
    var r=Quaternion.AngleAxis(i*2,Vector3.up)*radial;var p=center+r;var forward=Vector3.Cross(Vector3.up,r.normalized);
    foreach(float pitch in new[]{-.15f,0,.15f})if(guard.RequiredHeight(p,Quaternion.LookRotation((forward+Vector3.up*pitch).normalized),out float height))y=Mathf.Max(y,height+.6f);
   }
   if(y>5.5f)throw new Exception("Shark circuit cannot fit below surface");actor.position=new Vector3(start.x,y,start.z);report.AppendLine("Shark patrol depth from="+start.y+" to="+y+" radius="+shark.patrolRadius+" sampledCircuitPoses=540");
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"SharkPlacement.txt",report.ToString());Debug.Log("SHARK_PATROL_FITTED "+report);
 }
 public static void FinishPlacement(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var report=new StringBuilder();Physics.SyncTransforms();
  floors=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.sharedMesh&&c.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  const string art="Assets/UnderwaterArt/FeedbackRepair";Directory.CreateDirectory(art);AssetDatabase.Refresh();int index=0;
  foreach(var anchor in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Weathered supplied anchor - ")))foreach(var filter in anchor.GetComponentsInChildren<MeshFilter>()){
   var source=filter.sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);var vertices=mesh.vertices;var parents=Enumerable.Range(0,vertices.Length).ToArray();
   int Root(int i){while(parents[i]!=i){parents[i]=parents[parents[i]];i=parents[i];}return i;}
   void Join(int a,int b){parents[Root(a)]=Root(b);}
   var welded=new Dictionary<Vector3,int>();for(int i=0;i<vertices.Length;i++){if(welded.TryGetValue(vertices[i],out int j))Join(i,j);else welded.Add(vertices[i],i);}
   var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){Join(triangles[i],triangles[i+1]);Join(triangles[i],triangles[i+2]);}
   int rings=0;foreach(var part in Enumerable.Range(0,vertices.Length).GroupBy(Root).Select(g=>g.ToArray()).ToArray()){
    var bounds=new Bounds(filter.transform.TransformPoint(vertices[part[0]]),Vector3.zero);foreach(int i in part)bounds.Encapsulate(filter.transform.TransformPoint(vertices[i]));
    if(Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z)>.5f)continue;
    float gap=part.Min(i=>{var p=filter.transform.TransformPoint(vertices[i]);return p.y-Floor(p);});if(float.IsInfinity(gap))continue;
    foreach(int i in part)vertices[i]+=filter.transform.InverseTransformVector(Vector3.up*(-gap+.003f));rings++;
   }
   mesh.vertices=vertices;mesh.RecalculateBounds();string path=art+"/GroundedAnchor_"+index+++".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;foreach(var collider in anchor.GetComponentsInChildren<MeshCollider>())if(collider.sharedMesh==source)collider.sharedMesh=mesh;report.AppendLine(anchor.name+" grounded chain components="+rings);
  }
  foreach(var squid in UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None).Where(s=>s.holdDepth)){
   squid.Initialize();var start=squid.transform.position;var rotation=squid.transform.rotation;float original=squid.radius;bool found=false;
   for(float lift=0;lift<=4&&!found;lift+=.25f)for(float radius=original;radius>=.75f;radius-=.25f){bool clear=true;for(int i=0;i<121;i++){float phase=i*Mathf.PI*2/120;var p=start+Vector3.up*lift+rotation*Vector3.forward*(Mathf.Sin(phase)*radius);foreach(float y in new[]{-.15f,.15f}){var position=p+Vector3.up*y;var pose=rotation*Quaternion.Euler(0,Mathf.Sin(phase)*2,0);var guard=squid.GetComponent<ReefAnimalClearance>();if(!squid.ClearPose(position,pose)||guard&&guard.RequiredHeight(position,pose,out float height)&&height>position.y){clear=false;break;}}if(!clear)break;}if(clear){squid.radius=radius;squid.transform.position=start+Vector3.up*lift;found=true;break;}}
   if(!found)throw new Exception("No smooth squid patrol fits trench");report.AppendLine("Squid patrol radius from="+original+" to="+squid.radius+" verticalLift="+(squid.transform.position.y-start.y));
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"FinalPlacement.txt",report.ToString());Debug.Log("PLACEMENT_FINISHED "+report);
 }
}
}
