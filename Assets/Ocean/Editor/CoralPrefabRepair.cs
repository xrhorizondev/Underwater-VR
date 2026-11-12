using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater.Editor {
public static class CoralPrefabRepair {
 public const string Folder="Assets/UnderwaterArt/Corrections";
 public const string PrefabPath=Folder+"/Secale Coral.prefab";
 struct Vertex {public long key;public Vector3 p,n;public Vector2 uv;public Color color;public static Vertex Mix(Vertex a,Vertex b,float t)=>new Vertex{key=((Math.Min(a.key,b.key)+1)<<32)|(Math.Max(a.key,b.key)+1),p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t),color=Color.Lerp(a.color,b.color,t)};}
 public static int Apply(){
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var colonies=Array.FindAll(UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None),g=>g.name.StartsWith("Scanned Secale"));
  if(colonies.Length==0)throw new Exception("No Secale colonies");
  var canonical=Array.Find(colonies,g=>!g.GetComponent<SeaLife>());if(!canonical)throw new Exception("No ordinary coral prefab source");
  var corrected=new Dictionary<Mesh,Mesh>();bool alreadyClean=true;
  foreach(var mesh in canonical.GetComponentsInChildren<MeshFilter>()){
   var source=mesh.sharedMesh;if(corrected.ContainsKey(source))continue;
   if(AssetDatabase.GetAssetPath(source).StartsWith(Folder)&&AssetDatabase.GetAssetPath(source).Contains("_NoMount")){corrected.Add(source,source);continue;}
   alreadyClean=false;
   string path=Folder+"/"+source.name+"_NoMount.asset";
   var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   Matrix4x4 toRoot=canonical.transform.worldToLocalMatrix*mesh.transform.localToWorldMatrix;
   var clean=Clip(source,toRoot,.012f);
   if(existing){EditorUtility.CopySerialized(clean,existing);UnityEngine.Object.DestroyImmediate(clean);EditorUtility.SetDirty(existing);clean=existing;}else AssetDatabase.CreateAsset(clean,path);
   corrected.Add(source,clean);
  }
  foreach(var colony in colonies){
   foreach(var filter in colony.GetComponentsInChildren<MeshFilter>())if(corrected.TryGetValue(filter.sharedMesh,out var clean)){filter.sharedMesh=clean;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);EditorUtility.SetDirty(filter);}
   FitCollider(colony);
  }
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
  if(!prefab||!alreadyClean){var root=canonical.transform;Vector3 position=root.localPosition,scale=root.localScale;Quaternion rotation=root.localRotation;
   root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;root.localScale=Vector3.one;
   prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,PrefabPath,InteractionMode.AutomatedAction);
   root.localPosition=position;root.localRotation=rotation;root.localScale=scale;PrefabUtility.RecordPrefabInstancePropertyModifications(root);
  }
  var settings=new ConvertToPrefabInstanceSettings{objectMatchMode=ObjectMatchMode.ByHierarchy,componentsNotMatchedBecomesOverride=true,gameObjectsNotMatchedBecomesOverride=true,recordPropertyOverridesOfMatches=true,changeRootNameToAssetName=false};
  foreach(var colony in colonies)if(!PrefabUtility.IsPartOfPrefabInstance(colony.gameObject))PrefabUtility.ConvertToPrefabInstance(colony.gameObject,prefab,settings,InteractionMode.AutomatedAction);
  int procedural=ConvertProcedural(settings);AssetDatabase.SaveAssets();Debug.Log("CORAL_PREFABS_REPAIRED scanned="+colonies.Length+" procedural="+procedural+" cleanMeshes="+corrected.Count+" mountPlaneCut=0.012m");return colonies.Length+procedural;
 }
 static int ConvertProcedural(ConvertToPrefabInstanceSettings settings){
  var prefabs=new Dictionary<string,GameObject>();int count=0;
  foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))if(filter.name.StartsWith("Living coral |")){
   var root=filter.transform;var renderer=root.GetComponent<Renderer>();string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(filter.sharedMesh))+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderer.sharedMaterial));
   if(!prefabs.TryGetValue(key,out var prefab)){
    Vector3 position=root.localPosition,scale=root.localScale;Quaternion rotation=root.localRotation;root.localPosition=Vector3.zero;root.localRotation=Quaternion.identity;root.localScale=Vector3.one;
    prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Folder+"/Coral_"+key+".prefab",InteractionMode.AutomatedAction);root.localPosition=position;root.localRotation=rotation;root.localScale=scale;PrefabUtility.RecordPrefabInstancePropertyModifications(root);prefabs.Add(key,prefab);
   }else if(!PrefabUtility.IsPartOfPrefabInstance(root.gameObject))PrefabUtility.ConvertToPrefabInstance(root.gameObject,prefab,settings,InteractionMode.AutomatedAction);
   count++;
  }
  return count;
 }
 static void FitCollider(LODGroup group){
  var box=group.GetComponent<BoxCollider>();if(!box)return;bool first=true;Bounds bounds=default;
  foreach(var renderer in group.GetLODs()[0].renderers){var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;foreach(var v in filter.sharedMesh.vertices){var p=group.transform.InverseTransformPoint(filter.transform.TransformPoint(v));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}
  box.center=bounds.center;box.size=bounds.size;EditorUtility.SetDirty(box);
 }
 static Mesh Clip(Mesh source,Matrix4x4 toRoot,float plane){
  var vertices=source.vertices;var normals=source.normals;var uv=source.uv;var colors=source.colors;
  var positions=new List<Vector3>();var outputNormals=new List<Vector3>();var outputUV=new List<Vector2>();var outputColors=new List<Color>();var faces=new List<int>();
  Vector3 shift=toRoot.inverse.MultiplyVector(Vector3.up*plane);var remap=new Dictionary<long,int>();
  for(int submesh=0;submesh<source.subMeshCount;submesh++){
   var triangles=source.GetTriangles(submesh);
   for(int i=0;i<triangles.Length;i+=3){
    var polygon=new List<Vertex>(4);
    for(int corner=0;corner<3;corner++){int id=triangles[i+corner];polygon.Add(new Vertex{key=id,p=vertices[id],n=normals.Length==vertices.Length?normals[id]:Vector3.up,uv=uv.Length==vertices.Length?uv[id]:Vector2.zero,color=colors.Length==vertices.Length?colors[id]:Color.white});}
    var clipped=new List<Vertex>(4);
    for(int edge=0;edge<polygon.Count;edge++){
     var a=polygon[edge];var b=polygon[(edge+1)%polygon.Count];float ay=toRoot.MultiplyPoint3x4(a.p).y-plane,by=toRoot.MultiplyPoint3x4(b.p).y-plane;
     if(ay>=0)clipped.Add(a);if((ay>=0)!=(by>=0))clipped.Add(Vertex.Mix(a,b,ay/(ay-by)));
    }
    if(clipped.Count<3)continue;var ids=new int[clipped.Count];
    for(int k=0;k<clipped.Count;k++){var vertex=clipped[k];if(!remap.TryGetValue(vertex.key,out int id)){id=positions.Count;positions.Add(vertex.p-shift);outputNormals.Add(vertex.n);outputUV.Add(vertex.uv);outputColors.Add(vertex.color);remap.Add(vertex.key,id);}ids[k]=id;}
    for(int k=1;k<clipped.Count-1;k++){faces.Add(ids[0]);faces.Add(ids[k]);faces.Add(ids[k+1]);}
   }
  }
  if(faces.Count==0)throw new Exception("Coral trim removed whole mesh");
  var mesh=new Mesh{name=source.name+" natural base",indexFormat=IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetNormals(outputNormals);mesh.SetUVs(0,outputUV);mesh.SetColors(outputColors);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
 }
}
}
