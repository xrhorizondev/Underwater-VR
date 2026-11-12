using UnityEngine;
namespace Underwater {
// Trigger-only hand volume. ReefSphereBody remains responsible for solid reefs.
public sealed class FishTouchBody:MonoBehaviour {
 public CapsuleCollider touchCollider;
 public Bounds bodyBounds;
 public float bellySupportLocal;
 SeaLife interaction;ReefSphereBody body;float nextCheck,nextTouch,contactReach;bool touching;Vector3 contactVelocity;
 public float PalmLift=>(bellySupportLocal>0?bellySupportLocal:-bodyBounds.min.y)*Mathf.Abs(transform.lossyScale.y)+.002f;
 void Start(){interaction=GetComponent<SeaLife>();body=GetComponent<ReefSphereBody>();var scale=transform.lossyScale;contactReach=bodyBounds.extents.magnitude*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))+.10f;nextCheck=Time.time+(Mathf.Abs(transform.position.x*.31f+transform.position.z*.17f)%1)*.25f;}
 void Update(){
  if(!touchCollider||Time.time<nextCheck)return;nextCheck=Time.time+.25f;
  if(interaction&&interaction.IsHeld){touchCollider.enabled=false;return;}
  if(!Diver.Instance)return;
  float distance=(transform.position-Diver.Instance.transform.position).sqrMagnitude;
  touchCollider.enabled=distance<(touchCollider.enabled?64:36);
 }
 public float PalmDistance(Vector3 palm){
  return PointDistance(palm,.035f);
 }
 float PointDistance(Vector3 point,float handRadius){
  if(!touchCollider||!touchCollider.enabled)return float.PositiveInfinity;
  return Mathf.Max(0,Vector3.Distance(point,touchCollider.ClosestPoint(point))-handRadius);
 }
 public void Touch(Vector3 palm,float now){
  if(now<nextTouch)return;nextTouch=now+.7f;
  var away=transform.position-palm;if(away.sqrMagnitude<.00001f)away=-transform.forward;away.y=Mathf.Clamp(away.y,-.2f,.2f);
  contactVelocity=away.normalized*.4f;
 }
 public void StepContact(float dt){if(!body)body=GetComponent<ReefSphereBody>();if(contactVelocity.sqrMagnitude<.00001f)return;if(body)body.MoveSliding(contactVelocity*dt);contactVelocity*=Mathf.Exp(-5*dt);}
 void LateUpdate(){
  var diver=Diver.Instance;if(!diver||diver.MenuOpen||!touchCollider||!touchCollider.enabled)return;
  if(interaction&&interaction.IsHeld){contactVelocity=Vector3.zero;touching=false;return;}
  float reach=contactReach+.14f;
  var left=diver.LeftPalm;var right=diver.RightPalm;
  float dl=float.PositiveInfinity,dr=float.PositiveInfinity;
  if((left-transform.position).sqrMagnitude<reach*reach){dl=PalmDistance(left);float tip=PointDistance(diver.LeftFingertip,.012f);if(tip<dl){dl=tip;left=diver.LeftFingertip;}}
  if((right-transform.position).sqrMagnitude<reach*reach){dr=PalmDistance(right);float tip=PointDistance(diver.RightFingertip,.012f);if(tip<dr){dr=tip;right=diver.RightFingertip;}}
  float distance=Mathf.Min(dl,dr);if(distance>.06f)touching=false;
  if(distance<=.012f&&!touching){touching=true;if(!(interaction&&interaction.grabbable&&diver.GrabRequested))Touch(dl<dr?left:right,Time.time);}
  StepContact(Mathf.Min(Time.deltaTime,.05f));
 }
}
}
