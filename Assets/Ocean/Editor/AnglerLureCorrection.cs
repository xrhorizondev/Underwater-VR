using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class AnglerLureCorrection {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static DeepWaterEncounter[] Anglers()=>UnityEngine.Object.FindObjectsByType<DeepWaterEncounter>(FindObjectsSortMode.None).Where(e=>e.species==DeepWaterEncounter.Species.Angler).ToArray();
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);int lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
  const string haloPath="Assets/UnderwaterArt/ReviewCorrections/LureHalo.mat";var haloMaterial=AssetDatabase.LoadAssetAtPath<Material>(haloPath);if(!haloMaterial){haloMaterial=new Material(Shader.Find("Underwater/Lure Halo"));AssetDatabase.CreateAsset(haloMaterial,haloPath);}haloMaterial.color=new Color(.3f,1.4f,1.8f);haloMaterial.enableInstancing=true;EditorUtility.SetDirty(haloMaterial);
  const string meshPath="Assets/UnderwaterArt/ReviewCorrections/LureHalo.asset";var haloMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(!haloMesh){haloMesh=new Mesh{name="Lure halo quad",vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},triangles=new[]{0,1,2,0,2,3}};haloMesh.bounds=new Bounds(Vector3.zero,Vector3.one*1.5f);AssetDatabase.CreateAsset(haloMesh,meshPath);}
  foreach(var e in Anglers()){
   var bulb=e.transform.Find("Visible organic lure/Emissive lure bulb");if(!bulb)throw new Exception("Missing angler lure bulb");
   var light=bulb.GetComponent<Light>();if(!light)light=bulb.gameObject.AddComponent<Light>();
   light.type=LightType.Point;light.color=Color.cyan;light.intensity=.35f;light.range=.65f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForceVertex;
   var bodyLight=e.GetComponent<Light>();if(bodyLight)UnityEngine.Object.DestroyImmediate(bodyLight);
   if(!bulb.Find("Lure glow halo")){var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);halo.name="Lure glow halo";UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.transform.SetParent(bulb,false);halo.transform.localScale=Vector3.one*3.2f;var renderer=halo.GetComponent<Renderer>();renderer.sharedMaterial=haloMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;}
   // Cubic bounds contain the shader-facing quad at every fish/camera angle.
   bulb.Find("Lure glow halo").GetComponent<MeshFilter>().sharedMesh=haloMesh;
   var material=e.GetComponent<Renderer>().sharedMaterial;if(material.HasProperty("_BioEmission")){material.SetColor("_BioEmission",Color.clear);EditorUtility.SetDirty(material);}
  }
  if(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length!=lights)throw new Exception("Lure relocation changed light count");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();ReefRegressionFix.Validate();Capture();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);var anglers=Anglers();if(anglers.Length!=12)throw new Exception("Angler population changed");
  foreach(var e in anglers){
   var bulb=e.transform.Find("Visible organic lure/Emissive lure bulb");var light=bulb?bulb.GetComponent<Light>():null;
   if(e.GetComponent<Light>()||!light||e.GetComponentsInChildren<Light>().Length!=1||light.type!=LightType.Point||light.shadows!=LightShadows.None||light.renderMode!=LightRenderMode.ForceVertex||light.range>.651f)throw new Exception("Angler light must remain localized to the lure tip");
   var halo=bulb.Find("Lure glow halo");if(!halo||halo.GetComponent<Collider>()||ShaderUtil.ShaderHasError(halo.GetComponent<Renderer>().sharedMaterial.shader))throw new Exception("Missing or invalid localized lure halo");
   if(e.GetComponent<SeaLife>().grabbable||e.GetComponent<Renderer>().sharedMaterial.GetColor("_BioEmission").maxColorComponent>.0001f)throw new Exception("Angler body material/grab regression");
   var skin=e.GetComponent<Renderer>();var block=new MaterialPropertyBlock();var renderer=bulb.GetComponent<Renderer>();float resting=0;
   for(int phase=0;phase<3;phase++){
    for(int frame=0;frame<140;frame++){
     e.Step(.05f,4,e.transform.position+Vector3.forward*3,phase==1?0:3);skin.GetPropertyBlock(block);
     if(block.GetColor("_BioEmission").maxColorComponent>.0001f)throw new Exception("Angler body became emissive during touch/recovery");
     if((light.transform.position-bulb.position).sqrMagnitude>.000001f)throw new Exception("Point light detached from lure");
     if(phase==1&&frame==34){renderer.GetPropertyBlock(block);if(block.GetColor("_Color").g<1.8f||light.intensity<.7f)throw new Exception("Lure touch brightening lost");}
    }
    renderer.GetPropertyBlock(block);if(phase==0)resting=block.GetColor("_Color").g;if(phase==2&&(Mathf.Abs(block.GetColor("_Color").g-resting)>.001f||Mathf.Abs(light.intensity-.35f)>.001f))throw new Exception("Lure did not recover after touch");
   }
  }
  Debug.Log("ANGLER_LURE_VALIDATED anglers=12 bodyBioEmission=zero lightAtBulb=true touchAndRecovery=true shadowlessVertexLights=12 noAddedLights=true");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/AnglerLure");var e=Anglers()[0];var camera=Camera.main;camera.fieldOfView=45;camera.transform.position=e.transform.position+e.transform.TransformDirection(new Vector3(1.5f,.35f,.9f));camera.transform.LookAt(e.transform.position+e.transform.up*.1f);UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>().Apply(camera);
  for(int i=0;i<140;i++)e.Step(.05f,4,camera.transform.position,3);ReefCorrectionAudit.Capture(camera,"Renders/AnglerLure/Idle.png");
  for(int i=0;i<35;i++)e.Step(.05f,4,camera.transform.position,0);ReefCorrectionAudit.Capture(camera,"Renders/AnglerLure/Touch.png");
 }
}
}
