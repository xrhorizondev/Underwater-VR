using UnityEngine;

namespace Underwater {
public sealed class ReefDangerZone : MonoBehaviour {
 public SeaLife owner;
 public Collider[] surfaces;
 public LineRenderer[] outlines;
 public Renderer[] glowTargets;
 public Transform cautionIndicator;
 public float proximityDistance=.20f,cautionDistance=.10f,contactDistance=.012f;
 public float warningLeadTime=.15f;
 public struct Signals {public bool glow,caution,warn,contact;}
 bool warned,contactLatched,glowing;float cautionEntered=-1;
 MaterialPropertyBlock[] originalBlocks;Color[] originalColors;MaterialPropertyBlock tint;
 void Start(){if(glowTargets==null||glowTargets.Length==0)return;originalBlocks=new MaterialPropertyBlock[glowTargets.Length];originalColors=new Color[glowTargets.Length];tint=new MaterialPropertyBlock();for(int i=0;i<glowTargets.Length;i++){originalBlocks[i]=new MaterialPropertyBlock();glowTargets[i].GetPropertyBlock(originalBlocks[i]);var material=glowTargets[i].sharedMaterial;originalColors[i]=material&&material.HasProperty("_Color")?material.color:Color.white;}}
 public float SurfaceDistance(Vector3 palm,float handRadius=.04f){
  float distance=float.PositiveInfinity;
  if(surfaces!=null)foreach(var surface in surfaces)if(surface&&surface.enabled&&surface.gameObject.activeInHierarchy)
   distance=Mathf.Min(distance,Vector3.Distance(palm,surface.ClosestPoint(palm)));
  return Mathf.Max(0,distance-handRadius);
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
  float left=Mathf.Min(SurfaceDistance(diver.LeftPalm),SurfaceDistance(diver.LeftFingertip,.012f)),right=Mathf.Min(SurfaceDistance(diver.RightPalm),SurfaceDistance(diver.RightFingertip,.012f));
  var signals=Step(Mathf.Min(left,right),Time.time,diver.consequences);
  Show(signals);
  if(signals.warn&&diver.TryCaution(owner.Warning(),owner.cautionVoice))MarkWarningSpoken(Time.time);
  if(signals.contact){diver.Hurt(owner.reactionVoice,left<=right);owner.DangerReaction();}
  if(cautionIndicator&&signals.caution&&diver.Head)cautionIndicator.transform.rotation=Quaternion.LookRotation(cautionIndicator.transform.position-diver.Head.position,Vector3.up);
 }
 void Show(Signals signals){
  if(signals.glow!=glowing){glowing=signals.glow;if(outlines!=null)foreach(var outline in outlines)if(outline)outline.enabled=glowing;if(originalBlocks!=null)for(int i=0;i<glowTargets.Length;i++){if(!glowTargets[i])continue;if(glowing){glowTargets[i].GetPropertyBlock(tint);tint.SetColor("_Color",Color.Lerp(originalColors[i],new Color(1,.06f,.025f),.45f));glowTargets[i].SetPropertyBlock(tint);}else glowTargets[i].SetPropertyBlock(originalBlocks[i]);}}
  if(cautionIndicator&&cautionIndicator.gameObject.activeSelf!=signals.caution)cautionIndicator.gameObject.SetActive(signals.caution);
 }
 void OnDisable(){ResetEpisode();Show(default);}
}
}
