using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
[InitializeOnLoad] public static class CreatureMotionLiveReview {
 const string Key="Underwater.CreatureMotionReview",Output="Renders/CreatureMotionFix/Live";
 class Subject {public SeaLife life;public SkinnedMeshRenderer skin;public Vector3[] first;public Vector3 start;public float deformation,distance,maxStep;}
 static Subject[] subjects;static Camera camera;static double next;static int frames;static bool failed;static readonly List<string> lines=new();
 static CreatureMotionLiveReview(){EditorApplication.update+=Tick;}
 public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");EditorApplication.EnterPlaymode();}
 static Vector3[] Points(SkinnedMeshRenderer skin){var mesh=new Mesh();skin.BakeMesh(mesh);var points=mesh.vertices;UnityEngine.Object.DestroyImmediate(mesh);return points;}
 static void Tick(){if(!SessionState.GetBool(Key,false))return;if(!EditorApplication.isPlaying){if(subjects!=null){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}return;}if(Time.timeSinceLevelLoad<2)return;
  try{
   if(subjects==null){Directory.CreateDirectory(Output);typeof(Diver).GetField("menu",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(Diver.Instance,false);subjects=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>l.kind=="Grouper"||l.kind=="Reef shark").OrderBy(l=>l.kind).ThenBy(l=>l.name).Select(l=>new Subject{life=l,skin=l.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.enabled),start=l.transform.position}).ToArray();if(subjects.Length!=3)throw new Exception("Missing live fish");foreach(var s in subjects)s.first=Points(s.skin);camera=new GameObject("Creature live review camera").AddComponent<Camera>();camera.enabled=false;camera.CopyFrom(Camera.main);camera.enabled=false;camera.fieldOfView=42;camera.nearClipPlane=.02f;camera.farClipPlane=90;camera.targetTexture=new RenderTexture(800,500,24);RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",1);}
   foreach(var s in subjects){var points=Points(s.skin);for(int i=0;i<points.Length;i++)s.deformation=Mathf.Max(s.deformation,Vector3.Distance(s.first[i],points[i]));float delta=Vector3.Distance(s.start,s.life.transform.position);s.distance+=delta;s.maxStep=Mathf.Max(s.maxStep,delta);s.start=s.life.transform.position;}
   if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.1;
   foreach(var s in subjects){var bounds=s.skin.bounds;float length=bounds.size.magnitude;camera.transform.position=s.life.transform.position+s.life.transform.right*length*1.2f+Vector3.up*length*.2f+s.life.transform.forward*length*.15f;camera.transform.LookAt(bounds.center);camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;var picture=new Texture2D(800,500,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,800,500),0,0);picture.Apply();File.WriteAllBytes(Output+"/"+s.life.name.Replace(" ","")+"_"+frames.ToString("D3")+".png",picture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(picture);RenderTexture.active=previous;}
   frames++;if(frames<70)return;foreach(var s in subjects){string line=s.life.name+" actualSkinDeformation="+s.deformation+" travel="+s.distance+" maxFrameStep="+s.maxStep;lines.Add(line);Debug.Log("CREATURE_LIVE "+line);if(s.deformation<.025f||s.distance<.15f)throw new Exception("Live fish remains rigid or stationary: "+s.life.name);}File.WriteAllLines(Output+"/Validation.txt",lines);Debug.Log("CREATURE_LIVE_VALIDATED");EditorApplication.ExitPlaymode();
  }catch(Exception e){failed=true;Debug.LogException(e);EditorApplication.ExitPlaymode();}
 }
}
}
