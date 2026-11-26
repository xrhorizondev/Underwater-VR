using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater.Editor {
public static class OctopusRestRepair {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/OctopusRestRepair/";
 static MeshCollider[] floors;
 static float Floor(Vector3 p){float y=float.NegativeInfinity;foreach(var f in floors)if(f.Raycast(new Ray(new Vector3(p.x,40,p.z),Vector3.down),out var h,180))y=Mathf.Max(y,h.point.y);return y;}
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);Physics.SyncTransforms();
  var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(a=>a.kind=="Octopus");var ground=actor.GetComponent<OctopusGroundClearance>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var player=skin.GetComponentInParent<Animation>();player.clip.SampleAnimation(player.gameObject,0);
  // Preserve the imported anatomy and motion. Fit a shallow, smoothly blended
  // sand resting spot beneath it, capped below the complete animation footprint.
  var center=actor.transform.position;var supports=ground.supports.Select(p=>p+center).ToArray();float maxRise=0;int meshes=0;
  foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh&&f.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray()){
   var collider=filter.GetComponent<MeshCollider>();if(!collider||!collider.bounds.Intersects(new Bounds(center,new Vector3(7,20,7))))continue;
   var source=filter.sharedMesh;var points=source.vertices.Select(filter.transform.TransformPoint).ToArray();var uv=source.uv;var colors=source.colors;var vertices=new List<Vector3>();var uvs=new List<Vector2>();var shades=new List<Color>();var triangles=new List<int>[source.subMeshCount];var cache=new Dictionary<Vector3,int>();var elevated=new HashSet<int>();
   int Vertex(Vector3 p,Vector2 tex,Color color){
    if(cache.TryGetValue(p,out int existing))return existing;var world=p;float d=Vector2.Distance(new Vector2(world.x,world.z),new Vector2(center.x,center.z));
    float rise=.45f*Mathf.Exp(-d*d/(2*1.65f*1.65f))*Mathf.SmoothStep(1,0,Mathf.InverseLerp(2.3f,3.2f,d));float cap=float.PositiveInfinity;
    foreach(var support in supports)if(Mathf.Abs(support.x-world.x)<ground.footCell+.14f&&Mathf.Abs(support.z-world.z)<ground.footCell+.14f)cap=Mathf.Min(cap,support.y-.005f);
    world.y=Mathf.Max(p.y,Mathf.Min(p.y+rise,cap));maxRise=Mathf.Max(maxRise,world.y-p.y);int index=vertices.Count;if(world.y>p.y+.000001f)elevated.Add(index);cache.Add(p,index);vertices.Add(filter.transform.InverseTransformPoint(world));uvs.Add(tex);shades.Add(color);return index;
   }
   void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc,Color ca,Color cb,Color cc,int sub,int depth){
    var min=Vector3.Min(a,Vector3.Min(b,c));var max=Vector3.Max(a,Vector3.Max(b,c));bool near=min.x<center.x+3.3f&&max.x>center.x-3.3f&&min.z<center.z+3.3f&&max.z>center.z-3.3f;
    if(near&&depth<7&&Mathf.Max((a-b).magnitude,(b-c).magnitude,(c-a).magnitude)>.16f){var ab=(a+b)*.5f;var bc=(b+c)*.5f;var ac=(a+c)*.5f;var uab=(ua+ub)*.5f;var ubc=(ub+uc)*.5f;var uac=(ua+uc)*.5f;var cab=(ca+cb)*.5f;var cbc=(cb+cc)*.5f;var cac=(ca+cc)*.5f;Triangle(a,ab,ac,ua,uab,uac,ca,cab,cac,sub,depth+1);Triangle(ab,b,bc,uab,ub,ubc,cab,cb,cbc,sub,depth+1);Triangle(ac,bc,c,uac,ubc,uc,cac,cbc,cc,sub,depth+1);Triangle(ab,bc,ac,uab,ubc,uac,cab,cbc,cac,sub,depth+1);return;}
    triangles[sub].Add(Vertex(a,ua,ca));triangles[sub].Add(Vertex(b,ub,cb));triangles[sub].Add(Vertex(c,uc,cc));
   }
   for(int sub=0;sub<source.subMeshCount;sub++){triangles[sub]=new List<int>();var indices=source.GetTriangles(sub);for(int j=0;j<indices.Length;j+=3){int a=indices[j],b=indices[j+1],c=indices[j+2];Triangle(points[a],points[b],points[c],uv.Length>0?uv[a]:Vector2.zero,uv.Length>0?uv[b]:Vector2.zero,uv.Length>0?uv[c]:Vector2.zero,colors.Length>0?colors[a]:Color.white,colors.Length>0?colors[b]:Color.white,colors.Length>0?colors[c]:Color.white,sub,0);}}
   var mesh=new Mesh{name=source.name+" octopus resting sand",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(shades);mesh.subMeshCount=triangles.Length;for(int sub=0;sub<triangles.Length;sub++)mesh.SetTriangles(triangles[sub],sub);mesh.RecalculateNormals();mesh.RecalculateBounds();
   string path="Assets/UnderwaterArt/FeedbackRepair/OctopusRestingSand_"+meshes+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;
   // Retain the original extended collision bed. The visible sand mesh does
   // not cover all of that bed's outer safety margin.
   var supportIndices=new List<int>();foreach(var list in triangles)for(int j=0;j<list.Count;j+=3)if(elevated.Contains(list[j])||elevated.Contains(list[j+1])||elevated.Contains(list[j+2])){supportIndices.Add(list[j]);supportIndices.Add(list[j+1]);supportIndices.Add(list[j+2]);}
   for(int j=0;j<supportIndices.Count;j+=3){var a=filter.transform.TransformPoint(vertices[supportIndices[j]]);var b=filter.transform.TransformPoint(vertices[supportIndices[j+1]]);var c=filter.transform.TransformPoint(vertices[supportIndices[j+2]]);if(Vector3.Cross(b-a,c-a).y<0){int swap=supportIndices[j+1];supportIndices[j+1]=supportIndices[j+2];supportIndices[j+2]=swap;}}var used=supportIndices.Distinct().ToArray();var map=used.Select((old,i)=>new{old,i}).ToDictionary(v=>v.old,v=>v.i);var supportMesh=new Mesh{name="seabed octopus resting support",indexFormat=IndexFormat.UInt32};supportMesh.vertices=used.Select(i=>vertices[i]).ToArray();supportMesh.triangles=supportIndices.Select(i=>map[i]).ToArray();supportMesh.RecalculateBounds();string supportPath="Assets/UnderwaterArt/FeedbackRepair/OctopusRestingSupport.asset";var oldSupport=AssetDatabase.LoadAssetAtPath<Mesh>(supportPath);if(oldSupport){EditorUtility.CopySerialized(supportMesh,oldSupport);UnityEngine.Object.DestroyImmediate(supportMesh);supportMesh=oldSupport;EditorUtility.SetDirty(supportMesh);}else AssetDatabase.CreateAsset(supportMesh,supportPath);
   var support=new GameObject("Octopus resting sand support");support.layer=8;support.transform.SetParent(filter.transform,false);support.AddComponent<MeshCollider>().sharedMesh=supportMesh;meshes++;
  }
  Physics.SyncTransforms();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"Repair.txt","Preserved original octopus skeleton and animation. Fitted local sand below complete animation footprint. Meshes="+meshes+" maximum sand rise="+maxRise+"m.\n");OctopusGroundValidation.Run();Inspect();Debug.Log("OCTOPUS_REST_REPAIRED");
 }
 public static void Inspect(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);Physics.SyncTransforms();floors=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.sharedMesh&&c.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(a=>a.kind=="Octopus");var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var player=skin.GetComponentInParent<Animation>();var report=new StringBuilder();var baked=new Mesh();
  for(int frame=0;frame<8;frame++){player.clip.SampleAnimation(player.gameObject,player.clip.length*frame/8);skin.BakeMesh(baked,true);var points=baked.vertices.Select(skin.transform.TransformPoint).ToArray();var bottom=points.GroupBy(p=>new Vector2Int(Mathf.FloorToInt(p.x/.12f),Mathf.FloorToInt(p.z/.12f))).Select(g=>g.OrderBy(p=>p.y).First()).ToArray();var gaps=bottom.Select(p=>p.y-Floor(p)).OrderBy(v=>v).ToArray();report.AppendLine("Phase="+frame+" min="+gaps[0]+" median="+gaps[gaps.Length/2]+" p10="+gaps[gaps.Length/10]+" max="+gaps.Last());}
  File.WriteAllText(Output+"Inspection.txt",report.ToString());player.clip.SampleAnimation(player.gameObject,0);var camera=Camera.main;camera.farClipPlane=160;camera.fieldOfView=45;var center=skin.bounds.center;
  foreach(int side in new[]{0,2}){var direction=Quaternion.Euler(0,side*90,0)*Vector3.forward;var p=center+direction*5;p.y=Floor(p)+.12f;camera.transform.position=p;camera.transform.LookAt(new Vector3(center.x,Floor(actor.transform.position)+.25f,center.z));ReefCorrectionAudit.Capture(camera,Output+"Ground_"+side+".png");}
  camera.transform.position=center+new Vector3(3,2.5f,4);camera.transform.LookAt(center);ReefCorrectionAudit.Capture(camera,Output+"Overview.png");UnityEngine.Object.DestroyImmediate(baked);Debug.Log("OCTOPUS_REST_INSPECTED");
 }
}
}
