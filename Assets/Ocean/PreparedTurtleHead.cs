using UnityEngine;
namespace Underwater {
// Translate the existing TurtleSwimmer shader control into imported bone motion.
public sealed class PreparedTurtleHead : MonoBehaviour {
 public Transform head,actor;
 public Renderer control;
 MaterialPropertyBlock block;
 Quaternion previousBase,previousResult;bool applied;
 void LateUpdate(){Apply();}
 public void Apply(){
  if(!head||!actor||!control)return;
  // A culled animation may leave last frame's pose in place; do not accumulate yaw.
  if(applied&&Quaternion.Angle(head.localRotation,previousResult)<.001f)head.localRotation=previousBase;
  previousBase=head.localRotation;
  if(block==null)block=new MaterialPropertyBlock();control.GetPropertyBlock(block);
  head.rotation=Quaternion.AngleAxis(block.GetFloat("_HeadTurn")*Mathf.Rad2Deg,actor.up)*head.rotation;
  previousResult=head.localRotation;applied=true;
 }
}
}
