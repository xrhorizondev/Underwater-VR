using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Underwater.Editor {
[InitializeOnLoad]public static class CombJellyPreview {
 static CombJellyPreview(){if(SessionState.GetBool("CombBodyPreviewPending",false))EditorApplication.playModeStateChanged+=Entered;}
 [MenuItem("Underwater/Comb jelly/Reload saved rig")]
 public static void Reload(){if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play mode before reloading the saved rig.");return;}var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene,"Assets/Scenes/BeforeCombReload_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");Inspect();}
 [MenuItem("Underwater/Comb jelly/Inspect scene")]
 public static void Inspect(){var text=new StringBuilder();text.AppendLine("Playing="+EditorApplication.isPlaying);foreach(var a in Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Comb jelly")){var s=a.GetComponentInChildren<SkinnedMeshRenderer>();var m=a.GetComponent<JellyReferenceMotion>();text.AppendLine(a.name+" pos="+a.transform.position+" bones="+s.bones.Length+" bindposes="+s.sharedMesh.bindposes.Length+" crown="+s.bones.Any(b=>b&&b.name=="BodyCrown")+" lods="+a.GetComponentsInChildren<LODGroup>().Length+" active="+s.gameObject.activeInHierarchy+" enabled="+s.enabled+" bounds="+s.bounds+" clip="+(m.player&&m.player.clip?m.player.clip.name:"missing"));}Directory.CreateDirectory("Logs");File.WriteAllText("Logs/source-comb-snapshot.txt",text.ToString());Debug.Log("SOURCE_COMB_SNAPSHOT_WRITTEN");}
 [MenuItem("Underwater/Comb jelly/Preview body in Play mode")]
 public static void Preview(){if(!EditorApplication.isPlaying){SessionState.SetBool("CombBodyPreviewPending",true);EditorApplication.playModeStateChanged-=Entered;EditorApplication.playModeStateChanged+=Entered;EditorApplication.EnterPlaymode();return;}Position();}
 static void Entered(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode)return;SessionState.SetBool("CombBodyPreviewPending",false);EditorApplication.playModeStateChanged-=Entered;EditorApplication.delayCall+=Position;}
 static void Position(){if(!EditorApplication.isPlaying)return;if(!Diver.Instance){EditorApplication.delayCall+=Position;return;}var a=Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Comb jelly").OrderBy(a=>(a.transform.position-OceanWorld.Centers[3]).sqrMagnitude).First();var skin=a.GetComponentInChildren<SkinnedMeshRenderer>();var d=Object.FindFirstObjectByType<Diver>();d.ShowCreatureFacts=false;var camera=Camera.main;typeof(Diver).GetMethod("SetMenu",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(d,new object[]{false});var target=skin.bounds.center;var offset=camera.transform.position-d.transform.position;d.GetComponent<DiverCollisionMotor>().TryTeleport(target-Vector3.forward*1.5f-offset);camera.transform.LookAt(target);Selection.activeGameObject=a.gameObject;Inspect();Debug.Log("COMB_BODY_GAME_PREVIEW_READY");}
}
}
