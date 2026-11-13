using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReefReviewCorrections {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Folder="Assets/UnderwaterArt/ReviewCorrections";
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  GroundColonies();RepairAnemone();RepairOctopus();InstallCruisers();InstallAnimalSolids();ExtendOcean();
  Physics.SyncTransforms();foreach(var body in UnityEngine.Object.FindObjectsByType<ReefSphereBody>(FindObjectsSortMode.None))if(!body.Clear(body.transform.position)&&!body.PlaceNear(body.transform.position))throw new Exception("Blocked animal "+body.name);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();Capture();
 }
 static bool Support(Collider c)=>c is MeshCollider&&(c.name=="Porous reef outcrop"||c.name=="Eroded arch voussoir"||c.name=="Reef foundation stone"||c.name.StartsWith("Continuous seabed")||c.name.Contains("weathered reef bed")||c.transform.parent&&c.transform.parent.name.Contains("weathered reef bed"));
 static float Ground(Vector3 point,float above=1){float y=float.NegativeInfinity;var ray=new Ray(point+Vector3.up*above,Vector3.down);foreach(var c in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))if(Support(c)&&c.Raycast(ray,out var hit,20)&&hit.normal.y>.3f)y=Mathf.Max(y,hit.point.y);return float.IsNegativeInfinity(y)?ReefGeometry.Floor(point.x,point.z):y;}
 static void GroundColonies(){
  int count=0;var supports=Array.FindAll(UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None),c=>Support(c));
  foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
   if(!t.name.StartsWith("Scanned Secale")&&!t.name.StartsWith("Living coral |")&&t.name!="Seagrass patch")continue;
   if(t.name=="Living coral | 3"||t.name=="Seagrass patch"){
 var rootRay=new Ray(t.position+Vector3.up,Vector3.down);float closest=3;foreach(var c in supports)if(c.Raycast(rootRay,out var hit,closest)&&hit.normal.y>.45f){closest=hit.distance;t.rotation=Quaternion.FromToRotation(t.up,hit.normal)*t.rotation;}
}
   float bottom=float.PositiveInfinity;Vector3 foot=Vector3.zero;foreach(var f in t.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices){var p=f.transform.TransformPoint(v);if(p.y<bottom){bottom=p.y;foot=p;}}
   if(float.IsPositiveInfinity(bottom))continue;float top=float.NegativeInfinity;var ray=new Ray(new Vector3(t.position.x,bottom+.6f,t.position.z),Vector3.down);
   foreach(var c in supports)if(c.Raycast(ray,out var hit,15)&&hit.normal.y>.25f)top=Mathf.Max(top,hit.point.y);
   if(float.IsNegativeInfinity(top))top=ReefGeometry.Floor(foot.x,foot.z);
   float shift=top-bottom-.055f;if(Mathf.Abs(shift)>.015f){t.position+=Vector3.up*shift;PrefabUtility.RecordPrefabInstancePropertyModifications(t);count++;}
  }Physics.SyncTransforms();Debug.Log("REVIEW_GROUNDED colonies="+count);
 }
 static void RepairAnemone(){
  var old=GameObject.Find("Day 3 reef anemone");Vector3 p=old.transform.position;UnityEngine.Object.DestroyImmediate(old);
  var root=new GameObject("Day 3 reef anemone");p.y=Ground(p,1)+.025f;root.transform.position=p;
  var ray=new Ray(p+Vector3.up,Vector3.down);float closest=3;foreach(var c in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))if(Support(c)&&c.Raycast(ray,out var hit,closest)&&hit.normal.y>.35f){closest=hit.distance;root.transform.rotation=Quaternion.FromToRotation(Vector3.up,hit.normal);root.transform.position=hit.point-root.transform.up*.035f;}
  var e=root.AddComponent<ReefEncounter>();e.response=ReefEncounter.Response.Anemone;var arms=new List<Transform>();
  var material=Material("Anemone soft tissue",new Color(.49f,.35f,.28f));material.SetFloat("_Glossiness",.12f);
  Sphere("Embedded anemone foot",root.transform,new Vector3(0,-.035f,0),new Vector3(.62f,.1f,.62f),material);
  for(int i=0;i<48;i++){float a=i*2.39996f,r=.05f+.26f*Mathf.Sqrt(i/48f);var pivot=new GameObject("Soft retracting tentacle").transform;pivot.SetParent(root.transform,false);pivot.localPosition=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
   var points=new Vector3[9];var widths=new float[9];float length=.16f+.1f*(Mathf.Sin(i*4.1f)*.5f+.5f);for(int j=0;j<9;j++){float t=j/8f;points[j]=new Vector3(Mathf.Sin(t*2+i)*.035f*t,t*length,Mathf.Cos(t*2+i)*.035f*t);widths[j]=Mathf.Lerp(.016f,.008f,t);}Tube("Anemone tentacle",pivot,points,widths,material,"Tentacle"+i);Sphere("Rounded tentacle tip",pivot,points[8],Vector3.one*.023f,material);arms.Add(pivot);
  }e.tentacles=arms.ToArray();
 }
 static void RepairOctopus(){var e=GameObject.Find("Day 3 rocky octopus").GetComponent<ReefEncounter>();var body=e.GetComponent<ReefSphereBody>();body.radius=.23f;body.centerOffset=new Vector3(0,.15f,-.08f);Vector3 p=e.transform.position;p.y=Ground(p,2)+.1f;e.transform.position=p;Physics.SyncTransforms();if(!body.Clear(p))throw new Exception("Octopus substrate blocked");}
 static void InstallCruisers(){int count=0;
  foreach(var f in UnityEngine.Object.FindObjectsByType<FishBodyMotion>(FindObjectsSortMode.None))if(!f.GetComponent<ReefEncounter>()&&!f.GetComponent<ReefCruiser>()){
   var life=f.GetComponent<SeaLife>();if(life&&life.grabbable)continue;
   var mover=f.gameObject.AddComponent<ReefCruiser>();mover.speed=f.name=="Anglerfish"?.18f:.25f;mover.radius=2.4f;count++;
  }
  var squid=GameObject.Find("Giant squid distant silhouette");var swim=squid.GetComponent<ReefCruiser>();if(!swim)swim=squid.AddComponent<ReefCruiser>();swim.squid=true;swim.speed=.5f;swim.radius=6;var body=squid.GetComponent<ReefSphereBody>();if(!body)body=squid.AddComponent<ReefSphereBody>();body.radius=2;body.centerOffset=Vector3.zero;var squidPosition=squid.transform.position;squidPosition.y=Ground(squidPosition,3)+7;squid.transform.position=squidPosition;
  var coat=Material("Squid moving skin",new Color(.27f,.39f,.4f));coat.shader=Shader.Find("Underwater/Squid Skin");squid.GetComponent<Renderer>().sharedMaterial=coat;
  var glow=Material("Visible angler lure",new Color(.2f,.9f,1));glow.shader=Shader.Find("Underwater/Visible Lure");glow.color=new Color(.3f,1.4f,1.8f);glow.SetColor("_EmissionColor",new Color(.3f,1.5f,2));EditorUtility.SetDirty(glow);
  foreach(var f in UnityEngine.Object.FindObjectsByType<FishBodyMotion>(FindObjectsSortMode.None))if(f.name=="Anglerfish"||f.name=="Lumen"){
   var old=f.transform.Find("Visible organic lure");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var lure=new GameObject("Visible organic lure").transform;lure.SetParent(f.transform,false);
   Tube("Lure stalk",lure,new[]{new Vector3(0,.18f,.15f),new Vector3(0,.57f,.22f),new Vector3(0,.58f,.43f),new Vector3(0,.46f,.47f)},new[]{.018f,.014f,.01f,.008f},f.GetComponent<Renderer>().sharedMaterial,"LureStalk");
   Sphere("Emissive lure bulb",lure,new Vector3(0,.46f,.47f),Vector3.one*.12f,glow);
  }Debug.Log("REVIEW_CRUISERS fish="+count+" squid=1 visibleLures=installed");
 }
 static void InstallAnimalSolids(){int count=0;foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
  if(!t)continue;bool turtle=t.GetComponent<TurtleSwimmer>(),manta=t.GetComponent<MarineSwimmer>()||t.GetComponent<StingraySwimmer>(),squid=t.name=="Giant squid distant silhouette",fish=t.GetComponent<FishBodyMotion>()&&t.localScale.x>=.7f;
  if(!turtle&&!manta&&!squid&&!fish)continue;var old=t.Find("Animal body solid");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var solid=new GameObject("Animal body solid");solid.layer=9;solid.transform.SetParent(t,false);var capsule=solid.AddComponent<CapsuleCollider>();capsule.direction=2;capsule.isTrigger=false;
  capsule.center=turtle?new Vector3(0,0,.1f):squid?new Vector3(0,0,-3):Vector3.zero;capsule.radius=turtle?.35f:squid?1.7f:manta?.38f:.23f;capsule.height=turtle?1.25f:squid?20:manta?2.2f:1;count++;
 }Debug.Log("REVIEW_ANIMAL_SOLIDS count="+count+" layer=9 reefNavigationMask=8");}
 static void ExtendOcean(){
  var old=GameObject.Find("Distant seabed extension");if(old)UnityEngine.Object.DestroyImmediate(old);var root=new GameObject("Distant seabed extension");
  var source=Array.Find(UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None),r=>r.name.StartsWith("Continuous seabed"));
  SideExtension(root.transform,false,source.sharedMaterial);SideExtension(root.transform,true,source.sharedMaterial);EndExtension(root.transform,false,source.sharedMaterial);EndExtension(root.transform,true,source.sharedMaterial);
  var water=GameObject.Find("Moving underside of ocean surface");var geometry=new ReefGeometry();const int nx=48,nz=64;for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)geometry.Vertex(new Vector3(Mathf.Lerp(-350,350,x/(float)nx),8,Mathf.Lerp(-240,640,z/(float)nz)),new Vector2(x/(float)nx,z/(float)nz),Color.white);for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x;geometry.Quad(a,a+1,a+nx+2,a+nx+1);}water.GetComponent<MeshFilter>().sharedMesh=SaveMesh(geometry.Mesh("Expanded ocean surface"),"ExpandedSurface");
 }
 static void SideExtension(Transform parent,bool right,Material material){
  float sign=right?1:-1;var edge=new SortedDictionary<float,Vector3>();foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))if(f.name.StartsWith("Continuous seabed"))foreach(var v in f.sharedMesh.vertices){var p=f.transform.TransformPoint(v);if(Mathf.Abs(p.x-sign*45)<.01f)edge[Mathf.Round(p.z*10000)/10000]=p;}
  var g=new ReefGeometry();int rows=0;foreach(var pair in edge){for(int x=0;x<3;x++){float px=sign*(x==0?45:x==1?85:200);var p=x==0?pair.Value:new Vector3(px,ReefGeometry.Floor(px,pair.Value.z),pair.Value.z);g.Vertex(p,new Vector2(p.x*.24f,p.z*.24f),Color.white);}rows++;}
  for(int z=0;z<rows-1;z++)for(int x=0;x<2;x++){int a=z*3+x;if(right)g.Quad(a,a+3,a+4,a+1);else g.Quad(a,a+1,a+4,a+3);}MeshObject(parent,right?"East stitched sand":"West stitched sand",g.Mesh("Stitched seabed"),material);
 }
 static void EndExtension(Transform parent,bool north,Material material){
  var xs=new List<float>{-200,-85};for(int x=0;x<=120;x++)xs.Add(-45+x*.75f);xs.Add(85);xs.Add(200);var g=new ReefGeometry();int cols=xs.Count;const int rows=16;for(int z=0;z<=rows;z++){float pz=north?Mathf.Lerp(274,420,z/(float)rows):Mathf.Lerp(-30,-180,z/(float)rows);foreach(float px in xs)g.Vertex(new Vector3(px,ReefGeometry.Floor(px,pz),pz),new Vector2(px*.24f,pz*.24f),Color.white);}for(int z=0;z<rows;z++)for(int x=0;x<cols-1;x++){int a=z*cols+x;if(north)g.Quad(a,a+cols,a+cols+1,a+1);else g.Quad(a,a+1,a+cols+1,a+cols);}MeshObject(parent,north?"North stitched sand":"South stitched sand",g.Mesh("Extended seabed"),material);
 }
 static void MeshObject(Transform parent,string name,Mesh mesh,Material material){var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,name);obj.AddComponent<MeshRenderer>().sharedMaterial=material;}
 static Material Material(string name,Color color){string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard")){enableInstancing=true};AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;}
 static Mesh SaveMesh(Mesh mesh,string name){string path=Folder+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
 static void Sphere(string name,Transform parent,Vector3 p,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;}
 static void Tube(string name,Transform parent,Vector3[] points,float[] widths,Material material,string asset){var geometry=new ReefGeometry();geometry.Tube(points,widths,8,Color.white);var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=SaveMesh(geometry.Mesh(name),asset);g.AddComponent<MeshRenderer>().sharedMaterial=material;}
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int moving=0;foreach(var cruiser in UnityEngine.Object.FindObjectsByType<ReefCruiser>(FindObjectsSortMode.None)){var start=cruiser.transform.position;float travelled=0;for(int i=0;i<3600;i++){var before=cruiser.transform.position;cruiser.Step(1f/30);travelled+=Vector3.Distance(before,cruiser.transform.position);}if(travelled<cruiser.speed*30)throw new Exception("Stationary cruiser "+cruiser.name+" distance="+travelled);moving++;}
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();foreach(var school in UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None)){
   school.Initialize();var stalled=new float[school.fish.Length];for(int i=0;i<5400;i++){school.Simulate(1f/30);for(int j=0;j<stalled.Length;j++){var fish=school.fish[j];stalled[j]=fish.Velocity.magnitude<fish.cruiseSpeed*.08f?stalled[j]+1f/30:0;if(stalled[j]>8)throw new Exception("Fish stuck for 8 seconds: "+fish.name+" at "+fish.transform.position+" desired="+fish.SteeringDirection+" center="+fish.GetComponent<ReefSphereBody>().WorldCenter+" offset="+fish.GetComponent<ReefSphereBody>().centerOffset+" velocity="+fish.Velocity+" forward="+fish.transform.forward+" bodyClear="+fish.GetComponent<ReefSphereBody>().Clear(fish.transform.position)+" radius="+fish.GetComponent<ReefSphereBody>().radius+" overlaps="+string.Join(",",Array.ConvertAll(Physics.OverlapSphere(fish.transform.position,fish.GetComponent<ReefSphereBody>().radius,ReefSphereBody.SolidMask),c=>c.name))+Probe(fish.GetComponent<ReefSphereBody>()));}}
  }
  ValidateAnimalCollision();ReefDayThree.Validate();Debug.Log("REVIEW_VALIDATED cruisers="+moving+" seconds=120 schoolingSeconds=180 stallsOver8s=0 priorRegressions=passed");
 }
 static string Probe(ReefSphereBody body){string result=" probes=";foreach(var d in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up,Vector3.down}){if(Physics.SphereCast(body.WorldCenter,body.radius,d,out var h,3,ReefSphereBody.SolidMask))result+=d+":"+h.collider.name+":"+h.distance+";";else result+=d+":clear;";}return result;}
static void ValidateAnimalCollision(){
 EditorSceneManager.OpenScene(Scene);var motor=UnityEngine.Object.FindFirstObjectByType<DiverCollisionMotor>();motor.Initialize();var savedBounds=motor.diveBounds;motor.diveBounds=null;
 var obstacle=new GameObject("Animal collision test");obstacle.layer=9;obstacle.transform.position=new Vector3(900,1,0);obstacle.AddComponent<BoxCollider>().size=new Vector3(1,4,1);Physics.SyncTransforms();
 try{if(!motor.TryTeleport(new Vector3(900,0,-3)))throw new Exception("Animal collision test start blocked");motor.Move(Vector3.forward*6);if(Camera.main.transform.position.z>-.5f)throw new Exception("Swim passed through animal layer");if(motor.TryTeleport(new Vector3(900,0,0)))throw new Exception("Teleport entered animal body");var head=Camera.main.transform;head.localPosition+=Vector3.forward*3;motor.TrackHead();if(head.position.z>-.5f)throw new Exception("Room scale entered animal body");Debug.Log("ANIMAL_COLLISION_VALIDATED swim=blocked teleport=rejected tracking=blocked");}
 finally{motor.diveBounds=savedBounds;UnityEngine.Object.DestroyImmediate(obstacle);}
}
public static void Capture(){
 ReefDayThree.Capture();EditorSceneManager.OpenScene(Scene);var camera=Camera.main;camera.allowHDR=false;camera.fieldOfView=65;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();
 camera.transform.position=new Vector3(0,1,15);camera.transform.LookAt(camera.transform.position+new Vector3(.1f,1,.3f));atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/Review_Water_Surface.png");
 var angler=Array.Find(UnityEngine.Object.FindObjectsByType<FishBodyMotion>(FindObjectsSortMode.None),f=>f.name=="Anglerfish");camera.transform.position=angler.transform.position+new Vector3(1,.4f,.8f);camera.transform.LookAt(angler.transform.position+Vector3.up*.1f);atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/Review_Angler_Lure.png");
 camera.transform.position=new Vector3(20,2,20);camera.transform.LookAt(new Vector3(60,-4,30));atmosphere.Apply(camera);ReefCorrectionAudit.Capture(camera,"Renders/Review_Distant_Seabed.png");
}
}
}
