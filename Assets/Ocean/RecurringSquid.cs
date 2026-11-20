using UnityEngine;
namespace Underwater {
// One animal, encountered again after travelling through the intervening habitat.
public sealed class RecurringSquid:MonoBehaviour {
 public Vector3 trenchPosition,openWaterPosition;
 public Quaternion trenchRotation=Quaternion.identity,openWaterRotation=Quaternion.identity;
 public int Habitat {get;private set;}=4;
 void Update(){var diver=Diver.Instance;if(diver&&!diver.MenuOpen&&(diver.Zone==4||diver.Zone==6)&&diver.Zone!=Habitat)EnterHabitat(diver.Zone,diver.Head.position);}
 public bool EnterHabitat(int zone,Vector3 viewer){
  if(zone!=4&&zone!=6)return false;
  if(zone==Habitat)return true;
  // Never relocate an animal that is still in the diver's immediate view range.
  if(Vector3.Distance(viewer,transform.position)<45)return false;
  GetComponent<SquidSwimmer>().ResetPatrol(zone==4?trenchPosition:openWaterPosition,zone==4?trenchRotation:openWaterRotation,3,zone==4);
  Habitat=zone;return true;
 }
}
}
