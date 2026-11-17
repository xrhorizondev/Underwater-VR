using UnityEngine;
namespace Underwater {
// Match the existing Android school range. Never disable colliders or renderers.
public static class ReefSimulationRange {
 public static bool Contains(Vector3 actor,Vector3 viewer,float radius)=> (actor-viewer).sqrMagnitude<=radius*radius;
 public static bool Active(Transform actor,float radius=48){if(Application.platform!=RuntimePlatform.Android)return true;var d=Diver.Instance;return !d||!d.Head||Contains(actor.position,d.Head.position,radius);}
}
}
