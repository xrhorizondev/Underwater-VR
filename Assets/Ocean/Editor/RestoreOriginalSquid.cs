using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class RestoreOriginalSquid {
 public static void Run(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/UnderwaterArt/Meshes/GiantSquid.asset");var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/UnderwaterArt/ReviewCorrections/Squid moving skin.mat");if(!mesh||!material)throw new Exception("Original squid assets missing");int count=0;foreach(var squid in UnityEngine.Object.FindObjectsByType<SquidSwimmer>(FindObjectsSortMode.None)){var child=squid.transform.Find("Approved Blender squid");if(child)UnityEngine.Object.DestroyImmediate(child.gameObject);var filter=squid.GetComponent<MeshFilter>();if(!filter)filter=squid.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=squid.GetComponent<MeshRenderer>();if(!renderer)renderer=squid.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.enabled=true;squid.clearancePadding=0;foreach(var capsule in squid.GetComponentsInChildren<CapsuleCollider>())if(capsule.gameObject.layer==9){capsule.radius=1.7f;capsule.height=20;capsule.center=new Vector3(0,0,-3);}count++;}if(count!=2)throw new Exception("Expected two squid encounters");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ReefDaySix.Validate();Debug.Log("ORIGINAL_SQUID_RESTORED instances=2 BlenderAssetsPreserved=true");}
}
}
