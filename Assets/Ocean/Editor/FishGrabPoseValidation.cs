using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class FishGrabPoseValidation {
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  var rig=new GameObject("Fish holding pose validation");rig.transform.position=new Vector3(900,2,0);
  try{foreach(bool left in new[]{true,false}){
   var glove=ScubaGlove.Create(rig.transform,left);var skin=glove.GetComponentInChildren<SkinnedMeshRenderer>();
   var fish=new GameObject("Pose size fixture");var life=fish.AddComponent<SeaLife>();life.grabbable=true;
   var touch=fish.AddComponent<FishTouchBody>();touch.bodyBounds=new Bounds(Vector3.zero,new Vector3(.05f,.05f,.3f));
   var shape=fish.AddComponent<CapsuleCollider>();shape.isTrigger=true;shape.direction=2;shape.radius=.025f;shape.height=.3f;touch.touchCollider=shape;
   fish.transform.position=glove.GripAnchor.position;
   try{
    var anchor=glove.GripAnchor.position;var rotation=glove.Wrist.rotation;var open=new Mesh();skin.BakeMesh(open);life.Grab(glove.GripAnchor);
    glove.Animate(0,1,1f/72,life);if(glove.HoldingPoseWeight<=0||glove.HoldingPoseWeight>.12f)throw new Exception("Pose did not blend gradually");
    for(int i=0;i<24;i++)glove.Animate(0,1,1f/72,life);
    float small=glove.HoldingPoseWeight;if(small<.99f||skin.GetBlendShapeWeight(0)>.01f||skin.GetBlendShapeWeight(1)>.01f)throw new Exception("Normal input overrides holding pose");
    var held=new Mesh();skin.BakeMesh(held);float deformation=0;var av=open.vertices;var bv=held.vertices;for(int i=0;i<av.Length;i++)deformation=Mathf.Max(deformation,Vector3.Distance(av[i],bv[i]));
    if(deformation<.015f||Vector3.Distance(glove.GripAnchor.position,anchor)>.00001f||Quaternion.Angle(glove.Wrist.rotation,rotation)>.001f)throw new Exception("Hold lacks finger curl or moves wrist/contact anchor");
    life.Release();for(int i=0;i<24;i++)glove.Animate(0,0,1f/72);if(glove.HoldingPoseWeight>.001f)throw new Exception("Pose failed to release");
    touch.bodyBounds=new Bounds(Vector3.zero,new Vector3(.3f,.3f,.5f));life.Grab(glove.GripAnchor);for(int i=0;i<24;i++)glove.Animate(1,1,1f/72,life);if(glove.HoldingPoseWeight>=small-.2f)throw new Exception("Larger fish does not produce looser grip");
    life.StepHeld(1f/72,Time.time+2.1f);if(life.IsHeld)throw new Exception("Escape timer lost");for(int i=0;i<24;i++)glove.Animate(0,0,1f/72,life);if(glove.HoldingPoseWeight>.001f)throw new Exception("Escaped fish leaves hand in holding pose");
    UnityEngine.Object.DestroyImmediate(open);UnityEngine.Object.DestroyImmediate(held);
    Debug.Log("FISH_GRAB_POSE_VALIDATED left="+left+" deformation="+deformation+" sizeAdaptive=true smoothBlend=true anchorStable=true releaseAndEscape=true");
   }finally{UnityEngine.Object.DestroyImmediate(fish);UnityEngine.Object.DestroyImmediate(glove.gameObject);}
  }}finally{UnityEngine.Object.DestroyImmediate(rig);}
  QuestReefBuild.GloveTravelCheck();HandContactRevision.Validate();DiverRevision.Validate();
  Capture();Debug.Log("FISH_GRAB_POSE_REGRESSIONS_PASSED");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");System.IO.Directory.CreateDirectory("Renders/FishGrabPose");
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();var head=diver.transform.Find("Head");var camera=head.GetComponent<Camera>();
  var glove=ScubaGlove.Create(diver.transform,false);glove.transform.localPosition=new Vector3(.2f,1.3f,.5f);glove.transform.localRotation=Quaternion.Euler(-90,0,0);
  var candidates=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);var source=Array.Find(candidates,f=>f.grabbable&&f.kind=="Clownfish");
  var fish=UnityEngine.Object.Instantiate(source.gameObject,glove.GripAnchor.position+Vector3.up*.025f,Quaternion.identity).GetComponent<SeaLife>();fish.GetComponent<FishTouchBody>().touchCollider.enabled=true;Physics.SyncTransforms();fish.Grab(glove.GripAnchor);for(int i=0;i<24;i++){glove.Animate(0,1,1f/72,fish);fish.StepHeld(1f/72,Time.time+i/72f);}
  camera.transform.position=glove.GripAnchor.position+new Vector3(.25f,.18f,.3f);camera.transform.LookAt(glove.GripAnchor.position);camera.fieldOfView=45;camera.nearClipPlane=.01f;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/FishGrabPose/HoldingFish.png");
 }
}
}
