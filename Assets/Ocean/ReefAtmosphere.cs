using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater {
 [ExecuteAlways]
 public class ReefAtmosphere:MonoBehaviour {
  Camera cachedCamera;float nextLightingUpdate;public Material waterBackground;public Texture2D caustics;public Light sunlight;public float previewTime=12;
  public Color shallowWater=new Color(.018f,.36f,.56f);public Color deepWater=new Color(.003f,.025f,.095f);
  public void Apply(Camera camera){
   float y=camera?camera.transform.position.y:1;float deep=Mathf.Clamp01((-y-8)/38);
   float light=Mathf.Lerp(1,.025f,deep);Shader.SetGlobalFloat("_ReefLight",light);Shader.SetGlobalFloat("_ReefTime",Application.isPlaying?Time.time:previewTime);Shader.SetGlobalTexture("_ReefCaustics",caustics);
   RenderSettings.skybox=waterBackground;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=Color.Lerp(shallowWater,deepWater,deep);RenderSettings.fogDensity=Mathf.Lerp(.015f,.031f,deep);
   Shader.SetGlobalColor("_ReefWaterColor",RenderSettings.fogColor.linear);
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Color.Lerp(new Color(.19f,.34f,.51f),new Color(.025f,.055f,.08f),deep);RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.065f,.12f,.21f),new Color(.01f,.02f,.045f),deep);RenderSettings.ambientGroundColor=Color.Lerp(new Color(.035f,.065f,.085f),new Color(.01f,.02f,.035f),deep);RenderSettings.ambientIntensity=1;
   if(sunlight){sunlight.intensity=Mathf.Lerp(1.65f,.025f,deep);sunlight.color=Color.Lerp(new Color(.78f,.93f,1f),new Color(.28f,.52f,.8f),deep);sunlight.shadowStrength=.65f;}
   if(camera){camera.backgroundColor=RenderSettings.fogColor;camera.clearFlags=waterBackground?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;}
  }
  void Update(){if(!Application.isPlaying){Apply(null);return;}Shader.SetGlobalFloat("_ReefTime",Time.time);if(Time.unscaledTime<nextLightingUpdate)return;nextLightingUpdate=Time.unscaledTime+.1f;if(!cachedCamera)cachedCamera=Camera.main;Apply(cachedCamera);}
 }
}




