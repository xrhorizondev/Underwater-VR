using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
namespace Underwater.Editor {
public static class OceanBuild {
 public static void ConfigureXR(){
 System.IO.Directory.CreateDirectory("Assets/XR");
 var all=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/GeneralSettings.asset");
 if(!all){all=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(all,"Assets/XR/GeneralSettings.asset");}
 EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,all,true);
 foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android}){
 if(!all.HasManagerSettingsForBuildTarget(group))all.CreateDefaultManagerSettingsForBuildTarget(group);
 var general=all.SettingsForBuildTarget(group);general.InitManagerOnStart=true;
 XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group);
 var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);if(settings){var touch=settings.GetFeature<OculusTouchControllerProfile>();if(touch)touch.enabled=true;EditorUtility.SetDirty(settings);}
 EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
 }EditorUtility.SetDirty(all);AssetDatabase.SaveAssets();}
 [MenuItem("Underwater/Create exploration scene")]
 public static void CreateScene(){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Underwater World").AddComponent<OceanWorld>();System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/UnderwaterExploration.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/UnderwaterExploration.unity",true)};PlayerSettings.companyName="XR Horizon";PlayerSettings.productName="Underwater Exploration VR";PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;AssetDatabase.SaveAssets();Debug.Log("UNDERWATER_SCENE_READY");}
 public static void BuildWindows(){ConfigureXR();CreateScene();
 var graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];var serialized=new SerializedObject(graphics);var shaders=serialized.FindProperty("m_AlwaysIncludedShaders");var shader=Shader.Find("Standard");bool included=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)included=true;if(!included){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;serialized.ApplyModifiedProperties();}AssetDatabase.SaveAssets();
 var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Builds/Windows/Underwater Exploration.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);}
}
}
