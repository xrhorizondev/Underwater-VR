using UnityEngine;
namespace Underwater {
public class SeaLife:MonoBehaviour {
 public string kind; public bool danger,consequence=true,grabbable,observationOnly,stationary;
 public bool proximityEnabled=true,cautionEnabled=true; public AudioClip cautionVoice,reactionVoice;
 bool wasGlowing,hasEncounter;ReefDangerZone dangerZone;public bool IsHeld=>hand;
 ReefSphereBody body;FishAgent swimmer;Vector3 origin,scale; float reacted=-100,heldUntil,heldAt,palmLift=.04f;Transform hand;Renderer[] renderers;Material[] materials;Color[] colors;
 void Start(){hasEncounter=GetComponent<ReefEncounter>()||GetComponent<ReefCruiser>();dangerZone=GetComponent<ReefDangerZone>();body=GetComponent<ReefSphereBody>();swimmer=GetComponent<FishAgent>();origin=transform.position;scale=transform.localScale;renderers=GetComponentsInChildren<Renderer>();materials=new Material[renderers.Length];colors=new Color[renderers.Length];for(int i=0;i<renderers.Length;i++){materials[i]=(danger&&!dangerZone)?renderers[i].material:renderers[i].sharedMaterial;colors[i]=materials[i]&&materials[i].HasProperty("_Color")?materials[i].color:Color.white;}}
 void Update(){if(Application.platform==RuntimePlatform.Android&&!hand&&Diver.Instance&&(transform.position-Diver.Instance.transform.position).sqrMagnitude>48*48)return;if(hand){StepHeld(Mathf.Min(Time.deltaTime,.05f),Time.time);return;}
 if(!hasEncounter&&!swimmer&&!stationary&&kind!="Coral"&&kind!="Fire coral"&&kind!="Ray tail"){
 var target=origin+new Vector3(Mathf.Sin(Time.time*.16f+origin.z)*1.4f,Mathf.Sin(Time.time*.5f+origin.x)*.2f,Mathf.Cos(Time.time*.16f+origin.z)*.7f);
 if(kind=="Lumen"&&Diver.Instance&&Diver.Instance.Zone>=3)target=Diver.Instance.transform.position+Diver.Instance.Head.forward*2+Vector3.up;
 if(Time.time-reacted<2)target+=Vector3.forward*1.3f;
 if(body)body.Move((target-transform.position)*Mathf.Clamp01(Time.deltaTime*.7f));else transform.position=Vector3.Lerp(transform.position,target,Time.deltaTime*.7f);}
 if(kind=="Coral")transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time+origin.x)*4);
 if(danger&&dangerZone)return;
 if(materials==null||!Diver.Instance)return;float d=Diver.Instance.DistanceToHands(transform.position);
 if(observationOnly)return;
 if(danger){bool glow=proximityEnabled&&d<.65f;if(glow!=wasGlowing){wasGlowing=glow;for(int i=0;i<materials.Length;i++){materials[i].color=glow?Color.Lerp(colors[i],Color.red,.55f):colors[i];}}
 if(cautionEnabled&&d<.48f&&Time.time-reacted>4){Diver.Instance.Say(Warning(),cautionVoice);reacted=Time.time;}
 if(consequence&&d<.3f&&Time.time-reacted>.5f){Diver.Instance.Hurt(reactionVoice);reacted=Time.time;}}
 else if(d<.55f&&Time.time-reacted>3)React();
 }
 public string Warning(){if(kind=="Lionfish")return "Whoa. Don't touch those spines.";if(kind=="Ray tail")return "Careful. Watch the tail.";if(kind=="Moon jelly")return "Careful. Give it some space.";if(kind=="Fire coral")return "Careful. Don't touch that coral.";return "Don't touch that one.";}
 public void DangerReaction(){if(kind=="Lionfish")reacted=Time.time;}
 public void React(){reacted=Time.time;if(kind=="Turtle")Diver.Instance.Say("Sea turtle — Sea turtles breathe air.",null);}
 public void Grab(Transform h){if(!grabbable||hand)return;if(scale.sqrMagnitude<.00001f)scale=transform.localScale;if(!body)body=GetComponent<ReefSphereBody>();var touch=GetComponent<FishTouchBody>();palmLift=touch?touch.PalmLift:.04f;hand=h;heldAt=Time.time;heldUntil=heldAt+2;}
 public void StepHeld(float dt,float now){if(!hand)return;if(now>=heldUntil){Release();return;}var target=hand.position+hand.up*palmLift;if(now-heldAt>.2f&&(target-transform.position).sqrMagnitude>.45f*.45f){Release();return;}var delta=Vector3.ClampMagnitude(target-transform.position,3.5f*dt);if(body)body.Move(delta);else transform.position+=delta;var pose=hand.rotation*Quaternion.Euler(0,0,Mathf.Sin((now-heldAt)*12)*2);transform.rotation=Quaternion.Slerp(transform.rotation,pose,1-Mathf.Exp(-dt*20));transform.localScale=scale;}
 public void Release(){if(!hand)return;origin=transform.position;var heading=Vector3.ProjectOnPlane(hand.forward,Vector3.up);if(heading.sqrMagnitude<.01f)heading=Vector3.forward;transform.rotation=Quaternion.LookRotation(heading.normalized,Vector3.up);hand=null;transform.localScale=scale;}
}
}




