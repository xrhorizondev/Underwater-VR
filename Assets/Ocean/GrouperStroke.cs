using UnityEngine;
namespace Underwater {
// Match the wreck grouper's slow stroke to actual, collision-limited travel.
public sealed class GrouperStroke:MonoBehaviour {
 public Transform[] spine=System.Array.Empty<Transform>(),pectoralFins=System.Array.Empty<Transform>();
 Renderer skin;MaterialPropertyBlock block;Vector3 previous,previousForward;float phase,effort,turn;bool ready;Quaternion[] restSpine,restFins;
 static readonly float[] amplitude={.45f,3.2f,6.5f,10,14};public float Phase=>phase;
 public void Initialize(){if(ready)return;ready=true;foreach(var renderer in GetComponentsInChildren<Renderer>())if(renderer.enabled&&(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)){skin=renderer;break;}block=new MaterialPropertyBlock();previous=transform.position;previousForward=transform.forward;phase=transform.position.z*.37f;restSpine=new Quaternion[spine.Length];for(int i=0;i<spine.Length;i++)restSpine[i]=spine[i].localRotation;restFins=new Quaternion[pectoralFins.Length];for(int i=0;i<pectoralFins.Length;i++)restFins[i]=pectoralFins[i].localRotation;}
 void Start(){Initialize();}
 void LateUpdate(){
  var d=Diver.Instance;if(d&&d.MenuOpen||!ReefSimulationRange.Active(transform)){previous=transform.position;return;}
  Step(Mathf.Max(Time.deltaTime,.001f));
 }
 public void Step(float dt){Initialize();dt=Mathf.Clamp(dt,.001f,.05f);var delta=transform.position-previous;float speed=delta.sqrMagnitude>4?0:delta.magnitude/dt;previous=transform.position;effort=Mathf.Lerp(effort,Mathf.Clamp01(speed/.3f),1-Mathf.Exp(-dt*4));turn=Mathf.Lerp(turn,Mathf.Clamp(Vector3.SignedAngle(previousForward,transform.forward,Vector3.up)/dt,-35,35),1-Mathf.Exp(-dt*3));previousForward=transform.forward;phase+=dt*Mathf.PI*2*Mathf.Lerp(.25f,.75f,effort);
  if(spine.Length>0){ApplyPose(phase,effort,turn);return;}
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",phase);block.SetFloat("_TailAmplitude",Mathf.Lerp(.008f,.03f,effort));skin.SetPropertyBlock(block);}
 }
 // The atlas remains attached to skinned vertices; no animated texture coordinates.
 public void ApplyPose(float cycle,float swimming,float turning=0){Initialize();float strength=Mathf.Lerp(.22f,1,Mathf.Clamp01(swimming));for(int i=0;i<spine.Length;i++){float angle=Mathf.Sin(cycle-i*.62f)*amplitude[Mathf.Min(i,4)]*strength;float bend=turning*Mathf.Lerp(.018f,.12f,i/Mathf.Max(1f,spine.Length-1));spine[i].localRotation=restSpine[i]*Quaternion.Euler(0,angle+bend,0);}for(int i=0;i<pectoralFins.Length;i++){float side=i==0?-1:1;pectoralFins[i].localRotation=restFins[i]*Quaternion.Euler(0,Mathf.Sin(cycle*.5f+i*.7f)*4*strength,side*(5+Mathf.Sin(cycle*.5f+i*.7f)*9)*Mathf.Lerp(.5f,1,swimming));}}
}
}
