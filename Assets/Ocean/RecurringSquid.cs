using UnityEngine;
namespace Underwater {
// Legacy habitat preview data used by the editor setup and validation tools.
// Gameplay leaves the squid in its authored trench patrol. Do not relocate it
// when the diver changes zones: the open-water destination is beside the shark
// and can be in view even when the original trench position is far away.
public sealed class RecurringSquid:MonoBehaviour {
 public Vector3 trenchPosition,openWaterPosition;
 public Quaternion trenchRotation=Quaternion.identity,openWaterRotation=Quaternion.identity;
 public int Habitat {get;private set;}=4;
 public bool EnterHabitat(int zone,Vector3 viewer){
  // Explicit editor previews only; never move a live animal between habitats.
  if(Application.isPlaying)return false;
  if(zone!=4&&zone!=6)return false;
  if(zone==Habitat)return true;
  // Never relocate an animal that is still in the diver's immediate view range.
  if(Vector3.Distance(viewer,transform.position)<45)return false;
  GetComponent<SquidSwimmer>().ResetPatrol(zone==4?trenchPosition:openWaterPosition,zone==4?trenchRotation:openWaterRotation,3,zone==4);
  Habitat=zone;return true;
 }
}
}
