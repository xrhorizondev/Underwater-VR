using UnityEngine;
namespace Underwater {
// Autonomous motion for fish that previously had deformation but no position owner.
public sealed class ReefCruiser:MonoBehaviour {
 public float speed=.22f,radius=3;public bool squid;
 Vector3 home;float clock;bool initialized;ReefSphereBody body;SeaLife life;Renderer skin;MaterialPropertyBlock block;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(initialized)return;initialized=true;home=transform.position;body=GetComponent<ReefSphereBody>();life=GetComponent<SeaLife>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen)return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public void Step(float dt){Initialize();if(life&&life.IsHeld)return;clock+=dt;
  Vector3 goal=home+new Vector3(Mathf.Sin(clock*.12f)*radius,Mathf.Sin(clock*.21f)*.3f,Mathf.Cos(clock*.12f)*radius);
  if(life&&life.kind=="Lumen"&&Diver.Instance&&Diver.Instance.Head&&Diver.Instance.Zone>=3)goal=Diver.Instance.Head.position+Diver.Instance.Head.forward*2;
  Vector3 direction=(goal-transform.position).normalized;if(direction.sqrMagnitude<.01f)direction=transform.forward;
  direction=navigation.Direction(transform,body,direction,speed,dt);
  transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),25*dt);
  if(body)navigation.RecordMovement(body.MoveSliding(transform.forward*speed*dt).magnitude,speed*dt,dt);
  if(squid&&skin){skin.GetPropertyBlock(block);block.SetFloat("_SwimPhase",clock);skin.SetPropertyBlock(block);}
 }
}
}
