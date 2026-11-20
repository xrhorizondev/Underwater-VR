using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class CreatureReview {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 public static void Apply(){
  ReefBlenderSquid.Inspect();ReefBlenderSquid.Apply();
  var scene=EditorSceneManager.OpenScene(Scene);
  var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();var recurring=squid.GetComponent<RecurringSquid>();
  // Outside the swimming corridor, low over the trench floor. It is the same
  // animal on both passages, never a second spawn.
  squid.transform.SetPositionAndRotation(new Vector3(16,-48,132),Quaternion.Euler(0,90,0));squid.radius=3;squid.holdDepth=true;squid.PlaceAboveSeabed();
  recurring.trenchPosition=squid.transform.position;recurring.trenchRotation=squid.transform.rotation;
  recurring.openWaterPosition=new Vector3(16,-11,194);recurring.openWaterRotation=Quaternion.identity;
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){
   if(life.kind=="Anglerfish")life.grabbable=false;
   if(life.kind=="Manta") {var p=life.transform.position;p.z=8;life.transform.position=p;}
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var body=AssetDatabase.LoadAssetAtPath<Material>("Assets/UnderwaterArt/BlenderSquid/SquidBody.mat");body.SetFloat("_HeightStrength",.004f);EditorUtility.SetDirty(body);
  var skinImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/UnderwaterArt/BlenderSquid/SquidSkin.png");skinImporter.maxTextureSize=2048;var skinPlatform=skinImporter.GetPlatformTextureSettings("Android");skinPlatform.maxTextureSize=2048;skinImporter.SetPlatformTextureSettings(skinPlatform);skinImporter.SaveAndReimport();
  foreach(string directory in new[]{"LoggerheadTurtle","MantaRay","ReefShark"}){
   string folder="Assets/UnderwaterArt/PreparedMarine/"+directory;
   string normal=Directory.GetFiles(folder+"/textures","*.png").FirstOrDefault(p=>Path.GetFileName(p).ToLowerInvariant().Contains("normal"));
   if(normal==null)continue;normal=normal.Replace('\\','/');
   var importer=(TextureImporter)AssetImporter.GetAtPath(normal);importer.textureType=TextureImporterType.NormalMap;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.anisoLevel=2;
   var platform=importer.GetPlatformTextureSettings("Android");platform.overridden=true;platform.maxTextureSize=1024;platform.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
   foreach(string path in Directory.GetFiles(folder,"*.mat")){
    var mat=AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\','/'));
    if(mat.shader.name!="Standard"||path.ToLowerInvariant().Contains("eye"))continue;
    mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));mat.SetFloat("_BumpScale",.45f);mat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(mat);
   }
  }
  AssetDatabase.SaveAssets();Validate();
 }
 public static void Validate(){
  ReefBlenderSquid.Validate();ContactFitValidation.Run();
  EditorSceneManager.OpenScene(Scene);var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();var model=squid.transform.Find("Approved Blender squid").gameObject;var clip=model.GetComponent<Animation>().clip;var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
  var mesh=new Mesh();clip.SampleAnimation(model,0);skin.BakeMesh(mesh);var first=mesh.vertices;
  clip.SampleAnimation(model,clip.length);skin.BakeMesh(mesh);float seam=0;var last=mesh.vertices;for(int i=0;i<first.Length;i++)seam=Mathf.Max(seam,Vector3.Distance(first[i],last[i]));UnityEngine.Object.DestroyImmediate(mesh);
  if(seam>.01f)throw new Exception("Squid loop seam: "+seam);
  if(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Any(l=>l.kind=="Anglerfish"&&l.grabbable))throw new Exception("Anglerfish must respond to touch without grabbing");
  Debug.Log("CREATURE_REVIEW_VALIDATED squidLoopSeam="+seam+" singleDistantSquid=true anglerTouchOnly=true grabContactRegressions=true");
  Capture();Audit();
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/CreatureReview");var camera=Camera.main;camera.fieldOfView=55;
  var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();var model=squid.transform.Find("Approved Blender squid").gameObject;var clip=model.GetComponent<Animation>().clip;
  var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
  // Batch-mode camera renders can reuse the previous GPU skin buffer within
  // one editor frame. Render explicitly baked poses for phase inspection.
  var proxy=new GameObject("Sampled squid pose");proxy.transform.SetParent(skin.transform,false);var filter=proxy.AddComponent<MeshFilter>();proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
  for(int i=0;i<4;i++){clip.SampleAnimation(model,clip.length*i/4);var baked=new Mesh();skin.BakeMesh(baked,false);var v=baked.vertices;float scale=skin.rootBone.lossyScale.x;for(int j=0;j<v.Length;j++)v[j]/=scale;baked.vertices=v;baked.RecalculateBounds();filter.sharedMesh=baked;skin.enabled=false;camera.transform.position=squid.transform.TransformPoint(new Vector3(15,4,6));camera.transform.LookAt(squid.transform.TransformPoint(new Vector3(0,0,-3)));UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>().Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/CreatureReview/Squid_"+i+".png");skin.enabled=true;UnityEngine.Object.DestroyImmediate(baked);}
  UnityEngine.Object.DestroyImmediate(proxy);
 }
 public static void Audit(){
  EditorSceneManager.OpenScene(Scene);var report=new StringBuilder("Creature placement and animation audit\n");
  foreach(var group in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).GroupBy(l=>l.kind).OrderBy(g=>g.Key)){
   report.AppendLine(group.Key+" count="+group.Count());
   foreach(var life in group){
    int zone=Enumerable.Range(0,8).OrderBy(i=>Mathf.Abs(life.transform.position.z-OceanWorld.Centers[i].z)).First();
    report.AppendLine("  "+life.name+" position="+life.transform.position+" zone="+OceanWorld.Names[zone]+" grab="+life.grabbable+" observation="+life.observationOnly);
   }
  }
  foreach(var school in UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None))report.AppendLine("SCHOOL "+school.name+" center="+school.habitatCenter+" radius="+school.habitatRadius+" count="+school.GetComponentsInChildren<FishAgent>().Length);
  foreach(var animation in UnityEngine.Object.FindObjectsByType<Animation>(FindObjectsSortMode.None)){
   if(!animation.clip)continue;var bindings=AnimationUtility.GetCurveBindings(animation.clip);
   if(animation.GetComponentInParent<OpenWaterShark>()||animation.GetComponentInParent<SquidSwimmer>()){
    report.AppendLine("CLIP "+animation.name+" length="+animation.clip.length);
    foreach(var b in bindings.Where(b=>b.propertyName.Contains("Position"))) {var c=AnimationUtility.GetEditorCurve(animation.clip,b);float min=c.keys.Min(k=>k.value),max=c.keys.Max(k=>k.value);if(max-min>.001f)report.AppendLine(b.path+" "+b.propertyName+" range="+(max-min));}
    for(int i=0;i<=4;i++){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*i/4);foreach(var skin in animation.GetComponentsInChildren<SkinnedMeshRenderer>())report.AppendLine("  phase="+i+" skin="+skin.name+" bounds="+skin.bounds);}
   }
  }
  Directory.CreateDirectory("Documentation");File.WriteAllText("Documentation/CREATURE_AUDIT_2026_11_20.txt",report.ToString());Debug.Log("CREATURE_AUDIT_COMPLETE");
 }
}
}
