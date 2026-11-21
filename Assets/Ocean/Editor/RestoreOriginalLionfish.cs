using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Underwater.Editor {
public static class RestoreOriginalLionfish {
 public static void Run(){
  var baseline=EditorSceneManager.OpenScene("Assets/Scenes/LionfishOriginalBaseline.unity");var original=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(a=>a.kind=="Lionfish");
  if(original.transform.Find("Prepared animated model"))throw new Exception("Baseline is already the downloaded model");
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var current=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SeaLife>(true)).Single(a=>a.kind=="Lionfish");var parent=current.transform.parent;var position=current.transform.position;var rotation=current.transform.rotation;var scale=current.transform.localScale;var field=current.GetComponent<ReefAnimalClearance>().seabed;
  var restored=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(restored,scene);restored.name=current.name;restored.transform.SetParent(parent,false);restored.transform.SetPositionAndRotation(position,rotation);restored.transform.localScale=scale;
  UnityEngine.Object.DestroyImmediate(current.gameObject);EditorSceneManager.CloseScene(baseline,true);
  var guard=restored.GetComponent<ReefAnimalClearance>();if(!guard)guard=restored.AddComponent<ReefAnimalClearance>();guard.seabed=field;guard.animatedBody=AnimalSeabedRepair.Envelope(restored.transform);guard.Enforce();
  var renderers=restored.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();if(renderers.Length<5||restored.GetComponentsInChildren<SkinnedMeshRenderer>().Any())throw new Exception("Original procedural model not restored");
  var encounter=restored.GetComponent<ReefEncounter>();if(!encounter||encounter.response!=ReefEncounter.Response.Lionfish)throw new Exception("Original movement missing");
  var hazard=restored.GetComponent<ReefDangerZone>();if(!hazard||hazard.surfaces.Any(s=>!s||!s.enabled))throw new Exception("Original spine hazards missing");
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ReefDayTwo.Validate();PreparedMarineIntegration.Validate();
  Debug.Log("ORIGINAL_LIONFISH_RESTORED renderers="+renderers.Length+" originalFinAnimation=true originalSpineWarnings=true");
 }
 public static void VerifyAndCapture(){
  MarineModelRefresh.Regression();EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var life=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(a=>a.kind=="Lionfish");var surfaces=life.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();var bounds=surfaces[0].bounds;foreach(var r in surfaces){bounds.Encapsulate(r.bounds);r.gameObject.layer=31;}
  var camera=new GameObject("Original lionfish camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.07f,.09f);camera.nearClipPlane=.005f;camera.farClipPlane=100;camera.orthographic=true;camera.orthographicSize=bounds.extents.magnitude;camera.transform.position=bounds.center+(life.transform.right+life.transform.forward+Vector3.up*.25f).normalized*bounds.extents.magnitude*3;camera.transform.LookAt(bounds.center);RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.5f,.6f,.65f);Shader.SetGlobalFloat("_ReefLight",.8f);var lamp=new GameObject("Review light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.intensity=1;lamp.transform.rotation=Quaternion.Euler(25,-35,0);System.IO.Directory.CreateDirectory("Renders/OriginalLionfish");ReefCorrectionAudit.Capture(camera,"Renders/OriginalLionfish/Restored.png");Debug.Log("ORIGINAL_LIONFISH_VISUAL_VERIFIED parts="+surfaces.Length);
 }
}}