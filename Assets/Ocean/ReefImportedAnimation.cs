using UnityEngine;
namespace Underwater {
[DefaultExecutionOrder(-20)]
public sealed class ReefImportedAnimation:MonoBehaviour {
 public Transform tailWarning,tailBone;public float speed=.72f;
 public float minimumTravelRate=.55f;
 [Range(0,1)]public float cycleOffset;
 public bool distanceClock;public Transform travelActor;public float referenceTravelSpeed=.4f;
 public float CurrentRate{get;private set;}public float CycleTime{get;private set;}
 Vector3 previousPosition;bool ready;
 public void InitializePlayback(){if(ready)return;ready=true;player=GetComponent<Animation>();CurrentRate=speed;if(travelActor)previousPosition=travelActor.position;if(player&&player.clip){CycleTime=cycleOffset*player.clip.length;if(distanceClock){player.enabled=false;player.playAutomatically=false;}}}
 public void StepPlayback(float dt){InitializePlayback();if(!player||!player.clip||dt<=0)return;if(travelActor){var delta=travelActor.position-previousPosition;previousPosition=travelActor.position;float actual=delta.sqrMagnitude>4?0:delta.magnitude/dt;desiredRate=Mathf.Clamp(actual/Mathf.Max(.1f,referenceTravelSpeed),minimumTravelRate,1.6f);}float target=speed*(desiredRate>=0?desiredRate:1);CurrentRate=Mathf.Lerp(CurrentRate,target,1-Mathf.Exp(-dt*3.5f));CycleTime+=dt*CurrentRate;player.clip.SampleAnimation(gameObject,Mathf.Repeat(CycleTime,player.clip.length));var state=player[player.clip.name];if(state!=null){state.time=Mathf.Repeat(CycleTime,player.clip.length);state.speed=CurrentRate;}if(tailWarning&&tailBone)tailWarning.position=tailBone.position;}
 Animation player;float desiredRate=-1;
 // Locomotion supplies actual travel speed rather than letting a blocked
 // animal keep the same full-speed stroke.
 public void MatchTravel(float metresPerSecond,float referenceSpeed){desiredRate=Mathf.Clamp(metresPerSecond/Mathf.Max(.1f,referenceSpeed),minimumTravelRate,1.6f);}
 void Start(){if(distanceClock){InitializePlayback();return;}var animation=GetComponent<Animation>();if(animation&&animation.clip){var state=animation[animation.clip.name];state.speed=speed;animation.wrapMode=WrapMode.Loop;
 // Keep the compact bone clocks running between habitats. Renderer-based
 // culling can leave a legacy clip frozen at its initial sampled pose.
 animation.cullingType=AnimationCullingType.AlwaysAnimate;animation.Play();state.normalizedTime=cycleOffset;}if(cycleOffset!=0){var block=new MaterialPropertyBlock();foreach(var renderer in GetComponentsInChildren<Renderer>()){renderer.GetPropertyBlock(block);block.SetFloat("_CombPhase",cycleOffset*Mathf.PI*2);renderer.SetPropertyBlock(block);}}}
 void LateUpdate(){if(distanceClock){var diver=Diver.Instance;if(diver&&diver.MenuOpen||travelActor&&!ReefSimulationRange.Active(travelActor)){if(travelActor)previousPosition=travelActor.position;return;}StepPlayback(Mathf.Min(Time.deltaTime,.05f));return;}if(!player)player=GetComponent<Animation>();if(player&&player.clip){var state=player[player.clip.name];if(!player.isPlaying)player.Play(player.clip.name);if(state!=null&&desiredRate>=0)state.speed=Mathf.Lerp(state.speed,speed*desiredRate,1-Mathf.Exp(-Time.deltaTime*2.5f));}if(tailWarning&&tailBone)tailWarning.position=tailBone.position;}
}
}
