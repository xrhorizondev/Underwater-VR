using UnityEngine;
namespace Underwater {
// Observation encounter: pursuit earns advice, never contact damage.
public sealed class OpenWaterShark:MonoBehaviour {
 public AudioClip cautionVoice;public float patrolRadius=7;
 ReefSphereBody body;ReefAnimalClearance clearance;ReefImportedAnimation stroke;Renderer skin;Animation swim;MaterialPropertyBlock block;Vector3 home,previousViewer,steering;float clock,pursuit,cooldown,currentSpeed=.6f,bank;bool ready,hasViewer;
 public float ActualSpeed{get;private set;}
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();clearance=GetComponent<ReefAnimalClearance>();skin=GetComponent<Renderer>();if(skin&&!skin.enabled)skin=null;swim=GetComponentInChildren<Animation>();stroke=swim?swim.GetComponent<ReefImportedAnimation>():null;block=new MaterialPropertyBlock();home=transform.position;steering=transform.forward;}
 void Update(){var d=Diver.Instance;if(!d||!d.Head||d.MenuOpen||!ReefSimulationRange.Active(transform)){hasViewer=false;pursuit=0;return;}float dt=Mathf.Min(Time.deltaTime,.05f);Vector3 viewer=d.Head.position;Vector3 velocity=hasViewer?(viewer-previousViewer)/Mathf.Max(dt,.001f):Vector3.zero;if((viewer-previousViewer).sqrMagnitude>4)velocity=Vector3.zero;previousViewer=viewer;hasViewer=true;bool warn=Step(dt,viewer,velocity,d.Zone==6);if(warn&&!d.VoicePlaying&&d.TryCaution("Let's not chase it, just watch.",cautionVoice))MarkWarned();}
 public void MarkWarned(){cooldown=18;pursuit=0;}
 public bool Step(float dt,Vector3 viewer,Vector3 viewerVelocity,bool inHabitat){Initialize();clock+=dt;cooldown=Mathf.Max(0,cooldown-dt);var toward=transform.position-viewer;float distance=toward.magnitude;
  bool chasing=inHabitat&&distance<14&&distance>2&&viewerVelocity.magnitude>.9f&&Vector3.Dot(viewerVelocity.normalized,toward.normalized)>.65f;pursuit=chasing?pursuit+dt:0;
  // Lead around a broad patrol, with a gradual response to a pursuing diver.
  var radial=Vector3.ProjectOnPlane(transform.position-home,Vector3.up);
  if(radial.sqrMagnitude<1)radial=Vector3.back*patrolRadius;
  // A tangent plus gentle radial correction produces a broad continuous
  // circuit, without repeatedly steering into a short chord ahead.
  var desired=Vector3.Cross(Vector3.up,radial.normalized)+radial.normalized*Mathf.Clamp((patrolRadius-radial.magnitude)/Mathf.Max(1,patrolRadius),-.6f,.6f);
  desired.y=Mathf.Clamp(home.y+Mathf.Sin(clock*.13f)*.35f-transform.position.y,-.12f,.12f);
  float avoidance=inHabitat?Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,4,distance)):0;
  desired+=Vector3.ProjectOnPlane(toward,Vector3.up).normalized*avoidance*1.8f;
  float turnEase=Mathf.Lerp(.7f,1,Mathf.Clamp01(Vector3.Dot(transform.forward,desired.normalized)));
  float cruise=.7f*(1+.045f*Mathf.Sin(clock*.31f));currentSpeed=Mathf.MoveTowards(currentSpeed,Mathf.Lerp(cruise,1.15f,avoidance)*turnEase,.18f*dt);
  var direction=navigation.Direction(transform,body,desired.normalized,currentSpeed,dt,true);
  // Keep the clearance-tested escape slope. Flattening it after probing can
  // steer straight back into the ledge that the navigation just avoided.
  direction.y=Mathf.Clamp(direction.y,navigation.Avoiding?-.3f:-.14f,navigation.Avoiding?.3f:.14f);direction.Normalize();steering=Vector3.Slerp(steering,direction,1-Mathf.Exp(-dt*1.8f));
  if(steering.sqrMagnitude>.001f){var old=transform.forward;var forward=Vector3.RotateTowards(old,steering.normalized,(navigation.Avoiding?20:14)*Mathf.Deg2Rad*dt,0);float turn=Vector3.SignedAngle(old,forward,Vector3.up)/Mathf.Max(dt,.001f);bank=Mathf.Lerp(bank,Mathf.Clamp(-turn*.3f,-5,5),1-Mathf.Exp(-dt*1.6f));var rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);bool canTurn=true;if(body&&clearance&&clearance.RequiredHeight(transform.position,rotation,out float height)&&height>transform.position.y+.012f){body.MoveSliding(Vector3.up*Mathf.Min(height-transform.position.y+.02f,.25f*dt));canTurn=height<=transform.position.y+.012f;}if(canTurn)transform.rotation=rotation;}
  if(body){var delta=transform.forward*currentSpeed*dt;if(clearance&&clearance.RequiredHeight(transform.position+delta,transform.rotation,out float height)&&height>transform.position.y+delta.y+.012f)delta=Vector3.up*Mathf.Min(height-transform.position.y+.02f,.25f*dt);float moved=body.MoveSliding(delta).magnitude;ActualSpeed=moved/Mathf.Max(dt,.001f);navigation.RecordMovement(moved,currentSpeed*dt,dt);if(stroke)stroke.MatchTravel(ActualSpeed,.75f);}
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",clock*3);skin.SetPropertyBlock(block);}return pursuit>=.75f&&cooldown<=0;
 }
}
}
