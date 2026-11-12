using UnityEngine;

namespace Underwater {
// Matches the authored seabed footprint. Solid walls handle continuous movement;
// the volume also rejects teleports that would jump over a wall.
public sealed class ReefDiveBounds : MonoBehaviour {
 public Vector3 minimum = new Vector3(-44, -80, -16);
 public Vector3 maximum = new Vector3(44, 6.5f, 248);
 public bool Contains(Vector3 head, float clearance) =>
  head.x >= minimum.x + clearance && head.x <= maximum.x - clearance &&
  head.z >= minimum.z + clearance && head.z <= maximum.z - clearance &&
  head.y >= minimum.y + clearance && head.y <= maximum.y;
}
}
