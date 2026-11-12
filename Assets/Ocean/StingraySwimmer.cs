using UnityEngine;
namespace Underwater {
public sealed class StingraySwimmer:MonoBehaviour {
 public float speed=.28f,radius=2.5f,phase;
 ReefSphereBody body;Renderer skin;MaterialPropertyBlock block;Vector3 center;float clock;bool ready;
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();center=transform.position+transform.right*radius;}
 void Start(){Initialize();}
 void Update(){if(Application.platform==RuntimePlatform.Android&&Diver.Instance&&(transform.position-Diver.Instance.transform.position).sqrMagnitude>48*48)return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public void Step(float dt){
  Initialize();clock+=dt;
  var radial=Vector3.ProjectOnPlane(transform.position-center,Vector3.up);if(radial.sqrMagnitude<.01f)radial=-transform.right;
  var target=center+Quaternion.AngleAxis(32,Vector3.up)*radial.normalized*radius;target.y=center.y+Mathf.Sin(clock*.35f+phase)*.16f;
  var desired=(target-transform.position).normalized;
  if(body&&Physics.SphereCast(transform.position,body.radius+.08f,desired,out var hit,1.8f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))desired=(desired+hit.normal*3+Vector3.up*.15f).normalized;
  desired.y=Mathf.Clamp(desired.y,-.12f,.12f);
  var forward=Vector3.RotateTowards(transform.forward,desired,18*Mathf.Deg2Rad*dt,0);transform.rotation=Quaternion.LookRotation(forward,Vector3.up);
  if(body)body.Move(forward*speed*dt);
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_StrokePhase",clock*1.8f+phase);block.SetFloat("_StrokeStrength",.7f+.3f*Mathf.Sin(clock*.27f+phase));skin.SetPropertyBlock(block);}
 }
}
}
