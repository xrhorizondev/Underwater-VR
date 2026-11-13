using UnityEngine;
namespace Underwater {
// All jelly pieces deform around the same local origin and pulse phase.
public sealed class ReefJellyMotion:MonoBehaviour {
 public void Initialize(){var block=new MaterialPropertyBlock();foreach(var renderer in GetComponentsInChildren<MeshRenderer>()){
  if(!renderer.sharedMaterial||renderer.sharedMaterial.shader.name!="Underwater/Translucent Jelly Tissue")continue;
  renderer.GetPropertyBlock(block);block.SetVector("_TissueOffset",renderer.transform.localPosition);block.SetFloat("_JellyPhase",transform.position.x);renderer.SetPropertyBlock(block);
 }}
 void Start(){Initialize();}
}
}
