using UnityEngine;
namespace Underwater {
 public class MarineSwimmer:MonoBehaviour {
  ReefSphereBody body;Renderer skin;MaterialPropertyBlock properties;float clock,bank;bool initialized;
  public Vector3 center;public float radius=3,speed=.12f,phase;public Transform[] fins;public bool isRay;
  public void Initialize(){if(initialized)return;initialized=true;body=GetComponent<ReefSphereBody>();center=isRay?transform.position+transform.right*10:transform.position;skin=GetComponent<Renderer>();properties=new MaterialPropertyBlock();}
  void Start(){Initialize();}
  void Update(){Step(Mathf.Min(Time.deltaTime,.05f));}
  public void Step(float dt){Initialize();clock+=dt;
   if(isRay){
    Vector3 radial=Vector3.ProjectOnPlane(transform.position-center,Vector3.up);if(radial.sqrMagnitude<1)radial=-transform.right*10;
    Vector3 goal=center+Quaternion.AngleAxis(22,Vector3.up)*radial.normalized*10;goal.y=center.y+Mathf.Sin(clock*.12f)*.25f;
    Vector3 desired=(goal-transform.position).normalized;
    if(body&&Physics.SphereCast(transform.position,body.radius+.12f,transform.forward,out var hit,3,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))desired=(desired+hit.normal*3).normalized;
    desired.y=Mathf.Clamp(desired.y,-.1f,.1f);if(transform.position.y>4.5f)desired.y=-.1f;
    Vector3 forward=Vector3.RotateTowards(transform.forward,desired.normalized,10*Mathf.Deg2Rad*dt,0);float turn=Vector3.SignedAngle(transform.forward,forward,Vector3.up)/Mathf.Max(dt,.001f);bank=Mathf.MoveTowards(bank,Mathf.Clamp(-turn*.8f,-9,9),6*dt);
    transform.rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);
    if(body)body.Move(forward*.42f*dt);
    if(skin){skin.GetPropertyBlock(properties);properties.SetFloat("_StrokePhase",clock*2.05f+phase);properties.SetFloat("_StrokeStrength",.25f+.75f*Mathf.SmoothStep(0,1,(Mathf.Sin(clock*.34f)+1)*.5f));skin.SetPropertyBlock(properties);}return;
   }
   float t=clock*speed+phase;Vector3 p=center+new Vector3(Mathf.Sin(t)*radius,Mathf.Sin(t*1.8f)*.25f,Mathf.Cos(t)*radius*.45f);Vector3 delta=p-transform.position;if(delta.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),dt*2);if(body)body.Move(delta*Mathf.Clamp01(dt*1.5f));
  }
 }
}
