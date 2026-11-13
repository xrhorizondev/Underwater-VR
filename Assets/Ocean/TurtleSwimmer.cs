using UnityEngine;
namespace Underwater {
public class TurtleSwimmer:MonoBehaviour {
 ReefSphereBody body;Vector3 home;float clock,heading;Renderer[] skin;MaterialPropertyBlock block;
 void Start(){body=GetComponent<ReefSphereBody>();home=transform.position;heading=transform.eulerAngles.y;skin=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();}
 void Update(){
  float dt=Mathf.Min(Time.deltaTime,.05f);clock+=dt;
  Vector3 direction=Quaternion.Euler(0,heading+Mathf.Sin(clock*.16f)*18,0)*Vector3.forward;
  Vector3 back=home-transform.position;if(back.magnitude>5)direction=Vector3.Lerp(direction,back.normalized,Mathf.Clamp01((back.magnitude-5)*.5f));
  if(Physics.SphereCast(transform.position,body?body.radius+.1f:.22f,transform.forward,out var hit,1.6f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))direction=(direction+hit.normal*2+Vector3.up*.12f).normalized;
  direction.y=Mathf.Clamp(direction.y,-.06f,.06f);
  transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),12*dt);
  if(body)body.Move(transform.forward*(.19f+.035f*Mathf.Sin(clock*1.31f))*dt);
  foreach(var r in skin){r.GetPropertyBlock(block);block.SetFloat("_StrokePhase",clock/4.8f);float look=Mathf.Sin(clock*.7f)*.12f;var diver=Diver.Instance;if(diver&&diver.Head&&(diver.Head.position-transform.position).sqrMagnitude<9)look=Mathf.Clamp(transform.InverseTransformDirection(diver.Head.position-transform.position).x,-1,1)*.28f;block.SetFloat("_HeadTurn",look);float blink=Mathf.Clamp01(1-Mathf.Abs((clock+home.z)%6.5f-3.25f)/.14f);block.SetFloat("_Blink",blink);block.SetFloat("_EyePart",r.name.Contains("iris")||r.name.Contains("pupil")?1:0);r.SetPropertyBlock(block);}
 }
}
}

