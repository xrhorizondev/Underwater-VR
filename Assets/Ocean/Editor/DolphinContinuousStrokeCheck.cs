using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace Underwater.Editor {
public static class DolphinContinuousStrokeCheck {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var dolphins=UnityEngine.Object.FindObjectsByType<DolphinSwimController>(FindObjectsSortMode.None).OrderBy(d=>d.podIndex).ToArray();var report=new System.Text.StringBuilder();
 foreach(var d in dolphins){d.pod=null;d.Initialize();float minRate=float.MaxValue,minGlide=float.MaxValue,maxRate=0;int glides=0;var tail=d.animationPlayer.GetComponentsInChildren<Transform>().Single(t=>t.name=="TALE01");float windowMotion=0,minWindow=float.MaxValue;var last=tail.localRotation;
 for(int f=0;f<1200;f++){d.GetComponent<WreckCreature>().Step(.05f);d.GetComponent<ReefAnimalClearance>().Enforce();Physics.SyncTransforms();if(f>100){minRate=Mathf.Min(minRate,d.StrokeRate);maxRate=Mathf.Max(maxRate,d.StrokeRate);windowMotion+=Quaternion.Angle(last,tail.localRotation);if(d.Gliding){glides++;minGlide=Mathf.Min(minGlide,d.Effort);}if(f%60==59){if(f>=179)minWindow=Mathf.Min(minWindow,windowMotion);windowMotion=0;}}last=tail.localRotation;if(!d.GetComponent<ReefSphereBody>().Clear(d.transform.position))throw new Exception("Dolphin overlaps reef");}
 if(minRate<.12f||minGlide<.15f||minWindow<3||glides<20)throw new Exception("Continuous stroke missing: "+d.name+" rate="+minRate+" glide="+minGlide+" tail="+minWindow);
 report.AppendLine(d.name+": minimum stroke rate="+minRate+" Hz; minimum glide effort="+minGlide+"; minimum tail rotation per 3-second window="+minWindow+" degrees; glide samples="+glides);
 }
 Directory.CreateDirectory("Renders/DolphinVideoIssue");File.WriteAllText("Renders/DolphinVideoIssue/ContinuousStroke.txt",report.ToString());Debug.Log("CONTINUOUS_DOLPHIN_STROKE_VALIDATED\n"+report);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
 }
 public static void CompleteProof(){Run();CompactDolphinPatrol.Validate();DolphinLiveSyncCheck.Run();}
}
}
