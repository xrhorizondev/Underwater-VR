using UnityEngine;
namespace Underwater {
 public class MarineSwimmer:MonoBehaviour {
  ReefSphereBody body;Renderer skin;MaterialPropertyBlock properties;float clock,bank,bankVelocity;Vector3 heading;bool initialized;
  readonly ReefRayNavigation navigation=new ReefRayNavigation();
  readonly ReefSwimHeading course=new ReefSwimHeading();ReefAnimalClearance clearance;ReefLargeAnimalPatrol patrol;
  public Vector3 center;public float radius=3,speed=.12f,phase;public Transform[] fins;public bool isRay;public bool encounterRoll;
  public void Initialize(){if(initialized)return;initialized=true;heading=transform.forward;body=GetComponent<ReefSphereBody>();clearance=GetComponent<ReefAnimalClearance>();patrol=GetComponent<ReefLargeAnimalPatrol>();center=isRay?transform.position+transform.right*10:transform.position;skin=GetComponent<Renderer>();properties=new MaterialPropertyBlock();}
  void Start(){Initialize();}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen||!ReefSimulationRange.Active(transform))return;Step(Mathf.Min(Time.deltaTime,.05f));}
  public void Step(float dt){Initialize();clock+=dt;if(patrol){patrol.Step(dt);return;}
   if(isRay){
    Vector3 radial=Vector3.ProjectOnPlane(transform.position-center,Vector3.up);if(radial.sqrMagnitude<1)radial=-transform.right*10;
    Vector3 desired=Vector3.Cross(Vector3.up,radial.normalized)-radial.normalized*Mathf.Clamp((radial.magnitude-10)/10,-.5f,.5f);
    float height=center.y+Mathf.Sin(clock*.10f)*.12f;
    if(clearance)for(int station=1;station<=4;station++)if(clearance.RequiredHeight(transform.position+transform.forward*station,transform.rotation,out float floor))height=Mathf.Max(height,floor+.25f);
    desired.y=Mathf.Clamp((height-transform.position.y)*.35f,-.10f,.16f);
    desired=navigation.Direction(transform,body,desired,.42f,dt);
    desired.y=Mathf.Clamp(desired.y,-.12f,.18f);if(transform.position.y>4.8f)desired.y=Mathf.Min(desired.y,-.1f);
    Vector3 forward=course.Step(transform.forward,desired,dt,navigation.Avoiding?16:8,7,3,12);float turn=Vector3.SignedAngle(transform.forward,forward,Vector3.up)/Mathf.Max(dt,.001f);bank=Mathf.SmoothDampAngle(bank,Mathf.Clamp(-turn*.3f,-3,3),ref bankVelocity,1.2f,4,dt);
    var rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);
    if(!clearance||!clearance.RequiredHeight(transform.position,rotation,out float required)||required<=transform.position.y+.0001f)transform.rotation=rotation;
    heading=transform.forward;
    if(body){var delta=heading*.42f*dt;
     if(clearance){if(clearance.RequiredHeight(transform.position+delta,transform.rotation,out float floor)&&floor>transform.position.y+delta.y)delta.y=Mathf.Min(.14f*dt,Mathf.Max(delta.y,floor-transform.position.y));
      for(int attempt=0;attempt<12;attempt++){var next=transform.position+delta;if(clearance.RequiredHeight(next,transform.rotation,out float floorHeight)&&floorHeight<=next.y+.0001f)break;delta.x*=.5f;delta.z*=.5f;}}
     // Sliding on a sloped rock normal can add an unplanned vertical impulse.
     // The ray instead slows at contact while navigation finds a clear arc.
     navigation.RecordMovement(body.Move(delta).magnitude,.42f*dt,dt);}
    if(skin){skin.GetPropertyBlock(properties);properties.SetFloat("_StrokePhase",clock*2.05f+phase);properties.SetFloat("_StrokeStrength",.9f);skin.SetPropertyBlock(properties);}return;
   }
   float t=clock*speed+phase;Vector3 p=center+new Vector3(Mathf.Sin(t)*radius,Mathf.Sin(t*1.8f)*.25f,Mathf.Cos(t)*radius*.45f);Vector3 offset=p-transform.position;if(offset.sqrMagnitude>.00001f)transform.rotation=Quaternion.LookRotation(course.Step(transform.forward,offset,dt,18,10,5,20));if(body)body.Move(Vector3.ClampMagnitude(offset,Mathf.Max(.12f,speed))*dt);
  }
 }
}
