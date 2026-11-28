using UnityEngine;
namespace Underwater {
// Small fauna keep their budgeted range; visible large swimmers run beyond the far plane.
public static class ReefSimulationRange {
 public static bool Contains(Vector3 actor,Vector3 viewer,float radius)=> (actor-viewer).sqrMagnitude<=radius*radius;
 public static float Radius(Transform actor,float radius=48){return radius>=72||actor.GetComponent<ReefLargeAnimalPatrol>()||actor.GetComponentInParent<SquidSwimmer>()?200:radius;}
 public static bool Active(Transform actor,float radius=48){if(Application.platform!=RuntimePlatform.Android)return true;var d=Diver.Instance;return !d||!d.Head||Contains(actor.position,d.Head.position,Radius(actor,radius));}
}
}
