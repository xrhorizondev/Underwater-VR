using UnityEngine;
namespace Underwater {
// One owner for both steering and travel; looking around does not drag the fish.
public sealed class ReefFishFollower:MonoBehaviour {
 ReefSphereBody body;SeaLife life;Vector3 home,focus,heading;float clock,currentSpeed;bool ready;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();life=GetComponent<SeaLife>();home=focus=transform.position;heading=transform.forward;}
 void Update(){var d=Diver.Instance;if(d&&d.MenuOpen)return;Step(Mathf.Min(Time.deltaTime,.05f),d&&d.Zone>=3&&d.Head?(Vector3?)d.Head.position:null);}
 public void Step(float dt,Vector3? diver){Initialize();clock+=dt;if(life&&life.IsHeld){heading=transform.forward;currentSpeed=0;return;}
  var anchor=diver??home;focus=Vector3.MoveTowards(focus,anchor,1.6f*dt);
  var offset=transform.position-focus;offset.y=0;if(offset.sqrMagnitude<.01f)offset=-heading;
  var goal=focus+Quaternion.AngleAxis(30,Vector3.up)*offset.normalized*1.8f;goal.y=focus.y-.15f+Mathf.Sin(clock*.65f)*.12f;
  float distance=Vector3.Distance(transform.position,focus);float speed=Mathf.Lerp(.18f,1.1f,Mathf.InverseLerp(1.5f,5,distance));
  var desired=navigation.Direction(transform,body,(goal-transform.position).normalized,speed,dt);desired.y=Mathf.Clamp(desired.y,-.3f,.3f);
  heading=Vector3.RotateTowards(heading,desired.normalized,40*Mathf.Deg2Rad*dt,0).normalized;currentSpeed=Mathf.MoveTowards(currentSpeed,speed,.55f*dt);
  transform.rotation=Quaternion.LookRotation(heading,Vector3.up);
  if(body)navigation.RecordMovement(body.MoveSliding(heading*currentSpeed*dt).magnitude,currentSpeed*dt,dt);
 }
}
}
