using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LionfishRepairValidation {
 const string Folder="Assets/UnderwaterArt/ReferenceLionfish/";
 static Vector3[] Points(SkinnedMeshRenderer skin) {
  var m=skin.sharedMesh;var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*m.bindposes[i]).ToArray();var vertices=m.vertices;var weights=m.boneWeights;
  for(int i=0;i<vertices.Length;i++){var w=weights[i];var p=vertices[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return vertices;
 }
 public static void Run() {
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  const string baselinePath="Assets/LionfishRepairBaseline.fbx";
  AssetDatabase.ImportAsset(baselinePath,ImportAssetOptions.ForceSynchronousImport);
  var importer=(ModelImporter)AssetImporter.GetAtPath(baselinePath);importer.animationType=ModelImporterAnimationType.Legacy;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
  AssetDatabase.ImportAsset(Folder+"Lionfish.fbx",ImportAssetOptions.ForceSynchronousImport);
  var before=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(baselinePath));
  var after=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Lionfish.fbx"));
  var a=before.GetComponentsInChildren<SkinnedMeshRenderer>().OrderBy(r=>r.name).ToArray();var b=after.GetComponentsInChildren<SkinnedMeshRenderer>().OrderBy(r=>r.name).ToArray();
  float maxError=0,maxBounds=0;
  foreach(string name in new[]{"Idle","Swim","Breath"}) {
   var ca=AssetDatabase.LoadAllAssetsAtPath(baselinePath).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__") && c.name.EndsWith(name));
   var cb=AssetDatabase.LoadAllAssetsAtPath(Folder+"Lionfish.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__") && c.name.EndsWith(name));
   foreach(float phase in new[]{0f,.125f,.375f,.625f,.875f,1f}) {
    ca.SampleAnimation(before,phase*ca.length);cb.SampleAnimation(after,phase*cb.length);
    for(int i=0;i<a.Length;i++) {
     var pa=Points(a[i]);var pb=Points(b[i]);
     foreach(var p in pb)maxError=Mathf.Max(maxError,Mathf.Sqrt(pa.Min(q=>(p-q).sqrMagnitude)));
     var ba=new Bounds(pa[0],Vector3.zero);foreach(var p in pa)ba.Encapsulate(p);
     var bb=new Bounds(pb[0],Vector3.zero);foreach(var p in pb)bb.Encapsulate(p);
     maxBounds=Mathf.Max(maxBounds,(ba.min-bb.min).magnitude,(ba.max-bb.max).magnitude);
    }
   }
  }
  if(maxError>.000025f || maxBounds>.000002f)throw new Exception("Repair changed imported geometry "+maxError+" bounds="+maxBounds);
  var shader=Shader.Find("Underwater/Reference Lionfish");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader failed");
  var camera=new GameObject("Comparison camera").AddComponent<Camera>();camera.transform.position=new Vector3(.48f,.23f,.65f);camera.transform.LookAt(new Vector3(0,.025f,0));camera.orthographic=true;camera.orthographicSize=.29f;camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.07f,.085f);
  foreach(var spec in new[]{new Vector4(40,-35,0,1.1f),new Vector4(20,140,0,.65f)}){var l=new GameObject("Comparison light").AddComponent<Light>();l.type=LightType.Directional;l.intensity=spec.w;l.transform.rotation=Quaternion.Euler(spec.x,spec.y,spec.z);}
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.35f,.35f);RenderSettings.fog=false;
  var orientation=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"ReferenceLionfish.prefab").transform.GetChild(0).localRotation;
  var target=new RenderTexture(900,800,24);camera.targetTexture=target;
  foreach(var model in new[]{before,after}){
   before.SetActive(model==before);after.SetActive(model==after);model.transform.localRotation=orientation;
   foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+(skin.name.Contains("membrane")?"LionfishFins.mat":"LionfishSkin.mat"));
   var path=model==before?baselinePath:Folder+"Lionfish.fbx";var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__") && c.name.EndsWith("Swim"));clip.SampleAnimation(model,0);
   camera.Render();RenderTexture.active=target;var image=new Texture2D(900,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,800),0,0);image.Apply();System.IO.Directory.CreateDirectory("Renders/LionfishRepair");System.IO.File.WriteAllBytes("Renders/LionfishRepair/"+(model==before?"Unity_Before.png":"Unity_After.png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  }
  Debug.Log("LIONFISH_REPAIR_UNITY_PASSED triangles="+b.Sum(r=>r.sharedMesh.triangles.Length/3)+" vertexError="+maxError+" boundsError="+maxBounds+" poses=18");
 }
}
