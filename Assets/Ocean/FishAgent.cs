using UnityEngine;

namespace Underwater {
// Movement is measured in metres/second. Mesh deformation stays in body space.
public class FishAgent : MonoBehaviour {
 public float bodyLength=.35f, cruiseSpeed=.5f, variation;
 public Vector3 SteeringDirection {get;private set;}
 public Vector3 Velocity { get; private set; }
 public float TailPhase { get; private set; }
 public bool IsGrabbable=>interaction&&interaction.grabbable;
 ReefSphereBody body; SeaLife interaction;Renderer skin; MaterialPropertyBlock properties;
 readonly ReefRayNavigation escapeNavigation=new ReefRayNavigation();float stalled,escapeTime;
 readonly ReefSwimHeading course=new ReefSwimHeading();
 float currentSpeed, visualSpeed, bank, obstacleTimer; Vector3 avoidance,escapeVelocity;bool wasEscaping;
 public float VisualSpeed=>visualSpeed;
 public bool Escaping=>escapeTime>0;
 public void Initialize(int index) {
  body=GetComponent<ReefSphereBody>();interaction=GetComponent<SeaLife>();variation=index*2.399963f;
  TailPhase=variation;
  currentSpeed=cruiseSpeed;visualSpeed=cruiseSpeed;
  Velocity=transform.forward*currentSpeed;
  skin=GetComponentInChildren<Renderer>();properties=new MaterialPropertyBlock();
  UpdateSkin(0);
 }
 public void Step(float dt,float time,Vector3 steering,float speedScale=1) {
  if(dt<=0)return;if(interaction&&interaction.IsHeld){course.Reset();Velocity=Vector3.zero;visualSpeed=Mathf.Lerp(visualSpeed,0,1-Mathf.Exp(-dt*8));TailPhase+=dt*4;UpdateSkin(0);return;}
  obstacleTimer-=dt;
  if(obstacleTimer<=0){
   obstacleTimer=.16f+(variation%1)*.06f;avoidance=Vector3.zero;
   float lookAhead=Mathf.Max(.8f,currentSpeed*2.2f);
   if(Physics.SphereCast(transform.position,body?body.radius+.08f:bodyLength*.6f,transform.forward,out var hit,lookAhead,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore)){
    avoidance=hit.normal*3+Vector3.up*.5f;
   }
   if(Physics.Raycast(transform.position,Vector3.down,out var ground,bodyLength*2+.35f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))avoidance+=Vector3.up*2;
  }
  Vector3 desired=steering+avoidance;
  if(desired.sqrMagnitude<.001f)desired=transform.forward;
  if(stalled>.4f)escapeTime=3;escapeTime-=dt;if(escapeTime>0)desired=escapeNavigation.Direction(transform,body,desired.normalized,currentSpeed,dt,true);
  desired.Normalize();if(escapeTime<=0)desired.y=Mathf.Clamp(desired.y,-.18f,.18f);desired.Normalize();
  Vector3 forward=course.Step(transform.forward,desired,dt,escapeTime>0?60:26,escapeTime>0?22:10,escapeTime>0?12:7,escapeTime>0?60:28);
  float turn=Vector3.SignedAngle(transform.forward,forward,Vector3.up)/Mathf.Max(dt,.001f);
  bank=Mathf.Lerp(bank,Mathf.Clamp(-turn*.13f,-8,8),1-Mathf.Exp(-dt*3));
  ReefSwimHeading.TryRotate(transform,body,Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward));forward=transform.forward;
  float turnEase=Mathf.Lerp(.7f,1,Mathf.Clamp01(Vector3.Dot(forward,desired)));float targetSpeed=cruiseSpeed*(.9f+.12f*Mathf.Sin(time*.55f+variation))*turnEase*Mathf.Clamp01(speedScale);
  currentSpeed=Mathf.MoveTowards(currentSpeed,targetSpeed,cruiseSpeed*.55f*dt);
  float escapeEase=escapeTime>0?Mathf.Lerp(.18f,1,Mathf.Clamp01(Vector3.Dot(forward,desired))):1;var motion=forward*currentSpeed;
  if(escapeTime>0){if(!wasEscaping)escapeVelocity=Velocity;escapeVelocity=Vector3.MoveTowards(escapeVelocity,desired*currentSpeed*escapeEase,.45f*dt);motion=escapeVelocity;}wasEscaping=escapeTime>0;
  SteeringDirection=desired;Velocity=body?body.MoveSliding(motion*dt)/Mathf.Max(dt,.0001f):Vector3.zero;
  stalled=Velocity.magnitude<currentSpeed*.08f?stalled+dt:0;escapeNavigation.RecordMovement(Velocity.magnitude*dt,currentSpeed*dt,dt);
  // Independent, speed-coupled tail beats: no synchronized whole-body wobble.
  visualSpeed=Mathf.Lerp(visualSpeed,Velocity.magnitude,1-Mathf.Exp(-dt*6));TailPhase+=dt*Mathf.PI*2*Mathf.Lerp(.22f,3.6f,Mathf.Clamp01(visualSpeed/Mathf.Max(bodyLength*3,.1f)));
  UpdateSkin(turn);
 }
 void UpdateSkin(float turn){
  if(!skin)return;
  skin.GetPropertyBlock(properties);
  properties.SetFloat("_TailPhase",TailPhase);
  properties.SetFloat("_TailAmplitude",interaction&&interaction.IsHeld?.018f:Mathf.Lerp(.008f,.06f,Mathf.Clamp01(visualSpeed/Mathf.Max(bodyLength*3,.1f))));
  properties.SetFloat("_TurnBend",Mathf.Clamp(turn/55,-1,1)*.025f);
  skin.SetPropertyBlock(properties);
 }
}
}



