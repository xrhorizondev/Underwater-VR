using UnityEngine;
namespace Underwater {
// Dolphins whistle; a shark emits only the soft movement of water around its body.
[RequireComponent(typeof(AudioSource))]
public sealed class FeaturedCreatureAudio:MonoBehaviour {
 public AudioClip clip;public float audibleDistance=6,volume=.2f;
 AudioSource source;
 void Awake(){source=GetComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.clip=clip;source.spatialBlend=1;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=.8f;source.maxDistance=audibleDistance;source.priority=120;source.volume=0;}
 void Update(){var diver=Diver.Instance;float target=0;if(diver&&diver.Head&&!diver.MenuOpen&&clip){float distance=Vector3.Distance(transform.position,diver.Head.position);target=volume*(1-Mathf.SmoothStep(0,1,distance/audibleDistance))*(diver.VoicePlaying?.25f:1);}
  if(target>.001f&&!source.isPlaying){source.time=Random.value*clip.length;source.Play();}source.volume=Mathf.MoveTowards(source.volume,target,Time.deltaTime*.3f);if(target<=.001f&&source.volume<=.001f&&source.isPlaying)source.Stop();
 }
 void OnDisable(){if(source)source.Stop();}
}
}
