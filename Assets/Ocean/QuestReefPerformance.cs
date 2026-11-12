using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater {
// Installed in Android build scenes only. Colliders always remain active.
public class QuestReefPerformance:MonoBehaviour {
 Camera view;
 void Awake(){
  UnityEngine.XR.XRSettings.eyeTextureResolutionScale=.78f;QualitySettings.antiAliasing=2;QualitySettings.shadows=ShadowQuality.Disable;QualitySettings.shadowResolution=ShadowResolution.Low;QualitySettings.shadowDistance=16;QualitySettings.shadowCascades=0;QualitySettings.pixelLightCount=1;QualitySettings.lodBias=.65f;QualitySettings.maximumLODLevel=0;QualitySettings.vSyncCount=0;QualitySettings.realtimeReflectionProbes=false;
  QualitySettings.streamingMipmapsActive=true;QualitySettings.streamingMipmapsMemoryBudget=256;
  view=Camera.main;if(view){view.allowHDR=false;view.allowMSAA=true;view.farClipPlane=60;var optics=view.GetComponent<ReefOptics>();if(optics)optics.enabled=false;}
  // Mobile material conversion is performed by QuestReefBuild, before the APK
  // is written. Do not scan thousands of renderers or swap shaders on entry.
  // Unity culls against the stereo camera frustum and far plane, including the first frame after a teleport.
  bool measureTravel=ShouldMeasureHabitatTravel();
  bool measureFrames=measureTravel||ConsumeFrameTestRequest();
  if(measureFrames&&!GetComponent<ReefFrameMetrics>())gameObject.AddComponent<ReefFrameMetrics>();
  if(measureFrames)Debug.Log("REEF_DIAGNOSTICS travelRequested="+measureTravel);
  if(measureTravel)StartCoroutine(MeasureHabitatTravel());
 }
 static bool ConsumeFrameTestRequest(){
#if UNITY_ANDROID && !UNITY_EDITOR
  string marker=System.IO.Path.Combine(Application.persistentDataPath,"reef-frame-test");
  if(!System.IO.File.Exists(marker))return false;
  System.IO.File.Delete(marker);return true;
#else
  return false;
#endif
 }
 static bool ShouldMeasureHabitatTravel(){
#if UNITY_ANDROID && !UNITY_EDITOR
  using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
  using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
  using var intent=activity.Call<AndroidJavaObject>("getIntent");
  return intent.Call<bool>("getBooleanExtra","reefTravelTest",false)||System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"reef-travel-test"));
#else
  return false;
#endif
 }
 System.Collections.IEnumerator MeasureHabitatTravel(){
  yield return new WaitForSecondsRealtime(8);
  var motor=FindFirstObjectByType<DiverCollisionMotor>();
  if(!motor)yield break;
  Vector3 start=motor.transform.position;
  for(int i=0;i<OceanWorld.Centers.Length;i++){
   if(!motor.TryTeleport(OceanWorld.Centers[i])){Debug.Log("REEF_TRAVEL blocked="+i);continue;}
   float end=Time.realtimeSinceStartup+3;
   int frames=0,slow=0;
   float worst=0;
   while(Time.realtimeSinceStartup<end){
    yield return null;
    float ms=Time.unscaledDeltaTime*1000;
    frames++;
    if(ms>27.8f)slow++;
    worst=Mathf.Max(worst,ms);
   }
   Debug.Log($"REEF_TRAVEL habitat={i} frames={frames} overTwoRefreshes={slow} worstMs={worst:F1}");
  }
  motor.TryTeleport(start);
  Debug.Log("REEF_TRAVEL complete");
 }
}
}
