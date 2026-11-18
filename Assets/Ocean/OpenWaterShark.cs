using UnityEngine;
namespace Underwater {
// Observation encounter: pursuit earns advice, never contact damage.
public sealed class OpenWaterShark:MonoBehaviour {
 public AudioClip cautionVoice;public float patrolRadius=7;
 ReefSphereBody body;Renderer skin;MaterialPropertyBlock block;Vector3 home,previousViewer;float clock,pursuit,cooldown,currentSpeed=.6f,bank;bool ready,hasViewer;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();home=transform.position;}
 void Update(){var d=Diver.Instance;if(!d||!d.Head||d.MenuOpen||!ReefSimulationRange.Active(transform)){hasViewer=false;pursuit=0;return;}float dt=Mathf.Min(Time.deltaTime,.05f);Vector3 viewer=d.Head.position;Vector3 velocity=hasViewer?(viewer-previousViewer)/Mathf.Max(dt,.001f):Vector3.zero;if((viewer-previousViewer).sqrMagnitude>4)velocity=Vector3.zero;previousViewer=viewer;hasViewer=true;bool warn=Step(dt,viewer,velocity,d.Zone==6);if(warn&&!d.VoicePlaying&&d.TryCaution("Let's not chase it, just watch.",cautionVoice))MarkWarned();}
 public void MarkWarned(){cooldown=18;pursuit=0;}
 public bool Step(float dt,Vector3 viewer,Vector3 viewerVelocity,bool inHabitat){Initialize();clock+=dt;cooldown=Mathf.Max(0,cooldown-dt);var toward=transform.position-viewer;float distance=toward.magnitude;
  bool chasing=inHabitat&&distance<14&&distance>2&&viewerVelocity.magnitude>.9f&&Vector3.Dot(viewerVelocity.normalized,toward.normalized)>.65f;pursuit=chasing?pursuit+dt:0;
  // Lead around a broad patrol, with a gradual response to a pursuing diver.
  var radial=Vector3.ProjectOnPlane(transform.position-home,Vector3.up);
  if(radial.sqrMagnitude<1)radial=Vector3.back*patrolRadius;
  var goal=home+Quaternion.AngleAxis(25,Vector3.up)*radial.normalized*patrolRadius;
  goal.y=home.y+Mathf.Sin(clock*.13f)*.35f;
  float avoidance=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,4,distance));
  var desired=(goal-transform.position).normalized+toward.normalized*avoidance*1.8f;
  currentSpeed=Mathf.MoveTowards(currentSpeed,Mathf.Lerp(.6f,1.2f,avoidance),.22f*dt);
  var direction=navigation.Direction(transform,body,desired.normalized,currentSpeed,dt);direction.y=Mathf.Clamp(direction.y,-.18f,.18f);
  if(direction.sqrMagnitude>.001f){var old=transform.forward;var forward=Vector3.RotateTowards(old,direction.normalized,18*Mathf.Deg2Rad*dt,0);float turn=Vector3.SignedAngle(old,forward,Vector3.up)/Mathf.Max(dt,.001f);bank=Mathf.Lerp(bank,Mathf.Clamp(-turn*.35f,-6,6),1-Mathf.Exp(-dt*2));transform.rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);}
  if(body)navigation.RecordMovement(body.MoveSliding(transform.forward*currentSpeed*dt).magnitude,currentSpeed*dt,dt);
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",clock*3);skin.SetPropertyBlock(block);}return pursuit>=.75f&&cooldown<=0;
 }
}
}
