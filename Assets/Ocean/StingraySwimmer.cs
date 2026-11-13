using UnityEngine;
namespace Underwater {
public sealed class StingraySwimmer:MonoBehaviour {
 public float speed=.28f,radius=2.5f,phase;
 ReefSphereBody body;Renderer skin;MaterialPropertyBlock block;Vector3 center;float clock,bank;bool ready;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();center=transform.position+transform.right*radius;}
 void Start(){Initialize();}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen)return;if(Application.platform==RuntimePlatform.Android&&Diver.Instance&&(transform.position-Diver.Instance.transform.position).sqrMagnitude>48*48)return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public void Step(float dt){
  Initialize();clock+=dt;
  var radial=Vector3.ProjectOnPlane(transform.position-center,Vector3.up);if(radial.sqrMagnitude<.01f)radial=-transform.right;
  var target=center+Quaternion.AngleAxis(32,Vector3.up)*radial.normalized*radius;target.y=center.y+Mathf.Sin(clock*.35f+phase)*.16f;
  var desired=(target-transform.position).normalized;
  desired=navigation.Direction(transform,body,desired,speed,dt);desired.y=Mathf.Clamp(desired.y,-.3f,.3f);
  var forward=Vector3.RotateTowards(transform.forward,desired,(navigation.Avoiding?28:18)*Mathf.Deg2Rad*dt,0);float turn=Vector3.SignedAngle(transform.forward,forward,Vector3.up)/Mathf.Max(dt,.001f);bank=Mathf.MoveTowards(bank,Mathf.Clamp(-turn*.4f,-10,10),12*dt);transform.rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);
  if(body)navigation.RecordMovement(body.MoveSliding(forward*speed*dt).magnitude,speed*dt,dt);
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_StrokePhase",clock*2.3f+phase);block.SetFloat("_StrokeStrength",.9f+.1f*Mathf.Sin(clock*.27f+phase));skin.SetPropertyBlock(block);}
 }
}
}
