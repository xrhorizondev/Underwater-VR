using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
namespace Underwater.Editor {
[BuildCallbackVersion(11)]
public class QuestReefBuild : IProcessSceneWithReport {
 public int callbackOrder=>100;
 public void OnProcessScene(Scene scene,BuildReport report){if(report!=null&&report.summary.platform==BuildTarget.Android)OptimizeScene();}
 [MenuItem("Underwater/Quest 3/Configure Android optimization")]
 public static void Configure(){
  OceanBuild.ConfigureXR();
  Directory.CreateDirectory("Assets/Ocean/Resources");if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Ocean/Resources/QuestRuntimeStandard.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Ocean/Resources/QuestRuntimeStandard.mat");
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var included=graphics.FindProperty("m_AlwaysIncludedShaders");for(int i=included.arraySize-1;i>=0;i--){var sh=included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;if(sh&&sh.name=="Standard"){included.GetArrayElementAtIndex(i).objectReferenceValue=null;included.DeleteArrayElementAtIndex(i);}}graphics.ApplyModifiedProperties();
  PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});PlayerSettings.colorSpace=ColorSpace.Linear;
  var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);if(!xr)throw new Exception("Missing Android OpenXR settings");xr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;var meta=xr.GetFeature<MetaQuestFeature>();if(!meta)throw new Exception("Missing Meta Quest feature");meta.enabled=true;EditorUtility.SetDirty(meta);EditorUtility.SetDirty(xr);
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/UnderwaterArt"})){string path=AssetDatabase.GUIDToAssetPath(guid);if(AssetImporter.GetAtPath(path) is TextureImporter imp){var platform=imp.GetPlatformTextureSettings("Android");platform.name="Android";platform.overridden=true;bool coral=path.Contains("Secale_scan")||path.Contains("LivingCoral_")||path.Contains("BrainCoral_");platform.maxTextureSize=coral?2048:1024;platform.format=coral?TextureImporterFormat.ASTC_4x4:TextureImporterFormat.ASTC_6x6;imp.SetPlatformTextureSettings(platform);imp.mipmapEnabled=true;imp.streamingMipmaps=!coral;imp.anisoLevel=2;imp.SaveAndReimport();}}
  AssetDatabase.SaveAssets();Debug.Log("QUEST_CONFIGURED ARM64 IL2CPP Vulkan multiview ASTC6x6");
 }
 static void OptimizeScene(){
  long before=Triangles();int colonies=0;
  foreach(var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None)){
   var lods=group.GetLODs();if(lods.Length<3)continue;
   group.SetLODs(new[]{new LOD(.50f,lods[0].renderers),new LOD(.18f,lods[1].renderers),new LOD(.016f,lods[2].renderers)});group.fadeMode=LODFadeMode.None;group.RecalculateBounds();var coralLevels=group.GetLODs();var scale=group.transform.lossyScale;float visibleSize=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));coralLevels[2].screenRelativeTransitionHeight=Mathf.Min(.025f,visibleSize*.012f);group.SetLODs(coralLevels);colonies++;
  }
  var rockMeshes=new System.Collections.Generic.Dictionary<Mesh,Mesh>();
  foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   if(filter.name!="Porous reef outcrop"&&filter.name!="Eroded arch voussoir")continue;
   var full=filter.sharedMesh;
   if(!rockMeshes.TryGetValue(full,out var lighter)){
    if(!int.TryParse(full.name.Replace("Limestone_",""),out int variant))variant=rockMeshes.Count;
    var geometry=new ReefGeometry();
    geometry.Ellipsoid(Vector3.zero,Vector3.one,16,24,.23f,variant+17);
    lighter=geometry.Mesh(full.name+" Quest");rockMeshes.Add(full,lighter);
   }
   filter.sharedMesh=lighter;
  }
  var distanceMeshes=new System.Collections.Generic.Dictionary<Mesh,Mesh>();
  long fullTriangles=0,distanceTriangles=0;
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
   // Small procedural colonies otherwise submit their full mesh across the whole 82 m vista.
   if(r.name.StartsWith("Living coral |") || r.name=="Seagrass patch" || r.name=="Trailing tentacle"){
    var detail=r.gameObject.AddComponent<LODGroup>();
    float cutoff=r.name=="Trailing tentacle"?.075f:.042f;
    detail.SetLODs(new[]{new LOD(cutoff,new Renderer[]{r})});
    detail.RecalculateBounds();
    if(r.name.StartsWith("Living coral |")){
     var full=r.GetComponent<MeshFilter>().sharedMesh;
     if(!distanceMeshes.TryGetValue(full,out var low)){
      low=CoralDistanceMesh.Create(full);distanceMeshes.Add(full,low);
     }
     var child=new GameObject("Distant colony");
     child.transform.SetParent(r.transform,false);
     child.AddComponent<MeshFilter>().sharedMesh=low;
     var distant=child.AddComponent<MeshRenderer>();distant.sharedMaterial=r.sharedMaterial;
     distant.shadowCastingMode=ShadowCastingMode.Off;distant.reflectionProbeUsage=ReflectionProbeUsage.Off;
     float visibleSize=detail.size*Mathf.Max(Mathf.Abs(r.transform.lossyScale.x),Mathf.Abs(r.transform.lossyScale.z));
     // Keep touch-distance detail; use the same colony envelope sooner at mid range.
     detail.SetLODs(new[]{new LOD(.14f,new Renderer[]{r}),new LOD(Mathf.Min(.025f,visibleSize*.012f),new Renderer[]{distant})});
     detail.RecalculateBounds();
     fullTriangles+=(long)full.GetIndexCount(0)/3;distanceTriangles+=(long)low.GetIndexCount(0)/3;
    }
   }   r.shadowCastingMode=(r.name.StartsWith("Porous reef")||r.name.StartsWith("Eroded arch"))?ShadowCastingMode.On:ShadowCastingMode.Off;
   r.reflectionProbeUsage=ReflectionProbeUsage.Off;r.lightProbeUsage=LightProbeUsage.Off;
   foreach(var m in r.sharedMaterials)if(m)m.enableInstancing=true;
  }
  Debug.Log($"CORAL_DISTANCE_TRIANGLES full={fullTriangles} distant={distanceTriangles}");
  // Serialize the mobile materials in the built scene, preserving source assets.
  var mobileShader=Resources.Load<Shader>("QuestReef");
  if(!mobileShader)throw new Exception("Missing Quest reef shader");
  var mobileMaterials=new System.Collections.Generic.Dictionary<(Material,bool),Material>();
  foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){
   var materials=renderer.sharedMaterials;bool changed=false;
   for(int i=0;i<materials.Length;i++){
    var source=materials[i];if(!source||source.shader.name!="Underwater/Reef Surface")continue;
    bool closed=renderer.name=="Porous reef outcrop"||renderer.name=="Eroded arch voussoir"||renderer.name.StartsWith("Continuous seabed");
    for(var parent=renderer.transform;parent;parent=parent.parent)if(parent.name.StartsWith("Scanned ")||parent.name.StartsWith("Photogrammetry | weathered reef bed"))closed=true;
    var key=(source,closed);
    if(!mobileMaterials.TryGetValue(key,out var mobile)){
     mobile=new Material(source){shader=mobileShader,name=source.name+(closed?" Quest solid":" Quest two-sided"),enableInstancing=true};
     mobile.SetFloat("_Cull",closed?2:0);
     if(source.HasProperty("_WorldMapping")&&source.GetFloat("_WorldMapping")>.5f)mobile.EnableKeyword("REEF_WORLD_MAPPING");else mobile.DisableKeyword("REEF_WORLD_MAPPING");
     mobileMaterials.Add(key,mobile);
    }
    materials[i]=mobile;changed=true;
   }
   if(changed)renderer.sharedMaterials=materials;
  }
  Debug.Log("QUEST_MATERIALS_BAKED count="+mobileMaterials.Count);
  foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){light.shadows=light==RenderSettings.sun?LightShadows.Hard:LightShadows.None;if(light.type!=LightType.Directional)light.renderMode=LightRenderMode.ForceVertex;}
  // The diver's nearby cloud is already seeded. Do not also fast-forward each
  // 22-second environmental emitter on load or its first visible head turn.
  foreach(var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)){var main=ps.main;main.prewarm=false;main.cullingMode=ParticleSystemCullingMode.Pause;main.maxParticles=Mathf.Min(main.maxParticles,45);var emission=ps.emission;emission.rateOverTime=2;}
  if(!UnityEngine.Object.FindFirstObjectByType<QuestReefPerformance>())new GameObject("Quest 3 performance profile").AddComponent<QuestReefPerformance>();
  Debug.Log($"QUEST_SCENE_OPTIMIZED colonies={colonies} meshTrianglesAllLODsBefore={before} after={Triangles()}");
 }
 static long Triangles(){long count=0;foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))if(f.sharedMesh)for(int s=0;s<f.sharedMesh.subMeshCount;s++)count+=(long)f.sharedMesh.GetIndexCount(s)/3;return count;}
 public static void Manta(){RealisticReefBuilder.MantaUpgrade();}
 public static void Performance(){EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");ReefCollisionValidation.Run();FishMotionValidation.Run();var school=UnityEngine.Object.FindFirstObjectByType<FishSchool>();school.Initialize();for(int i=0;i<720;i++)school.Simulate(1f/72);if(school.FlockingPasses>110)throw new Exception("Flocking cadence regression");Debug.Log("PERFORMANCE_VALIDATED flockingPasses="+school.FlockingPasses+" over720Frames noPlayerBuild=true");}
 public static void GloveTravelCheck(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  var motor=UnityEngine.Object.FindFirstObjectByType<DiverCollisionMotor>();motor.Initialize();
  int clear=0;
  foreach(var center in OceanWorld.Centers)if(motor.TryTeleport(center))clear++;
  var camera=Camera.main;
  foreach(bool left in new[]{true,false}){
   var hand=ScubaGlove.Create(camera.transform,left);
   var renderer=hand.GetComponentInChildren<SkinnedMeshRenderer>();
   if(renderer.sharedMesh.blendShapeCount!=2||renderer.sharedMesh.vertexCount<1000)throw new Exception("Incomplete glove mesh");
   hand.Animate(1,0,1);
   if(renderer.GetBlendShapeWeight(0)!=100||renderer.GetBlendShapeWeight(1)!=0)throw new Exception("Trigger pose failed");
   hand.Animate(0,1,1);
   if(renderer.GetBlendShapeWeight(0)!=0||renderer.GetBlendShapeWeight(1)!=100)throw new Exception("Grip pose failed");
  }
  Debug.Log($"GLOVE_TRAVEL_CHECK two hands passed; clear habitat destinations={clear}/8");
 } public static void GrabCheck(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();
  var hand=new GameObject("Grab test hand").transform;
  var fish=Array.FindAll(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None),f=>f.grabbable);
  if(fish.Length<2)throw new Exception("Expected clownfish colony");
  for(int i=0;i<fish.Length;i++)fish[i].transform.position=hand.position+Vector3.right*(.4f-i*.04f);
  diver.UpdateGrab(true,hand,fish);
  var nearest=fish[fish.Length-1];
  if(!nearest.IsHeld||Array.FindAll(fish,f=>f.IsHeld).Length!=1)throw new Exception("Grip failed to select one nearest fish");
  nearest.Release();diver.UpdateGrab(true,hand,fish);
  if(Array.Exists(fish,f=>f.IsHeld))throw new Exception("Grip reacquired after escape");
  diver.UpdateGrab(false,hand,fish);diver.UpdateGrab(true,hand,fish);diver.UpdateGrab(false,hand,fish);
  if(Array.Exists(fish,f=>f.IsHeld))throw new Exception("Fish still held after release");
  Debug.Log("CLOWNFISH_GRAB_VALIDATED");
 } public static void Immersion(){ImmersionPreview.Run();}
 public static void Validate(){Configure();EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");OptimizeScene();RealisticReefBuilder.ValidateScene();ReefCollisionValidation.Run();FishMotionValidation.Run();}
 public static void Build(){Configure();Directory.CreateDirectory("Builds/Quest3");var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Underwater_Realistic.unity"},"Builds/Quest3/Underwater-Quest3.apk",BuildTarget.Android,Environment.GetEnvironmentVariable("REEF_CLEAN_BUILD")=="1"?BuildOptions.CleanBuildCache:BuildOptions.None);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Quest build failed: "+report.summary.result);Debug.Log("QUEST_BUILD_SUCCESS bytes="+report.summary.totalSize);}
}
}
















