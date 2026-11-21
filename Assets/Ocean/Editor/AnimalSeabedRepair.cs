using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class AnimalSeabedRepair {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity", FieldPath="Assets/UnderwaterArt/AnimalSeabedField.asset";
 static T[] All<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
 static Transform[] Animals(){
  var roots=new HashSet<Transform>(All<SeaLife>().Where(a=>a.kind!="Fire coral"&&a.kind!="Coral"&&a.kind!="Ray tail"&&a.kind!="Anemone").Select(a=>a.transform));
  foreach(var c in All<MonoBehaviour>())if(c is FishAgent||c is FishBodyMotion||c is SquidSwimmer||c is TurtleSwimmer||c is MarineSwimmer||c is StingraySwimmer||c is WreckCreature||c is OpenWaterShark||c is ReefCruiser)roots.Add(c.transform);
  return roots.Where(t=>!roots.Any(p=>p!=t&&t.IsChildOf(p))).OrderBy(t=>t.position.z).ToArray();
 }
 static Renderer[] Surfaces(Transform actor)=>actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
 static Vector3[] Points(Renderer r){
  if(r is SkinnedMeshRenderer skin){
   if(skin.bones.Length>0)return OctopusRepairValidation.Points(skin);
   return skin.sharedMesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();
  }
  var filter=r.GetComponent<MeshFilter>();return filter&&filter.sharedMesh?filter.sharedMesh.vertices.Select(v=>r.transform.TransformPoint(v)).ToArray():Array.Empty<Vector3>();
 }
 static void Include(ref Bounds bounds,ref bool first,Vector3 p){if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
 static Vector3 ShaderTravel(Renderer renderer){
  Vector3 travel=Vector3.zero;var mesh=renderer is SkinnedMeshRenderer s?s.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;var b=mesh?mesh.bounds:new Bounds();
  foreach(var m in renderer.sharedMaterials){if(!m||!m.shader)continue;Vector3 p=Vector3.zero;switch(m.shader.name){
   case "Underwater/Living Fish Skin":p=new Vector3(.12f,.008f,0);break;
   case "Underwater/Manta Skin":p=new Vector3(.04f,.9f,0);break;
   case "Underwater/Stingray Skin":p=new Vector3(.22f,.72f,0);break;
   case "Underwater/Rigged Ray Skin":if(m.HasProperty("_ProceduralStroke")&&m.GetFloat("_ProceduralStroke")>.5f)p=new Vector3(.05f,.23f,0);break;
   case "Underwater/Loggerhead Skin":p=new Vector3(.25f,.5f,.12f);break;
   case "Underwater/Reef Shark Skin":p.x=.17f;break;
   case "Underwater/Wreck Creature Skin":p=new Vector3(.045f,.19f,0);break;
   case "Underwater/Squid Skin":p=new Vector3(.8f,.8f,0);break;
   case "Underwater/Translucent Jelly Tissue":p=new Vector3((Mathf.Abs(b.center.x)+b.extents.x+Mathf.Abs(renderer.transform.localPosition.x))*.04f,(Mathf.Abs(b.center.y)+b.extents.y+Mathf.Abs(renderer.transform.localPosition.y))*.07f,(Mathf.Abs(b.center.z)+b.extents.z+Mathf.Abs(renderer.transform.localPosition.z))*.04f);break;
   default:if(m.HasProperty("_Sway"))p.x+=Mathf.Abs(m.GetFloat("_Sway"))*(Mathf.Abs(b.center.y)+b.extents.y);if(m.HasProperty("_Swim"))p.x+=Mathf.Abs(m.GetFloat("_Swim"));break;
  }travel=Vector3.Max(travel,p);}return travel;
 }
 public static Bounds Envelope(Transform actor){
  var renderers=Surfaces(actor);bool first=true;Bounds bounds=default;
  void Sample(){foreach(var r in renderers)foreach(var p in Points(r))Include(ref bounds,ref first,actor.InverseTransformPoint(p));}
  Sample();
  foreach(var detail in actor.GetComponentsInChildren<ReefFishDetail>())foreach(var mesh in new[]{detail.full,detail.medium,detail.distant}.Where(m=>m).Distinct())foreach(var p in mesh.vertices)Include(ref bounds,ref first,actor.InverseTransformPoint(detail.transform.TransformPoint(p)));
  foreach(var animation in actor.GetComponentsInChildren<Animation>()){
   var clips=animation.Cast<AnimationState>().Select(s=>s.clip).Where(c=>c).Distinct().ToArray();
   foreach(var clip in clips){int frames=Mathf.Clamp(Mathf.CeilToInt(clip.length*clip.frameRate),32,600);for(int sample=0;sample<=frames;sample++){clip.SampleAnimation(animation.gameObject,clip.length*sample/frames);Sample();}}
   if(animation.clip)animation.clip.SampleAnimation(animation.gameObject,0);
  }
  if(first)throw new Exception("No animal surface: "+actor.name);
  var pad=Vector3.zero;foreach(var r in renderers){var travel=ShaderTravel(r);var matrix=actor.worldToLocalMatrix*r.transform.localToWorldMatrix;var x=matrix.MultiplyVector(Vector3.right*travel.x);var y=matrix.MultiplyVector(Vector3.up*travel.y);var z=matrix.MultiplyVector(Vector3.forward*travel.z);pad=Vector3.Max(pad,new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));}
  // Extra interpolation and procedural head-motion reserve, in actor units.
  pad+=bounds.size*.035f+Vector3.one*.015f;bounds.Expand(pad*2);return bounds;
 }
 static ReefSeabedField BakeSand(){
  var floors=All<MeshFilter>().Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray();if(floors.Length<8)throw new Exception("Missing authored sand meshes");
  var existing=AssetDatabase.LoadAssetAtPath<ReefSeabedField>(FieldPath);var field=existing?existing:ScriptableObject.CreateInstance<ReefSeabedField>();field.cell=.5f;
  float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minZ=minX,maxZ=maxX;
  foreach(var floor in floors)foreach(var v in floor.sharedMesh.vertices){var p=floor.transform.TransformPoint(v);minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
  field.origin=new Vector2(Mathf.Floor(minX/field.cell)*field.cell,Mathf.Floor(minZ/field.cell)*field.cell);field.width=Mathf.CeilToInt((maxX-field.origin.x)/field.cell)+1;field.depth=Mathf.CeilToInt((maxZ-field.origin.y)/field.cell)+1;
  field.heights=Enumerable.Repeat(float.NegativeInfinity,field.width*field.depth).ToArray();int triangles=0;
  foreach(var floor in floors){var vertices=floor.sharedMesh.vertices.Select(p=>floor.transform.TransformPoint(p)).ToArray();var indices=floor.sharedMesh.triangles;
   for(int i=0;i<indices.Length;i+=3){var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];triangles++;
    int x0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x,Mathf.Min(b.x,c.x))-field.origin.x)/field.cell),0,field.width-1),x1=Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.x,Mathf.Max(b.x,c.x))-field.origin.x)/field.cell),0,field.width-1);
    int z0=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z,Mathf.Min(b.z,c.z))-field.origin.y)/field.cell),0,field.depth-1),z1=Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.z,Mathf.Max(b.z,c.z))-field.origin.y)/field.cell),0,field.depth-1);
    float top=Mathf.Max(a.y,Mathf.Max(b.y,c.y));for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){int index=x+z*field.width;field.heights[index]=Mathf.Max(field.heights[index],top);}
   }
  }
  // Unknown edge cells stay blocked rather than allowing an animal to sink.
  for(int i=0;i<field.heights.Length;i++)if(float.IsInfinity(field.heights[i]))field.heights[i]=ReefSeabedField.MissingHeight;
  if(!existing)AssetDatabase.CreateAsset(field,FieldPath);EditorUtility.SetDirty(field);
  Debug.Log("ANIMAL_SAND_FIELD meshes="+floors.Length+" triangles="+triangles+" cells="+field.heights.Length);return field;
 }
 [Serializable] public class ActorResult {public string name,kind;public Vector3 position;public float initialClearanceDeficit,lift;public Bounds envelope;}
 [Serializable] public class Report {public string status;public int animals,guarded,raised;public float maxLift;public long checkedVertices;public float minimumGap;public ActorResult[] actors;}
 static string Kind(Transform actor)=>actor.GetComponent<SeaLife>()?.kind??actor.name;
 [MenuItem("Underwater/Repair All Animal Sand Clearance")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var field=BakeSand();var actors=Animals();var results=new List<ActorResult>();int raised=0;float maxLift=0;
  foreach(var actor in actors){
   if(actor.GetComponent<OctopusGroundClearance>()){Debug.Log("ANIMAL_SAND_DEDICATED "+actor.name);continue;}
   var guard=actor.GetComponent<ReefAnimalClearance>();if(!guard)guard=actor.gameObject.AddComponent<ReefAnimalClearance>();guard.seabed=field;guard.animatedBody=Envelope(actor);
   var before=actor.position;if(!guard.RequiredHeight(before,actor.rotation,out float required))throw new Exception("Animal outside sand field: "+actor.name);
   if(required>6)throw new Exception("Animal clearance exceeds water bounds: "+actor.name+" required="+required+" envelope="+guard.animatedBody);
   float lift=guard.Enforce();if(lift>.0001f)raised++;maxLift=Mathf.Max(maxLift,lift);
   results.Add(new ActorResult{name=actor.name,kind=Kind(actor),position=actor.position,initialClearanceDeficit=required-before.y,lift=lift,envelope=guard.animatedBody});EditorUtility.SetDirty(guard);
   if(lift>.001f)Debug.Log("ANIMAL_SAND_RAISED "+Kind(actor)+" lift="+lift+" from="+before+" to="+actor.position);
  }
  // Record only safe, neutral animation poses in the saved scene.
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  var report=new Report{status="placement-repaired",animals=actors.Length,guarded=results.Count,raised=raised,maxLift=maxLift,actors=results.ToArray()};Directory.CreateDirectory("Renders/AnimalSeabedAudit");File.WriteAllText("Renders/AnimalSeabedAudit/report.json",JsonUtility.ToJson(report,true));
  Debug.Log("ANIMAL_SAND_REPAIRED animals="+actors.Length+" guarded="+results.Count+" raised="+raised+" maximumLift="+maxLift);
  Validate();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);var actors=Animals();long vertices=0;float minGap=float.PositiveInfinity;
  foreach(var actor in actors){
   var guard=actor.GetComponent<ReefAnimalClearance>();if(!guard){if(actor.GetComponent<OctopusGroundClearance>())continue;throw new Exception("Unguarded animal: "+actor.name);}
   var home=actor.position;var rotation=actor.rotation;var renderers=Surfaces(actor);
   // Half-frame phases differ from the footprint bake, including every source clip.
   foreach(var animation in actor.GetComponentsInChildren<Animation>())foreach(var clip in animation.Cast<AnimationState>().Select(s=>s.clip).Where(c=>c).Distinct())for(int phase=0;phase<65;phase++){
    clip.SampleAnimation(animation.gameObject,clip.length*(phase+.5f)/65);
    foreach(var r in renderers)foreach(var world in Points(r)){
     var local=actor.InverseTransformPoint(world);if(!guard.animatedBody.Contains(local))throw new Exception("Animation escapes clearance envelope: "+actor.name+" "+local);
     if(!guard.seabed.UpperHeight(world.x,world.x,world.z,world.z,out float floor))throw new Exception("Missing sand");float gap=world.y-floor;minGap=Mathf.Min(minGap,gap);vertices++;if(gap<-.0001f)throw new Exception("Animated animal penetrates sand: "+actor.name+" gap="+gap);
    }
   }
   foreach(var r in renderers)foreach(var world in Points(r)){if(!guard.seabed.UpperHeight(world.x,world.x,world.z,world.z,out float floor)||world.y<floor)throw new Exception("Animal intersects sand: "+actor.name);vertices++;minGap=Mathf.Min(minGap,world.y-floor);}
   // Test sloping terrain, yaw/pitch/bank, and a full manta touch roll.
   foreach(var offset in new[]{Vector3.zero,Vector3.forward*3,Vector3.back*3,Vector3.left*3,Vector3.right*3})for(int heading=0;heading<8;heading++){
    actor.SetPositionAndRotation(home+offset,Quaternion.Euler(heading%2==0?18:-18,heading*45,actor.GetComponent<MarineSwimmer>()?heading*45:heading%2==0?12:-12));guard.Enforce();if(!guard.RequiredHeight(actor.position,actor.rotation,out float height)||actor.position.y<height-.0001f)throw new Exception("Rotation or slope outran animal clearance: "+actor.name);
   }
   actor.SetPositionAndRotation(home,rotation);foreach(var a in actor.GetComponentsInChildren<Animation>())if(a.clip)a.clip.SampleAnimation(a.gameObject,0);
  }
  Debug.Log("ANIMAL_SAND_POSES_VALIDATED animals="+actors.Length+" vertexSamples="+vertices+" minSandGap="+minGap+" offsetAndRotationCases="+((actors.Length-1)*40));
  ValidateDetailLevels();Simulate();OctopusGroundValidation.Validate();
 }
 public static void ValidateDetailLevels(){
  EditorSceneManager.OpenScene(Scene);int variants=0;long vertices=0;
  // Android installs these transient mesh levels during its build pass.
  // Exercise the same generated meshes without saving or building a player.
  FishDistanceMesh.Install();
  foreach(var guard in All<ReefAnimalClearance>())foreach(var detail in guard.GetComponentsInChildren<ReefFishDetail>())foreach(var mesh in new[]{detail.full,detail.medium,detail.distant}.Where(m=>m).Distinct()){
   foreach(var p in mesh.vertices){if(!guard.animatedBody.Contains(guard.transform.InverseTransformPoint(detail.transform.TransformPoint(p))))throw new Exception("Fish detail level escapes clearance: "+guard.name+" mesh="+mesh.name);vertices++;}variants++;
  }
  Debug.Log("ANIMAL_SAND_DETAIL_LEVELS_VALIDATED variants="+variants+" vertices="+vertices);
 }
 public static void Simulate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var schools=All<FishSchool>();foreach(var school in schools)school.Initialize();var guards=All<ReefAnimalClearance>();var actors=Animals();
  var cruisers=All<ReefCruiser>();var rays=All<StingraySwimmer>();var marines=All<MarineSwimmer>();var sharks=All<OpenWaterShark>();var squid=All<SquidSwimmer>();var wreck=All<WreckCreature>();var followers=All<ReefFishFollower>();var encounters=All<ReefEncounter>().Where(e=>e.response!=ReefEncounter.Response.Anemone).ToArray();
  const float dt=1f/20;float maxLift=0;int checks=0;
  for(int frame=0;frame<3600;frame++){
   foreach(var school in schools)school.Simulate(dt);foreach(var c in cruisers)c.Step(dt);foreach(var r in rays)r.Step(dt,frame%600<50);foreach(var m in marines)m.Step(dt);foreach(var shark in sharks)shark.Step(dt,shark.transform.position+Vector3.right*8,Vector3.forward, true);foreach(var s in squid)s.Step(dt);foreach(var w in wreck)w.Step(dt);foreach(var f in followers)f.Step(dt,null);foreach(var e in encounters)e.Step(dt,e.transform.position-Vector3.forward,frame%500<50?.1f:10);
   foreach(var guard in guards){maxLift=Mathf.Max(maxLift,guard.Enforce());if(!guard.RequiredHeight(guard.transform.position,guard.transform.rotation,out float height)||guard.transform.position.y<height-.0001f)throw new Exception("Moving animal entered sand: "+guard.name);checks++;}
  }
  Debug.Log("ANIMAL_SAND_PATROLS_VALIDATED seconds=180 animals="+actors.Length+" poseChecks="+checks+" maxCorrection="+maxLift+" noRuntimeMeshBake=true");
 }
 public static void Capture(){
  var report=JsonUtility.FromJson<Report>(File.ReadAllText("Renders/AnimalSeabedAudit/report.json"));
  foreach(string kind in new[]{"Anglerfish","Comb jelly","Stingray","Box jelly","Turtle","Manta","Octopus"}){
   EditorSceneManager.OpenScene(Scene);var actor=Animals().Where(a=>Kind(a)==kind).OrderByDescending(a=>report.actors.Where(r=>r.kind==kind).Select(r=>Vector3.Distance(r.position,a.position)<.001f?r.lift:0).DefaultIfEmpty().Max()).First();
   foreach(var animation in actor.GetComponentsInChildren<Animation>())if(animation.clip)animation.clip.SampleAnimation(animation.gameObject,0);
   var guard=actor.GetComponent<ReefAnimalClearance>();var surface=Surfaces(actor);Bounds bounds=default;bool first=true;foreach(var r in surface)foreach(var p in Points(r))Include(ref bounds,ref first,p);
   foreach(var r in surface){
    r.gameObject.layer=31;
    if(r is SkinnedMeshRenderer s){
     // Multiple scenes rendered within the same editor frame can reuse stale
     // GPU skin buffers. Render the exact sampled vertices for these stills.
     var mesh=UnityEngine.Object.Instantiate(s.sharedMesh);mesh.ClearBlendShapes();mesh.vertices=Points(s);mesh.RecalculateBounds();mesh.RecalculateNormals();
     var proxy=new GameObject("Sampled animal clearance pose");proxy.layer=31;proxy.transform.SetParent(actor,true);proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;
    }
   }
   foreach(var floor in All<MeshFilter>().Where(f=>f.sharedMesh&&f.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0))floor.gameObject.layer=31;
   float floorY=guard&&guard.seabed.UpperHeight(actor.position.x,actor.position.x,actor.position.z,actor.position.z,out float y)?y:actor.position.y-.2f;
   var center=bounds.center;center.y=(bounds.max.y+Mathf.Min(bounds.min.y,floorY))*.5f;float size=Mathf.Max(.8f,Mathf.Max(bounds.size.magnitude*.65f,(bounds.max.y-Mathf.Min(bounds.min.y,floorY))*.65f));
   var camera=new GameObject("Animal sand inspection camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.10f,.14f);camera.nearClipPlane=.01f;camera.farClipPlane=200;camera.fieldOfView=42;var view=center+new Vector3(1,1.3f,1).normalized*size*3.5f;
   if(guard&&guard.seabed.UpperHeight(view.x,view.x,view.z,view.z,out float cameraFloor))view.y=Mathf.Max(view.y,cameraFloor+size*.8f);
   Physics.SyncTransforms();for(int i=0;i<16&&Physics.Linecast(view,bounds.center,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);i++)view.y+=size*.5f;
   camera.transform.position=view;camera.transform.LookAt(center);RenderSettings.fog=false;RenderSettings.ambientLight=Color.gray;Shader.SetGlobalFloat("_ReefLight",.8f);
   float lift=report.actors.Where(r=>r.kind==kind&&Vector3.Distance(r.position,actor.position)<.001f).Select(r=>r.lift).DefaultIfEmpty().Max();
   if(lift>.001f){actor.position-=Vector3.up*lift;ReefCorrectionAudit.Capture(camera,"Renders/AnimalSeabedAudit/"+kind.Replace(' ','_')+"_Before.png");actor.position+=Vector3.up*lift;}
   ReefCorrectionAudit.Capture(camera,"Renders/AnimalSeabedAudit/"+kind.Replace(' ','_')+"_After.png");
   UnityEngine.Object.DestroyImmediate(camera.gameObject);
  }
  Debug.Log("ANIMAL_SAND_CAPTURES_SAVED species=7");
 }
 public static void ValidateTerrainEdges(){
  var field=ScriptableObject.CreateInstance<ReefSeabedField>();field.cell=1;field.width=field.depth=2;field.heights=new[]{0f,0f,0f,ReefSeabedField.MissingHeight};
  var actor=new GameObject("Seabed edge validation");var guard=actor.AddComponent<ReefAnimalClearance>();guard.seabed=field;guard.animatedBody=new Bounds(Vector3.zero,Vector3.one*.1f);
  try{
   actor.transform.position=new Vector3(.2f,-10,.2f);guard.Enforce();var safe=actor.transform.position;
   if(safe.y<.08f||safe.y>1)throw new Exception("Failed below-sand recovery");
   if(field.UpperHeight(1.1f,1.2f,1.1f,1.2f,out _)||field.UpperHeight(-1,-.5f,0,.5f,out _))throw new Exception("Missing terrain accepted");
   if(guard.ConstrainPosition(new Vector3(1.5f,0,1.5f))!=safe)throw new Exception("Animal escaped into an unbaked sand cell");
   actor.transform.position=new Vector3(100,0,100);guard.Enforce();if(actor.transform.position!=safe)throw new Exception("Failed boundary recovery");
   Debug.Log("ANIMAL_SAND_BOUNDARIES_VALIDATED belowSandRecovery=true missingCellsBlocked=true outsideFieldBlocked=true");
  }finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(field);}
 }
 public static void FinalChecks(){ValidateTerrainEdges();ValidateDetailLevels();Capture();}
}
}
