using System;
using UnityEngine;
using UnityEditor;
namespace Underwater.Editor {
public static partial class RealisticReefBuilder {
 static void InstallReefCollision(){
  var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="ReefSolid";tags.ApplyModifiedProperties();
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
   if(t.name.StartsWith("Scanned ")||t.name.StartsWith("Living coral |")){
    var box=t.gameObject.AddComponent<BoxCollider>();bool first=true;Bounds bounds=new Bounds();
    foreach(var mf in t.GetComponentsInChildren<MeshFilter>()){var b=mf.sharedMesh.bounds;for(int i=0;i<8;i++){var p=t.InverseTransformPoint(mf.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}
    box.center=bounds.center;box.size=bounds.size;t.gameObject.layer=8;
   }
  }
  foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   bool solid=mf.name.StartsWith("Continuous seabed")||mf.name=="Porous reef outcrop"||mf.name=="Eroded arch voussoir"||mf.name=="Exposed hull rib"||mf.name=="Broken hull plank";
   for(var p=mf.transform;p;p=p.parent)if(p.name.StartsWith("Photogrammetry | weathered reef bed"))solid=true;
   if(!solid)continue;mf.gameObject.layer=8;var collider=mf.GetComponent<MeshCollider>();if(!collider)collider=mf.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mf.sharedMesh;
  }
  Physics.SyncTransforms();int animals=0;
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
   if(!t.GetComponent<FishAgent>()&&!t.GetComponent<FishBodyMotion>()&&!t.GetComponent<TurtleSwimmer>()&&!t.GetComponent<MarineSwimmer>())continue;
   var body=t.gameObject.AddComponent<ReefSphereBody>();float radius=.1f;
   foreach(var mf in t.GetComponentsInChildren<MeshFilter>())foreach(var v in mf.sharedMesh.vertices)radius=Mathf.Max(radius,Vector3.Distance(t.position,mf.transform.TransformPoint(v)));
   body.radius=radius*1.1f+.025f;
   if(!body.PlaceNear(t.position))throw new Exception("Cannot find clear animal spawn: "+t.name);animals++;
  }
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();diver.gameObject.AddComponent<DiverCollisionMotor>();
  Debug.Log("REEF_COLLISION_INSTALLED animals="+animals);
 }
}
}
