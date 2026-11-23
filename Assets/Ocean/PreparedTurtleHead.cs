using UnityEngine;
namespace Underwater {
// Translate the existing TurtleSwimmer shader control into imported bone motion.
public sealed class PreparedTurtleHead : MonoBehaviour {
 public Transform head,actor;
 public Renderer control;
 MaterialPropertyBlock block;
 Quaternion previousBase,previousResult;bool applied;
 float yaw;
 public float CurrentYaw=>yaw;
 public void StepLook(float dt,float target){yaw=Mathf.Lerp(yaw,Mathf.Clamp(target,-.12f,.12f),1-Mathf.Exp(-Mathf.Max(0,dt)*4));ApplyYaw(yaw);}
 void LateUpdate(){if(!control)return;if(block==null)block=new MaterialPropertyBlock();control.GetPropertyBlock(block);StepLook(Mathf.Min(Time.deltaTime,.05f),block.GetFloat("_HeadTurn"));}
 public void Apply(){
  if(!control)return;if(block==null)block=new MaterialPropertyBlock();control.GetPropertyBlock(block);ApplyYaw(Mathf.Clamp(block.GetFloat("_HeadTurn"),-.12f,.12f));
 }
 void ApplyYaw(float turn){
  if(!head||!actor||!control)return;
  // A culled animation may leave last frame's pose in place; do not accumulate yaw.
  if(applied&&Quaternion.Angle(head.localRotation,previousResult)<.001f)head.localRotation=previousBase;
  previousBase=head.localRotation;
  if(block==null)block=new MaterialPropertyBlock();control.GetPropertyBlock(block);
  head.rotation=Quaternion.AngleAxis(turn*Mathf.Rad2Deg,actor.up)*head.rotation;
  previousResult=head.localRotation;applied=true;
 }
}
}
