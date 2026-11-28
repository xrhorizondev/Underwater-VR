using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class LargeAnimalPathRevision {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static readonly Vector2[] Sizes={new Vector2(12,10),new Vector2(10,8),new Vector2(8,8)};
 static bool Clear(Transform actor,Vector3 position,Quaternion rotation,float margin){
  var envelope=actor.GetComponent<ReefAnimalClearance>();var body=actor.GetComponent<ReefSphereBody>();
  if(envelope){var scale=actor.lossyScale;var e=Vector3.Scale(envelope.animatedBody.extents,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))+Vector3.one*margin;
   var center=position+rotation*Vector3.Scale(envelope.animatedBody.center,scale);
   if(center.y+e.y>7.5f||Physics.CheckBox(center,e,rotation,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))return false;
   if(!envelope.RequiredHeight(position,rotation,out float height)||position.y<height+margin)return false;
  }
  return !body||!Physics.CheckSphere(position+rotation*Vector3.Scale(body.centerOffset,actor.lossyScale),body.radius+margin,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);
 }
 static ReefLargeAnimalPatrol FindLane(Transform[] actors,Vector3 origin,float speed){
  foreach(var size in Sizes)foreach(float shift in new[]{0f,6f,-6f,12f,-12f})foreach(float shiftZ in new[]{0f,6f,-6f})for(float lift=0;lift<=24;lift+=.5f){
   var center=origin+new Vector3(shift,lift,shiftZ);bool clear=true;
   for(int i=0;i<128&&clear;i++){float angle=i*Mathf.PI*2/128;var p=center+new Vector3(size.x*Mathf.Sin(angle),0,size.y*Mathf.Cos(angle));var q=Quaternion.LookRotation(new Vector3(size.x*Mathf.Cos(angle),0,-size.y*Mathf.Sin(angle)));
    foreach(var actor in actors)if(!Clear(actor,p,q,1)){clear=false;break;}
   }
   if(!clear)continue;
   ReefLargeAnimalPatrol first=null;
   for(int i=0;i<actors.Length;i++){var actor=actors[i];var path=actor.GetComponent<ReefLargeAnimalPatrol>();if(!path)path=actor.gameObject.AddComponent<ReefLargeAnimalPatrol>();path.center=center;path.radii=size;path.phase=i*Mathf.PI*2/actors.Length;path.speed=speed;path.rockMargin=1;
    // Start at the route's nearest longitudinal station; no runtime entry snap.
    if(actors.Length==1)path.phase=Mathf.Atan2((actor.position.x-center.x)/size.x,(actor.position.z-center.z)/size.y);
    actor.SetPositionAndRotation(path.Position(path.phase),path.Rotation(path.phase));
    foreach(var clip in actor.GetComponentsInChildren<ReefImportedAnimation>())clip.minimumTravelRate=Mathf.Max(.35f,clip.minimumTravelRate);
    EditorUtility.SetDirty(path);if(!first)first=path;
    Debug.Log("LARGE_LANE "+actor.name+" center="+center+" radii="+size+" start="+actor.position);
   }
   return first;
  }
  throw new Exception("No rock-clear patrol lane for "+actors[0].name);
 }
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();
  var manta=UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None).Single(m=>m.isRay);
  FindLane(new[]{manta.transform},manta.transform.position+manta.transform.right*10,.42f);
  var dolphins=UnityEngine.Object.FindObjectsByType<DolphinSwimController>(FindObjectsSortMode.None).OrderBy(d=>d.podIndex).ToArray();
  var dolphinLane=FindLane(dolphins.Select(d=>d.transform).ToArray(),dolphins[0].GetComponent<WreckCreature>().habitatCenter,.95f);
  foreach(var dolphin in dolphins)dolphin.GetComponent<WreckCreature>().habitatCenter=dolphinLane.center;
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();
  var sharkLane=FindLane(new[]{shark.transform},shark.transform.position+shark.transform.right*12,1.05f);
  sharkLane.phase=Mathf.PI*.5f;shark.transform.SetPositionAndRotation(sharkLane.Position(sharkLane.phase),sharkLane.Rotation(sharkLane.phase));
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  ValidateTwice();
 }
 public static void ValidateTwice(){
  ValidateSceneLinks();
  var report=new StringBuilder();Directory.CreateDirectory("Renders/LargeAnimalPaths");
  for(int pass=1;pass<=2;pass++){
   EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var paths=UnityEngine.Object.FindObjectsByType<ReefLargeAnimalPatrol>(FindObjectsSortMode.None);
   if(paths.Length!=5)throw new Exception("Expected manta, three dolphins and shark patrols");
   foreach(var path in paths){
    for(int sample=0;sample<720;sample++){float t=sample*Mathf.PI*2/720;if(!Clear(path.transform,path.Position(t),path.Rotation(t),.8f))throw new Exception("Patrol approaches rock: "+path.name+" phase="+t);}
    if(ReefSimulationRange.Radius(path.transform)<190)throw new Exception("Visible large animal still distance-paused");
    float distance=0,maxTurn=0,worstStop=0,stop=0;var clips=path.GetComponentsInChildren<ReefImportedAnimation>();foreach(var clip in clips)clip.InitializePlayback();
    // Six minutes covers multiple whole circuits, including the loop seam.
    float dt=pass==1?1f/30:1f/72;
    var initialCycles=clips.Select(c=>c.CycleTime).ToArray();
    for(int frame=0;frame<Mathf.RoundToInt(360/dt);frame++){
     var before=path.transform.position;var heading=path.transform.forward;
     var manta=path.GetComponent<MarineSwimmer>();var dolphin=path.GetComponent<DolphinSwimController>();var shark=path.GetComponent<OpenWaterShark>();
     if(manta)manta.Step(dt);else if(dolphin)dolphin.Step(dt);else if(shark)shark.Step(dt,OceanWorld.Centers[6],Vector3.zero,true);
     foreach(var clip in clips)if(clip.distanceClock)clip.StepPlayback(dt);
     Physics.SyncTransforms();float step=Vector3.Distance(before,path.transform.position);distance+=step;stop=step<.002f*dt?stop+dt:0;worstStop=Mathf.Max(worstStop,stop);maxTurn=Mathf.Max(maxTurn,Vector3.Angle(heading,path.transform.forward)/dt);
     if(!Clear(path.transform,path.transform.position,path.transform.rotation,.7f)||step>path.speed*dt*1.1f)throw new Exception("Patrol collision/jump: "+path.name);
    }
    if(distance<path.speed*350||worstStop>.1f||maxTurn>16)throw new Exception("Patrol stalled or snapped: "+path.name+" distance="+distance+" stop="+worstStop+" turn="+maxTurn);
    for(int c=0;c<clips.Length;c++)if(clips[c].distanceClock&&(clips[c].CurrentRate<.2f||clips[c].CycleTime-initialCycles[c]<30))throw new Exception("Swim animation stopped: "+path.name);
    report.AppendLine("Pass "+pass+" "+path.name+" seconds=360 travel="+distance+" maxTurn="+maxTurn+" longestStop="+worstStop+" rockMargin>=0.7m range=200m");
    File.WriteAllText("Renders/LargeAnimalPaths/Validation.txt",report.ToString());
   }
   var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();if(ReefSimulationRange.Radius(squid.transform,72)<190)throw new Exception("Distant squid paused");
   NaturalFishMotionAudit.MovementOnly();
   report.AppendLine("Pass "+pass+": complete school/featured movement and collision audit passed.");
   File.WriteAllText("Renders/LargeAnimalPaths/Validation.txt",report.ToString());
   Debug.Log("LARGE_ANIMAL_MOVEMENT_PASS="+pass);
  }
  Debug.Log("LARGE_ANIMAL_PATHS_VALIDATED_TWICE");
 }
 public static void ValidateSceneLinks(){
  var text=File.ReadAllText(Scene);var headers=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^--- !u!\d+ &(\d+)[^\r\n]*");
  var blocks=new System.Collections.Generic.Dictionary<string,string>();
  for(int i=0;i<headers.Count;i++){int end=i+1<headers.Count?headers[i+1].Index:text.Length;blocks.Add(headers[i].Groups[1].Value,text.Substring(headers[i].Index,end-headers[i].Index));}
  int patrols=0;
  foreach(var entry in blocks){if(!entry.Value.Contains("guid: cf6cb3f79ccd5b84ba4735849b75ebc0"))continue;
   var owner=System.Text.RegularExpressions.Regex.Match(entry.Value,@"m_GameObject: \{fileID: (\d+)\}").Groups[1].Value;
   if(!blocks.TryGetValue(owner,out var actor)||!actor.Contains("- component: {fileID: "+entry.Key+"}"))throw new Exception("Patrol is not referenced by its GameObject: "+entry.Key);
   foreach(System.Text.RegularExpressions.Match reference in System.Text.RegularExpressions.Regex.Matches(actor,@"- component: \{fileID: (\d+)\}")){
    if(!blocks.TryGetValue(reference.Groups[1].Value,out var component)||!component.Contains("m_GameObject: {fileID: "+owner+"}"))throw new Exception("Invalid animal component link: "+owner);
   }
   patrols++;
  }
  if(patrols!=5)throw new Exception("Expected five correctly linked patrols, found "+patrols);
  Debug.Log("PATROL_SCENE_LINKS_VALIDATED count=5 bidirectional=true duplicateIds=false");
 }
 public static void ValidateSquidTwice(){
  var report=new StringBuilder();Directory.CreateDirectory("Renders/LargeAnimalPaths");
  for(int pass=1;pass<=2;pass++){
   EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();
   float dt=pass==1?1f/30:1f/72,distance=0,maxStep=0;
   for(int frame=0;frame<Mathf.RoundToInt(360/dt);frame++){var before=squid.transform.position;squid.Step(dt);Physics.SyncTransforms();float step=Vector3.Distance(before,squid.transform.position);distance+=step;maxStep=Mathf.Max(maxStep,step);if(!squid.ClearPose(squid.transform.position,squid.transform.rotation)||step>.1f)throw new Exception("Squid collision or jump");}
   if(distance<10||!ReefSimulationRange.Contains(squid.transform.position,squid.transform.position+Vector3.right*160,ReefSimulationRange.Radius(squid.transform,72)))throw new Exception("Distant squid stalled");
   var player=squid.GetComponentInChildren<Animation>();if(!player||!player.clip)throw new Exception("Squid swim clip missing");
   var bones=player.GetComponentsInChildren<Transform>();player.clip.SampleAnimation(player.gameObject,0);var rotations=bones.Select(b=>b.localRotation).ToArray();var positions=bones.Select(b=>b.localPosition).ToArray();player.clip.SampleAnimation(player.gameObject,player.clip.length*.25f);
   if(!bones.Select((b,i)=>Quaternion.Angle(b.localRotation,rotations[i])>.1f||Vector3.Distance(b.localPosition,positions[i])>.001f).Any(changed=>changed))throw new Exception("Squid clip is static");
   report.AppendLine("Pass "+pass+" squid seconds=360 travel="+distance+" maxStep="+maxStep+" fullBodyClear=true visibleAt160mActive=true animatedBones=true");
  }
  File.WriteAllText("Renders/LargeAnimalPaths/Squid.txt",report.ToString());Debug.Log("SQUID_MOVEMENT_VALIDATED_TWICE");
 }
}
}
