using UnityEngine;

// A shared clock keeps the two comparison assets on the exact same pose.
// This component belongs only to the inspection scene, not gameplay fish.
public sealed class LionfishComparisonPlayback : MonoBehaviour {
 public AnimationClip clip;
 void OnEnable(){var player=GetComponent<Animation>();if(player)player.enabled=false;}
 void LateUpdate(){if(clip)clip.SampleAnimation(gameObject,Mathf.Repeat(Time.time,clip.length));}
}
