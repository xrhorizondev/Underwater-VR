using UnityEngine;
namespace Underwater {
public sealed class CreatureContactSurface:MonoBehaviour {
 public Collider[] surfaces;
 public float Distance(Vector3 point){float result=float.MaxValue;if(surfaces!=null)foreach(var c in surfaces)if(c&&c.enabled)result=Mathf.Min(result,Vector3.Distance(point,c.ClosestPoint(point)));return result;}
}
}
