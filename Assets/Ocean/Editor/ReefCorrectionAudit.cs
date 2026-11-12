using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefCorrectionAudit {
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  var coral=Array.Find(UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None),g=>g.name.StartsWith("Scanned Secale"));
  if(!coral)throw new Exception("No scanned coral");
  var instance=UnityEngine.Object.Instantiate(coral.gameObject,new Vector3(900,0,0),Quaternion.identity);instance.transform.localScale=Vector3.one;
  var group=instance.GetComponent<LODGroup>();group.ForceLOD(0);Bounds bounds=new Bounds(instance.transform.position,Vector3.zero);bool first=true;
  foreach(var renderer in group.GetLODs()[0].renderers){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
  foreach(var mesh in instance.GetComponentsInChildren<MeshFilter>()){
   var bins=new int[20];float min=float.PositiveInfinity,max=float.NegativeInfinity;
   foreach(var vertex in mesh.sharedMesh.vertices){float y=instance.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex)).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
   foreach(var vertex in mesh.sharedMesh.vertices){float y=instance.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex)).y;bins[Mathf.Clamp((int)((y-min)/Mathf.Max(.0001f,max-min)*20),0,19)]++;}
   Debug.Log("CORAL_MESH name="+mesh.sharedMesh.name+" vertices="+mesh.sharedMesh.vertexCount+" minY="+min+" maxY="+max+" bins="+string.Join(",",bins));
  }
  var camera=new GameObject("Coral audit camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.12f,.15f);camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x)*1.25f;
  camera.transform.position=bounds.center+new Vector3(1,.15f,1).normalized*bounds.size.magnitude*2;camera.transform.LookAt(bounds.center);RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",1);
  var light=new GameObject("Coral audit light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-35,0);light.intensity=1;
  Directory.CreateDirectory("Renders");Capture(camera,"Renders/Coral_Base_Audit.png");
  Debug.Log("CORAL_AUDIT_RENDERED");
 }
 public static void Capture(Camera camera,string path){var rt=new RenderTexture(1400,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
}
}
