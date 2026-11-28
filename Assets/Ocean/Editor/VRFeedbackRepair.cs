using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class VRFeedbackRepair {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/VRFeedback/";
 const string Art="Assets/UnderwaterArt/VRFeedback/";
 static Vector3[] Points(Renderer r){if(r is SkinnedMeshRenderer s)return OctopusRepairValidation.Points(s);var f=r.GetComponent<MeshFilter>();return f&&f.sharedMesh?f.sharedMesh.vertices.Select(p=>r.transform.TransformPoint(p)).ToArray():Array.Empty<Vector3>();}
 static Renderer[] Visuals(SeaLife a)=>a.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
 static Bounds BoundsOf(Vector3[] points){var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return b;}
 static T Save<T>(T asset,string name) where T:UnityEngine.Object {var path=Art+name+".asset";var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(asset,path);return asset;}
 static BoxCollider Box(Transform parent,string name,Bounds b){var go=new GameObject(name);go.layer=2;go.transform.SetParent(parent,false);var c=go.AddComponent<BoxCollider>();c.isTrigger=true;c.center=b.center;var scale=parent.lossyScale;var minimum=new Vector3(.008f/Mathf.Max(.0001f,Mathf.Abs(scale.x)),.008f/Mathf.Max(.0001f,Mathf.Abs(scale.y)),.008f/Mathf.Max(.0001f,Mathf.Abs(scale.z)));c.size=Vector3.Max(b.size,minimum);return c;}
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){
   if(a.kind.ToLowerInvariant().Contains("jelly"))FitJelly(a);
   if(a.kind=="Lionfish")FitLionfish(a);
   if(a.kind=="Reef shark")FitShark(a);
  }
  foreach(var d in UnityEngine.Object.FindObjectsByType<DolphinSwimController>(FindObjectsSortMode.None)){d.compactPatrol=false;d.referenceSpeed=1.05f;var w=d.GetComponent<WreckCreature>();w.habitatCenter=d.compactCenter;EditorUtility.SetDirty(w);EditorUtility.SetDirty(d);}
  var moray=UnityEngine.Object.FindFirstObjectByType<CurledMorayEncounter>();moray.restClip.SampleAnimation(moray.animationPlayer.gameObject,0);
  var vertices=Points(moray.skin).Select(p=>moray.transform.InverseTransformPoint(p)).ToArray();var ground=moray.GetComponent<ReefAnimalClearance>();
  ground.groundSupportPoints=vertices.GroupBy(p=>new Vector2Int(Mathf.FloorToInt(p.x/.12f),Mathf.FloorToInt(p.z/.12f))).Select(g=>g.OrderBy(p=>p.y).First()).ToArray();ground.clearance=.008f;
  if(!ground.RequiredHeight(moray.transform.position,moray.transform.rotation,out float floor))throw new Exception("Moray has no physical support");moray.transform.position=new Vector3(moray.transform.position.x,floor,moray.transform.position.z);EditorUtility.SetDirty(ground);
  FitEyes();Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("VR_FEEDBACK_APPLIED");
 }
 static void ResetZone(SeaLife a,out ReefDangerZone zone,out Transform root){
  zone=a.GetComponent<ReefDangerZone>();if(!zone)zone=a.gameObject.AddComponent<ReefDangerZone>();
  if(zone.outlines!=null)foreach(var l in zone.outlines)if(l)l.enabled=false;
  if(zone.cautionIndicator)zone.cautionIndicator.gameObject.SetActive(false);
  foreach(var t in a.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Fitted jelly tissue").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  foreach(var c in a.GetComponentsInChildren<Collider>(true))c.enabled=false;
  var old=a.transform.Find("Fitted touch surfaces");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  root=new GameObject("Fitted touch surfaces").transform;root.SetParent(a.transform,false);zone.owner=a;zone.outlines=Array.Empty<LineRenderer>();zone.glowTargets=Array.Empty<Renderer>();zone.cautionDistance=.055f;zone.proximityDistance=.12f;zone.contactDistance=.008f;
 }
 public static void FitJelly(SeaLife a){
  var visuals=Visuals(a);ResetZone(a,out var zone,out var root);var surfaces=new System.Collections.Generic.List<Collider>();
  foreach(var r in visuals){var world=Points(r);if(world.Length==0)continue;
   if(r is SkinnedMeshRenderer skin&&skin.bones.Length>0){
    var weights=skin.sharedMesh.boneWeights;
    foreach(var group in Enumerable.Range(0,world.Length).GroupBy(i=>{var w=weights[i];int bone=w.boneIndex0;float weight=w.weight0;if(w.weight1>weight){bone=w.boneIndex1;weight=w.weight1;}if(w.weight2>weight){bone=w.boneIndex2;weight=w.weight2;}if(w.weight3>weight)bone=w.boneIndex3;return bone;})){
     if(group.Count()<4)continue;var bone=skin.bones[group.Key];var b=BoundsOf(group.Select(i=>bone.InverseTransformPoint(world[i])).ToArray());var holder=new GameObject("Fitted jelly tissue").transform;holder.SetParent(root,false);holder.SetParent(bone,true);holder.localPosition=Vector3.zero;holder.localRotation=Quaternion.identity;holder.localScale=Vector3.one;surfaces.Add(Box(holder,"Jelly tissue contact",b));
    }
   }else{
    var points=world.Select(p=>a.transform.InverseTransformPoint(p)).ToArray();var b=BoundsOf(points);float cut=b.max.y-b.size.y*.24f;
    var bell=points.Where(p=>p.y>=cut).ToArray();if(bell.Length>3)surfaces.Add(Box(root,"Thin bell contact",BoundsOf(bell)));
    foreach(var group in points.Where(p=>p.y<cut).GroupBy(p=>new Vector2Int(Mathf.FloorToInt((Mathf.Atan2(p.z-b.center.z,p.x-b.center.x)+Mathf.PI)/(Mathf.PI*2)*8)%8,Mathf.FloorToInt((cut-p.y)/Mathf.Max(.08f,b.size.y*.26f)))))if(group.Count()>3)surfaces.Add(Box(root,"Tentacle contact",BoundsOf(group.ToArray())));
   }
  }
  zone.surfaces=surfaces.ToArray();a.cautionEnabled=true;a.cautionVoice=Resources.Load<AudioClip>(a.kind=="Box jelly"?"DangerBoxJelly":"DangerMoonJelly");
  if(a.kind!="Box jelly")a.consequence=false;EditorUtility.SetDirty(a);EditorUtility.SetDirty(zone);
 }
 static void FitLionfish(SeaLife a){
  var points=Visuals(a).SelectMany(Points).Select(p=>a.transform.InverseTransformPoint(p)).ToArray();var bounds=BoundsOf(points);ResetZone(a,out var zone,out var root);
  // Fit the warning to actual visible fins rather than the displaced former model.
  var mesh=new Mesh{name="Lionfish contact hull"};var chosen=points.Where((p,i)=>i%Mathf.Max(1,points.Length/160)==0).Take(180).ToArray();mesh.vertices=chosen;var triangles=new System.Collections.Generic.List<int>();for(int i=1;i<chosen.Length-1;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);}mesh.triangles=triangles.ToArray();mesh=Save(mesh,"LionfishContactHull");var hull=root.gameObject.AddComponent<MeshCollider>();root.gameObject.layer=2;hull.sharedMesh=mesh;hull.convex=true;hull.isTrigger=true;zone.surfaces=new Collider[]{hull};
  var solid=new GameObject("Fitted lionfish body");solid.layer=9;solid.transform.SetParent(root,false);var capsule=solid.AddComponent<CapsuleCollider>();capsule.direction=2;capsule.center=bounds.center;capsule.radius=Mathf.Min(bounds.extents.x,bounds.extents.y)*.65f;capsule.height=bounds.size.z*.72f;
  var body=a.GetComponent<ReefSphereBody>();if(body){body.centerOffset=bounds.center;EditorUtility.SetDirty(body);}var guard=a.GetComponent<ReefAnimalClearance>();if(guard){guard.animatedBody=bounds;guard.animatedBody.Expand(.06f);EditorUtility.SetDirty(guard);guard.Enforce();}EditorUtility.SetDirty(zone);
 }
 static void FitShark(SeaLife a){
  var points=Visuals(a).SelectMany(Points).Select(p=>a.transform.InverseTransformPoint(p)).ToArray();var b=BoundsOf(points);var old=a.transform.Find("Fitted shark body");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var go=new GameObject("Fitted shark body");go.layer=9;go.transform.SetParent(a.transform,false);var c=go.AddComponent<CapsuleCollider>();c.direction=2;c.center=b.center;c.radius=Mathf.Min(b.extents.x*.5f,b.extents.y*.65f);c.height=b.size.z*.82f;
  var shark=a.GetComponent<OpenWaterShark>();shark.patrolRadius=12;EditorUtility.SetDirty(shark);
  var animation=a.GetComponentInChildren<ReefImportedAnimation>();if(animation){animation.speed=.52f;animation.minimumTravelRate=.35f;animation.distanceClock=true;animation.travelActor=a.transform;animation.referenceTravelSpeed=1.05f;EditorUtility.SetDirty(animation);}
 }
 static void FitEyes(){
  var cache=new System.Collections.Generic.Dictionary<Mesh,Mesh>();int count=0;
  foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   if(!f.GetComponent<FishAgent>()&&!f.GetComponent<FishBodyMotion>())continue;var r=f.GetComponent<Renderer>();if(!r||!r.enabled||!r.sharedMaterial||r.sharedMaterial.shader.name!="Underwater/Living Fish Skin")continue;
   var source=f.sharedMesh;if(!cache.TryGetValue(source,out var mesh)){
    var tags=source.uv2;mesh=UnityEngine.Object.Instantiate(source);mesh.name=source.name+" visible eyes";var v=mesh.vertices;
    if(tags.Length==v.Length&&tags.Any(t=>t.x>.5f)){
     foreach(int side in new[]{-1,1})foreach(int part in new[]{1,2}){var indices=Enumerable.Range(0,v.Length).Where(i=>Mathf.RoundToInt(tags[i].x)==part&&Mathf.Sign(v[i].x)==side).ToArray();if(indices.Length==0)continue;var center=indices.Aggregate(Vector3.zero,(p,i)=>p+v[i])/indices.Length;foreach(var i in indices)v[i]=center+(v[i]-center)*1.35f+Vector3.right*side*.003f;}
     mesh.vertices=v;mesh.RecalculateBounds();mesh.RecalculateNormals();
    }else{
     UnityEngine.Object.DestroyImmediate(mesh);var b=BoundsOf(source.vertices);var fit=typeof(ReefRealismPass).GetMethod("WithEyes",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);mesh=(Mesh)fit.Invoke(null,new object[]{source,b.center.y-b.size.y*.055f,b.max.z-b.size.z*.085f,b.size.z*.020f,1f});
    }
    mesh=Save(mesh,"Eyes_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)));cache[source]=mesh;
   }f.sharedMesh=mesh;EditorUtility.SetDirty(f);var detail=f.GetComponent<ReefFishDetail>();if(detail){detail.full=mesh;detail.medium=Save(FishDistanceMesh.Create(mesh,48),mesh.name+"_Medium");detail.distant=Save(FishDistanceMesh.Create(mesh,24),mesh.name+"_Distant");EditorUtility.SetDirty(detail);}count++;
  }Debug.Log("VR_FISH_EYES_FITTED count="+count);
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();Directory.CreateDirectory(Output);var report=new StringBuilder();int warnings=0;
  foreach(var zone in UnityEngine.Object.FindObjectsByType<ReefDangerZone>(FindObjectsSortMode.None)){
   if(!zone.owner||zone.surfaces==null||!zone.surfaces.Any(c=>c&&c.enabled))throw new Exception("Missing active warning surface: "+zone.name);
   foreach(var c in zone.surfaces.Where(c=>c&&c.enabled)){Physics.SyncTransforms();var point=c.ClosestPoint(c.bounds.center);float near=zone.SurfaceDistance(point,0);if(near>.001f)throw new Exception("Collider contact missed");zone.ResetEpisode();var signals=zone.Step(0,10,true);if(!signals.warn)throw new Exception("No warning on touch: "+zone.owner.kind);zone.MarkWarningSpoken(10);if(zone.Step(0,10.05f,true).contact)throw new Exception("Contact consequence before warning lead");if(zone.Step(zone.proximityDistance+.1f,11,true).warn)throw new Exception("Warning outside surface range");warnings++;}
  }
  report.AppendLine("Warning volumes tested="+warnings+"; surface contact, distant silence and warning-before-consequence passed.");
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind.ToLowerInvariant().Contains("jelly")))if(a.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))throw new Exception("Jelly still has an oversized solid barrier");
  report.AppendLine("All jellies use fitted trigger surfaces; moon/comb jellies warn without damage.");
  var moray=UnityEngine.Object.FindFirstObjectByType<CurledMorayEncounter>();moray.Initialize();var start=moray.transform.position;var ground=moray.GetComponent<ReefAnimalClearance>();Physics.SyncTransforms();if(!moray.RequestTouch(moray.touchVolumes[2].bounds.center))throw new Exception("Moray touch missed");float maxLift=0,maxStep=0;
  for(int i=0;i<100;i++){var before=moray.transform.position;moray.Step(.05f);ground.Enforce();Physics.SyncTransforms();maxLift=Mathf.Max(maxLift,moray.transform.position.y-start.y);maxStep=Mathf.Max(maxStep,Vector3.Distance(before,moray.transform.position));if(moray.State==CurledMorayEncounter.Phase.Rising||moray.State==CurledMorayEncounter.Phase.Uncurling||moray.State==CurledMorayEncounter.Phase.Swimming)throw new Exception("Moray resumed aerial sequence");}
  if(maxLift>.07f||maxStep>.025f||moray.State!=CurledMorayEncounter.Phase.Resting)throw new Exception("Moray retreat jumped or failed to settle");report.AppendLine("Moray grounded retreat: max lift="+maxLift+" max frame step="+maxStep+" travel="+moray.LastTravel);
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var dolphins=UnityEngine.Object.FindObjectsByType<DolphinSwimController>(FindObjectsSortMode.None).OrderBy(d=>d.podIndex).ToArray();var travel=new float[dolphins.Length];var stopped=new float[dolphins.Length];float longestStop=0,minGap=1000,maxRadius=0,maxYaw=0;
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();float sharkTravel=0,sharkStop=0,longestSharkStop=0;
  if(!shark.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger&&c.gameObject.layer==9))throw new Exception("Shark lacks player collider");
  for(int frame=0;frame<3600;frame++){
   for(int i=0;i<dolphins.Length;i++){var d=dolphins[i];if(d.compactPatrol)throw new Exception("Dolphin still on timed oval");var before=d.transform.position;var forward=d.transform.forward;d.Step(.05f);d.GetComponent<ReefAnimalClearance>().Enforce();float moved=(d.transform.position-before).magnitude;travel[i]+=moved;stopped[i]=moved<.0025f?stopped[i]+.05f:0;longestStop=Mathf.Max(longestStop,stopped[i]);maxRadius=Mathf.Max(maxRadius,Vector3.ProjectOnPlane(d.transform.position-d.GetComponent<WreckCreature>().habitatCenter,Vector3.up).magnitude);maxYaw=Mathf.Max(maxYaw,Vector3.Angle(forward,d.transform.forward)/.05f);if(!d.GetComponent<ReefSphereBody>().Clear(d.transform.position))throw new Exception("Dolphin collided with reef");}
   for(int i=0;i<dolphins.Length;i++)for(int j=i+1;j<dolphins.Length;j++)minGap=Mathf.Min(minGap,Vector3.Distance(dolphins[i].transform.position,dolphins[j].transform.position));
   var p=shark.transform.position;shark.Step(.05f,OceanWorld.Centers[6],Vector3.zero,true);shark.GetComponent<ReefAnimalClearance>().Enforce();float movedShark=(shark.transform.position-p).magnitude;sharkTravel+=movedShark;sharkStop=movedShark<.0025f?sharkStop+.05f:0;longestSharkStop=Mathf.Max(longestSharkStop,sharkStop);if(!shark.GetComponent<ReefSphereBody>().Clear(shark.transform.position))throw new Exception("Shark collided with reef");
  }
  report.AppendLine("180s free dolphin swimming: travel="+string.Join(",",travel)+" longest slow/stop="+longestStop+" min pod gap="+minGap+" range="+maxRadius+" max yaw="+maxYaw);
  report.AppendLine("180s shark cruise: travel="+sharkTravel+" longest slow/stop="+longestSharkStop+" player body collider=true");
  File.WriteAllText(Output+"Validation.txt",report.ToString());if(travel.Any(t=>t<65)||longestStop>4||minGap<2.8f||maxYaw>38||sharkTravel<80||longestSharkStop>4)throw new Exception("Motion quality limits failed: "+report);
  EditorSceneManager.OpenScene(Scene);var rig=new GameObject("Hand alignment validation");try{foreach(bool left in new[]{true,false}){var hand=ScubaGlove.Create(rig.transform,left);hand.SetControllerFit(Vector3.zero,Vector3.zero,left);foreach(var angles in new[]{Vector3.zero,new Vector3(-90,0,0),new Vector3(23,78,-34)}){hand.transform.localRotation=Quaternion.Euler(angles);var mesh=hand.GetComponentInChildren<SkinnedMeshRenderer>().transform;var palm=mesh.TransformPoint(new Vector3(hand.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.bounds.center.x,0,-.015f));if(Vector3.Distance(palm,hand.transform.position)>.0001f)throw new Exception("Palm not centered on controller grip");}UnityEngine.Object.DestroyImmediate(hand.gameObject);}}finally{UnityEngine.Object.DestroyImmediate(rig);}report.AppendLine("Left/right palm centering passed across neutral, upright and rotated controller poses. Physical fit still requires headset confirmation.");File.WriteAllText(Output+"Validation.txt",report.ToString());Debug.Log("VR_FEEDBACK_VALIDATED\n"+report);
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var cam=Camera.main;cam.nearClipPlane=.01f;cam.fieldOfView=45;var optics=cam.GetComponent<ReefOptics>();if(optics)optics.enabled=false;
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.grabbable).GroupBy(a=>a.kind).Select(g=>g.First())){var visuals=Visuals(a);if(visuals.Length==0)continue;var p=visuals.SelectMany(Points).ToArray();if(p.Length==0)continue;var b=BoundsOf(p);cam.transform.position=b.center+(a.transform.right+a.transform.forward*.18f)*Mathf.Max(.3f,b.size.magnitude*1.2f);cam.transform.LookAt(b.center);UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(cam);ReefCorrectionAudit.Capture(cam,Output+"Fish_"+a.kind.Replace(" ","_")+".png");}
  var moray=UnityEngine.Object.FindFirstObjectByType<CurledMorayEncounter>();moray.Initialize();var center=BoundsOf(Points(moray.skin)).center;cam.transform.position=center+moray.transform.right*2+Vector3.up*.65f;cam.transform.LookAt(center);UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(cam);ReefCorrectionAudit.Capture(cam,Output+"MorayGrounded.png");
  var rig=new GameObject("Glove review");rig.transform.position=new Vector3(900,2,0);foreach(bool left in new[]{true,false}){var hand=ScubaGlove.Create(rig.transform,left);hand.transform.localPosition=new Vector3(left?-.15f:.15f,0,0);hand.transform.localRotation=Quaternion.Euler(-90,0,0);hand.Animate(.12f,.12f,1);}cam.transform.position=rig.transform.position+new Vector3(0,.45f,.5f);cam.transform.LookAt(rig.transform.position);RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.35f,.4f,.45f);ReefCorrectionAudit.Capture(cam,Output+"Hands.png");UnityEngine.Object.DestroyImmediate(rig);Debug.Log("VR_FEEDBACK_CAPTURED");
 }
 public static void VisualReview(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);
  foreach(var shaderName in new[]{"Underwater/Neoprene Glove","Underwater/Living Fish Skin"}){var shader=Shader.Find(shaderName);if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Shader failed: "+shaderName);}
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();var skin=Visuals(shark.GetComponent<SeaLife>()).OfType<SkinnedMeshRenderer>().First();var animation=shark.GetComponentInChildren<ReefImportedAnimation>();var flex=shark.GetComponentInChildren<SharkBodyMotion>();
  var initial=Points(skin).Select(p=>shark.transform.InverseTransformPoint(p)).ToArray();float minZ=initial.Min(p=>p.z),maxZ=initial.Max(p=>p.z);var indices=Enumerable.Range(0,initial.Length).Where(i=>initial[i].z<minZ+(maxZ-minZ)*.18f).ToArray();var samples=new System.Collections.Generic.List<Vector3>();
  for(int i=0;i<180;i++){shark.Step(1f/60,OceanWorld.Centers[6],Vector3.zero,true);animation.StepPlayback(1f/60);if(flex)flex.Step(1f/60);var points=Points(skin);samples.Add(indices.Aggregate(Vector3.zero,(p,k)=>p+shark.transform.InverseTransformPoint(points[k]))/indices.Length);}
  float lateral=samples.Max(p=>p.x)-samples.Min(p=>p.x),vertical=samples.Max(p=>p.y)-samples.Min(p=>p.y);if(lateral<.04f||lateral<vertical*1.4f)throw new Exception("Shark lacks a lateral caudal stroke: lateral="+lateral+" vertical="+vertical);File.AppendAllText(Output+"Validation.txt","Shark tail stroke: lateral="+lateral+" local m; vertical="+vertical+" local m; both repaired shaders compiled.\n");
  var cam=Camera.main;var center=BoundsOf(Points(skin)).center;cam.transform.position=center+shark.transform.right*9+Vector3.up;cam.transform.LookAt(center);cam.fieldOfView=45;UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(cam);ReefCorrectionAudit.Capture(cam,Output+"SharkStroke.png");Capture();Debug.Log("VR_VISUAL_REVIEW_PASSED");
 }
 public static void AlignMorayToSand(){
  var scene=EditorSceneManager.OpenScene(Scene);var moray=UnityEngine.Object.FindFirstObjectByType<CurledMorayEncounter>();moray.restClip.SampleAnimation(moray.animationPlayer.gameObject,0);Physics.SyncTransforms();var center=BoundsOf(Points(moray.skin)).center;
  if(Physics.Raycast(center+Vector3.up*.4f,Vector3.down,out var hit,2,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore)&&hit.normal.y>.65f)moray.transform.rotation=Quaternion.FromToRotation(moray.transform.up,hit.normal)*moray.transform.rotation;
  var ground=moray.GetComponent<ReefAnimalClearance>();if(!ground.RequiredHeight(moray.transform.position,moray.transform.rotation,out var y))throw new Exception("Missing moray sand support");moray.transform.position=new Vector3(moray.transform.position.x,y,moray.transform.position.z);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Finish();
 }
 public static void Finish(){Validate();VisualReview();}
 public static void Run(){Apply();Finish();}
 public static void Inspect(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();Directory.CreateDirectory(Output);var report=new StringBuilder();
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.danger||a.kind.Contains("jelly")||a.kind=="Moray"||a.kind=="Reef shark"||a.kind=="Dolphin").OrderBy(a=>a.kind)){
   report.AppendLine(a.kind+" | "+a.name+" position="+a.transform.position+" scale="+a.transform.lossyScale+" danger="+a.danger+" caution="+a.cautionEnabled+" consequence="+a.consequence+" observation="+a.observationOnly);
   foreach(var r in a.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)))report.AppendLine("  VISUAL "+r.name+" bounds="+r.bounds+" material="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+":"+m.shader.name:"null")));
   foreach(var c in a.GetComponentsInChildren<Collider>(true))report.AppendLine("  COLLIDER "+c.name+" "+c.GetType().Name+" enabled="+c.enabled+" trigger="+c.isTrigger+" layer="+c.gameObject.layer+" bounds="+c.bounds);
   var zone=a.GetComponent<ReefDangerZone>();if(zone)report.AppendLine("  ZONE "+EditorJsonUtility.ToJson(zone));
   var guard=a.GetComponent<ReefAnimalClearance>();if(guard)report.AppendLine("  GROUND "+EditorJsonUtility.ToJson(guard));
  }
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.grabbable).GroupBy(a=>a.kind).Select(g=>g.First())){
   report.AppendLine("FISH "+a.kind);
   foreach(var r in a.GetComponentsInChildren<Renderer>(true))if(r.name.ToLowerInvariant().Contains("eye"))report.AppendLine("  EYE "+r.name+" enabled="+r.enabled+" scale="+r.transform.localScale+" pos="+r.transform.localPosition+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));
  }
  File.WriteAllText(Output+"Before.txt",report.ToString());Debug.Log("VR_FEEDBACK_INSPECTED");
 }
}
}
