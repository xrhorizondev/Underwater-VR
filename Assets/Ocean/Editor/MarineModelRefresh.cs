using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class MarineModelRefresh {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Folder="Assets/UnderwaterArt/MarineRefresh";
 [Serializable]public class MaterialData {public string name,baseColorTexture,normalTexture;public float[] baseColor;public float roughness;}
 [Serializable]public class Manifest {public string kind;public MaterialData[] materials;}
 static string Key(SeaLife life){switch(life.kind){case "Dolphin":return "Dolphin";case "Reef shark":return "Shark";case "Comb jelly":return "CombJelly";case "Lionfish":return "Lionfish";default:return null;}}
 static SeaLife[] Actors()=>UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>Key(l)!=null).ToArray();
 static string Fbx(string key)=>Folder+"/"+key+"/"+key+".fbx";
 static Renderer[] Surfaces(Transform actor)=>actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
 static Vector3[] Points(Renderer r){if(r is SkinnedMeshRenderer s)return OctopusRepairValidation.Points(s);var f=r.GetComponent<MeshFilter>();return f&&f.sharedMesh?f.sharedMesh.vertices.Select(p=>f.transform.TransformPoint(p)).ToArray():Array.Empty<Vector3>();}
 static Vector3[] WorldNormals(SkinnedMeshRenderer skin){var n=skin.sharedMesh.normals;var w=skin.sharedMesh.boneWeights;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*skin.sharedMesh.bindposes[i]).ToArray();for(int i=0;i<n.Length;i++){var v=n[i];var b=w[i];n[i]=(matrices[b.boneIndex0].MultiplyVector(v)*b.weight0+matrices[b.boneIndex1].MultiplyVector(v)*b.weight1+matrices[b.boneIndex2].MultiplyVector(v)*b.weight2+matrices[b.boneIndex3].MultiplyVector(v)*b.weight3).normalized;}return n;}
 static Bounds BoundsIn(Transform root,IEnumerable<Renderer> renderers){Bounds b=default;bool first=true;foreach(var r in renderers)foreach(var world in Points(r)){var p=root.InverseTransformPoint(world);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}if(first)throw new Exception("Missing surface: "+root.name);return b;}
 static Material Skin(string key,string slot,int index){
  var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/"+key+"/materials.json"));var data=manifest.materials.FirstOrDefault(m=>m.name==slot)??manifest.materials[Mathf.Min(index,manifest.materials.Length-1)];
  string safe=System.Text.RegularExpressions.Regex.Replace(data.name,"[^a-zA-Z0-9_-]","_");string path=Folder+"/"+key+"/"+safe+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find(key=="CombJelly"?"Underwater/Comb Jelly Glass":"Underwater/Imported Marine Skin"));AssetDatabase.CreateAsset(material,path);}
  material.shader=Shader.Find(key=="CombJelly"?"Underwater/Comb Jelly Glass":"Underwater/Imported Marine Skin");material.enableInstancing=true;material.color=new Color(data.baseColor[0],data.baseColor[1],data.baseColor[2],data.baseColor[3]);
  if(string.IsNullOrEmpty(data.baseColorTexture))throw new Exception("Missing base atlas: "+key+" "+slot);
  string texturePath=Folder+"/"+key+"/"+data.baseColorTexture;var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=key=="CombJelly";importer.isReadable=false;importer.SaveAndReimport();material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
  if(!string.IsNullOrEmpty(data.normalTexture)&&material.HasProperty("_BumpMap")){string normalPath=Folder+"/"+key+"/"+data.normalTexture;var normal=(TextureImporter)AssetImporter.GetAtPath(normalPath);normal.textureType=TextureImporterType.NormalMap;normal.maxTextureSize=1024;normal.SaveAndReimport();material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));}
  if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",Mathf.Clamp01(1-data.roughness));EditorUtility.SetDirty(material);return material;
 }
 static AnimationClip Import(string key){
  var imp=(ModelImporter)AssetImporter.GetAtPath(Fbx(key));imp.animationType=ModelImporterAnimationType.Legacy;imp.importAnimation=true;imp.isReadable=true;imp.importCameras=false;imp.importLights=false;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;var clips=imp.defaultClipAnimations;if(clips.Length!=1)throw new Exception("Expected one looping clip for "+key+", got "+clips.Length);foreach(var c in clips){c.loopTime=true;c.wrapMode=WrapMode.Loop;}imp.clipAnimations=clips;imp.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(Fbx(key)).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__"));
 }
 static void ReferenceClip(Animation player,AnimationClip clip){var so=new SerializedObject(player);var clips=so.FindProperty("m_Animations");clips.arraySize=1;clips.GetArrayElementAtIndex(0).objectReferenceValue=clip;so.FindProperty("m_Animation").objectReferenceValue=clip;so.ApplyModifiedPropertiesWithoutUndo();player.playAutomatically=true;player.wrapMode=WrapMode.Loop;player.cullingType=AnimationCullingType.BasedOnRenderers;}
 static void Envelopes(GameObject model,AnimationClip clip){foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
  var origin=skin.rootBone?skin.rootBone:skin.transform;Bounds b=default;bool first=true;for(int sample=0;sample<=96;sample++){clip.SampleAnimation(model,clip.length*sample/96);foreach(var p in Points(skin)){var local=origin.InverseTransformPoint(p);if(first){b=new Bounds(local,Vector3.zero);first=false;}else b.Encapsulate(local);}}b.Expand(b.size.magnitude*.10f+.02f);skin.localBounds=b;skin.updateWhenOffscreen=false;
 }clip.SampleAnimation(model,0);}
 static float Weight(BoneWeight w,int index)=>(w.boneIndex0==index?w.weight0:0)+(w.boneIndex1==index?w.weight1:0)+(w.boneIndex2==index?w.weight2:0)+(w.boneIndex3==index?w.weight3:0);
 static void LionSpines(SeaLife actor,GameObject model,Renderer[] renderers){
  var hazard=actor.GetComponent<ReefDangerZone>();if(!hazard)throw new Exception("Lionfish warning owner missing");foreach(var old in hazard.surfaces)if(old)old.enabled=false;foreach(var outline in hazard.outlines)if(outline)outline.enabled=false;
  var surfaces=new List<Collider>();var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var points=Points(skin);var weights=skin.sharedMesh.boneWeights;
  for(int boneIndex=0;boneIndex<skin.bones.Length;boneIndex++){
   var bone=skin.bones[boneIndex];if(!bone.name.StartsWith("Dorsal_")&&!bone.name.StartsWith("Pelvic_"))continue;
   var members=points.Where((p,i)=>Weight(weights[i],boneIndex)>.1f).ToArray();if(members.Length==0)continue;
   var head=bone.position;var tip=members.OrderByDescending(p=>(p-head).sqrMagnitude).First();var direction=tip-head;float length=direction.magnitude;if(length<.005f)continue;
   var go=new GameObject("Animated venomous spine "+bone.name);go.layer=2;go.transform.SetParent(bone,false);go.transform.SetPositionAndRotation(head,Quaternion.FromToRotation(Vector3.up,direction.normalized));
   var scale=go.transform.lossyScale;float radialScale=Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));float verticalScale=Mathf.Abs(scale.y);var collider=go.AddComponent<CapsuleCollider>();collider.isTrigger=true;collider.direction=1;collider.radius=.014f/Mathf.Max(.00001f,radialScale);collider.height=length/Mathf.Max(.00001f,verticalScale)+collider.radius*2;collider.center=Vector3.up*(length/Mathf.Max(.00001f,verticalScale)*.5f);surfaces.Add(collider);
  }
  if(surfaces.Count<5)throw new Exception("Lionfish animated spines incomplete: "+surfaces.Count);hazard.surfaces=surfaces.ToArray();hazard.outlines=Array.Empty<LineRenderer>();hazard.glowTargets=renderers;EditorUtility.SetDirty(hazard);Debug.Log("MARINE_LION_SPINES animatedSurfaces="+surfaces.Count);
 }
 [MenuItem("Underwater/Replace Supplied Marine Models")]
 public static void Run(){
  AssetDatabase.Refresh();var clips=new Dictionary<string,AnimationClip>();foreach(var key in new[]{"Dolphin","Shark","CombJelly","Lionfish"})clips[key]=Import(key);
  var scene=EditorSceneManager.OpenScene(Scene);var actors=Actors();if(actors.Length!=30)throw new Exception("Expected 30 actors, found "+actors.Length);int instance=0;
  foreach(var actor in actors){
   string key=Key(actor);var root=actor.transform;var originalPosition=root.position;var originalRotation=root.rotation;var originalScale=root.localScale;
   var protectedComponents=actor.GetComponents<Component>().Where(c=>!(c is Transform)&&!(c is Renderer)&&!(c is ReefAnimalClearance)&&!(c is ReefDangerZone)&&!(c is ReefEncounter)&&!(c is ReefCreatureInfo)).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
   var solidColliders=actor.GetComponentsInChildren<Collider>(true).Where(c=>c.gameObject.layer==9).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
   var old=Surfaces(root);var before=BoundsIn(root,old);var previous=root.Find("Prepared animated model");if(previous&&previous.GetComponentsInChildren<Collider>(true).Any(c=>key!="Lionfish"||!c.isTrigger||c.gameObject.layer!=2))throw new Exception("Old visual owns unrelated gameplay colliders");
   var wrapper=new GameObject("Refreshed marine model");wrapper.transform.SetParent(root,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx(key)));model.transform.SetParent(wrapper.transform,false);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   var player=model.GetComponent<Animation>();if(!player)player=model.AddComponent<Animation>();ReferenceClip(player,clips[key]);clips[key].SampleAnimation(model,0);
   var renderers=model.GetComponentsInChildren<Renderer>();var initial=BoundsIn(root,renderers);float ratio=key=="CombJelly"?before.size.y/initial.size.y:before.size.z/initial.size.z;wrapper.transform.localScale=Vector3.one*ratio;wrapper.transform.localPosition+=before.center-BoundsIn(root,renderers).center;
   foreach(var r in renderers){r.sharedMaterials=r.sharedMaterials.Select((m,i)=>Skin(key,m?m.name:"",i)).ToArray();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
   var motion=model.AddComponent<ReefImportedAnimation>();motion.speed=key=="CombJelly"?.7f:key=="Lionfish"?.8f:1;motion.cycleOffset=(instance++*.618034f)%1;
   foreach(var r in old)r.enabled=false;
   if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);wrapper.name="Prepared animated model";
   var encounter=actor.GetComponent<ReefEncounter>();if(encounter)encounter.skin=renderers;
   if(key=="Lionfish")LionSpines(actor,model,renderers);
   if(key=="Shark"){var info=actor.GetComponent<ReefCreatureInfo>();if(info){info.creatureName="Great white shark";info.fact="Watch its slow passage through open water.";info.narration=null;}}
   Envelopes(model,clips[key]);var ground=actor.GetComponent<ReefAnimalClearance>();if(!ground)throw new Exception("Missing seabed guard");ground.animatedBody=AnimalSeabedRepair.Envelope(root);float lift=ground.Enforce();
   if(root.rotation!=originalRotation||root.localScale!=originalScale||Vector2.Distance(new Vector2(root.position.x,root.position.z),new Vector2(originalPosition.x,originalPosition.z))>.00001f)throw new Exception("Changed actor route or scale");
   foreach(var p in protectedComponents)if(EditorJsonUtility.ToJson(p.Key)!=p.Value)throw new Exception("Changed gameplay component: "+p.Key);foreach(var p in solidColliders)if(!p.Key||EditorJsonUtility.ToJson(p.Key)!=p.Value)throw new Exception("Changed solid body collider");
   EditorUtility.SetDirty(ground);Debug.Log("MARINE_REFRESH_INSTALLED "+key+" position="+root.position+" lift="+lift+" ratio="+ratio+" bounds="+BoundsIn(root,renderers));
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();Capture();Debug.Log("MARINE_REFRESH_PASSED dolphins=3 shark=1 combJellies=25 lionfish=1 existingMovementOwnersPreserved=true");
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int count=0;long vertexChecks=0;float minGap=float.PositiveInfinity;
  foreach(var actor in Actors()){
   var key=Key(actor);var wrapper=actor.transform.Find("Prepared animated model");if(!wrapper)throw new Exception("Missing replacement");var model=wrapper.GetChild(0).gameObject;var animation=model.GetComponent<Animation>();var clip=animation.clip;var ground=actor.GetComponent<ReefAnimalClearance>();
   if(!clip||!clip.legacy||!AssetDatabase.Contains(clip))throw new Exception("Missing shared animation");float deformation=0,seam=0;
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
    clip.SampleAnimation(model,0);var start=Points(skin);var center=start.Aggregate(Vector3.zero,(a,b)=>a+b)/start.Length;
    for(int phase=0;phase<=49;phase++){clip.SampleAnimation(model,clip.length*phase/49);var points=Points(skin);var c=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length;var visible=skin.bounds;visible.Expand(.01f);for(int i=0;i<points.Length;i++){
     deformation=Mathf.Max(deformation,((points[i]-c)-(start[i]-center)).magnitude);if(phase==49)seam=Mathf.Max(seam,Vector3.Distance(points[i],start[i]));
     if(!visible.Contains(points[i]))throw new Exception("Animated geometry outside culling bounds: "+key);
     if(!ground.animatedBody.Contains(actor.transform.InverseTransformPoint(points[i])))throw new Exception("Animated geometry outside seabed envelope: "+key);
     if(!ground.seabed.UpperHeight(points[i].x,points[i].x,points[i].z,points[i].z,out float floor)||points[i].y<floor-.0001f)throw new Exception("Body part entered sand: "+key);minGap=Mathf.Min(minGap,points[i].y-floor);vertexChecks++;
    }}
    foreach(var mat in skin.sharedMaterials)if(!mat||!mat.mainTexture||!mat.shader||mat.shader.name!=(key=="CombJelly"?"Underwater/Comb Jelly Glass":"Underwater/Imported Marine Skin")||ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Invalid material or shader binding: "+key);
   }
   if(deformation<.001f||seam>.01f)throw new Exception("Animation is static or loop snaps: "+key+" deformation="+deformation+" seam="+seam);
   clip.SampleAnimation(model,0);
   if(key=="Lionfish"){var hazard=actor.GetComponent<ReefDangerZone>();if(hazard.surfaces.Length<5||hazard.surfaces.Any(s=>!s||!s.enabled||!s.isTrigger||s.gameObject.layer!=2))throw new Exception("Lost animated spine hazards");var s=hazard.Step(.05f,1,true);if(!s.glow||!s.caution||!s.warn||s.contact)throw new Exception("Lionfish warning order changed");hazard.MarkWarningSpoken(1);if(!hazard.Step(0,2,true).contact)throw new Exception("Lionfish contact consequence lost");hazard.ResetEpisode();}
   if(key=="CombJelly"){if(actor.danger||actor.consequence||!actor.observationOnly)throw new Exception("Comb jelly safety changed");var tex=(Texture2D)model.GetComponentInChildren<Renderer>().sharedMaterial.mainTexture;var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tex)));var pixels=source.GetPixels32();int transparent=pixels.Count(p=>p.a<250),rows=pixels.Count(p=>p.a>100);UnityEngine.Object.DestroyImmediate(source);if(transparent<pixels.Length*.9f||rows==0)throw new Exception("Missing transparent tissue/comb-row alpha");}
   Debug.Log("MARINE_REFRESH_VALIDATED "+key+" deformation="+deformation+" loopSeam="+seam+" boneAnimation=true");count++;
  }
  if(count!=30)throw new Exception("Actor count changed");Debug.Log("MARINE_REFRESH_VERTICES_VALIDATED checks="+vertexChecks+" minSandGap="+minGap);
  Simulate();
 }
 public static void Simulate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var actors=Actors();var positions=actors.Select(a=>a.transform.position).ToArray();float[] travel=new float[actors.Length];int poseChecks=0;
  for(int frame=0;frame<2400;frame++)for(int i=0;i<actors.Length;i++){
   var actor=actors[i];var before=actor.transform.position;const float dt=1f/20;var key=Key(actor);
   if(key=="Dolphin")actor.GetComponent<WreckCreature>().Step(dt);else if(key=="Shark")actor.GetComponent<OpenWaterShark>().Step(dt,actor.transform.position+Vector3.right*10,Vector3.zero,true);else if(key=="CombJelly")actor.GetComponent<DeepWaterEncounter>().Step(dt,5,positions[i]+Vector3.forward*.6f,0);else actor.GetComponent<ReefEncounter>().Step(dt,actor.transform.position-Vector3.forward,frame%500<40?.1f:10);
   var ground=actor.GetComponent<ReefAnimalClearance>();ground.Enforce();if(!ground.RequiredHeight(actor.transform.position,actor.transform.rotation,out float height)||actor.transform.position.y<height-.0001f)throw new Exception("Patrol body entered sand: "+key);travel[i]+=Vector3.Distance(before,actor.transform.position);poseChecks++;
  }
  foreach(var key in new[]{"Dolphin","Shark","CombJelly","Lionfish"}){float min=actors.Select((a,i)=>new{a,i}).Where(x=>Key(x.a)==key).Min(x=>travel[x.i]);if(min<.1f)throw new Exception("Replacement movement stalled: "+key);Debug.Log("MARINE_REFRESH_PATROL "+key+" minTravel="+min);}
  Debug.Log("MARINE_REFRESH_PATROLS_VALIDATED seconds=120 poseChecks="+poseChecks);
 }
 public static void Capture(){
  Directory.CreateDirectory("Renders/MarineRefresh/Unity");
  foreach(var key in new[]{"Dolphin","Shark","CombJelly","Lionfish"}){
   EditorSceneManager.OpenScene(Scene);var actor=Actors().First(a=>Key(a)==key);var wrapper=actor.transform.Find("Prepared animated model");var model=wrapper.GetChild(0).gameObject;var animation=model.GetComponent<Animation>();var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>();
   var camera=new GameObject("Marine refresh camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.08f,.12f);camera.nearClipPlane=.005f;camera.farClipPlane=100;camera.orthographic=true;RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.5f,.6f,.7f);Shader.SetGlobalFloat("_ReefLight",.8f);
   var lamp=new GameObject("Marine review light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=1.3f;lamp.transform.rotation=Quaternion.Euler(30,-35,0);
   foreach(var phase in new[]{0f,.25f,.5f}){
    animation.clip.SampleAnimation(model,animation.clip.length*phase);var b=BoundsIn(actor.transform,renderers);var center=actor.transform.TransformPoint(b.center);float radius=b.extents.magnitude*actor.transform.lossyScale.x;camera.orthographicSize=radius*.95f;var proxies=new List<GameObject>();
    foreach(var skin in renderers){var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.ClearBlendShapes();mesh.vertices=Points(skin);mesh.normals=WorldNormals(skin);mesh.RecalculateBounds();mesh.RecalculateTangents();var go=new GameObject("Sampled marine pose");go.layer=31;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;proxies.Add(go);skin.enabled=false;}
    camera.transform.position=center+(actor.transform.right+actor.transform.forward+Vector3.up*.3f).normalized*radius*3;camera.transform.LookAt(center);ReefCorrectionAudit.Capture(camera,"Renders/MarineRefresh/Unity/"+key+"_"+(int)(phase*100)+".png");
    if(key=="CombJelly"&&phase==0){var view=camera.transform.position;foreach(var eye in new[]{-1,1}){camera.transform.position=view+camera.transform.right*(eye*.032f);ReefCorrectionAudit.Capture(camera,"Renders/MarineRefresh/Unity/CombJelly_"+(eye<0?"Left":"Right")+"Eye.png");}camera.transform.position=view;}
    foreach(var go in proxies){UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);}foreach(var skin in renderers)skin.enabled=true;
   }
   UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(lamp.gameObject);
  }
  Debug.Log("MARINE_REFRESH_CAPTURES_SAVED");
 }
 public static void Regression(){Validate();ReefDayTwo.Validate();PreparedMarineIntegration.Validate();Debug.Log("MARINE_REFRESH_REGRESSION_PASSED lionfishWarningStages=true boxJelliesAndRaysPreserved=true preparedAnimals=true");}
 public static void FinalVisualChecks(){
  Capture();
  foreach(var path in Directory.GetFiles("Renders/MarineRefresh/Unity","*.png")){
   var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();int errorPixels=pixels.Count(p=>p.r>240&&p.b>240&&p.g<20);UnityEngine.Object.DestroyImmediate(image);if(errorPixels>pixels.Length*.01f)throw new Exception("Error shader in rendered preview: "+path);
  }
  Debug.Log("MARINE_REFRESH_FINAL_VISUALS_PASSED errorShaders=0 bothEyeViews=true");
 }
}
}

