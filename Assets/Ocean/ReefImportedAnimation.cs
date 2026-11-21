using UnityEngine;
namespace Underwater {
public sealed class ReefImportedAnimation:MonoBehaviour {
 public Transform tailWarning,tailBone;public float speed=.72f;
 [Range(0,1)]public float cycleOffset;
 Animation player;float desiredRate=-1;
 // Locomotion supplies actual travel speed rather than letting a blocked
 // animal keep the same full-speed stroke.
 public void MatchTravel(float metresPerSecond,float referenceSpeed){desiredRate=Mathf.Clamp(metresPerSecond/Mathf.Max(.1f,referenceSpeed),.55f,1.6f);}
 void Start(){var animation=GetComponent<Animation>();if(animation&&animation.clip){var state=animation[animation.clip.name];state.speed=speed;animation.wrapMode=WrapMode.Loop;
 // Keep the compact bone clocks running between habitats. Renderer-based
 // culling can leave a legacy clip frozen at its initial sampled pose.
 animation.cullingType=AnimationCullingType.AlwaysAnimate;animation.Play();state.normalizedTime=cycleOffset;}if(cycleOffset!=0){var block=new MaterialPropertyBlock();foreach(var renderer in GetComponentsInChildren<Renderer>()){renderer.GetPropertyBlock(block);block.SetFloat("_CombPhase",cycleOffset*Mathf.PI*2);renderer.SetPropertyBlock(block);}}}
 void LateUpdate(){if(!player)player=GetComponent<Animation>();if(player&&player.clip){var state=player[player.clip.name];if(!player.isPlaying)player.Play(player.clip.name);if(state!=null&&desiredRate>=0)state.speed=Mathf.Lerp(state.speed,speed*desiredRate,1-Mathf.Exp(-Time.deltaTime*2.5f));}if(tailWarning&&tailBone)tailWarning.position=tailBone.position;}
}
}
