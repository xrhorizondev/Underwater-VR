using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
[InitializeOnLoad]
public static class MorayLiveMotionCheck {
 const string Key="MorayLiveMotionCheck";static double started;static Transform tail,jaw;static Quaternion tailStart,jawStart;static float tailMotion,jawMotion;static Transform[] checkedBones;static Vector3[] lengths,scales;
 static MorayLiveMotionCheck(){EditorApplication.playModeStateChanged+=State;}
 public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}
 static void Check(){
  try{
   var motion=UnityEngine.Object.FindFirstObjectByType<YellowMorayMotion>();if(!motion)throw new Exception("Live moray missing");
   var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();if(diver&&diver.MenuOpen)diver.SendMessage("SetMenu",false);
   if(!tail){var bones=motion.animationPlayer.GetComponentsInChildren<Transform>();tail=bones.Single(t=>t.name=="Spine11");jaw=bones.Single(t=>t.name=="Jaw");tailStart=tail.localRotation;jawStart=jaw.localRotation;checkedBones=bones;lengths=bones.Select(b=>b.localPosition).ToArray();scales=bones.Select(b=>b.localScale).ToArray();}
   for(int i=0;i<checkedBones.Length;i++)if(Vector3.Distance(lengths[i],checkedBones[i].localPosition)>.00001f||Vector3.Distance(scales[i],checkedBones[i].localScale)>.00001f)throw new Exception("Moray bone stretched: "+checkedBones[i].name);
   tailMotion=Mathf.Max(tailMotion,Quaternion.Angle(tailStart,tail.localRotation));jawMotion=Mathf.Max(jawMotion,Quaternion.Angle(jawStart,jaw.localRotation));
   if(EditorApplication.timeSinceStartup-started<5)return;
   if(tailMotion<.5f||jawMotion<.3f)throw new Exception("Play-mode idle is static: tail="+tailMotion+" jaw="+jawMotion);
   motion.actor.React();for(int frame=0;frame<600;frame++){motion.actor.Step(1f/60);motion.Step(1f/60);for(int i=0;i<checkedBones.Length;i++)if(Vector3.Distance(lengths[i],checkedBones[i].localPosition)>.00001f||Vector3.Distance(scales[i],checkedBones[i].localScale)>.00001f)throw new Exception("Transition stretched bone: "+checkedBones[i].name);}
   Debug.Log("MORAY_ROTATION_ONLY_PASSED reactionRetreatRecovery=600steps boneLengthsAndScalesPreserved=true");
   Debug.Log("MORAY_LIVE_MOTION_PASSED seconds=5 tailDegrees="+tailMotion+" jawDegrees="+jawMotion+" state="+motion.CurrentState);Finish(0);
  }catch(Exception error){Debug.LogException(error);Finish(1);}
 }
 static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
}
}

