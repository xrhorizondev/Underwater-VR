using UnityEngine;
namespace Underwater {
// A small dead band prevents detail from alternating during natural head sway.
[DisallowMultipleComponent,RequireComponent(typeof(LODGroup))]
public sealed class ReefStableLod:MonoBehaviour {
 public const float Hysteresis=.12f;
 public static int FadingCorals {get;private set;}
 public static bool CrossFadeEnabled {get;private set;}=true;
 static bool readFadePreference;
 public static void RefreshFadePreference(){CrossFadeEnabled=!System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"reef-opaque-lod"));readFadePreference=true;}
 static Camera sharedCamera;static int projectionFrame=-1;static float verticalScale;static Vector3 previousView;static int travelFrame=-1;
 LODGroup group;LOD[] levels;int current=-2;float next;Transform viewer;Camera cameraView;bool coral;
 public static bool IsCoral(LODGroup group){
  for(var t=group.transform;t;t=t.parent){var name=t.name.ToLowerInvariant();if(name.Contains("coral")||name.StartsWith("scanned "))return true;}
  return false;
 }
 public static void ExtendViewingRange(LODGroup group){
  if(IsCoral(group)){ConfigureCoral(group);return;}
  var levels=group.GetLODs();var scale=group.transform.lossyScale;
  float size=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
  for(int i=0;i<levels.Length;i++){
   float cap=i==levels.Length-1?size/180f:size/(i==0?24f:48f);
   levels[i].screenRelativeTransitionHeight=Mathf.Min(levels[i].screenRelativeTransitionHeight,cap);
  }
  group.SetLODs(levels);
 }
 public static void ConfigureCoral(LODGroup group){
  var lods=group.GetLODs();var s=group.transform.lossyScale;float size=group.size*Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));
  float margin=size*.5f;for(int i=0;i<lods.Length;i++){
   float distance=i==lods.Length-1?90+margin:(i==0?10:24)+margin;
   lods[i].screenRelativeTransitionHeight=Mathf.Clamp(size/(2*distance),.00001f,.95f);
   // Unity expresses width as a fraction of the whole screen-size interval.
   // A fixed 0.25 on LOD0 would start fading tiny corals at arm's length.
   float upper=i==0?1:lods[i-1].screenRelativeTransitionHeight;
   lods[i].fadeTransitionWidth=Mathf.Clamp(lods[i].screenRelativeTransitionHeight*.2f/Mathf.Max(.00001f,upper-lods[i].screenRelativeTransitionHeight),.0001f,.5f);
  }
  group.SetLODs(lods);group.fadeMode=LODFadeMode.None;group.animateCrossFading=false;group.ForceLOD(-1);
 }
 void Start(){if(!readFadePreference)RefreshFadePreference();group=GetComponent<LODGroup>();coral=IsCoral(group);if(coral)ConfigureCoral(group);levels=group.GetLODs();if(!sharedCamera)sharedCamera=Camera.main;cameraView=sharedCamera;if(cameraView)viewer=cameraView.transform;next=Time.unscaledTime+(Mathf.Abs(GetHashCode())%120)*.001f;}
 public static bool NeedsCoralFade(LOD[] lods,float height){
  for(int i=0;i<lods.Length;i++){float lower=lods[i].screenRelativeTransitionHeight;float upper=i==0?1:lods[i-1].screenRelativeTransitionHeight;float start=lower+(upper-lower)*lods[i].fadeTransitionWidth;if(height>=lower*.92f&&height<=start*1.08f)return true;}
  return false;
 }
 public int Choose(float height){if(levels==null){group=GetComponent<LODGroup>();levels=group.GetLODs();}int chosen=-1;for(int i=0;i<levels.Length;i++)if(height>=levels[i].screenRelativeTransitionHeight){chosen=i;break;}if(current==-2)return current=chosen;int previousRank=current<0?levels.Length:current,rank=chosen<0?levels.Length:chosen;if(rank>previousRank&&height>=levels[previousRank].screenRelativeTransitionHeight*(1-Hysteresis))return current;if(rank<previousRank&&height<levels[rank].screenRelativeTransitionHeight*(1+Hysteresis))return current;return current=chosen;}
 void Update(){
  // Coral groups use Unity's per-eye native selection and spatial crossfade.
  // Do not ForceLOD: doing so bypasses the renderer's transition weights.
  if(coral){
   if(!viewer||Time.unscaledTime<next)return;next=Time.unscaledTime+.12f;
   if(projectionFrame!=Time.frameCount){projectionFrame=Time.frameCount;verticalScale=cameraView.stereoEnabled?Mathf.Abs(cameraView.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).m11):1/Mathf.Tan(cameraView.fieldOfView*Mathf.Deg2Rad*.5f);}
   var coralScale=transform.lossyScale;float coralDistance=Mathf.Max(.01f,Vector3.Distance(viewer.position,transform.TransformPoint(group.localReferencePoint)));float coralHeight=group.size*Mathf.Max(Mathf.Abs(coralScale.x),Mathf.Abs(coralScale.y),Mathf.Abs(coralScale.z))*QualitySettings.lodBias*verticalScale/(2*coralDistance);
   // Crossfade variants discard fragments and inhibit the fast opaque depth
   // path on Quest. Pay that cost only around an actual transition band.
   var mode=CrossFadeEnabled&&NeedsCoralFade(levels,coralHeight)?LODFadeMode.CrossFade:LODFadeMode.None;if(group.fadeMode!=mode){FadingCorals+=mode==LODFadeMode.CrossFade?1:-1;group.fadeMode=mode;}return;
  }
#if UNITY_EDITOR
  // Forced LOD applies to every camera, including a nearby Scene view while
  // the diver is in another habitat. Let Unity select per camera in the editor.
  if(group)group.ForceLOD(-1);return;
#else
  if(!viewer)return;if(projectionFrame!=Time.frameCount){projectionFrame=Time.frameCount;if((viewer.position-previousView).sqrMagnitude>4)travelFrame=Time.frameCount;previousView=viewer.position;verticalScale=cameraView.stereoEnabled?Mathf.Abs(cameraView.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).m11):1/Mathf.Tan(cameraView.fieldOfView*Mathf.Deg2Rad*.5f);}if(Time.unscaledTime<next&&travelFrame!=Time.frameCount)return;next=Time.unscaledTime+.12f;var scale=transform.lossyScale;float distance=Mathf.Max(.01f,Vector3.Distance(viewer.position,transform.TransformPoint(group.localReferencePoint)));float height=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))*QualitySettings.lodBias*verticalScale/(2*distance);int selected=Choose(height);group.ForceLOD(selected<0?levels.Length:selected);
#endif
 }
 void OnDisable(){if(group){if(coral&&group.fadeMode==LODFadeMode.CrossFade)FadingCorals=Mathf.Max(0,FadingCorals-1);group.ForceLOD(-1);}}
}
}
