using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefRepair {
 public static void Run(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");int grounded=0,colliders=0;
  foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   if(!f.sharedMesh)continue;
   bool rock=f.name=="Porous reef outcrop";bool solid=rock||f.name=="Eroded arch voussoir"||f.name.StartsWith("Continuous seabed")||f.name=="Exposed hull rib"||f.name=="Broken hull plank";
   for(var t=f.transform;t;t=t.parent)if(t.name.StartsWith("Photogrammetry | weathered reef bed"))solid=true;
   if(rock){float offset=float.PositiveInfinity;foreach(var v in f.sharedMesh.vertices){var p=f.transform.TransformPoint(v);offset=Mathf.Min(offset,p.y-ReefGeometry.Floor(p.x,p.z));}if(offset>-.12f){f.transform.position-=Vector3.up*(offset+.12f);grounded++;}}
   if(solid){f.gameObject.layer=8;var c=f.GetComponent<MeshCollider>();if(!c){c=f.gameObject.AddComponent<MeshCollider>();colliders++;}c.sharedMesh=f.sharedMesh;}
  }
    Physics.SyncTransforms();
  var supports=Array.FindAll(UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None),c=>c.name=="Porous reef outcrop"||c.name.StartsWith("Continuous seabed"));
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
   if(!t.name.StartsWith("Scanned ")&&!t.name.StartsWith("Living coral |"))continue;
   float bottom=float.PositiveInfinity;foreach(var f in t.GetComponentsInChildren<MeshFilter>()){if(!f.sharedMesh)continue;foreach(var v in f.sharedMesh.vertices)bottom=Mathf.Min(bottom,f.transform.TransformPoint(v).y);}
   if(float.IsInfinity(bottom))continue;var ray=new Ray(new Vector3(t.position.x,bottom+.1f,t.position.z),Vector3.down);float distance=15;bool found=false;
   foreach(var c in supports)if(c.Raycast(ray,out var hit,distance)){distance=hit.distance;found=true;}
   if(found&&distance>.22f){t.position-=Vector3.up*(distance-.065f);grounded++;}
  }
  Physics.SyncTransforms();
  var motor=UnityEngine.Object.FindFirstObjectByType<DiverCollisionMotor>();if(!motor)motor=UnityEngine.Object.FindFirstObjectByType<Diver>().gameObject.AddComponent<DiverCollisionMotor>();motor.Initialize();
  if(!UnityEngine.Object.FindFirstObjectByType<QuestReefPerformance>())new GameObject("Reef performance").AddComponent<QuestReefPerformance>();
  EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ReefCollisionValidation.Run();
  motor.Initialize();motor.Move(Vector3.up*100);if(Camera.main.transform.position.y>6.501f)throw new Exception("Water ceiling failed");
  Debug.Log($"REEF_REPAIR_VALIDATED grounded={grounded} addedColliders={colliders} waterCeiling=6.5");
 }
}
}

