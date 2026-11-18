using UnityEngine;
namespace Underwater {
// Trigger-only hand volume. ReefSphereBody remains responsible for solid reefs.
public sealed class FishTouchBody:MonoBehaviour {
 public CapsuleCollider touchCollider;
 public Bounds bodyBounds;
 public float bellySupportLocal;
 SeaLife interaction;ReefSphereBody body;float nextCheck,nextTouch,contactReach;bool touching;Vector3 contactVelocity;
 // Seat the soft belly slightly into the cupped glove so fins do not leave a visible air gap.
 public float PalmLift=>Mathf.Max(0,(bellySupportLocal>0?bellySupportLocal:-bodyBounds.min.y)*Mathf.Abs(transform.lossyScale.y)-.010f);
 void Start(){interaction=GetComponent<SeaLife>();body=GetComponent<ReefSphereBody>();var scale=transform.lossyScale;contactReach=bodyBounds.extents.magnitude*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))+.10f;nextCheck=Time.time+(Mathf.Abs(transform.position.x*.31f+transform.position.z*.17f)%1)*.25f;}
 void Update(){
  if(!touchCollider||Time.time<nextCheck)return;nextCheck=Time.time+.25f;
  if(interaction&&interaction.IsHeld){touchCollider.enabled=false;return;}
  if(!Diver.Instance)return;
  float distance=(transform.position-Diver.Instance.transform.position).sqrMagnitude;
  touchCollider.enabled=distance<(touchCollider.enabled?64:36);
 }
 public float PalmDistance(Vector3 palm){
  if(!touchCollider||!touchCollider.enabled)return float.PositiveInfinity;
  var life=interaction?interaction:GetComponent<SeaLife>();
  if(life&&life.grabbable){if(InsideGrip(palm))return 0;return Mathf.Max(0,Vector3.Distance(palm,ProjectGripSurface(palm,0))-.035f);}
  return PointDistance(palm,.035f);
 }
 // Central flesh volume excludes the tail and tall, thin dorsal fins.
 public Vector3 GripCenter=>bodyBounds.center+Vector3.forward*bodyBounds.size.z*.08f;
 public Vector3 GripRadii=>new Vector3(Mathf.Max(.018f,bodyBounds.extents.x*.98f),Mathf.Max(.018f,bodyBounds.extents.y*.78f),Mathf.Max(.025f,bodyBounds.extents.z*.66f));
 public Vector3 GrabSurface(Vector3 palm){return ProjectGripSurface(palm,.006f);}
 public Vector3 ProjectGripSurface(Vector3 point,float padding){
  var radii=GripRadii;var local=transform.InverseTransformPoint(point)-GripCenter;
  var unit=new Vector3(local.x/radii.x,local.y/radii.y,local.z/radii.z);if(unit.sqrMagnitude<.000001f)unit=Vector3.up;
  var surface=GripCenter+Vector3.Scale(unit.normalized,radii);var world=transform.TransformPoint(surface);
  var normal=transform.TransformDirection(new Vector3(unit.x/radii.x,unit.y/radii.y,unit.z/radii.z)).normalized;
  return world+normal*padding;
 }
 public bool InsideGrip(Vector3 world,float padding=0){
  var scale=transform.lossyScale;var p=transform.InverseTransformPoint(world)-GripCenter;var r=GripRadii+new Vector3(padding/Mathf.Max(.001f,Mathf.Abs(scale.x)),padding/Mathf.Max(.001f,Mathf.Abs(scale.y)),padding/Mathf.Max(.001f,Mathf.Abs(scale.z)));
  return p.x*p.x/(r.x*r.x)+p.y*p.y/(r.y*r.y)+p.z*p.z/(r.z*r.z)<1;
 }
 public Vector3 PalmSupport(Vector3 outward){
  var n=transform.InverseTransformDirection(-outward).normalized;var r=GripRadii;var q=Vector3.Scale(r,n);float length=Mathf.Max(.00001f,q.magnitude);
  return transform.TransformPoint(GripCenter+Vector3.Scale(r,q)/length);
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
