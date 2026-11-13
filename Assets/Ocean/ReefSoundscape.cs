using UnityEngine;
namespace Underwater {
// Ambience clips are baked offline. No music or runtime audio synthesis.
public class ReefSoundscape:MonoBehaviour {
 AudioSource water,effects,whale,reef;AudioClip click,travel;bool ready;
 public void Initialize(){if(ready)return;water=Source("Underwater water bed",false);effects=Source("Travel cues",false);whale=Source("Distant whale calls",true);reef=Source("Reef fish clicks",true);
  water.clip=Resources.Load<AudioClip>("ReefWaterAmbience");whale.clip=Resources.Load<AudioClip>("ReefWhaleCalls");reef.clip=Resources.Load<AudioClip>("ReefFishSounds");click=Resources.Load<AudioClip>("ReefClick");travel=Resources.Load<AudioClip>("ReefTravel");
  foreach(var source in new[]{water,whale,reef}){source.loop=true;source.volume=0;if(source.clip)source.Play();else Debug.LogError("Missing underwater ambience clip for "+source.name);}effects.volume=.18f;ready=true;
 }
 AudioSource Source(string label,bool spatial){var go=new GameObject(label);go.transform.SetParent(transform,false);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.spatialBlend=spatial?1:0;a.dopplerLevel=0;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=2;a.maxDistance=35;a.priority=spatial?160:96;return a;}
 void Start(){Initialize();}
 void Update(){if(!ready)return;var d=Diver.Instance;float duck=d&&d.VoicePlaying?.4f:1;float deep=d&&d.Head?Mathf.Clamp01((-d.Head.position.y-8)/38):0;
  water.volume=Mathf.MoveTowards(water.volume,.46f*duck,Time.unscaledDeltaTime*.25f);whale.volume=Mathf.MoveTowards(whale.volume,Mathf.Lerp(.14f,.24f,deep)*duck,Time.unscaledDeltaTime*.15f);reef.volume=Mathf.MoveTowards(reef.volume,Mathf.Lerp(.26f,.07f,deep)*duck,Time.unscaledDeltaTime*.2f);
  if(d&&d.Head){whale.transform.position=d.Head.position+new Vector3(-12,-4,16);reef.transform.position=d.Head.position+new Vector3(3,-1,4);}
 }
 void OnApplicationPause(bool paused){if(!paused)Resume();}
 void OnApplicationFocus(bool focused){if(focused)Resume();}
 void Resume(){if(!ready)return;foreach(var a in new[]{water,whale,reef})if(a&&a.clip&&!a.isPlaying)a.Play();}
 public void Cue(bool teleport){var clip=teleport?travel:click;if(effects&&clip)effects.PlayOneShot(clip);}
}
}
