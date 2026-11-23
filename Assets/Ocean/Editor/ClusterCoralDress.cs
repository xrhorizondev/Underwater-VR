using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Random=UnityEngine.Random;
namespace Underwater.Editor {
public static class ClusterCoralDress {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Folder="Assets/UnderwaterArt/ClusterCorals/";
 static Mesh Save(Mesh m,string name){string path=Folder+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(m,path);return m;}
 static MeshRenderer Part(Transform parent,string name,List<CombineInstance> parts,Material mat){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true);mesh=Save(mesh,name);var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;return r;}
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var old=GameObject.Find("Coral accents on rock islands");if(old)UnityEngine.Object.DestroyImmediate(old);var root=new GameObject("Coral accents on rock islands");var islands=GameObject.Find("Outer seabed rock gardens");if(!islands)throw new Exception("Missing rock islands");Random.InitState(99264);
  string[] names={"Plate_0","Brain_0","Fan_0"};string[] skins={"07 Acropora teal","11 Brain coral ridges","08 Coral lavender"};var near=new Mesh[3];var far=new Mesh[3];var materials=new Material[3];for(int i=0;i<3;i++){var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/UnderwaterArt/Meshes/"+names[i]+".asset");near[i]=Save(FishDistanceMesh.Create(source,10),"Shared"+names[i]);far[i]=Save(FishDistanceMesh.Create(source,4),"SharedLow"+names[i]);materials[i]=AssetDatabase.LoadAssetAtPath<Material>("Assets/UnderwaterArt/Materials/"+skins[i]+".mat");if(!materials[i])throw new Exception("Missing coral material");}
  int count=0,index=0;long highTris=0,lowTris=0;var positions=new List<Vector3>();
  foreach(var rock in islands.GetComponentsInChildren<MeshCollider>()){
   if(Random.value>.72f)continue;int kind=index++%3;var probe=new GameObject("Temporary visual rock probe");probe.transform.SetPositionAndRotation(rock.transform.position,rock.transform.rotation);probe.transform.localScale=rock.transform.lossyScale;var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=rock.GetComponent<MeshFilter>().sharedMesh;Physics.SyncTransforms();var bounds=collider.bounds;var close=new List<CombineInstance>();var distant=new List<CombineInstance>();
   int target=Random.Range(2,5);for(int attempt=0;attempt<60&&close.Count<target;attempt++){
    var ray=new Ray(new Vector3(Random.Range(bounds.min.x,bounds.max.x),bounds.max.y+2,Random.Range(bounds.min.z,bounds.max.z)),Vector3.down);if(!collider.Raycast(ray,out var hit,bounds.size.y+4)||hit.normal.y<.65f||positions.Any(p=>(p-hit.point).sqrMagnitude<.7f))continue;
    float scale=kind==2?Random.Range(.5f,.9f):Random.Range(.55f,1.1f);var rotation=Quaternion.Euler(Random.Range(-4f,4f),Random.Range(0,360),Random.Range(-4f,4f));var p=hit.point-Vector3.up*(near[kind].bounds.min.y*scale+.025f);var matrix=Matrix4x4.TRS(p,rotation,Vector3.one*scale);close.Add(new CombineInstance{mesh=near[kind],transform=matrix});distant.Add(new CombineInstance{mesh=far[kind],transform=matrix});positions.Add(hit.point);count++;highTris+=near[kind].triangles.Length/3;lowTris+=far[kind].triangles.Length/3;
   }
   UnityEngine.Object.DestroyImmediate(probe);if(close.Count==0)continue;var patch=new GameObject("Corals on "+rock.name);patch.transform.SetParent(root.transform,false);var r=Part(patch.transform,"Coral_"+index,close,materials[kind]);var lr=Part(patch.transform,"CoralLow_"+index,distant,materials[kind]);var lod=patch.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.3f,new[]{r}),new LOD(.14f,new[]{lr})});lod.RecalculateBounds();
  }
  AssetDatabase.SaveAssets();RealisticReefBuilder.ValidateScene();ReefCollisionValidation.Run();EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("Renders/ClusterCorals");File.WriteAllText("Renders/ClusterCorals/Validation.txt","Corals="+count+" patches="+root.transform.childCount+"\nHigh triangles="+highTris+" distant="+lowTris+"\nSaved visual rock raycast anchoring, no added colliders, lights, shadows or runtime scripts. Shared existing materials and two mesh LODs with early culling.\n");
  var cam=Camera.main;var optics=cam.GetComponent<ReefOptics>();if(optics)optics.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.02f,.25f,.38f);cam.farClipPlane=90;cam.fieldOfView=65;var focus=positions.OrderBy(p=>(p-new Vector3(20,0,-35)).sqrMagnitude).First();cam.transform.position=focus+new Vector3(7,5,-8);cam.transform.LookAt(focus);ReefCorrectionAudit.Capture(cam,"Renders/ClusterCorals/CoralRock.png");Debug.Log("CLUSTER_CORALS_VALIDATED count="+count+" high="+highTris+" distant="+lowTris);
 }
}
}
