using UnityEngine;
namespace Underwater {
// Observation encounter: pursuit earns advice, never contact damage.
public sealed class OpenWaterShark:MonoBehaviour {
 public AudioClip cautionVoice;public float patrolRadius=7;
 ReefSphereBody body;Renderer skin;MaterialPropertyBlock block;Vector3 home,previousViewer;float clock,pursuit,cooldown;bool ready,hasViewer;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();home=transform.position;}
 void Update(){var d=Diver.Instance;if(!d||!d.Head||d.MenuOpen||!ReefSimulationRange.Active(transform)){hasViewer=false;pursuit=0;return;}float dt=Mathf.Min(Time.deltaTime,.05f);Vector3 viewer=d.Head.position;Vector3 velocity=hasViewer?(viewer-previousViewer)/Mathf.Max(dt,.001f):Vector3.zero;if((viewer-previousViewer).sqrMagnitude>4)velocity=Vector3.zero;previousViewer=viewer;hasViewer=true;bool warn=Step(dt,viewer,velocity,d.Zone==6);if(warn&&!d.VoicePlaying&&d.TryCaution("Let's not chase it, just watch.",cautionVoice))MarkWarned();}
 public void MarkWarned(){cooldown=18;pursuit=0;}
 public bool Step(float dt,Vector3 viewer,Vector3 viewerVelocity,bool inHabitat){Initialize();clock+=dt;cooldown=Mathf.Max(0,cooldown-dt);var toward=transform.position-viewer;float distance=toward.magnitude;
  bool chasing=inHabitat&&distance<14&&distance>2&&viewerVelocity.magnitude>.9f&&Vector3.Dot(viewerVelocity.normalized,toward.normalized)>.65f;pursuit=chasing?pursuit+dt:0;
  var goal=home+new Vector3(Mathf.Sin(clock*.055f)*patrolRadius,Mathf.Sin(clock*.13f)*.35f,Mathf.Cos(clock*.055f)*patrolRadius);bool avoiding=distance<8;if(avoiding)goal=transform.position+toward.normalized*6;float speed=avoiding?1.8f:.6f;
  var direction=navigation.Direction(transform,body,(goal-transform.position).normalized,speed,dt);direction.y=Mathf.Clamp(direction.y,-.25f,.25f);if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),45*dt);if(body)navigation.RecordMovement(body.MoveSliding(transform.forward*speed*dt).magnitude,speed*dt,dt);
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",clock*3);skin.SetPropertyBlock(block);}return pursuit>=.75f&&cooldown<=0;
 }
}
}
