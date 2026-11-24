using UnityEngine;
namespace Underwater {
// For interactive fish whose position is owned by SeaLife (including Lumen and grabbing).
public class FishBodyMotion:MonoBehaviour {
 Renderer skin;MaterialPropertyBlock block;SeaLife interaction;Vector3 previous;float phase,visualSpeed,referenceSpeed=1;bool ownsTurning,ready;
 public float VisualSpeed=>visualSpeed;public float Phase=>phase;
 public void Initialize(){if(ready)return;ready=true;skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();interaction=GetComponent<SeaLife>();var cruise=GetComponent<ReefCruiser>();ownsTurning=!GetComponent<ReefFishFollower>()&&!cruise;referenceSpeed=cruise?Mathf.Max(.1f,cruise.speed):1;previous=transform.position;phase=transform.position.x*2.4f;}
 void Start(){Initialize();}
 void LateUpdate(){
  var d=Diver.Instance;if(d&&d.MenuOpen||!ReefSimulationRange.Active(transform)&&!(interaction&&interaction.IsHeld)){previous=transform.position;return;}Step(Mathf.Min(Time.deltaTime,.05f));
 }
 public void Step(float dt){Initialize();dt=Mathf.Max(dt,.001f);Vector3 delta=transform.position-previous;Vector3 v=delta.sqrMagnitude>4?Vector3.zero:delta/dt;previous=transform.position;
  bool held=interaction&&interaction.IsHeld;
  if(ownsTurning&&!held&&v.sqrMagnitude>.0004f){v.y=Mathf.Clamp(v.y,-v.magnitude*.18f,v.magnitude*.18f);transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(v),55*dt);}
  visualSpeed=Mathf.Lerp(visualSpeed,held?0:v.magnitude,1-Mathf.Exp(-dt*5));float effort=Mathf.Clamp01(visualSpeed/referenceSpeed);phase+=dt*Mathf.PI*2*(held?1.1f:Mathf.Lerp(.22f,3.2f,effort));
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",phase);block.SetFloat("_TailAmplitude",held?.018f:Mathf.Lerp(.008f,.055f,effort));skin.SetPropertyBlock(block);}
 }
}
}
