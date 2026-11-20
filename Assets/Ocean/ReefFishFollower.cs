using UnityEngine;
namespace Underwater {
// Trail a swimming diver, circle forward on stopping, then hover facing them.
public sealed class ReefFishFollower:MonoBehaviour {
 ReefSphereBody body;SeaLife life;
 Vector3 home,focus,heading,previousViewer,viewerVelocity,travelForward,travelVelocity;
 float clock,idleTime,frontBlend,orbitYaw;bool ready,hasViewer;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();life=GetComponent<SeaLife>();home=focus=transform.position;heading=transform.forward;travelForward=Vector3.forward;}
 void Update(){var d=Diver.Instance;if(d&&(d.MenuOpen||d.Zone<3&&!ReefSimulationRange.Active(transform))){hasViewer=false;viewerVelocity=Vector3.zero;return;}Step(Mathf.Min(Time.deltaTime,.05f),d&&d.Zone>=3&&d.Head?(Vector3?)d.Head.position:null,d&&d.Head?d.Head.forward:Vector3.forward);}
 public void Step(float dt,Vector3? diver){Step(dt,diver,Vector3.forward);}
 public void Step(float dt,Vector3? diver,Vector3 viewForward){
  Initialize();if(dt<=0)return;clock+=dt;
  if(life&&life.IsHeld){heading=transform.forward;travelVelocity=Vector3.zero;hasViewer=false;return;}
  var anchor=diver??home;
  var flatView=Vector3.ProjectOnPlane(viewForward,Vector3.up);if(flatView.sqrMagnitude<.01f)flatView=travelForward;flatView.Normalize();
  if(diver.HasValue){
   if(!hasViewer){previousViewer=anchor;travelForward=flatView;var radial=Vector3.ProjectOnPlane(transform.position-anchor,Vector3.up);orbitYaw=Mathf.Atan2(radial.x,radial.z)*Mathf.Rad2Deg;focus=anchor;idleTime=0;}
   var delta=anchor-previousViewer;previousViewer=anchor;hasViewer=true;
   // Teleports are not swimming velocity; the fish catches up without jumping.
   var measured=delta.sqrMagnitude>4?Vector3.zero:Vector3.ProjectOnPlane(delta/dt,Vector3.up);
   viewerVelocity=Vector3.Lerp(viewerVelocity,measured,1-Mathf.Exp(-dt*4));
   float viewerSpeed=viewerVelocity.magnitude;
   if(viewerSpeed>.18f){idleTime=0;travelForward=Vector3.RotateTowards(travelForward,viewerVelocity.normalized,45*Mathf.Deg2Rad*dt,0);}
   else if(viewerSpeed<.10f)idleTime+=dt;
   frontBlend=Mathf.MoveTowards(frontBlend,idleTime>.65f?1:0,dt*.4f);
  }else{hasViewer=false;frontBlend=Mathf.MoveTowards(frontBlend,0,dt*.4f);}
  focus=Vector3.MoveTowards(focus,anchor,2*dt);
  var back=-travelForward+Vector3.Cross(Vector3.up,travelForward)*.25f;back.Normalize();
  float backYaw=Mathf.Atan2(back.x,back.z)*Mathf.Rad2Deg,frontYaw=Mathf.Atan2(flatView.x,flatView.z)*Mathf.Rad2Deg;
  float targetYaw=Mathf.LerpAngle(backYaw,frontYaw,Mathf.SmoothStep(0,1,frontBlend));
  orbitYaw=Mathf.MoveTowardsAngle(orbitYaw,targetYaw,35*dt);
  var goal=focus+Quaternion.Euler(0,orbitYaw,0)*Vector3.forward*Mathf.Lerp(2.2f,1.8f,frontBlend);
  goal.y=focus.y-.18f+Mathf.Sin(clock*.65f)*.07f;
  var offset=goal-transform.position;float distance=offset.magnitude;
  float speed=Mathf.Min(1.5f,Mathf.Max(0,distance-.06f)*.8f);
  var desired=offset.sqrMagnitude>.00001f?offset.normalized:heading;
  var away=Vector3.ProjectOnPlane(transform.position-anchor,Vector3.up);
  if(diver.HasValue&&away.sqrMagnitude<1.3f*1.3f&&away.sqrMagnitude>.001f)desired=(desired+away.normalized*2).normalized;
  desired=navigation.Direction(transform,body,desired,speed,dt,true);
  if(!navigation.Avoiding)desired.y=Mathf.Clamp(desired.y,-.3f,.3f);
  travelVelocity=Vector3.MoveTowards(travelVelocity,desired.normalized*speed,.7f*dt);
  float faceViewer=diver.HasValue?frontBlend*(1-Mathf.SmoothStep(.25f,1.0f,distance)):0;
  var swimming=travelVelocity.sqrMagnitude>.0025f?travelVelocity.normalized:heading;
  var look=Vector3.Lerp(swimming,(anchor-transform.position).normalized,faceViewer);
  if(look.sqrMagnitude>.0001f)heading=Vector3.RotateTowards(heading,look.normalized,40*Mathf.Deg2Rad*dt,0).normalized;
  transform.rotation=Quaternion.LookRotation(heading,Vector3.up);
  if(body)navigation.RecordMovement(body.MoveSliding(travelVelocity*dt).magnitude,travelVelocity.magnitude*dt,dt);
 }
}
}
