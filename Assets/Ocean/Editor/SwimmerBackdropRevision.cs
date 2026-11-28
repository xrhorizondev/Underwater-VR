using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class SwimmerBackdropRevision {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/SwimmerBackdrop/";
 static Dictionary<string,string> ProtectedPoses(){
  var result=new Dictionary<string,string>();
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
   if(t.GetComponent<SeaLife>()||t.GetComponent<TeleportShell>()||t.GetComponent<Diver>()||t.name=="Head"||t.name=="Eroded arch voussoir"||t.name.Contains("ship on"))
    result[GlobalObjectId.GetGlobalObjectIdSlow(t).ToString()]=EditorJsonUtility.ToJson(t);
  return result;
 }
 public static void Run(){
  Directory.CreateDirectory(Output);EditorSceneManager.OpenScene(Scene);var poses=ProtectedPoses();CaptureBackdrop("Before");
  OuterSeabedDress.Run();ClusterCoralDress.Run();EditorSceneManager.OpenScene(Scene);
  var after=ProtectedPoses();foreach(var pair in poses)if(!after.TryGetValue(pair.Key,out string pose)||pose!=pair.Value)throw new Exception("Protected reef/rig pose changed: "+pair.Key);
  var islands=GameObject.Find("Outer seabed rock gardens");var colliders=islands.GetComponentsInChildren<MeshCollider>();
  if(colliders.Length<30)throw new Exception("Too few background islands");
  foreach(var c in colliders)if(!c.sharedMesh||c.gameObject.layer!=8)throw new Exception("Background rock lost solid geometry");
  CaptureBackdrop("After");ValidateHands();ReefCollisionValidation.Run();QuestReefBuild.GloveTravelCheck();QuestReefBuild.GrabCheck();
  File.WriteAllText(Output+"Validation.txt","Swimmer hand shader rendered from both sides. Both hand meshes contain open, trigger, grip and hold poses within their visibility bounds; dynamic occlusion disabled; offscreen skin updates enabled.\nBackground rock islands="+colliders.Length+"; protected animal, shell, reef arch and rig poses="+poses.Count+" unchanged.\nCollision, grab latch and eight habitat destinations validated. Physical Quest visibility and performance require a headset check.\n");
  Debug.Log("SWIMMER_BACKDROP_VALIDATED islands="+colliders.Length+" protectedPoses="+poses.Count+" handVisibility=true oldGrab=true");
 }
 static void CaptureBackdrop(string suffix){
  var cam=Camera.main;cam.transform.position=new Vector3(25,3,-16);cam.transform.LookAt(new Vector3(65,-1,-65));cam.fieldOfView=78;cam.farClipPlane=160;
  UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(cam);ReefCorrectionAudit.Capture(cam,Output+"Backdrop"+suffix+".png");
 }
 public static void ReviewHands(){Directory.CreateDirectory(Output);ValidateHands();}
 static void ValidateHands(){
  EditorSceneManager.OpenScene(Scene);var cam=Camera.main;var rig=new GameObject("Swimmer hand visibility review");rig.transform.position=new Vector3(900,2,0);
  try{
   foreach(bool left in new[]{true,false}){
    var hand=ScubaGlove.Create(rig.transform,left);hand.transform.localPosition=new Vector3(left?-.13f:.13f,0,0);hand.transform.localRotation=Quaternion.Euler(-90,0,0);
    hand.transform.rotation=Quaternion.FromToRotation(hand.GripAnchor.up,Vector3.up)*hand.transform.rotation;
    var skin=hand.GetComponentInChildren<SkinnedMeshRenderer>();
    if(skin.allowOcclusionWhenDynamic||!skin.updateWhenOffscreen)throw new Exception("Hands can be hidden by dynamic occlusion");
    for(int pose=0;pose<4;pose++){
     for(int channel=0;channel<3;channel++)skin.SetBlendShapeWeight(channel,pose==channel+1?100:0);
     var baked=new Mesh();skin.BakeMesh(baked);foreach(var v in baked.vertices)if(!skin.localBounds.Contains(v))throw new Exception("Hand pose outside visibility bounds");UnityEngine.Object.DestroyImmediate(baked);
    }
    hand.Animate(.1f,.15f,1);if(ShaderUtil.ShaderHasError(skin.sharedMaterial.shader))throw new Exception("Swimmer skin shader failed");
   }
   RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);cam.fieldOfView=48;cam.nearClipPlane=.01f;
   foreach(bool palm in new[]{true,false}){cam.transform.position=rig.transform.position+new Vector3(0,palm?.42f:-.42f,.12f);cam.transform.LookAt(rig.transform.position+Vector3.forward*.02f);ReefCorrectionAudit.Capture(cam,Output+(palm?"HandsPalm":"HandsBack")+".png");}
  }finally{UnityEngine.Object.DestroyImmediate(rig);}
 }
}
}
