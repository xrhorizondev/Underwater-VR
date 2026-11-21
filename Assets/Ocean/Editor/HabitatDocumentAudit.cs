using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class HabitatDocumentAudit {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 [Serializable]public class Entry {public string kind,name,parent;public Vector3 position;public int zone;public string[] controllers;public bool matchesDocument;}
 [Serializable]public class SharkMotion {public string clip;public float seconds,tailLateralRange,tailVerticalRange,headMovement,loopSeam;public Vector3 position,bodySize;public bool lateralSwimming,zone7,distant;}
 [Serializable]public class Report {public string status;public Entry[] animals;public SharkMotion shark;public string[] notes;}
 static int Zone(Vector3 p)=>Enumerable.Range(0,8).OrderBy(i=>(p-OceanWorld.Centers[i]).sqrMagnitude).First()+1;
 static int[] Expected(string kind){switch(kind){case "Clownfish":case "Manta":case "Fire coral":return new[]{1};case "Octopus":case "Lionfish":return new[]{2};case "Dolphin":case "Moray":return new[]{3};case "Grouper":return new[]{3,8};case "Comb jelly":case "Lumen":return new[]{4};case "Giant squid":case "Anglerfish":return new[]{5};case "Moon jelly":case "Box jelly":return new[]{6};case "Reef shark":return new[]{7};case "Stingray":case "Ray tail":return new[]{8};case "Turtle":return new[]{1,7};default:return Array.Empty<int>();}}
 static Vector3[] Points(SkinnedMeshRenderer skin,Transform root)=>OctopusRepairValidation.Points(skin).Select(p=>root.InverseTransformPoint(p)).ToArray();
 static SharkMotion SharkCheck(){
  var shark=UnityEngine.Object.FindAnyObjectByType<OpenWaterShark>();var root=shark.transform;var animation=shark.GetComponentInChildren<Animation>();var skin=shark.GetComponentInChildren<SkinnedMeshRenderer>();var clip=animation.clip;clip.SampleAnimation(animation.gameObject,0);var start=Points(skin,root);float minZ=start.Min(p=>p.z),maxZ=start.Max(p=>p.z),length=maxZ-minZ;var tail=Enumerable.Range(0,start.Length).Where(i=>start[i].z<minZ+length*.12f).ToArray();var head=Enumerable.Range(0,start.Length).Where(i=>start[i].z>maxZ-length*.15f).ToArray();var tailCenters=new List<Vector3>();var headCenters=new List<Vector3>();float seam=0;
  for(int phase=0;phase<=120;phase++){clip.SampleAnimation(animation.gameObject,clip.length*phase/120);var p=Points(skin,root);tailCenters.Add(tail.Aggregate(Vector3.zero,(a,i)=>a+p[i])/tail.Length);headCenters.Add(head.Aggregate(Vector3.zero,(a,i)=>a+p[i])/head.Length);if(phase==120)for(int i=0;i<p.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(p[i],start[i]));}
  clip.SampleAnimation(animation.gameObject,0);float lateral=tailCenters.Max(p=>p.x)-tailCenters.Min(p=>p.x),vertical=tailCenters.Max(p=>p.y)-tailCenters.Min(p=>p.y);var size=new Vector3(start.Max(p=>p.x)-start.Min(p=>p.x),start.Max(p=>p.y)-start.Min(p=>p.y),length);
  return new SharkMotion{clip=clip.name,seconds=clip.length,tailLateralRange=lateral,tailVerticalRange=vertical,headMovement=headCenters.Max(p=>Vector3.Distance(p,headCenters[0])),loopSeam=seam,position=root.position,bodySize=size,lateralSwimming=lateral>.02f&&lateral>vertical*4,zone7=Zone(root.position)==7,distant=Mathf.Abs(root.position.x)>12};
 }
 public static void Inspect(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var entries=new List<Entry>();foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){int zone=Zone(life.transform.position);var expected=Expected(life.kind);entries.Add(new Entry{kind=life.kind,name=life.name,parent=life.transform.parent?life.transform.parent.name:"",position=life.transform.position,zone=zone,matchesDocument=expected.Length==0||expected.Contains(zone),controllers=life.GetComponents<MonoBehaviour>().Select(c=>c.GetType().Name).ToArray()});}
  foreach(var squid in UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None))if(!squid.GetComponent<SeaLife>())entries.Add(new Entry{kind="Giant squid",name=squid.name,parent=squid.transform.parent.name,position=squid.transform.position,zone=Zone(squid.transform.position),matchesDocument=Zone(squid.transform.position)==5,controllers=new[]{"SquidSwimmer"}});
  var report=new Report{status="audited",animals=entries.ToArray(),shark=SharkCheck(),notes=new[]{"Great-white model retained per user's supplied-model request; document names a reef shark.","Fixed trench-only squid retained per earlier no-relocation request; the document additionally describes a distant Open Water squid.","Octopus is in Rocky Ledge but rests over sand; document describes a rock perch. Its existing arm-clearance/escape controller was not redesigned by this placement audit.","Restored Moon jellies remain information-only; document also specifies proximity glow/caution without consequence.","Shark fast-pursuit voice warning exists; a dedicated pursuit glow is absent from the current controller.","Pufferfish is mentioned in the interaction examples, but no pufferfish actor or assigned habitat exists."}};Directory.CreateDirectory("Renders/HabitatDocumentAudit");File.WriteAllText("Renders/HabitatDocumentAudit/audit.json",JsonUtility.ToJson(report,true));foreach(var group in entries.GroupBy(e=>e.kind).OrderBy(g=>g.Key))Debug.Log("HABITAT_INVENTORY "+group.Key+" count="+group.Count()+" zones="+string.Join(",",group.GroupBy(e=>e.zone).Select(g=>g.Key+":"+g.Count()))+" mismatches="+group.Count(e=>!e.matchesDocument));Debug.Log("SHARK_REAL_MOTION "+JsonUtility.ToJson(report.shark));
 }
 static void Place(SeaLife life,Vector3 target,Transform parent,int zone){
  var guard=life.GetComponent<ReefAnimalClearance>();var body=life.GetComponent<ReefSphereBody>();if(!guard||!body||!guard.RequiredHeight(target,life.transform.rotation,out float floor))throw new Exception("Missing jelly clearance");target.y=Mathf.Max(target.y,floor);
  bool clear=false;for(int step=0;step<20;step++){if(body.Clear(target)){clear=true;break;}target.y+=.15f;}if(!clear||Zone(target)!=zone)throw new Exception("Jelly target outside safe habitat: "+life.name);
  var old=life.transform.position;life.transform.SetParent(parent,true);life.transform.position=target;guard.Enforce();Debug.Log("HABITAT_JELLY_MOVED "+life.kind+" from="+old+" to="+life.transform.position+" zone="+zone);
 }
 public static void AlignJellyHabitats(){
  var scene=EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var all=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);var badComb=all.Where(a=>a.kind=="Comb jelly"&&Zone(a.transform.position)!=4).OrderBy(a=>a.transform.position.z).ToArray();var badMoon=all.Where(a=>a.kind=="Moon jelly"&&Zone(a.transform.position)!=6).OrderBy(a=>a.transform.position.z).ToArray();if(badComb.Length!=11||badMoon.Length!=14)throw new Exception("Unexpected jelly layout; no relocation applied");
  var before=all.Where(a=>!badComb.Contains(a)&&!badMoon.Contains(a)).ToDictionary(a=>a,a=>EditorJsonUtility.ToJson(a.transform));var combSlots=badComb.Select(a=>a.transform.position).ToArray();var moonSlots=badMoon.Select(a=>a.transform.position).ToArray();var twilight=all.First(a=>a.kind=="Comb jelly"&&Zone(a.transform.position)==4).transform.parent;var drift=all.First(a=>a.kind=="Moon jelly"&&Zone(a.transform.position)==6).transform.parent;
  for(int i=0;i<badComb.Length;i++)Place(badComb[i],moonSlots[i],twilight,4);
  for(int i=0;i<badMoon.Length;i++){
   if(i<combSlots.Length){Place(badMoon[i],combSlots[i],drift,6);continue;}
   bool placed=false;for(int attempt=0;attempt<180&&!placed;attempt++){
    float angle=attempt*2.399963f;float radius=3+(attempt%7)*1.5f;var p=OceanWorld.Centers[5]+new Vector3(Mathf.Cos(angle)*radius,.5f+((attempt%3)-1)*.4f,Mathf.Sin(angle)*radius);
    if(all.Any(a=>a!=badMoon[i]&&(a.kind=="Moon jelly"||a.kind=="Box jelly")&&Vector3.Distance(a.transform.position,p)<2.1f))continue;
    var guard=badMoon[i].GetComponent<ReefAnimalClearance>();if(!guard.RequiredHeight(p,badMoon[i].transform.rotation,out float y))continue;p.y=Mathf.Max(p.y,y);if(!badMoon[i].GetComponent<ReefSphereBody>().Clear(p))continue;Place(badMoon[i],p,drift,6);placed=true;
   }if(!placed)throw new Exception("No safe Moon jelly slot in Jellyfish Drift");
  }
  foreach(var entry in before)if(EditorJsonUtility.ToJson(entry.Key.transform)!=entry.Value)throw new Exception("Changed an unrelated animal placement");
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Inspect();ValidatePlacementsAndShark();Debug.Log("HABITAT_DOCUMENT_ALIGNMENT_PASSED relocatedCombJellies=11 relocatedMoonJellies=14 otherAnimalsUnchanged=true");
 }
 public static void ValidatePlacementsAndShark(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var all=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);int checks=0;
  foreach(var life in all){var expected=Expected(life.kind);if(expected.Length>0&&!expected.Contains(Zone(life.transform.position)))throw new Exception("Animal in wrong habitat: "+life.kind+" "+life.transform.position);}
  foreach(var life in all.Where(a=>a.kind=="Comb jelly"||a.kind=="Moon jelly")){
   int zone=life.kind=="Comb jelly"?4:6;var encounter=life.GetComponent<DeepWaterEncounter>();var ground=life.GetComponent<ReefAnimalClearance>();var home=life.transform.position;
   for(int frame=0;frame<1200;frame++){encounter.Step(1f/30,zone-1,home+Vector3.forward*.6f,0);ground.Enforce();if(Zone(life.transform.position)!=zone||!ground.RequiredHeight(life.transform.position,life.transform.rotation,out float y)||life.transform.position.y<y-.0001f)throw new Exception("Jelly left its habitat or entered sand");checks++;}
  }
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var motion=SharkCheck();if(!motion.lateralSwimming||!motion.zone7||!motion.distant||motion.loopSeam>.001f||motion.headMovement>motion.tailLateralRange*.15f)throw new Exception("Shark animation/location mismatch");
  var shark=UnityEngine.Object.FindAnyObjectByType<OpenWaterShark>();var lifeShark=shark.GetComponent<SeaLife>();if(lifeShark.danger||lifeShark.consequence||!lifeShark.observationOnly)throw new Exception("Shark contact became punitive");float minDistance=float.PositiveInfinity,maxStep=0;const float dt=1f/30;
  for(int frame=0;frame<3600;frame++){var previous=shark.transform.position;shark.Step(dt,OceanWorld.Centers[6],Vector3.zero,true);float step=Vector3.Distance(previous,shark.transform.position);maxStep=Mathf.Max(maxStep,step);minDistance=Mathf.Min(minDistance,Vector3.Distance(shark.transform.position,OceanWorld.Centers[6]));if(Zone(shark.transform.position)!=7||step>1.21f*dt)throw new Exception("Shark left habitat or jumped");}
  for(int frame=0;frame<120;frame++)if(shark.Step(dt,shark.transform.position-Vector3.forward*8,Vector3.forward*.4f,true))throw new Exception("Slow approach triggered shark warning");bool warned=false;for(int frame=0;frame<40;frame++)warned|=shark.Step(dt,shark.transform.position-Vector3.forward*8,Vector3.forward*1.3f,true);if(!warned)throw new Exception("Fast pursuit warning missing");shark.MarkWarned();for(int frame=0;frame<90;frame++)if(shark.Step(dt,shark.transform.position-Vector3.forward*8,Vector3.forward*1.3f,true))throw new Exception("Shark warning ignored cooldown");
  Debug.Log("HABITAT_DOCUMENT_VALIDATED jellyMotionChecks="+checks+" sharkSeconds=120 sharkMinRouteDistance="+minDistance+" maxSharkFrameStep="+maxStep+" lateralTail=true fastPursuitOnly=true noContactPunishment=true");
 }
}
}
