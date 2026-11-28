using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReefLodBalance {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/ReefLodBalance/";
 static long Triangles(Renderer[] renderers){long total=0;foreach(var r in renderers){var f=r.GetComponent<MeshFilter>();if(f&&f.sharedMesh)total+=f.sharedMesh.GetIndexCount(0)/3;}return total;}
 public static void Run(){
  Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(Scene);var groups=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(ReefStableLod.IsCoral).ToArray();var report=new StringBuilder();
  foreach(var group in groups){
   ReefStableLod.ConfigureCoral(group);var lods=group.GetLODs();var scale=group.transform.lossyScale;float size=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
   float nearHeight=size*1.15f/(2*(6+size*.5f));float fadeStart=lods[0].screenRelativeTransitionHeight+(1-lods[0].screenRelativeTransitionHeight)*lods[0].fadeTransitionWidth;
   if(nearHeight<=fadeStart||group.fadeMode!=LODFadeMode.None||group.animateCrossFading)throw new Exception("Near coral detail or spatial fade setup failed: "+group.name);
   if(ReefStableLod.NeedsCoralFade(lods,10f)||!ReefStableLod.NeedsCoralFade(lods,lods[0].screenRelativeTransitionHeight*1.1f))throw new Exception("Fade shader not limited to transition bands");
   for(int i=1;i<lods.Length;i++)if(lods[i].screenRelativeTransitionHeight>=lods[i-1].screenRelativeTransitionHeight)throw new Exception("Invalid coral LOD ordering");
  }
  var shaderNames=groups.SelectMany(g=>g.GetLODs()).SelectMany(l=>l.renderers).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader).Select(m=>m.shader.name).Distinct().ToArray();report.AppendLine("Coral shaders: "+string.Join(", ",shaderNames));
  foreach(var name in shaderNames)if(name!="Standard"&&name!="Underwater/Reef Surface"&&name!="Underwater/Quest Reef")throw new Exception("Coral shader needs crossfade support: "+name);
  foreach(var name in new[]{"Underwater/Reef Surface","Underwater/Quest Reef"}){var shader=Shader.Find(name);if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Crossfade shader compile failed: "+name);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var cam=Camera.main;cam.fieldOfView=90;cam.aspect=1;cam.farClipPlane=160;long fullSum=0,lodSum=0,peakFull=0,peakLod=0;int views=0;
  foreach(var center in OceanWorld.Centers)for(int yaw=0;yaw<360;yaw+=45){
   cam.transform.position=center+Vector3.up*1.6f;cam.transform.rotation=Quaternion.Euler(0,yaw,0);var planes=GeometryUtility.CalculateFrustumPlanes(cam);long full=0,lod=0;
   foreach(var group in groups){var levels=group.GetLODs();if(!levels[0].renderers.Any(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds)))continue;
    full+=Triangles(levels[0].renderers);var s=group.transform.lossyScale;float size=group.size*Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));float distance=Mathf.Max(.01f,Vector3.Distance(cam.transform.position,group.transform.TransformPoint(group.localReferencePoint)));float height=size*1.15f/(2*distance);
    for(int i=0;i<levels.Length;i++)if(height>=levels[i].screenRelativeTransitionHeight){lod+=Triangles(levels[i].renderers);float upper=i==0?1:levels[i-1].screenRelativeTransitionHeight;float start=levels[i].screenRelativeTransitionHeight+(upper-levels[i].screenRelativeTransitionHeight)*levels[i].fadeTransitionWidth;if(i+1<levels.Length&&height<start)lod+=Triangles(levels[i+1].renderers);break;}
   }
   fullSum+=full;lodSum+=lod;peakFull=Math.Max(peakFull,full);peakLod=Math.Max(peakLod,lod);views++;
  }
  report.AppendLine("Coral groups="+groups.Length+"; near detail retained at least 6m from bounds; native spatial dither crossfades only near transitions, no per-frame forced coral selection.");
  report.AppendLine("64 habitat views conservative triangle estimate (includes both meshes while fading): full-detail sum="+fullSum+" balanced sum="+lodSum+" reduction="+(1-(double)lodSum/fullSum).ToString("P1")+" peak full="+peakFull+" peak balanced="+peakLod);
  File.WriteAllText(Output+"Validation.txt",report.ToString());if(lodSum>=fullSum*.8)throw new Exception("LOD geometry savings are insufficient");
  EditorSceneManager.OpenScene(Scene);cam=Camera.main;var target=groups.FirstOrDefault(); // Scene reopened: use fresh references for captures.
  target=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(ReefStableLod.IsCoral).Where(g=>g.GetLODs().Length>=3).OrderBy(g=>(g.transform.position-cam.transform.position).sqrMagnitude).First();
  var focus=target.GetLODs()[0].renderers[0].bounds.center;foreach(float distance in new[]{5f,16f,24f}){cam.transform.position=focus+new Vector3(0,.15f,-1).normalized*distance;cam.transform.LookAt(focus);UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(cam);ReefCorrectionAudit.Capture(cam,Output+"Coral_"+distance+"m.png");}
  ReefCollisionValidation.Run();QuestReefBuild.GloveTravelCheck();Debug.Log("REEF_LOD_BALANCED\n"+report);
 }
}
}
