using UnityEngine;
namespace Underwater {
// A quiet acknowledgment, with the final habitat left available indefinitely.
public sealed class DiveEnding:MonoBehaviour {
 public AudioClip endingVoice;public bool Completed {get;private set;}float dwell;bool pending;
 public bool Step(float dt,bool inFinale,bool paused){if(Completed||paused)return false;if(!inFinale){dwell=0;pending=false;return false;}dwell+=dt;if(dwell>=20)pending=true;return pending;}
 public void Complete(){Completed=true;pending=false;}
 void Update(){var d=Diver.Instance;if(!d)return;if(Step(Mathf.Min(Time.deltaTime,.1f),d.Zone==7,d.MenuOpen)&&!d.MenuOpen&&!d.VoicePlaying){d.Say("We can stay here a while.",endingVoice);Complete();}}
}
}
