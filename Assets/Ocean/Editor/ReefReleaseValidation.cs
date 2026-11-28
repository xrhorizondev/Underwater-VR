using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
// Run independent suites even when another suite fails, retaining every failure.
public static class ReefReleaseValidation {
 public static void Run(){
  var failures=new List<string>();var results=new List<string>();
  Check("Hand contact",ContactFitValidation.ValidateHands,results,failures);
  Check("Catch follow",HandContactRevision.Validate,results,failures);
  Check("Grab latch",QuestReefBuild.GrabCheck,results,failures);
  Check("Travel clearance",QuestReefBuild.GloveTravelCheck,results,failures);
  Check("Collision and performance cadence",QuestReefBuild.Performance,results,failures);
  Check("All animal movement",NaturalFishMotionAudit.Run,results,failures);
  Check("Diver and shark",DiverRevision.Validate,results,failures);
  Check("Warning surfaces",VRFeedbackRepair.Validate,results,failures);
  Check("Facing, hand bypass and manta",ReefComfortRevision.Run,results,failures);
  Check("LOD and shaders",ReefLodBalance.Run,results,failures);
  Directory.CreateDirectory("Renders/ReleaseValidation");
  File.WriteAllLines("Renders/ReleaseValidation/Results.txt",results);
  if(failures.Count>0)throw new Exception("Release validation failed: "+string.Join("; ",failures));
  Debug.Log("REEF_RELEASE_VALIDATED suites="+results.Count);
 }
 static void Check(string name,Action action,List<string> results,List<string> failures){
  try{EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");action();results.Add("PASS "+name);Debug.Log("RELEASE_PASS "+name);}
  catch(Exception error){results.Add("FAIL "+name+": "+error);failures.Add(name+": "+error.Message);Debug.LogError("RELEASE_FAIL "+name+": "+error);}
 }
}
}
