using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class CreatureMotionFollowup {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Output="Renders/CreatureMotionFollowup/",Art="Assets/UnderwaterArt/CreatureMotionFollowup/";
 static T Save<T>(T asset,string name) where T:UnityEngine.Object{string path=Art+name+(asset is Material?".mat":".asset");var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(asset,path);return asset;}
 static Bounds BoundsOf(Vector3[] p){var b=new Bounds(p[0],Vector3.zero);foreach(var v in p)b.Encapsulate(v);return b;}
 static Vector3[] Points(Renderer r){if(r is SkinnedMeshRenderer s)return OctopusRepairValidation.Points(s);var f=r.GetComponent<MeshFilter>();return f&&f.sharedMesh?f.sharedMesh.vertices.Select(p=>r.transform.TransformPoint(p)).ToArray():Array.Empty<Vector3>();}
 static Renderer[] Visible(SeaLife life)=>life.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);Directory.CreateDirectory(Art);AssetDatabase.Refresh();int groupers=0,jellies=0,eyes=0;
  var meshes=new System.Collections.Generic.Dictionary<Mesh,Mesh>();
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){
   if(life.kind=="Grouper"){var swim=life.GetComponent<GrouperSwimController>();if(!swim)swim=life.gameObject.AddComponent<GrouperSwimController>();var old=life.GetComponent<ReefCruiser>();if(old)old.enabled=false;life.stationary=true;EditorUtility.SetDirty(life);groupers++;}
   if(life.kind=="Comb jelly"){
    VRFeedbackRepair.FitJelly(life);var body=life.GetComponent<ReefSphereBody>();body.radius=.18f;EditorUtility.SetDirty(body);
    var zone=life.GetComponent<ReefDangerZone>();zone.cautionDistance=.025f;zone.proximityDistance=.06f;foreach(var box in zone.surfaces.OfType<BoxCollider>())box.size*=.92f;EditorUtility.SetDirty(zone);jellies++;
   }
   foreach(var r in Visible(life)){
    var mats=r.sharedMaterials;bool changed=false;
    for(int slot=0;slot<mats.Length;slot++){
     var source=mats[slot];if(!source||!new[]{"Underwater/Living Fish Skin","Underwater/Instanced Marine","Underwater/Skinned Grouper"}.Contains(source.shader.name))continue;
     var material=new Material(source){name=life.kind+" natural skin and eyes",enableInstancing=true};
     var mesh=r is SkinnedMeshRenderer skin?skin.sharedMesh:r.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var tags=mesh.uv2;var b=BoundsOf(vertices);
     float z=b.max.z-b.size.z*.105f,y=b.center.y+b.size.y*.035f;
     if(source.HasProperty("_SurfaceEyes")&&source.GetFloat("_SurfaceEyes")>.5f){var previous=source.GetVector("_EyeParams");z=previous.x;y=previous.y;}
     if(tags.Length==vertices.Length){var iris=Enumerable.Range(0,vertices.Length).Where(i=>tags[i].x>.5f&&tags[i].x<1.5f).ToArray();if(iris.Length>0){var center=iris.Aggregate(Vector3.zero,(v,i)=>v+vertices[i])/iris.Length;z=center.z;y=center.y;}}
     if(life.kind=="Lumen"||life.kind=="Grouper"){z=b.min.z+b.size.z*.945f;y=b.center.y-b.size.y*.067f;}
     float size=b.size.z*(life.kind=="Lumen"||life.kind=="Grouper"?.017f:life.kind=="Clownfish"||life.kind=="Royal gramma"?.022f:life.kind=="Lyretail anthias"||life.kind=="Copperband butterflyfish"?.019f:.018f);
     Color irisColor=life.kind=="Blue tang"?new Color(.18f,.13f,.075f):life.kind=="Yellow tang"?new Color(.40f,.28f,.055f):life.kind=="Clownfish"?new Color(.31f,.16f,.055f):life.kind=="Royal gramma"?new Color(.46f,.26f,.07f):life.kind=="Lyretail anthias"?new Color(.45f,.18f,.075f):life.kind=="Copperband butterflyfish"?new Color(.24f,.18f,.09f):new Color(.32f,.23f,.09f);
     material.SetFloat("_SurfaceEyes",1);material.SetVector("_EyeParams",new Vector4(z,y,size,size*.94f));material.SetColor("_EyeIris",irisColor);
     if(life.kind=="Lumen"){material.SetFloat("_TailAmplitude",.055f);material.SetFloat("_BodyGloss",.12f);material.SetFloat("_FillLight",.4f);}
     string key=life.kind.Replace(" ","_")+"_"+slot;mats[slot]=Save(material,key);changed=true;eyes++;
     if(r is SkinnedMeshRenderer skinned&&source.shader.name=="Underwater/Skinned Grouper"){
      if(!meshes.TryGetValue(mesh,out var headMesh)){headMesh=UnityEngine.Object.Instantiate(mesh);headMesh.uv3=vertices.Select(v=>new Vector2(v.z,v.y)).ToArray();headMesh=Save(headMesh,"GrouperHeadCoordinates");meshes[mesh]=headMesh;}skinned.sharedMesh=headMesh;EditorUtility.SetDirty(skinned);
     }
     if(tags.Length==vertices.Length&&tags.Any(t=>t.x>.5f)&&r is MeshRenderer){
      if(!meshes.TryGetValue(mesh,out var bodyMesh)){bodyMesh=UnityEngine.Object.Instantiate(mesh);var triangles=mesh.triangles;var keep=new System.Collections.Generic.List<int>();for(int i=0;i<triangles.Length;i+=3)if(tags[triangles[i]].x<.5f&&tags[triangles[i+1]].x<.5f&&tags[triangles[i+2]].x<.5f){keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}var used=keep.Distinct().OrderBy(i=>i).ToArray();var remap=used.Select((old,index)=>new {old,index}).ToDictionary(p=>p.old,p=>p.index);var normals=mesh.normals;var uv=mesh.uv;var tangents=mesh.tangents;bodyMesh.Clear();bodyMesh.vertices=used.Select(i=>vertices[i]).ToArray();bodyMesh.normals=used.Select(i=>normals[i]).ToArray();bodyMesh.uv=used.Select(i=>uv[i]).ToArray();bodyMesh.uv2=new Vector2[used.Length];if(tangents.Length==vertices.Length)bodyMesh.tangents=used.Select(i=>tangents[i]).ToArray();bodyMesh.triangles=keep.Select(i=>remap[i]).ToArray();bodyMesh.RecalculateBounds();bodyMesh=Save(bodyMesh,"Body_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)));meshes[mesh]=bodyMesh;}
      r.GetComponent<MeshFilter>().sharedMesh=bodyMesh;var lod=r.GetComponent<ReefFishDetail>();if(lod){lod.full=bodyMesh;lod.medium=Save(FishDistanceMesh.Create(bodyMesh,48),key+"_Medium");lod.distant=Save(FishDistanceMesh.Create(bodyMesh,24),key+"_Distant");EditorUtility.SetDirty(lod);}
     }
    }
    if(changed){r.sharedMaterials=mats;EditorUtility.SetDirty(r);}
   }
  }
  var moray=UnityEngine.Object.FindFirstObjectByType<CurledMorayEncounter>();foreach(var c in moray.touchVolumes){c.enabled=true;c.isTrigger=false;c.gameObject.layer=9;EditorUtility.SetDirty(c);}
  Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MOTION_FOLLOWUP_APPLIED groupers="+groupers+" combJellies="+jellies+" eyeMaterials="+eyes);
 }
 public static void Inspect(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var sb=new StringBuilder();
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Lumen"||a.kind=="Grouper"||a.kind=="Moray"||a.kind=="Comb jelly").GroupBy(a=>a.kind).Select(g=>g.First())){
   sb.AppendLine(a.kind+" pos="+a.transform.position);
   foreach(var c in a.GetComponents<Component>())sb.AppendLine("COMPONENT "+c.GetType().Name+" "+EditorJsonUtility.ToJson(c));
   foreach(var r in a.GetComponentsInChildren<Renderer>(true))if(r is MeshRenderer||r is SkinnedMeshRenderer){sb.AppendLine("RENDER "+r.name+" enabled="+r.enabled+" "+r.GetType().Name+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?AssetDatabase.GetAssetPath(m)+":"+m.shader.name:"null")));if(r is SkinnedMeshRenderer s)sb.AppendLine("BONES "+string.Join(",",s.bones.Select(b=>b.name)));}
   foreach(var c in a.GetComponentsInChildren<Collider>(true))sb.AppendLine("COLLIDER "+c.name+" "+c.GetType().Name+" active="+c.enabled+" trigger="+c.isTrigger+" localScale="+c.transform.lossyScale+" "+EditorJsonUtility.ToJson(c));
  }
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.grabbable).GroupBy(a=>a.kind).Select(g=>g.First())){sb.AppendLine("FISH "+a.kind);foreach(var r in a.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)))foreach(var m in r.sharedMaterials)sb.AppendLine("MAT "+AssetDatabase.GetAssetPath(m)+" "+m.shader.name+" "+EditorJsonUtility.ToJson(m));}
  File.WriteAllText(Output+"Before.txt",sb.ToString());Debug.Log("MOTION_FOLLOWUP_INSPECTED");
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();Directory.CreateDirectory(Output);var report=new StringBuilder();
  var lives=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None);var lumen=lives.Single(a=>a.kind=="Lumen");var follower=lumen.GetComponent<ReefFishFollower>();var motion=lumen.GetComponent<FishBodyMotion>();var home=lumen.transform.position;float travel=0,maxTurn=0,maxStep=0;motion.Initialize();
  for(int i=0;i<3600;i++){float t=i*.01666667f;Vector3 viewer=home+new Vector3(Mathf.Sin(t*.09f)*3,0,Mathf.Cos(t*.09f)*3);if(t>20&&t<35)viewer=home+new Vector3(Mathf.Sin(20*.09f)*3,0,Mathf.Cos(20*.09f)*3);var before=lumen.transform.position;var forward=lumen.transform.forward;follower.Step(1f/60,viewer,Vector3.forward);motion.Step(1f/60);float step=Vector3.Distance(before,lumen.transform.position);travel+=step;maxStep=Mathf.Max(maxStep,step);maxTurn=Mathf.Max(maxTurn,Vector3.Angle(forward,lumen.transform.forward)*60);if(!lumen.GetComponent<ReefSphereBody>().Clear(lumen.transform.position))throw new Exception("Lumen hit reef");}
  var block=new MaterialPropertyBlock();lumen.GetComponent<Renderer>().GetPropertyBlock(block);if(travel<5||maxStep>.026f||maxTurn>42||block.GetFloat("_TailAmplitude")<.034f||motion.Phase<20)throw new Exception("Lumen motion or stroke failed: travel="+travel+" turn="+maxTurn+" phase="+motion.Phase);report.AppendLine("Lumen: 60-second follow/stop/resume test, travel="+travel+" m, max step="+maxStep+" m, max turn="+maxTurn+" deg/s, tail amplitude="+block.GetFloat("_TailAmplitude")+" m.");
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var groupers=UnityEngine.Object.FindObjectsByType<GrouperSwimController>(FindObjectsSortMode.None);foreach(var g in groupers){var stroke=g.GetComponent<GrouperStroke>();float distance=0,maxYaw=0,maxJump=0,stop=0,longestStop=0;for(int i=0;i<3600;i++){var before=g.transform.position;g.Step(.05f);if(stroke)stroke.Step(.05f);g.GetComponent<ReefAnimalClearance>().Enforce();float moved=(g.transform.position-before).magnitude;distance+=moved;maxYaw=Mathf.Max(maxYaw,g.TurnRate);maxJump=Mathf.Max(maxJump,moved);stop=moved<.001f?stop+.05f:0;longestStop=Mathf.Max(longestStop,stop);if(!g.GetComponent<ReefSphereBody>().Clear(g.transform.position))throw new Exception("Grouper hit reef");}report.AppendLine("Grouper "+g.name+": 180 seconds, travel="+distance+" m, max turn="+maxYaw+" deg/s, max step="+maxJump+" m, longest stop="+longestStop+" s.");if(distance<12||maxYaw>20||maxJump>.022f||longestStop>6)throw new Exception("Grouper motion failed: "+report);}
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();int jellies=0,contacts=0;
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.kind=="Comb jelly")){var visual=BoundsOf(Visible(a).SelectMany(Points).ToArray());var zone=a.GetComponent<ReefDangerZone>();foreach(var c in zone.surfaces){if(!c||!c.enabled||!c.isTrigger)throw new Exception("Invalid comb sensor");var size=c.bounds.size;if(size.x>visual.size.x+.03f||size.y>visual.size.y+.03f||size.z>visual.size.z+.03f)throw new Exception("Comb sensor larger than rendered jelly: "+size+" vs "+visual.size);zone.ResetEpisode();if(!zone.Step(0,10,true).warn)throw new Exception("Comb contact warning lost");contacts++;}jellies++;}report.AppendLine("Comb jellies="+jellies+", fitted sensor volumes="+contacts+"; no metre-sized minimum; surface warnings passed.");
  var moray=UnityEngine.Object.FindFirstObjectByType<CurledMorayEncounter>();moray.Initialize();if(moray.touchVolumes.Any(c=>!c||!c.enabled||c.isTrigger||c.gameObject.layer!=9))throw new Exception("Moray body collider missing");var start=Points(moray.skin);var initial=moray.transform.position;Physics.SyncTransforms();var hand=moray.touchVolumes[0].ClosestPoint(moray.touchVolumes[0].bounds.center);moray.Step(.05f,hand);float reaction=0,deformation=0,lift=0;
  for(int i=0;i<90;i++){moray.Step(.05f);moray.GetComponent<ReefAnimalClearance>().Enforce();Physics.SyncTransforms();reaction=Mathf.Max(reaction,moray.ReactionWeight);lift=Mathf.Max(lift,moray.transform.position.y-initial.y);if(i==8){var posed=Points(moray.skin);for(int v=0;v<start.Length;v++)deformation=Mathf.Max(deformation,(posed[v]-start[v]).magnitude);}}
  if(reaction<.8f||deformation<.035f||moray.CompletedEscapes<1||lift>.08f)throw new Exception("Moray collider response failed: weight="+reaction+" deformation="+deformation+" lift="+lift);report.AppendLine("Moray: "+moray.touchVolumes.Length+" solid body sections; palm collision triggered weight="+reaction+", visible vertex movement="+deformation+" m, max lift="+lift+" m.");
  foreach(var shaderName in new[]{"Underwater/Living Fish Skin","Underwater/Instanced Marine","Underwater/Skinned Grouper"})if(ShaderUtil.ShaderHasError(Shader.Find(shaderName)))throw new Exception("Shader error: "+shaderName);
  var materials=AssetDatabase.FindAssets("t:Material",new[]{Art}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();foreach(var m in materials)if(m.GetFloat("_SurfaceEyes")<.5f||m.GetVector("_EyeParams").z<=.001f||m.GetVector("_EyeParams").w<=.001f)throw new Exception("Missing textured eye pattern");report.AppendLine("Species skin/eye materials="+materials.Length+"; all three skin shaders compiled.");File.WriteAllText(Output+"Validation.txt",report.ToString());Debug.Log("MOTION_FOLLOWUP_VALIDATED\n"+report);
 }
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var camera=Camera.main;camera.nearClipPlane=.01f;camera.fieldOfView=42;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;
  foreach(var a in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(a=>a.grabbable||a.kind=="Lumen"||a.kind=="Grouper"||a.kind=="Moray").GroupBy(a=>a.kind).Select(g=>g.First())){
   var skin=Visible(a);if(skin.Length==0)continue;var bounds=BoundsOf(skin.SelectMany(Points).ToArray());camera.transform.position=bounds.center+(a.transform.right+a.transform.forward*.1f+Vector3.up*.08f)*bounds.size.magnitude*1.3f;camera.transform.LookAt(bounds.center);UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>()?.Apply(camera);string name=a.kind.Replace(" ","_");ReefCorrectionAudit.Capture(camera,Output+name+".png");var rightPosition=camera.transform.position;var rightRotation=camera.transform.rotation;camera.transform.position=bounds.center+(-a.transform.right+a.transform.forward*.1f+Vector3.up*.08f)*bounds.size.magnitude*1.3f;camera.transform.LookAt(bounds.center);ReefCorrectionAudit.Capture(camera,Output+name+"_Left.png");camera.transform.SetPositionAndRotation(rightPosition,rightRotation);
   if(a.kind=="Moray"){var m=a.GetComponent<CurledMorayEncounter>();m.RequestTouch(m.touchVolumes[0].bounds.center);for(int i=0;i<8;i++)m.Step(.05f);DolphinMotionIntegration.CapturePosed(camera,m.skin,Output+"MorayTouch.png");}
   if(a.kind=="Lumen"){var r=a.GetComponent<Renderer>();var block=new MaterialPropertyBlock();block.SetFloat("_TailAmplitude",.055f);block.SetFloat("_TailPhase",Mathf.PI*.5f);r.SetPropertyBlock(block);ReefCorrectionAudit.Capture(camera,Output+"LumenStrokeA.png");block.SetFloat("_TailPhase",Mathf.PI*1.5f);r.SetPropertyBlock(block);ReefCorrectionAudit.Capture(camera,Output+"LumenStrokeB.png");}
  }
  var imageA=new Texture2D(2,2);var imageB=new Texture2D(2,2);imageA.LoadImage(File.ReadAllBytes(Output+"LumenStrokeA.png"));imageB.LoadImage(File.ReadAllBytes(Output+"LumenStrokeB.png"));var pixelsA=imageA.GetPixels32();var pixelsB=imageB.GetPixels32();int changed=0;for(int i=0;i<pixelsA.Length;i++){var p=pixelsA[i];var q=pixelsB[i];if(Mathf.Abs(p.r-q.r)+Mathf.Abs(p.g-q.g)+Mathf.Abs(p.b-q.b)>12)changed++;}UnityEngine.Object.DestroyImmediate(imageA);UnityEngine.Object.DestroyImmediate(imageB);if(changed<500)throw new Exception("Lumen GPU body stroke is not visibly changing pixels: "+changed);File.AppendAllText(Output+"Validation.txt","Lumen rendered stroke comparison: "+changed+" pixels changed; bilateral fish eye views captured.\n");Debug.Log("MOTION_FOLLOWUP_CAPTURED visibleStrokePixels="+changed);
 }
 public static void Run(){Apply();Validate();ReefCollisionValidation.Run();Capture();}
}
}
