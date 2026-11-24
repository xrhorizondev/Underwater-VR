using UnityEngine;
public sealed class MorayComparisonPlayback:MonoBehaviour {
 public AnimationClip clip;
 void Start(){var a=GetComponent<Animation>();if(a&&clip){a.wrapMode=WrapMode.Loop;a.cullingType=AnimationCullingType.AlwaysAnimate;a.Play(clip.name);}}
}
