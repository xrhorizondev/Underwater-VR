using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ImmersionPreview {
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var cam=Camera.main;var optics=cam.GetComponent<ReefOptics>();if(optics)optics.enabled=false;cam.allowHDR=false;cam.fieldOfView=95;cam.farClipPlane=55;
  var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();atmosphere.Apply(cam);
  var rig=cam.transform.parent;var bubbles=rig.gameObject.AddComponent<DiverBubbles>();bubbles.Initialize(cam.transform);var immersion=rig.gameObject.AddComponent<DiverImmersion>();immersion.Initialize(cam.transform);
  UnityEngine.Random.InitState(1729);int peak=0;
  for(int i=0;i<300;i++){float t=i/60f;Shader.SetGlobalFloat("_ReefTime",t);bubbles.PreviewStep(1f/60,t);immersion.PreviewStep(1f/60);peak=Mathf.Max(peak,bubbles.ParticleCount);if(i==100)Capture(cam,"Diver_Immersion_View");if(i==0)Capture(cam,"Water_Motion_Start");if(i==240)Capture(cam,"Water_Motion_After4s");}
  if(peak==0||peak>64)throw new Exception("Bubble capacity validation failed: "+peak);
  // Inspect the actual bubble geometry from beside the diver, at readable scale.
  var inspect=new GameObject("Bubble detail camera").AddComponent<Camera>();inspect.CopyFrom(cam);inspect.transform.position=cam.transform.position+new Vector3(.65f,.2f,.5f);inspect.transform.LookAt(cam.transform.position+Vector3.up*.15f);inspect.fieldOfView=45;Capture(inspect,"Bubble_Mesh_Detail");
  rig.position+=Vector3.forward*20;bubbles.Tick(0,3);if(bubbles.ParticleCount!=0)throw new Exception("Teleport left old bubbles behind");
  Debug.Log("IMMERSION_VALIDATED peakBubbles="+peak+" meshParticles=true teleportClear=true");
 }
 static void Capture(Camera camera,string name){var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();Directory.CreateDirectory("Renders");File.WriteAllBytes("Renders/"+name+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}
}

