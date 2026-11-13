using UnityEngine;
namespace Underwater {
public sealed class WreckCreature:MonoBehaviour {
 public enum Species {Moray,Grouper,Dolphin}
 public Species species;public Transform jaw;public Vector3 habitatCenter;public float phase;
 WreckCreature[] pod;ReefSphereBody body;ReefCreatureInfo info;Collider touch;Renderer skin;MaterialPropertyBlock block;Vector3 home,homeForward,loopCenter;float clock,reacted=-100,cooldown;bool ready;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 public float JawAngle{get;private set;}public bool Reacting=>clock-reacted<4;
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();info=GetComponent<ReefCreatureInfo>();skin=GetComponent<Renderer>();touch=transform.Find("Animal body solid")?.GetComponent<Collider>();block=new MaterialPropertyBlock();home=transform.position;homeForward=transform.forward;loopCenter=habitatCenter;if(species==Species.Dolphin)pod=FindObjectsByType<WreckCreature>(FindObjectsSortMode.None);}
 public float HandDistance(Vector3 hand)=>touch?Vector3.Distance(hand,touch.ClosestPoint(hand)):Mathf.Max(0,Vector3.Distance(hand,transform.position)-.3f);
 void Update(){var diver=Diver.Instance;if(diver&&diver.MenuOpen)return;Initialize();Vector3? head=diver&&diver.Head?diver.Head.position:(Vector3?)null;Vector3? palm=diver?(HandDistance(diver.LeftPalm)<HandDistance(diver.RightPalm)?diver.LeftPalm:diver.RightPalm):(Vector3?)null;Step(Mathf.Min(Time.deltaTime,.05f),head,palm);}
 public void React(){Initialize();if(clock<cooldown)return;reacted=clock;cooldown=clock+9;if(info)info.Reveal();}
 public void Step(float dt,Vector3? head=null,Vector3? hand=null){Initialize();clock+=dt;
  if(hand.HasValue&&HandDistance(hand.Value)<.16f||species==Species.Moray&&head.HasValue&&Vector3.Distance(head.Value,home)<1.5f)React();
  float age=clock-reacted;
  if(species==Species.Moray){JawAngle=age<1.3f?38:7+Mathf.Sin(clock*1.8f)*3;if(jaw)jaw.localRotation=Quaternion.Slerp(jaw.localRotation,Quaternion.Euler(JawAngle,0,0),1-Mathf.Exp(-dt*7));float retreat=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.2f)/1))*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-3)/4)))*.65f;Vector3 delta=Vector3.ClampMagnitude(home-homeForward*retreat-transform.position,.55f*dt);if(body)body.MoveSliding(delta);return;}
  Vector3 goal;float speed;
  if(species==Species.Dolphin){var focus=habitatCenter;if(head.HasValue&&Vector3.Distance(head.Value,habitatCenter)<14){focus=habitatCenter+Vector3.ClampMagnitude(head.Value-habitatCenter,7);focus.y=Mathf.Clamp(head.Value.y+.35f,habitatCenter.y-1.5f,habitatCenter.y+2);}
   loopCenter=Vector3.MoveTowards(loopCenter,focus,dt*.6f);float radius=age<9?3.4f:4.5f;float angle=clock*.16f+phase;goal=loopCenter+new Vector3(Mathf.Sin(angle)*radius,Mathf.Sin(angle*1.4f)*.3f,Mathf.Cos(angle)*radius);speed=age<9?1:.7f;
   if(head.HasValue&&(transform.position-head.Value).sqrMagnitude<2.2f*2.2f)goal=transform.position+(transform.position-head.Value).normalized*4;
  }else{goal=home+new Vector3(Mathf.Sin(clock*.13f)*1.6f,Mathf.Sin(clock*.23f)*.1f,Mathf.Cos(clock*.13f)*1.2f);speed=age<4?.045f:.17f;}
  var direction=(goal-transform.position).normalized;if(direction.sqrMagnitude<.001f)direction=transform.forward;if(species==Species.Dolphin&&pod!=null){Vector3 separation=Vector3.zero;foreach(var other in pod)if(other&&other!=this&&other.species==Species.Dolphin){Vector3 away=transform.position-other.transform.position;float distance=away.magnitude;if(distance>0.01f&&distance<4)separation+=away/distance*(4-distance)*.9f;}direction=(direction+separation).normalized;}
  direction=navigation.Direction(transform,body,direction,speed,dt);
  if(species==Species.Dolphin&&pod!=null)foreach(var other in pod)if(other&&other!=this&&other.species==Species.Dolphin){Vector3 away=transform.position-other.transform.position;float gap=away.magnitude;if(gap>.01f&&gap<3.8f)direction=(direction+away/gap*(3.8f-gap)*4).normalized;}
  transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),28*dt);if(body){Vector3 delta=transform.forward*speed*dt,before=transform.position;if(species==Species.Dolphin&&pod!=null)foreach(var other in pod)if(other&&other!=this&&other.species==Species.Dolphin){Vector3 away=transform.position-other.transform.position;if(away.sqrMagnitude<3.2f*3.2f&&Vector3.Dot(delta,away)<0)delta=Vector3.ProjectOnPlane(delta,away.normalized);}
   body.MoveSliding(delta);if(species==Species.Dolphin&&pod!=null)foreach(var other in pod)if(other&&other!=this&&other.species==Species.Dolphin&&(transform.position-other.transform.position).sqrMagnitude<2.8f*2.8f)transform.position=before;
   navigation.RecordMovement(Vector3.Distance(before,transform.position),speed*dt,dt);
  }
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",clock*(species==Species.Dolphin?3.4f:4)+phase);skin.SetPropertyBlock(block);}
 }
}
}
