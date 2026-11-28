using System;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ContactFitValidation {
 public static void Run(){ValidateHands();DiverRevision.Validate();}
 public static void ValidateHands(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var sources=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(f=>f.grabbable).GroupBy(f=>f.kind).Select(g=>g.First()).ToArray();int cases=0;
  foreach(bool left in new[]{true,false})foreach(var source in sources)foreach(var side in new[]{Vector3.left,Vector3.right,Vector3.up,Vector3.down}){
   var rig=new GameObject("Contact-fit test");rig.transform.position=new Vector3(900,2,0);var glove=ScubaGlove.Create(rig.transform,left);var other=ScubaGlove.Create(rig.transform,!left);
   var fish=UnityEngine.Object.Instantiate(source.gameObject,rig.transform.position,Quaternion.Euler(0,35,0)).GetComponent<SeaLife>();var clearance=fish.GetComponent<ReefAnimalClearance>();if(clearance)UnityEngine.Object.DestroyImmediate(clearance);var body=fish.GetComponent<FishTouchBody>();body.touchCollider.enabled=true;Physics.SyncTransforms();
   try{
    var normal=fish.transform.TransformDirection(side);glove.transform.rotation=Quaternion.FromToRotation(glove.GripAnchor.up,normal)*glove.transform.rotation;
    var surface=body.PalmSupport(normal);glove.transform.position+=surface-glove.GripAnchor.position;
    var wrist=glove.transform.position;var fit=glove.Wrist.localPosition;fish.Grab(glove.GripAnchor);float now=Time.time;
    for(int i=0;i<32;i++){fish.StepHeld(1f/72,now+i/72f);glove.Animate(0,1,1f/72,fish);glove.ResolveContact();}
    if(!fish.IsHeld||glove.PenetratingVertices(body)!=0)throw new Exception("Holding fingers intersect body: "+source.kind+" left="+left+" side="+side);
    if(Vector3.Distance(glove.transform.position,wrist)>.00001f)throw new Exception("Finger fitting moved tracked hand root");
    if(Vector3.Distance(glove.Wrist.localPosition,fit)>.00001f)throw new Exception("Contact fitting changed controller alignment");
    var tracked=other.transform.position;other.transform.position+=fish.transform.TransformPoint(body.GripCenter)-other.GripAnchor.position;tracked=other.transform.position;
    other.SetContactFish(fish);other.Animate(0,0,1f/72);other.ResolveContact();
    if(other.Wrist.localPosition.sqrMagnitude>.0000001f)throw new Exception("Free-hand contact displaced the wrist from the controller");
    if(other.PenetratingVertices(body)!=0||Vector3.Distance(other.transform.position,tracked)>.00001f)throw new Exception("Free-hand blocking penetrates fish or moves tracked root");
    for(int i=0;i<18;i++){glove.transform.rotation=Quaternion.AngleAxis(1,Vector3.up)*glove.transform.rotation;glove.transform.position+=Vector3.forward*.001f;fish.StepHeld(1f/72,now+(32+i)/72f);glove.Animate(0,1,1f/72,fish);glove.ResolveContact();if(glove.PenetratingVertices(body)!=0)throw new Exception("Moving contact penetrated body");}
    fish.Release();glove.Animate(0,0,1);glove.ResolveContact();other.SetContactFish(null);other.ResolveContact();
    var rendered=glove.GetComponentInChildren<SkinnedMeshRenderer>();var releasedMesh=new Mesh();rendered.BakeMesh(releasedMesh);foreach(var v in releasedMesh.vertices)if(!rendered.localBounds.Contains(v))throw new Exception("Released hand leaves culling bounds");UnityEngine.Object.DestroyImmediate(releasedMesh);cases++;
   }finally{UnityEngine.Object.DestroyImmediate(fish.gameObject);UnityEngine.Object.DestroyImmediate(rig);}
  }
  Debug.Log("CONTACT_FIT_VALIDATED cases="+cases+" renderedHandVerticesOutsideBody=true movingContacts=true freeHandBlocked=true trackedRootsUnchanged=true");
  HandContactRevision.Validate();Capture();QuestReefBuild.GrabCheck();QuestReefBuild.GloveTravelCheck();
 }
 public static void Capture(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");System.IO.Directory.CreateDirectory("Renders/ContactFit");var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();var camera=Camera.main;
  var glove=ScubaGlove.Create(diver.transform,false);glove.transform.localPosition=new Vector3(.2f,1.3f,.5f);glove.transform.localRotation=Quaternion.Euler(-90,0,0);
  var all=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);var source=Array.Find(all,f=>f.grabbable&&f.kind=="Blue tang");
  var fish=UnityEngine.Object.Instantiate(source.gameObject,glove.GripAnchor.position+glove.GripAnchor.up*.1f,Quaternion.identity).GetComponent<SeaLife>();var clearance=fish.GetComponent<ReefAnimalClearance>();if(clearance)UnityEngine.Object.DestroyImmediate(clearance);var body=fish.GetComponent<FishTouchBody>();body.touchCollider.enabled=true;Physics.SyncTransforms();fish.Grab(glove.GripAnchor);
  for(int i=0;i<32;i++){fish.StepHeld(1f/72,Time.time+i/72f);glove.Animate(0,1,1f/72,fish);glove.ResolveContact();}
  camera.transform.position=glove.GripAnchor.position+new Vector3(.28f,.2f,.32f);camera.transform.LookAt(fish.transform.TransformPoint(body.GripCenter));camera.fieldOfView=48;camera.nearClipPlane=.01f;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/ContactFit/BodyGrip.png");
 }
}
}
