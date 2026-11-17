using UnityEngine;
namespace Underwater {
// Android build-only geometry levels. The renderer, material blocks and colliders stay intact.
public sealed class ReefFishDetail:MonoBehaviour {
 public Mesh full,medium,distant;public float closeDistance=2.2f;MeshFilter filter;SeaLife life;float nextCheck;
 public void Select(Vector3 viewer,bool held=false){if(!filter){filter=GetComponent<MeshFilter>();life=GetComponent<SeaLife>();}float sq=(viewer-transform.position).sqrMagnitude;var target=held||sq<closeDistance*closeDistance?full:sq<closeDistance*closeDistance*9?medium:distant;if(filter.sharedMesh!=target)filter.sharedMesh=target;}
 void Update(){var d=Diver.Instance;if(!d||!d.Head)return;bool held=life&&life.IsHeld;if(!held&&Time.time<nextCheck)return;nextCheck=Time.time+.18f;Select(d.Head.position,held);}
}
}
