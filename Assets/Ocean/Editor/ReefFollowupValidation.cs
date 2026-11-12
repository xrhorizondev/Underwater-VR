using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefFollowupValidation {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 public static void Apply(){ReefCorrections.Apply();Run();}
 public static void Run(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int rays=0;float worstStall=0;
  foreach(var manta in UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None))if(manta.isRay){worstStall=Mathf.Max(worstStall,Simulate(manta.transform,manta.GetComponent<ReefSphereBody>(),manta.Step,.42f));rays++;}
  foreach(var ray in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None)){worstStall=Mathf.Max(worstStall,Simulate(ray.transform,ray.GetComponent<ReefSphereBody>(),ray.Step,ray.speed));rays++;}
  if(rays!=4)throw new Exception("Expected surface manta and all three stingrays");
  EditorSceneManager.OpenScene(Scene);ValidateTouch();ValidatePalmGap();ReefCorrections.Validate();
  Debug.Log($"FOLLOWUP_VALIDATED rays={rays} simulatedSecondsEach=360 maxStationarySeconds={worstStall:F2} touchNudge=passed");
 }
 static float Simulate(Transform actor,ReefSphereBody body,Action<float> step,float speed){
  float stalled=0,worst=0,travel=0;const float dt=1f/30;
  for(int frame=0;frame<10800;frame++){
   var before=actor.position;step(dt);float distance=Vector3.Distance(before,actor.position);travel+=distance;
   if(!body.Clear(actor.position))throw new Exception("Ray penetrated reef: "+actor.name);
   if(distance>speed*dt*1.05f)throw new Exception("Ray jumped: "+actor.name);
   stalled=distance<speed*dt*.08f?stalled+dt:0;worst=Mathf.Max(worst,stalled);
   if(stalled>4)throw new Exception("Ray remained stuck for four seconds: "+actor.name+" at "+actor.position);
  }
  if(travel<speed*300)throw new Exception("Ray travelled too little: "+actor.name);
  Debug.Log($"RAY_LONG_CHECK name={actor.name} metres={travel:F1} maxStationarySeconds={worst:F2}");return worst;
 }
 static void ValidateTouch(){
  var source=Array.Find(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None),x=>x.grabbable);
  var fish=UnityEngine.Object.Instantiate(source.gameObject,new Vector3(900,2,0),Quaternion.identity);
  try{
   var touch=fish.GetComponent<FishTouchBody>();var body=fish.GetComponent<ReefSphereBody>();var start=fish.transform.position;touch.Touch(start-Vector3.right*.05f,0);
   for(int frame=0;frame<72;frame++)touch.StepContact(1f/72);
   float moved=Vector3.Distance(start,fish.transform.position);if(moved<.04f||moved>.13f||!body.Clear(fish.transform.position))throw new Exception("Fish touch response failed: "+moved);
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=new Vector3(900.5f,2,0);wall.transform.localScale=new Vector3(.1f,2,2);Physics.SyncTransforms();
   try{touch.Touch(fish.transform.position-Vector3.right*.05f,1);for(int frame=0;frame<72;frame++)touch.StepContact(1f/72);if(!body.Clear(fish.transform.position))throw new Exception("Touch pushed fish through reef");}
   finally{UnityEngine.Object.DestroyImmediate(wall);}
  }finally{UnityEngine.Object.DestroyImmediate(fish);}
 }
 static void ValidatePalmGap(){
  var source=Array.Find(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None),x=>x.grabbable);var rig=new GameObject("Palm support test").transform;rig.position=new Vector3(900,2,0);
  try{foreach(bool left in new[]{false,true}){
   var glove=ScubaGlove.Create(rig,left);glove.transform.localRotation=Quaternion.Euler(0,0,left?90:-90);glove.Animate(.2f,.55f,1);
   var fish=UnityEngine.Object.Instantiate(source.gameObject,glove.GripAnchor.position,glove.GripAnchor.rotation);var life=fish.GetComponent<SeaLife>();var touch=fish.GetComponent<FishTouchBody>();float now=Time.time;
   try{life.Grab(glove.GripAnchor);for(int i=0;i<40;i++)life.StepHeld(1f/72,now+i/72f);float gap=Vector3.Dot(fish.transform.position-glove.GripAnchor.position,glove.GripAnchor.up)-touch.bellySupportLocal*Mathf.Abs(fish.transform.lossyScale.y);if(Mathf.Abs(gap)>.006f)throw new Exception("Fish belly floats above palm: "+gap);Debug.Log("PALM_SUPPORT_CHECK left="+left+" gapMetres="+gap.ToString("F4"));}
   finally{UnityEngine.Object.DestroyImmediate(fish);UnityEngine.Object.DestroyImmediate(glove.gameObject);}
  }}finally{UnityEngine.Object.DestroyImmediate(rig.gameObject);}
 }
}
}
