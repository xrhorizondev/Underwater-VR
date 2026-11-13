using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReefDayThree {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity",Folder="Assets/UnderwaterArt/DayThree";
 [MenuItem("Underwater/Install Day 3 encounters")]
 public static void Apply(){
  var scene=EditorSceneManager.OpenScene(Scene);System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
  var school=GameObject.Find("Clownfish coral territory").GetComponent<FishSchool>();school.reefEncounter=true;EditorUtility.SetDirty(school);
  foreach(var swim in UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None))if(swim.isRay){swim.encounterRoll=true;EditorUtility.SetDirty(swim);}
  foreach(var life in UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.kind=="Lionfish"){var e=life.GetComponent<ReefEncounter>();if(!e)e=life.gameObject.AddComponent<ReefEncounter>();e.response=ReefEncounter.Response.Lionfish;EditorUtility.SetDirty(e);}
  if(!GameObject.Find("Day 3 reef anemone")){
   var root=new GameObject("Day 3 reef anemone");root.transform.position=new Vector3(school.habitatCenter.x,ReefGeometry.Floor(school.habitatCenter.x,school.habitatCenter.z)+.16f,school.habitatCenter.z);
   var e=root.AddComponent<ReefEncounter>();e.response=ReefEncounter.Response.Anemone;var arms=new List<Transform>();var material=Material("Anemone",new Color(.52f,.38f,.24f));
   Shape("Anemone foot",root.transform,new Vector3(0,-.08f,0),new Vector3(.55f,.16f,.55f),material);
   for(int i=0;i<32;i++){float a=i*2.39996f,r=.1f+.3f*Mathf.Sqrt(i/32f);var pivot=new GameObject("Retracting tentacle").transform;pivot.SetParent(root.transform,false);pivot.localPosition=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);Shape("Soft tentacle",pivot,new Vector3(0,.18f,0),new Vector3(.045f,.36f,.045f),material);arms.Add(pivot);}e.tentacles=arms.ToArray();
  }
  var anemone=GameObject.Find("Day 3 reef anemone").transform;if(Physics.Raycast(school.habitatCenter+Vector3.up*2,Vector3.down,out var perch,8,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))anemone.position=perch.point+Vector3.up*.16f;
  var oldOctopus=GameObject.Find("Day 3 rocky octopus");if(oldOctopus)UnityEngine.Object.DestroyImmediate(oldOctopus);{
   var lion=Array.Find(UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None),l=>l.kind=="Lionfish");
   var root=new GameObject("Day 3 rocky octopus");root.transform.position=lion.transform.position+new Vector3(-2,-.4f,1);
   var body=root.AddComponent<ReefSphereBody>();body.radius=.7f;Physics.SyncTransforms();if(!body.PlaceNear(root.transform.position))throw new Exception("No clear octopus spawn");
   var life=root.AddComponent<SeaLife>();life.kind="Octopus";life.stationary=true;
   var e=root.AddComponent<ReefEncounter>();e.response=ReefEncounter.Response.Octopus;var material=Material("Octopus",new Color(.55f,.33f,.18f));material.shader=Shader.Find("Underwater/Octopus Skin");EditorUtility.SetDirty(material);var eye=Material("Octopus eyes",new Color(.02f,.025f,.015f));
   Shape("Mantle",root.transform,new Vector3(0,.15f,-.12f),new Vector3(.38f,.36f,.58f),material);
   Shape("Left eye",root.transform,new Vector3(-.11f,.17f,.11f),Vector3.one*.05f,eye);Shape("Right eye",root.transform,new Vector3(.11f,.17f,.11f),Vector3.one*.05f,eye);
   var arms=new List<Transform>();for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var pivot=new GameObject("Curling arm").transform;pivot.SetParent(root.transform,false);pivot.localRotation=Quaternion.Euler(0,i*45,0);var geometry=new ReefGeometry();var points=new Vector3[17];var widths=new float[17];for(int j=0;j<17;j++){float t=j/16f;points[j]=new Vector3(Mathf.Sin(t*4+i)*.06f*t,-.015f+Mathf.Sin(t*Mathf.PI)*.04f,.08f+t*.53f);widths[j]=Mathf.Lerp(.062f,.003f,t);}geometry.Tube(points,widths,10,Color.white);var mesh=geometry.Mesh("Octopus tapered arm "+i);string path=Folder+"/Arm"+i+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else {AssetDatabase.CreateAsset(mesh,path);saved=mesh;}var limb=new GameObject("Tapered arm");limb.transform.SetParent(pivot,false);limb.AddComponent<MeshFilter>().sharedMesh=saved;limb.AddComponent<MeshRenderer>().sharedMaterial=material;arms.Add(pivot);}e.tentacles=arms.ToArray();
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();Capture();
 }
 static Material Material(string name,Color color){string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard")){color=color,enableInstancing=true};AssetDatabase.CreateAsset(m,path);}return m;}
 static void Shape(string name,Transform parent,Vector3 p,Vector3 scale,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;}
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var school=GameObject.Find("Clownfish coral territory").GetComponent<FishSchool>();Require(school.reefEncounter,"Clownfish response missing");school.Initialize();school.Scatter(school.habitatCenter+Vector3.forward);
  for(int i=0;i<1800;i++)school.Simulate(1f/30);
  foreach(var fish in school.fish){Require(Vector3.Distance(fish.transform.position,school.habitatCenter)<school.habitatRadius+1,"Clownfish failed return");Require(fish.GetComponent<ReefSphereBody>().Clear(fish.transform.position),"Clownfish penetration");}
  foreach(var e in UnityEngine.Object.FindObjectsByType<ReefEncounter>(FindObjectsSortMode.None)){
   e.Initialize();var start=e.transform.position;Vector3 source=start+Vector3.right; e.Step(.05f,source,.1f);Require(e.Reaction>.9f,"Missing encounter reaction");if(e.response==ReefEncounter.Response.Octopus){Require(e.skin.Length>0,"Octopus skin cache missing after scene reload");var block=new MaterialPropertyBlock();e.skin[0].GetPropertyBlock(block);Require(block.GetColor("_Color").r>.5f,"Octopus color response missing");}
   for(int i=0;i<60;i++)e.Step(.05f,source,10);
   if(e.response!=ReefEncounter.Response.Anemone){Require(Vector3.Distance(start,e.transform.position)>.05f,"Missing retreat");Require(e.GetComponent<ReefSphereBody>().Clear(e.transform.position),"Encounter penetration");}
   for(int i=0;i<240;i++)e.Step(.05f,source,10);Require(e.Reaction==0,"Reaction never recovered");
  }
  var manta=GameObject.Find("Manta ray gliding over the reef").GetComponent<MarineSwimmer>();Require(manta.encounterRoll,"Manta roll missing");for(int i=0;i<1800;i++)manta.Step(1f/30);Require(manta.GetComponent<ReefSphereBody>().Clear(manta.transform.position),"Manta penetration");
  ReefCorrections.Validate();Debug.Log("DAY_THREE_VALIDATED clownfishScatterReturn=passed encounters=passed recovery=passed mantaRoll=enabled previousRegressions=passed");
 }
 public static void Capture(){
 EditorSceneManager.OpenScene(Scene);System.IO.Directory.CreateDirectory("Renders");var camera=Camera.main;camera.allowHDR=false;camera.fieldOfView=48;
 foreach(string name in new[]{"Day 3 reef anemone","Day 3 rocky octopus"}){var t=GameObject.Find(name).transform;Vector3 offset=new Vector3(1,1,1.5f);for(int angle=0;angle<360;angle+=45){var candidate=Quaternion.Euler(0,angle,0)*offset;if(!Physics.Linecast(t.position+Vector3.up*.3f,t.position+candidate,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore)){offset=candidate;break;}}camera.transform.position=t.position+offset;camera.transform.LookAt(t.position+Vector3.up*.1f);ReefCorrectionAudit.Capture(camera,"Renders/"+name.Replace(" ","_")+".png");}
 foreach(var turtle in UnityEngine.Object.FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None)){if(turtle.transform.position.z>50)continue;foreach(var mesh in turtle.GetComponentsInChildren<MeshFilter>())Debug.Log("TURTLE_PART "+mesh.name+" bounds="+mesh.sharedMesh.bounds);}
}
static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
}
