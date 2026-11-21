using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class RestoreEarlierMoonJellies {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static SeaLife[] Life()=>UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);
 static void CombineEarlierVisual(GameObject actor){
  var pieces=actor.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();var material=pieces[0].sharedMaterial;
  // The earlier shader pulses every piece about the actor origin. With unit
  // scale and rotation, merging translated vertices preserves that exact pulse.
  if(pieces.Any(r=>r.transform.parent!=actor.transform||r.transform.localRotation!=Quaternion.identity||r.transform.localScale!=Vector3.one||r.sharedMaterial!=material))return;
  const string folder="Assets/UnderwaterArt/RestoredMoonJellies",path=folder+"/EarlierJellyCombined.asset";
  if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/UnderwaterArt","RestoredMoonJellies");
  var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){
   mesh=new Mesh{name="Earlier jelly | original bell and all tentacles"};mesh.CombineMeshes(pieces.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(r.transform.localPosition,r.transform.localRotation,r.transform.localScale)}).ToArray(),true,true);AssetDatabase.CreateAsset(mesh,path);
  }
  var expected=pieces.SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>p+r.transform.localPosition)).ToArray();var actual=mesh.vertices;
  if(expected.Length!=actual.Length)throw new Exception("Restored jelly vertex count changed");for(int i=0;i<actual.Length;i++)if(Vector3.Distance(expected[i],actual[i])>.00001f)throw new Exception("Restored jelly shape changed while combining");
  var visual=new GameObject("Earlier moon jelly visual");visual.transform.SetParent(actor.transform,false);visual.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;foreach(var piece in pieces)piece.enabled=false;
 }
 static Bounds VisibleBounds(Transform actor){bool first=true;Bounds b=default;foreach(var r in actor.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled))foreach(var p in r.GetComponent<MeshFilter>().sharedMesh.vertices){var world=r.transform.TransformPoint(p);if(first){b=new Bounds(world,Vector3.zero);first=false;}else b.Encapsulate(world);}if(first)throw new Exception("Missing earlier jelly mesh");return b;}
 [MenuItem("Underwater/Restore Earlier Jelly Model As Moon Jellies")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);var all=Life();var combs=all.Where(a=>a.kind=="Comb jelly").OrderBy(a=>a.transform.position.z).ToArray();if(combs.Length!=25||all.Any(a=>a.kind=="Moon jelly"))throw new Exception("Expected 25 combs and no restored moons");
  var original=all.ToDictionary(a=>a,a=>EditorJsonUtility.ToJson(a.transform));var occupied=combs.Select(a=>a.transform.position).ToList();int index=0;
  foreach(var comb in combs){
   var go=UnityEngine.Object.Instantiate(comb.gameObject,comb.transform.parent);go.name="Moon jelly";var moon=go.GetComponent<SeaLife>();moon.kind="Moon jelly";moon.danger=false;moon.consequence=false;moon.grabbable=false;moon.observationOnly=true;moon.stationary=true;moon.cautionVoice=null;moon.reactionVoice=null;
   var imported=go.transform.Find("Prepared animated model");if(!imported)throw new Exception("Current comb model missing");UnityEngine.Object.DestroyImmediate(imported.gameObject);
   int surfaces=0;foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>(true))if(renderer.sharedMaterial&&renderer.sharedMaterial.shader.name=="Underwater/Translucent Jelly Tissue"){renderer.enabled=true;surfaces++;}
   if(surfaces!=11)throw new Exception("Earlier bell/tentacles incomplete: "+surfaces);
   CombineEarlierVisual(go);
   var encounter=go.GetComponent<DeepWaterEncounter>();encounter.species=DeepWaterEncounter.Species.MoonJelly;
   var info=go.GetComponent<ReefCreatureInfo>();if(info){info.creatureName="Moon jelly";info.fact="Watch its gentle pulse and trailing tentacles.";info.narration=null;}
   var guard=go.GetComponent<ReefAnimalClearance>();guard.animatedBody=AnimalSeabedRepair.Envelope(go.transform);var body=go.GetComponent<ReefSphereBody>();var home=comb.transform.position;bool found=false;float best=float.PositiveInfinity;Vector3 bestPose=home;
   foreach(float distance in new[]{2.8f,3.6f,4.4f,5.2f})for(int direction=0;direction<16;direction++){
    float angle=direction*Mathf.PI/8+index*2.399963f;var candidate=home+new Vector3(Mathf.Cos(angle)*distance,0,Mathf.Sin(angle)*distance);
    if(!guard.RequiredHeight(candidate,go.transform.rotation,out float required))continue;candidate.y=Mathf.Max(candidate.y,required);
    for(int lift=0;lift<8;lift++){
     if(occupied.All(p=>Vector3.Distance(p,candidate)>=2.1f)&&body.Clear(candidate)){float score=distance+Mathf.Abs(candidate.y-home.y)*2;if(score<best){best=score;bestPose=candidate;found=true;}break;}candidate.y+=.2f;
    }
   }
   if(!found)throw new Exception("No clear nearby moon jelly placement");go.transform.position=bestPose;guard.Enforce();go.GetComponent<ReefJellyMotion>()?.Initialize();occupied.Add(go.transform.position);index++;
   Debug.Log("EARLIER_MOON_RESTORED position="+go.transform.position+" originalComb="+home+" earlierSurfaces="+surfaces);
  }
  foreach(var entry in original)if(EditorJsonUtility.ToJson(entry.Key.transform)!=entry.Value)throw new Exception("Changed existing animal placement");
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();Capture();Debug.Log("EARLIER_MOONS_RESTORED_PASSED moonJellies=25 combJellies=25 existingAnimalsUnchanged=true");
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var all=Life();var moons=all.Where(a=>a.kind=="Moon jelly").ToArray();var combs=all.Where(a=>a.kind=="Comb jelly").ToArray();if(moons.Length!=25||combs.Length!=25||all.Count(a=>a.kind=="Box jelly")!=3)throw new Exception("Jelly counts changed incorrectly");int checks=0;
  foreach(var comb in combs)if(!comb.transform.Find("Prepared animated model")||!comb.GetComponentInChildren<Animation>()?.clip)throw new Exception("New comb model or animation lost");
  foreach(var moon in moons){
   if(moon.transform.Find("Prepared animated model")||moon.GetComponentsInChildren<SkinnedMeshRenderer>().Length!=0||moon.GetComponent<ReefDangerZone>()||moon.danger||moon.consequence||!moon.observationOnly)throw new Exception("Moon visual or behavior incorrect");
   var renderers=moon.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();if((renderers.Length!=1&&renderers.Length!=11)||renderers.Any(r=>r.sharedMaterial.shader.name!="Underwater/Translucent Jelly Tissue"||ShaderUtil.ShaderHasError(r.sharedMaterial.shader)))throw new Exception("Earlier jelly visual not restored");
   var ground=moon.GetComponent<ReefAnimalClearance>();var encounter=moon.GetComponent<DeepWaterEncounter>();if(encounter.species!=DeepWaterEncounter.Species.MoonJelly||moon.GetComponent<ReefCreatureInfo>().creatureName!="Moon jelly")throw new Exception("Moon label or drift missing");var home=moon.transform.position;
   if(combs.Any(c=>Vector3.Distance(c.transform.position,home)<2))throw new Exception("Moon overlaps a new comb at spawn");
   for(int frame=0;frame<1200;frame++){encounter.Step(1f/30,5,home+Vector3.forward*.6f,0);ground.Enforce();if(!ground.RequiredHeight(moon.transform.position,moon.transform.rotation,out float height)||moon.transform.position.y<height-.0001f)throw new Exception("Moon tentacles entered sand");checks++;}
  }
  Debug.Log("EARLIER_MOONS_VALIDATED moons=25 combs=25 secondsPerMoon=40 sandChecks="+checks+" safeDrift=true");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/RestoredMoonJellies");var all=Life();var moon=all.First(a=>a.kind=="Moon jelly"&&a.transform.position.z>140);var comb=all.Where(a=>a.kind=="Comb jelly").OrderBy(a=>Vector3.Distance(a.transform.position,moon.transform.position)).First();
  foreach(var r in moon.GetComponentsInChildren<Renderer>())r.gameObject.layer=31;moon.GetComponent<ReefJellyMotion>()?.Initialize();
  var camera=new GameObject("Moon jelly review camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.09f,.13f);camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.orthographic=true;RenderSettings.fog=false;
  var bounds=VisibleBounds(moon.transform);camera.orthographicSize=bounds.extents.magnitude*1.05f;camera.transform.position=bounds.center+new Vector3(1,.25f,1).normalized*bounds.size.magnitude*3;camera.transform.LookAt(bounds.center);ReefCorrectionAudit.Capture(camera,"Renders/RestoredMoonJellies/MoonJelly.png");
  UnityEngine.Object.DestroyImmediate(camera.gameObject);Debug.Log("EARLIER_MOON_PREVIEW_SAVED");
 }
 public static void Regression(){Validate();ReefDayTwo.Validate();PreparedMarineIntegration.Validate();Debug.Log("EARLIER_MOON_REGRESSION_PASSED warningsAndOtherPreparedAnimals=true");}
}
}
