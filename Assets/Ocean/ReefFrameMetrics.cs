using UnityEngine;
using UnityEngine.XR;
using Unity.Profiling;

namespace Underwater {
// Fixed-size counters: no per-frame strings, allocations or scene searches.
// CPU/GPU timings may be unavailable on a runtime; zero is never a pass.
public sealed class ReefFrameMetrics : MonoBehaviour {
 readonly FrameTiming[] timing=new FrameTiming[1];
 readonly int[] histogram=new int[251];
 Transform head;Quaternion previous;float windowStart;int frames,turnFrames,slowTurns,overBudget;
 float worst,cpuWorst,gpuWorst,xrGpuRawWorst,refreshRate=72;bool firstFocusedFrame;
 XRDisplaySubsystem display;ProfilerRecorder mainThread;
 void Start(){
  if(Camera.main)head=Camera.main.transform;previous=head?head.rotation:Quaternion.identity;windowStart=Time.realtimeSinceStartup;
  var displays=new System.Collections.Generic.List<XRDisplaySubsystem>();SubsystemManager.GetSubsystems(displays);
  foreach(var candidate in displays)if(candidate.running){display=candidate;break;}
  if(display!=null&&display.TryGetDisplayRefreshRate(out float hz)&&hz>0)refreshRate=hz;
  mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
 }
 void LateUpdate(){
  if(!Application.isFocused)return;
  if(!firstFocusedFrame){
   firstFocusedFrame=true;previous=head?head.rotation:Quaternion.identity;
   Debug.Log($"REEF_FIRST_FOCUSED_FRAME time={Time.realtimeSinceStartup:F3} loadingBoundaryIntervalMs={Time.unscaledDeltaTime*1000:F1} refreshHz={refreshRate:F1}");
   return; // Report the loading boundary separately from steady exploration.
  }
  float ms=Time.unscaledDeltaTime*1000;
  bool turning=head&&Quaternion.Angle(previous,head.rotation)>30*Time.unscaledDeltaTime;
  if(head)previous=head.rotation;
  frames++;histogram[Mathf.Clamp(Mathf.CeilToInt(ms),0,250)]++;worst=Mathf.Max(worst,ms);
  if(ms>1000f/refreshRate)overBudget++;
  if(turning){turnFrames++;if(ms>1500f/refreshRate)slowTurns++;}
  FrameTimingManager.CaptureFrameTimings();
  if(FrameTimingManager.GetLatestTimings(1,timing)>0){cpuWorst=Mathf.Max(cpuWorst,(float)timing[0].cpuFrameTime);gpuWorst=Mathf.Max(gpuWorst,(float)timing[0].gpuFrameTime);}
  // Keep the provider value raw: this Quest/OpenXR combination returns values
  // matching VrApi milliseconds despite the API documenting seconds.
  if(display!=null&&display.TryGetAppGPUTimeLastFrame(out float gpuRaw))xrGpuRawWorst=Mathf.Max(xrGpuRawWorst,gpuRaw);
  if(mainThread.Valid)cpuWorst=Mathf.Max(cpuWorst,mainThread.LastValue/1000000f);
  if(Time.realtimeSinceStartup-windowStart<5)return;
  int target=Mathf.CeilToInt(frames*.95f),sum=0,p95=250;
  for(int i=0;i<histogram.Length;i++){sum+=histogram[i];if(sum>=target){p95=i;break;}}
  Debug.Log($"REEF_FRAMES time={Time.realtimeSinceStartup:F1} zone={(Diver.Instance?Diver.Instance.Zone:-1)} frames={frames} p95MsCeil={p95} worstMs={worst:F1} refreshHz={refreshRate:F1} intervalOverBudget={overBudget} turnFrames={turnFrames} turnMissedIntervals={slowTurns} mainThreadWorstMs={cpuWorst:F1} gpuWorstMs={gpuWorst:F1} xrGpuRawWorst={xrGpuRawWorst:F3} mainThreadTimingAvailable={cpuWorst>0} gpuTimingAvailable={gpuWorst>0}");
  System.Array.Clear(histogram,0,histogram.Length);frames=turnFrames=slowTurns=overBudget=0;worst=cpuWorst=gpuWorst=xrGpuRawWorst=0;windowStart=Time.realtimeSinceStartup;
 }
 void OnApplicationFocus(bool focused){if(!focused)return;System.Array.Clear(histogram,0,histogram.Length);frames=turnFrames=slowTurns=overBudget=0;worst=cpuWorst=gpuWorst=xrGpuRawWorst=0;windowStart=Time.realtimeSinceStartup;if(head)previous=head.rotation;}
 void OnDestroy(){mainThread.Dispose();}
}
}
