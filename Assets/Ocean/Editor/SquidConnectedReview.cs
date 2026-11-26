using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
 public static class SquidConnectedReview {
  public static void Run(){
   const string model="Assets/UnderwaterArt/BlenderSquid/ReferenceSquid.fbx",output="Renders/SquidConnectedRepair/";
   Directory.CreateDirectory(output);AssetDatabase.ImportAsset(model,ImportAssetOptions.ForceUpdate);
   EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
   var squid=UnityEngine.Object.FindFirstObjectByType<SquidSwimmer>();var skin=squid.GetComponentInChildren<SkinnedMeshRenderer>();
   if(AssetDatabase.GetAssetPath(skin.sharedMesh)!=model)throw new Exception("Unexpected scene squid mesh: "+AssetDatabase.GetAssetPath(skin.sharedMesh));
   var player=squid.GetComponentInChildren<Animation>();var root=skin.bones.First(b=>b.name=="Root");
   var surfaces=squid.GetComponentsInChildren<Renderer>();foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(!surfaces.Contains(r))r.enabled=false;
   var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;
   camera.nearClipPlane=.01f;camera.farClipPlane=120;camera.orthographic=true;camera.orthographicSize=2.2f;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.09f,.12f);
   RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.7f,.7f,.7f);
      Bounds envelope=default;bool first=true;
   for(int frame=0;frame<=48;frame++){player.clip.SampleAnimation(player.gameObject,player.clip.length*frame/48);foreach(var point in OctopusRepairValidation.Points(skin)){var local=skin.transform.InverseTransformPoint(point);if(first){envelope=new Bounds(local,Vector3.zero);first=false;}else envelope.Encapsulate(local);}}
   envelope.Expand(envelope.size.magnitude*.05f);skin.localBounds=envelope;
   File.WriteAllText(output+"Bounds.json","{\"center\":"+JsonUtility.ToJson(envelope.center)+",\"extents\":"+JsonUtility.ToJson(envelope.extents)+"}");
   foreach(float phase in new[]{0f,.25f,.5f,.75f}){
    player.clip.SampleAnimation(player.gameObject,player.clip.length*phase);skin.updateWhenOffscreen=true;skin.localBounds=envelope;
    var points=OctopusRepairValidation.Points(skin);var worldBounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)worldBounds.Encapsulate(point);worldBounds.Expand(.3f);skin.bounds=worldBounds;
    var focus=root.position+squid.transform.forward*.15f;
    camera.transform.position=focus+squid.transform.right*4-squid.transform.up*3-squid.transform.forward*2;
    camera.transform.LookAt(focus,squid.transform.forward);ReefCorrectionAudit.Capture(camera,output+"Roots_"+(int)(phase*100)+".png");
   }
   camera.orthographicSize=12;var bounds=skin.bounds;camera.transform.position=bounds.center+squid.transform.right*24+squid.transform.up*8;camera.transform.LookAt(bounds.center);ReefCorrectionAudit.Capture(camera,output+"Whole.png");
   File.WriteAllText(output+"UnityValidation.txt","Scene uses "+AssetDatabase.GetAssetPath(skin.sharedMesh)+"\nTriangles: "+skin.sharedMesh.triangles.Length/3+"\nBones: "+skin.bones.Length+"\nClip: "+player.clip.name+"\nCaptured arm roots at four animation phases. Scene not saved.\n");
   Debug.Log("SQUID_CONNECTED_UNITY_REVIEW_PASSED");
  }
 }
}





