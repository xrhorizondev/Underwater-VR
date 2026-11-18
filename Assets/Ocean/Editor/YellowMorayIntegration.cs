using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class YellowMorayIntegration {
 const string Folder="Assets/UnderwaterArt/YellowMoray",Scene="Assets/Scenes/Underwater_Realistic.unity",Fbx=Folder+"/YellowMoray.fbx";
 static Vector3[] SkinPoints(SkinnedMeshRenderer skin){
  var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var matrices=skin.bones.Select((bone,i)=>bone.localToWorldMatrix*mesh.bindposes[i]).ToArray();
  for(int i=0;i<vertices.Length;i++){var w=weights[i];var p=vertices[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return vertices;
 }
 static Bounds Bounds(Transform root,Renderer[] renderers){
  bool first=true;var bounds=new Bounds();
  foreach(var renderer in renderers){Vector3[] vertices;if(renderer is SkinnedMeshRenderer skin)vertices=SkinPoints(skin);else{var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;vertices=filter.sharedMesh.vertices.Select(v=>renderer.transform.TransformPoint(v)).ToArray();}
   foreach(var vertex in vertices){var p=root.InverseTransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
  }return bounds;
 }

 static void AlignAndFit(WreckCreature actor,Transform wrapper,Animation player){
  var old=actor.GetComponentsInChildren<Renderer>(true).Where(r=>!r.transform.IsChildOf(wrapper)).ToArray();var oldBounds=Bounds(actor.transform,old);
  wrapper.localPosition=Vector3.zero;wrapper.localRotation=Quaternion.identity;wrapper.localScale=Vector3.one;
  player.Cast<AnimationState>().Select(s=>s.clip).Single(c=>c.name.EndsWith("Idle")).SampleAnimation(player.gameObject,0);
  var bones=player.GetComponentsInChildren<Transform>();var head=bones.Single(t=>t.name=="Head");var tail=bones.Single(t=>t.name=="Spine11");var jaw=bones.Single(t=>t.name=="Jaw");
  var forward=wrapper.InverseTransformPoint(head.position)-wrapper.InverseTransformPoint(tail.position);var up=Vector3.ProjectOnPlane(wrapper.InverseTransformPoint(head.position)-wrapper.InverseTransformPoint(jaw.position),forward).normalized;
  wrapper.localRotation=Quaternion.Inverse(Quaternion.LookRotation(forward,up));var renderers=player.GetComponentsInChildren<Renderer>();var actual=Bounds(actor.transform,renderers);
  for(int fit=0;fit<8;fit++){actual=Bounds(actor.transform,renderers);float ratio=oldBounds.size.z/actual.size.z;if(Mathf.Abs(1-ratio)<.005f)break;wrapper.localScale*=Mathf.Sqrt(ratio);}
  actual=Bounds(actor.transform,renderers);if(Mathf.Abs(actual.size.z-oldBounds.size.z)>.03f)throw new Exception("Actual moray scale did not converge");wrapper.localPosition+=oldBounds.center-actual.center;
  Debug.Log("YELLOW_MORAY_FITTED old="+oldBounds+" new="+Bounds(actor.transform,renderers)+" visualScale="+wrapper.localScale);
 }
 static void SavePrefab(WreckCreature actor){var clone=UnityEngine.Object.Instantiate(actor.gameObject);clone.name="Yellow moray encounter";clone.transform.position=Vector3.zero;PrefabUtility.SaveAsPrefabAsset(clone,Folder+"/YellowMorayEncounter.prefab");UnityEngine.Object.DestroyImmediate(clone);}

 public static void Run(){
  AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath(Fbx);importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.isReadable=true;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.materialImportMode=ModelImporterMaterialImportMode.None;
  var takes=importer.defaultClipAnimations;foreach(var take in takes){take.loopTime=!take.name.EndsWith("React")&&!take.name.EndsWith("Recover");take.wrapMode=take.loopTime?WrapMode.Loop:WrapMode.ClampForever;}importer.clipAnimations=takes;importer.SaveAndReimport();
  var normalImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/MorayNormal.png");normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();
  var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/YellowMoray.mat");if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Folder+"/YellowMoray.mat");}
  material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/MoraySkin.png");material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/MorayNormal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.25f);material.SetFloat("_Glossiness",.35f);material.enableInstancing=true;
  var scene=EditorSceneManager.OpenScene(Scene);var actor=UnityEngine.Object.FindObjectsByType<WreckCreature>(FindObjectsSortMode.None).Single(a=>a.species==WreckCreature.Species.Moray);
  var before=EditorJsonUtility.ToJson(actor);var pose=actor.transform.localToWorldMatrix;var colliders=actor.GetComponentsInChildren<Collider>().ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
  var old=actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var oldBounds=Bounds(actor.transform,old);
  if(actor.transform.Find("Yellow moray animated visual"))throw new Exception("Already installed");var wrapper=new GameObject("Yellow moray animated visual").transform;wrapper.SetParent(actor.transform,false);
  var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));model.transform.SetParent(wrapper,false);
  var bones=model.GetComponentsInChildren<Transform>();var head=bones.Single(b=>b.name=="Head");var tail=bones.Single(b=>b.name=="Spine11");var jaw=bones.Single(b=>b.name=="Jaw");
  var forward=wrapper.InverseTransformPoint(head.position)-wrapper.InverseTransformPoint(tail.position);var up=Vector3.ProjectOnPlane(wrapper.InverseTransformPoint(head.position)-wrapper.InverseTransformPoint(jaw.position),forward).normalized;
  wrapper.localRotation=Quaternion.Inverse(Quaternion.LookRotation(forward.normalized,up));
  var renderers=model.GetComponentsInChildren<Renderer>();foreach(var r in renderers){r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
  var bounds=Bounds(actor.transform,renderers);float scale=oldBounds.size.z/bounds.size.z;wrapper.localScale=Vector3.one*scale;bounds=Bounds(actor.transform,renderers);wrapper.localPosition+=oldBounds.center-bounds.center;
  var clips=AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();if(clips.Length!=5)throw new Exception("Expected five animation clips");
  var player=model.GetComponent<Animation>();if(!player)player=model.AddComponent<Animation>();var serialized=new SerializedObject(player);var entries=serialized.FindProperty("m_Animations");entries.arraySize=clips.Length;for(int i=0;i<clips.Length;i++)entries.GetArrayElementAtIndex(i).objectReferenceValue=clips[i];serialized.FindProperty("m_Animation").objectReferenceValue=clips.Single(c=>c.name.EndsWith("Idle"));serialized.ApplyModifiedPropertiesWithoutUndo();player.playAutomatically=true;player.cullingType=AnimationCullingType.BasedOnRenderers;
  AlignAndFit(actor,wrapper,player);FitBounds(player);
  clips.Single(c=>c.name.EndsWith("Idle")).SampleAnimation(model,0);var motion=wrapper.gameObject.AddComponent<YellowMorayMotion>();motion.actor=actor;motion.animationPlayer=player;
  foreach(var r in old)r.enabled=false;
  if(EditorJsonUtility.ToJson(actor)!=before||actor.transform.localToWorldMatrix!=pose||colliders.Any(p=>EditorJsonUtility.ToJson(p.Key)!=p.Value))throw new Exception("Existing moray behavior or collider settings changed");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  var prefab=UnityEngine.Object.Instantiate(actor.gameObject);prefab.name="Yellow moray encounter";prefab.transform.position=Vector3.zero;PrefabUtility.SaveAsPrefabAsset(prefab,Folder+"/YellowMorayEncounter.prefab");UnityEngine.Object.DestroyImmediate(prefab);
  Validate();Capture();Debug.Log("YELLOW_MORAY_INSTALLED existingPlacementAndGameplayPreserved=true clips=5");
 }
 static void FitBounds(Animation player){
  var clips=player.Cast<AnimationState>().Select(s=>s.clip).ToArray();
  foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>()){
   skin.updateWhenOffscreen=false;var envelope=new Bounds();bool first=true;var space=skin.rootBone?skin.rootBone:skin.transform;
   foreach(var clip in clips)for(int i=0;i<=24;i++){clip.SampleAnimation(player.gameObject,clip.length*i/24);foreach(var vertex in SkinPoints(skin)){var p=space.InverseTransformPoint(vertex);if(first){envelope=new Bounds(p,Vector3.zero);first=false;}else envelope.Encapsulate(p);}}
   envelope.Expand(envelope.size.magnitude*.08f);skin.localBounds=envelope;
  }
 }
 public static void Refine(){var scene=EditorSceneManager.OpenScene(Scene);var motion=UnityEngine.Object.FindFirstObjectByType<YellowMorayMotion>();AlignAndFit(motion.actor,motion.transform,motion.animationPlayer);FitBounds(motion.animationPlayer);motion.Clip("Idle").SampleAnimation(motion.animationPlayer.gameObject,0);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);SavePrefab(motion.actor);Validate();Capture();}
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);var motion=UnityEngine.Object.FindFirstObjectByType<YellowMorayMotion>();if(!motion||!motion.actor||motion.actor.species!=WreckCreature.Species.Moray)throw new Exception("Missing moray bridge");
  var player=motion.animationPlayer;foreach(var name in new[]{"Idle","Swim","React","Retreat","Recover"}){var clip=motion.Clip(name);if(clip.length<1)throw new Exception("Clip too short");var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();var a=new Mesh();var b=new Mesh();clip.SampleAnimation(player.gameObject,0);skin.BakeMesh(a);clip.SampleAnimation(player.gameObject,clip.length*.45f);skin.BakeMesh(b);float delta=0;var av=a.vertices;var bv=b.vertices;for(int i=0;i<av.Length;i++)delta=Mathf.Max(delta,Vector3.Distance(av[i],bv[i]));UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);if(delta<.0001f)throw new Exception("Nondeforming clip "+name);Debug.Log("YELLOW_MORAY_CLIP_VALIDATED "+name+" deformation="+delta);}
  var actor=motion.actor;actor.Initialize();var home=actor.transform.position;actor.React();bool retreat=false,recover=false,react=false;for(int i=0;i<300;i++){actor.Step(1f/30);motion.Step(1f/30);react|=motion.CurrentState=="React";retreat|=motion.CurrentState=="Retreat";recover|=motion.CurrentState=="Recover";}if(!react||!retreat||!recover||Vector3.Distance(home,actor.transform.position)>.08f)throw new Exception("Moray clip/movement state transitions failed");
  ReefDayFour.Validate();Debug.Log("YELLOW_MORAY_VALIDATED movementAndRecovery=true previousWreckAndHabitatChecks=true");
 }
 public static void Capture(){EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/YellowMorayUnity");var motion=UnityEngine.Object.FindFirstObjectByType<YellowMorayMotion>();var camera=Camera.main;motion.Clip("Idle").SampleAnimation(motion.animationPlayer.gameObject,.5f);var bounds=Bounds(motion.actor.transform,motion.GetComponentsInChildren<Renderer>());var center=motion.actor.transform.TransformPoint(bounds.center);Debug.Log("YELLOW_MORAY_WORLD_BOUNDS "+bounds+" actor="+motion.actor.transform.position);camera.transform.position=center+motion.actor.transform.forward*2.3f+motion.actor.transform.right*1.0f+Vector3.up*.65f;camera.transform.LookAt(center);camera.fieldOfView=45;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/YellowMorayUnity/WreckMoray.png");}
 public static void FinalChecks(){
  Validate();EditorSceneManager.OpenScene(Scene);var motion=UnityEngine.Object.FindFirstObjectByType<YellowMorayMotion>();var player=motion.animationPlayer;var skins=player.GetComponentsInChildren<SkinnedMeshRenderer>();int triangles=0;
  foreach(var skin in skins){triangles+=skin.sharedMesh.triangles.Length/3;if(skin.sharedMaterials.Length!=1||!skin.sharedMaterial.mainTexture||ShaderUtil.ShaderHasError(skin.sharedMaterial.shader))throw new Exception("Moray material/texture invalid");
   foreach(var clip in player.Cast<AnimationState>().Select(s=>s.clip))for(int i=0;i<=24;i++){clip.SampleAnimation(player.gameObject,clip.length*i/24);var visible=skin.bounds;visible.Expand(.005f);foreach(var point in SkinPoints(skin))if(!visible.Contains(point))throw new Exception("Moray animated vertex outside visibility bounds: "+clip.name);}
  }
  motion.Clip("Idle").SampleAnimation(player.gameObject,0);var bounds=Bounds(motion.actor.transform,skins);if(bounds.size.z<2||bounds.size.z>2.8f||triangles>5000)throw new Exception("Moray scene scale or triangle budget invalid");
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/YellowMorayEncounter.prefab");if(!prefab||!prefab.GetComponentInChildren<YellowMorayMotion>())throw new Exception("Missing reusable encounter prefab");
  Debug.Log("YELLOW_MORAY_FINAL_PASSED triangles="+triangles+" length="+bounds.size.z+" clips=5 allSampledVerticesInBounds=true materials=1 prefab=true");Capture();
 }
 public static void RefreshRealism(){
  AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath(Fbx);var takes=importer.defaultClipAnimations;
  foreach(var take in takes){take.loopTime=!take.name.EndsWith("React")&&!take.name.EndsWith("Recover");take.wrapMode=take.loopTime?WrapMode.Loop:WrapMode.ClampForever;}importer.clipAnimations=takes;importer.SaveAndReimport();
  var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/YellowMoray.mat");material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/MoraySurface.png"));material.EnableKeyword("_METALLICGLOSSMAP");material.SetFloat("_GlossMapScale",1);material.SetFloat("_Metallic",0);material.SetFloat("_BumpScale",.4f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
  Refine();FinalChecks();
 }
}
}
