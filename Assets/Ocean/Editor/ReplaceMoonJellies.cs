using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReplaceMoonJellies {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static SeaLife[] Jellies()=>UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);
 [MenuItem("Underwater/Replace Moon Jellies With Comb Jellies")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);var all=Jellies();var template=all.First(a=>a.kind=="Comb jelly");var moons=all.Where(a=>a.kind=="Moon jelly").ToArray();if(moons.Length!=11)throw new Exception("Expected 11 moon jellies, found "+moons.Length);
  foreach(var moon in moons){
   var position=moon.transform.position;var rotation=moon.transform.rotation;var scale=moon.transform.localScale;int sibling=moon.transform.GetSiblingIndex();
   var replacement=UnityEngine.Object.Instantiate(template.gameObject,moon.transform.parent);replacement.name="Comb jelly";replacement.transform.SetPositionAndRotation(position,rotation);replacement.transform.localScale=scale;replacement.transform.SetSiblingIndex(sibling);
   var life=replacement.GetComponent<SeaLife>();life.kind="Comb jelly";life.danger=false;life.consequence=false;life.observationOnly=true;life.stationary=true;life.grabbable=false;life.cautionVoice=null;life.reactionVoice=null;
   var info=replacement.GetComponent<ReefCreatureInfo>();if(info){info.creatureName="Comb jelly";info.fact="Moving comb rows scatter light.";}
   if(replacement.GetComponent<ReefDangerZone>())throw new Exception("Comb template must have no hazard");
   var ground=replacement.GetComponent<ReefAnimalClearance>();if(!ground)throw new Exception("Comb template has no seabed protection");ground.Enforce();
   Physics.SyncTransforms();var body=replacement.GetComponent<ReefSphereBody>();if(body&&!body.Clear(replacement.transform.position)&&!body.PlaceNear(replacement.transform.position))throw new Exception("Comb jelly placement obstructed");
   ground.Enforce();Debug.Log("MOON_TO_COMB_REPLACED from="+position+" to="+replacement.transform.position+" safeInformationOnly=true");
   UnityEngine.Object.DestroyImmediate(moon.gameObject);
  }
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();Capture();Debug.Log("MOON_TO_COMB_REPLACEMENT_PASSED replaced=11 combJellies=25 moonJellies=0 boxJellies=3");
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var all=Jellies();var combs=all.Where(a=>a.kind=="Comb jelly").ToArray();
  if(all.Any(a=>a.kind=="Moon jelly")||combs.Length!=25||all.Count(a=>a.kind=="Box jelly")!=3)throw new Exception("Jelly species counts changed incorrectly");
  foreach(var life in combs){
   if(life.danger||life.consequence||!life.observationOnly||!life.stationary||life.grabbable||life.GetComponent<ReefDangerZone>()||life.transform.Find("Prepared animated model"))throw new Exception("Old moon model or warnings retained");
   if(life.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.sharedMaterial&&ShaderUtil.ShaderHasError(r.sharedMaterial.shader)))throw new Exception("Invalid comb material");
   var encounter=life.GetComponent<DeepWaterEncounter>();var ground=life.GetComponent<ReefAnimalClearance>();var body=life.GetComponent<ReefSphereBody>();if(!encounter||encounter.species!=DeepWaterEncounter.Species.CombJelly||!ground||!body)throw new Exception("Missing comb jelly behavior or clearance");
   var start=life.transform.position;
   for(int frame=0;frame<600;frame++){encounter.Step(1f/30,5,start+Vector3.forward*.5f,0);ground.Enforce();if(!ground.RequiredHeight(life.transform.position,life.transform.rotation,out float y)||life.transform.position.y<y-.0001f||!body.Clear(life.transform.position))throw new Exception("Comb jelly entered seabed/reef");}
  }
  Debug.Log("MOON_TO_COMB_VALIDATED safeCombJellies=25 secondsPerActor=20 moonModelsAndHazardsRemoved=true seabedProtection=true");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);var actor=Jellies().Where(a=>a.kind=="Comb jelly"&&a.transform.position.z>145).First();
  foreach(var renderer in actor.GetComponentsInChildren<Renderer>())if(!(renderer is LineRenderer))renderer.gameObject.layer=31;
  foreach(var floor in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh&&f.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0))floor.gameObject.layer=31;
  var camera=new GameObject("Comb jelly replacement review").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.10f,.14f);camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.fieldOfView=42;var center=actor.transform.position+Vector3.down*.4f;camera.transform.position=center+new Vector3(2,2.5f,2);camera.transform.LookAt(center);RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",.8f);actor.GetComponent<ReefJellyMotion>()?.Initialize();Directory.CreateDirectory("Renders/CombJellyReplacement");ReefCorrectionAudit.Capture(camera,"Renders/CombJellyReplacement/CombJelly.png");
 }
 public static void Regression(){ReefDayTwo.Validate();PreparedMarineIntegration.Validate();ReefDayFive.Validate();Debug.Log("MOON_TO_COMB_REGRESSIONS_PASSED hazards=true preparedAnimals=true deepEncounters=true");}
}
}
