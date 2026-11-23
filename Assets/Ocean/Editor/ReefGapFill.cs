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
public static class ReefGapFill {
 public static void ValidateSaved(){EditorSceneManager.OpenScene(Scene);var root=GameObject.Find("Natural reef gap planting");if(!root||root.GetComponentsInChildren<MeshCollider>().Length!=106)throw new Exception("Gap-fill scene records missing");foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(!f.sharedMesh||!f.GetComponent<Renderer>().sharedMaterial)throw new Exception("Missing gap-fill asset");RealisticReefBuilder.ValidateScene();ReefCollisionValidation.Run();Debug.Log("MERGED_GAP_FILL_VALIDATED");}
 const string Scene="Assets/Scenes/Underwater_Realistic.unity", Art="Assets/UnderwaterArt/GapFill/";
 static bool Ground(Vector3 p,out float y){y=float.NegativeInfinity;foreach(var h in Physics.RaycastAll(new Vector3(p.x,40,p.z),Vector3.down,180,1<<8,QueryTriggerInteraction.Ignore))if(h.collider.name.StartsWith("Continuous seabed"))y=Mathf.Max(y,h.point.y);return !float.IsNegativeInfinity(y);}
 static Mesh Save(Mesh m,string name){string path=Art+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(m,path);return m;}
 static GameObject Model(string name,Mesh mesh,Material mat,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;return go;}
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Art);AssetDatabase.Refresh();var old=GameObject.Find("Natural reef gap planting");if(old)UnityEngine.Object.DestroyImmediate(old);
  var root=new GameObject("Natural reef gap planting");Physics.SyncTransforms();Random.InitState(231126);
  var rocks=new Mesh[3];for(int i=0;i<3;i++){var b=new ReefGeometry();b.Ellipsoid(Vector3.zero,Vector3.one,10,14,.24f,71+i);rocks[i]=Save(b.Mesh("Low limestone rubble "+i),"Limestone"+i);}
  var rockMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/UnderwaterArt/Materials/02 Encrusted limestone.mat");var grassMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/UnderwaterArt/Materials/10 Seagrass.mat");var grass=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/UnderwaterArt/Meshes/Seagrass_1.asset");
  if(!rockMat||!grassMat||!grass)throw new Exception("Missing matching reef assets");
  var occupied=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Porous reef outcrop"||r.name.Contains("weathered reef bed")||r.GetComponent<TeleportShell>()||r.GetComponent<SeaLife>()).Select(r=>r.bounds).ToList();
  int count=0,plants=0;var supports=new List<Vector3>();
  for(int zone=0;zone<8;zone++){
   var patch=new GameObject(OceanWorld.Names[zone]+" sand-gap clusters");patch.transform.SetParent(root.transform);var blades=new List<CombineInstance>();
   int target=(zone==0||zone==1||zone==7)?24:12;
   for(int attempt=0,placed=0;attempt<450&&placed<target;attempt++){
    float z=OceanWorld.Centers[zone].z+Random.Range(-14f,14f),x=Random.Range(-31f,31f);float channel=Mathf.Sin(z*.115f)*2;
    if(Mathf.Abs(x-channel)<2.4f)continue;var p=new Vector3(x,0,z);if(!Ground(p,out float y))continue;p.y=y;
    if(occupied.Any(b=>{var q=b.ClosestPoint(p);return new Vector2(q.x-p.x,q.z-p.z).sqrMagnitude<2.2f*2.2f;})||supports.Any(q=>new Vector2(q.x-x,q.z-z).sqrMagnitude<10))continue;
    float radius=Random.Range(.65f,1.55f);var rock=Model("Grounded gap limestone",rocks[count%3],rockMat,patch.transform);rock.layer=8;rock.transform.position=p;rock.transform.rotation=Quaternion.Euler(Random.Range(-12,12),Random.Range(0,360),Random.Range(-10,10));rock.transform.localScale=new Vector3(radius,radius*Random.Range(.35f,.65f),radius*Random.Range(.75f,1.3f));
    float lift=float.NegativeInfinity;foreach(var v in rock.GetComponent<MeshFilter>().sharedMesh.vertices){var w=rock.transform.TransformPoint(v);if(Ground(w,out float floor))lift=Mathf.Max(lift,floor-w.y);}rock.transform.position+=Vector3.up*(lift-.09f);rock.AddComponent<MeshCollider>().sharedMesh=rocks[count%3];
    var lod=rock.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.014f,new[]{rock.GetComponent<Renderer>()})});lod.RecalculateBounds();supports.Add(p);count++;placed++;
    if(zone!=0&&zone!=1&&zone!=2&&zone!=7)continue;
    for(int j=0;j<5;j++){float angle=Random.value*Mathf.PI*2,dist=radius+Random.Range(.35f,1.4f);var g=p+new Vector3(Mathf.Cos(angle)*dist,0,Mathf.Sin(angle)*dist);if(Mathf.Abs(g.x-Mathf.Sin(g.z*.115f)*2)<2.1f||!Ground(g,out float gy))continue;g.y=gy-.035f;float size=Random.Range(.8f,1.6f);blades.Add(new CombineInstance{mesh=grass,transform=Matrix4x4.TRS(g,Quaternion.Euler(0,Random.Range(0,360),0),new Vector3(size,Random.Range(.8f,1.5f),size))});plants++;}
   }
   if(blades.Count>0){var m=new Mesh{name="Shared seagrass gap meadow",indexFormat=IndexFormat.UInt32};m.CombineMeshes(blades.ToArray(),true,true);m=Save(m,"Meadow"+zone);Model("Batched gap seagrass",m,grassMat,patch.transform);}
  }
  Physics.SyncTransforms();RealisticReefBuilder.ValidateScene();ReefCollisionValidation.Run();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("Renders/GapFill");File.WriteAllText("Renders/GapFill/Validation.txt","Grounded rocks="+count+"\nSeagrass tufts="+plants+"\n3 shared 280-triangle rock meshes; plants batched per habitat.\nSaved terrain grounding, 4.8m central corridor, existing object spacing, scene and collision checks passed.\n");
  var cam=Camera.main;var optics=cam.GetComponent<ReefOptics>();if(optics)optics.enabled=false;cam.farClipPlane=90;cam.fieldOfView=75;cam.transform.position=new Vector3(0,7,-15);cam.transform.LookAt(new Vector3(0,-3,13));ReefCorrectionAudit.Capture(cam,"Renders/GapFill/SunlitReef.png");Debug.Log("REEF_GAP_FILL_PASSED rocks="+count+" plants="+plants);
 }
}
}

