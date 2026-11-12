using UnityEngine;
namespace Underwater {
// Samples are baked in the editor, so entering VR never synthesizes audio.
public class ReefSoundscape:MonoBehaviour {
 AudioSource music,effects;AudioClip click,travel;bool ready;
 void Start(){music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();music.clip=Resources.Load<AudioClip>("ReefOceanMusic")??Resources.Load<AudioClip>("ReefScore");click=Resources.Load<AudioClip>("ReefClick");travel=Resources.Load<AudioClip>("ReefTravel");music.loop=true;music.volume=0;music.spatialBlend=0;music.priority=64;effects.spatialBlend=0;effects.volume=.24f;ready=true;if(music.clip)music.Play();else Debug.LogError("Underwater background music is missing.");Debug.Log($"REEF_AUDIO music={music.clip?.name} listenerVolume={AudioListener.volume} listenerPaused={AudioListener.pause} sampleRate={AudioSettings.outputSampleRate}");}
 void Update(){if(!ready)return;float target=Diver.Instance&&Diver.Instance.VoicePlaying?.12f:.38f;music.volume=Mathf.MoveTowards(music.volume,target,Time.unscaledDeltaTime*.3f);}
 void OnApplicationPause(bool paused){if(!paused)ResumeMusic();}
 void OnApplicationFocus(bool focused){if(focused)ResumeMusic();}
 void ResumeMusic(){if(ready&&music&&music.clip&&!music.isPlaying)music.Play();}
 public void Cue(bool teleport){var clip=teleport?travel:click;if(effects&&clip)effects.PlayOneShot(clip);}
}
}
