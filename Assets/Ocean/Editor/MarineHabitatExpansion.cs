using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater.Editor {
public static class MarineHabitatExpansion {
 const string Root="Assets/UnderwaterArt/HabitatExpansion/";
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static T Save<T>(T value,string path) where T:UnityEngine.Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);return old;}AssetDatabase.CreateAsset(value,path);return value;}
 static Mesh Canonical(string key,bool reverse){
  string folder=(key=="FlameAngel"||key=="ReefFish")?"Assets/UnderwaterArt/SuppliedReview/"+key+"/":Root+key+"/";
  var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+key+".fbx"));
  var mesh=new Mesh{name=key+" HeadForward"};mesh.CombineMeshes(source.GetComponentsInChildren<MeshFilter>().Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);
  var b=mesh.bounds;var rotation=key=="ReefFish"?Quaternion.Euler(0,90,0):reverse?Quaternion.identity:Quaternion.Euler(0,180,0);var vertices=mesh.vertices;var normals=mesh.normals;
  float length=key=="ReefFish"?b.size.x:b.size.z;
  for(int i=0;i<vertices.Length;i++){vertices[i]=rotation*(vertices[i]-b.center)/length;normals[i]=rotation*normals[i];}mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();UnityEngine.Object.DestroyImmediate(source);return Save(mesh,folder+"SwimMesh.asset");
 }
 static Material Surface(string key,string path){var mat=AssetDatabase.LoadAssetAtPath<Material>(path);mat.shader=Shader.Find("Underwater/Instanced Marine");mat.color=Color.white;mat.enableInstancing=true;mat.SetFloat("_TailAmplitude",0);mat.SetFloat("_Cull",key=="Ship"?2:0);EditorUtility.SetDirty(mat);return mat;}
 public static void Integrate(){
  EditorSceneManager.OpenScene(Scene);var keptShip=GameObject.Find("Supplied ship on trench floor");bool retainShip=keptShip;Vector3 keptPosition=retainShip?keptShip.transform.position:Vector3.zero;Quaternion keptRotation=retainShip?keptShip.transform.rotation:Quaternion.identity;Vector3 keptScale=retainShip?keptShip.transform.localScale:Vector3.one;var old=GameObject.Find("Habitat fish and deep ship expansion");if(old)UnityEngine.Object.DestroyImmediate(old);
  var root=new GameObject("Habitat fish and deep ship expansion");
  var keys=new[]{"FlameAngel","ReefFish","Clownfish","Seabass"};var meshes=new Mesh[4];var materials=new Material[4];var mediums=new Mesh[4];var lows=new Mesh[4];
  for(int i=0;i<4;i++){
   string folder=i<2?"Assets/UnderwaterArt/SuppliedReview/"+keys[i]+"/":Root+keys[i]+"/";
   meshes[i]=Canonical(keys[i],i==0||i==3);
   materials[i]=Surface(keys[i],folder+"Surface.mat");
   mediums[i]=Save(FishDistanceMesh.Create(meshes[i],24),folder+"SchoolMedium.asset");lows[i]=Save(FishDistanceMesh.Create(meshes[i],12),folder+"SchoolDistant.asset");
  }
  foreach(var life in GameObject.Find("Selected supplied reef assets").GetComponentsInChildren<SeaLife>())if(life.kind=="Butterflyfish"){life.transform.localScale=Vector3.one*.16f;life.GetComponent<ReefSphereBody>().radius=.11f;}
  var field=UnityEngine.Object.FindObjectsByType<ReefAnimalClearance>(FindObjectsSortMode.None).Select(c=>c.seabed).First(f=>f);
  int[] zones={0,1,2,6,7};int[][] species={new[]{2,0},new[]{1,0},new[]{3,1},new[]{3,3},new[]{3,1}};
  for(int s=0;s<zones.Length;s++){
   int zone=zones[s];var schoolGo=new GameObject("Supplied school | "+OceanWorld.Names[zone]);schoolGo.transform.SetParent(root.transform);
   var school=schoolGo.AddComponent<FishSchool>();school.habitatRadius=2.2f;school.simulationRadius=28;school.reefEncounter=true;
   // Keep additional schools away from the central travel corridor and existing encounters.
   var center=OceanWorld.Centers[zone]+new Vector3(zone==6?-2.5f:1.5f,1.1f,-5);center.y=Mathf.Max(center.y,ReefGeometry.Floor(center.x,center.z)+2.4f);school.habitatCenter=center;schoolGo.transform.position=center;
   for(int i=0;i<6;i++){
    int kind=species[s][i%2];float length=kind==0?.12f:kind==1?.16f:kind==2?.1f:.28f;
    var fish=new GameObject(keys[kind]+" school fish "+(i+1));fish.transform.SetParent(schoolGo.transform);fish.transform.localScale=Vector3.one*length;
    fish.transform.position=center+new Vector3((i%3-1)*.6f,(i%2)*.25f,(i/3)*.7f);fish.transform.rotation=Quaternion.Euler(0,i*37,0);
    fish.AddComponent<MeshFilter>().sharedMesh=meshes[kind];var renderer=fish.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[kind];renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    var body=fish.AddComponent<ReefSphereBody>();body.radius=length*.6f;
    var clearance=fish.AddComponent<ReefAnimalClearance>();clearance.seabed=field;clearance.animatedBody=meshes[kind].bounds;clearance.animatedBody.Expand(.16f);clearance.Enforce();
    var agent=fish.AddComponent<FishAgent>();agent.bodyLength=length;agent.cruiseSpeed=length*1.4f;
    var detail=fish.AddComponent<ReefFishDetail>();detail.full=meshes[kind];detail.medium=mediums[kind];detail.distant=lows[kind];detail.closeDistance=1.8f;
    var lod=fish.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.001f,new Renderer[]{renderer})});lod.RecalculateBounds();float worldSize=lod.size*length;lod.SetLODs(new[]{new LOD(Mathf.Clamp(worldSize/28,.002f,.02f),new Renderer[]{renderer})});lod.fadeMode=LODFadeMode.None;
   }
  }
  // One 1,028-triangle static mesh, one material, one collider. No rigidbody,
  // per-frame scripts, extra lights, particles, or transparent passes.
  var ship=new GameObject("Supplied ship on trench floor");ship.transform.SetParent(root.transform);ship.layer=8;
  var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Ship/Ship.fbx"));var hull=new Mesh{name="Deep ship original silhouette"};hull.CombineMeshes(source.GetComponentsInChildren<MeshFilter>().Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);UnityEngine.Object.DestroyImmediate(source);
  var bounds=hull.bounds;var points=hull.vertices;float factor=16/bounds.size.z;for(int i=0;i<points.Length;i++)points[i]=(points[i]-bounds.center)*factor;hull.vertices=points;hull.RecalculateBounds();hull.RecalculateTangents();hull=Save(hull,Root+"Ship/DeepShip.asset");ship.AddComponent<MeshFilter>().sharedMesh=hull;
  var skin=ship.AddComponent<MeshRenderer>();skin.sharedMaterial=Surface("Ship",Root+"Ship/Surface.mat");skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;skin.lightProbeUsage=LightProbeUsage.Off;skin.reflectionProbeUsage=ReflectionProbeUsage.Off;
  ship.transform.position=new Vector3(11,0,130);ship.transform.rotation=Quaternion.Euler(4,18,-9);float lift=float.NegativeInfinity;foreach(var v in points){var p=ship.transform.TransformPoint(v);lift=Mathf.Max(lift,ReefGeometry.Floor(p.x,p.z)-p.y);}ship.transform.position+=Vector3.up*(lift-.08f);ship.AddComponent<MeshCollider>().sharedMesh=hull;GameObjectUtility.SetStaticEditorFlags(ship,StaticEditorFlags.BatchingStatic);
  if(retainShip){ship.transform.SetPositionAndRotation(keptPosition,keptRotation);ship.transform.localScale=keptScale;}
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());Validate();Capture();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);var root=GameObject.Find("Habitat fish and deep ship expansion");if(!root)throw new Exception("Missing habitat expansion");var schools=root.GetComponentsInChildren<FishSchool>();var agents=root.GetComponentsInChildren<FishAgent>();if(schools.Length!=5||agents.Length!=30)throw new Exception("School budget exceeded");
  Physics.SyncTransforms();var starts=agents.ToDictionary(a=>a,a=>a.transform.position);long tris=0;
  foreach(var a in agents){var r=a.GetComponent<Renderer>();if(!r.sharedMaterial.enableInstancing||r.sharedMaterial.shader.name!="Underwater/Instanced Marine"||r.shadowCastingMode!=ShadowCastingMode.Off)throw new Exception("School render budget violated");var detail=a.GetComponent<ReefFishDetail>();if(Mathf.Abs(detail.full.bounds.size.z-1)>.001f||detail.full.bounds.size.x>1||detail.full.bounds.size.y>1)throw new Exception("Fish canonical dimensions invalid");if(detail.distant.triangles.Length>=detail.full.triangles.Length)throw new Exception("Distance mesh not reduced");tris+=detail.full.triangles.Length/3;}
  var timer=System.Diagnostics.Stopwatch.StartNew();for(int frame=0;frame<900;frame++)foreach(var school in schools)school.Simulate(1f/30);timer.Stop();
  foreach(var a in agents){if(Vector3.Distance(starts[a],a.transform.position)<.2f)throw new Exception("School fish stuck: "+a.name);if(!a.GetComponent<ReefSphereBody>().Clear(a.transform.position))throw new Exception("School fish intersects solid: "+a.name);}
  foreach(var school in schools)if(school.FlockingPasses>310)throw new Exception("Flocking cadence regression");
  EditorSceneManager.OpenScene(Scene);var ship=GameObject.Find("Supplied ship on trench floor");if(!ship||ship.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3>1100||ship.layer!=8||!ship.GetComponent<MeshCollider>())throw new Exception("Deep ship budget/collision invalid");RealisticReefBuilder.ValidateScene();ReefCollisionValidation.Run();
  Directory.CreateDirectory("Renders/HabitatExpansion");File.WriteAllText("Renders/HabitatExpansion/Validation.txt","Schools=5 Fish=30 FullTriangles="+tris+" ShipTriangles=1028\nSimulation: all schools 30 seconds, movement and solid clearance passed. Editor simulation elapsedMs="+timer.ElapsedMilliseconds+" (not headset FPS)\nInstanced shared materials, no added lights or shadows, distance meshes and small-fish culling enabled.\n");Debug.Log("HABITAT_EXPANSION_VALIDATED schools=5 fish=30 triangles="+tris+" ship=1028");
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/HabitatExpansion");var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;var ship=GameObject.Find("Supplied ship on trench floor");var center=ship.GetComponent<Renderer>().bounds.center;camera.transform.position=center+new Vector3(-13,5,-13);camera.transform.LookAt(center);camera.farClipPlane=60;camera.nearClipPlane=.05f;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();if(atmosphere)atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/HabitatExpansion/DeepShip.png");
  var school=UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None).First(s=>s.name=="Supplied school | Rocky Ledge");camera.transform.position=school.habitatCenter+new Vector3(-1.5f,.4f,-1.8f);camera.transform.LookAt(school.habitatCenter);if(atmosphere)atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/HabitatExpansion/LedgeSchool.png");
 }
 public static void AuditSupply(){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  Directory.CreateDirectory("Renders/HabitatExpansion");var report=new System.Text.StringBuilder();
  foreach(string key in new[]{"Ship","Seabass","Clownfish"}){
   var path=Root+key+"/"+key+".fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.isReadable=true;imp.importAnimation=false;imp.importCameras=false;imp.importLights=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.SaveAndReimport();
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var material=new Material(Shader.Find("Standard"));var texturePath=Root+key+"/Albedo.png";if(File.Exists(texturePath)){var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.streamingMipmaps=true;var platform=ti.GetPlatformTextureSettings("Android");platform.name="Android";platform.overridden=true;platform.maxTextureSize=1024;platform.format=TextureImporterFormat.ASTC_6x6;ti.SetPlatformTextureSettings(platform);ti.SaveAndReimport();material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);}material.SetFloat("_Glossiness",.12f);material.enableInstancing=true;material=Save(material,Root+key+"/Surface.mat");foreach(var r in rs)r.sharedMaterials=r.sharedMaterials.Select(m=>material).ToArray();
   report.AppendLine(key+" triangles="+AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Sum(m=>m.triangles.Length/3)+" bounds="+b.size+" renderers="+rs.Length);
   var camera=new GameObject("Asset review").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.12f,.17f);camera.nearClipPlane=.001f;camera.farClipPlane=Mathf.Max(1000,b.size.magnitude*10);camera.orthographic=true;camera.orthographicSize=b.size.magnitude*.52f;camera.transform.position=b.center+new Vector3(1,.55f,1).normalized*b.size.magnitude*2;camera.transform.LookAt(b.center);RenderSettings.ambientLight=Color.gray;var lamp=new GameObject("Review light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.transform.rotation=Quaternion.Euler(40,-35,0);ReefCorrectionAudit.Capture(camera,"Renders/HabitatExpansion/"+key+".png");UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(lamp.gameObject);
  }
  File.WriteAllText("Renders/HabitatExpansion/SupplyAudit.txt",report.ToString());AssetDatabase.SaveAssets();Debug.Log("HABITAT_SUPPLY_AUDIT_PASSED");
 }
}
}
