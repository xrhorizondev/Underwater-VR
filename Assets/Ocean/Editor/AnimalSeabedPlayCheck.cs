using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
[InitializeOnLoad]
public static class AnimalSeabedPlayCheck {
 const string Key="Underwater.AnimalSandPlayCheck";
 static int checks,frames;static double nextCheck;static Vector3[] starts;static ReefAnimalClearance[] guards;static bool configured;static string failure;
 static AnimalSeabedPlayCheck(){EditorApplication.update+=Tick;}
 public static void Run(){SessionState.SetInt(Key,1);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");EditorApplication.EnterPlaymode();}
 static void Tick(){
  int phase=SessionState.GetInt(Key,0);if(phase==0)return;
  if(phase==2&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt(Key,0);Debug.Log("ANIMAL_SAND_PLAYMODE_VALIDATED seconds=30 allMovementOwners=true");EditorApplication.Exit(0);return;}
  if(phase==3&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt(Key,0);Debug.LogError("ANIMAL_SAND_PLAYMODE_FAILED");EditorApplication.Exit(1);return;}
  if(!EditorApplication.isPlaying||EditorApplication.isCompiling||Time.timeSinceLevelLoad<2)return;
  try{
   if(!configured){
    var diver=Diver.Instance;if(!diver||!diver.Head)return;
    typeof(Diver).GetField("menu",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(diver,false);
    guards=UnityEngine.Object.FindObjectsByType<ReefAnimalClearance>(FindObjectsSortMode.None);starts=guards.Select(g=>g.transform.position).ToArray();configured=true;Debug.Log("ANIMAL_SAND_PLAYMODE_STARTED guarded="+guards.Length);
   }
   frames++;
   if(EditorApplication.timeSinceStartup<nextCheck)return;nextCheck=EditorApplication.timeSinceStartup+.1;
   foreach(var guard in guards){if(!guard.RequiredHeight(guard.transform.position,guard.transform.rotation,out float height)||guard.transform.position.y<height-.0005f)throw new Exception("Live animal below animated clearance: "+guard.name+" actual="+guard.transform.position+" required="+height+" time="+Time.timeSinceLevelLoad);checks++;}
   if(Time.timeSinceLevelLoad<30)return;
   int moving=guards.Where((g,i)=>Vector3.Distance(starts[i],g.transform.position)>.02f).Count();
   if(moving<200)throw new Exception("Too few active live patrols: "+moving);
   // Check actual live bone poses as well as the complete safety envelopes.
   foreach(var guard in guards)foreach(var skin in guard.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled))foreach(var point in OctopusRepairValidation.Points(skin))if(!guard.animatedBody.Contains(guard.transform.InverseTransformPoint(point)))throw new Exception("Live skeleton escaped envelope: "+guard.name+" part="+skin.name);
   Directory.CreateDirectory("Renders/AnimalSeabedAudit");File.WriteAllText("Renders/AnimalSeabedAudit/playmode.json","{\"status\":\"passed\",\"seconds\":30,\"guarded\":"+guards.Length+",\"moving\":"+moving+",\"clearanceChecks\":"+checks+"}");
   Debug.Log("ANIMAL_SAND_LIVE_RESULT moving="+moving+" checks="+checks+" frames="+frames);SessionState.SetInt(Key,2);EditorApplication.ExitPlaymode();
  }catch(Exception e){failure=e.ToString();Debug.LogError(failure);SessionState.SetInt(Key,3);EditorApplication.ExitPlaymode();}
 }
}
}

