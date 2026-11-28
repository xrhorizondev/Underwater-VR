using UnityEngine;
namespace Underwater {
public class TurtleSwimmer:MonoBehaviour {
 ReefSphereBody body;Vector3 home;float clock,heading;Renderer[] skin;MaterialPropertyBlock block;
 ReefImportedAnimation stroke;bool ready;public float ActualSpeed{get;private set;}
 readonly ReefSwimHeading course=new ReefSwimHeading();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();home=transform.position;heading=transform.eulerAngles.y;skin=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();stroke=GetComponentInChildren<ReefImportedAnimation>();}
 void Start(){Initialize();}
 void Update(){
  if(Diver.Instance&&Diver.Instance.MenuOpen||!ReefSimulationRange.Active(transform))return;Step(Mathf.Min(Time.deltaTime,.05f));
 }
 public void Step(float dt){Initialize();clock+=dt;
  Vector3 direction=Quaternion.Euler(0,heading+Mathf.Sin(clock*.16f)*18,0)*Vector3.forward;
  Vector3 back=home-transform.position;if(back.magnitude>5)direction=Vector3.Lerp(direction,back.normalized,Mathf.Clamp01((back.magnitude-5)*.5f));
  if(Physics.SphereCast(transform.position,body?body.radius+.1f:.22f,transform.forward,out var hit,1.6f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))direction=(direction+hit.normal*2+Vector3.up*.12f).normalized;
  direction.y=Mathf.Clamp(direction.y,-.06f,.06f);
  ReefSwimHeading.TryRotate(transform,body,Quaternion.LookRotation(course.Step(transform.forward,direction,dt,10,5,3,14)));
  Vector3 before=transform.position;if(body)body.MoveSliding(transform.forward*(.19f+.02f*Mathf.Sin(clock*1.31f))*dt);ActualSpeed=Vector3.Distance(before,transform.position)/Mathf.Max(.001f,dt);if(stroke)stroke.MatchTravel(ActualSpeed,.2f);
  foreach(var r in skin){r.GetPropertyBlock(block);block.SetFloat("_StrokePhase",clock/4.8f);float look=Mathf.Sin(clock*.7f)*.12f;var diver=Diver.Instance;if(diver&&diver.Head&&(diver.Head.position-transform.position).sqrMagnitude<9)look=Mathf.Clamp(transform.InverseTransformDirection(diver.Head.position-transform.position).x,-1,1)*.28f;block.SetFloat("_HeadTurn",look);float blink=Mathf.Clamp01(1-Mathf.Abs((clock+home.z)%6.5f-3.25f)/.14f);block.SetFloat("_Blink",blink);block.SetFloat("_EyePart",r.name.Contains("iris")||r.name.Contains("pupil")?1:0);r.SetPropertyBlock(block);}
 }
}
}

