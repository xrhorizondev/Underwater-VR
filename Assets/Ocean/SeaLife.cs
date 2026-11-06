using UnityEngine;
namespace Underwater {
public class SeaLife:MonoBehaviour {
 public string kind; public bool danger,consequence=true,grabbable,observationOnly,stationary;
 public bool proximityEnabled=true,cautionEnabled=true; public AudioClip cautionVoice,reactionVoice;
 bool wasGlowing;public bool IsHeld=>hand;
 ReefSphereBody body;FishAgent swimmer;Vector3 origin,scale; float reacted=-100,heldUntil;Transform hand;Renderer[] renderers;Material[] materials;Color[] colors;
 void Start(){body=GetComponent<ReefSphereBody>();swimmer=GetComponent<FishAgent>();origin=transform.position;scale=transform.localScale;renderers=GetComponentsInChildren<Renderer>();materials=new Material[renderers.Length];colors=new Color[renderers.Length];for(int i=0;i<renderers.Length;i++){materials[i]=(danger||kind=="Octopus")?renderers[i].material:renderers[i].sharedMaterial;colors[i]=materials[i]&&materials[i].HasProperty("_Color")?materials[i].color:Color.white;}}
 void Update(){if(Application.platform==RuntimePlatform.Android&&!hand&&Diver.Instance&&(transform.position-Diver.Instance.transform.position).sqrMagnitude>48*48)return;if(hand){if(body)body.Move(hand.position-transform.position);transform.rotation=hand.rotation;transform.localScale=scale;if(Time.time>heldUntil)Release();return;}
 if(!swimmer&&!stationary&&kind!="Coral"&&kind!="Fire coral"&&kind!="Ray tail"){
 var target=origin+new Vector3(Mathf.Sin(Time.time*.16f+origin.z)*1.4f,Mathf.Sin(Time.time*.5f+origin.x)*.2f,Mathf.Cos(Time.time*.16f+origin.z)*.7f);
 if(kind=="Lumen"&&Diver.Instance&&Diver.Instance.Zone>=3)target=Diver.Instance.transform.position+Diver.Instance.Head.forward*2+Vector3.up;
 if(Time.time-reacted<2)target+=Vector3.forward*1.3f;
 if(body)body.Move((target-transform.position)*Mathf.Clamp01(Time.deltaTime*.7f));else transform.position=Vector3.Lerp(transform.position,target,Time.deltaTime*.7f);}
 if(kind=="Coral")transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time+origin.x)*4);
 if(materials==null||!Diver.Instance)return;float d=Diver.Instance.DistanceToHands(transform.position);
 if(observationOnly)return;
 if(danger){bool glow=proximityEnabled&&d<.65f;if(glow!=wasGlowing){wasGlowing=glow;for(int i=0;i<materials.Length;i++){materials[i].color=glow?Color.Lerp(colors[i],Color.red,.55f):colors[i];}}
 if(cautionEnabled&&d<.48f&&Time.time-reacted>4){Diver.Instance.Say(Warning(),cautionVoice);reacted=Time.time;}
 if(consequence&&d<.3f&&Time.time-reacted>.5f){Diver.Instance.Hurt(reactionVoice);reacted=Time.time;}}
 else if(d<.55f&&Time.time-reacted>3)React();
 }
 string Warning(){if(kind=="Lionfish")return "Whoa. Don't touch those spines.";if(kind=="Ray tail")return "Careful. Watch the tail.";if(kind=="Moon jelly")return "Careful. Give it some space.";return "Don't touch that one.";}
 public void React(){reacted=Time.time;if(kind=="Octopus")foreach(var m in materials)m.color=new Color(.7f,.2f,.5f);if(kind=="Turtle")Diver.Instance.Say("Sea turtle — Sea turtles breathe air.",null);}
 public void Grab(Transform h){if(!grabbable||hand)return;hand=h;heldUntil=Time.time+2;}
 public void Release(){if(!hand)return;origin=transform.position;hand=null;transform.localScale=scale;}
}
}




