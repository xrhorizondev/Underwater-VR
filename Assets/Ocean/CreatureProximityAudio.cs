using System.Collections.Generic;
using UnityEngine;
namespace Underwater {
// A bounded voice pool, rather than an AudioSource on every schooling fish.
public sealed class CreatureProximityAudio:MonoBehaviour {
 readonly List<Transform> animals=new List<Transform>();readonly AudioSource[] voices=new AudioSource[4];readonly Transform[] owners=new Transform[4];
 AudioClip fish,dolphin;float refresh;
 void Start(){fish=Resources.Load<AudioClip>("ReefFishSounds");dolphin=Resources.Load<AudioClip>("DolphinNearby");
  foreach(var life in FindObjectsByType<SeaLife>(FindObjectsSortMode.None))if(life.GetComponent<FishAgent>()||life.GetComponent<FishBodyMotion>()||life.GetComponent<WreckCreature>()||life.GetComponent<MarineSwimmer>()||life.GetComponent<StingraySwimmer>())animals.Add(life.transform);
  foreach(var fishAgent in FindObjectsByType<FishAgent>(FindObjectsSortMode.None))if(!animals.Contains(fishAgent.transform))animals.Add(fishAgent.transform);
  for(int i=0;i<voices.Length;i++){var go=new GameObject("Nearby animal voice "+i);go.transform.SetParent(transform,false);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=true;a.spatialBlend=1;a.dopplerLevel=0;a.minDistance=.6f;a.maxDistance=6;a.rolloffMode=AudioRolloffMode.Linear;a.priority=150;voices[i]=a;}
 }
 void Update(){var d=Diver.Instance;if(!d||!d.Head)return;refresh-=Time.deltaTime;
  if(refresh<=0){refresh=.4f;animals.Sort((a,b)=>(a.position-d.Head.position).sqrMagnitude.CompareTo((b.position-d.Head.position).sqrMagnitude));}
  for(int i=0;i<voices.Length;i++){var a=voices[i];Transform desired=null;for(int n=0;n<animals.Count;n++){var candidate=animals[n];bool taken=false;for(int k=0;k<i;k++)if(owners[k]==candidate)taken=true;if(!taken&&(candidate.position-d.Head.position).sqrMagnitude<36){desired=candidate;break;}}
   if(desired!=owners[i]){a.volume=Mathf.MoveTowards(a.volume,0,Time.deltaTime*.7f);if(a.volume>.001f)continue;a.Stop();owners[i]=desired;if(desired){var creature=desired.GetComponent<WreckCreature>();a.clip=creature&&creature.species==WreckCreature.Species.Dolphin?dolphin:fish;if(a.clip){a.time=(i*3.13f)%a.clip.length;a.Play();}}}
   if(!owners[i])continue;a.transform.position=owners[i].position;float proximity=1-Mathf.SmoothStep(0,1,Vector3.Distance(a.transform.position,d.Head.position)/6);float volume=proximity*(d.VoicePlaying?.06f:.18f);a.volume=Mathf.MoveTowards(a.volume,volume,Time.deltaTime*.25f);
  }
 }
}
}
