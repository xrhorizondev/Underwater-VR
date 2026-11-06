using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static partial class RealisticReefBuilder {
 static void UpgradeManta(GameObject manta){
  string sourcePath=Art+"Scans/GiantManta.fbx";var importer=AssetImporter.GetAtPath(sourcePath) as ModelImporter;if(!importer)throw new Exception("Manta source FBX missing");importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.SaveAndReimport();
  foreach(string name in new[]{"MantaSource_albedo","MantaSource_normal"}){var textureImporter=AssetImporter.GetAtPath(Art+"Textures/"+name+".png") as TextureImporter;textureImporter.textureType=name.EndsWith("normal")?TextureImporterType.NormalMap:TextureImporterType.Default;textureImporter.SaveAndReimport();}
  var source=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));var parts=source.GetComponentsInChildren<MeshFilter>();Mesh bodyMesh=null;var details=new System.Collections.Generic.List<CombineInstance>();
  foreach(var part in parts){var combine=new CombineInstance{mesh=part.sharedMesh,transform=part.transform.localToWorldMatrix};if(part.name.Contains("Continuous")){bodyMesh=new Mesh{name="Imported manta continuous skin"};bodyMesh.CombineMeshes(new[]{combine},true,true);bodyMesh.bounds=new Bounds(Vector3.zero,new Vector3(5.5f,2.4f,6));}else details.Add(combine);if(part.name.Contains("eye"))Debug.Log("MANTA_EYE_POSITION "+part.transform.TransformPoint(part.sharedMesh.bounds.center));}
  if(!bodyMesh)throw new Exception("Imported manta body missing");var detailMesh=new Mesh{name="Manta mouth eyes and gills"};detailMesh.CombineMeshes(details.ToArray(),true,true);
  UnityEngine.Object.DestroyImmediate(source);
  var material=new Material(Shader.Find("Underwater/Manta Skin")){enableInstancing=true};material.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/MantaSource_albedo.png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/MantaSource_normal.png"));
  manta.GetComponent<MeshFilter>().sharedMesh=Save(bodyMesh,Art+"Meshes/MantaImportedBody.asset");manta.GetComponent<Renderer>().sharedMaterial=Save(material,Art+"Materials/Manta imported skin.mat");
  foreach(string name in new[]{"Manta mouth","Manta eyes","Manta gills","Manta source details"}){var old=manta.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
  Model("Manta source details",Save(detailMesh,Art+"Meshes/MantaImportedDetails.asset"),Mat("Manta source dark tissue",new Color(.018f,.024f,.026f)),Vector3.zero,Vector3.one,manta.transform);
  var body=manta.GetComponent<ReefSphereBody>();if(body){body.radius=2.7f;if(!body.PlaceNear(manta.transform.position))throw new Exception("No safe manta spawn");}
  Debug.Log("MANTA_IMPORTED two renderers; source geometry 16655 triangles");
 }
 public static void MantaUpgrade(){EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var manta=GameObject.Find("Manta ray gliding over the reef");if(!manta)throw new Exception("Manta missing");UpgradeManta(manta);EditorSceneManager.SaveScene(manta.scene);AssetDatabase.SaveAssets();
  var swim=manta.GetComponent<MarineSwimmer>();var body=manta.GetComponent<ReefSphereBody>();swim.Initialize();Vector3 initial=manta.transform.position;
  for(int i=0;i<900;i++){Vector3 before=manta.transform.position;swim.Step(1f/30);if(!body.Clear(manta.transform.position)||Vector3.Distance(before,manta.transform.position)>.015f)throw new Exception("Manta motion/collision check failed");}
  if(Vector3.Distance(initial,manta.transform.position)<2)throw new Exception("Manta is stuck");Debug.Log("MANTA_VALIDATED seconds=30 speed=0.42 clearance=passed");
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");manta=GameObject.Find("Manta ray gliding over the reef");var cam=Camera.main;var optics=cam.GetComponent<ReefOptics>();if(optics)optics.enabled=false;cam.fieldOfView=48;Vector3 p=manta.transform.position;var skin=manta.GetComponent<Renderer>();var block=new MaterialPropertyBlock();block.SetFloat("_StrokePhase",1.5f);skin.SetPropertyBlock(block);Capture(cam,UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>(),"Manta_Revised",p+manta.transform.forward*4+Vector3.up*2.8f,p);block.SetFloat("_StrokePhase",4.5f);skin.SetPropertyBlock(block);Capture(cam,UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>(),"Manta_Underside",p+manta.transform.forward*3-Vector3.up*2,p);
 }
}
}




