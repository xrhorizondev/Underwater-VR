using UnityEngine;
namespace Underwater {
 [ExecuteAlways,RequireComponent(typeof(Camera))]
 public class ReefOptics:MonoBehaviour {
  public Shader shader;public float exposure=1.05f;Material material;
  void OnEnable(){GetComponent<Camera>().depthTextureMode|=DepthTextureMode.Depth;}
  void OnRenderImage(RenderTexture source,RenderTexture dest){var c=GetComponent<Camera>();if(!shader||c.stereoEnabled){Graphics.Blit(source,dest);return;}if(!material)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};material.SetMatrix("_InverseProjection",c.projectionMatrix.inverse);material.SetMatrix("_CameraWorld",c.cameraToWorldMatrix);material.SetFloat("_Exposure",exposure);Graphics.Blit(source,dest,material);}
  void OnDisable(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
