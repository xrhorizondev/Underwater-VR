using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace Underwater.Editor {
public static class DolphinVisibleAudit {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
 public static void Run(){EditorSceneManager.OpenScene(Scene);var text=new System.Text.StringBuilder();foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 var skin=r as SkinnedMeshRenderer;var filter=r.GetComponent<MeshFilter>();var mesh=skin?skin.sharedMesh:filter?filter.sharedMesh:null;if(!mesh)continue;var asset=AssetDatabase.GetAssetPath(mesh);if(!asset.ToLower().Contains("dolphin")&&!mesh.name.ToLower().Contains("dolphin")&&!r.name.ToLower().Contains("dolphin"))continue;
 text.AppendLine("RENDER "+Path(r.transform)+" pos="+r.transform.position+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" mesh="+asset+" skin="+(bool)skin);var life=r.GetComponentInParent<SeaLife>();if(life)text.AppendLine("ACTOR "+life.name+" kind="+life.kind+" wreckEnabled="+(life.GetComponent<WreckCreature>()?life.GetComponent<WreckCreature>().enabled:false)+" driver="+(bool)life.GetComponent<DolphinSwimController>());
 foreach(var lod in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++)if(levels[i].renderers.Contains(r))text.AppendLine("LOD OWNER "+Path(lod.transform)+" enabled="+lod.enabled+" level="+i+" threshold="+levels[i].screenRelativeTransitionHeight);}
 }Directory.CreateDirectory("Renders/UserDolphinIssue");File.WriteAllText("Renders/UserDolphinIssue/RenderAudit.txt",text.ToString());Debug.Log("DOLPHIN_VISIBLE_AUDITED\n"+text);}
}
}
