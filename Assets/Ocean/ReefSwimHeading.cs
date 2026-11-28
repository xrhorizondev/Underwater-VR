using UnityEngine;
namespace Underwater {
// Rate and acceleration limits keep changing flock/navigation goals from
// producing abrupt yaw reversals or whole-body vertical rocking.
public sealed class ReefSwimHeading {
 float yawRate,pitchRate;
 public void Reset(){yawRate=pitchRate=0;}
 public static bool TryRotate(Transform actor,ReefSphereBody body,Quaternion rotation){var previous=actor.rotation;actor.rotation=rotation;if(body&&!body.Clear(actor.position)){actor.rotation=previous;return false;}return true;}
 public Vector3 Step(Vector3 forward,Vector3 desired,float dt,float yawLimit,float pitchLimit=10,float pitchSpeed=8,float acceleration=35){
  if(dt<=0||desired.sqrMagnitude<.00001f)return forward;
  var flat=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;if(flat.sqrMagnitude<.01f)flat=Vector3.forward;
  var goal=Vector3.ProjectOnPlane(desired,Vector3.up).normalized;if(goal.sqrMagnitude<.01f)goal=flat;
  float error=Vector3.SignedAngle(flat,goal,Vector3.up);
  // Keep the chosen arc when a goal jitters across directly behind the body.
  if(Mathf.Abs(error)>150&&Mathf.Abs(yawRate)>1)error=Mathf.Sign(yawRate)*Mathf.Abs(error);
  // Ease into the course instead of holding maximum yaw until the last instant.
  float target=Mathf.Sign(error)*Mathf.Min(yawLimit,Mathf.Min(Mathf.Abs(error)*1.2f,Mathf.Sqrt(2*acceleration*Mathf.Abs(error))));
  yawRate=Mathf.MoveTowards(yawRate,target,acceleration*dt);
  float step=yawRate*dt;if(Mathf.Sign(step)==Mathf.Sign(error)&&Mathf.Abs(step)>Mathf.Abs(error)){step=error;yawRate=step/dt;}
  flat=Quaternion.AngleAxis(step,Vector3.up)*flat;
  float pitch=Mathf.Asin(Mathf.Clamp(forward.normalized.y,-1,1))*Mathf.Rad2Deg;
  float wanted=Mathf.Clamp(Mathf.Asin(Mathf.Clamp(desired.normalized.y,-1,1))*Mathf.Rad2Deg,-pitchLimit,pitchLimit);
  pitch=Mathf.SmoothDampAngle(pitch,wanted,ref pitchRate,.85f,pitchSpeed,dt)*Mathf.Deg2Rad;
  return (flat*Mathf.Cos(pitch)+Vector3.up*Mathf.Sin(pitch)).normalized;
 }
}
}
