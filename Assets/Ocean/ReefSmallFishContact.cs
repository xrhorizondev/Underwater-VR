using UnityEngine;
namespace Underwater {
// A held or passing small fish must never move the tracked diver capsule.
// Trigger volumes still serve hands; larger animal colliders stay physical.
[DefaultExecutionOrder(100)]
public sealed class ReefSmallFishContact:MonoBehaviour {
 public Collider[] smallFishSurfaces;
 public void Apply(CharacterController diver){if(!diver||smallFishSurfaces==null)return;foreach(var surface in smallFishSurfaces)if(surface)Physics.IgnoreCollision(diver,surface,true);}
 void Start(){var diver=Diver.Instance;if(diver)Apply(diver.GetComponent<CharacterController>());}
}
}
