using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class RequestedCreatureCorrections {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 const string Folder="Assets/UnderwaterArt/CreatureCorrections";
 static readonly System.Collections.Generic.HashSet<ulong> touched=new();
 static void Touch(GameObject go){touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(go).targetObjectId);foreach(var c in go.GetComponents<Component>())if(c)touched.Add(GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId);}
 static T Save<T>(T value,string path) where T:UnityEngine.Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);return old;}AssetDatabase.CreateAsset(value,path);return value;}
 static Bounds LocalBounds(Transform owner){bool first=true;Bounds bounds=default;foreach(var r in owner.GetComponentsInChildren<Renderer>()){
  if(!(r is MeshRenderer||r is SkinnedMeshRenderer)||!r.enabled)continue;var b=r.bounds;
  for(int i=0;i<8;i++){var p=owner.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
 }return bounds;}
 public static void Run(){
  Directory.CreateDirectory(Folder);var scene=EditorSceneManager.OpenScene(Scene);var ship=GameObject.Find("Supplied ship on trench floor");string shipPose=EditorJsonUtility.ToJson(ship.transform);
  foreach(var key in new[]{"FlameAngel","ReefFish","Clownfish","Seabass"}){
   string folder=(key=="FlameAngel"||key=="ReefFish")?"Assets/UnderwaterArt/SuppliedReview/":"Assets/UnderwaterArt/HabitatExpansion/";
   var full=AssetDatabase.LoadAssetAtPath<Mesh>(folder+key+"/SwimMesh.asset");
   if(!full.name.Contains("HeadForward")){var q=Quaternion.Euler(0,180,0);var v=full.vertices;var n=full.normals;for(int i=0;i<v.Length;i++){v[i]=q*v[i];n[i]=q*n[i];}full.vertices=v;full.normals=n;full.RecalculateBounds();full.RecalculateTangents();full.name=key+" HeadForward";EditorUtility.SetDirty(full);}
   Save(FishDistanceMesh.Create(full,24),folder+key+"/SchoolMedium.asset");Save(FishDistanceMesh.Create(full,12),folder+key+"/SchoolDistant.asset");
  }
  int grabs=0,soft=0,jellies=0;
  var roots=new[]{GameObject.Find("Selected supplied reef assets"),GameObject.Find("Habitat fish and deep ship expansion")};
  foreach(var root in roots)foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){
   if(!filter.sharedMesh||!filter.sharedMesh.name.Contains("HeadForward"))continue;var go=filter.gameObject;Touch(go);var life=go.GetComponent<SeaLife>();if(!life)life=go.AddComponent<SeaLife>();life.kind=filter.sharedMesh.name.StartsWith("FlameAngel")?"Flame angelfish":filter.sharedMesh.name.StartsWith("ReefFish")?"Butterflyfish":filter.sharedMesh.name.StartsWith("Clownfish")?"Clownfish":"Seabass";life.grabbable=true;life.danger=false;life.observationOnly=false;
   var touch=go.GetComponent<FishTouchBody>();if(!touch)touch=go.AddComponent<FishTouchBody>();var shape=touch.touchCollider;if(!shape){var volume=new GameObject("Fish touch volume");volume.layer=2;volume.transform.SetParent(go.transform,false);shape=volume.AddComponent<CapsuleCollider>();}
   var b=filter.sharedMesh.bounds;touch.bodyBounds=b;touch.bellySupportLocal=-b.min.y;shape.direction=2;shape.center=b.center;shape.radius=Mathf.Max(.02f,Mathf.Max(b.extents.x,b.extents.y)*.8f);shape.height=Mathf.Max(b.size.z*.85f,shape.radius*2);shape.isTrigger=true;shape.enabled=true;touch.touchCollider=shape;grabs++;
  }
  // Small fish yield at contact. Large-animal layer-9 solids remain physical.
  foreach(var body in UnityEngine.Object.FindObjectsByType<ReefSphereBody>(FindObjectsSortMode.None)){
   var life=body.GetComponent<SeaLife>();bool small=body.GetComponent<FishAgent>()||life&&life.grabbable;
   if(!small||LocalBounds(body.transform).size.magnitude*Mathf.Max(body.transform.lossyScale.x,body.transform.lossyScale.y,body.transform.lossyScale.z)>.8f)continue;
   foreach(var collider in body.GetComponentsInChildren<Collider>())if(!collider.isTrigger){Touch(collider.gameObject);collider.isTrigger=true;collider.gameObject.layer=2;EditorUtility.SetDirty(collider);soft++;}
  }
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None)){
   if(life.kind=="Lumen"){
    Touch(life.gameObject);var renderer=life.GetComponent<Renderer>();var mat=new Material(renderer.sharedMaterial){name="Natural companion skin",shader=Shader.Find("Underwater/Instanced Marine"),enableInstancing=true};mat.color=Color.white;mat.SetFloat("_TailAmplitude",.045f);renderer.sharedMaterial=Save(mat,Folder+"/NaturalCompanion.mat");foreach(var light in life.GetComponentsInChildren<Light>()){Touch(light.gameObject);light.enabled=false;}
   }
   if(life.kind!="Moon jelly"&&life.kind!="Comb jelly"&&life.kind!="Box jelly")continue;
   var existing=life.GetComponentsInChildren<Collider>().FirstOrDefault(c=>!c.isTrigger&&c.enabled&&c.gameObject.layer==9);if(existing){jellies++;continue;}
   Touch(life.gameObject);var b=LocalBounds(life.transform);var go=new GameObject("Jelly bell collision");go.transform.SetParent(life.transform,false);go.layer=9;var shape=go.AddComponent<SphereCollider>();shape.center=new Vector3(b.center.x,b.max.y-Mathf.Min(b.size.y*.22f,Mathf.Max(b.extents.x,b.extents.z)*.6f),b.center.z);shape.radius=Mathf.Max(.03f,Mathf.Max(b.extents.x,b.extents.z)*.82f);jellies++;
  }
  var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();Touch(diver.gameObject);var contacts=diver.GetComponent<ReefSmallFishContact>();if(!contacts)contacts=diver.gameObject.AddComponent<ReefSmallFishContact>();contacts.smallFishSurfaces=UnityEngine.Object.FindObjectsByType<FishTouchBody>(FindObjectsSortMode.None).Where(f=>f.bodyBounds.size.magnitude*Mathf.Max(f.transform.lossyScale.x,f.transform.lossyScale.y,f.transform.lossyScale.z)<.8f).SelectMany(f=>f.GetComponentsInChildren<Collider>().Concat(f.touchCollider?new[]{(Collider)f.touchCollider}:Array.Empty<Collider>())).Distinct().ToArray();
  RefineShark();
  if(EditorJsonUtility.ToJson(ship.transform)!=shipPose)throw new Exception("Ship pose changed");
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Renders/CreatureCorrections/TouchedIds.txt",string.Join("\n",touched));File.WriteAllText("Renders/CreatureCorrections/ShipPose.json",shipPose);Debug.Log($"REQUESTED_CORRECTIONS_APPLIED grabFish={grabs} softColliders={soft} jellyBellColliders={jellies} shipPose=preserved");Validate();
 }
 static void RefineShark(){
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();var player=shark.GetComponentInChildren<Animation>();Touch(player.gameObject);var clip=UnityEngine.Object.Instantiate(player.clip);clip.name="Shark progressive whole body swim";clip.legacy=true;clip.wrapMode=WrapMode.Loop;
  var names=new[]{"Body","TailBase","TailMid","TailTip","Pectoral_-1","Pectoral_1"};float[] amplitudes={2.5f,6,10,14,2,2};float duration=clip.length;
  foreach(var name in names){int index=Array.IndexOf(names,name);var bone=player.GetComponentsInChildren<Transform>().First(t=>t.name==name);player.clip.SampleAnimation(player.gameObject,0);var rest=bone.localRotation;var axis=bone.parent.InverseTransformDirection(index<4?shark.transform.up:shark.transform.forward).normalized;var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
   for(int sample=0;sample<=120;sample++){float t=duration*sample/120;float angle=amplitudes[index]*Mathf.Sin(t/duration*Mathf.PI*2-index*.65f);var q=Quaternion.AngleAxis(angle,axis)*rest;float[] values={q.x,q.y,q.z,q.w};for(int k=0;k<4;k++)curves[k].AddKey(t,values[k]);}
   string path=AnimationUtility.CalculateTransformPath(bone,player.transform);for(int k=0;k<4;k++){var curve=curves[k];curve.preWrapMode=curve.postWrapMode=WrapMode.Loop;for(int i=0;i<curve.length;i++)AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[k]),curve);}
  }
  clip.EnsureQuaternionContinuity();clip=Save(clip,Folder+"/SharkWholeBody.anim");var so=new SerializedObject(player);var clips=so.FindProperty("m_Animations");clips.arraySize=1;clips.GetArrayElementAtIndex(0).objectReferenceValue=clip;so.FindProperty("m_Animation").objectReferenceValue=clip;so.ApplyModifiedPropertiesWithoutUndo();player.wrapMode=WrapMode.Loop;
  Bounds bodyBounds=default;bool first=true;foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>()){
   Touch(skin.gameObject);Bounds envelope=default;bool empty=true;for(int sample=0;sample<=48;sample++){clip.SampleAnimation(player.gameObject,clip.length*sample/48);foreach(var world in OctopusRepairValidation.Points(skin)){var local=(skin.rootBone?skin.rootBone:skin.transform).InverseTransformPoint(world);if(empty){envelope=new Bounds(local,Vector3.zero);empty=false;}else envelope.Encapsulate(local);var actor=shark.transform.InverseTransformPoint(world);if(first){bodyBounds=new Bounds(actor,Vector3.zero);first=false;}else bodyBounds.Encapsulate(actor);}}envelope.Expand(.08f);skin.localBounds=envelope;
  }
  clip.SampleAnimation(player.gameObject,0);var clearance=shark.GetComponent<ReefAnimalClearance>();if(clearance){Touch(shark.gameObject);bodyBounds.Expand(.08f);clearance.animatedBody=bodyBounds;}
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);int count=0;foreach(var root in new[]{GameObject.Find("Selected supplied reef assets"),GameObject.Find("Habitat fish and deep ship expansion")})foreach(var life in root.GetComponentsInChildren<SeaLife>())if(life.GetComponent<MeshFilter>()&&life.GetComponent<MeshFilter>().sharedMesh.name.Contains("HeadForward")){
   var touch=life.GetComponent<FishTouchBody>();if(!life.grabbable||!touch||!touch.touchCollider||!touch.touchCollider.isTrigger)throw new Exception("Missing fish grab volume");var clone=UnityEngine.Object.Instantiate(life.gameObject,new Vector3(800,2,0),Quaternion.identity);var copy=clone.GetComponent<SeaLife>();var hand=new GameObject("Grab validation hand").transform;hand.position=clone.GetComponent<FishTouchBody>().PalmSupport(Vector3.up);copy.Grab(hand);if(!copy.IsHeld)throw new Exception("Fish grab failed");copy.StepHeld(.01f,Time.time+2.1f);if(copy.IsHeld)throw new Exception("Automatic release failed");UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(hand.gameObject);count++;
  }
  if(count!=34)throw new Exception("Expected 34 supplied grab fish, got "+count);
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.kind=="Moon jelly"||life.kind=="Comb jelly"||life.kind=="Box jelly")if(!life.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger&&c.gameObject.layer==9))throw new Exception("Missing jelly bell collision");
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();var anim=shark.GetComponentInChildren<Animation>();foreach(string name in new[]{"Body","TailBase","TailMid","TailTip"}){var bone=anim.GetComponentsInChildren<Transform>().First(t=>t.name==name);anim.clip.SampleAnimation(anim.gameObject,0);var initial=bone.localRotation;float largest=0;for(int i=1;i<12;i++){anim.clip.SampleAnimation(anim.gameObject,anim.clip.length*i/12);largest=Mathf.Max(largest,Quaternion.Angle(initial,bone.localRotation));}if(largest<2)throw new Exception("Shark bone motion missing: "+name);}
  RealisticReefBuilder.ValidateScene();Debug.Log("REQUESTED_CORRECTIONS_VALIDATED grabRelease=34 allJelliesCollide=true progressiveSharkBody=true");
 }
 public static void VisualChecks(){
  Audit();EditorSceneManager.OpenScene(Scene);var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();var player=shark.GetComponentInChildren<Animation>();var skin=player.GetComponentInChildren<SkinnedMeshRenderer>();var camera=Camera.main;var optics=camera.GetComponent<ReefOptics>();if(optics)optics.enabled=false;RenderSettings.fog=false;Shader.SetGlobalFloat("_ReefLight",1);camera.fieldOfView=42;camera.nearClipPlane=.01f;camera.transform.position=shark.transform.position+shark.transform.right*7+Vector3.up*1.2f;camera.transform.LookAt(shark.transform.position);
  for(int i=0;i<4;i++){player.clip.SampleAnimation(player.gameObject,player.clip.length*i/4);var baked=UnityEngine.Object.Instantiate(skin.sharedMesh);baked.ClearBlendShapes();baked.vertices=OctopusRepairValidation.Points(skin);baked.RecalculateBounds();baked.RecalculateNormals();var proxy=new GameObject("Shark sampled pose");proxy.AddComponent<MeshFilter>().sharedMesh=baked;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;ReefCorrectionAudit.Capture(camera,"Renders/CreatureCorrections/Shark_"+i+".png");skin.enabled=true;UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(baked);}
  EditorSceneManager.OpenScene(Scene);var life=UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).First(l=>l.kind=="Lumen");var encounter=life.GetComponent<DeepWaterEncounter>();for(int i=0;i<100;i++)encounter.Step(.05f,4,life.transform.position+Vector3.forward*2,3);var block=new MaterialPropertyBlock();life.GetComponent<Renderer>().GetPropertyBlock(block);if(block.GetColor("_BioEmission").maxColorComponent>.00001f)throw new Exception("Companion body still emissive");
  var rig=new GameObject("Contact test rig");rig.transform.position=new Vector3(900,2,0);rig.AddComponent<CharacterController>();var head=new GameObject("Head");head.transform.SetParent(rig.transform,false);head.transform.localPosition=Vector3.up*1.6f;var motor=rig.AddComponent<DiverCollisionMotor>();motor.Initialize();var little=GameObject.CreatePrimitive(PrimitiveType.Sphere);little.layer=2;little.transform.position=rig.transform.position+Vector3.up;little.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();var start=rig.transform.position;motor.Move(Vector3.zero);if((rig.transform.position-start).sqrMagnitude>.00001f)throw new Exception("Small fish displaced diver");UnityEngine.Object.DestroyImmediate(little);var big=GameObject.CreatePrimitive(PrimitiveType.Cube);big.layer=9;big.transform.position=rig.transform.position+new Vector3(2,1,0);big.transform.localScale=new Vector3(1,3,3);Physics.SyncTransforms();motor.Move(Vector3.right*4);if(rig.transform.position.x>901.3f)throw new Exception("Large animal no longer blocks diver");UnityEngine.Object.DestroyImmediate(big);UnityEngine.Object.DestroyImmediate(rig);Debug.Log("REQUESTED_VISUAL_AND_CONTACT_CHECKS_PASSED companionEmission=0 smallFishDisplacement=0 largeAnimalCollision=retained");
 }
 public static void Audit(){
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory("Renders/CreatureCorrections");var report=new System.Text.StringBuilder();
  var ship=GameObject.Find("Supplied ship on trench floor");report.AppendLine("SHIP "+ship.transform.position+" rotation="+ship.transform.eulerAngles+" scale="+ship.transform.localScale);
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Where(l=>l.kind=="Lumen"||l.kind.Contains("jelly")||l.kind=="Reef shark")){
   report.AppendLine(life.name+" kind="+life.kind+" colliders="+life.GetComponentsInChildren<Collider>().Length);
   if(life.kind=="Lumen")foreach(var r in life.GetComponentsInChildren<Renderer>()){report.AppendLine(" LUMEN "+r.name+" shader="+r.sharedMaterial.shader.name);if(r.sharedMaterial.HasProperty("_BioEmission"))report.AppendLine(" emission="+r.sharedMaterial.GetColor("_BioEmission"));}
  }
  var shark=UnityEngine.Object.FindFirstObjectByType<OpenWaterShark>();foreach(var anim in shark.GetComponentsInChildren<Animation>())foreach(var binding in AnimationUtility.GetCurveBindings(anim.clip))report.AppendLine("SHARK_CURVE "+binding.path+" "+binding.propertyName);
  foreach(var bone in shark.GetComponentsInChildren<Transform>())report.AppendLine("SHARK_BONE "+bone.name+" local="+bone.localPosition);
  foreach(var key in new[]{"FlameAngel","ReefFish","Clownfish","Seabass"}){
   string folder=(key=="FlameAngel"||key=="ReefFish")?"Assets/UnderwaterArt/SuppliedReview/":"Assets/UnderwaterArt/HabitatExpansion/";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+key+"/SwimMesh.asset");var go=new GameObject(key);go.transform.position=new Vector3(800,0,0);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+key+"/Surface.mat");
   var mark=GameObject.CreatePrimitive(PrimitiveType.Sphere);mark.transform.position=go.transform.position+Vector3.forward*.65f;mark.transform.localScale=Vector3.one*.06f;mark.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Unlit/Color")){color=Color.red};
   var cam=new GameObject("Audit camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.03f,.08f,.12f);cam.transform.position=go.transform.position+new Vector3(2,.25f,0);cam.transform.LookAt(go.transform.position);cam.orthographic=true;cam.orthographicSize=.8f;cam.nearClipPlane=.01f;cam.farClipPlane=10;RenderSettings.fog=false;RenderSettings.ambientLight=Color.gray;Shader.SetGlobalFloat("_ReefLight",1);ReefCorrectionAudit.Capture(cam,"Renders/CreatureCorrections/"+key+"_ForwardRed.png");UnityEngine.Object.DestroyImmediate(cam.gameObject);UnityEngine.Object.DestroyImmediate(mark);UnityEngine.Object.DestroyImmediate(go);
  }
  File.WriteAllText("Renders/CreatureCorrections/Audit.txt",report.ToString());Debug.Log("REQUESTED_CREATURE_AUDIT_COMPLETE");
 }
}
}
