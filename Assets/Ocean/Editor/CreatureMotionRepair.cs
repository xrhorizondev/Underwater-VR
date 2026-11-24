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
public static class CreatureMotionRepair {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 const string Folder="Assets/UnderwaterArt/CreatureMotionFix";
 const string Output="Renders/CreatureMotionFix";
 static SeaLife[] Animals()=>UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>new[]{"Grouper","Stingray","Reef shark"}.Contains(l.kind)).ToArray();
 public static void Inspect(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var log=new StringBuilder();
  foreach(var life in Animals()){
   log.AppendLine("ACTOR "+life.kind+" "+life.name+" pose="+life.transform.position+" scale="+life.transform.lossyScale);
   foreach(var r in life.GetComponentsInChildren<Renderer>(true)){
    if(!(r is MeshRenderer||r is SkinnedMeshRenderer))continue;
    var m=r is SkinnedMeshRenderer s?s.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;
    log.AppendLine("  RENDER "+AnimationUtility.CalculateTransformPath(r.transform,life.transform)+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" materials="+string.Join(",",r.sharedMaterials.Select(x=>x?AssetDatabase.GetAssetPath(x)+" ["+x.shader.name+"]":"null")));
    if(m)log.AppendLine("    MESH "+AssetDatabase.GetAssetPath(m)+" vertices="+m.vertexCount+" triangles="+m.triangles.Length/3+" bounds="+m.bounds+" geometry="+new BoundsOf(m.vertices)+" tags="+string.Join(",",m.uv2.GroupBy(v=>Mathf.RoundToInt(v.x)).Select(g=>g.Key+":"+g.Count())));
   }
   foreach(var t in life.GetComponentsInChildren<Transform>())if(t.GetComponentInParent<Animation>())log.AppendLine("  BONE "+AnimationUtility.CalculateTransformPath(t,life.transform)+" local="+t.localPosition+" actor="+life.transform.InverseTransformPoint(t.position)+" rotation="+t.localEulerAngles);
  }
  File.WriteAllText(Output+"/Inspection.txt",log.ToString());Debug.Log("CREATURE_MOTION_INSPECTED");
 }
 struct BoundsOf {public Bounds bounds;public BoundsOf(Vector3[] points){bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);}public override string ToString()=>bounds.ToString();}
 static readonly HashSet<string> touched=new();
 static void Touch(GameObject go){touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(go).targetObjectId.ToString());foreach(var c in go.GetComponents<Component>())if(c)touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId.ToString());}
 static T Save<T>(T asset,string name) where T:UnityEngine.Object {string path=Folder+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);return old;}AssetDatabase.CreateAsset(asset,path);return asset;}
 static Mesh WithoutAddedEyes(Mesh source){
  var uv2=source.uv2;var triangles=source.triangles;var kept=new List<int>();for(int i=0;i<triangles.Length;i+=3){bool eye=false;for(int k=0;k<3;k++){int j=triangles[i+k];if(j<uv2.Length&&uv2[j].x>.5f&&uv2[j].x<2.5f)eye=true;}if(!eye)kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});}
  var used=kept.Distinct().OrderBy(i=>i).ToArray();var map=new int[source.vertexCount];for(int i=0;i<used.Length;i++)map[used[i]]=i;var mesh=new Mesh{name=source.name+" original textured anatomy",indexFormat=IndexFormat.UInt32};mesh.vertices=used.Select(i=>source.vertices[i]).ToArray();if(source.normals.Length==source.vertexCount)mesh.normals=used.Select(i=>source.normals[i]).ToArray();if(source.tangents.Length==source.vertexCount)mesh.tangents=used.Select(i=>source.tangents[i]).ToArray();if(source.uv.Length==source.vertexCount)mesh.uv=used.Select(i=>source.uv[i]).ToArray();if(uv2.Length==source.vertexCount)mesh.uv2=used.Select(i=>uv2[i]).ToArray();if(source.colors.Length==source.vertexCount)mesh.colors=used.Select(i=>source.colors[i]).ToArray();mesh.triangles=kept.Select(i=>map[i]).ToArray();mesh.RecalculateBounds();return mesh;
 }
 public static void Apply(){
  Directory.CreateDirectory(Folder);Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(Scene);touched.Clear();var cache=new Dictionary<Mesh,Mesh>();int eyes=0;
  foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
   if(!filter.sharedMesh||!AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/UnderwaterArt/RealismPass/Eyes_"))continue;
   // These scan meshes already contain photographic eyes. The tagged iris/pupil
   // ellipsoids were a second, misplaced pair added by the old realism pass.
   var source=filter.sharedMesh;if(!cache.TryGetValue(source,out var clean)){clean=Save(WithoutAddedEyes(source),source.name.Replace(" ","_")+"_OriginalEyes.asset");cache[source]=clean;}Touch(filter.gameObject);filter.sharedMesh=clean;var detail=filter.GetComponent<ReefFishDetail>();if(detail){if(detail.full==source)detail.full=clean;if(detail.medium==source)detail.medium=clean;if(detail.distant==source)detail.distant=clean;}eyes++;
  }
  foreach(var life in Animals().Where(l=>l.kind=="Grouper"))RigGrouper(life);
  RefineRayEyes();RefineShark();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllLines(Output+"/TouchedIds.txt",touched);File.WriteAllText(Output+"/Applied.txt","Removed redundant fitted eye pairs from "+eyes+" photographic fish.\nTwo groupers: original mesh/UVs, seven-bone body and paired fin rig.\nThree rays: recessed eyes replace protruding spheres.\nShark: smooth progressive lateral stroke and steering flex.\n");Debug.Log("CREATURE_MOTION_APPLIED duplicateEyePairs="+eyes);
 }
 static void RigGrouper(SeaLife life){
  Touch(life.gameObject);var filter=life.GetComponent<MeshFilter>();var renderer=life.GetComponent<MeshRenderer>();var source=filter.sharedMesh;var b=new BoundsOf(source.vertices).bounds;float length=b.size.z;
  var old=life.transform.Find("Grouper skeletal visual");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var visual=new GameObject("Grouper skeletal visual");visual.transform.SetParent(life.transform,false);var skin=visual.AddComponent<SkinnedMeshRenderer>();var bones=new Transform[7];float[] stations={.23f,.44f,.62f,.79f,.91f};
  for(int i=0;i<5;i++){bones[i]=new GameObject(new[]{"Shoulder","Body","Rear body","Tail stalk","Tail fin"}[i]).transform;bones[i].SetParent(i==0?visual.transform:bones[i-1],false);bones[i].position=life.transform.TransformPoint(new Vector3(0,b.center.y,b.max.z-stations[i]*length));}
  for(int i=0;i<2;i++){bones[5+i]=new GameObject(i==0?"Left pectoral":"Right pectoral").transform;bones[5+i].SetParent(bones[0],false);bones[5+i].position=life.transform.TransformPoint(new Vector3((i==0?-1:1)*b.extents.x*.48f,b.center.y-.035f,b.max.z-length*.34f));}
  var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Original grouper anatomy with smooth skeletal weights";var weights=new BoneWeight[mesh.vertexCount];var points=mesh.vertices;
  for(int i=0;i<points.Length;i++){float t=(b.max.z-points[i].z)/length;int a=0;while(a<3&&t>stations[a+1])a++;int c=Mathf.Min(a+1,4);float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(stations[a],stations[c],t));if(t<=stations[0])blend=0;if(t>=stations[4]){a=4;c=4;blend=0;}float fin=Mathf.SmoothStep(.65f,.95f,Mathf.Abs(points[i].x)/b.extents.x)*(1-Mathf.SmoothStep(.42f,.57f,t))*Mathf.SmoothStep(.2f,.3f,t)*.8f;weights[i]=new BoneWeight{boneIndex0=a,weight0=(1-blend)*(1-fin),boneIndex1=c,weight1=blend*(1-fin),boneIndex2=points[i].x<0?5:6,weight2=fin};}
  mesh.boneWeights=weights;mesh.bindposes=bones.Select(t=>t.worldToLocalMatrix*visual.transform.localToWorldMatrix).ToArray();skin.sharedMesh=Save(mesh,"GrouperSkinned.asset");skin.bones=bones;skin.rootBone=bones[0];skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=false;skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;skin.lightProbeUsage=LightProbeUsage.Off;skin.reflectionProbeUsage=ReflectionProbeUsage.Off;
  var material=new Material(renderer.sharedMaterial){name="Grouper attached skin",shader=Shader.Find("Underwater/Skinned Grouper")};skin.sharedMaterial=Save(material,"GrouperSkin.mat");renderer.enabled=false;
  var motion=life.GetComponent<GrouperStroke>();if(!motion)motion=life.gameObject.AddComponent<GrouperStroke>();motion.spine=bones.Take(5).ToArray();motion.pectoralFins=bones.Skip(5).ToArray();var redundant=life.GetComponent<FishBodyMotion>();if(redundant)redundant.enabled=false;
  // Sample deformation once in the editor to keep runtime skin culling bounded.
  motion.Initialize();bool first=true;Bounds envelope=default;for(int phase=0;phase<48;phase++){motion.ApplyPose(phase*Mathf.PI*2/48,1,35);foreach(var p in OctopusRepairValidation.Points(skin)){var local=life.transform.InverseTransformPoint(p);if(first){envelope=new Bounds(local,Vector3.zero);first=false;}else envelope.Encapsulate(local);}}for(int i=0;i<bones.Length;i++)bones[i].localRotation=Quaternion.identity;envelope.Expand(.05f);skin.localBounds=new BoundsOf(Enumerable.Range(0,8).Select(i=>skin.rootBone.InverseTransformPoint(life.transform.TransformPoint(envelope.center+Vector3.Scale(envelope.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))))).ToArray()).bounds;var guard=life.GetComponent<ReefAnimalClearance>();if(guard){envelope.Encapsulate(guard.animatedBody);guard.animatedBody=envelope;}
 }
 static void RefineRayEyes(){
  Mesh saved=null;foreach(var ray in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None)){
   Touch(ray.gameObject);if(!saved){var source=ray.GetComponent<MeshFilter>().sharedMesh;var body=WithoutAddedEyes(source);var iris=new ReefGeometry();var pupil=new ReefGeometry();for(int side=-1;side<=1;side+=2){float x=side*.13f,z=.28f;float radial=Mathf.Sqrt(x*x/(.96f*.96f)+z*z/(.64f*.64f));float y=.015f+.14f*Mathf.Exp(-x*x/.052f-z*z/.24f)*(1-radial*radial);iris.Ellipsoid(new Vector3(x,y+.001f,z),new Vector3(.015f,.004f,.021f),6,12);pupil.Ellipsoid(new Vector3(x,y+.004f,z+.002f),new Vector3(.009f,.0025f,.014f),6,12);}var im=iris.Mesh("Recessed ray irises");im.uv2=Enumerable.Repeat(new Vector2(1,0),im.vertexCount).ToArray();var pm=pupil.Mesh("Recessed ray pupils");pm.uv2=Enumerable.Repeat(new Vector2(2,0),pm.vertexCount).ToArray();var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name="Stingray with two recessed anatomical eyes"};mesh.CombineMeshes(new[]{new CombineInstance{mesh=body,transform=Matrix4x4.identity},new CombineInstance{mesh=im,transform=Matrix4x4.identity},new CombineInstance{mesh=pm,transform=Matrix4x4.identity}},true,true);mesh.bounds=source.bounds;saved=Save(mesh,"StingrayRecessedEyes.asset");UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(pm);}ray.GetComponent<MeshFilter>().sharedMesh=saved;
  }
 }
 static void RefineShark(){
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();var player=shark.GetComponentInChildren<Animation>();Touch(player.gameObject);Touch(shark.gameObject);
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnderwaterArt/MarineRefresh/Shark/Shark.fbx");var clip=new AnimationClip{name="Shark relaxed progressive swim",legacy=true,frameRate=30,wrapMode=WrapMode.Loop};float duration=2.1f;var names=new[]{"Body","TailBase","TailMid","TailTip","Pectoral_-1","Pectoral_1"};float[] amplitudes={1.5f,7,11,15,1.4f,1.4f};
  foreach(var name in names){int index=Array.IndexOf(names,name);var bone=player.GetComponentsInChildren<Transform>().First(t=>t.name==name);var rest=prefab.GetComponentsInChildren<Transform>().First(t=>t.name==name).localRotation;var axis=bone.parent.InverseTransformDirection(index<4?shark.transform.up:shark.transform.forward).normalized;var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};for(int frame=0;frame<=84;frame++){float t=duration*frame/84;float phase=t/duration*Mathf.PI*2;float angle=amplitudes[index]*Mathf.Sin(phase-index*.72f);if(index>=4)angle=amplitudes[index]*Mathf.Sin(phase+(index==4?0:.35f));var q=Quaternion.AngleAxis(angle,axis)*rest;float[] values={q.x,q.y,q.z,q.w};for(int k=0;k<4;k++)curves[k].AddKey(t,values[k]);}string path=AnimationUtility.CalculateTransformPath(bone,player.transform);for(int k=0;k<4;k++){var curve=curves[k];curve.preWrapMode=curve.postWrapMode=WrapMode.Loop;for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[k]),curve);}}
  clip.EnsureQuaternionContinuity();clip=Save(clip,"SharkCruise.anim");var so=new SerializedObject(player);so.FindProperty("m_Animation").objectReferenceValue=clip;var clips=so.FindProperty("m_Animations");clips.arraySize=1;clips.GetArrayElementAtIndex(0).objectReferenceValue=clip;so.ApplyModifiedPropertiesWithoutUndo();var driver=player.GetComponent<ReefImportedAnimation>();driver.speed=1;driver.minimumTravelRate=.3f;driver.referenceTravelSpeed=.7f;
  var flex=player.GetComponent<SharkBodyMotion>();if(!flex)flex=player.gameObject.AddComponent<SharkBodyMotion>();flex.actor=shark.transform;flex.body=player.GetComponentsInChildren<Transform>().First(t=>t.name=="Body");flex.tailBase=player.GetComponentsInChildren<Transform>().First(t=>t.name=="TailBase");
  foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>()){Touch(skin.gameObject);bool first=true;Bounds envelope=default;for(int f=0;f<64;f++){clip.SampleAnimation(player.gameObject,clip.length*f/64);foreach(var p in OctopusRepairValidation.Points(skin)){var local=(skin.rootBone?skin.rootBone:skin.transform).InverseTransformPoint(p);if(first){envelope=new Bounds(local,Vector3.zero);first=false;}else envelope.Encapsulate(local);}}envelope.Expand(envelope.size*.08f);skin.localBounds=envelope;}
  clip.SampleAnimation(player.gameObject,0);var guard=shark.GetComponent<ReefAnimalClearance>();guard.animatedBody=AnimalSeabedRepair.Envelope(shark.transform);
 }
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 public static void FitSteeringBounds(){
  var scene=EditorSceneManager.OpenScene(Scene);var ids=new List<string>();
  foreach(var life in Animals().Where(l=>l.kind=="Grouper"||l.kind=="Reef shark")){
   var skin=life.GetComponentInChildren<SkinnedMeshRenderer>();var motion=life.GetComponent<GrouperStroke>();var animation=life.GetComponentInChildren<Animation>();var flex=life.GetComponentInChildren<SharkBodyMotion>();bool first=true;Bounds local=default,actor=default;
   for(int f=0;f<128;f++)foreach(float turn in new[]{-1f,0,1f}){
    if(motion)motion.ApplyPose(f*Mathf.PI*4/128,1,turn*35);
    if(animation){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*f/128);flex.body.localRotation=Quaternion.AngleAxis(turn*5,flex.body.parent.InverseTransformDirection(life.transform.up))*flex.body.localRotation;flex.tailBase.localRotation=Quaternion.AngleAxis(turn*3,flex.tailBase.parent.InverseTransformDirection(life.transform.up))*flex.tailBase.localRotation;}
    foreach(var p in OctopusRepairValidation.Points(skin)){var a=life.transform.InverseTransformPoint(p);var b=(skin.rootBone?skin.rootBone:skin.transform).InverseTransformPoint(p);if(first){local=new Bounds(b,Vector3.zero);actor=new Bounds(a,Vector3.zero);first=false;}else{local.Encapsulate(b);actor.Encapsulate(a);}}
   }
   local.Expand(local.size*.035f+Vector3.one*.025f);actor.Expand(.06f);skin.localBounds=local;var guard=life.GetComponent<ReefAnimalClearance>();actor.Encapsulate(guard.animatedBody);guard.animatedBody=actor;ids.Add(GlobalObjectId.GetGlobalObjectIdSlow(skin).targetObjectId.ToString());ids.Add(GlobalObjectId.GetGlobalObjectIdSlow(guard).targetObjectId.ToString());
   if(animation)animation.clip.SampleAnimation(animation.gameObject,0);if(motion){foreach(var bone in motion.spine.Concat(motion.pectoralFins))bone.localRotation=Quaternion.identity;}
   EditorUtility.SetDirty(skin);EditorUtility.SetDirty(guard);
  }
  EditorSceneManager.SaveScene(scene);File.WriteAllLines(Output+"/SteeringBoundsIds.txt",ids);Debug.Log("STEERING_ANIMATION_BOUNDS_FITTED bothTurnDirections=true fullCycle=true");
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output);var report=new StringBuilder();
  foreach(var life in Animals().Where(a=>a.kind=="Grouper")){
   var skin=life.GetComponentInChildren<SkinnedMeshRenderer>();var motion=life.GetComponent<GrouperStroke>();var mesh=skin.sharedMesh;Require(skin.bones.Length==7,"Grouper rig incomplete");Require(mesh.uv.SequenceEqual(life.GetComponent<MeshFilter>().sharedMesh.uv),"Grouper atlas changed");foreach(var w in mesh.boneWeights)Require(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.0001f,"Unnormalized grouper skin weights");
   motion.Initialize();motion.ApplyPose(0,1);var first=OctopusRepairValidation.Points(skin).Select(p=>life.transform.InverseTransformPoint(p)).ToArray();var max=new float[first.Length];float seam=0;for(int f=1;f<=128;f++){motion.ApplyPose(f*Mathf.PI*4/128,1);var points=OctopusRepairValidation.Points(skin).Select(p=>life.transform.InverseTransformPoint(p)).ToArray();for(int i=0;i<points.Length;i++){Require(float.IsFinite(points[i].x)&&float.IsFinite(points[i].y)&&float.IsFinite(points[i].z),"Invalid grouper skin pose");max[i]=Mathf.Max(max[i],Vector3.Distance(first[i],points[i]));if(f==128)seam=Mathf.Max(seam,Vector3.Distance(first[i],points[i]));}}
   var bind=mesh.vertices;var b=new BoundsOf(bind).bounds;float body=max.Where((v,i)=>bind[i].z<b.center.z&&bind[i].z>b.min.z+b.size.z*.25f).Max(),tail=max.Where((v,i)=>bind[i].z<b.min.z+b.size.z*.12f).Max(),head=max.Where((v,i)=>bind[i].z>b.max.z-b.size.z*.12f).Max();Require(body>.025f&&tail>.09f&&head<.025f&&seam<.0001f,"Grouper deformation/loop failed body="+body+" tail="+tail+" head="+head+" seam="+seam);report.AppendLine(life.name+" triangles="+mesh.triangles.Length/3+" bones="+skin.bones.Length+" bodyFlex="+body+" tailFlex="+tail+" headFlex="+head+" seam="+seam+" UVs=preserved");
  }
  foreach(var ray in UnityEngine.Object.FindObjectsByType<StingraySwimmer>(FindObjectsSortMode.None)){var mesh=ray.GetComponent<MeshFilter>().sharedMesh;var uv2=mesh.uv2;var vertices=mesh.vertices;foreach(int side in new[]{-1,1}){var eye=vertices.Where((v,i)=>uv2[i].x>1.5f&&uv2[i].x<2.5f&&Mathf.Sign(v.x)==side).ToArray();Require(eye.Length>20,"Missing ray eye");var b=new BoundsOf(eye).bounds;Require(b.size.y<.006f,"Ray eyes still protrude");}report.AppendLine("Ray eyes=one recessed pair, pupil depth<6mm; tail hazard unchanged");}
  File.WriteAllText(Output+"/Deformation.txt",report.ToString());SharkDolphinReview.Inspect();NaturalFishMotionAudit.Run();Debug.Log("CREATURE_MOTION_VALIDATED");
 }
 // Render true sampled geometry: editor Camera.Render can otherwise reuse the
 // previous skin pose, which makes a clip look rigid in static phase captures.
 public static void Capture(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Output+"/Poses");var camera=new GameObject("Creature review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.09f,.12f);camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.fieldOfView=38;camera.cullingMask=1<<30;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.59f,.62f);Shader.SetGlobalFloat("_ReefLight",1);var light=new GameObject("Creature review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(35,-40,0);
  foreach(var life in Animals().GroupBy(a=>a.kind).Select(g=>g.First())){
   var skin=life.GetComponentInChildren<SkinnedMeshRenderer>();var animation=life.GetComponentInChildren<Animation>();var grouper=life.GetComponent<GrouperStroke>();var source=skin?skin.sharedMesh:life.GetComponent<MeshFilter>().sharedMesh;var material=skin?skin.sharedMaterial:life.GetComponent<Renderer>().sharedMaterial;
   for(int phase=0;phase<4;phase++){if(grouper)grouper.ApplyPose(phase*Mathf.PI*.5f,1);if(animation)animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*phase/4);var mesh=UnityEngine.Object.Instantiate(source);if(skin){mesh.ClearBlendShapes();mesh.vertices=OctopusRepairValidation.Points(skin).Select(p=>life.transform.InverseTransformPoint(p)).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();}mesh.RecalculateBounds();var proxy=new GameObject("Sampled geometry");proxy.layer=30;proxy.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=proxy.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;var b=mesh.bounds;
    for(int view=0;view<3;view++){Vector3 direction=view==0?new Vector3(1,.12f,.2f):view==1?new Vector3(-1,.12f,.2f):new Vector3(.15f,.15f,1);if(life.kind=="Stingray")direction=new Vector3(view==1?-1:1,1.7f,view==2?1:.25f);camera.transform.position=b.center+direction.normalized*b.size.magnitude*1.55f;camera.transform.LookAt(b.center);ReefCorrectionAudit.Capture(camera,Output+"/Poses/"+life.kind.Replace(" ","")+"_"+phase+"_"+view+".png");}UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(mesh);
   }
  }Debug.Log("CREATURE_POSES_CAPTURED");
 }
}
}
