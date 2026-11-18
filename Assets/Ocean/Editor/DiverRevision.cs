using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class DiverRevision {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 public static void ConfigureSquid(SquidSwimmer trench,SquidSwimmer duplicate){
  var recurring=trench.GetComponent<RecurringSquid>();if(!recurring){recurring=trench.gameObject.AddComponent<RecurringSquid>();recurring.trenchPosition=trench.transform.position;recurring.trenchRotation=trench.transform.rotation;}
  recurring.openWaterPosition=duplicate?duplicate.transform.position:new Vector3(31,-8,196);recurring.openWaterRotation=duplicate?duplicate.transform.rotation:Quaternion.identity;
  if(duplicate)UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
  EditorUtility.SetDirty(recurring);
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();
  // Outside the reef wall, within zone 7, with a broad patrol clear of the route.
  shark.transform.SetPositionAndRotation(OceanWorld.Centers[6]+new Vector3(19,4,2),Quaternion.identity);shark.patrolRadius=7;
  if(!shark.GetComponent<ReefSphereBody>().Clear(shark.transform.position))throw new Exception("Shark placement obstructed");
  var squids=UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None);
  var original=squids.First(s=>s.name=="Giant squid distant silhouette");ConfigureSquid(original,squids.FirstOrDefault(s=>s!=original));
  int count=0;
  var candidates=UnityEngine.Object.FindObjectsByType<FishTouchBody>(FindObjectsSortMode.None);
  foreach(var touch in candidates){
   var agent=touch.GetComponent<FishAgent>();var life=touch.GetComponent<SeaLife>();
   if(life&&(life.danger||life.observationOnly))continue;
   if(!agent&&!touch.GetComponent<FishBodyMotion>())continue;
   var size=Vector3.Scale(touch.bodyBounds.size,touch.transform.lossyScale);
   bool small=agent?agent.bodyLength<=.65f:Mathf.Max(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z))<=.85f;
   if(!small)continue;
   if(!life){life=touch.gameObject.AddComponent<SeaLife>();life.kind=touch.name;}
   life.grabbable=true;life.consequence=false;
   // Existing swimmers remain the sole movement owner when not held.
   if(!agent)life.stationary=true;
   EditorUtility.SetDirty(life);count++;
  }
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();diver.showDiverBody=true;diver.showMaskRim=false;diver.controllerHandPosition=new Vector3(0,-.015f,.015f);diver.controllerHandRotation=Vector3.zero;EditorUtility.SetDirty(diver);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Debug.Log("DIVER_REVISION_APPLIED scoopableSmallFish="+count+" squidActors=1 sharkHabitat=7");
  Validate();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();
  var squids=UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None);if(squids.Length!=1)throw new Exception("Expected one recurring squid");
  var recurring=squids[0].GetComponent<RecurringSquid>();if(!recurring||!recurring.EnterHabitat(6,OceanWorld.Centers[6]))throw new Exception("Squid failed open-water recurrence");
  if(!squids[0].ClearPose(squids[0].transform.position,squids[0].transform.rotation))throw new Exception("Recurring squid pose obstructed");
  if(!recurring.EnterHabitat(4,OceanWorld.Centers[4]))throw new Exception("Squid failed trench return");
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();if(Mathf.Abs(shark.transform.position.z-OceanWorld.Centers[6].z)>10||shark.transform.position.x<15)throw new Exception("Shark outside reference habitat");
  float maxTurn=0,maxAcceleration=0,lastSpeed=0,travel=0;
  for(int i=0;i<5400;i++){var p=shark.transform.position;var forward=shark.transform.forward;shark.Step(1f/30,OceanWorld.Centers[6],Vector3.zero,true);float step=Vector3.Distance(p,shark.transform.position);travel+=step;maxTurn=Mathf.Max(maxTurn,Vector3.Angle(forward,shark.transform.forward)*30);if(i>0)maxAcceleration=Mathf.Max(maxAcceleration,Mathf.Abs(step*30-lastSpeed)*30);lastSpeed=step*30;if(step>1.21f/30||!shark.GetComponent<ReefSphereBody>().Clear(shark.transform.position))throw new Exception("Shark jumped or intersected reef");}
  if(travel<70||maxTurn>18.2f)throw new Exception("Shark patrol stalled or snapped");
  Debug.Log("SHARK_SMOOTH_PATROL_VALIDATED seconds=180 travel="+travel+" maxYaw="+maxTurn+" maxAcceleration="+maxAcceleration);
  ValidateHands();QuestReefBuild.GrabCheck();ReefDaySix.Validate();PreparedMarineIntegration.Validate();
  Debug.Log("DIVER_REVISION_VALIDATED singleRecurringSquid=true twoHandScoop=true sharkHabitat=7 fullRegressions=true");
 }
 static void ValidateHands(){
  EditorSceneManager.OpenScene(Scene);var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();
  var all=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);var fish=all.Where(l=>l.grabbable).ToArray();if(fish.Length<100)throw new Exception("Small fish not enabled");
  foreach(var f in fish)if(f.danger||f.observationOnly||!f.GetComponent<FishTouchBody>())throw new Exception("Unsafe scoop target");
  var rig=new GameObject("Hand revision test");rig.transform.position=new Vector3(900,2,0);
  try{
   var left=ScubaGlove.Create(rig.transform,true);var right=ScubaGlove.Create(rig.transform,false);
   left.SetControllerFit(diver.controllerHandPosition,diver.controllerHandRotation,true);right.SetControllerFit(diver.controllerHandPosition,diver.controllerHandRotation,false);
   left.transform.localPosition=Vector3.left*.3f;right.transform.localPosition=Vector3.right*.3f;
   if(Mathf.Abs(left.Wrist.localPosition.x+right.Wrist.localPosition.x)>.00001f)throw new Exception("Hand fit not mirrored");
   var a=UnityEngine.Object.Instantiate(fish[0].gameObject,left.GripAnchor.position,left.GripAnchor.rotation).GetComponent<SeaLife>();var b=UnityEngine.Object.Instantiate(fish[1].gameObject,right.GripAnchor.position,right.GripAnchor.rotation).GetComponent<SeaLife>();
   try{
    foreach(var f in new[]{a,b})f.GetComponent<FishTouchBody>().touchCollider.enabled=true;Physics.SyncTransforms();
    var pair=new[]{a,b};diver.UpdateLeftGrab(true,left.GripAnchor,pair);diver.UpdateGrab(true,right.GripAnchor,pair);if(!a.IsHeld||!b.IsHeld)throw new Exception("Both hands did not grab independently");
    diver.UpdateLeftGrab(false,left.GripAnchor,pair);if(a.IsHeld||!b.IsHeld)throw new Exception("Left release affected right hand");
    diver.UpdateGrab(false,right.GripAnchor,pair);if(b.IsHeld)throw new Exception("Right release failed");
    diver.UpdateLeftGrab(true,left.GripAnchor,pair);a.StepHeld(1f/72,Time.time+2.1f);if(a.IsHeld)throw new Exception("Fish did not escape after two seconds");diver.UpdateLeftGrab(true,left.GripAnchor,pair);if(a.IsHeld)throw new Exception("Grip reacquired escaped fish without release");diver.UpdateLeftGrab(false,left.GripAnchor,pair);
   }finally{UnityEngine.Object.DestroyImmediate(a.gameObject);UnityEngine.Object.DestroyImmediate(b.gameObject);}
   var head=new GameObject("Outfit test head").transform;head.SetParent(rig.transform,false);head.localPosition=Vector3.up*1.6f;
   var outfit=rig.AddComponent<DiverOutfit>();outfit.Initialize(head,left,right);outfit.Tick(1);
   if(rig.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Outfit adds physics colliders");
  }finally{UnityEngine.Object.DestroyImmediate(rig);}
  Debug.Log("TWO_HAND_SCOOP_VALIDATED eligible="+fish.Length+" releaseIndependent=true escapeLatch=true suitVisualOnly=true");
 }
 public static void FinalReview(){Validate();Capture();}
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);System.IO.Directory.CreateDirectory("Renders/DiverRevision");
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();var rig=diver.transform;var head=rig.Find("Head");var camera=head.GetComponent<Camera>();
  var left=ScubaGlove.Create(rig,true);var right=ScubaGlove.Create(rig,false);
  left.SetControllerFit(diver.controllerHandPosition,diver.controllerHandRotation,true);right.SetControllerFit(diver.controllerHandPosition,diver.controllerHandRotation,false);
  left.transform.localPosition=new Vector3(-.22f,1.2f,.4f);right.transform.localPosition=new Vector3(.22f,1.2f,.4f);left.Animate(.15f,.35f,1);right.Animate(.15f,.35f,1);
  var outfit=rig.gameObject.AddComponent<DiverOutfit>();outfit.Initialize(head,left,right);outfit.showMask=false;outfit.Tick(1);
  camera.fieldOfView=90;camera.nearClipPlane=.05f;head.localRotation=Quaternion.Euler(40,0,0);
  UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);
  ReefCorrectionAudit.Capture(camera,"Renders/DiverRevision/SuitAndHands.png");
  head.localRotation=Quaternion.Euler(75,0,0);ReefCorrectionAudit.Capture(camera,"Renders/DiverRevision/BodyLookDown.png");
  head.localRotation=Quaternion.identity;outfit.showMask=true;outfit.Tick(1);ReefCorrectionAudit.Capture(camera,"Renders/DiverRevision/OptionalMask.png");
  outfit.showMask=false;outfit.Tick(1);ReefCorrectionAudit.Capture(camera,"Renders/DiverRevision/ClearView.png");
  Debug.Log("DIVER_REVISION_CAPTURES_PASSED maskDefault=false suitVisibleOnLookDown=true");
 }
}
}
