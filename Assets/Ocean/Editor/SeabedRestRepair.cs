using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class SeabedRestRepair {
 public static void Run(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");Physics.SyncTransforms();
  var floors=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.sharedMesh&&c.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  Func<Vector3,float> floor=p=>{float y=float.NegativeInfinity;foreach(var c in floors)if(c.Raycast(new Ray(new Vector3(p.x,40,p.z),Vector3.down),out var hit,180))y=Mathf.Max(y,hit.point.y);if(float.IsInfinity(y))throw new Exception("Missing seabed");return y;};
  Directory.CreateDirectory("Renders/SeabedRest");var report=new System.Text.StringBuilder();
  foreach(var anchor in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Weathered supplied anchor - "))){
   var p=anchor.position;const float d=.5f;float dx=(floor(p+Vector3.right*d)-floor(p-Vector3.right*d))/(2*d),dz=(floor(p+Vector3.forward*d)-floor(p-Vector3.forward*d))/(2*d);
   anchor.rotation=Quaternion.FromToRotation(Vector3.up,new Vector3(-dx,1,-dz).normalized)*Quaternion.Euler(90,anchor.eulerAngles.y,0);
   var points=anchor.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
   float shift=points.Max(v=>floor(v)-v.y);anchor.position+=Vector3.up*(shift-.01f);Physics.SyncTransforms();
   report.AppendLine(anchor.name+" verticalShift="+shift);
  }
  EditorSceneManager.SaveScene(scene);
  OctopusGroundValidation.Run();
  File.WriteAllText("Renders/SeabedRest/Report.txt",report.ToString());
  CullingAnchorReview.Capture();Debug.Log("SEABED_REST_REPAIR_SUCCESS");
 }
}
}
