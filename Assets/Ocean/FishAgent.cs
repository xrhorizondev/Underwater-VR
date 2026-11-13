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
 float currentSpeed, bank, obstacleTimer; Vector3 avoidance;
 public void Initialize(int index) {
  body=GetComponent<ReefSphereBody>();interaction=GetComponent<SeaLife>();variation=index*2.399963f;
  TailPhase=variation;
  currentSpeed=cruiseSpeed;
  Velocity=transform.forward*currentSpeed;
  skin=GetComponentInChildren<Renderer>();properties=new MaterialPropertyBlock();
  UpdateSkin(0);
 }
 public void Step(float dt,float time,Vector3 steering) {
  if(interaction&&interaction.IsHeld){Velocity=Vector3.zero;TailPhase+=dt*7;UpdateSkin(0);return;}
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
  Vector3 flat=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;Vector3 targetFlat=Vector3.ProjectOnPlane(desired,Vector3.up).normalized;float yawStep=Mathf.Clamp(Vector3.SignedAngle(flat,targetFlat,Vector3.up),-50*dt,50*dt);flat=Quaternion.AngleAxis(yawStep,Vector3.up)*flat;float pitch=Mathf.MoveTowards(Mathf.Asin(Mathf.Clamp(transform.forward.y,-1,1)),Mathf.Clamp(Mathf.Asin(desired.y),-Mathf.Deg2Rad*10,Mathf.Deg2Rad*10),Mathf.Deg2Rad*12*dt);Vector3 forward=flat*Mathf.Cos(pitch)+Vector3.up*Mathf.Sin(pitch);
  float turn=Vector3.SignedAngle(transform.forward,forward,Vector3.up)/Mathf.Max(dt,.001f);
  bank=Mathf.Lerp(bank,Mathf.Clamp(-turn*.13f,-8,8),1-Mathf.Exp(-dt*3));
  transform.rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);
  float targetSpeed=cruiseSpeed*(.9f+.12f*Mathf.Sin(time*.55f+variation));
  currentSpeed=Mathf.MoveTowards(currentSpeed,targetSpeed,cruiseSpeed*.55f*dt);
  SteeringDirection=desired;Velocity=body?body.MoveSliding((escapeTime>0?desired:forward)*currentSpeed*dt)/Mathf.Max(dt,.0001f):Vector3.zero;
  stalled=Velocity.magnitude<currentSpeed*.08f?stalled+dt:0;escapeNavigation.RecordMovement(Velocity.magnitude*dt,currentSpeed*dt,dt);
  // Independent, speed-coupled tail beats: no synchronized whole-body wobble.
  TailPhase+=dt*Mathf.PI*2*Mathf.Lerp(1.6f,3.6f,Mathf.Clamp01(currentSpeed/Mathf.Max(bodyLength*3,.1f)));
  UpdateSkin(turn);
 }
 void UpdateSkin(float turn){
  if(!skin)return;
  skin.GetPropertyBlock(properties);
  properties.SetFloat("_TailPhase",TailPhase);
  properties.SetFloat("_TailAmplitude",.045f+.02f*Mathf.Clamp01(currentSpeed/Mathf.Max(bodyLength*3,.1f)));
  properties.SetFloat("_TurnBend",Mathf.Clamp(turn/55,-1,1)*.025f);
  skin.SetPropertyBlock(properties);
 }
}
}



