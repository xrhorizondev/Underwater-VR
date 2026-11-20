using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class OctopusRepairValidation {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static Vector3[] Points(SkinnedMeshRenderer skin){var mesh=skin.sharedMesh;var v=mesh.vertices;var delta=new Vector3[v.Length];for(int shape=0;shape<mesh.blendShapeCount;shape++){float w=skin.GetBlendShapeWeight(shape)/100;mesh.GetBlendShapeFrameVertices(shape,mesh.GetBlendShapeFrameCount(shape)-1,delta,null,null);for(int i=0;i<v.Length;i++)v[i]+=delta[i]*w;}var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var weights=mesh.boneWeights;for(int i=0;i<v.Length;i++){var w=weights[i];var p=v[i];v[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return v;}
 public static void Run(){
  AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/UnderwaterArt/PreparedMarine/Octopus/Octopus.fbx");var takes=importer.clipAnimations;foreach(var take in takes){take.lastFrame=301;take.loopTime=true;take.wrapMode=WrapMode.Loop;}importer.clipAnimations=takes;importer.SaveAndReimport();var scene=EditorSceneManager.OpenScene(Scene);var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(l=>l.kind=="Octopus");
  var wrapper=actor.transform.Find("Prepared animated model");var skin=wrapper.GetComponentInChildren<SkinnedMeshRenderer>();var animation=wrapper.GetComponentInChildren<Animation>();var clip=animation.clip;
  if(skin.bones.Any(b=>!b)||skin.bones.Length!=75||skin.sharedMesh.blendShapeCount!=2||skin.sharedMesh.triangles.Length/3<58000)throw new Exception("Restored octopus rig/morph/geometry missing");
  string before=EditorJsonUtility.ToJson(actor.GetComponent<ReefEncounter>());var pose=actor.transform.localToWorldMatrix;var colliders=actor.GetComponentsInChildren<Collider>().ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
  bool first=true;var envelope=new Bounds();float deformation=0;Vector3[] initial=null;float[] min={100,100},max={-100,-100};
  for(int frame=0;frame<=60;frame++){
   clip.SampleAnimation(animation.gameObject,clip.length*frame/60);var points=Points(skin);if(initial==null)initial=points;
   for(int i=0;i<points.Length;i++){var p=skin.rootBone.InverseTransformPoint(points[i]);if(first){envelope=new Bounds(p,Vector3.zero);first=false;}else envelope.Encapsulate(p);if(float.IsNaN(points[i].x)||float.IsInfinity(points[i].x))throw new Exception("Invalid animated octopus vertex");deformation=Mathf.Max(deformation,Vector3.Distance(initial[i],points[i]));}
   for(int i=0;i<2;i++){float value=skin.GetBlendShapeWeight(i);min[i]=Mathf.Min(min[i],value);max[i]=Mathf.Max(max[i],value);}
  }
  if(deformation<.03f||max.Zip(min,(a,b)=>a-b).Max()<1)throw new Exception("Octopus rig or morph animation is static");
  envelope.Expand(envelope.size.magnitude*.06f);skin.localBounds=envelope;skin.updateWhenOffscreen=false;
  clip.SampleAnimation(animation.gameObject,0);if(before!=EditorJsonUtility.ToJson(actor.GetComponent<ReefEncounter>())||pose!=actor.transform.localToWorldMatrix||colliders.Any(p=>EditorJsonUtility.ToJson(p.Key)!=p.Value))throw new Exception("Octopus encounter/placement/collider modified");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  for(int frame=0;frame<=60;frame++){clip.SampleAnimation(animation.gameObject,clip.length*frame/60);var visible=skin.bounds;visible.Expand(.01f);foreach(var p in Points(skin))if(!visible.Contains(p))throw new Exception("Animated octopus outside culling bounds");}
  Debug.Log("OCTOPUS_REPAIR_VALIDATED triangles="+skin.sharedMesh.triangles.Length/3+" bones="+skin.bones.Length+" morphs="+skin.sharedMesh.blendShapeCount+" frames=61 deformation="+deformation+" morphRanges="+string.Join(",",max.Zip(min,(a,b)=>a-b))+" encounterAndCollidersPreserved=true");
  Capture();PreparedMarineIntegration.Validate();
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/OctopusUnity");var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(l=>l.kind=="Octopus");var skin=actor.transform.Find("Prepared animated model").GetComponentInChildren<SkinnedMeshRenderer>();var animation=skin.GetComponentInParent<Animation>();var camera=Camera.main;
  for(int frame=0;frame<4;frame++){
   animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*frame/4);var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.ClearBlendShapes();var points=Points(skin);mesh.vertices=points;mesh.RecalculateBounds();mesh.RecalculateNormals();
   var proxy=new GameObject("Sampled octopus pose");proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
   var bounds=mesh.bounds;float size=bounds.extents.magnitude;camera.transform.position=bounds.center+(actor.transform.forward*2.4f+actor.transform.right*1.5f+Vector3.up*1.8f)*size;camera.transform.LookAt(bounds.center);camera.fieldOfView=45;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/OctopusUnity/Phase_"+frame+".png");skin.enabled=true;UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(mesh);
  }
 }
}
}
