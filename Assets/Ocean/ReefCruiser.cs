using UnityEngine;
namespace Underwater {
// Autonomous motion for fish that previously had deformation but no position owner.
public sealed class ReefCruiser:MonoBehaviour {
 public float speed=.22f,radius=3;public bool squid;
 Vector3 home;float clock,currentSpeed;bool initialized;ReefSphereBody body;ReefAnimalClearance clearance;SeaLife life;Renderer skin;MaterialPropertyBlock block;
 public float ActualSpeed{get;private set;}
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(initialized)return;initialized=true;home=transform.position;body=GetComponent<ReefSphereBody>();clearance=GetComponent<ReefAnimalClearance>();life=GetComponent<SeaLife>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen||!ReefSimulationRange.Active(transform))return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public void Step(float dt){Initialize();if(life&&life.IsHeld)return;clock+=dt;
  Vector3 goal=home+new Vector3(Mathf.Sin(clock*.12f)*radius,Mathf.Sin(clock*.21f)*.3f,Mathf.Cos(clock*.12f)*radius);
  if(life&&life.kind=="Lumen"&&Diver.Instance&&Diver.Instance.Head&&Diver.Instance.Zone>=3)goal=Diver.Instance.Head.position+Diver.Instance.Head.forward*2;
  Vector3 direction=(goal-transform.position).normalized;if(direction.sqrMagnitude<.01f)direction=transform.forward;
  direction=navigation.Direction(transform,body,direction,speed,dt);
  var nextRotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),25*dt);bool canTurn=true;if(body&&clearance&&clearance.RequiredHeight(transform.position,nextRotation,out float height)&&height>transform.position.y+.012f){body.MoveSliding(Vector3.up*Mathf.Min(height-transform.position.y+.02f,.3f*dt));canTurn=height<=transform.position.y+.012f;}if(canTurn)transform.rotation=nextRotation;
  float targetSpeed=squid?speed:speed*(.94f+.06f*Mathf.Sin(clock*.31f+home.x))*Mathf.Lerp(.5f,1,Mathf.Max(0,Vector3.Dot(transform.forward,direction)));currentSpeed=Mathf.MoveTowards(currentSpeed,targetSpeed,Mathf.Max(.05f,speed*.65f)*dt);if(body){var delta=transform.forward*currentSpeed*dt;if(clearance&&clearance.RequiredHeight(transform.position+delta,transform.rotation,out float floor)&&floor>transform.position.y+delta.y+.012f)delta=Vector3.up*Mathf.Min(floor-transform.position.y+.02f,.3f*dt);var moved=body.MoveSliding(delta).magnitude;ActualSpeed=moved/Mathf.Max(dt,.001f);navigation.RecordMovement(moved,currentSpeed*dt,dt);}
  if(squid&&skin){skin.GetPropertyBlock(block);block.SetFloat("_SwimPhase",clock);skin.SetPropertyBlock(block);}
 }
}
}
