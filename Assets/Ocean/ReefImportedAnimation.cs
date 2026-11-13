using UnityEngine;
namespace Underwater {
public sealed class ReefImportedAnimation:MonoBehaviour {
 public Transform tailWarning,tailBone;public float speed=.72f;
 void Start(){var animation=GetComponent<Animation>();if(animation&&animation.clip){animation[animation.clip.name].speed=speed;animation.wrapMode=WrapMode.Loop;animation.Play();}}
 void LateUpdate(){if(tailWarning&&tailBone)tailWarning.position=tailBone.position;}
}
}
