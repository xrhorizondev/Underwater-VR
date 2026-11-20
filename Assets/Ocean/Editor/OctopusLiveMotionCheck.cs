using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
[InitializeOnLoad] public static class OctopusLiveMotionCheck {
 const string Key="OctopusLiveMotionCheck";static double started;static SkinnedMeshRenderer skin;static Animation animation;static Quaternion[] rest;static float boneMotion;static float[] min,max;
 static OctopusLiveMotionCheck(){EditorApplication.playModeStateChanged+=State;}
 public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}}
 static void Check(){try{
  if(!skin){var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(l=>l.kind=="Octopus");skin=actor.transform.Find("Prepared animated model").GetComponentInChildren<SkinnedMeshRenderer>();animation=skin.GetComponentInParent<Animation>();animation.cullingType=AnimationCullingType.AlwaysAnimate;rest=skin.bones.Select(b=>b.localRotation).ToArray();min=new[]{100f,100f};max=new[]{-100f,-100f};}
  for(int i=0;i<rest.Length;i++)boneMotion=Mathf.Max(boneMotion,Quaternion.Angle(rest[i],skin.bones[i].localRotation));for(int i=0;i<2;i++){var value=skin.GetBlendShapeWeight(i);min[i]=Mathf.Min(min[i],value);max[i]=Mathf.Max(max[i],value);}
  if(EditorApplication.timeSinceStartup-started<5)return;
  if(!animation.isPlaying||boneMotion<1||max.Zip(min,(a,b)=>a-b).Max()<.1f)throw new Exception("Octopus runtime rig/morph motion is static");
  var camera=new GameObject("Live octopus QA camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;var bounds=skin.bounds;float size=bounds.extents.magnitude;camera.transform.position=bounds.center+new Vector3(size*1.4f,size*1.5f,size*2.3f);camera.transform.LookAt(bounds.center);camera.fieldOfView=45;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);System.IO.Directory.CreateDirectory("Renders/OctopusUnity");ReefCorrectionAudit.Capture(camera,"Renders/OctopusUnity/PlayMode.png");
  Debug.Log("OCTOPUS_LIVE_MOTION_PASSED seconds=5 boneDegrees="+boneMotion+" morphRange="+string.Join(",",max.Zip(min,(a,b)=>a-b))+" originalRigAndMorphAutoplay=true");Finish(0);
 }catch(Exception e){Debug.LogException(e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Check;SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
}
}
