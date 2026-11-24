using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class RemoveImportedSmallFish {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static readonly string[] Folders={"Assets/UnderwaterArt/SuppliedReview/FlameAngel","Assets/UnderwaterArt/SuppliedReview/ReefFish","Assets/UnderwaterArt/HabitatExpansion/Clownfish","Assets/UnderwaterArt/HabitatExpansion/Seabass"};
 static readonly string[] Keys={"FlameAngel","ReefFish","Clownfish","Seabass"};
 static bool FishAsset(string path)=>Folders.Any(f=>path==f||path.StartsWith(f+"/"))||Keys.Any(k=>path=="Assets/UnderwaterArt/LeadReview/"+k+"Close.asset");
 public static void Run(){
 var scene=EditorSceneManager.OpenScene(Scene);
 var fish=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>f.sharedMesh&&FishAsset(AssetDatabase.GetAssetPath(f.sharedMesh))).Select(f=>f.GetComponentInParent<SeaLife>()?f.GetComponentInParent<SeaLife>().gameObject:f.gameObject).Distinct().ToArray();if(fish.Length==0)throw new Exception("No imported small fish found");
 var roots=fish.Select(f=>f.transform).ToArray();var protectedActors=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>!fish.Contains(a.gameObject)).ToDictionary(a=>a,a=>EditorJsonUtility.ToJson(a)+EditorJsonUtility.ToJson(a.transform));
 var names=fish.Select(f=>f.name+" at "+f.transform.position).ToArray();foreach(var f in fish)UnityEngine.Object.DestroyImmediate(f);
 foreach(var contact in UnityEngine.Object.FindObjectsByType<ReefSmallFishContact>(FindObjectsInactive.Include,FindObjectsSortMode.None))contact.smallFishSurfaces=contact.smallFishSurfaces.Where(c=>c).ToArray();
 foreach(var school in UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(school.fish!=null)school.fish=school.fish.Where(f=>f).ToArray();if(school.name.StartsWith("Supplied school |")&&school.transform.childCount==0)UnityEngine.Object.DestroyImmediate(school.gameObject);}
 foreach(var pair in protectedActors)if(!pair.Key||EditorJsonUtility.ToJson(pair.Key)+EditorJsonUtility.ToJson(pair.Key.transform)!=pair.Value)throw new Exception("Unrelated actor changed");
 EditorSceneManager.SaveScene(scene);
 var targets=Folders.Concat(Keys.Select(k=>"Assets/UnderwaterArt/LeadReview/"+k+"Close.asset")).ToArray();var targetAssets=AssetDatabase.GetAllAssetPaths().Where(FishAsset).ToHashSet();
 foreach(var other in AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p!=Scene))if(AssetDatabase.GetDependencies(other,true).Any(targetAssets.Contains))throw new Exception("Other scene uses removed fish: "+other);
 if(AssetDatabase.GetDependencies(Scene,true).Any(targetAssets.Contains))throw new Exception("Main scene still references fish asset");
 foreach(var path in targets)if(!AssetDatabase.DeleteAsset(path))throw new Exception("Cannot delete "+path);
 AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Scene);if(UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(f=>f.sharedMesh&&FishAsset(AssetDatabase.GetAssetPath(f.sharedMesh))))throw new Exception("Imported fish remain");
 if(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Count(a=>a.kind=="Comb jelly")!=25)throw new Exception("Comb jellies changed");
 Directory.CreateDirectory("Renders/RemovedSmallFish");File.WriteAllLines("Renders/RemovedSmallFish/Removed.txt",names);File.WriteAllLines("Renders/RemovedSmallFish/DeletedAssets.txt",targets);File.WriteAllText("Renders/RemovedSmallFish/Validation.txt","Removed "+fish.Length+" imported small fish; four species folders and four close-detail meshes deleted. No remaining scene dependencies on deleted assets. Other actors preserved, including 25 reduced comb jellies. Fish-school and small-fish contact references cleaned.\n");Debug.Log("IMPORTED_SMALL_FISH_REMOVED count="+fish.Length);
 }
}
}
