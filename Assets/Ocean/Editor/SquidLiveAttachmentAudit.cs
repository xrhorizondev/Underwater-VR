#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
namespace Underwater.Editor {
[InitializeOnLoad] public static class SquidLiveAttachmentAudit {
 const string Request=".utmp/SquidLiveAttachmentAudit.request",Output="Renders/SquidLiveAttachment/";
 static SquidLiveAttachmentAudit(){EditorApplication.delayCall+=()=>{if(File.Exists(Request)){File.Delete(Request);Run();}};}
 [MenuItem("Underwater/Audit selected squid attachment")]
 public static void Run(){
  Directory.CreateDirectory(Output);var sb=new StringBuilder();sb.AppendLine("LIVE SCENE "+UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path+" playing="+EditorApplication.isPlaying);
  var actors=UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None);
  foreach(var a in actors){sb.AppendLine("ACTOR "+a.name+" "+a.transform.position);
   foreach(var r in a.GetComponentsInChildren<Renderer>(true)){
    var sk=r as SkinnedMeshRenderer;var mf=r.GetComponent<MeshFilter>();var mesh=sk?sk.sharedMesh:mf?mf.sharedMesh:null;
    sb.AppendLine("RENDERER "+r.name+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" type="+r.GetType().Name+" mesh="+(mesh?mesh.name:"null")+" path="+(mesh?AssetDatabase.GetAssetPath(mesh):"null")+" triangles="+(mesh?mesh.triangles.Length/3:0)+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));
    if(mesh){sb.AppendLine("VERTICES "+mesh.vertexCount+" bounds="+mesh.bounds+" id="+mesh.GetInstanceID());var coords=mesh.vertices;var inRoots=coords.Where(p=>p.z<.03f&&p.z>-.03f).ToArray();}
    if(sk){sb.AppendLine("BONES "+sk.bones.Length+" root="+sk.rootBone.name);}
   }
  }
  var selected=Selection.activeGameObject;sb.AppendLine("SELECTED "+(selected?selected.name:"null"));File.WriteAllText(Output+"LiveAudit.txt",sb.ToString());
  foreach(var actor in actors)CaptureLive(actor);
  Debug.Log("LIVE_SQUID_AUDIT_DONE\n"+sb);
 }
 static void CaptureLive(SquidSwimmer actor){
  var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var root=skin.bones.First(b=>b.name=="Root");var axis=(skin.bones.First(b=>b.name=="Mantle").position-root.position).normalized;var across=Vector3.Cross(Vector3.up,axis).normalized;var focus=root.position-axis*.25f-Vector3.up*.6f;
  var go=new GameObject("Temporary live squid audit camera");go.hideFlags=HideFlags.HideAndDontSave;var camera=go.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.07f,.1f);camera.nearClipPlane=.01f;camera.farClipPlane=200;camera.orthographic=true;camera.orthographicSize=1.65f;
  foreach(int side in new[]{1,-1}){camera.transform.position=focus+across*5*side-Vector3.up*1.8f;camera.transform.LookAt(focus,Vector3.up);var rt=new RenderTexture(1400,900,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(Output+"LiveUnderside_"+side+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  UnityEngine.Object.DestroyImmediate(go);
 }
}
}
#endif


