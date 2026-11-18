using System.Linq;
using UnityEngine;
namespace Underwater {
// Bone animation only; WreckCreature/ReefSphereBody retain world movement.
public sealed class YellowMorayMotion:MonoBehaviour {
 public WreckCreature actor;public Animation animationPlayer;public bool freeSwim;
 public string CurrentState {get;private set;}="Idle";
 Vector3 previous;bool ready,wasReacting;float reactionClock=100,stateClock;
 System.Collections.Generic.Dictionary<string,AnimationClip> clips;
 Transform[] poseBones;Vector3[] blendPositions,blendScales;Quaternion[] blendRotations;float blendClock=1;Vector3[] restPositions,restScales;Quaternion[] restRotations;
 void Start(){Initialize();}
 void Initialize(){if(ready)return;ready=true;if(!animationPlayer)animationPlayer=GetComponentInChildren<Animation>();animationPlayer.enabled=false;animationPlayer.playAutomatically=false;previous=actor?actor.transform.position:transform.position;poseBones=animationPlayer.GetComponentsInChildren<Transform>();blendPositions=new Vector3[poseBones.Length];blendScales=new Vector3[poseBones.Length];blendRotations=new Quaternion[poseBones.Length];restPositions=poseBones.Select(b=>b.localPosition).ToArray();restScales=poseBones.Select(b=>b.localScale).ToArray();restRotations=poseBones.Select(b=>b.localRotation).ToArray();}
 void LateUpdate(){var diver=Diver.Instance;if(diver&&diver.MenuOpen)return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public AnimationClip Clip(string state){if(clips==null){if(!animationPlayer)animationPlayer=GetComponentInChildren<Animation>();clips=animationPlayer.Cast<AnimationState>().Select(s=>s.clip).ToDictionary(c=>c.name.Split('|').Last(),c=>c);}return clips[state];}
 public void Step(float dt){
  Initialize();reactionClock+=dt;stateClock+=dt;string state="Idle";
  if(actor){bool reacting=actor.Reacting;if(reacting&&!wasReacting)reactionClock=0;wasReacting=reacting;
   float velocity=Vector3.Dot(actor.transform.position-previous,actor.transform.forward)/Mathf.Max(dt,.001f);previous=actor.transform.position;
   state=reactionClock<.7f?"React":velocity<-.01f?"Retreat":velocity>.01f?"Recover":"Idle";
  }else if(freeSwim)state="Swim";
  var clip=Clip(state);bool changed=state!=CurrentState;CurrentState=state;
  if(changed){stateClock=0;blendClock=0;for(int i=0;i<poseBones.Length;i++){blendPositions[i]=poseBones[i].localPosition;blendRotations[i]=poseBones[i].localRotation;blendScales[i]=poseBones[i].localScale;}}
  float time=stateClock;time=state=="React"||state=="Recover"?Mathf.Min(time,clip.length):time%clip.length;
  // One explicit clock/sample path in play mode and validation. Renderer
  // culling or the legacy component's autoplay cannot freeze the idle pose.
  clip.SampleAnimation(animationPlayer.gameObject,time);blendClock+=dt;
  // Rotation-only deformation: preserve imported bone lengths and mesh scale.
  for(int i=0;i<poseBones.Length;i++){var bone=poseBones[i];bone.localPosition=restPositions[i];bone.localScale=restScales[i];float limit=bone.name=="Jaw"?24:bone.name=="Head"?1.5f:7;var calm=Quaternion.Slerp(restRotations[i],bone.localRotation,1f);bone.localRotation=Quaternion.RotateTowards(restRotations[i],calm,limit);}
  if(blendClock<.4f){float mix=Mathf.SmoothStep(0,1,blendClock/.4f);for(int i=0;i<poseBones.Length;i++){var bone=poseBones[i];bone.localPosition=Vector3.Lerp(blendPositions[i],bone.localPosition,mix);bone.localRotation=Quaternion.Slerp(blendRotations[i],bone.localRotation,mix);bone.localScale=Vector3.Lerp(blendScales[i],bone.localScale,mix);}}
 }
}
}


