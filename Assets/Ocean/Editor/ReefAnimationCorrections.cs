using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReefAnimationCorrections {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Folder="Assets/UnderwaterArt/AnimationCorrections";
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  RestoreFollower();CurvedLures();AttachTentacles();
  foreach(var ray in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None)){ray.speed=.32f;EditorUtility.SetDirty(ray);var mesh=ray.GetComponent<MeshFilter>().sharedMesh;mesh.RecalculateBounds();var b=mesh.bounds;b.Expand(new Vector3(.15f,1.3f,.15f));mesh.bounds=b;EditorUtility.SetDirty(mesh);}
  var squid=GameObject.Find("Giant squid distant silhouette");var old=squid.GetComponent<ReefCruiser>();if(old)UnityEngine.Object.DestroyImmediate(old);var swim=squid.GetComponent<SquidSwimmer>();if(!swim)swim=squid.AddComponent<SquidSwimmer>();Physics.SyncTransforms();swim.PlaceAboveSeabed();EditorUtility.SetDirty(swim);
  // Keep the already-expanded ocean mesh; only the original shader is restored.
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ReefDayTwo.Apply();Validate();Capture();
 }
 static void RestoreFollower(){foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.kind=="Lumen"){
  var cruise=life.GetComponent<ReefCruiser>();if(cruise)UnityEngine.Object.DestroyImmediate(cruise);
  foreach(string name in new[]{"Visible organic lure","Lumen organic lure"}){var old=life.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}life.stationary=false;EditorUtility.SetDirty(life);
 }}
 static Vector3 Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t){float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
 static void CurvedLures(){
  var points=new Vector3[33];var widths=new float[33];for(int i=0;i<33;i++){float t=i/32f;points[i]=Bezier(new Vector3(0,.17f,.12f),new Vector3(0,1.02f,.08f),new Vector3(0,1.08f,.76f),new Vector3(0,.46f,.65f),t);widths[i]=Mathf.Lerp(.017f,.006f,t);}
  var geometry=new ReefGeometry();geometry.Tube(points,widths,8,Color.white);var mesh=Save(geometry.Mesh("Curved anglerfish illicium"),"CurvedLure");
  foreach(var fish in UnityEngine.Object.FindObjectsByType<FishBodyMotion>(FindObjectsSortMode.None))if(fish.name=="Anglerfish"){
   var root=fish.transform.Find("Visible organic lure");if(!root)throw new Exception("Missing angler lure");root.Find("Lure stalk").GetComponent<MeshFilter>().sharedMesh=mesh;root.Find("Emissive lure bulb").localPosition=points[32];
  }
 }
 static void AttachTentacles(){
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.kind.Contains("jelly")){
   int index=0;foreach(var filter in life.GetComponentsInChildren<MeshFilter>())if(filter.name=="Trailing tentacle"){
    int id=index++;float a=id*Mathf.PI*2/10;var points=new Vector3[14];var widths=new float[14];for(int j=0;j<14;j++){float t=j/13f;points[j]=new Vector3(Mathf.Sin(t*7+id)*.045f*t,-t*1.2f,Mathf.Cos(t*6+id)*.045f*t);widths[j]=.004f*(1-t*.8f);}
    var geometry=new ReefGeometry();geometry.Tube(points,widths,5,Color.white);filter.sharedMesh=Save(geometry.Mesh("Attached jelly tentacle "+id),"AttachedTentacle"+id);
    filter.transform.localPosition=new Vector3(Mathf.Cos(a)*.22f,Mathf.Sqrt(1-.55f*.55f)*.24f-.012f,Mathf.Sin(a)*.22f);
   }
   var motion=life.GetComponent<ReefJellyMotion>();if(!motion)motion=life.gameObject.AddComponent<ReefJellyMotion>();motion.Initialize();
  }
 }
 static Mesh Save(Mesh mesh,string name){string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();if(!squid)throw new Exception("Missing full-body squid swimmer");float distance=0;for(int i=0;i<10800;i++){var before=squid.transform.position;squid.Step(1f/30);distance+=Vector3.ProjectOnPlane(squid.transform.position-before,Vector3.up).magnitude;if(squid.transform.position.y>4)throw new Exception("Squid rose above safe underwater depth: "+squid.transform.position);if(!squid.ClearPose(squid.transform.position,squid.transform.rotation))throw new Exception("Squid mantle/arms entered reef");}if(distance<25)throw new Exception("Squid failed to travel");Debug.Log("SQUID_FULL_BODY_VALIDATED seconds=360 horizontalDistance="+distance+" final="+squid.transform.position);
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int rays=0;foreach(var ray in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None)){float travel=0;for(int i=0;i<10800;i++){var before=ray.transform.position;ray.Step(1f/30);travel+=Vector3.Distance(before,ray.transform.position);if(!ray.GetComponent<ReefSphereBody>().Clear(ray.transform.position))throw new Exception("Ray entered reef");}if(travel<50)throw new Exception("Ray stuck");var properties=new MaterialPropertyBlock();ray.GetComponent<Renderer>().GetPropertyBlock(properties);if(ray.transform.Find("Rigged stingray"))ReefCreaturePolish.ValidateImportedRay(ray);else if(properties.GetFloat("_StrokeStrength")<.7f)throw new Exception("Ray fin strength missing");rays++;}
  EditorSceneManager.OpenScene(Scene);int jelly=0;foreach(var motion in UnityEngine.Object.FindObjectsByType<ReefJellyMotion>(FindObjectsSortMode.None)){motion.Initialize();foreach(var mesh in motion.GetComponentsInChildren<MeshFilter>())if(mesh.name=="Trailing tentacle"){
   var vertices=mesh.sharedMesh.vertices;var anchor=Vector3.zero;for(int i=0;i<5;i++)anchor+=vertices[i];anchor=mesh.transform.localPosition+anchor/5;
   float r=new Vector2(anchor.x,anchor.z).magnitude;float bellY=Mathf.Sqrt(1-r*r/.16f)*.24f;if(Mathf.Abs(anchor.y-bellY)>.02f)throw new Exception("Detached jelly anchor");
   var block=new MaterialPropertyBlock();mesh.GetComponent<Renderer>().GetPropertyBlock(block);if(Vector3.Distance(block.GetVector("_TissueOffset"),mesh.transform.localPosition)>.001f)throw new Exception("Jelly pulse origin mismatch");
  }jelly++;}
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.kind=="Lumen"&&(life.GetComponent<ReefCruiser>()||life.transform.Find("Visible organic lure")||life.transform.Find("Lumen organic lure")||life.stationary))throw new Exception("Lumen old follow behavior not restored");
  var water=GameObject.Find("Moving underside of ocean surface").GetComponent<MeshFilter>().sharedMesh.bounds;if(water.size.x<600||water.size.z<800)throw new Exception("Expanded surface lost");ReefReviewCorrections.Validate();Debug.Log("ANIMATION_CORRECTIONS_VALIDATED rays="+rays+" attachedJellies="+jelly+" follower=original surface=expanded");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders");var camera=Camera.main;camera.allowHDR=false;camera.fieldOfView=48;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();
  var ray=UnityEngine.Object.FindFirstObjectByType<StingraySwimmer>();var skin=ray.GetComponent<Renderer>();var properties=new MaterialPropertyBlock();for(int i=0;i<3;i++){properties.SetFloat("_StrokePhase",i*Mathf.PI/2);properties.SetFloat("_StrokeStrength",1);skin.SetPropertyBlock(properties);camera.transform.position=ray.transform.position+ray.transform.forward*2+Vector3.up*1.3f;camera.transform.LookAt(ray.transform.position);atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/Ray_Stroke_"+i+".png");}
  var jelly=UnityEngine.Object.FindFirstObjectByType<ReefJellyMotion>();jelly.Initialize();camera.transform.position=jelly.transform.position+new Vector3(1,-.1f,1.5f);camera.transform.LookAt(jelly.transform.position+Vector3.down*.3f);atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/Jelly_Attached.png");
  ReefReviewCorrections.Capture();
 }
}
}
