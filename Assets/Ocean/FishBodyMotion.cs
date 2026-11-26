using UnityEngine;
namespace Underwater {
// For interactive fish whose position is owned by SeaLife (including Lumen and grabbing).
public class FishBodyMotion:MonoBehaviour {
 Renderer skin;MaterialPropertyBlock block;SeaLife interaction;Vector3 previous,previousForward;float phase,visualSpeed,referenceSpeed=1,turn;bool ownsTurning,ready,companion;
 public float VisualSpeed=>visualSpeed;public float Phase=>phase;
 public void Initialize(){if(ready)return;ready=true;skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();interaction=GetComponent<SeaLife>();var cruise=GetComponent<ReefCruiser>();companion=GetComponent<ReefFishFollower>();ownsTurning=!companion&&!cruise;referenceSpeed=cruise?Mathf.Max(.1f,cruise.speed):companion?.65f:1;previous=transform.position;previousForward=transform.forward;phase=transform.position.x*2.4f;}
 void Start(){Initialize();}
 void LateUpdate(){
  var d=Diver.Instance;if(d&&d.MenuOpen||!ReefSimulationRange.Active(transform)&&!(interaction&&interaction.IsHeld)){previous=transform.position;return;}Step(Mathf.Min(Time.deltaTime,.05f));
 }
 public void Step(float dt){Initialize();dt=Mathf.Max(dt,.001f);Vector3 delta=transform.position-previous;Vector3 v=delta.sqrMagnitude>4?Vector3.zero:delta/dt;previous=transform.position;
  bool held=interaction&&interaction.IsHeld;
  if(ownsTurning&&!held&&v.sqrMagnitude>.0004f){v.y=Mathf.Clamp(v.y,-v.magnitude*.18f,v.magnitude*.18f);transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(v),55*dt);}
  visualSpeed=Mathf.Lerp(visualSpeed,held?0:v.magnitude,1-Mathf.Exp(-dt*5));float effort=Mathf.Clamp01(visualSpeed/referenceSpeed);phase+=dt*Mathf.PI*2*(held?1.1f:companion?Mathf.Lerp(.65f,1.8f,effort):Mathf.Lerp(.22f,3.2f,effort));
  turn=Mathf.Lerp(turn,Mathf.Clamp(Vector3.SignedAngle(previousForward,transform.forward,Vector3.up)/dt/40,-1,1),1-Mathf.Exp(-dt*5));previousForward=transform.forward;
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",phase);block.SetFloat("_TailAmplitude",held?.018f:Mathf.Lerp(companion?.035f:.008f,companion?.075f:.055f,effort));block.SetFloat("_TurnBend",turn*.018f);skin.SetPropertyBlock(block);}
 }
}
}
