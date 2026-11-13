using UnityEngine;
namespace Underwater {
// Optional center lets bottom-dwelling animals keep their feet on the substrate.
public class ReefSphereBody : MonoBehaviour {
 public const int SolidMask=1<<8;
 public float radius=.25f;public Vector3 centerOffset;
 public Vector3 WorldCenter=>transform.position+transform.TransformVector(centerOffset);
 public bool Clear(Vector3 p)=>!Physics.CheckSphere(p+transform.TransformVector(centerOffset),radius,SolidMask,QueryTriggerInteraction.Ignore);
 public Vector3 Move(Vector3 delta){
  Vector3 start=transform.position;float distance=delta.magnitude;
  if(distance<.000001f||!Clear(start))return Vector3.zero;
  if(Physics.SphereCast(WorldCenter,radius,delta/distance,out var hit,distance+.015f,SolidMask,QueryTriggerInteraction.Ignore))distance=Mathf.Max(0,hit.distance-.015f);
  Vector3 end=start+delta.normalized*distance;if(Clear(end))transform.position=end;return transform.position-start;
 }
 public bool PlaceNear(Vector3 p){
  if(Clear(p)){transform.position=p;return true;}
  for(float d=.25f;d<=12;d+=.25f)for(int i=0;i<17;i++){
   Vector3 direction=i==0?Vector3.up:new Vector3(Mathf.Cos(i*2.399963f),.5f+(i%3)*.5f,Mathf.Sin(i*2.399963f)).normalized;
   Vector3 candidate=p+direction*d;if(Clear(candidate)){transform.position=candidate;return true;}
  }return false;
 }
 public Vector3 MoveSliding(Vector3 delta){
  var start=transform.position;var center=WorldCenter;var moved=Move(delta);var remaining=delta-moved;
  if(remaining.sqrMagnitude>.0000001f&&delta.sqrMagnitude>.0000001f&&Physics.SphereCast(center,radius,delta.normalized,out var hit,delta.magnitude+.025f,SolidMask,QueryTriggerInteraction.Ignore)){
   var slide=Vector3.ProjectOnPlane(remaining,hit.normal);if(slide.sqrMagnitude>.0000001f)Move(slide);
  }return transform.position-start;
 }
}
}
