using UnityEngine;
namespace Underwater {
[RequireComponent(typeof(CharacterController))]
public class DiverCollisionMotor:MonoBehaviour {
 CharacterController controller;Transform head;Vector3 previousLocal;
 public void Initialize(){controller=GetComponent<CharacterController>();head=transform.Find("Head");controller.radius=.28f;controller.skinWidth=.025f;controller.stepOffset=0;controller.minMoveDistance=0;controller.slopeLimit=90;Fit();previousLocal=head.localPosition;}
 void Fit(){float height=Mathf.Clamp(head.localPosition.y+.18f,.65f,2.4f);controller.height=height;controller.center=new Vector3(head.localPosition.x,height*.5f,head.localPosition.z);}
 public void TrackHead(){
  if(!controller)Initialize();
  Vector3 tracking=head.localPosition-previousLocal;if(tracking.sqrMagnitude<.000025f)return;tracking.y=0;
  // Sweep the old capsule across room-scale motion before moving its local center.
  Vector3 start=transform.position;controller.Move(transform.TransformVector(tracking));Vector3 permitted=transform.position-start;
  controller.enabled=false;transform.position=start+permitted-transform.TransformVector(tracking);
  float vertical=head.localPosition.y-previousLocal.y;
  if(Mathf.Abs(vertical)>.00001f){Vector3 direction=Vector3.up*Mathf.Sign(vertical);Vector3 from=head.position-Vector3.up*vertical;if(Physics.SphereCast(from,.2f,direction,out var hit,Mathf.Abs(vertical)+.025f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))transform.position-=direction*(Mathf.Abs(vertical)-Mathf.Max(0,hit.distance-.025f));}
  Fit();controller.enabled=true;
  controller.Move(Vector3.zero);previousLocal=head.localPosition;
 }
 public Vector3 Move(Vector3 delta){if(!controller)Initialize();Vector3 start=transform.position;controller.Move(delta);return transform.position-start;}
 public bool TryTeleport(Vector3 feet){
  if(!controller)Initialize();Fit();
  Vector3 offset=transform.TransformVector(new Vector3(head.localPosition.x,0,head.localPosition.z));
  Vector3 root=feet-offset;Vector3 center=root+transform.TransformVector(controller.center);float extent=controller.height*.5f-controller.radius;
  if(Physics.CheckCapsule(center+Vector3.up*extent,center-Vector3.up*extent,controller.radius+.03f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))return false;
  controller.enabled=false;transform.position=root;controller.enabled=true;previousLocal=head.localPosition;return true;
 }
}
}


