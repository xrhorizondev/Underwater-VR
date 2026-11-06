using System;
using UnityEngine;
namespace Underwater.Editor {
public static class ReefCollisionValidation {
 public static void Run(){
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Collision validation wall";wall.layer=8;wall.transform.position=new Vector3(1000,0,0);wall.transform.localScale=new Vector3(1,12,12);
  var fish=new GameObject("Collision validation fish");var body=fish.AddComponent<ReefSphereBody>();body.radius=.4f;fish.transform.position=new Vector3(997,0,0);Physics.SyncTransforms();body.Move(Vector3.right*10);
  Require(fish.transform.position.x<999.11f&&fish.transform.position.x>998.9f&&body.Clear(fish.transform.position),"Fish swept wall contact");
  var rig=new GameObject("Collision validation diver");rig.transform.position=new Vector3(997,0,0);var head=new GameObject("Head");head.transform.SetParent(rig.transform,false);head.transform.localPosition=Vector3.up*1.6f;var motor=rig.AddComponent<DiverCollisionMotor>();motor.Initialize();Physics.SyncTransforms();motor.Move(new Vector3(10,0,1));Require(rig.transform.position.x<999.25f&&rig.transform.position.z>.5f,"Diver wall slide");
  Vector3 before=rig.transform.position;Require(!motor.TryTeleport(new Vector3(1000,0,0))&&rig.transform.position==before,"Blocked teleport");
  head.transform.localPosition+=Vector3.right*3;motor.TrackHead();Require(head.transform.position.x<999.3f,"Tracked head wall protection");
  UnityEngine.Object.DestroyImmediate(wall);wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=8;wall.transform.position=new Vector3(1000,-2,0);wall.transform.localScale=new Vector3(20,1,20);Physics.SyncTransforms();Require(motor.TryTeleport(new Vector3(1000,0,0)),"Clear teleport");motor.Move(Vector3.down*20);Require(rig.transform.position.y>-1.55f,"Diver seabed contact");
  UnityEngine.Object.DestroyImmediate(fish);UnityEngine.Object.DestroyImmediate(rig);UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();Debug.Log("REEF_COLLISION_VALIDATED fishSweep diverWallSlide blockedTeleport roomScale seabed");
 }
 static void Require(bool ok,string message){if(!ok)throw new Exception(message+" failed");}
}
}
