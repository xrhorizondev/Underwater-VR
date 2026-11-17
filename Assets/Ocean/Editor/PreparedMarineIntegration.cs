using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class PreparedMarineIntegration {
 const string Folder="Assets/UnderwaterArt/PreparedMarine", Scene="Assets/Scenes/Underwater_Realistic.unity";
 static string Pose(Transform t)=>t.localPosition.ToString("F8")+t.localRotation.ToString("F8")+t.localScale.ToString("F8");
 static string Key(string kind){switch(kind){case "Turtle":return "LoggerheadTurtle";case "Manta":return "MantaRay";case "Reef shark":return "ReefShark";case "Octopus":return "Octopus";case "Moon jelly":return "MoonJelly";case "Box jelly":return "BoxJelly";default:return null;}}
 public static void Run(){
  AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene(Scene);
  var actors=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>Key(l.kind)!=null).ToArray();
  if(actors.Length!=19)throw new Exception("Expected 19 existing creature instances, found "+actors.Length);
  var protectedTypes=new[]{typeof(SeaLife),typeof(TurtleSwimmer),typeof(MarineSwimmer),typeof(ReefSphereBody),typeof(FishTouchBody),typeof(OpenWaterShark),typeof(ReefCreatureInfo),typeof(ReefJellyMotion)};
  var baseline=actors.SelectMany(a=>a.GetComponents<Component>().Where(c=>protectedTypes.Contains(c.GetType()))).ToDictionary(c=>c, c=>EditorJsonUtility.ToJson(c));
  var colliders=actors.SelectMany(a=>a.GetComponentsInChildren<Collider>(true)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c)+Pose(c.transform));
  var roots=actors.ToDictionary(a=>a,a=>Pose(a.transform));
  foreach(var actor in actors){
   if(actor.transform.Find("Prepared animated model"))throw new Exception("Already integrated: "+actor.name);
   string key=Key(actor.kind),directory=Folder+"/"+key;
   string path=Directory.GetFiles(directory,"*.fbx")[0].Replace('\\','/');
   var old=actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is LineRenderer)&&!(r is ParticleSystemRenderer)).ToArray();var oldBounds=Bounds(actor.transform,old);
   var wrapper=new GameObject("Prepared animated model");wrapper.transform.SetParent(actor.transform,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(wrapper.transform,false);
   var animation=model.GetComponent<Animation>();if(!animation)animation=model.AddComponent<Animation>();
   var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
   ReferenceClip(animation,clip);animation.wrapMode=WrapMode.Loop;animation.playAutomatically=true;animation.cullingType=AnimationCullingType.BasedOnRenderers;
   clip.SampleAnimation(model,0);
   wrapper.transform.localRotation=Quaternion.Euler(0,key=="LoggerheadTurtle"?180:key=="ReefShark"?-90:0,0);
   var renderers=model.GetComponentsInChildren<Renderer>();var initial=Bounds(actor.transform,renderers);
   float scale=key=="BoxJelly"?oldBounds.size.y/initial.size.y:key=="ReefShark"?oldBounds.size.z/initial.size.z:oldBounds.size.x/initial.size.x;
   wrapper.transform.localScale=Vector3.one*scale;
   var fitted=Bounds(actor.transform,renderers);wrapper.transform.localPosition+=oldBounds.center-fitted.center;
   foreach(var renderer in renderers){
    var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=Skin(key,slots[i]?slots[i].name:"Skin",renderer);
    renderer.sharedMaterials=slots;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
   }
   // Keep all old objects and references for existing controller/proxy ownership.
   foreach(var r in old)r.enabled=false;
   var encounter=actor.GetComponent<ReefEncounter>();if(encounter){encounter.skin=encounter.skin.Concat(renderers).Distinct().ToArray();EditorUtility.SetDirty(encounter);}
   var hazard=actor.GetComponent<ReefDangerZone>();if(hazard){hazard.glowTargets=renderers;EditorUtility.SetDirty(hazard);}
   if(key=="LoggerheadTurtle"){
    var adapter=wrapper.AddComponent<PreparedTurtleHead>();adapter.actor=actor.transform;adapter.control=renderers[0];adapter.head=model.GetComponentsInChildren<Transform>().First(t=>t.name=="head.002");
   }
   SetEnvelope(model,animation,actor.transform);
   Debug.Log("MARINE_INSTALLED "+actor.kind+" "+actor.name+" bounds="+Bounds(actor.transform,renderers));
  }
  foreach(var entry in baseline)if(EditorJsonUtility.ToJson(entry.Key)!=entry.Value)throw new Exception("Gameplay settings changed: "+entry.Key);
  foreach(var entry in colliders)if(EditorJsonUtility.ToJson(entry.Key)+Pose(entry.Key.transform)!=entry.Value)throw new Exception("Existing collider changed: "+entry.Key.name);
  foreach(var entry in roots)if(Pose(entry.Key.transform)!=entry.Value)throw new Exception("Actor placement changed: "+entry.Key.name);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Validate();Capture();
  Debug.Log("MARINE_INTEGRATION_PASSED actors=19 existingGameplaySettingsAndCollidersUnchanged=true");
 }
 static Material Skin(string key,string slot,Renderer renderer){
  string directory=Folder+"/"+key,path=directory+"/"+slot+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
  bool eyes=slot.ToLower().Contains("eye"),jelly=key.Contains("Jelly");
  material=new Material(Shader.Find(key=="Octopus"?"Underwater/Octopus Skin":key=="LoggerheadTurtle"&&eyes?"Underwater/Prepared Turtle Eyes":jelly?"Underwater/Prepared Jelly":"Standard"));material.color=eyes&&key=="ReefShark"?new Color(.008f,.012f,.016f):Color.white;
  string texture=Directory.GetFiles(directory+"/textures","*.png").FirstOrDefault(p=>p.ToLower().Contains("albedo")||p.ToLower().Contains("basecolor"));
  if(texture!=null&&!(eyes&&key=="ReefShark"))material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture.Replace('\\','/'));
  if(key=="Octopus")material.SetFloat("_PreparedMesh",1);
  if(key=="LoggerheadTurtle"&&eyes){var skin=(SkinnedMeshRenderer)renderer;material.SetFloat("_EyeY",skin.sharedMesh.vertices.Average(v=>v.y));}
  if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",eyes?.6f:.25f);
  material.enableInstancing=true;AssetDatabase.CreateAsset(material,path);return material;
 }
 static Vector3[] Points(SkinnedMeshRenderer skin){
  var mesh=skin.sharedMesh;var v=mesh.vertices;var delta=new Vector3[v.Length];
  for(int shape=0;shape<mesh.blendShapeCount;shape++){float w=skin.GetBlendShapeWeight(shape)/100;if(Mathf.Abs(w)<.00001f)continue;mesh.GetBlendShapeFrameVertices(shape,mesh.GetBlendShapeFrameCount(shape)-1,delta,null,null);for(int i=0;i<v.Length;i++)v[i]+=delta[i]*w;}
  var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var weights=mesh.boneWeights;
  for(int i=0;i<v.Length;i++){var w=weights[i];var p=v[i];v[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return v;
 }
 static void SetEnvelope(GameObject model,Animation animation,Transform actor){
  var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();var envelopes=new Bounds[skins.Length];bool first=true;
  for(int sample=0;sample<=32;sample++){animation.clip.SampleAnimation(model,animation.clip.length*sample/32);for(int i=0;i<skins.Length;i++)foreach(var p in Points(skins[i])){var local=(skins[i].rootBone?skins[i].rootBone:skins[i].transform).InverseTransformPoint(p);if(first){envelopes[i]=new Bounds(local,Vector3.zero);first=false;}else envelopes[i].Encapsulate(local);} }
  for(int i=0;i<skins.Length;i++){var b=envelopes[i];b.Expand(b.size.magnitude*.12f+.02f);skins[i].localBounds=b;skins[i].updateWhenOffscreen=false;}animation.clip.SampleAnimation(model,0);
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int count=0;
  foreach(var actor in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>Key(l.kind)!=null)){
   var wrapper=actor.transform.Find("Prepared animated model");if(!wrapper)throw new Exception("Missing visual: "+actor.name);var model=wrapper.GetChild(0).gameObject;var animation=model.GetComponent<Animation>();if(!animation||!animation.clip)throw new Exception("Missing clip");
   float deformation=0;foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
    animation.clip.SampleAnimation(model,0);var start=Points(skin);var center=start.Aggregate(Vector3.zero,(a,b)=>a+b)/start.Length;
    for(int sample=1;sample<=13;sample++){animation.clip.SampleAnimation(model,animation.clip.length*sample/31);var p=Points(skin);var c=p.Aggregate(Vector3.zero,(a,b)=>a+b)/p.Length;var visible=skin.bounds;visible.Expand(.02f);for(int i=0;i<p.Length;i++){deformation=Mathf.Max(deformation,((p[i]-c)-(start[i]-center)).magnitude);if(!visible.Contains(p[i]))throw new Exception("Culling bounds exclude animated vertex: "+actor.name+" "+skin.name+" bounds="+visible+" point="+p[i]);}}
    foreach(var mat in skin.sharedMaterials)if(!mat||!mat.shader||ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Invalid material");
   }
   if(deformation<.0001f)throw new Exception("No skin deformation: "+actor.name);
   var adapter=wrapper.GetComponent<PreparedTurtleHead>();if(adapter){
    var properties=new MaterialPropertyBlock();adapter.control.GetPropertyBlock(properties);var original=new MaterialPropertyBlock();adapter.control.GetPropertyBlock(original);var rotation=adapter.head.localRotation;
    properties.SetFloat("_HeadTurn",.2f);adapter.control.SetPropertyBlock(properties);adapter.Apply();var once=adapter.head.localRotation;adapter.Apply();if(Quaternion.Angle(rotation,once)<1||Quaternion.Angle(once,adapter.head.localRotation)>.01f)throw new Exception("Head response missing or accumulates while animation is culled");
    adapter.head.localRotation=rotation;adapter.control.SetPropertyBlock(original);
    if(!wrapper.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial.shader.name=="Underwater/Prepared Turtle Eyes"&&r.sharedMaterial.HasProperty("_Blink")))throw new Exception("Blink material missing");
   }
   var zone=actor.GetComponent<ReefDangerZone>();if(zone){if(zone.surfaces.Any(c=>!c))throw new Exception("Lost hazard surface");var s=zone.Step(.05f,1,true);if(!s.glow||!s.caution||!s.warn)throw new Exception("Hazard warning lost");zone.MarkWarningSpoken(1);var contact=zone.Step(0,2,true);if(contact.contact!=(actor.kind=="Box jelly"))throw new Exception("Hazard consequence changed");zone.ResetEpisode();}
   var encounter=actor.GetComponent<ReefEncounter>();if(encounter){encounter.Initialize();encounter.Step(.05f,actor.transform.position+Vector3.right,.1f);if(encounter.Reaction<.9f)throw new Exception("Octopus reaction lost");var r=wrapper.GetComponentInChildren<Renderer>();var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);if(block.GetColor("_Color").r<.5f)throw new Exception("Octopus tint not reaching new model");}
   Debug.Log("MARINE_VALIDATED "+actor.kind+" deform="+deformation+" clip="+animation.clip.length);count++;
  }
  if(count!=19)throw new Exception("Instance count changed");
 }
 public static void Regression(){Validate();ReefDayTwo.Validate();ReefDayThree.Validate();ReefDaySix.Validate();Debug.Log("MARINE_REGRESSIONS_PASSED headBlinkBridge=true hazards=true encounters=true navigation=true daySix=true");}
 public static bool ValidatePreparedTurtleEyes(TurtleSwimmer turtle){
  var adapter=turtle.GetComponentInChildren<PreparedTurtleHead>();if(!adapter)return false;
  var eyes=adapter.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(s=>s.name=="LoggerheadTurtle_Eyes");
  if(!eyes||!eyes.enabled||!adapter.head||eyes.sharedMesh.triangles.Length==0)throw new Exception("Missing prepared turtle eyes");
  var points=Points(eyes).Select(p=>turtle.transform.InverseTransformPoint(p)).ToArray();
  float center=turtle.transform.InverseTransformPoint(adapter.head.position).x;
  if(points.Count(p=>p.x<center-.005f)<20||points.Count(p=>p.x>center+.005f)<20)throw new Exception("Prepared turtle eye coverage must include both sides");return true;
 }
 public static void FinishRegression(){ReefDaySix.Validate();Debug.Log("MARINE_FINAL_REGRESSIONS_PASSED sourceRigEyeGeometryBothSides=true daySixAndEarlierHabitatChecks=true");}
 public static void FinishVisualChecks(){ReefRealismPass.Validate();ReefCreaturePolish.Validate();Debug.Log("MARINE_FINAL_REGRESSIONS_PASSED sourceRigEyeGeometryBothSides=true finalGraphicsCapture=true priorBehaviorChecksPassed=true");}
 static void ReferenceClip(Animation animation,AnimationClip clip){
  // Animation.AddClip clones the clip into the scene. Keep the imported asset reference.
  var serialized=new SerializedObject(animation);var clips=serialized.FindProperty("m_Animations");clips.arraySize=1;clips.GetArrayElementAtIndex(0).objectReferenceValue=clip;serialized.FindProperty("m_Animation").objectReferenceValue=clip;serialized.ApplyModifiedPropertiesWithoutUndo();
 }
 public static void ShareClips(){
  var scene=EditorSceneManager.OpenScene(Scene);int count=0;
  foreach(var actor in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>Key(l.kind)!=null)){
   var model=actor.transform.Find("Prepared animated model").GetChild(0).gameObject;var animation=model.GetComponent<Animation>();var obsolete=animation.Cast<AnimationState>().Select(s=>s.clip).Where(c=>!AssetDatabase.Contains(c)).Distinct().ToArray();
   string path=Directory.GetFiles(Folder+"/"+Key(actor.kind),"*.fbx")[0].Replace('\\','/');var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));ReferenceClip(animation,clip);foreach(var old in obsolete)UnityEngine.Object.DestroyImmediate(old);count++;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(new FileInfo(Scene).Length>40000000)throw new Exception("Scene still contains excessive embedded data");Validate();ReefRealismPass.Validate();Debug.Log("MARINE_SHARED_CLIPS_PASSED instances="+count+" sceneBytes="+new FileInfo(Scene).Length);
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/PreparedMarineUnity");var seen=new System.Collections.Generic.HashSet<string>();
  foreach(var actor in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>Key(l.kind)!=null)){
   string key=Key(actor.kind);if(!seen.Add(key))continue;var visual=actor.transform.Find("Prepared animated model");foreach(var t in visual.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
   var animation=visual.GetComponentInChildren<Animation>();animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*.13f);
   var bounds=Bounds(actor.transform,visual.GetComponentsInChildren<Renderer>());var center=actor.transform.TransformPoint(bounds.center);float radius=bounds.extents.magnitude*actor.transform.lossyScale.x;
   var camObject=new GameObject("Marine review camera");var camera=camObject.AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.08f,.11f);camera.fieldOfView=38;camera.nearClipPlane=.01f;camera.farClipPlane=100;
   camera.transform.position=center+new Vector3(1,.6f,1).normalized*radius*3.5f;camera.transform.LookAt(center);
   var lamp=new GameObject("Marine review light");var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.cullingMask=1<<31;lamp.transform.rotation=Quaternion.Euler(35,-25,0);RenderSettings.fog=false;RenderSettings.ambientLight=Color.gray;
   var target=new RenderTexture(900,700,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(900,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,700),0,0);image.Apply();File.WriteAllBytes("Renders/PreparedMarineUnity/"+key+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camObject);UnityEngine.Object.DestroyImmediate(lamp);foreach(var t in visual.GetComponentsInChildren<Transform>())t.gameObject.layer=0;
  }
 }
 public static void Inspect(){
  AssetDatabase.Refresh();
  foreach(var path in Directory.GetFiles(Folder,"*.fbx",SearchOption.AllDirectories)){
   var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Legacy;imp.importAnimation=true;imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;imp.animationCompression=ModelImporterAnimationCompression.Off;
   var clips=imp.defaultClipAnimations;foreach(var c in clips){c.loopTime=true;c.wrapMode=WrapMode.Loop;}imp.clipAnimations=clips;imp.SaveAndReimport();
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   Debug.Log("MARINE_MODEL "+path+" bounds="+Bounds(model.transform,model.GetComponentsInChildren<Renderer>())+" clips="+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Select(c=>c.name)));
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())Debug.Log("MARINE_SKIN "+skin.name+" mats="+string.Join(",",skin.sharedMaterials.Select(m=>m?m.name:"NULL"))+" bones="+string.Join(",",skin.bones.Select(b=>b.name)));
   foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name.ToLower().Contains("head")||t.name.ToLower().Contains("eye"))Debug.Log("MARINE_HEAD "+t.name+" pos="+model.transform.InverseTransformPoint(t.position));
   UnityEngine.Object.DestroyImmediate(model);
  }
  EditorSceneManager.OpenScene(Scene);
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(new[]{"Turtle","Manta","Octopus","Reef shark","Moon jelly","Box jelly"}.Contains(life.kind))Debug.Log("MARINE_ACTOR "+life.kind+" "+life.name+" bounds="+Bounds(life.transform,life.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is LineRenderer)).ToArray())+" components="+string.Join(",",life.GetComponents<Component>().Select(c=>c.GetType().Name)));
 }
 static Bounds Bounds(Transform root,Renderer[] renderers){bool first=true;var b=new Bounds();foreach(var r in renderers){if(r is LineRenderer||r is ParticleSystemRenderer)continue;var filter=r.GetComponent<MeshFilter>();var mesh=filter?filter.sharedMesh:null;if(mesh){foreach(var vertex in mesh.vertices){var point=root.InverseTransformPoint(r.transform.TransformPoint(vertex));if(first){b=new Bounds(point,Vector3.zero);first=false;}else b.Encapsulate(point);}continue;}Bounds rb=mesh?mesh.bounds:r is SkinnedMeshRenderer s?s.sharedMesh.bounds:r.localBounds;for(int i=0;i<8;i++){var p=rb.center+Vector3.Scale(rb.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));p=root.InverseTransformPoint(r.transform.TransformPoint(p));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
}
}
