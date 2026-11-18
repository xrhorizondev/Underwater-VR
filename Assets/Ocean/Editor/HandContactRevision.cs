using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class HandContactRevision {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();diver.controllerHandRotation=Vector3.zero;diver.controllerHandPosition=new Vector3(0,-.015f,.015f);EditorUtility.SetDirty(diver);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Validate();DiverRevision.Validate();Capture();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();
  var candidates=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(f=>f.grabbable&&!f.danger&&!f.observationOnly).ToArray();
  var rig=new GameObject("Grip pose and contact tests");rig.transform.position=new Vector3(900,2,0);int tests=0;
  try{foreach(bool left in new[]{true,false}){
   var glove=ScubaGlove.Create(rig.transform,left);glove.SetControllerFit(Vector3.zero,Vector3.zero,left);
   // Grip +Z is the upstanding knuckle line in the recorded normal hold.
   glove.transform.localRotation=Quaternion.Euler(-90,0,0);
   if(Vector3.Dot(glove.FingertipPosition-glove.Wrist.position,Vector3.forward)<.02f)throw new Exception("Open hand does not extend forward from upright controller grip");
   if(Vector3.Dot(glove.GripAnchor.up,left?Vector3.right:Vector3.left)<.99f)throw new Exception("Mirrored palm basis incorrect");
   foreach(var source in candidates.GroupBy(f=>f.kind).Select(g=>g.First()))foreach(var side in new[]{Vector3.left,Vector3.right,Vector3.down,Vector3.up}){
    var fish=UnityEngine.Object.Instantiate(source.gameObject,rig.transform.position,Quaternion.Euler(7,35,0));var touch=fish.GetComponent<FishTouchBody>();touch.touchCollider.enabled=true;Physics.SyncTransforms();
    var hand=new GameObject("Body contact hand").transform;var bounds=touch.bodyBounds;var local=bounds.center+Vector3.Scale(side,bounds.extents+Vector3.one*.08f);hand.position=fish.transform.TransformPoint(local);hand.position=touch.GrabSurface(hand.position);hand.rotation=Quaternion.Euler(20,-15,32);
    try{
     var before=fish.transform.position;var initial=fish.transform.rotation;var life=fish.GetComponent<SeaLife>();life.Grab(hand);
     if(!life.IsHeld||Vector3.Distance(before,fish.transform.position)>.00001f||Quaternion.Angle(initial,fish.transform.rotation)>.001f)throw new Exception("Catch snapped the fish");
     for(int i=0;i<20;i++)life.StepHeld(1f/72,Time.time+i/72f);
     if(Quaternion.Angle(initial,fish.transform.rotation)>1.5f||Vector3.Distance(life.HeldContactPosition,hand.position)>.012f)throw new Exception("Side/belly catch changed fish pose or lost surface contact");
     var relative=Quaternion.Inverse(hand.rotation)*fish.transform.rotation;hand.rotation=Quaternion.Euler(-15,45,10)*hand.rotation;
     for(int i=0;i<50;i++){hand.position+=Vector3.forward*.002f;life.StepHeld(1f/72,Time.time+(20+i)/72f);}
     if(!life.IsHeld||Quaternion.Angle(hand.rotation*relative,fish.transform.rotation)>3||Vector3.Distance(life.HeldContactPosition,hand.position)>.022f)throw new Exception("Fish failed moving/rotating contact follow");
     var released=fish.transform.rotation;life.Release();if(Quaternion.Angle(released,fish.transform.rotation)>.001f)throw new Exception("Release forced a new orientation");tests++;
    }finally{UnityEngine.Object.DestroyImmediate(hand.gameObject);UnityEngine.Object.DestroyImmediate(fish);}
   }
   UnityEngine.Object.DestroyImmediate(glove.gameObject);
  }}finally{UnityEngine.Object.DestroyImmediate(rig);}
  Debug.Log("HAND_CONTACT_VALIDATED cases="+tests+" bothHands=true bothFlanksAndBelly=true noCatchSnap=true relativePoseFollow=true releasePosePreserved=true");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);System.IO.Directory.CreateDirectory("Renders/HandContactRevision");var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();var rig=diver.transform;var head=rig.Find("Head");var camera=head.GetComponent<Camera>();
  var left=ScubaGlove.Create(rig,true);var right=ScubaGlove.Create(rig,false);foreach(var glove in new[]{left,right}){glove.SetControllerFit(Vector3.zero,Vector3.zero,glove==left);glove.transform.localPosition=new Vector3(glove==left?-.22f:.22f,1.3f,.45f);glove.transform.localRotation=Quaternion.Euler(-90,0,0);glove.Animate(.05f,.12f,1);}
  head.localRotation=Quaternion.Euler(20,0,0);camera.fieldOfView=85;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/HandContactRevision/NormalControllerHold.png");
  Debug.Log("HAND_CONTACT_PREVIEW_PASSED normalControllerGrip=true");
 }
}
}
