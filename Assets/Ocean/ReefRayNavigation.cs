using UnityEngine;
namespace Underwater {
// Persistent escape heading prevents an orbit target from pinning a ray to rock.
public sealed class ReefRayNavigation {
 static readonly float[] Headings={0,30,-30,60,-60,90,-90,135,-135,180};
 ReefAnimalClearance ground;Vector3 escape;float probeTimer,hold,stalled;
 public bool Avoiding {get;private set;}
 public Vector3 Direction(Transform actor,ReefSphereBody body,Vector3 goal,float speed,float dt,bool freeEscape=false){
  probeTimer-=dt;hold-=dt;
  if(!body)return goal;
  if(!ground)ground=actor.GetComponent<ReefAnimalClearance>();
  if(probeTimer>0)return hold>0?escape:goal;
  probeTimer=.18f;float reach=Mathf.Max(2.5f,speed*7);
  // Finish a clearance-tested turn before choosing another relative heading.
  // Re-selecting every probe made a slow fish chase a receding escape vector.
  if(Avoiding&&hold>0&&escape.sqrMagnitude>.1f&&body.Clear(actor.position+escape*.05f)&&!Physics.SphereCast(body.WorldCenter,body.radius,escape,out _,reach*.65f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore)){
   if(Vector3.Dot(actor.forward,escape)<.7f)hold=Mathf.Max(hold,.4f);return escape;
  }
  bool sandBlocked=ground&&ground.RequiredHeight(actor.position+goal*reach,Quaternion.LookRotation(goal),out float sandHeight)&&sandHeight>actor.position.y+reach*.15f;
  bool blocked=sandBlocked||Physics.SphereCast(body.WorldCenter,body.radius,actor.forward,out _,reach,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore)||Physics.SphereCast(body.WorldCenter,body.radius,goal,out _,reach,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);
  if(!blocked&&stalled<.2f){if(hold>0)return escape;Avoiding=false;return goal;}
  Avoiding=true;float best=float.NegativeInfinity;Vector3 selected=-actor.forward;
  foreach(float yaw in Headings)for(int level=-1;level<=1;level++){
   var candidate=Quaternion.AngleAxis(yaw,Vector3.up)*Vector3.ProjectOnPlane(actor.forward,Vector3.up).normalized;candidate.y=level*.3f;
   if(actor.position.y>7.5f-body.radius-.3f&&candidate.y>0)continue;
   candidate.Normalize();if(!body.Clear(actor.position+candidate*.01f)||!body.Clear(actor.position+candidate*.05f))continue;float clearance=reach;
   if(Physics.SphereCast(body.WorldCenter,body.radius,candidate,out var hit,reach,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))clearance=hit.distance;
   if(ground&&ground.RequiredHeight(actor.position+candidate*clearance,Quaternion.LookRotation(candidate),out float required)&&required-actor.position.y>Mathf.Max(.15f,clearance*.22f))continue;
   float score=clearance/reach*4-Mathf.Abs(candidate.y)*.8f+Vector3.Dot(candidate,goal)*.65f+Vector3.Dot(candidate,actor.forward)*.25f+(hold>0?Vector3.Dot(candidate,escape)*.45f:0);
   if(score>best){best=score;selected=candidate;}
  }
  if(freeEscape&&stalled>.2f)foreach(var candidate in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up,Vector3.down}){
   if(actor.position.y>7.5f-body.radius-.3f&&candidate.y>0)continue;
   if(!body.Clear(actor.position+candidate*.01f)||!body.Clear(actor.position+candidate*.05f))continue;float clearance=reach;if(Physics.SphereCast(body.WorldCenter,body.radius,candidate,out var hit,reach,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))clearance=hit.distance;
   if(ground&&ground.RequiredHeight(actor.position+candidate*clearance,Quaternion.LookRotation(candidate),out float required)&&required-actor.position.y>Mathf.Max(.15f,clearance*.22f))continue;
   float score=clearance/reach*4-Mathf.Abs(candidate.y)*.8f+Vector3.Dot(candidate,goal)*.65f+Vector3.Dot(candidate,actor.forward)*.25f;
   if(score>best){best=score;selected=candidate;}
  }
  escape=selected;hold=1.1f;return escape;
 }
 public void RecordMovement(float distance,float expected,float dt){stalled=distance<expected*.08f?stalled+dt:0;}
}
}
