using UnityEngine;
namespace Underwater {
public sealed class StingraySwimmer:MonoBehaviour {
 public float speed=.28f,radius=2.5f,phase;
 ReefSphereBody body;Renderer skin;MaterialPropertyBlock block;Vector3 center;float clock,bank,strokePhase,effort;bool ready;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 readonly ReefSwimHeading course=new ReefSwimHeading();
 public float TouchResponse {get;private set;}
 public bool SafeBodyTouch(Vector3 world){var p=transform.InverseTransformPoint(world);float depth=p.z>=0?.64f:.54f;return p.x*p.x/(.96f*.96f)+p.z*p.z/(depth*depth)<=1&&Mathf.Abs(p.y-.04f)<.28f;}
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();center=transform.position+transform.right*radius;}
 void Start(){Initialize();}
 void Update(){var d=Diver.Instance;if(d&&d.MenuOpen)return;if(Application.platform==RuntimePlatform.Android&&d&&(transform.position-d.transform.position).sqrMagnitude>48*48)return;Step(Mathf.Min(Time.deltaTime,.05f),d&&(SafeBodyTouch(d.LeftFingertip)||SafeBodyTouch(d.RightFingertip)));}
 public void Step(float dt)=>Step(dt,false);
 public void Step(float dt,bool safeTouch){
  Initialize();clock+=dt;TouchResponse=Mathf.MoveTowards(TouchResponse,safeTouch?1:0,dt*(safeTouch?2:.65f));
  var radial=Vector3.ProjectOnPlane(transform.position-center,Vector3.up);if(radial.sqrMagnitude<.01f)radial=-transform.right;
  var target=center+Quaternion.AngleAxis(32,Vector3.up)*radial.normalized*radius;target.y=center.y+Mathf.Sin(clock*.35f+phase)*.16f;
  var desired=(target-transform.position).normalized;
  desired=navigation.Direction(transform,body,desired,speed,dt);desired.y=Mathf.Clamp(desired.y,-.3f,.3f);
  var forward=course.Step(transform.forward,desired,dt,navigation.Avoiding?22:14,9,5,20);float turn=Vector3.SignedAngle(transform.forward,forward,Vector3.up)/Mathf.Max(dt,.001f);bank=Mathf.MoveTowards(bank,Mathf.Clamp(-turn*.3f,-6,6),5*dt);ReefSwimHeading.TryRotate(transform,body,Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward));forward=transform.forward;
  float actual=0;if(body){float moved=body.MoveSliding(forward*speed*dt).magnitude;actual=moved/Mathf.Max(dt,.001f);navigation.RecordMovement(moved,speed*dt,dt);}effort=Mathf.Lerp(effort,Mathf.Clamp01(actual/Mathf.Max(.01f,speed)),1-Mathf.Exp(-dt*4));strokePhase+=dt*2.3f*Mathf.Lerp(.15f,1,effort);
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_StrokePhase",strokePhase+phase);block.SetFloat("_StrokeStrength",Mathf.Lerp(.4f,.9f,effort)+.06f*Mathf.Sin(clock*.27f+phase)+TouchResponse*.18f);skin.SetPropertyBlock(block);}
 }
}
}
