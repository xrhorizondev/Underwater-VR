using UnityEngine;
namespace Underwater {
// Encounter locomotion supplies collision-resolved distance. Breathing/hover
// fins keep their own clock, so a blocked fish does not paddle at travel speed.
public sealed class LionfishTravelAnimation : MonoBehaviour {
 public GameObject visual;public AnimationClip clip;
 public float metresPerStroke=.22f,turnDegreesPerSecond=55;
 public float TravelSpeed{get;private set;}public float SwimClock{get;private set;}
 public float BreathClock{get;private set;}public float PlaybackRate{get;private set;}
 Transform[] spine;Quaternion[] restRotation,travelRotation;Vector3[] restPosition,restScale,travelPosition,travelScale;
 ReefSphereBody body;float measuredSpeed;bool initialized;
 public void Initialize(){if(initialized)return;initialized=true;body=GetComponent<ReefSphereBody>();var names=new[]{"Body","TailBase","TailFan"};spine=new Transform[3];var all=visual.GetComponentsInChildren<Transform>();for(int i=0;i<3;i++)foreach(var t in all)if(t.name==names[i])spine[i]=t;
  restRotation=new Quaternion[3];travelRotation=new Quaternion[3];restPosition=new Vector3[3];restScale=new Vector3[3];travelPosition=new Vector3[3];travelScale=new Vector3[3];for(int i=0;i<3;i++){restRotation[i]=spine[i].localRotation;restPosition[i]=spine[i].localPosition;restScale[i]=spine[i].localScale;}
  var player=visual.GetComponent<Animation>();if(player)player.enabled=false;
 }
 public Vector3 Steer(Vector3 desired,float dt){Initialize();if(desired.sqrMagnitude<.00001f)return transform.forward;desired.Normalize();var before=transform.rotation;var direction=Vector3.RotateTowards(transform.forward,desired,turnDegreesPerSecond*Mathf.Deg2Rad*dt,0);transform.rotation=Quaternion.LookRotation(direction,Vector3.up);if(body&&!body.Clear(transform.position)){transform.rotation=before;return Vector3.zero;}float alignment=Mathf.Max(0,Vector3.Dot(transform.forward,desired));return transform.forward*Mathf.Lerp(.12f,1,alignment*alignment);}
 public void MatchTravel(float actualSpeed){measuredSpeed=Mathf.Max(0,actualSpeed);}
 public void StepVisual(float dt){Initialize();if(!clip||!visual)return;TravelSpeed=Mathf.Lerp(TravelSpeed,measuredSpeed,1-Mathf.Exp(-dt*8));PlaybackRate=TravelSpeed*clip.length/Mathf.Max(.01f,2*metresPerStroke);SwimClock+=dt*PlaybackRate;BreathClock+=dt;
  clip.SampleAnimation(visual,Mathf.Repeat(SwimClock,clip.length));for(int i=0;i<3;i++){travelRotation[i]=spine[i].localRotation;travelPosition[i]=spine[i].localPosition;travelScale[i]=spine[i].localScale;}
  clip.SampleAnimation(visual,Mathf.Repeat(BreathClock,clip.length));float strength=Mathf.Clamp01(TravelSpeed/.16f);for(int i=0;i<3;i++){spine[i].localRotation=Quaternion.Slerp(restRotation[i],travelRotation[i],strength);spine[i].localPosition=Vector3.Lerp(restPosition[i],travelPosition[i],strength);spine[i].localScale=Vector3.Lerp(restScale[i],travelScale[i],strength);}
 }
 void LateUpdate(){var d=Diver.Instance;if(d&&d.MenuOpen)return;if(!ReefSimulationRange.Active(transform))return;StepVisual(Mathf.Min(Time.deltaTime,.05f));}
}
}
