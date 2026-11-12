using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefCorrections {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);string palette=CoralPalette();AddFoundations();InstallRays();InstallFishColliders();CoralPrefabRepair.Apply();
  Physics.SyncTransforms();foreach(var body in UnityEngine.Object.FindObjectsByType<ReefSphereBody>(FindObjectsSortMode.None))if(!body.Clear(body.transform.position)&&!body.PlaceNear(body.transform.position))throw new Exception("Blocked animal: "+body.name);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  ReefDayTwo.Apply();if(CoralPalette()!=palette)throw new Exception("Coral palette changed during prefab conversion");Validate();CapturePreviews();
 }
 static string CoralPalette(){var counts=new SortedDictionary<string,int>();foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){bool coral=renderer.name.StartsWith("Living coral |");for(var p=renderer.transform;p;p=p.parent)if(p.name.StartsWith("Scanned Secale"))coral=true;if(!coral)continue;string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderer.sharedMaterial));counts.TryGetValue(key,out int count);counts[key]=count+1;}var rows=new List<string>();foreach(var pair in counts)rows.Add(pair.Key+":"+pair.Value);return string.Join("|",rows);}
 static void InstallRays(){
  string path=CoralPrefabRepair.Folder+"/Stingray skin.mat";System.IO.Directory.CreateDirectory(CoralPrefabRepair.Folder);AssetDatabase.Refresh();
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  int count=0;
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.kind=="Stingray"){
   if(!material){material=new Material(life.GetComponent<Renderer>().sharedMaterial){shader=Resources.Load<Shader>("StingraySkin"),enableInstancing=true};AssetDatabase.CreateAsset(material,path);}
   life.GetComponent<Renderer>().sharedMaterial=material;life.stationary=true;
   var body=life.GetComponent<ReefSphereBody>();if(!body)body=life.gameObject.AddComponent<ReefSphereBody>();body.radius=1.5f;
   Physics.SyncTransforms();var original=life.transform.position;bool clear=body.Clear(original);
   for(float offset=.3f;!clear&&offset<=3;offset+=.3f){var p=original+Vector3.right*offset;if(body.Clear(p)){life.transform.position=p;clear=true;}}
   if(!clear&&!body.PlaceNear(original))throw new Exception("No clear stingray spawn");
   var swimmer=life.GetComponent<StingraySwimmer>();if(!swimmer)swimmer=life.gameObject.AddComponent<StingraySwimmer>();swimmer.phase=count*2.1f;count++;
  }
  Debug.Log("STINGRAYS_RESTORED count="+count);
 }
 static void InstallFishColliders(){
  var animals=new HashSet<Transform>();
  foreach(var fish in UnityEngine.Object.FindObjectsByType<FishAgent>(FindObjectsSortMode.None))animals.Add(fish.transform);
  foreach(var fish in UnityEngine.Object.FindObjectsByType<FishBodyMotion>(FindObjectsSortMode.None))animals.Add(fish.transform);
  foreach(var fish in UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None))animals.Add(fish.transform);
  foreach(var fish in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None))animals.Add(fish.transform);
  foreach(var fish in UnityEngine.Object.FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None))animals.Add(fish.transform);
  foreach(var animal in animals){
   var touch=animal.GetComponent<FishTouchBody>();if(!touch)touch=animal.gameObject.AddComponent<FishTouchBody>();
   var volume=animal.Find("Fish touch volume");if(!volume){volume=new GameObject("Fish touch volume").transform;volume.SetParent(animal,false);}volume.gameObject.layer=2;
   var shape=volume.GetComponent<CapsuleCollider>();if(!shape)shape=volume.gameObject.AddComponent<CapsuleCollider>();
   bool first=true;Bounds bounds=default;
   foreach(var filter in animal.GetComponentsInChildren<MeshFilter>())foreach(var vertex in filter.sharedMesh.vertices){var p=animal.InverseTransformPoint(filter.transform.TransformPoint(vertex));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
   if(first)throw new Exception("No animal mesh for collider: "+animal.name);
   touch.bodyBounds=bounds;touch.bellySupportLocal=MeasureBelly(animal,bounds);touch.touchCollider=shape;shape.direction=2;shape.center=bounds.center;shape.radius=Mathf.Max(.02f,Mathf.Max(bounds.extents.x,bounds.extents.y));shape.height=Mathf.Max(bounds.size.z,shape.radius*2);shape.isTrigger=true;shape.enabled=false;
  }
  Debug.Log("FISH_TOUCH_COLLIDERS_INSTALLED count="+animals.Count+" triggerOnly=true distanceCulled=true");
 }
 static float MeasureBelly(Transform animal,Bounds bounds){
  var values=new List<float>();foreach(var filter in animal.GetComponentsInChildren<MeshFilter>()){
   var vertices=filter.sharedMesh.vertices;var normals=filter.sharedMesh.normals;if(normals.Length!=vertices.Length)continue;
   for(int i=0;i<vertices.Length;i++){var p=animal.InverseTransformPoint(filter.transform.TransformPoint(vertices[i]));var n=animal.InverseTransformDirection(filter.transform.TransformDirection(normals[i]));if(Mathf.Abs(p.x-bounds.center.x)<bounds.extents.x*.6f&&Mathf.Abs(p.z)<bounds.extents.z*.4f&&n.y<-.45f)values.Add(p.y);}
  }
  if(values.Count==0)return Mathf.Max(.01f,-bounds.min.y);values.Sort();return Mathf.Max(.01f,-values[Mathf.Clamp(values.Count/10,0,values.Count-1)]);
 }
 static void AddFoundations(){
  var rock=Array.Find(UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None),r=>r.name=="Porous reef outcrop");if(!rock)throw new Exception("No reef rock material");
  var meshes=new Mesh[3];for(int i=0;i<3;i++){string path=CoralPrefabRepair.Folder+"/Foundation_"+i+".asset";System.IO.Directory.CreateDirectory(CoralPrefabRepair.Folder);meshes[i]=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!meshes[i]){var g=new ReefGeometry();g.Ellipsoid(Vector3.zero,Vector3.one,12,18,.20f,i+71);meshes[i]=g.Mesh("Foundation stone "+i);AssetDatabase.CreateAsset(meshes[i],path);}}
  var existing=GameObject.Find("Grounded reef foundations");if(existing){ConnectArchFeet(existing.transform,meshes,rock.sharedMaterial);return;}
  var root=new GameObject("Grounded reef foundations").transform;
  var arch=Array.FindAll(UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None),f=>f.name=="Eroded arch voussoir");
  if(arch.Length>0){Array.Sort(arch,(a,b)=>a.transform.position.x.CompareTo(b.transform.position.x));foreach(var end in new[]{arch[0],arch[arch.Length-1]}){var bounds=end.GetComponent<Renderer>().bounds;Column(root,new Vector3(bounds.center.x,bounds.min.y+.25f,bounds.center.z),2,meshes,rock.sharedMaterial);}}
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name.StartsWith("Photogrammetry | weathered reef bed")){
   var renderers=t.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   for(int row=-1;row<=1;row++)Column(root,new Vector3(bounds.center.x,bounds.min.y+.25f,bounds.center.z+row*bounds.extents.z*.55f),Mathf.Clamp(bounds.extents.x*.65f,1.8f,4),meshes,rock.sharedMaterial);
  }
  ConnectArchFeet(root,meshes,rock.sharedMaterial);Debug.Log("REEF_FOUNDATIONS_ADDED stones="+root.childCount);
 }
 static void ConnectArchFeet(Transform root,Mesh[] meshes,Material material){
  Physics.SyncTransforms();var arch=Array.FindAll(UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None),f=>f.name=="Eroded arch voussoir");if(arch.Length==0)return;Array.Sort(arch,(a,b)=>a.transform.position.x.CompareTo(b.transform.position.x));
  foreach(var end in new[]{arch[0],arch[arch.Length-1]}){
   Vector3 foot=Vector3.up*float.MaxValue;foreach(var vertex in end.sharedMesh.vertices){var p=end.transform.TransformPoint(vertex);if(p.y<foot.y)foot=p;}
   float top=float.NegativeInfinity;var ray=new Ray(foot+Vector3.up*2,Vector3.down);foreach(var collider in root.GetComponentsInChildren<MeshCollider>())if(collider.Raycast(ray,out var hit,20))top=Mathf.Max(top,hit.point.y);
   if(top>=foot.y-.05f)continue;
   var stone=new GameObject("Reef foundation stone");stone.layer=8;stone.transform.SetParent(root,false);stone.transform.position=foot-Vector3.up*.55f;stone.transform.localScale=new Vector3(1.8f,.95f,1.8f);var mesh=meshes[root.childCount%meshes.Length];stone.AddComponent<MeshFilter>().sharedMesh=mesh;stone.AddComponent<MeshRenderer>().sharedMaterial=material;stone.AddComponent<MeshCollider>().sharedMesh=mesh;
  }
  Physics.SyncTransforms();
 }
 static void Column(Transform root,Vector3 top,float width,Mesh[] meshes,Material material){
  if(Mathf.Abs(top.x-Mathf.Sin(top.z*.115f)*2)<3.2f)return;
  float floor=ReefGeometry.Floor(top.x,top.z),gap=top.y-floor;if(gap<.3f)return;int levels=Mathf.CeilToInt(gap/1.25f);
  for(int level=0;level<levels;level++){
   var obj=new GameObject("Reef foundation stone");obj.transform.SetParent(root,false);obj.layer=8;obj.transform.position=new Vector3(top.x,floor+.75f+level*1.25f,top.z);obj.transform.rotation=Quaternion.Euler(0,(root.childCount*73)%360,0);obj.transform.localScale=new Vector3(width*(1-level*.04f),.95f,width*.85f);
   var mesh=meshes[root.childCount%meshes.Length];obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;obj.AddComponent<MeshCollider>().sharedMesh=mesh;
   if(level==0){float offset=float.PositiveInfinity;foreach(var vertex in mesh.vertices){var p=obj.transform.TransformPoint(vertex);offset=Mathf.Min(offset,p.y-ReefGeometry.Floor(p.x,p.z));}obj.transform.position-=Vector3.up*(offset+.1f);}
  }
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int rays=0;
  foreach(var ray in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None)){
   Vector3 start=ray.transform.position;var body=ray.GetComponent<ReefSphereBody>();
   for(int i=0;i<900;i++){var before=ray.transform.position;ray.Step(1f/30);if(!body.Clear(ray.transform.position)||(ray.transform.position-before).magnitude>.012f)throw new Exception("Ray movement/collision failed");}
   var block=new MaterialPropertyBlock();ray.GetComponent<Renderer>().GetPropertyBlock(block);if(!ray.GetComponent<Renderer>().sharedMaterial.HasProperty("_StrokePhase")||block.GetFloat("_StrokePhase")==0)throw new Exception("Missing ray wing animation");
   if(Vector3.Distance(start,ray.transform.position)<.5f)throw new Exception("Stingray stuck: "+ray.name);rays++;
  }
  if(rays!=3)throw new Exception("Expected all three stingrays");
  foreach(var manta in UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None))if(manta.isRay){var start=manta.transform.position;for(int i=0;i<900;i++)manta.Step(1f/30);if(Vector3.Distance(start,manta.transform.position)<2||!manta.GetComponent<ReefSphereBody>().Clear(manta.transform.position))throw new Exception("Existing manta swim regressed");}
  EditorSceneManager.OpenScene(Scene);int colliders=0,prefabs=0;
  foreach(var fish in UnityEngine.Object.FindObjectsByType<FishTouchBody>(FindObjectsSortMode.None)){if(!fish.touchCollider||!fish.touchCollider.isTrigger||fish.touchCollider.gameObject.layer!=2)throw new Exception("Invalid fish touch volume");colliders++;}
  foreach(var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))if(group.name.StartsWith("Scanned Secale")){if(!PrefabUtility.IsPartOfPrefabInstance(group.gameObject))throw new Exception("Unlinked coral prefab");prefabs++;}
  foreach(var mesh in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))if(mesh.name.StartsWith("Living coral |")){if(!PrefabUtility.IsPartOfPrefabInstance(mesh.gameObject))throw new Exception("Unlinked procedural coral prefab");prefabs++;}
  ValidateGrip();ReefDayTwo.Validate();Debug.Log($"CORRECTIONS_VALIDATED animatedStingrays={rays} fishColliders={colliders} coralPrefabs={prefabs} palmGrip=passed");
 }
 static void ValidateGrip(){
  var source=Array.Find(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None),f=>f.grabbable);var fish=UnityEngine.Object.Instantiate(source.gameObject,new Vector3(900,2,.12f),Quaternion.identity);
  var hand=new GameObject("Palm pose validation").transform;hand.position=new Vector3(900,2,0);hand.rotation=Quaternion.LookRotation(Vector3.right,Vector3.up);var life=fish.GetComponent<SeaLife>();
  try{float now=Time.time;life.Grab(hand);for(int i=0;i<40;i++)life.StepHeld(1f/72,now+i/72f);if(!life.IsHeld||Vector3.Dot(fish.transform.up,hand.up)<.98f||Vector3.Distance(fish.transform.position,hand.position)>.2f)throw new Exception("Fish palm alignment failed");life.StepHeld(1f/72,now+2.1f);if(life.IsHeld)throw new Exception("Fish failed two-second escape");}
  finally{UnityEngine.Object.DestroyImmediate(fish);UnityEngine.Object.DestroyImmediate(hand.gameObject);}
 }
 public static void CapturePreviews(){
  if(!System.IO.File.Exists("Renders/Coral_Base_Before.png")&&System.IO.File.Exists("Renders/Coral_Base_Audit.png"))System.IO.File.Copy("Renders/Coral_Base_Audit.png","Renders/Coral_Base_Before.png",true);
  ReefCorrectionAudit.Run();EditorSceneManager.OpenScene(Scene);System.IO.Directory.CreateDirectory("Renders");
  var camera=Camera.main;camera.allowHDR=false;camera.fieldOfView=70;camera.transform.position=new Vector3(0,-1,10);camera.transform.LookAt(new Vector3(0,-2.5f,23));ReefCorrectionAudit.Capture(camera,"Renders/Reef_Arch_Foundations.png");
  var rig=new GameObject("Palm preview rig").transform;rig.position=new Vector3(900,2,0);var glove=ScubaGlove.Create(rig,false);glove.transform.localRotation=Quaternion.Euler(0,0,-90);glove.Animate(.2f,.55f,1);
  if(Vector3.Dot(glove.GripAnchor.up,Vector3.up)<.99f)throw new Exception("Grip anchor disagrees with visible palm orientation");
  var source=Array.Find(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None),f=>f.grabbable);var fish=UnityEngine.Object.Instantiate(source.gameObject,glove.GripAnchor.position,Quaternion.identity);var life=fish.GetComponent<SeaLife>();life.Grab(glove.GripAnchor);for(int i=0;i<40;i++)life.StepHeld(1f/72,Time.time+i/72f);
  RenderSettings.fog=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.12f,.15f);camera.fieldOfView=45;camera.nearClipPlane=.01f;camera.transform.position=glove.GripAnchor.position+new Vector3(.25f,.3f,.25f);camera.transform.LookAt(glove.GripAnchor.position);ReefCorrectionAudit.Capture(camera,"Renders/Fish_Palm_Grip.png");
  Debug.Log("CORRECTION_PREVIEWS_RENDERED");
 }
}
}
