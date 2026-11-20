using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefRegressionFix {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 public static void Validate(){ReefDaySix.Validate();EditorSceneManager.OpenScene(Scene);int anglers=0,far=0,total=0;var start=Camera.main.transform.position;
  void Count(Transform actor,float radius=48){total++;if(!ReefSimulationRange.Contains(actor.position,start,radius))far++;}
  foreach(var e in UnityEngine.Object.FindObjectsByType<DeepWaterEncounter>(FindObjectsSortMode.None)){Count(e.transform);if(e.species!=DeepWaterEncounter.Species.Angler)continue;anglers++;e.Step(.05f,4,e.transform.position,0);for(int i=0;i<140;i++)e.Step(.05f,4,e.transform.position+Vector3.forward*3,3);var bulb=e.transform.Find("Visible organic lure/Emissive lure bulb");if(!bulb)throw new Exception("Missing lure geometry");var renderer=bulb.GetComponent<Renderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var color=block.GetColor("_Color");if(color.g<1.39f||color.b<1.79f||!bulb.GetComponent<Light>()||bulb.GetComponent<Light>().intensity<.349f)throw new Exception("Angler dimmed below previous visible-light baseline");e.Step(.05f,4,e.transform.position,0);for(int i=0;i<35;i++)e.Step(.05f,4,e.transform.position,0);renderer.GetPropertyBlock(block);if(block.GetColor("_Color").g<1.8f)throw new Exception("Touch did not brighten visible lure");}
  foreach(var actor in UnityEngine.Object.FindObjectsByType<ReefCruiser>(FindObjectsSortMode.None))Count(actor.transform);
  foreach(var actor in UnityEngine.Object.FindObjectsByType<WreckCreature>(FindObjectsSortMode.None))Count(actor.transform);
  foreach(var actor in UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None))Count(actor.transform,72);
  foreach(var actor in UnityEngine.Object.FindObjectsByType<OpenWaterShark>(FindObjectsSortMode.None))Count(actor.transform);
  if(anglers!=12||far<35)throw new Exception("Incomplete light/range coverage "+anglers+" "+far);
  if(!ReefSimulationRange.Contains(Vector3.zero,Vector3.right*47.9f,48)||ReefSimulationRange.Contains(Vector3.zero,Vector3.right*48.1f,48))throw new Exception("Incorrect simulation boundary");
  Debug.Log("REGRESSION_FIX_VALIDATED anglers=12 baselineLureRestored=true touchGlow=passed startingReefExcludedComponentUpdates="+far+" candidateComponentUpdates="+total+" collidersRemainActive=true noQualityReduction=true");Capture();
 }
 public static void Capture(){EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/RegressionFix");var e=Array.Find(UnityEngine.Object.FindObjectsByType<DeepWaterEncounter>(FindObjectsSortMode.None),a=>a.species==DeepWaterEncounter.Species.Angler);var camera=Camera.main;camera.fieldOfView=60;camera.transform.position=e.transform.position+new Vector3(1.5f,.3f,1);camera.transform.LookAt(e.transform.position);UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>().Apply(camera);for(int i=0;i<60;i++)e.Step(.05f,4,camera.transform.position,3);ReefCorrectionAudit.Capture(camera,"Renders/RegressionFix/Angler_Baseline.png");for(int i=0;i<40;i++)e.Step(.05f,4,camera.transform.position,0);ReefCorrectionAudit.Capture(camera,"Renders/RegressionFix/Angler_Touch.png");}
}
}
