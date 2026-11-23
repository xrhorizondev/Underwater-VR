using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class CullingAnchorReview {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/CullingAnchorReview";
 static readonly HashSet<ulong> touched=new();
 static void Touch(GameObject go){touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(go).targetObjectId);foreach(var c in go.GetComponents<Component>())if(c)touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId);}
 static IEnumerable<Vector3> Points(Renderer renderer){if(renderer is SkinnedMeshRenderer skin)return OctopusRepairValidation.Points(skin);var filter=renderer.GetComponent<MeshFilter>();return filter&&filter.sharedMesh?filter.sharedMesh.vertices.Select(v=>filter.transform.TransformPoint(v)):Array.Empty<Vector3>();}
 static bool Ground(float x,float z,out float y){y=float.NegativeInfinity;foreach(var hit in Physics.RaycastAll(new Vector3(x,40,z),Vector3.down,180,1<<8,QueryTriggerInteraction.Ignore)){
  string name=hit.collider.name;if(!name.StartsWith("Continuous seabed")&&!name.Contains("reef bed"))continue;y=Mathf.Max(y,hit.point.y);
 }return !float.IsNegativeInfinity(y);}
 public static void Run(){
  Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(Scene);var report=new System.Text.StringBuilder();var ship=GameObject.Find("Supplied ship on trench floor");string shipPose=EditorJsonUtility.ToJson(ship.transform);Physics.SyncTransforms();
  foreach(var anchor in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Weathered supplied anchor - ")).ToArray()){
   Touch(anchor.gameObject);anchor.localScale*=1.2f;anchor.rotation=Quaternion.Euler(90,anchor.eulerAngles.y,0);
   // Lay the exported XY anchor/chain plane onto the seabed. Ground the whole
   // rotated mesh against saved terrain, rather than its analytic approximation.
   var support=anchor.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>anchor.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();float required=float.NegativeInfinity;
   foreach(var p in support){var world=anchor.TransformPoint(p);if(Ground(world.x,world.z,out float y))required=Mathf.Max(required,y-world.y);}
   if(float.IsNegativeInfinity(required))throw new Exception("No saved seabed under anchor");anchor.position+=Vector3.up*(required-.015f);Physics.SyncTransforms();
   float minGap=float.PositiveInfinity;foreach(var p in support){var world=anchor.TransformPoint(p);if(Ground(world.x,world.z,out float y))minGap=Mathf.Min(minGap,world.y-y);}report.AppendLine(anchor.name+" scale="+anchor.localScale+" closestAnchorGap="+minGap);
  }
  var visited=new HashSet<Mesh>();int fishBounds=0;
  foreach(var detail in UnityEngine.Object.FindObjectsByType<ReefFishDetail>(FindObjectsSortMode.None))foreach(var mesh in new[]{detail.full,detail.medium,detail.distant})if(mesh&&visited.Add(mesh)){
   mesh.RecalculateBounds();var b=mesh.bounds;b.Encapsulate(new Bounds(Vector3.zero,new Vector3(.44f,.65f,1.1f)));b.Expand(new Vector3(.22f,.04f,.02f));mesh.bounds=b;EditorUtility.SetDirty(mesh);fishBounds++;
  }
  foreach(var agent in UnityEngine.Object.FindObjectsByType<FishAgent>(FindObjectsSortMode.None)){
   var filter=agent.GetComponent<MeshFilter>();if(filter&&filter.sharedMesh&&visited.Add(filter.sharedMesh)){var b=filter.sharedMesh.bounds;b.Expand(new Vector3(.22f,.04f,.02f));filter.sharedMesh.bounds=b;EditorUtility.SetDirty(filter.sharedMesh);}
  }
  int cutoffs=0;
  foreach(var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))if(group.GetComponent<FishAgent>()||group.GetComponent<ReefFishDetail>()){
   Touch(group.gameObject);var levels=group.GetLODs();if(levels.Length>0){levels[levels.Length-1].screenRelativeTransitionHeight=Mathf.Min(levels[levels.Length-1].screenRelativeTransitionHeight,.00065f);group.SetLODs(levels);group.RecalculateBounds();cutoffs++;}
  }
  var water=GameObject.Find("Moving underside of ocean surface");Touch(water);var waterMesh=water.GetComponent<MeshFilter>().sharedMesh;var bounds=waterMesh.bounds;bounds.center=new Vector3(bounds.center.x,-25,bounds.center.z);bounds.size=new Vector3(bounds.size.x,150,bounds.size.z);waterMesh.bounds=bounds;EditorUtility.SetDirty(waterMesh);water.GetComponent<Renderer>().allowOcclusionWhenDynamic=false;water.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  if(water.GetComponent<LODGroup>())throw new Exception("Water must not use LOD cutoff");
  if(EditorJsonUtility.ToJson(ship.transform)!=shipPose)throw new Exception("Ship moved");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"/TouchedIds.txt",string.Join("\n",touched));File.WriteAllText(Output+"/MeshPaths.txt",string.Join("\n",visited.Select(m=>AssetDatabase.GetAssetPath(m)).Append(AssetDatabase.GetAssetPath(waterMesh)).Where(p=>!string.IsNullOrEmpty(p)).Distinct()));report.AppendLine("Fish expanded meshes="+fishBounds+" fish cutoff repairs="+cutoffs+" surface="+AssetDatabase.GetAssetPath(waterMesh));File.WriteAllText(Output+"/Repairs.txt",report.ToString());Validate();Capture();FishReview();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var camera=Camera.main;camera.farClipPlane=60;camera.nearClipPlane=.05f;camera.fieldOfView=90;camera.aspect=1;var surface=GameObject.Find("Moving underside of ocean surface").GetComponent<Renderer>();int views=0;
  foreach(var zone in OceanWorld.Centers)foreach(float pitch in new[]{-89f,-60f,-30f})foreach(float yaw in new[]{0f,90f,180f,270f}){camera.transform.position=zone+Vector3.up*1.6f;camera.transform.rotation=Quaternion.Euler(pitch,yaw,0);if(!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),surface.bounds))throw new Exception("Top surface frustum rejected");views++;}
  foreach(var detail in UnityEngine.Object.FindObjectsByType<ReefFishDetail>(FindObjectsSortMode.None))foreach(var mesh in new[]{detail.full,detail.medium,detail.distant}){var v=mesh.vertices;for(int i=0;i<v.Length;i++){float tail=Mathf.Clamp01((.28f-v[i].z)/.78f);foreach(float sign in new[]{-1f,1f}){var p=v[i];p.x+=sign*tail*tail*.1f;if(!mesh.bounds.Contains(p))throw new Exception("Fish swim escapes culling bounds: "+mesh.name);}}}
  RealisticReefBuilder.ValidateScene();Debug.Log("CULLING_ANCHOR_VALIDATED upwardViews="+views+" paddedFishBounds=true scene=true");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;camera.farClipPlane=60;camera.nearClipPlane=.05f;camera.fieldOfView=90;camera.aspect=1.4f;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();
  foreach(int zone in new[]{0,4,7}){camera.transform.position=OceanWorld.Centers[zone]+Vector3.up*1.6f;camera.transform.rotation=Quaternion.Euler(-60,0,0);atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,Output+"/SurfaceZone"+zone+".png");}
  foreach(var anchor in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Weathered supplied anchor - ")).ToArray()){var b=anchor.GetComponentsInChildren<Renderer>()[0].bounds;camera.transform.position=b.center+new Vector3(3,1.3f,3);camera.transform.LookAt(b.center);camera.fieldOfView=45;atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,Output+"/Anchor"+(anchor.name.Contains("Rays")?7:2)+".png");}
 }
 public static void FishReview(){
  EditorSceneManager.OpenScene(Scene);var actors=new Dictionary<string,Transform>();foreach(var agent in UnityEngine.Object.FindObjectsByType<FishAgent>(FindObjectsSortMode.None)){
   var renderer=agent.GetComponent<Renderer>();if(!renderer||!renderer.sharedMaterial)continue;var life=agent.GetComponent<SeaLife>();string key=life?life.kind:agent.name;key=key+" | "+renderer.sharedMaterial.name;if(!actors.ContainsKey(key))actors[key]=agent.transform;
  }
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(new[]{"Flame angelfish","Butterflyfish","Seabass","Clownfish","Lumen","Anglerfish","Lionfish","Grouper","Moray","Reef shark","Manta","Stingray","Turtle","Dolphin"}.Contains(life.kind)&&!actors.Keys.Any(k=>k.StartsWith(life.kind+" | ")))actors[life.kind+" | encounter"]=life.transform;
  Directory.CreateDirectory(Output+"/Fish");var report=new System.Text.StringBuilder();int index=0;RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);Shader.SetGlobalFloat("_ReefLight",1);
  var previews=new List<GameObject>();var camera=new GameObject("Fish inspection camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.06f,.08f);camera.orthographic=true;camera.orthographicSize=.68f;camera.nearClipPlane=.01f;camera.farClipPlane=30;
  foreach(var pair in actors.OrderBy(p=>p.Key)){
   foreach(var preview in previews)preview.SetActive(false);
   var actor=pair.Value;var root=new GameObject("Fish inspection "+index);root.transform.position=new Vector3(800,0,0);var renderers=actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();bool first=true;Bounds b=default;
   foreach(var r in renderers){var points=Points(r).Select(p=>actor.InverseTransformPoint(p)).ToArray();if(points.Length==0)continue;foreach(var p in points){if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}Mesh source=r is SkinnedMeshRenderer sk?sk.sharedMesh:r.GetComponent<MeshFilter>().sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);mesh.ClearBlendShapes();mesh.vertices=points;mesh.RecalculateBounds();mesh.RecalculateNormals();var part=new GameObject(r.name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;report.AppendLine(index+" "+pair.Key+" renderer="+r.name+" material="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+" shader="+m.shader.name+" texture="+(m.mainTexture?AssetDatabase.GetAssetPath(m.mainTexture):"none"):"MISSING")));
   }
   if(first){UnityEngine.Object.DestroyImmediate(root);continue;}float size=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));root.transform.localScale=Vector3.one/size;root.transform.position-=b.center/size;camera.transform.position=new Vector3(802,.1f,0);camera.transform.LookAt(new Vector3(800,0,0));ReefCorrectionAudit.Capture(camera,Output+"/Fish/"+index.ToString("00")+".png");
   camera.transform.position=new Vector3(798,.1f,0);camera.transform.LookAt(new Vector3(800,0,0));ReefCorrectionAudit.Capture(camera,Output+"/Fish/"+index.ToString("00")+"_Reverse.png");
   int col=index%4,row=index/4;root.transform.position+=new Vector3(0,-row*1.6f,col*1.7f);previews.Add(root);index++;
  }
  foreach(var preview in previews)preview.SetActive(true);int rows=(index+3)/4;camera.transform.position=new Vector3(815,-(rows-1)*.8f,2.55f);camera.transform.LookAt(new Vector3(800,-(rows-1)*.8f,2.55f));camera.orthographicSize=Mathf.Max(rows*.8f,2.7f);ReefCorrectionAudit.Capture(camera,Output+"/FishOverview.png");File.WriteAllText(Output+"/FishInventory.txt",report.ToString());Debug.Log("FISH_TEXTURE_REVIEW_RENDERED variants="+index);
 }
}
}
