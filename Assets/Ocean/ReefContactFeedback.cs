using UnityEngine;

namespace Underwater {
public sealed class ReefContactFeedback : MonoBehaviour {
 MeshRenderer overlay;Material material;Mesh mesh;float remaining;
 void Awake(){
  var shader=Resources.Load<Shader>("ReefContactEdge");if(!shader)return;
  material=new Material(shader){enableInstancing=true};
  mesh=new Mesh{name="Stereo caution edge"};
  mesh.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0)};
  mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.bounds=new Bounds(Vector3.zero,Vector3.one*4);
  var quad=new GameObject("Brief contact edge");quad.transform.SetParent(transform,false);quad.transform.localPosition=Vector3.forward*.2f;
  quad.AddComponent<MeshFilter>().sharedMesh=mesh;overlay=quad.AddComponent<MeshRenderer>();overlay.sharedMaterial=material;
  overlay.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;overlay.receiveShadows=false;overlay.enabled=false;
 }
 public void Pulse(){if(overlay){remaining=.22f;material.SetFloat("_Intensity",.32f);overlay.enabled=true;}}
 void LateUpdate(){if(!overlay||remaining<=0)return;remaining=Mathf.Max(0,remaining-Time.unscaledDeltaTime);material.SetFloat("_Intensity",remaining/.22f*.32f);if(remaining<=0)overlay.enabled=false;}
 void OnDisable(){remaining=0;if(overlay)overlay.enabled=false;}
 void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
}
}
