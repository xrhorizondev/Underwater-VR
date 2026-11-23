using UnityEngine;
namespace Underwater {
// Match the wreck grouper's slow stroke to actual, collision-limited travel.
public sealed class GrouperStroke:MonoBehaviour {
 Renderer skin;MaterialPropertyBlock block;Vector3 previous;float phase;bool ready;
 public float Phase=>phase;
 public void Initialize(){if(ready)return;ready=true;foreach(var renderer in GetComponentsInChildren<Renderer>())if(renderer.enabled&&(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)){skin=renderer;break;}block=new MaterialPropertyBlock();previous=transform.position;}
 void Start(){Initialize();}
 void LateUpdate(){
  var d=Diver.Instance;if(d&&d.MenuOpen||!ReefSimulationRange.Active(transform)){previous=transform.position;return;}
  Step(Mathf.Max(Time.deltaTime,.001f));
 }
 public void Step(float dt){Initialize();dt=Mathf.Max(dt,.001f);float speed=Vector3.Distance(previous,transform.position)/dt;previous=transform.position;float effort=Mathf.Clamp01(speed/.17f);phase+=Mathf.Min(dt,.05f)*Mathf.PI*2*Mathf.Lerp(.12f,.65f,effort);
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",phase);block.SetFloat("_TailAmplitude",Mathf.Lerp(.008f,.03f,effort));skin.SetPropertyBlock(block);}
 }
}
}
