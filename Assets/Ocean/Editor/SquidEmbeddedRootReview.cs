using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class SquidEmbeddedRootReview {
 const string Model="Assets/UnderwaterArt/BlenderSquid/ReferenceSquid.fbx",Prefab="Assets/UnderwaterArt/BlenderSquid/GiantSquid.prefab",Output="Renders/SquidEmbeddedRoots/";
 static void BoundsAndCheck(GameObject model,StringBuilder report){
  var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var animation=model.GetComponentInChildren<Animation>();if(!skin||!animation||!animation.clip||skin.bones.Length!=30)throw new Exception("Original squid rig/clip missing");
  Bounds envelope=default;bool first=true;Vector3[] start=null,end=null;float movement=0;
  for(int f=0;f<=48;f++){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*f/48);var points=OctopusRepairValidation.Points(skin);if(f==0)start=points;if(f==48)end=points;for(int i=0;i<points.Length;i++){var local=skin.rootBone.InverseTransformPoint(points[i]);if(first){envelope=new Bounds(local,Vector3.zero);first=false;}else envelope.Encapsulate(local);if(f==12)movement=Mathf.Max(movement,Vector3.Distance(points[i],start[i]));}}
  envelope.Expand(.002f);skin.localBounds=envelope;skin.updateWhenOffscreen=false;
  float seam=start.Zip(end,(a,b)=>Vector3.Distance(a,b)).Max();if(seam>.001f||movement<.01f)throw new Exception("Invalid squid animation seam/deformation "+seam+" / "+movement);
  for(int f=0;f<=48;f++){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*f/48);var visible=skin.bounds;visible.Expand(.02f);foreach(var p in OctopusRepairValidation.Points(skin))if(!visible.Contains(p))throw new Exception("Squid outside animation bounds at phase "+f);}
  animation.clip.SampleAnimation(animation.gameObject,0);report.AppendLine(model.name+": triangles="+skin.sharedMesh.triangles.Length/3+", bones="+skin.bones.Length+", clip="+animation.clip.name+", samples=49, seam="+seam+", deformation="+movement+", cullingBounds=passed");
 }
 public static void Run(){
  Directory.CreateDirectory(Output);AssetDatabase.Refresh();var imp=(ModelImporter)AssetImporter.GetAtPath(Model);imp.animationType=ModelImporterAnimationType.Legacy;imp.importAnimation=true;imp.isReadable=true;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.materialImportMode=ModelImporterMaterialImportMode.None;var clips=imp.defaultClipAnimations;foreach(var c in clips){c.name="Swim";c.loopTime=true;c.wrapMode=WrapMode.Loop;}imp.clipAnimations=clips;imp.SaveAndReimport();
  var report=new StringBuilder();var prefab=PrefabUtility.LoadPrefabContents(Prefab);try{BoundsAndCheck(prefab,report);PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var actors=UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None);if(actors.Length==0)throw new Exception("Gameplay squid missing");
  foreach(var actor in actors){var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();if(AssetDatabase.GetAssetPath(skin.sharedMesh)!=Model)throw new Exception("Gameplay squid uses another model");BoundsAndCheck(actor.gameObject,report);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var review=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));model.transform.position=Vector3.zero;BoundsAndCheck(model,report);
  var camera=new GameObject("Squid repair camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.07f,.10f);camera.nearClipPlane=.01f;camera.farClipPlane=200;camera.orthographic=true;
  RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.75f,.75f,.75f);var light=new GameObject("Soft daylight",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=.7f;light.transform.rotation=Quaternion.Euler(35,30,0);
  var sk=model.GetComponentInChildren<SkinnedMeshRenderer>();var anim=model.GetComponentInChildren<Animation>();var root=sk.bones.First(b=>b.name=="Root");var axis=(sk.bones.First(b=>b.name=="Mantle").position-root.position).normalized;var across=Vector3.Cross(Vector3.up,axis).normalized;
  var pts=OctopusRepairValidation.Points(sk);var bounds=new Bounds(pts[0],Vector3.zero);foreach(var p in pts)bounds.Encapsulate(p);
  camera.orthographicSize=12;camera.transform.position=bounds.center+across*30+Vector3.up*9;camera.transform.LookAt(bounds.center);
  EditorSceneManager.SaveScene(review,"Assets/Scenes/Squid_Old_Repaired_Review.unity");
  foreach(float phase in new[]{0f,.25f,.5f,.75f}){
   anim.clip.SampleAnimation(anim.gameObject,anim.clip.length*phase);
   var sampled=UnityEngine.Object.Instantiate(sk.sharedMesh);sampled.vertices=OctopusRepairValidation.Points(sk);sampled.RecalculateBounds();sampled.RecalculateNormals();var proxy=new GameObject("Sampled animation pose",typeof(MeshFilter),typeof(MeshRenderer));proxy.GetComponent<MeshFilter>().sharedMesh=sampled;proxy.GetComponent<MeshRenderer>().sharedMaterials=sk.sharedMaterials;sk.enabled=false;
   camera.orthographicSize=12;camera.transform.position=bounds.center+across*30+Vector3.up*9;camera.transform.LookAt(bounds.center);ReefCorrectionAudit.Capture(camera,Output+"Whole_"+(int)(phase*100)+".png");
   var focus=root.position-axis*.5f;camera.orthographicSize=3.8f;camera.transform.position=focus+across*7-Vector3.up*5-axis*5;camera.transform.LookAt(focus,axis);ReefCorrectionAudit.Capture(camera,Output+"Roots_"+(int)(phase*100)+".png");
   camera.orthographicSize=7;focus=root.position-axis*5;camera.transform.position=focus+across*13-Vector3.up*5;camera.transform.LookAt(focus,axis);ReefCorrectionAudit.Capture(camera,Output+"Tentacles_"+(int)(phase*100)+".png");
   sk.enabled=true;UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(sampled);
  }
  AssetDatabase.SaveAssets();File.WriteAllText(Output+"UnityValidation.txt",report+"Original gameplay squid updated; navigation, colliders, authored placement and materials preserved. Separate old-model repair scene saved.\n");Debug.Log("SQUID_EMBEDDED_ROOTS_UNITY_PASSED\n"+report);
 }
}
}


