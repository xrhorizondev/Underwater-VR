using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Underwater.Editor {
public static class ReefDayOne {
 const string ScenePath="Assets/Scenes/Underwater_Realistic.unity";
 [MenuItem("Underwater/Apply side boundaries and bake startup audio")]
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(ScenePath);
  var motor=UnityEngine.Object.FindFirstObjectByType<DiverCollisionMotor>();
  if(!motor)throw new Exception("Saved diver collision motor missing");
  var bounds=UnityEngine.Object.FindFirstObjectByType<ReefDiveBounds>();
  if(!bounds)bounds=new GameObject("Dive perimeter | solid sides").AddComponent<ReefDiveBounds>();
  motor.diveBounds=bounds;
  Wall(bounds,"Left side",new Vector3(-44.5f,-36.75f,116),new Vector3(1,89.5f,266));
  Wall(bounds,"Right side",new Vector3(44.5f,-36.75f,116),new Vector3(1,89.5f,266));
  Wall(bounds,"Reef entrance end",new Vector3(0,-36.75f,-16.5f),new Vector3(90,89.5f,1));
  Wall(bounds,"Final wreck end",new Vector3(0,-36.75f,248.5f),new Vector3(90,89.5f,1));
  Wall(bounds,"Below seabed safety",new Vector3(0,-80.5f,116),new Vector3(90,1,266));
  RepairShellClearance(motor);BakeAudio();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Validate();
 }
 static void Wall(ReefDiveBounds bounds,string name,Vector3 center,Vector3 size){
  var child=bounds.transform.Find(name);
  if(!child){child=new GameObject(name).transform;child.SetParent(bounds.transform,false);}
  child.position=center;child.gameObject.layer=8;
  var box=child.GetComponent<BoxCollider>();if(!box)box=child.gameObject.AddComponent<BoxCollider>();
  box.size=size;box.isTrigger=false;
 }
 static Vector3 Landing(TeleportShell shell){var point=shell.transform.position;point.y=shell.GetComponent<Collider>().bounds.max.y+.15f;return point;}
 static void RepairShellClearance(DiverCollisionMotor motor){
  motor.Initialize();Vector3 saved=motor.transform.position;int repaired=0;Physics.SyncTransforms();
  foreach(var shell in UnityEngine.Object.FindObjectsByType<TeleportShell>(FindObjectsSortMode.None)){
   if(motor.TryTeleport(Landing(shell)))continue;
   Vector3 original=shell.transform.position;bool found=false;
   for(int ring=1;ring<=5&&!found;ring++)for(int step=0;step<16&&!found;step++){
    float angle=step*Mathf.PI/8;var point=original+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(ring*.4f);
    point.y=ReefGeometry.Floor(point.x,point.z)+.12f;shell.transform.position=point;Physics.SyncTransforms();
    found=motor.TryTeleport(Landing(shell));
   }
   if(!found){shell.transform.position=original;throw new Exception("No nearby clear landing for shell "+original);}
   repaired++;Debug.Log("TELEPORT_CLEARANCE_REPAIRED from="+original+" to="+shell.transform.position);
  }
  // Editor-only authoring pass; do not save a validation teleport as the spawn.
  var controller=motor.GetComponent<CharacterController>();controller.enabled=false;motor.transform.position=saved;controller.enabled=true;
  Physics.SyncTransforms();Debug.Log("TELEPORT_CLEARANCE repaired="+repaired);
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();
  ReefCollisionValidation.Run();
  var motor=UnityEngine.Object.FindFirstObjectByType<DiverCollisionMotor>();motor.Initialize();
  var bounds=motor.diveBounds;if(!bounds)throw new Exception("Missing perimeter reference");
  int clear=0;foreach(var point in OceanWorld.Centers)if(motor.TryTeleport(point))clear++;
  Require(clear==8,"All habitat menu destinations must remain clear");
  int shells=0,blocked=0;foreach(var shell in UnityEngine.Object.FindObjectsByType<TeleportShell>(FindObjectsSortMode.None)){
   Vector3 landing=Landing(shell);
   if(!motor.TryTeleport(landing)){
    blocked++;var controller=motor.GetComponent<CharacterController>();
    var center=landing+controller.center;float extent=controller.height*.5f-controller.radius;
    var hits=Physics.OverlapCapsule(center+Vector3.up*extent,center-Vector3.up*extent,controller.radius+.03f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);
    Debug.LogError("BLOCKED_SHELL position="+shell.transform.position+" obstacles="+string.Join(",",Array.ConvertAll(hits,h=>h.name)));
   }shells++;
  }
  Require(shells==24&&blocked==0,"Expected 24 clear teleport shells; blocked="+blocked);
  foreach(var target in new[]{new Vector3(-46,0,100),new Vector3(46,0,100),new Vector3(0,0,-18),new Vector3(0,0,250)})
   Require(!motor.TryTeleport(target),"Outside teleport was accepted");
  var starts=new[]{new Vector3(-42,3,100),new Vector3(42,3,100),new Vector3(0,3,-14),new Vector3(0,3,246)};
  var directions=new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward};
  for(int i=0;i<starts.Length;i++){
   Require(motor.TryTeleport(starts[i]),"Boundary test start blocked");motor.Move(directions[i]*8);
   Require(bounds.Contains(Camera.main.transform.position,.2f),"Swim escaped perimeter");
   Camera.main.transform.localPosition+=directions[i]*3;motor.TrackHead();
   Require(bounds.Contains(Camera.main.transform.position,.15f),"Tracked head escaped perimeter");
   Camera.main.transform.localPosition-=directions[i]*3;motor.TrackHead();
  }
  motor.Move(Vector3.up*100);Require(Camera.main.transform.position.y<=6.501f,"Water ceiling failed");
  FishMotionValidation.Run();QuestReefBuild.GrabCheck();
  foreach(string clip in new[]{"ReefBreathing","ReefScore","ReefClick","ReefTravel"})Require(Resources.Load<AudioClip>(clip),"Missing baked audio "+clip);
  Debug.Log("DAY_ONE_VALIDATED sides=4 outsideTeleports=rejected roomScale=passed menuDestinations=8 shells=24 ceiling=6.5 audio=baked");
 }
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static void BakeAudio(){
  Write("ReefBreathing",8,null,true);
  Write("ReefScore",16,(i,rate)=>{float t=(float)i/rate,value=0;foreach(float f in new[]{130.8128f,196f,261.6256f,329.6276f})value+=Mathf.Sin(2*Mathf.PI*Mathf.Round(f*16)*t/16)*.12f;return value*(.7f+.3f*Mathf.Cos(2*Mathf.PI*t/16));});
  Write("ReefClick",.12f,(i,rate)=>Cue((float)i/rate,.12f));
  Write("ReefTravel",.7f,(i,rate)=>Cue((float)i/rate,.7f));
  AssetDatabase.Refresh();
  foreach(string name in new[]{"ReefBreathing","ReefScore","ReefClick","ReefTravel"}){
   var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Ocean/Resources/"+name+".wav");
   var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;sample.preloadAudioData=true;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
  }
 }
 static float Cue(float t,float duration)=>Mathf.Sin(2*Mathf.PI*(540*t+180*t*t))*Mathf.Sin(Mathf.PI*t/duration)*Mathf.Exp(-t*5)*.4f;
 static void Write(string name,float seconds,Func<int,int,float> sample,bool breathing=false){
  const int rate=22050;int count=Mathf.RoundToInt(seconds*rate);var rng=new System.Random(48);float low=0;
  using(var writer=new BinaryWriter(File.Create("Assets/Ocean/Resources/"+name+".wav"))){
   writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
   for(int i=0;i<count;i++){
    float value;
    if(breathing){float t=(float)i/rate,phase=t%4;float exhale=phase<1.8f?Mathf.Sin(phase/1.8f*Mathf.PI):0;float inhale=phase>2.1f?Mathf.Sin((phase-2.1f)/1.9f*Mathf.PI):0;
     low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,.055f);float bubble=0;
     for(int j=0;j<9;j++){float age=phase-(.12f+j*.17f);if(age>0&&age<.11f)bubble+=Mathf.Sin(2*Mathf.PI*(340+j*47)*age*(1+age*.8f))*Mathf.Exp(-age*62)*.025f;}
     value=low*(.035f+.14f*exhale+.06f*inhale)+bubble*exhale+Mathf.Sin(t*2*Mathf.PI*42)*.002f;
    }else value=sample(i,rate);
    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));
   }
  }
 }
}
}
