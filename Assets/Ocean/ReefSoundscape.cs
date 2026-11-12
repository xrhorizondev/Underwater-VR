using UnityEngine;
namespace Underwater {
// Samples are baked in the editor, so entering VR never synthesizes audio.
public class ReefSoundscape:MonoBehaviour {
 AudioSource music,effects;AudioClip click,travel;
 void Start(){music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();music.clip=Resources.Load<AudioClip>("ReefScore");click=Resources.Load<AudioClip>("ReefClick");travel=Resources.Load<AudioClip>("ReefTravel");music.loop=true;music.volume=.12f;music.spatialBlend=0;effects.spatialBlend=0;effects.volume=.18f;if(music.clip)music.Play();}
 public void Cue(bool teleport){var clip=teleport?travel:click;if(effects&&clip)effects.PlayOneShot(clip);}
}
}
