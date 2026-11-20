using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class OctopusGroundValidation {
 const string Scene="Assets/Scenes/Underwater_Realistic.unity";
 static SeaLife Actor()=>UnityEngine.Object.FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Single(l=>l.kind=="Octopus");
 public static void CheckDelivery(){Validate();PreparedMarineIntegration.Validate();OctopusLiveMotionCheck.Run();}
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(Scene);var actor=Actor();var start=actor.transform.position;var skin=actor.transform.Find("Prepared animated model").GetComponentInChildren<SkinnedMeshRenderer>();var animation=skin.GetComponentInParent<Animation>();
  var ground=actor.GetComponent<OctopusGroundClearance>();if(!ground)ground=actor.gameObject.AddComponent<OctopusGroundClearance>();
  var cells=new Dictionary<Vector2Int,float>();
  for(int phase=0;phase<=300;phase++){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*phase/300);foreach(var p in OctopusRepairValidation.Points(skin)){var q=p-start;var cell=new Vector2Int(Mathf.FloorToInt(q.x/ground.footCell),Mathf.FloorToInt(q.z/ground.footCell));if(!cells.TryGetValue(cell,out float y)||q.y<y)cells[cell]=q.y;}}
  ground.supports=cells.Select(p=>new Vector3(p.Key.x*ground.footCell,p.Value-.025f,p.Key.y*ground.footCell)).ToArray();
  ground.terrainOrigin=new Vector2(start.x-10,start.z-10);ground.terrainWidth=ground.terrainDepth=Mathf.CeilToInt(20/ground.terrainCell);ground.terrainHeights=Enumerable.Repeat(float.NegativeInfinity,ground.terrainWidth*ground.terrainDepth).ToArray();
  var floors=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.sharedMesh&&c.sharedMesh.name.IndexOf("seabed",StringComparison.OrdinalIgnoreCase)>=0).ToArray();if(floors.Length==0)throw new Exception("Saved seabed colliders not found");
  foreach(var floor in floors){var mesh=floor.sharedMesh;var vertices=mesh.vertices.Select(p=>floor.transform.TransformPoint(p)).ToArray();var triangles=mesh.triangles;
   for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
    int x0=Mathf.FloorToInt((Mathf.Min(a.x,Mathf.Min(b.x,c.x))-ground.terrainOrigin.x)/ground.terrainCell),x1=Mathf.FloorToInt((Mathf.Max(a.x,Mathf.Max(b.x,c.x))-ground.terrainOrigin.x)/ground.terrainCell);
    int z0=Mathf.FloorToInt((Mathf.Min(a.z,Mathf.Min(b.z,c.z))-ground.terrainOrigin.y)/ground.terrainCell),z1=Mathf.FloorToInt((Mathf.Max(a.z,Mathf.Max(b.z,c.z))-ground.terrainOrigin.y)/ground.terrainCell);
    if(x1<0||z1<0||x0>=ground.terrainWidth||z0>=ground.terrainDepth)continue;
    float y=Mathf.Max(a.y,Mathf.Max(b.y,c.y));for(int z=Mathf.Max(0,z0);z<=Mathf.Min(ground.terrainDepth-1,z1);z++)for(int x=Mathf.Max(0,x0);x<=Mathf.Min(ground.terrainWidth-1,x1);x++){int index=x+z*ground.terrainWidth;ground.terrainHeights[index]=Mathf.Max(ground.terrainHeights[index],y);}
   }
  }
  if(ground.terrainHeights.Any(y=>float.IsInfinity(y)||float.IsNaN(y)))throw new Exception("Seabed patch has holes");
  float height=ground.RequiredHeight(start);if(float.IsInfinity(height))throw new Exception("Invalid arm support footprint");var position=start;position.y=height;actor.transform.position=position;Physics.SyncTransforms();
  var body=actor.GetComponent<ReefSphereBody>();if(!body.Clear(position))throw new Exception("Raised octopus body is obstructed");
  animation.clip.SampleAnimation(animation.gameObject,0);EditorUtility.SetDirty(ground);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("OCTOPUS_GROUND_BAKED supportCells="+ground.supports.Length+" terrainCells="+ground.terrainHeights.Length+" initialLift="+(height-start.y)+" raisedPosition="+position);
  Validate();OctopusRepairValidation.Capture();
 }
 public static void Validate(){
  EditorSceneManager.OpenScene(Scene);var actor=Actor();var ground=actor.GetComponent<OctopusGroundClearance>();var skin=actor.transform.Find("Prepared animated model").GetComponentInChildren<SkinnedMeshRenderer>();var animation=skin.GetComponentInParent<Animation>();var home=actor.transform.position;float minGap=float.PositiveInfinity;int samples=0;
  // Includes half-frame animation phases that were not footprint bake samples.
  foreach(var offset in new[]{Vector3.zero,Vector3.forward*2,Vector3.back*2,Vector3.left*2,Vector3.right*2}){
   var p=home+offset;p.y=ground.RequiredHeight(p);actor.transform.position=p;
   for(int phase=0;phase<=120;phase++){animation.clip.SampleAnimation(animation.gameObject,animation.clip.length*(phase+.5f)/121);foreach(var point in OctopusRepairValidation.Points(skin)){float gap=point.y-ground.SandUpperHeight(point);minGap=Mathf.Min(minGap,gap);if(gap<-.0001f)throw new Exception("Animated arm penetrates sand: "+gap+" phase="+phase+" position="+p);samples++;}}
  }
  float maxSpeed=0,maxTravel=0;
  for(int heading=0;heading<8;heading++){
   EditorSceneManager.OpenScene(Scene);actor=Actor();ground=actor.GetComponent<OctopusGroundClearance>();home=actor.transform.position;var encounter=actor.GetComponent<ReefEncounter>();encounter.Initialize();var body=actor.GetComponent<ReefSphereBody>();var direction=Quaternion.Euler(0,heading*45,0)*Vector3.forward;
   // Includes collision sliding, repeated escape triggers and the return home.
   for(int i=0;i<3600;i++){var before=actor.transform.position;encounter.Step(1f/60,before-direction,i<1800?.1f:10);var after=actor.transform.position;maxSpeed=Mathf.Max(maxSpeed,Mathf.Abs(after.y-before.y)*60);maxTravel=Mathf.Max(maxTravel,Vector3.Distance(home,after));if(after.y<ground.RequiredHeight(after)-.0001f)throw new Exception("Crawl/escape outran ground clearance heading="+heading);if(!body.Clear(after))throw new Exception("Octopus body entered solid geometry");}
  }
  if(maxTravel<.25f)throw new Exception("Grounded octopus cannot travel");
  if(maxSpeed>.66f)throw new Exception("Grounding correction snapped vertically");
  Debug.Log("OCTOPUS_GROUND_VALIDATED vertexSamples="+samples+" minSandGap="+minGap+" headings=8 secondsPerHeading=60 maxTravel="+maxTravel+" maxVerticalSpeed="+maxSpeed+" noRuntimeMeshBake=true");
  EditorSceneManager.OpenScene(Scene);
 }
}
}
