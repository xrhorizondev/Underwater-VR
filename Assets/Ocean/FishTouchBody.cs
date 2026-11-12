using UnityEngine;
namespace Underwater {
// Trigger-only hand volume. ReefSphereBody remains responsible for solid reefs.
public sealed class FishTouchBody:MonoBehaviour {
 public CapsuleCollider touchCollider;
 public Bounds bodyBounds;
 SeaLife interaction;float nextCheck;
 public float PalmLift=>bodyBounds.extents.y*Mathf.Abs(transform.lossyScale.y)+.012f;
 void Start(){interaction=GetComponent<SeaLife>();nextCheck=Time.time+(Mathf.Abs(transform.position.x*.31f+transform.position.z*.17f)%1)*.25f;}
 void Update(){
  if(!touchCollider||Time.time<nextCheck)return;nextCheck=Time.time+.25f;
  if(interaction&&interaction.IsHeld){touchCollider.enabled=false;return;}
  if(!Diver.Instance)return;
  float distance=(transform.position-Diver.Instance.transform.position).sqrMagnitude;
  touchCollider.enabled=distance<(touchCollider.enabled?64:36);
 }
 public float PalmDistance(Vector3 palm){
  if(!touchCollider||!touchCollider.enabled)return float.PositiveInfinity;
  return Mathf.Max(0,Vector3.Distance(palm,touchCollider.ClosestPoint(palm))-.035f);
 }
}
}
