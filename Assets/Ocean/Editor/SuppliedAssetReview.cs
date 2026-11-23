using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class SuppliedAssetReview {
 const string Root="Assets/UnderwaterArt/SuppliedReview/";
 public static void ValidateFinal(){
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  RealisticReefBuilder.ValidateScene();
  var group=GameObject.Find("Selected supplied reef assets");
  if(!group||group.GetComponentsInChildren<SeaLife>().Length!=4||group.GetComponentsInChildren<BoxCollider>().Length!=2)throw new Exception("Selected asset count mismatch");
  foreach(var life in group.GetComponentsInChildren<SeaLife>()){
   var start=life.transform.position;var cruise=life.GetComponent<ReefCruiser>();for(int i=0;i<300;i++)cruise.Step(1f/30);
   if(Vector3.Distance(start,life.transform.position)<.3f)throw new Exception("Fish cannot swim: "+life.name);
  }
  foreach(var r in group.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported||!m.mainTexture)throw new Exception("Invalid supplied surface: "+r.name);
  Debug.Log("SUPPLIED_FINAL_VALIDATION_PASSED fish=4 anchors=2 movement=passed materials=passed");
 }
 public static void Integrate(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  var old=GameObject.Find("Selected supplied reef assets");if(old)UnityEngine.Object.DestroyImmediate(old);
  var group=new GameObject("Selected supplied reef assets");
  foreach(var key in new[]{"FlameAngel","ReefFish"}){
   var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+key+"/"+key+".fbx"));
   var meshes=source.GetComponentsInChildren<MeshFilter>();var combines=meshes.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray();
   var mesh=new Mesh();mesh.CombineMeshes(combines,true,true);var b=mesh.bounds;var vertices=mesh.vertices;
   // Align each source axis (butterflyfish length X; angelfish length Z) to the
   // project's head +Z, one-metre canonical fish before applying swim deformation.
   var alignment=Quaternion.Euler(0,key=="ReefFish"?90:0,0);float bodyLength=key=="ReefFish"?b.size.x:b.size.z;
   for(int i=0;i<vertices.Length;i++)vertices[i]=alignment*(vertices[i]-b.center)/bodyLength;
   mesh.vertices=vertices;var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=alignment*normals[i];mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=key+" HeadForward";
   var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Root+key+"/SwimMesh.asset");if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,Root+key+"/SwimMesh.asset");UnityEngine.Object.DestroyImmediate(source);
   var material=AssetDatabase.LoadAssetAtPath<Material>(Root+key+"/Surface.mat");material.shader=Shader.Find("Underwater/Instanced Marine");material.color=Color.white;material.enableInstancing=true;EditorUtility.SetDirty(material);
   for(int i=0;i<2;i++){
    var go=new GameObject(key+" supplied reef visitor "+(i+1));go.transform.SetParent(group.transform);go.transform.position=new Vector3((key=="FlameAngel"?-2:2)+i*.7f,-1+i*.4f,3+i*4);go.transform.localScale=Vector3.one*(key=="FlameAngel"?.12f:.16f);
    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
    var life=go.AddComponent<SeaLife>();life.kind=key=="FlameAngel"?"Flame angelfish":"Butterflyfish";
    var body=go.AddComponent<ReefSphereBody>();body.radius=key=="FlameAngel"?.09f:.11f;var cruise=go.AddComponent<ReefCruiser>();cruise.radius=1.5f;cruise.speed=.16f;go.AddComponent<FishBodyMotion>();
    var info=go.AddComponent<ReefCreatureInfo>();info.creatureName=life.kind;info.fact=key=="FlameAngel"?"Bright stripes help break up its outline.":"A narrow snout reaches food between coral branches.";
    if(!body.Clear(go.transform.position))throw new Exception("Supplied fish spawn blocked");
    var start=go.transform.position;for(int step=0;step<300;step++)cruise.Step(1f/30);if(Vector3.Distance(start,go.transform.position)<.3f)throw new Exception("Supplied fish movement blocked");go.transform.position=start;go.transform.rotation=Quaternion.identity;
   }
  }
  foreach(int zone in new[]{2,7}){
   var anchor=new GameObject("Weathered supplied anchor - "+OceanWorld.Names[zone]);anchor.transform.SetParent(group.transform);
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Anchor/Anchor.fbx"),anchor.transform);model.transform.localPosition=Vector3.zero;
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"Anchor/Surface.mat")).ToArray();
   var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.localScale*=2.1f/b.size.x;
   anchor.transform.rotation=Quaternion.Euler(78,zone==2?25:-35,0);anchor.transform.position=new Vector3(2.8f,0,OceanWorld.Centers[zone].z+5);
   float lift=float.NegativeInfinity;foreach(var f in model.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices){var p=f.transform.TransformPoint(v);lift=Mathf.Max(lift,ReefGeometry.Floor(p.x,p.z)-p.y);}anchor.transform.position+=Vector3.up*(lift+.015f);
   anchor.layer=8;var box=anchor.AddComponent<BoxCollider>();b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);box.center=anchor.transform.InverseTransformPoint(b.center);box.size=new Vector3(2.1f,.9f,.14f);
  }
  EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  RealisticReefBuilder.ValidateScene();
  Directory.CreateDirectory("Renders/SuppliedReview");
  var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;camera.transform.position=new Vector3(8,-18,72);camera.transform.LookAt(new Vector3(2.8f,-20,69));camera.farClipPlane=100;ReefCorrectionAudit.Capture(camera,"Renders/SuppliedReview/WreckAnchor.png");
  var fish=GameObject.Find("FlameAngel supplied reef visitor 1");RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",1);camera.transform.position=fish.transform.position+new Vector3(.4f,.15f,.4f);camera.transform.LookAt(fish.transform.position);camera.nearClipPlane=.01f;camera.fieldOfView=40;ReefCorrectionAudit.Capture(camera,"Renders/SuppliedReview/IntegratedFlameAngel.png");
  Debug.Log("SUPPLIED_INTEGRATION_PASSED fish=4 anchors=2 movement=passed sceneValidation=passed");
 }
 public static void Audit(){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  Directory.CreateDirectory("Renders/SuppliedReview");
  var report=new System.Text.StringBuilder();
  foreach(var key in new[]{"Anchor","FlameAngel","ReefFish"}){
   string path=Root+key+"/"+key+".fbx";
   var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.isReadable=true;imp.importCameras=false;imp.importLights=false;imp.importAnimation=true;imp.SaveAndReimport();
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   long tris=0;foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())tris+=mesh.triangles.Length/3;
   report.AppendLine(key+" triangles="+tris+" bounds="+b.size+" clips="+AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Count(c=>!c.name.StartsWith("__")));
   string tex=Directory.GetFiles(Root+key,"Albedo.*").FirstOrDefault(p=>!p.EndsWith(".meta"));
   var mat=new Material(Shader.Find("Standard"));mat.color=tex==null?new Color(.55f,.6f,.63f):Color.white;mat.SetFloat("_Glossiness",.15f);
   if(tex!=null){var ti=(TextureImporter)AssetImporter.GetAtPath(tex);ti.maxTextureSize=1024;ti.SaveAndReimport();mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(tex);}
   AssetDatabase.CreateAsset(mat,Root+key+"/Surface.mat");foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(m=>mat).ToArray();
   var cam=new GameObject("Review camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.12f,.17f);cam.nearClipPlane=.001f;cam.farClipPlane=Mathf.Max(1000,b.size.magnitude*10);cam.orthographic=true;cam.orthographicSize=b.size.magnitude*.6f;cam.transform.position=b.center+new Vector3(1,.4f,1).normalized*b.size.magnitude*2;cam.transform.LookAt(b.center);
   var lamp=new GameObject("Review light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.transform.rotation=Quaternion.Euler(40,-35,0);RenderSettings.ambientLight=Color.gray;
   ReefCorrectionAudit.Capture(cam,"Renders/SuppliedReview/"+key+".png");
   UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cam.gameObject);UnityEngine.Object.DestroyImmediate(lamp.gameObject);
  }
  File.WriteAllText("Renders/SuppliedReview/audit.txt",report.ToString());AssetDatabase.SaveAssets();Debug.Log("SUPPLIED_AUDIT_PASSED");
 }
}
}



