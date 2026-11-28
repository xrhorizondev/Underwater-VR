using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReefComfortRevision {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/ReefComfort/";
 static void Require(bool result,string message){if(!result)throw new Exception(message);}
 public static void Run(){
  Directory.CreateDirectory(Output);var report=new StringBuilder();var scene=EditorSceneManager.OpenScene(Scene);
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();diver.transform.rotation=Quaternion.identity;
  int corals=0;long triangles=0;foreach(var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))if(ReefStableLod.IsCoral(group)){
   var stable=group.GetComponent<ReefStableLod>();if(!stable)stable=group.gameObject.AddComponent<ReefStableLod>();
   ReefStableLod.ConfigureCoral(group);Require(stable.Choose(10f)==0,"Near coral detail missing");
   foreach(var r in group.GetLODs()[0].renderers){var f=r.GetComponent<MeshFilter>();if(f&&f.sharedMesh)triangles+=f.sharedMesh.triangles.Length/3;}
   corals++;
  }
  Require(corals>100,"Coral detail coverage missing");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  report.AppendLine("Coral groups using native crossfade LOD="+corals+"; total authored coral triangles="+triangles+" (all scene, not simultaneously visible).");
  CheckFacing(report);CheckSchool(report);File.WriteAllText(Output+"Validation.txt",report.ToString());CheckManta(report);File.WriteAllText(Output+"Validation.txt",report.ToString());
  ReefCollisionValidation.Run();QuestReefBuild.GloveTravelCheck();QuestReefBuild.GrabCheck();Debug.Log("REEF_COMFORT_VALIDATED\n"+report);
 }
 static void CheckFacing(StringBuilder report){
  var rig=new GameObject("Facing test");var head=new GameObject("Head").transform;head.SetParent(rig.transform,false);head.localPosition=new Vector3(.3f,1.6f,.2f);rig.transform.position=new Vector3(900,2,0);var motor=rig.AddComponent<DiverCollisionMotor>();motor.Initialize();
  try{foreach(float yaw in new[]{-180f,-90f,0,45,120,179}){head.localRotation=Quaternion.Euler(12,yaw,0);var eye=head.position;Require(motor.FaceHeading(Vector3.forward),"Heading alignment failed");Require(Vector3.Dot(Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized,Vector3.forward)>.9999f&&Vector3.Distance(head.position,eye)<.0001f,"Startup facing moved the headset or faced backward");}}
  finally{UnityEngine.Object.DestroyImmediate(rig);}report.AppendLine("Six headset starting headings face reef +Z; room-scale eye position preserved.");
 }
 static void CheckSchool(StringBuilder report){
  var root=new GameObject("Hand bypass school");root.transform.position=new Vector3(900,2,0);var school=root.AddComponent<FishSchool>();school.habitatCenter=root.transform.position+Vector3.forward*3;school.habitatRadius=6;
  try{
   for(int i=0;i<12;i++){var fish=new GameObject("Lane fish "+i);fish.transform.SetParent(root.transform,false);fish.transform.localPosition=new Vector3((i%4-1.5f)*.3f,0,-2-i/4*.3f);fish.AddComponent<ReefSphereBody>().radius=.08f;var a=fish.AddComponent<FishAgent>();a.bodyLength=.18f;a.cruiseSpeed=.65f;}
   school.Initialize();float closest=100,maxWidth=0;int left=0,right=0;const float dt=1f/60;
   for(int f=0;f<2400;f++){bool present=f<1200;school.Simulate(dt,present?(Vector3?)root.transform.position:null,null);foreach(var a in school.fish){var p=a.transform.position-root.transform.position;if(present){closest=Mathf.Min(closest,p.magnitude);if(Mathf.Abs(p.z)<.3f){if(p.x<-.2f)left++;if(p.x>.2f)right++;}maxWidth=Mathf.Max(maxWidth,Mathf.Abs(p.x));}}}
   var centroid=school.fish.Aggregate(Vector3.zero,(sum,a)=>sum+a.transform.position)/school.fish.Length;float spread=school.fish.Average(a=>Vector3.Distance(a.transform.position,centroid));
   report.AppendLine("Hand bypass: min palm clearance="+closest+" left/right samples="+left+"/"+right+" widest half-width="+maxWidth+" regroup mean spread="+spread);
   File.WriteAllText(Output+"School.txt",report.ToString());Require(closest>.20f&&left>0&&right>0,"School did not bypass both sides of the hand safely");Require(spread<3,"School failed to regroup after hand removal");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static void CheckManta(StringBuilder report){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var manta=UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None).First(m=>m.isRay);var body=manta.GetComponent<ReefSphereBody>();var clearance=manta.GetComponent<ReefAnimalClearance>();manta.Initialize();clearance?.Enforce();float travel=0,maxVertical=0,maxTurn=0;const float dt=1f/60;
  for(int f=0;f<7200;f++){var p=manta.transform.position;var forward=manta.transform.forward;manta.Step(dt);clearance?.Enforce();Physics.SyncTransforms();var delta=manta.transform.position-p;travel+=delta.magnitude;maxVertical=Mathf.Max(maxVertical,Mathf.Abs(delta.y)/dt);maxTurn=Mathf.Max(maxTurn,Vector3.Angle(forward,manta.transform.forward)/dt);Require(!body||body.Clear(manta.transform.position),"Manta intersects reef");}
  report.AppendLine("Manta 120s: travel="+travel+" max vertical speed="+maxVertical+" max turn="+maxTurn);File.WriteAllText(Output+"Validation.txt",report.ToString());Require(travel>8&&maxVertical<.2f&&maxTurn<18,"Manta stalled or has abrupt vertical/turn movement");
 }
 public static void AllMotion(){NaturalFishMotionAudit.MovementOnly();}
}
}
