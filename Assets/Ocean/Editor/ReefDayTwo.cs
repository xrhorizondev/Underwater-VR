using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Underwater.Editor {
public static class ReefDayTwo {
 const string ScenePath="Assets/Scenes/Underwater_Realistic.unity";
 [MenuItem("Underwater/Install surface danger warnings")]
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(ScenePath);
  var shader=Resources.Load<Shader>("ReefHazardGlow");if(!shader)throw new Exception("Missing hazard glow shader");
  const string materialPath="Assets/Ocean/Resources/ReefHazardGlow.mat";
  var glow=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(!glow){glow=new Material(shader){enableInstancing=true};AssetDatabase.CreateAsset(glow,materialPath);}
  int installed=0;
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){
   if(!life.danger)continue;
   // Re-running this focused installer replaces only its own generated proxies/UI.
   var generated=life.transform.Find("Day 2 hazard surfaces");if(generated)UnityEngine.Object.DestroyImmediate(generated.gameObject);
   var root=new GameObject("Day 2 hazard surfaces").transform;root.SetParent(life.transform,false);
   var zone=life.GetComponent<ReefDangerZone>();if(!zone)zone=life.gameObject.AddComponent<ReefDangerZone>();zone.owner=life;
   var surfaces=new List<Collider>();
   if(life.kind=="Ray tail"){
    var box=Proxy(root,life.transform,"Tail and barb surface");box.center=new Vector3(.05f,0,-.35f);box.size=new Vector3(.22f,.16f,1.65f);surfaces.Add(box);
   }else if(life.kind=="Fire coral"&&life.GetComponent<BoxCollider>())surfaces.Add(life.GetComponent<BoxCollider>());
   else foreach(var mesh in life.GetComponentsInChildren<MeshFilter>()){
    bool spine=life.kind=="Lionfish"&&mesh.name=="Venomous fin ray";
    bool jelly=life.kind.Contains("jelly")&&(mesh.name=="Pulsing bell"||mesh.name=="Trailing tentacle");
    if(!spine&&!jelly)continue;
    if(spine){
     var proxy=new GameObject("Spine surface");proxy.layer=2;proxy.transform.SetParent(root,false);proxy.transform.SetPositionAndRotation(mesh.transform.position,mesh.transform.rotation);proxy.transform.localScale=mesh.transform.lossyScale;
     // Root inherits the owner scale; compensate so this proxy matches the mesh.
     var scale=root.lossyScale;proxy.transform.localScale=new Vector3(mesh.transform.lossyScale.x/scale.x,mesh.transform.lossyScale.y/scale.y,mesh.transform.lossyScale.z/scale.z);
     var collider=proxy.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;collider.convex=true;collider.isTrigger=true;surfaces.Add(collider);
    }else{
     var box=Proxy(root,mesh.transform,mesh.name+" surface");box.center=mesh.sharedMesh.bounds.center;box.size=mesh.sharedMesh.bounds.size;surfaces.Add(box);
    }
   }
   if(surfaces.Count==0)throw new Exception("No hazard surface for "+life.kind);
   Physics.SyncTransforms();var outlines=new List<LineRenderer>();Bounds bounds=surfaces[0].bounds;
   foreach(var surface in surfaces){outlines.Add(Outline(surface,root,glow));bounds.Encapsulate(surface.bounds);}
   zone.surfaces=surfaces.ToArray();zone.outlines=outlines.ToArray();
   var indicator=new GameObject("Caution indicator");indicator.transform.SetParent(root,false);
   indicator.transform.position=bounds.center+Vector3.up*(bounds.extents.y+.12f);
   float size=Mathf.Max(.001f,Mathf.Abs(root.lossyScale.x));indicator.transform.localScale=Vector3.one/size;
   Stroke(indicator.transform,glow,new[]{Vector3.up*.065f,Vector3.up*-.015f},.014f);
   Stroke(indicator.transform,glow,new[]{Vector3.up*-.05f,Vector3.up*-.06f},.018f);
   zone.cautionIndicator=indicator.transform;indicator.SetActive(false);
   if(life.kind=="Moon jelly")life.consequence=false;
   string voice=life.kind=="Fire coral"?"DangerFireCoral":life.kind=="Lionfish"?"DangerLionfish":life.kind=="Ray tail"?"DangerRayTail":life.kind=="Moon jelly"?"DangerMoonJelly":"DangerBoxJelly";
   life.cautionVoice=Resources.Load<AudioClip>(voice);life.reactionVoice=Resources.Load<AudioClip>("DangerReaction");
   if(!life.cautionVoice||!life.reactionVoice)throw new Exception("Missing placeholder voice for "+life.kind);
   EditorUtility.SetDirty(life);EditorUtility.SetDirty(zone);installed++;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Debug.Log("DAY_TWO_INSTALLED hazards="+installed);Validate();
 }
 static BoxCollider Proxy(Transform root,Transform source,string name){
  var obj=new GameObject(name);obj.layer=2;obj.transform.SetParent(root,false);obj.transform.SetPositionAndRotation(source.position,source.rotation);
  var a=source.lossyScale;var b=root.lossyScale;obj.transform.localScale=new Vector3(a.x/b.x,a.y/b.y,a.z/b.z);
  var box=obj.AddComponent<BoxCollider>();box.isTrigger=true;return box;
 }
 static LineRenderer Outline(Collider surface,Transform root,Material material){
  var obj=new GameObject("Soft proximity outline");obj.transform.SetParent(surface.transform,false);
  // Existing solid coral colliders must not acquire generated children outside
  // the installer's own root, so put that outline in generated space instead.
  bool existing=!surface.transform.IsChildOf(root);
  if(existing){obj.transform.SetParent(root,false);obj.transform.SetPositionAndRotation(surface.transform.position,surface.transform.rotation);obj.transform.localScale=Vector3.one;}
  var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;
  line.startWidth=line.endWidth=.012f;line.startColor=line.endColor=new Color(1,.055f,.025f,.6f);
  line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
  Bounds bounds=surface is BoxCollider box?new Bounds(box.center,box.size):((MeshCollider)surface).sharedMesh.bounds;
  int[] route={0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4};var points=new Vector3[route.Length];
  for(int i=0;i<points.Length;i++){int corner=route[i];var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));points[i]=existing?obj.transform.InverseTransformPoint(surface.transform.TransformPoint(p)):p;}
  line.positionCount=points.Length;line.SetPositions(points);line.enabled=false;return line;
 }
 static void Stroke(Transform parent,Material material,Vector3[] points,float width){
  var obj=new GameObject("Warning stroke");obj.transform.SetParent(parent,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;
  line.startWidth=line.endWidth=width;line.startColor=line.endColor=new Color(1,.65f,.25f,.9f);line.positionCount=points.Length;line.SetPositions(points);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();int total=0,tails=0,moon=0;
  foreach(var zone in UnityEngine.Object.FindObjectsByType<ReefDangerZone>(FindObjectsSortMode.None)){
   Require(zone.owner&&zone.owner.danger&&zone.surfaces.Length>0&&zone.outlines.Length==zone.surfaces.Length,"Incomplete hazard");
   Require(zone.cautionIndicator&&!zone.cautionIndicator.gameObject.activeSelf,"Indicator must start hidden");
   foreach(var line in zone.outlines)Require(line&&!line.enabled,"Outline must start hidden");
   foreach(var surface in zone.surfaces){Require(surface,"Missing hazard surface");if(surface.isTrigger)Require(surface.gameObject.layer==2,"Proxy affects solid collision");}
   Require(zone.owner.cautionVoice&&zone.owner.reactionVoice,"Unassigned warning voice");
   Require(zone.owner.reactionVoice.length<1,"Reaction must be under a second");
   if(zone.owner.kind=="Ray tail"){tails++;foreach(var surface in zone.surfaces)Require(surface.transform.IsChildOf(zone.transform),"Tail hazard escaped tail transform");Require(zone.SurfaceDistance(zone.transform.parent.TransformPoint(new Vector3(1,.1f,0)))>zone.proximityDistance,"Safe ray wing triggers tail warning");Require(zone.SurfaceDistance(zone.transform.parent.position)>zone.proximityDistance,"Safe ray body triggers tail warning");}
   if(zone.owner.kind=="Moon jelly"){moon++;Require(!zone.owner.consequence,"Moon jelly must have no consequence");}
   total++;
  }
  Require(tails==3&&moon>0&&total>10,"Expected saved ray/jelly hazards");
  ValidateStages();ValidateSurfaceDistance();ReefDayOne.Validate();
  Require(Resources.Load<Shader>("ReefContactEdge"),"Missing stereo contact shader");
  Debug.Log($"DAY_TWO_VALIDATED hazards={total} tails={tails} moonJellies={moon} independentStages=passed surfaceDistance=passed warningLead=passed contactLatch=passed");
 }
 static void ValidateStages(){
  var test=new GameObject("Danger state validation");
  try{
   var life=test.AddComponent<SeaLife>();life.danger=true;var zone=test.AddComponent<ReefDangerZone>();zone.owner=life;
   Require(!zone.Step(.21f,0,true).glow,"Early glow");Require(zone.Step(.19f,0,true).glow,"Missing proximity glow");
   var caution=zone.Step(.09f,1,true);Require(caution.caution&&caution.warn&&!caution.contact,"Caution ordering");zone.MarkWarningSpoken(1);
   Require(!zone.Step(0,1.05f,true).contact,"Consequence preceded warning lead");Require(zone.Step(0,1.2f,true).contact,"Missing contact");Require(!zone.Step(0,2,true).contact,"Repeated contact while held");
   zone.Step(.3f,3,true);Require(zone.Step(.09f,4,true).warn,"Warning failed to rearm");
   zone.ResetEpisode();life.proximityEnabled=false;Require(!zone.Step(.09f,5,true).glow&&zone.Step(.09f,5,true).warn,"Proximity toggle disabled caution");
   life.cautionEnabled=false;Require(zone.Step(0,5,true).contact,"Caution toggle disabled independent contact");
   zone.ResetEpisode();life.consequence=false;Require(!zone.Step(0,6,true).contact,"Disabled local consequence fired");
   life.consequence=true;Require(!zone.Step(0,6,false).contact,"Disabled global consequence fired");
  }finally{UnityEngine.Object.DestroyImmediate(test);}
 }
 static void ValidateSurfaceDistance(){
  var test=new GameObject("Scaled surface validation");
  try{
   test.transform.position=new Vector3(900,0,0);test.transform.localScale=new Vector3(2,1,1);var box=test.AddComponent<BoxCollider>();box.size=Vector3.one;
   var zone=test.AddComponent<ReefDangerZone>();zone.surfaces=new Collider[]{box};Physics.SyncTransforms();
   Require(Mathf.Abs(zone.SurfaceDistance(new Vector3(901.24f,0,0))-.20f)<.001f,"World surface distance ignored scale/palm radius");
   Require(zone.SurfaceDistance(new Vector3(900,0,0))==0,"Inside collider is not contact");
  }finally{UnityEngine.Object.DestroyImmediate(test);}
 }
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
}
}
