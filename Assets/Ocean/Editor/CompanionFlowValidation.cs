using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class CompanionFlowValidation {
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  foreach(float dt in new[]{1f/30,1f/72}){
   var viewer=new Vector3(1000,-20,1000);var fish=new GameObject("Companion flow test");fish.transform.position=viewer-Vector3.forward*2.2f;
   var body=fish.AddComponent<ReefSphereBody>();body.radius=.2f;var follower=fish.AddComponent<ReefFishFollower>();
   float maxTurn=0,maxStep=0,minDistance=100;Vector3 previousVelocity=Vector3.zero;
   void Tick(Vector3 forward){var before=fish.transform.position;var heading=fish.transform.forward;follower.Step(dt,viewer,forward);var delta=fish.transform.position-before;var velocity=delta/dt;maxStep=Mathf.Max(maxStep,delta.magnitude);maxTurn=Mathf.Max(maxTurn,Vector3.Angle(heading,fish.transform.forward)/dt);minDistance=Mathf.Min(minDistance,Vector3.Distance(viewer,fish.transform.position));if(delta.magnitude>1.501f*dt||Vector3.Angle(heading,fish.transform.forward)>40.2f*dt)throw new Exception("Companion snapped");if(!body.Clear(fish.transform.position))throw new Exception("Companion reef overlap");previousVelocity=velocity;}
   try{
    for(float t=0;t<18;t+=dt){viewer+=Vector3.forward*.65f*dt;Tick(Vector3.forward);}
    if(Vector3.Dot(fish.transform.position-viewer,Vector3.forward)>-1)throw new Exception("Companion not trailing while swimming");
    for(float t=0;t<30;t+=dt)Tick(Vector3.forward);
    if(Vector3.Dot(fish.transform.position-viewer,Vector3.forward)<1.3f)throw new Exception("Companion failed to come in front after stopping: "+(fish.transform.position-viewer));
    if(Vector3.Dot(fish.transform.forward,(viewer-fish.transform.position).normalized)<.95f)throw new Exception("Resting companion not facing camera");
    for(float t=0;t<20;t+=dt)Tick(Vector3.right);
    if(Vector3.Dot(fish.transform.position-viewer,Vector3.right)<1.3f)throw new Exception("Companion did not smoothly follow resting camera direction");
    for(float t=0;t<18;t+=dt){viewer+=Vector3.right*.65f*dt;Tick(Vector3.right);}
    if(Vector3.Dot(fish.transform.position-viewer,Vector3.right)>-1)throw new Exception("Companion did not resume trailing");
    viewer+=Vector3.forward*64;Tick(Vector3.back);
    if(minDistance<1.0f)throw new Exception("Companion crossed through viewer: "+minDistance);
    Debug.Log("COMPANION_FLOW_VALIDATED hz="+1/dt+" trailThenFrontThenFaceThenTrail=true maxTurn="+maxTurn+" maxStep="+maxStep+" minViewerDistance="+minDistance+" teleportNoSnap=true");
   }finally{UnityEngine.Object.DestroyImmediate(fish);}
  }
  ReefCreaturePolish.Validate();
 }
}
}
