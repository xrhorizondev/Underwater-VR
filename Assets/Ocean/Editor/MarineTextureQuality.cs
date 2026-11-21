using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class MarineTextureQuality {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 [Serializable] public class Row {public string kind,renderer,material,shader;public string[] textures;public bool surfaceDepth;}
 [Serializable] public class Report {public int animalObjects,uniqueTextures,normalMaps;public Row[] surfaces;}
 static readonly HashSet<string> Featured=new HashSet<string>{"Lionfish","Dolphin","Reef shark","Grouper","Moray","Octopus","Turtle","Stingray","Manta"};
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);AssetDatabase.Refresh();
  // Rebind the extra eye submesh by its imported material name, preserving the
  // scene's existing bones, navigation, hazards and placement.
  var lion=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(a=>a.kind=="Lionfish");var model=lion.transform.Find("Prepared animated model").GetChild(0).gameObject;
  var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/UnderwaterArt/MarineRefresh/Lionfish/Lionfish.fbx");importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnderwaterArt/MarineRefresh/Lionfish/Lionfish.fbx").GetComponentInChildren<SkinnedMeshRenderer>();var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMesh=source.sharedMesh;
  var skinMethod=typeof(MarineModelRefresh).GetMethod("Skin",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
  skin.sharedMaterials=source.sharedMaterials.Select((m,i)=>(Material)skinMethod.Invoke(null,new object[]{"Lionfish",m.name,i})).ToArray();
  foreach(var material in skin.sharedMaterials){material.SetFloat("_BumpScale",material.name=="Lionfish_Eyes"?1:.45f);material.SetFloat("_Glossiness",material.name=="Lionfish_Eyes"?.82f:.36f);EditorUtility.SetDirty(material);}
  if(skin.sharedMaterials.Length!=3||!skin.sharedMaterials.Any(m=>m.name=="Lionfish_Eyes"))throw new Exception("Missing anatomical eyes");
  var envelopeMethod=typeof(MarineModelRefresh).GetMethod("Envelopes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);envelopeMethod.Invoke(null,new object[]{model,model.GetComponent<Animation>().clip});
  var guard=lion.GetComponent<ReefAnimalClearance>();guard.animatedBody=AnimalSeabedRepair.Envelope(lion.transform);guard.Enforce();EditorUtility.SetDirty(guard);
  // Genuine authored roughness and cavity maps, retained from the supplied ZIP.
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Dolphin"))foreach(var r in life.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled))foreach(var m in r.sharedMaterials){m.SetTexture("_RoughnessMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UnderwaterArt/MarineRefresh/Dolphin/textures/Dolphin_Roughness.png"));m.SetFloat("_UseRoughnessMap",1);m.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UnderwaterArt/MarineRefresh/Dolphin/textures/Dolphin_Occlusion.png"));EditorUtility.SetDirty(m);}
  var actors=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);var budgets=new Dictionary<string,int>();var normals=new HashSet<string>();var rows=new List<Row>();
  var roots=actors.Select(a=>(kind:a.kind,transform:a.transform)).Concat(UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None).Where(s=>!s.GetComponent<SeaLife>()).Select(s=>(kind:"Giant squid",transform:s.transform))).ToArray();
  foreach(var life in roots)foreach(var r in life.transform.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)))foreach(var m in r.sharedMaterials){
   if(!m||!m.shader||ShaderUtil.ShaderHasError(m.shader))throw new Exception("Broken animal material: "+life.kind);var paths=new List<string>();bool depth=false;
   for(int i=0;i<ShaderUtil.GetPropertyCount(m.shader);i++){if(ShaderUtil.GetPropertyType(m.shader,i)!=ShaderUtil.ShaderPropertyType.TexEnv)continue;var property=ShaderUtil.GetPropertyName(m.shader,i);var texture=m.GetTexture(property);if(!texture)continue;string path=AssetDatabase.GetAssetPath(texture);if(string.IsNullOrEmpty(path)||!(AssetImporter.GetAtPath(path) is TextureImporter))continue;int budget=Featured.Contains(life.kind)?2048:1024;budgets[path]=budgets.ContainsKey(path)?Mathf.Max(budgets[path],budget):budget;paths.Add(property+": "+path);if(property=="_BumpMap"){normals.Add(path);depth=true;}if(property=="_HeightMap")depth=true;
   }
   rows.Add(new Row{kind=life.kind,renderer=r.name,material=m.name,shader=m.shader.name,textures=paths.ToArray(),surfaceDepth=depth});
  }
  foreach(var pair in budgets){var imp=(TextureImporter)AssetImporter.GetAtPath(pair.Key);imp.GetSourceTextureWidthAndHeight(out int width,out int height);int max=Mathf.Min(pair.Value,Mathf.NextPowerOfTwo(Mathf.Max(width,height)));imp.maxTextureSize=max;imp.mipmapEnabled=true;imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=4;imp.textureCompression=TextureImporterCompression.CompressedHQ;
   if(normals.Contains(pair.Key)){imp.textureType=TextureImporterType.NormalMap;imp.sRGBTexture=false;}
   var android=imp.GetPlatformTextureSettings("Android");if(android.overridden){android.maxTextureSize=max;android.compressionQuality=100;imp.SetPlatformTextureSettings(android);}imp.SaveAndReimport();
  }
  Directory.CreateDirectory("Renders/MarineQuality");File.WriteAllText("Renders/MarineQuality/texture-audit.json",JsonUtility.ToJson(new Report{animalObjects=roots.Length,uniqueTextures=budgets.Count,normalMaps=normals.Count,surfaces=rows.ToArray()},true));
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MARINE_TEXTURE_QUALITY_SAVED actors="+actors.Length+" uniqueTextures="+budgets.Count+" normalMaps="+normals.Count+" lionfishTriangles="+skin.sharedMesh.triangles.Length/3);
  MarineModelRefresh.Regression();HabitatDocumentAudit.ValidatePlacementsAndShark();MarineModelRefresh.FinalVisualChecks();
 }
}}

