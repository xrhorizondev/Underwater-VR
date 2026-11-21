using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefDayFive {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static DeepWaterEncounter Encounter(GameObject go,DeepWaterEncounter.Species species){var e=go.GetComponent<DeepWaterEncounter>();if(!e)e=go.AddComponent<DeepWaterEncounter>();e.species=species;return e;}
 static void Info(GameObject go,string name,string fact,float range=3.2f){var info=go.GetComponent<ReefCreatureInfo>();if(!info)info=go.AddComponent<ReefCreatureInfo>();info.creatureName=name;info.fact=fact;info.observationRange=range;}
 public static void Run(){Apply();Validate();Capture();}
 public static void Apply(){var scene=EditorSceneManager.OpenScene(Scene);
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){
   if(life.kind=="Lumen"){Encounter(life.gameObject,DeepWaterEncounter.Species.Lumen);life.danger=false;life.grabbable=false;Info(life.gameObject,"Lumen","A quiet companion through deeper water.");}
   if(life.kind=="Comb jelly"){life.observationOnly=true;life.danger=false;life.consequence=false;life.stationary=true;life.grabbable=false;if(!life.GetComponent<ReefSphereBody>())life.gameObject.AddComponent<ReefSphereBody>().radius=.45f;Encounter(life.gameObject,DeepWaterEncounter.Species.CombJelly);Info(life.gameObject,"Comb jelly","Moving comb rows scatter light.");}
  }
  foreach(var fish in UnityEngine.Object.FindObjectsByType<FishBodyMotion>(FindObjectsSortMode.None))if(fish.name=="Anglerfish")Encounter(fish.gameObject,DeepWaterEncounter.Species.Angler);
  var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();if(!squid)throw new Exception("Missing squid");Info(squid.gameObject,"Giant squid","Watch its slow passage through the darkness.",32);var squidLife=squid.GetComponent<SeaLife>();if(squidLife){squidLife.observationOnly=true;squidLife.grabbable=false;squidLife.danger=false;}
  Physics.SyncTransforms();foreach(var e in UnityEngine.Object.FindObjectsByType<DeepWaterEncounter>(FindObjectsSortMode.None)){var body=e.GetComponent<ReefSphereBody>();if(body&&!body.Clear(e.transform.position)&&!body.PlaceNear(e.transform.position))throw new Exception("Blocked deep encounter "+e.name);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
 public static void Validate(){EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int lumen=0,anglers=0,jellies=0;DeepWaterEncounter firstAngler=null;
  foreach(var e in UnityEngine.Object.FindObjectsByType<DeepWaterEncounter>(FindObjectsSortMode.None)){
   var p=e.transform.position;var life=e.GetComponent<SeaLife>();if(e.species==DeepWaterEncounter.Species.Lumen){lumen++;if(!e.GetComponent<ReefFishFollower>()||e.GetComponent<ReefCruiser>()||e.GetComponentInChildren<Light>())throw new Exception("Lumen movement/lure changed");for(int i=0;i<120;i++)e.Step(1f/30,2,p+Vector3.forward*3,5);float low=e.Brightness;for(int i=0;i<120;i++)e.Step(1f/30,3,p+Vector3.forward*3,5);if(e.Brightness<low+.5f)throw new Exception("Lumen reveal failed");}
   if(e.species==DeepWaterEncounter.Species.Angler){anglers++;if(!e.GetComponent<ReefCruiser>())throw new Exception("Angler lost swimming");e.Step(.05f,4,p+Vector3.forward,0);for(int i=0;i<25;i++)e.Step(.05f,4,p+Vector3.forward,0);if(e.Brightness<.8f)throw new Exception("Angler touch failed");for(int i=0;i<140;i++)e.Step(.05f,4,p+Vector3.forward,2);if(e.Brightness>.08f)throw new Exception("Angler failed to dim");if(firstAngler&&firstAngler.Brightness>.08f)throw new Exception("Angler response leaked");firstAngler=e;}
   if(e.species==DeepWaterEncounter.Species.CombJelly){jellies++;if(!life||life.danger||life.consequence||!life.observationOnly||!life.stationary)throw new Exception("Comb jelly is not information-only");for(int i=0;i<180;i++){e.Step(1f/30,3,p+Vector3.forward*.5f,0);if(!e.GetComponent<ReefSphereBody>().Clear(e.transform.position))throw new Exception("Comb jelly entered reef");}if(Vector3.Distance(p,e.transform.position)<.1f)throw new Exception("Comb avoidance stalled");}
  }if(lumen!=1||anglers!=12||jellies!=25)throw new Exception("Incomplete deep encounters "+lumen+"/"+anglers+"/"+jellies);
  var camera=Camera.main;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();float last=0;for(int depth=8;depth<=48;depth++){camera.transform.position=new Vector3(0,-depth,100);atmosphere.Apply(camera);if(RenderSettings.fogDensity<last||RenderSettings.fogColor.maxColorComponent<.02f)throw new Exception("Depth transition failed");last=RenderSettings.fogDensity;}
  Debug.Log("DAY_FIVE_VALIDATED lumenReveal=passed noLure=true anglers=12 individualTouchRecovery=passed combJellies=25 safeAvoidance=passed continuousDepthFog=passed");
  ReefRealismPass.Validate();ReefCreaturePolish.Validate();
 }
 public static void Capture(){EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/DayFive");var camera=Camera.main;camera.fieldOfView=65;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();
  var encounters=UnityEngine.Object.FindObjectsByType<DeepWaterEncounter>(FindObjectsSortMode.None);foreach(var species in new[]{DeepWaterEncounter.Species.Lumen,DeepWaterEncounter.Species.Angler}){var e=Array.Find(encounters,a=>a.species==species);bool lumen=species==DeepWaterEncounter.Species.Lumen;for(int i=0;i<45;i++)e.Step(1f/30,lumen?3:4,e.transform.position+Vector3.forward,0);camera.transform.position=e.transform.position+new Vector3(lumen?2:1,.25f,1);camera.transform.LookAt(e.transform.position);atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/DayFive/"+(lumen?"Lumen":"Angler")+".png");}
  foreach(int zone in new[]{3,4}){camera.transform.position=OceanWorld.Centers[zone]+new Vector3(0,1,-5);camera.transform.LookAt(OceanWorld.Centers[zone]+Vector3.forward*8);atmosphere.Apply(camera);foreach(var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)){ps.Simulate(12);ps.Pause();}ReefCorrectionAudit.Capture(camera,"Renders/DayFive/Zone_"+(zone+1)+".png");}
 }
}
}
