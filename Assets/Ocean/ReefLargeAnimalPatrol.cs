using UnityEngine;
namespace Underwater {
// Authored loops are checked with the entire animated envelope plus rock clearance.
// Position and tangent share one clock, so turns do not steer into the inside of a bend.
public sealed class ReefLargeAnimalPatrol:MonoBehaviour {
 public Vector3 center;public Vector2 radii=new Vector2(12,10);
 public float phase,speed=.8f,rockMargin=.8f;
 ReefSphereBody body;float angle;bool ready;
 public float ActualSpeed{get;private set;}
 public Vector3 Position(float t)=>center+new Vector3(radii.x*Mathf.Sin(t),0,radii.y*Mathf.Cos(t));
 public Quaternion Rotation(float t)=>Quaternion.LookRotation(new Vector3(radii.x*Mathf.Cos(t),0,-radii.y*Mathf.Sin(t)),Vector3.up);
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();angle=phase;}
 public void Step(float dt){
  Initialize();if(dt<=0)return;
  float derivative=new Vector2(radii.x*Mathf.Cos(angle),radii.y*Mathf.Sin(angle)).magnitude;
  float next=angle+speed*dt/Mathf.Max(.1f,derivative);var rotation=Rotation(next);var oldRotation=transform.rotation;var old=transform.position;
  transform.rotation=rotation;
  if(body&&!body.Clear(old)){transform.rotation=oldRotation;ActualSpeed=0;return;}
  var target=Position(next);var delta=Vector3.ClampMagnitude(target-old,speed*dt*1.05f);
  if(body)body.Move(delta);else transform.position+=delta;
  ActualSpeed=Vector3.Distance(old,transform.position)/dt;
  if(Vector3.Distance(transform.position,target)<.002f)angle=Mathf.Repeat(next,Mathf.PI*2);
 }
}
}
