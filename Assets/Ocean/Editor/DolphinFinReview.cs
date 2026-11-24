using System;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace Underwater.Editor {
public static class DolphinFinReview {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/DolphinFins/";
 public static void Run(){
 var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var report=new System.Text.StringBuilder();foreach(var d in UnityEngine.Object.FindObjectsByType<DolphinSwimController>(FindObjectsSortMode.None)){
 d.Initialize();var root=d.transform;var skin=d.skin;var guard=d.GetComponent<ReefAnimalClearance>();var local=skin.localBounds;var actor=guard.animatedBody;
 for(int f=0;f<120;f++){d.StepVisual(.1f,d.referenceSpeed,0);foreach(float effort in new[]{0f,1f})foreach(float turn in new[]{-1f,1f}){d.ApplyPose(f/120f*Mathf.PI*2,effort,turn,0,-10);foreach(var p in OctopusRepairValidation.Points(skin)){actor.Encapsulate(root.InverseTransformPoint(p));local.Encapsulate((skin.rootBone?skin.rootBone:skin.transform).InverseTransformPoint(p));}}}
 skin.localBounds=local;guard.animatedBody=actor;d.ApplyPose(0,0,0,0,-10);var first=OctopusRepairValidation.Points(skin);var weights=skin.sharedMesh.boneWeights;float left=0,right=0;
 for(int f=0;f<100;f++){d.StepVisual(.1f,d.referenceSpeed,0);d.ApplyPose(0,0,0,0,-10);var pts=OctopusRepairValidation.Points(skin);for(int i=0;i<pts.Length;i++){var w=weights[i];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};for(int j=0;j<4;j++){if(values[j]<.4f)continue;var name=skin.bones[ids[j]].name;if(name.StartsWith("FLIPPER_L"))left=Mathf.Max(left,(pts[i]-first[i]).magnitude);if(name.StartsWith("FLIPPER_R"))right=Mathf.Max(right,(pts[i]-first[i]).magnitude);}}}
 if(left<.025f||right<.025f)throw new Exception("Fin motion missing: L="+left+" R="+right);report.AppendLine(d.name+" independent gliding fin motion: left="+left+" m, right="+right+" m; tail phase/effort held fixed for geometry check.");typeof(DolphinSwimController).GetField("finTravel",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(d,0f);d.ApplyPose(0,0,0,0,-10);
 }
 EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"Validation.txt",report.ToString());Capture();Debug.Log("DOLPHIN_FINS_VALIDATED\n"+report);
 }
 public static void Capture(){
 EditorSceneManager.OpenScene(Scene);var d=UnityEngine.Object.FindObjectsByType<DolphinSwimController>(FindObjectsSortMode.None).OrderBy(x=>x.podIndex).First();d.Initialize();var cam=Camera.main;var optics=cam.GetComponent<ReefOptics>();if(optics)optics.enabled=false;var fin=d.animationPlayer.GetComponentsInChildren<Transform>().Single(t=>t.name=="FLIPPER_L01");var anchor=fin.position;cam.transform.position=anchor+d.transform.right*1.45f-d.transform.forward*.4f-Vector3.up*.1f;cam.transform.LookAt(anchor);cam.fieldOfView=46;Directory.CreateDirectory(Output+"Animation");
 for(int f=0;f<72;f++){d.StepVisual(1f/8,d.referenceSpeed,0);d.ApplyPose(0,0,0,0,-10);DolphinMotionIntegration.CapturePosed(cam,d.skin,Output+"Animation/Frame"+f.ToString("D3")+".png");}EditorSceneManager.OpenScene(Scene);
 }
 public static void CompleteProof(){NaturalDolphinReview.Validate();DolphinLiveSyncCheck.Run();}
}
}

