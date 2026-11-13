using UnityEngine;
namespace Underwater {
 public class MarineSwimmer:MonoBehaviour {
  ReefSphereBody body;Renderer skin;MaterialPropertyBlock properties;float clock,bank,bankVelocity;Vector3 heading;bool initialized;
  readonly ReefRayNavigation navigation=new ReefRayNavigation();
  public Vector3 center;public float radius=3,speed=.12f,phase;public Transform[] fins;public bool isRay;public bool encounterRoll;
  public void Initialize(){if(initialized)return;initialized=true;heading=transform.forward;body=GetComponent<ReefSphereBody>();center=isRay?transform.position+transform.right*10:transform.position;skin=GetComponent<Renderer>();properties=new MaterialPropertyBlock();}
  void Start(){Initialize();}
  void Update(){Step(Mathf.Min(Time.deltaTime,.05f));}
  public void Step(float dt){Initialize();clock+=dt;
   if(isRay){
    Vector3 radial=Vector3.ProjectOnPlane(transform.position-center,Vector3.up);if(radial.sqrMagnitude<1)radial=-transform.right*10;
    Vector3 goal=center+Quaternion.AngleAxis(22,Vector3.up)*radial.normalized*10;goal.y=center.y+Mathf.Sin(clock*.12f)*.25f;
    Vector3 desired=(goal-transform.position).normalized;
    desired=navigation.Direction(transform,body,desired,.42f,dt);
    desired.y=Mathf.Clamp(desired.y,-.3f,.3f);if(transform.position.y>4.8f)desired.y=Mathf.Min(desired.y,-.1f);
    Vector3 forward=Vector3.RotateTowards(heading,desired.normalized,(navigation.Avoiding?24:10)*Mathf.Deg2Rad*dt,0);float turn=Vector3.SignedAngle(heading,forward,Vector3.up)/Mathf.Max(dt,.001f);heading=forward;bank=Mathf.SmoothDampAngle(bank,Mathf.Clamp(-turn*.65f,-9,9),ref bankVelocity,.85f,10,dt);
    transform.rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);
    if(body)navigation.RecordMovement(body.MoveSliding(forward*.42f*dt).magnitude,.42f*dt,dt);
    if(skin){skin.GetPropertyBlock(properties);properties.SetFloat("_StrokePhase",clock*2.05f+phase);properties.SetFloat("_StrokeStrength",.9f);skin.SetPropertyBlock(properties);}return;
   }
   float t=clock*speed+phase;Vector3 p=center+new Vector3(Mathf.Sin(t)*radius,Mathf.Sin(t*1.8f)*.25f,Mathf.Cos(t)*radius*.45f);Vector3 delta=p-transform.position;if(delta.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),dt*2);if(body)body.Move(delta*Mathf.Clamp01(dt*1.5f));
  }
 }
}
