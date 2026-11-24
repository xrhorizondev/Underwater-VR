using UnityEngine;
public class CombJellyComparisonPlayback : MonoBehaviour {
 public AnimationClip clip;
 void OnEnable(){var a=GetComponent<Animation>();if(a&&clip){a.wrapMode=WrapMode.Loop;a.Play(clip.name);}}
}
