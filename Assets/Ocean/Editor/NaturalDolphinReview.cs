using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace Underwater.Editor {
public static class NaturalDolphinReview {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/NaturalDolphins/";
 static WreckCreature[] Actors()=>UnityEngine.Object.FindObjectsByType<WreckCreature>(FindObjectsSortMode.None).Where(a=>a.species==WreckCreature.Species.Dolphin).OrderBy(a=>a.name).ToArray();
 [Serializable]class Track {public string name;public Vector3[] points;}
 [Serializable]class Paths {public Track[] tracks;}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);foreach(var actor in Actors()){var d=actor.GetComponent<DolphinSwimController>();d.Initialize();d.ApplyPose(0,0,0,0,-10);var points=OctopusRepairValidation.Points(d.skin).Select(actor.transform.InverseTransformPoint).ToArray();float length=(points.Max(p=>p.z)-points.Min(p=>p.z))*actor.transform.lossyScale.z;d.bodyLength=Mathf.Clamp(length,1,8);d.referenceSpeed=d.bodyLength*.28f;d.minimumSeparation=Mathf.Max(3.9f,actor.GetComponent<ReefSphereBody>().radius*2+.18f);EditorUtility.SetDirty(d);}EditorSceneManager.SaveScene(scene);Validate();Debug.Log("NATURAL_DOLPHIN_PROFILE_APPLIED");}
 public static void CompleteProof(){Validate();DolphinMotionIntegration.ValidateSolo();DolphinLiveSyncCheck.Run();}
 public static void Validate(){
 EditorSceneManager.OpenScene(Scene);var actors=Actors();var dolphins=actors.Select(a=>a.GetComponent<DolphinSwimController>()).ToArray();var paths=actors.Select(a=>new List<Vector3>()).ToArray();float[] distance=new float[3],stalls=new float[3],lastSpeed=new float[3],lastTurn=new float[3];float minGap=float.MaxValue,maxTurn=0,maxTurnAcceleration=0,maxLinearAcceleration=0,maxStep=0,maxStall=0,maxRange=0,minAlignment=1;int glides=0,swims=0,coastFrames=0;var modes=new HashSet<string>();
 for(int f=0;f<3600;f++){
  for(int i=0;i<3;i++){var actor=actors[i];var d=dolphins[i];var start=actor.transform.position;actor.Step(.05f);actor.GetComponent<ReefAnimalClearance>().Enforce();Physics.SyncTransforms();var delta=actor.transform.position-start;distance[i]+=delta.magnitude;maxStep=Mathf.Max(maxStep,delta.magnitude);if(delta.sqrMagnitude>.000001f)minAlignment=Mathf.Min(minAlignment,Vector3.Dot(delta.normalized,actor.transform.forward));if(!actor.GetComponent<ReefSphereBody>().Clear(actor.transform.position))throw new Exception("Natural dolphin overlaps reef: "+actor.name+" frame="+f);stalls[i]=delta.magnitude<.002f*.05f?stalls[i]+.05f:0;maxStall=Mathf.Max(maxStall,stalls[i]);maxRange=Mathf.Max(maxRange,Vector3.ProjectOnPlane(actor.transform.position-actor.habitatCenter,Vector3.up).magnitude);maxTurn=Mathf.Max(maxTurn,Mathf.Abs(d.TurnRate));if(f>10){maxTurnAcceleration=Mathf.Max(maxTurnAcceleration,Mathf.Abs(d.TurnRate-lastTurn[i])/.05f);maxLinearAcceleration=Mathf.Max(maxLinearAcceleration,Mathf.Abs(d.ActualSpeed-lastSpeed[i])/.05f);}lastTurn[i]=d.TurnRate;lastSpeed[i]=d.ActualSpeed;modes.Add(d.Mode.ToString());if(d.Gliding)glides++;else swims++;if(d.ActualSpeed>.3f&&d.Effort<.15f)coastFrames++;if(f%10==0)paths[i].Add(actor.transform.position);
  }
  for(int i=0;i<3;i++)for(int j=i+1;j<3;j++)minGap=Mathf.Min(minGap,Vector3.Distance(actors[i].transform.position,actors[j].transform.position));
 }
 var text="180-second natural-course simulation\nBody lengths="+string.Join(",",dolphins.Select(d=>d.bodyLength.ToString("F2")))+" m; reference speeds="+string.Join(",",dolphins.Select(d=>d.referenceSpeed.ToString("F2")))+" m/s\nTravel="+string.Join(",",distance.Select(x=>x.ToString("F2")))+" m; closest root spacing="+minGap+" m\nMax heading rate="+maxTurn+" deg/s; max heading acceleration="+maxTurnAcceleration+" deg/s²; max linear acceleration="+maxLinearAcceleration+" m/s²\nMax step="+maxStep+" m; longest stop="+maxStall+" s; max habitat radius="+maxRange+" m; min forward alignment="+minAlignment+"\nGlide samples="+glides+" powered samples="+swims+" observed grouping="+string.Join(",",modes)+"\n";
 File.WriteAllText(Output+"Validation.txt",text);File.WriteAllText(Output+"Paths.json",JsonUtility.ToJson(new Paths{tracks=actors.Select((a,i)=>new Track{name=a.name,points=paths[i].ToArray()}).ToArray()},true));Debug.Log("NATURAL_DOLPHIN_METRICS\n"+text);
 if(distance.Any(d=>d<20)||minGap<3.2f||maxStep>.09f||maxTurn>14||maxTurnAcceleration>15||maxLinearAcceleration>.5f||maxStall>6||glides==0||swims==0||coastFrames<20||maxRange>25)throw new Exception("Natural cruising checks failed; inspect metrics");EditorSceneManager.OpenScene(Scene);
 }
}
}


