using UnityEngine;

namespace Underwater {
public sealed class ReefDangerZone : MonoBehaviour {
 public SeaLife owner;
 public Collider[] surfaces;
 public LineRenderer[] outlines;
 public Transform cautionIndicator;
 public float proximityDistance=.20f,cautionDistance=.10f,contactDistance=.012f;
 public float warningLeadTime=.15f;
 public struct Signals {public bool glow,caution,warn,contact;}
 bool warned,contactLatched,glowing;float cautionEntered=-1;
 public float SurfaceDistance(Vector3 palm){
  float distance=float.PositiveInfinity;
  if(surfaces!=null)foreach(var surface in surfaces)if(surface&&surface.enabled&&surface.gameObject.activeInHierarchy)
   distance=Mathf.Min(distance,Vector3.Distance(palm,surface.ClosestPoint(palm)));
  return Mathf.Max(0,distance-.04f); // Approximate visible palm radius, in world metres.
 }
 public Signals Step(float distance,float now,bool allowConsequences){
  if(!owner||!owner.danger||distance>proximityDistance+.06f){ResetEpisode();return default;}
  var result=new Signals{glow=owner.proximityEnabled&&distance<=proximityDistance,caution=owner.cautionEnabled&&distance<=cautionDistance};
  if(result.caution&&cautionEntered<0)cautionEntered=now;
  result.warn=result.caution&&!warned;
  if(owner.consequence&&owner.kind!="Moon jelly"&&allowConsequences&&!contactLatched&&distance<=contactDistance&&
   (!owner.cautionEnabled||warned&&now-cautionEntered>=warningLeadTime)){
   result.contact=true;contactLatched=true;
  }
  return result;
 }
 public void MarkWarningSpoken(float now){warned=true;cautionEntered=now;}
 public void ResetEpisode(){warned=false;contactLatched=false;cautionEntered=-1;}
 void Update(){
  var diver=Diver.Instance;
  if(!owner||!owner.isActiveAndEnabled||!diver||diver.MenuOpen||(diver.transform.position-transform.position).sqrMagnitude>36){ResetEpisode();Show(default);return;}
  float left=SurfaceDistance(diver.LeftPalm),right=SurfaceDistance(diver.RightPalm);
  var signals=Step(Mathf.Min(left,right),Time.time,diver.consequences);
  Show(signals);
  if(signals.warn&&diver.TryCaution(owner.Warning(),owner.cautionVoice))MarkWarningSpoken(Time.time);
  if(signals.contact){diver.Hurt(owner.reactionVoice,left<=right);owner.DangerReaction();}
  if(cautionIndicator&&signals.caution&&diver.Head)cautionIndicator.transform.rotation=Quaternion.LookRotation(cautionIndicator.transform.position-diver.Head.position,Vector3.up);
 }
 void Show(Signals signals){
  if(signals.glow!=glowing){glowing=signals.glow;if(outlines!=null)foreach(var outline in outlines)if(outline)outline.enabled=glowing;}
  if(cautionIndicator&&cautionIndicator.gameObject.activeSelf!=signals.caution)cautionIndicator.gameObject.SetActive(signals.caution);
 }
 void OnDisable(){ResetEpisode();Show(default);}
}
}
