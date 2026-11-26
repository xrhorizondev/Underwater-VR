using UnityEngine;
namespace Underwater {
// A small dead band prevents detail from alternating during natural head sway.
[DisallowMultipleComponent,RequireComponent(typeof(LODGroup))]
public sealed class ReefStableLod:MonoBehaviour {
 public const float Hysteresis=.12f;
 static Camera sharedCamera;static int projectionFrame=-1;static float verticalScale;static Vector3 previousView;static int travelFrame=-1;
 LODGroup group;LOD[] levels;int current=-2;float next;Transform viewer;Camera cameraView;
 void Start(){group=GetComponent<LODGroup>();levels=group.GetLODs();if(!sharedCamera)sharedCamera=Camera.main;cameraView=sharedCamera;if(cameraView)viewer=cameraView.transform;next=Time.unscaledTime+(Mathf.Abs(GetHashCode())%120)*.001f;}
 public int Choose(float height){if(levels==null){group=GetComponent<LODGroup>();levels=group.GetLODs();}int chosen=-1;for(int i=0;i<levels.Length;i++)if(height>=levels[i].screenRelativeTransitionHeight){chosen=i;break;}if(current==-2)return current=chosen;int previousRank=current<0?levels.Length:current,rank=chosen<0?levels.Length:chosen;if(rank>previousRank&&height>=levels[previousRank].screenRelativeTransitionHeight*(1-Hysteresis))return current;if(rank<previousRank&&height<levels[rank].screenRelativeTransitionHeight*(1+Hysteresis))return current;return current=chosen;}
 void Update(){
#if UNITY_EDITOR
  // Forced LOD applies to every camera, including a nearby Scene view while
  // the diver is in another habitat. Let Unity select per camera in the editor.
  if(group)group.ForceLOD(-1);return;
#else
  if(!viewer)return;if(projectionFrame!=Time.frameCount){projectionFrame=Time.frameCount;if((viewer.position-previousView).sqrMagnitude>4)travelFrame=Time.frameCount;previousView=viewer.position;verticalScale=cameraView.stereoEnabled?Mathf.Abs(cameraView.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).m11):1/Mathf.Tan(cameraView.fieldOfView*Mathf.Deg2Rad*.5f);}if(Time.unscaledTime<next&&travelFrame!=Time.frameCount)return;next=Time.unscaledTime+.12f;var scale=transform.lossyScale;float distance=Mathf.Max(.01f,Vector3.Distance(viewer.position,transform.TransformPoint(group.localReferencePoint)));float height=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))*QualitySettings.lodBias*verticalScale/(2*distance);int selected=Choose(height);group.ForceLOD(selected<0?levels.Length:selected);
#endif
 }
 void OnDisable(){if(group)group.ForceLOD(-1);}
}
}
