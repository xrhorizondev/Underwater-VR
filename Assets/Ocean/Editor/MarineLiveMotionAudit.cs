using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
[InitializeOnLoad] public static class MarineLiveMotionAudit {
 const string Key="Underwater.MarineLiveMotionAudit";
 class Sample {public Animation animation;public SkinnedMeshRenderer skin;public Vector3[] first;public float movement,time0;}
 static List<Sample> samples;static double began,nextFrame;static bool failed;static Camera preview;static Transform lion;static int imageIndex;
 static MarineLiveMotionAudit(){EditorApplication.update+=Tick;}
 public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");EditorApplication.EnterPlaymode();}
 static Vector3[] Points(SkinnedMeshRenderer skin){var m=new Mesh();skin.BakeMesh(m);var p=m.vertices;UnityEngine.Object.DestroyImmediate(m);return p;}
 static void Tick(){if(!SessionState.GetBool(Key,false))return;if(!EditorApplication.isPlaying){if(samples!=null){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}return;}if(Time.timeSinceLevelLoad<2)return;
 try{if(samples==null){samples=UnityEngine.Object.FindObjectsByType<ReefImportedAnimation>(FindObjectsSortMode.None).Select(m=>new Sample{animation=m.GetComponent<Animation>(),skin=m.GetComponentInChildren<SkinnedMeshRenderer>()}).Where(s=>s.skin&&s.animation&&s.animation.clip&&s.animation.GetComponentInParent<SeaLife>()&&new[]{"Dolphin","Reef shark","Comb jelly","Lionfish"}.Contains(s.animation.GetComponentInParent<SeaLife>().kind)).ToList();foreach(var s in samples){s.first=Points(s.skin);s.time0=s.animation[s.animation.clip.name].time;}began=EditorApplication.timeSinceStartup;
var subject=samples.Single(s=>s.animation.GetComponentInParent<SeaLife>().kind=="Lionfish");lion=subject.animation.GetComponentInParent<SeaLife>().transform;subject.skin.gameObject.layer=31;
preview=new GameObject("Live lionfish preview").AddComponent<Camera>();preview.enabled=false;preview.cullingMask=1<<31;preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=new Color(.025f,.06f,.08f);preview.orthographic=true;preview.orthographicSize=.95f;preview.nearClipPlane=.01f;preview.farClipPlane=20;preview.targetTexture=new RenderTexture(640,480,24);
var lamp=new GameObject("Live preview light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=.9f;lamp.transform.rotation=Quaternion.Euler(30,-35,0);RenderSettings.ambientLight=new Color(.45f,.5f,.55f);RenderSettings.fog=false;Directory.CreateDirectory("Renders/MarineQuality/Animation");
}
if(imageIndex<30&&EditorApplication.timeSinceStartup>=nextFrame){nextFrame=EditorApplication.timeSinceStartup+.18;var center=lion.position+Vector3.up*.05f;var side=imageIndex<15?1:-1;preview.transform.position=center+(lion.right*side+lion.forward*.8f+Vector3.up*.12f).normalized*3;preview.transform.LookAt(center);preview.Render();var previous=RenderTexture.active;RenderTexture.active=preview.targetTexture;var image=new Texture2D(640,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,480),0,0);image.Apply();File.WriteAllBytes("Renders/MarineQuality/Animation/Lionfish_"+imageIndex.ToString("D2")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;imageIndex++;}
 foreach(var s in samples){var points=Points(s.skin);for(int i=0;i<points.Length;i++)s.movement=Mathf.Max(s.movement,Vector3.Distance(points[i],s.first[i]));}
 if(EditorApplication.timeSinceStartup-began<12)return;
 Directory.CreateDirectory("Renders/MarineQuality");var lines=samples.Select(s=>s.animation.GetComponentInParent<SeaLife>().kind+" movement="+s.movement+" playing="+s.animation.isPlaying+" speed="+s.animation[s.animation.clip.name].speed+" time="+s.animation[s.animation.clip.name].time+" initial="+s.time0+" culling="+s.animation.cullingType).ToArray();File.WriteAllLines("Renders/MarineQuality/live-motion.txt",lines);foreach(var l in lines)Debug.Log("LIVE_MARINE "+l);if(samples.Count!=30)throw new Exception("Unexpected animated animal count");if(samples.Any(s=>s.movement<.001f))throw new Exception("Runtime skeletal animation stalled");EditorApplication.ExitPlaymode();
 }catch(Exception e){Debug.LogError(e);failed=true;EditorApplication.ExitPlaymode();}}
}}



