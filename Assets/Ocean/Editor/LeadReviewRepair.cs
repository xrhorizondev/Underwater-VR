using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class LeadReviewRepair {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/LeadReview";
 const string Folder="Assets/UnderwaterArt/LeadReview";
 static readonly System.Collections.Generic.HashSet<ulong> touched=new();
 static void Touch(GameObject go){touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(go).targetObjectId);foreach(var c in go.GetComponents<Component>())if(c)touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId);}
 static T Save<T>(T value,string path) where T:UnityEngine.Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);return old;}AssetDatabase.CreateAsset(value,path);return value;}
 static Mesh Refine(Mesh source){
  var vertices=source.vertices.ToList();var normals=source.normals.ToList();var uv=source.uv.ToList();var uv2=source.uv2.Length==source.vertexCount?source.uv2.ToList():Enumerable.Repeat(Vector2.zero,source.vertexCount).ToList();var cache=new System.Collections.Generic.Dictionary<(int,int),int>();var faces=new System.Collections.Generic.List<int>();
  int Mid(int a,int b){var key=(Mathf.Min(a,b),Mathf.Max(a,b));if(cache.TryGetValue(key,out int result))return result;result=vertices.Count;cache[key]=result;var mid=(vertices[a]+vertices[b])*.5f;if(Vector3.Dot(normals[a],normals[b])>.4f){var pa=mid-normals[a]*Vector3.Dot(mid-vertices[a],normals[a]);var pb=mid-normals[b]*Vector3.Dot(mid-vertices[b],normals[b]);mid=Vector3.Lerp(mid,(pa+pb)*.5f,.65f);}vertices.Add(mid);normals.Add((normals[a]+normals[b]).normalized);uv.Add((uv[a]+uv[b])*.5f);uv2.Add((uv2[a]+uv2[b])*.5f);return result;}
  var triangles=source.triangles;for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2],ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);faces.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});}
  var mesh=new Mesh{name=source.name+" CloseQuality",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,uv2);mesh.SetTriangles(faces,0);mesh.RecalculateTangents();mesh.bounds=source.bounds;return mesh;
 }
 static bool Floor(float x,float z,out float y){y=float.NegativeInfinity;foreach(var hit in Physics.RaycastAll(new Vector3(x,40,z),Vector3.down,180,1<<8,QueryTriggerInteraction.Ignore))if(hit.collider.name.StartsWith("Continuous seabed"))y=Mathf.Max(y,hit.point.y);return !float.IsNegativeInfinity(y);}
 public static void Run(){
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(Scene);var ship=GameObject.Find("Supplied ship on trench floor");var shipPose=EditorJsonUtility.ToJson(ship.transform);int fishCount=0,neckVertices=0;var report=new System.Text.StringBuilder();
  foreach(string key in new[]{"FlameAngel","ReefFish","Clownfish","Seabass"}){
   string sourceFolder=(key=="FlameAngel"||key=="ReefFish")?"Assets/UnderwaterArt/SuppliedReview/":"Assets/UnderwaterArt/HabitatExpansion/";sourceFolder+=key+"/";var source=AssetDatabase.LoadAssetAtPath<Mesh>(sourceFolder+"SwimMesh.asset");var quality=Save(Refine(source),Folder+"/"+key+"Close.asset");var material=AssetDatabase.LoadAssetAtPath<Material>(sourceFolder+"Surface.mat");material.SetFloat("_Copperband",key=="ReefFish"?1:0);material.SetFloat("_BodyGloss",.055f);material.SetFloat("_EyeGloss",.45f);EditorUtility.SetDirty(material);
   if(material.mainTexture){var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.mainTexture));importer.maxTextureSize=2048;importer.anisoLevel=2;importer.mipmapEnabled=true;importer.streamingMipmaps=true;var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=2048;android.format=TextureImporterFormat.ASTC_4x4;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();}
   foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh==source).ToArray()){
    Touch(filter.gameObject);filter.sharedMesh=quality;var detail=filter.GetComponent<ReefFishDetail>();if(!detail)detail=filter.gameObject.AddComponent<ReefFishDetail>();detail.full=quality;detail.medium=AssetDatabase.LoadAssetAtPath<Mesh>(sourceFolder+"SchoolMedium.asset");detail.distant=AssetDatabase.LoadAssetAtPath<Mesh>(sourceFolder+"SchoolDistant.asset");detail.closeDistance=2;fishCount++;
   }
   report.AppendLine(key+" closeTriangles="+quality.triangles.Length/3+" mediumTriangles="+AssetDatabase.LoadAssetAtPath<Mesh>(sourceFolder+"SchoolMedium.asset").triangles.Length/3);
  }
  foreach(var turtle in UnityEngine.Object.FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None)){
   var anim=turtle.GetComponentInChildren<Animation>();Touch(anim.gameObject);var stroke=anim.GetComponent<ReefImportedAnimation>();if(!stroke)stroke=anim.gameObject.AddComponent<ReefImportedAnimation>();stroke.speed=.72f;stroke.minimumTravelRate=.12f;anim.cullingType=AnimationCullingType.AlwaysAnimate;
   var adapter=turtle.GetComponentInChildren<PreparedTurtleHead>();var skin=turtle.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name.Contains("Body"));Touch(skin.gameObject);var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.name="Turtle continuous soft neck";var points=OctopusRepairValidation.Points(skin);var weights=mesh.boneWeights;int head=Array.IndexOf(skin.bones,adapter.head),neck=Array.IndexOf(skin.bones,adapter.head.parent);if(head<0||neck<0)throw new Exception("Turtle neck bones not found");float joint=turtle.transform.InverseTransformPoint(adapter.head.position).z;
   var headBones=skin.bones.Select(b=>b==adapter.head||b.IsChildOf(adapter.head)).ToArray();
   for(int i=0;i<points.Length;i++){
    var p=turtle.transform.InverseTransformPoint(points[i]);if(Mathf.Abs(p.z-joint)>.13f||Mathf.Abs(p.x)>.22f)continue;var old=weights[i];var ids=new[]{old.boneIndex0,old.boneIndex1,old.boneIndex2,old.boneIndex3};var ws=new[]{old.weight0,old.weight1,old.weight2,old.weight3};float total=0;for(int j=0;j<4;j++)if(headBones[ids[j]]||ids[j]==neck)total+=ws[j];if(total<.8f)continue;float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(joint-.12f,joint+.12f,p.z));weights[i]=new BoneWeight{boneIndex0=head,weight0=blend,boneIndex1=neck,weight1=1-blend};neckVertices++;
   }
   mesh.boneWeights=weights;mesh=Save(mesh,Folder+"/TurtleSoftNeck.asset");skin.sharedMesh=mesh;
  }
  var parent=GameObject.Find("Habitat fish and deep ship expansion").transform;Touch(parent.gameObject);
  foreach(var school in parent.GetComponentsInChildren<FishSchool>().Where(s=>s.name.StartsWith("Supplied school | ")).ToArray()){
   var actors=school.GetComponentsInChildren<FishAgent>();if(actors.Length!=6)continue;Touch(school.gameObject);var extra=new GameObject(school.name.Replace("Supplied school","Reef detail school"));extra.transform.SetParent(parent,false);var center=school.habitatCenter+Vector3.forward*12;center.y=Mathf.Max(center.y,ReefGeometry.Floor(center.x,center.z)+2);extra.transform.position=center;var split=extra.AddComponent<FishSchool>();split.habitatCenter=center;split.habitatRadius=2.5f;split.simulationRadius=28;split.reefEncounter=true;
   for(int i=3;i<6;i++){Touch(actors[i].gameObject);actors[i].transform.SetParent(extra.transform,true);actors[i].transform.position=center+new Vector3((i-4)*.6f,0,(i-3)*.4f);actors[i].GetComponent<ReefAnimalClearance>()?.Enforce();}
  }
  var decoration=new GameObject("Reef detail in open seabed gaps");var templates=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(g=>g.name.StartsWith("Scanned ")&&g.GetLODs().Length>=3).OrderBy(g=>g.GetLODs()[0].renderers.Sum(r=>r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh.triangles.Length:0)).Take(4).ToArray();if(templates.Length==0)throw new Exception("Missing reusable coral templates");
  float[] stations={14,22,46,54,74,86,178,186,206,214,230,238};int clusters=0;
  for(int i=0;i<stations.Length;i++){
   float x=(i%2==0?-1:1)*(7+i%3);float z=stations[i];if(!Floor(x,z,out float ground))continue;var template=templates[i%templates.Length];var prefab=PrefabUtility.GetCorrespondingObjectFromSource(template.gameObject);var colony=prefab?(GameObject)PrefabUtility.InstantiatePrefab(prefab):UnityEngine.Object.Instantiate(template.gameObject);colony.name=template.name+" | infill "+i;colony.transform.SetParent(decoration.transform,false);colony.transform.position=new Vector3(x,ground,z);colony.transform.rotation=Quaternion.Euler(0,i*137.5f,0);colony.transform.localScale=template.transform.localScale*(.5f+(i%3)*.12f);float lift=float.NegativeInfinity;
   foreach(var filter in colony.GetComponentsInChildren<MeshFilter>())foreach(var v in filter.sharedMesh.vertices){var p=filter.transform.TransformPoint(v);if(Floor(p.x,p.z,out float y))lift=Mathf.Max(lift,y-p.y);}if(!float.IsNegativeInfinity(lift))colony.transform.position+=Vector3.up*(lift-.02f);clusters++;
  }
  if(shipPose!=EditorJsonUtility.ToJson(ship.transform))throw new Exception("Ship placement changed");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"/TouchedIds.txt",string.Join("\n",touched));report.AppendLine($"fish={fishCount} neckVertices={neckVertices} infillColonies={clusters} ship=preserved");File.WriteAllText(Output+"/Repairs.txt",report.ToString());Validate();Capture();
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",1);camera.orthographic=true;camera.orthographicSize=.12f;camera.nearClipPlane=.01f;camera.farClipPlane=10;
  foreach(string kind in new[]{"Butterflyfish","Flame angelfish"}){
   var actor=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).First(l=>l.kind==kind&&l.GetComponent<MeshFilter>());camera.transform.position=actor.transform.position+actor.transform.right*.6f;camera.transform.LookAt(actor.transform.position);var r=actor.GetComponent<Renderer>();camera.orthographicSize=r.bounds.size.magnitude*.55f;ReefCorrectionAudit.Capture(camera,Output+"/"+kind.Replace(" ","")+".png");
  }
  EditorSceneManager.OpenScene(Scene);camera=Camera.main;optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",1);var turtle=UnityEngine.Object.FindFirstObjectByType<TurtleSwimmer>();var adapter=turtle.GetComponentInChildren<PreparedTurtleHead>();var anim=turtle.GetComponentInChildren<Animation>();camera.fieldOfView=38;camera.transform.position=adapter.head.position+turtle.transform.right*1.4f+Vector3.up*.25f;camera.transform.LookAt(adapter.head.position);
  for(int pose=0;pose<3;pose++){
   anim.clip.SampleAnimation(anim.gameObject,0);var block=new MaterialPropertyBlock();block.SetFloat("_HeadTurn",(pose-1)*.12f);adapter.control.SetPropertyBlock(block);adapter.Apply();var proxies=new System.Collections.Generic.List<GameObject>();
   foreach(var skin in turtle.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled)){
    var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.ClearBlendShapes();mesh.vertices=OctopusRepairValidation.Points(skin);mesh.RecalculateBounds();mesh.RecalculateNormals();var proxy=new GameObject("Turtle sampled pose");proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;proxies.Add(proxy);
   }
   ReefCorrectionAudit.Capture(camera,Output+"/TurtleNeck"+pose+".png");foreach(var proxy in proxies){UnityEngine.Object.DestroyImmediate(proxy.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(proxy);}foreach(var skin in turtle.GetComponentsInChildren<SkinnedMeshRenderer>())skin.enabled=true;
  }
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var root=GameObject.Find("Habitat fish and deep ship expansion");if(root.GetComponentsInChildren<FishAgent>().Length!=30||root.GetComponentsInChildren<FishSchool>().Length!=10)throw new Exception("Fish distribution changed total population");foreach(var turtle in UnityEngine.Object.FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None)){turtle.Initialize();float travel=0;for(int i=0;i<600;i++){var before=turtle.transform.position;turtle.Step(1f/30);travel+=Vector3.Distance(before,turtle.transform.position);}if(travel<.5f)throw new Exception("Turtle movement stalled");var head=turtle.GetComponentInChildren<PreparedTurtleHead>();float previous=0;for(int i=0;i<120;i++){head.StepLook(1f/72,i<60?.5f:-.5f);if(Mathf.Abs(head.CurrentYaw-previous)>.015f||Mathf.Abs(head.CurrentYaw)>.121f)throw new Exception("Turtle head snaps");previous=head.CurrentYaw;}}
  EditorSceneManager.OpenScene(Scene);var grouper=UnityEngine.Object.FindObjectsByType<WreckCreature>(FindObjectsSortMode.None).First(c=>c.species==WreckCreature.Species.Grouper);float distance=0;for(int i=0;i<600;i++){var before=grouper.transform.position;grouper.Step(1f/30);distance+=Vector3.Distance(before,grouper.transform.position);}if(distance<.5f||grouper.StrokePhase<=0)throw new Exception("Grouper motion/stroke failed");
  foreach(var other in UnityEngine.Object.FindObjectsByType<GrouperStroke>(FindObjectsSortMode.None)){var cruise=other.GetComponent<ReefCruiser>();if(!cruise)throw new Exception("Second grouper has no motion owner");var start=other.transform.position;other.Initialize();for(int i=0;i<600;i++){cruise.Step(1f/30);other.Step(1f/30);}if(Vector3.Distance(start,other.transform.position)<.2f||other.Phase<=0)throw new Exception("Second grouper movement/stroke failed");}
  var rig=new GameObject("Hand coordination test");foreach(bool left in new[]{true,false}){var glove=ScubaGlove.Create(rig.transform,left);if(Vector3.Distance(glove.PalmPosition,glove.GripAnchor.position)>.0001f)throw new Exception("Palm/grip mismatch");UnityEngine.Object.DestroyImmediate(glove.gameObject);}UnityEngine.Object.DestroyImmediate(rig);EditorSceneManager.OpenScene(Scene);RealisticReefBuilder.ValidateScene();Debug.Log("LEAD_REVIEW_VALIDATED turtleTravelAndSmoothHead=true grouperStroke=true palmGripAligned=true");
 }
 public static void Audit(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var log=new System.Text.StringBuilder();
  foreach(var turtle in UnityEngine.Object.FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None)){
   log.AppendLine("TURTLE "+turtle.name+" components="+string.Join(",",turtle.GetComponents<Component>().Select(c=>c.GetType().Name)));
   var head=turtle.GetComponentInChildren<PreparedTurtleHead>();log.AppendLine("HEAD "+head.head.name+" parent="+head.head.parent.name+" position="+head.head.position);
   foreach(var bone in turtle.GetComponentsInChildren<Transform>().Where(t=>t.name.ToLower().Contains("head")||t.name.ToLower().Contains("neck")))log.AppendLine("BONE "+bone.name+" parent="+bone.parent.name+" local="+bone.localPosition);
   foreach(var anim in turtle.GetComponentsInChildren<Animation>())log.AppendLine("CLIP "+anim.clip.name+" duration="+anim.clip.length+" speed="+anim[anim.clip.name].speed+" adapter="+(bool)anim.GetComponent<ReefImportedAnimation>());
  }
  foreach(var actor in UnityEngine.Object.FindObjectsByType<WreckCreature>(FindObjectsSortMode.None).Where(a=>a.species==WreckCreature.Species.Grouper))log.AppendLine("GROUPER "+actor.name+" components="+string.Join(",",actor.GetComponents<Component>().Select(c=>c.GetType().Name))+" shader="+actor.GetComponent<Renderer>().sharedMaterial.shader.name+" bounds="+actor.GetComponent<MeshFilter>().sharedMesh.bounds);
  log.AppendLine("SCHOOLS "+UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None).Length);File.WriteAllText(Output+"/Audit.txt",log.ToString());Debug.Log("LEAD_REVIEW_AUDIT_COMPLETE");
 }
 public static void FinishGroupers(){
  var scene=EditorSceneManager.OpenScene(Scene);int count=0;foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>l.kind=="Grouper")){
   if(life.GetComponent<WreckCreature>())continue;Touch(life.gameObject);life.stationary=true;var old=life.GetComponent<FishBodyMotion>();if(old)old.enabled=false;var cruise=life.GetComponent<ReefCruiser>();if(!cruise)cruise=life.gameObject.AddComponent<ReefCruiser>();cruise.speed=.17f;cruise.radius=1.6f;if(!life.GetComponent<GrouperStroke>())life.gameObject.AddComponent<GrouperStroke>();count++;
  }
  EditorSceneManager.SaveScene(scene);File.WriteAllText(Output+"/GrouperTouchedIds.txt",string.Join("\n",touched));Debug.Log("GROUPER_CONTROLLERS_MATCHED additional="+count);Validate();
 }
}
}
