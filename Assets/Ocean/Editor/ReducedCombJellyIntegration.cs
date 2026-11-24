using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReducedCombJellyIntegration {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Folder="Assets/UnderwaterArt/CombJellyComparison/";
 static SeaLife[] Actors()=>UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Comb jelly").ToArray();
 static Vector3[] Points(SkinnedMeshRenderer s){return OctopusRepairValidation.Points(s);}
 static Bounds BoundsOf(Transform root,IEnumerable<Renderer> skins){bool first=true;Bounds b=default;foreach(var r in skins){Vector3[] p=r is SkinnedMeshRenderer s?Points(s):r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>r.transform.TransformPoint(v)).ToArray();foreach(var w in p){var v=root.InverseTransformPoint(w);if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);}}if(first)throw new Exception("Missing jelly mesh");return b;}
 public static void Run(){
 var scene=EditorSceneManager.OpenScene(Scene);var actors=Actors();if(actors.Length==0)throw new Exception("No comb jellies");
 var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Tissue.mat");var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Reduced.fbx");var clip=AssetDatabase.LoadAllAssetsAtPath(Folder+"Reduced.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__"));if(!mat||!asset||ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Missing reduced jelly assets");
 var protectedObjects=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>!t.GetComponentInParent<SeaLife>()||t.GetComponentInParent<SeaLife>().kind!="Comb jelly").SelectMany(t=>t.GetComponents<Component>()).Where(c=>c).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
 int n=0;var report=new System.Text.StringBuilder();
 foreach(var actor in actors){
 var root=actor.transform;var components=root.GetComponents<Component>().ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));var colliders=root.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
 var old=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();var previous=root.Find("Prepared animated model");if(previous&&previous.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("Visual contains gameplay collider");var oldBounds=BoundsOf(root,old);var guard=actor.GetComponent<ReefAnimalClearance>();if(!guard)throw new Exception("Missing clearance owner");
 var wrapper=new GameObject("Reduced comb jelly visual");wrapper.transform.SetParent(root,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(wrapper.transform,false);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
 var player=model.GetComponent<Animation>();if(!player)player=model.AddComponent<Animation>();player.AddClip(clip,clip.name);player.clip=clip;player.playAutomatically=true;player.wrapMode=WrapMode.Loop;player.cullingType=AnimationCullingType.AlwaysAnimate;
 var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMaterial=mat;skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skin.receiveShadows=false;skin.updateWhenOffscreen=false;
 // Fit the entire pulse inside the existing visual and clearance envelope.
 Bounds envelope=default;bool first=true;for(int f=0;f<=24;f++){clip.SampleAnimation(model,clip.length*f/24);foreach(var w in Points(skin)){var p=root.InverseTransformPoint(w);if(first){envelope=new Bounds(p,Vector3.zero);first=false;}else envelope.Encapsulate(p);}}
 var available=oldBounds;available.SetMinMax(Vector3.Max(oldBounds.min,guard.animatedBody.min),Vector3.Min(oldBounds.max,guard.animatedBody.max));if(available.size.x<=0||available.size.y<=0||available.size.z<=0)throw new Exception("Invalid old envelope");float scale=Mathf.Min(available.size.x/envelope.size.x,available.size.y/envelope.size.y,available.size.z/envelope.size.z)*.985f;wrapper.transform.localScale=Vector3.one*scale;wrapper.transform.localPosition=available.center-envelope.center*scale;
 Bounds skinBounds=default;first=true;for(int f=0;f<=24;f++){clip.SampleAnimation(model,clip.length*f/24);foreach(var w in Points(skin)){var p=(skin.rootBone?skin.rootBone:skin.transform).InverseTransformPoint(w);if(first){skinBounds=new Bounds(p,Vector3.zero);first=false;}else skinBounds.Encapsulate(p);}}skinBounds.Expand(.015f);skin.localBounds=skinBounds;clip.SampleAnimation(model,0);
 foreach(var r in old)r.enabled=false;if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);wrapper.name="Prepared animated model";var motion=model.AddComponent<ReefImportedAnimation>();motion.speed=.7f;motion.cycleOffset=(n++*.618034f)%1;
 var lod=wrapper.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.015f,new Renderer[]{skin})});lod.RecalculateBounds();
 foreach(var pair in components){if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value){if(pair.Key is Renderer)continue;throw new Exception("Root gameplay changed: "+pair.Key.GetType().Name);}}
 foreach(var pair in colliders)if(!pair.Key||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Collider changed");
 report.AppendLine(actor.name+" at "+root.position+" fitted scale="+scale+" old size="+oldBounds.size+" new size="+BoundsOf(root,new[]{skin}).size);
 }
 foreach(var pair in protectedObjects)if(!pair.Key||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Unrelated object changed: "+pair.Key);
 EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("Renders/ReducedCombJellyIntegration");File.WriteAllText("Renders/ReducedCombJellyIntegration/Placement.txt",report.ToString());Validate();Capture();Debug.Log("REDUCED_COMB_JELLIES_INTEGRATED count="+n);
 }
 public static void Validate(){
 EditorSceneManager.OpenScene(Scene);var actors=Actors();float minGap=float.MaxValue,minTravel=float.MaxValue;int checks=0;
 foreach(var actor in actors){var wrapper=actor.transform.Find("Prepared animated model");var skin=wrapper.GetComponentInChildren<SkinnedMeshRenderer>();var player=wrapper.GetComponentInChildren<Animation>();var motion=wrapper.GetComponentInChildren<ReefImportedAnimation>();if(skin.sharedMesh.triangles.Length/3!=21999||skin.bones.Length!=5||skin.sharedMaterial!=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Tissue.mat")||!motion)throw new Exception("Wrong reduced asset");if(actor.danger||actor.consequence||!actor.observationOnly)throw new Exception("Comb jelly safety changed");if(actor.GetComponentsInChildren<Renderer>().Count(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer))!=1)throw new Exception("Duplicate visible jelly");
 var guard=actor.GetComponent<ReefAnimalClearance>();for(int f=0;f<=16;f++){player.clip.SampleAnimation(player.gameObject,player.clip.length*f/16);foreach(var w in Points(skin)){var p=actor.transform.InverseTransformPoint(w);var allowed=guard.animatedBody;allowed.Expand(.001f);if(!allowed.Contains(p))throw new Exception("Pulse escaped clearance envelope");checks++;}}player.clip.SampleAnimation(player.gameObject,0);
 var home=actor.transform.position;float travel=0;for(int f=0;f<600;f++){var prev=actor.transform.position;actor.GetComponent<DeepWaterEncounter>().Step(.05f,4,home+Vector3.forward*4,5);guard.Enforce();if(!guard.RequiredHeight(actor.transform.position,actor.transform.rotation,out float h))throw new Exception("No seabed bounds");minGap=Mathf.Min(minGap,actor.transform.position.y-h);if(actor.transform.position.y<h-.001f)throw new Exception("Seabed overlap");travel+=Vector3.Distance(prev,actor.transform.position);}minTravel=Mathf.Min(minTravel,travel);
 }
 if(minTravel<.1f)throw new Exception("Drift stalled");File.WriteAllText("Renders/ReducedCombJellyIntegration/Validation.txt",actors.Length+" comb jellies replaced; 21,999 triangles and 5 bones each.\nAll pulse vertices within existing clearance envelope: "+checks+" checks.\n30-second drift per jelly; minimum travel="+minTravel+" m; minimum seabed clearance margin="+minGap+" m.\nActor transforms, root gameplay components, colliders and unrelated scene objects preserved. Information-only behavior retained. No Quest FPS measurement.\n");EditorSceneManager.OpenScene(Scene);Debug.Log("REDUCED_COMB_JELLY_VALIDATED count="+actors.Length);
 }
 public static void Capture(){EditorSceneManager.OpenScene(Scene);var actor=Actors().OrderBy(a=>(a.transform.position-new Vector3(0,3,110)).sqrMagnitude).First();var skin=actor.transform.Find("Prepared animated model").GetComponentInChildren<SkinnedMeshRenderer>();var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;camera.transform.position=skin.bounds.center+actor.transform.right*1.5f;camera.transform.LookAt(skin.bounds.center);camera.fieldOfView=45;camera.nearClipPlane=.01f;ReefCorrectionAudit.Capture(camera,"Renders/ReducedCombJellyIntegration/InHabitat.png");}
}
}

