using UnityEngine;
namespace Underwater {
public sealed class ReefImportedAnimation:MonoBehaviour {
 public Transform tailWarning,tailBone;public float speed=.72f;
 [Range(0,1)]public float cycleOffset;
 void Start(){var animation=GetComponent<Animation>();if(animation&&animation.clip){var state=animation[animation.clip.name];state.speed=speed;animation.wrapMode=WrapMode.Loop;
 // Keep the compact bone clocks running between habitats. Renderer-based
 // culling can leave a legacy clip frozen at its initial sampled pose.
 animation.cullingType=AnimationCullingType.AlwaysAnimate;animation.Play();state.normalizedTime=cycleOffset;}if(cycleOffset!=0){var block=new MaterialPropertyBlock();foreach(var renderer in GetComponentsInChildren<Renderer>()){renderer.GetPropertyBlock(block);block.SetFloat("_CombPhase",cycleOffset*Mathf.PI*2);renderer.SetPropertyBlock(block);}}}
 void LateUpdate(){if(tailWarning&&tailBone)tailWarning.position=tailBone.position;}
}
}
