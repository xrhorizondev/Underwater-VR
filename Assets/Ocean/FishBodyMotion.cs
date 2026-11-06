using UnityEngine;
namespace Underwater {
// For interactive fish whose position is owned by SeaLife (including Lumen and grabbing).
public class FishBodyMotion:MonoBehaviour {
 Renderer skin;MaterialPropertyBlock block;SeaLife interaction;Vector3 previous;float phase;
 void Start(){skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();interaction=GetComponent<SeaLife>();previous=transform.position;phase=transform.position.x*2.4f;}
 void LateUpdate(){
  float dt=Mathf.Max(Time.deltaTime,.001f);Vector3 v=(transform.position-previous)/dt;previous=transform.position;
  bool held=interaction&&interaction.IsHeld;
  if(!held&&v.sqrMagnitude>.0004f){v.y=Mathf.Clamp(v.y,-v.magnitude*.18f,v.magnitude*.18f);transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(v),55*dt);}
  phase+=dt*Mathf.PI*2*(held?1.2f:Mathf.Lerp(1.4f,3.4f,Mathf.Clamp01(v.magnitude)));
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_TailPhase",phase);block.SetFloat("_TailAmplitude",held?.025f:.055f);skin.SetPropertyBlock(block);}
 }
}
}
