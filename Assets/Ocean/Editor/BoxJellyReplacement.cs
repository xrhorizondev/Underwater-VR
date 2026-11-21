using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class BoxJellyReplacement {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity", Folder="Assets/UnderwaterArt/ImportedBoxJelly", Model=Folder+"/BoxJelly.fbx";
 static string Pose(Transform t)=>EditorJsonUtility.ToJson(t);
 static Bounds BoundsIn(Transform root,Renderer[] renderers){
  bool first=true;Bounds bounds=default;
  // Renderer bounds may be stale after scaling, or deliberately enlarged for
  // animated culling. Fit the actual surface rather than that safety envelope.
  foreach(var r in renderers){
   var skin=r as SkinnedMeshRenderer;var filter=r.GetComponent<MeshFilter>();
   var points=skin?Points(skin):filter&&filter.sharedMesh?filter.sharedMesh.vertices.Select(p=>filter.transform.TransformPoint(p)).ToArray():Array.Empty<Vector3>();
   foreach(var world in points){var p=root.InverseTransformPoint(world);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
  }
  if(first)throw new Exception("No jelly renderers");return bounds;
 }
 static Vector3[] Points(SkinnedMeshRenderer skin){
  var mesh=skin.sharedMesh;var v=mesh.vertices;var weights=mesh.boneWeights;
  var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
  for(int i=0;i<v.Length;i++){var w=weights[i];var p=v[i];v[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return v;
 }
 static void ReferenceClip(Animation animation,AnimationClip clip){
  var serialized=new SerializedObject(animation);var clips=serialized.FindProperty("m_Animations");clips.arraySize=1;clips.GetArrayElementAtIndex(0).objectReferenceValue=clip;serialized.FindProperty("m_Animation").objectReferenceValue=clip;serialized.ApplyModifiedPropertiesWithoutUndo();
  animation.wrapMode=WrapMode.Loop;animation.playAutomatically=true;animation.cullingType=AnimationCullingType.BasedOnRenderers;
 }
 static Material MaterialFor(string name){
  string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
  material=new Material(Shader.Find("Underwater/Imported Box Jelly"));material.name=name;material.enableInstancing=true;
  bool tentacle=name.Contains("tentacle"),bell=name.Contains("bell"),canal=name.Contains("canal");
  material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/textures/"+(tentacle?"Tentacles":"Bell")+".png");
  material.color=bell?new Color(.994136f,.994136f,.994136f,.567046f):canal?new Color(.642415f,.642415f,.642415f,.177640f):Color.white;
  AssetDatabase.CreateAsset(material,path);return material;
 }
 [MenuItem("Underwater/Replace Box Jelly From Imported FBX")]
 public static void Run(){
  AssetDatabase.Refresh();
  foreach(var path in new[]{Folder+"/textures/Tentacles.png",Folder+"/textures/Bell.png"}){
   var texture=(TextureImporter)AssetImporter.GetAtPath(path);texture.maxTextureSize=1024;texture.alphaSource=TextureImporterAlphaSource.FromInput;texture.alphaIsTransparency=true;texture.mipmapEnabled=true;texture.SaveAndReimport();
  }
  var importer=(ModelImporter)AssetImporter.GetAtPath(Model);importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
  var clips=importer.defaultClipAnimations;if(clips.Length!=1)throw new Exception("Expected one source animation, found "+clips.Length);
  foreach(var c in clips){c.loopTime=true;c.wrapMode=WrapMode.Loop;}importer.clipAnimations=clips;importer.SaveAndReimport();
  var clip=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__"));
  if(clip.length<6||!clip.legacy)throw new Exception("Source animation not imported correctly");
  var scene=EditorSceneManager.OpenScene(Scene);var actors=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Box jelly").ToArray();if(actors.Length!=3)throw new Exception("Expected 3 box jellies, found "+actors.Length);
  foreach(var actor in actors){
   string rootPose=Pose(actor.transform);var protectedComponents=actor.GetComponents<Component>().Where(c=>!(c is Transform)&&!(c is ReefDangerZone)&&!(c is ReefEncounter)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
   var colliders=actor.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c)+Pose(c.transform));
   var old=actor.transform.Find("Prepared animated model");if(!old||old.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Unexpected visual ownership");
   var oldRenderers=old.GetComponentsInChildren<Renderer>();var oldBounds=BoundsIn(actor.transform,oldRenderers);
   var wrapper=new GameObject("Replacement box jelly visual");wrapper.transform.SetParent(actor.transform,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));model.transform.SetParent(wrapper.transform,false);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   var animation=model.GetComponent<Animation>();if(!animation)animation=model.AddComponent<Animation>();ReferenceClip(animation,clip);clip.SampleAnimation(model,0);
   var renderers=model.GetComponentsInChildren<Renderer>();var initial=BoundsIn(actor.transform,renderers);wrapper.transform.localScale=Vector3.one*(oldBounds.size.y/initial.size.y);wrapper.transform.localPosition+=oldBounds.center-BoundsIn(actor.transform,renderers).center;
   var fitted=BoundsIn(actor.transform,renderers);if(Mathf.Abs(fitted.size.y-oldBounds.size.y)>.001f||Vector3.Distance(fitted.center,oldBounds.center)>.001f)throw new Exception("Jelly visual fitting failed");
   foreach(var renderer in renderers){renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>MaterialFor(m.name)).ToArray();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
   var hazard=actor.GetComponent<ReefDangerZone>();if(hazard)hazard.glowTargets=renderers;
   var encounter=actor.GetComponent<ReefEncounter>();if(encounter)encounter.skin=encounter.skin.Where(r=>r&&!oldRenderers.Contains(r)).Concat(renderers).ToArray();
   // Sample the complete source cycle for conservative animated culling bounds.
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
    bool first=true;Bounds envelope=default;var origin=skin.rootBone?skin.rootBone:skin.transform;
    for(int sample=0;sample<=48;sample++){clip.SampleAnimation(model,clip.length*sample/48);foreach(var p in Points(skin)){var local=origin.InverseTransformPoint(p);if(first){envelope=new Bounds(local,Vector3.zero);first=false;}else envelope.Encapsulate(local);}}
    envelope.Expand(envelope.size.magnitude*.1f+.02f);skin.localBounds=envelope;skin.updateWhenOffscreen=false;
   }
   clip.SampleAnimation(model,0);
   foreach(var entry in protectedComponents)if(EditorJsonUtility.ToJson(entry.Key)!=entry.Value)throw new Exception("Changed gameplay component "+entry.Key);
   foreach(var entry in colliders)if(EditorJsonUtility.ToJson(entry.Key)+Pose(entry.Key.transform)!=entry.Value)throw new Exception("Changed collider "+entry.Key);
   if(Pose(actor.transform)!=rootPose)throw new Exception("Changed actor placement");
   UnityEngine.Object.DestroyImmediate(old.gameObject);wrapper.name="Prepared animated model";
   Debug.Log("BOX_JELLY_REPLACED "+actor.name+" oldBounds="+oldBounds+" newBounds="+BoundsIn(actor.transform,renderers)+" collidersPreserved="+colliders.Count);
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Validate();Capture();Debug.Log("BOX_JELLY_REPLACEMENT_PASSED actors="+actors.Length+" gameplayAndCollidersPreserved=true clip="+clip.name+" duration="+clip.length);
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);int count=0;
  foreach(var actor in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Box jelly")){
   var wrapper=actor.transform.Find("Prepared animated model");var animation=wrapper.GetComponentInChildren<Animation>();var model=animation.gameObject;var clip=animation.clip;
   if(!AssetDatabase.Contains(clip)||!clip.legacy||clip.length<6)throw new Exception("Invalid shared source clip");
   float deformation=0,seam=0;int triangles=0;
   foreach(var skin in wrapper.GetComponentsInChildren<SkinnedMeshRenderer>()){
    triangles+=skin.sharedMesh.triangles.Length/3;clip.SampleAnimation(model,0);var start=Points(skin);var center=start.Aggregate(Vector3.zero,(a,b)=>a+b)/start.Length;
    for(int sample=1;sample<=24;sample++){clip.SampleAnimation(model,clip.length*sample/24);var p=Points(skin);var c=p.Aggregate(Vector3.zero,(a,b)=>a+b)/p.Length;var visible=skin.bounds;visible.Expand(.02f);for(int i=0;i<p.Length;i++){deformation=Mathf.Max(deformation,((p[i]-c)-(start[i]-center)).magnitude);if(!visible.Contains(p[i]))throw new Exception("Animated vertex outside bounds");if(sample==24)seam=Mathf.Max(seam,Vector3.Distance(p[i],start[i]));}}
    foreach(var mat in skin.sharedMaterials)if(!mat||!mat.mainTexture||ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Invalid source material");
   }
   if(triangles!=3544||deformation<.001f)throw new Exception("Missing geometry or animation");
   var hazard=actor.GetComponent<ReefDangerZone>();if(!hazard||hazard.surfaces.Any(c=>!c)||hazard.glowTargets.Any(r=>!r))throw new Exception("Lost hazard references");
   var warning=hazard.Step(.05f,1,true);if(!warning.glow||!warning.caution||!warning.warn||warning.contact)throw new Exception("Warning order changed");hazard.MarkWarningSpoken(1);if(!hazard.Step(0,2,true).contact)throw new Exception("Contact consequence lost");hazard.ResetEpisode();
   clip.SampleAnimation(model,0);Debug.Log("BOX_JELLY_VALIDATED "+actor.name+" triangles="+triangles+" deformation="+deformation+" loopSeam="+seam);count++;
  }
  if(count!=3)throw new Exception("Box jelly count changed");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/BoxJellyReplacement");var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).First(a=>a.kind=="Box jelly");var visual=actor.transform.Find("Prepared animated model");
  foreach(var t in visual.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
  var camera=new GameObject("Box jelly review camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.09f,.13f);camera.nearClipPlane=.005f;camera.farClipPlane=100;camera.orthographic=true;
  var bounds=BoundsIn(actor.transform,visual.GetComponentsInChildren<Renderer>());var center=actor.transform.TransformPoint(bounds.center);float radius=bounds.extents.magnitude*actor.transform.lossyScale.x;camera.orthographicSize=radius*.9f;RenderSettings.fog=false;
  var animation=visual.GetComponentInChildren<Animation>();foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.forceMatrixRecalculationPerRender=true;skin.updateWhenOffscreen=true;}
  foreach(var phase in new[]{0f,.25f,.5f}){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*phase);camera.transform.position=center+new Vector3(1,.2f,1).normalized*radius*3;camera.transform.LookAt(center);ReefCorrectionAudit.Capture(camera,"Renders/BoxJellyReplacement/Unity_"+(int)(phase*100)+".png");}
  UnityEngine.Object.DestroyImmediate(camera.gameObject);
  // Capture changes are deliberately not saved.
 }
}
}


