using UnityEngine;
namespace Underwater {
// Original, loopable ambient score and soft UI/teleport cues; generated once, never per frame.
public class ReefSoundscape:MonoBehaviour {
 AudioSource music,effects;AudioClip score,click,travel;
 void Start(){music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();score=Tone(true,16);click=Tone(false,.12f);travel=Tone(false,.7f);music.clip=score;music.loop=true;music.volume=.12f;music.spatialBlend=0;effects.spatialBlend=0;effects.volume=.18f;music.Play();}
 AudioClip Tone(bool pad,float duration){const int rate=22050;var data=new float[Mathf.RoundToInt(rate*duration)];float[] notes={130.8128f,196f,261.6256f,329.6276f};for(int i=0;i<data.Length;i++){float t=(float)i/rate;float value=0;if(pad){foreach(float f in notes){float cycles=Mathf.Round(f*duration);value+=Mathf.Sin(2*Mathf.PI*cycles*t/duration)*.12f;}value*=.7f+.3f*Mathf.Cos(2*Mathf.PI*t/duration);}else value=Mathf.Sin(2*Mathf.PI*(540*t+180*t*t))*Mathf.Sin(Mathf.PI*t/duration)*Mathf.Exp(-t*5)*.4f;data[i]=value;}var clip=AudioClip.Create(pad?"Reef ambient harmony":"Reef interaction cue",data.Length,1,rate,false);clip.SetData(data,0);return clip;}
 public void Cue(bool teleport){if(effects)effects.PlayOneShot(teleport?travel:click);}
 void OnDestroy(){if(score)Destroy(score);if(click)Destroy(click);if(travel)Destroy(travel);}
}
}
