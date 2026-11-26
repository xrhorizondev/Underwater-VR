using UnityEngine;
namespace Underwater {
// Optional world-space facts. Curiosity reactions never depend on showing UI.
public sealed class ReefCreatureInfo:MonoBehaviour {
 public string creatureName,fact;public AudioClip narration;public bool cardsEnabled=true;
 public float observationRange=3.2f;
 public Vector3 localFocus;public float bodyRadius=.3f;public bool fittedFocus;
 static ReefCreatureInfo active;
 public Vector3 Focus=>fittedFocus?transform.TransformPoint(localFocus):transform.position;
 Transform card;TextMesh text;Renderer panel;MaterialPropertyBlock block;float until,opacity,gaze;static float nextNarration;
 public bool Visible=>opacity>.01f;
 public void Initialize(){if(card)return;var root=new GameObject("Optional creature fact");card=root.transform;card.SetParent(transform,false);
  var backing=new GameObject("Fact backing");backing.transform.SetParent(card,false);var mesh=new Mesh{name="Creature fact panel"};mesh.vertices=new[]{new Vector3(-.32f,-.11f,.01f),new Vector3(.32f,-.11f,.01f),new Vector3(-.32f,.11f,.01f),new Vector3(.32f,.11f,.01f)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateBounds();backing.AddComponent<MeshFilter>().sharedMesh=mesh;panel=backing.AddComponent<MeshRenderer>();panel.allowOcclusionWhenDynamic=false;panel.sharedMaterial=Resources.Load<Material>("CreatureFactPanel");block=new MaterialPropertyBlock();
  var label=new GameObject("Name and fact");label.transform.SetParent(card,false);label.transform.localPosition=new Vector3(-.29f,.075f,0);text=label.AddComponent<TextMesh>();text.text=creatureName+"\n"+Wrap(fact);text.GetComponent<Renderer>().allowOcclusionWhenDynamic=false;text.fontSize=64;text.characterSize=.0065f;text.anchor=TextAnchor.UpperLeft;text.alignment=TextAlignment.Left;card.gameObject.SetActive(false);
 }
 static string Wrap(string value){string result="",line="";foreach(string word in (value??"").Split(' ')){if(line.Length+word.Length+1>28){result+=line+"\n";line=word;}else line+=(line.Length>0?" ":"")+word;}return result+line;}
 public void Reveal(){var d=Diver.Instance;if(!cardsEnabled||!d||!d.ShowCreatureFacts||d.MenuOpen)return;Initialize();if(active&&active!=this)active.until=0;active=this;until=Time.time+5;if(narration&&!d.VoicePlaying&&Time.time>=nextNarration){nextNarration=Time.time+8;d.Say(creatureName+" — "+fact,narration);}}
 void Update(){var d=Diver.Instance;if(!d||!d.Head)return;bool allowed=cardsEnabled&&d.ShowCreatureFacts&&!d.MenuOpen;Vector3 to=Focus-d.Head.position;
  bool watching=allowed&&to.sqrMagnitude<Mathf.Pow(observationRange+bodyRadius,2)&&Vector3.Dot(d.Head.forward,to.normalized)>.965f;gaze=watching?gaze+Time.deltaTime:0;if(gaze>.8f){Reveal();gaze=-8;}
  if(!card)return;opacity=Mathf.MoveTowards(opacity,allowed&&Time.time<until?1:0,Time.deltaTime*2);if(opacity<=.01f){if(card.gameObject.activeSelf)card.gameObject.SetActive(false);return;}Position(d.Head);SetOpacity();
 }
 void Position(Transform viewer){Vector3 direction=(Focus-viewer.position).normalized;float distance=Mathf.Clamp(Vector3.Distance(Focus,viewer.position),.85f,2.3f);var local=viewer.InverseTransformDirection(direction);local.z=Mathf.Max(.4f,local.z);local.x=Mathf.Clamp(local.x/local.z,-.35f,.35f);local.y=Mathf.Clamp(local.y/local.z,-.25f,.2f);direction=viewer.TransformDirection(new Vector3(local.x,local.y,1).normalized);card.position=viewer.position+direction*distance+viewer.up*.22f;card.rotation=Quaternion.LookRotation(card.position-viewer.position,viewer.up);var scale=transform.lossyScale;card.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));}
 void SetOpacity(){card.gameObject.SetActive(opacity>.01f);text.color=new Color(.92f,.98f,1,opacity);block.SetColor("_Color",new Color(.015f,.045f,.06f,.72f*opacity));panel.SetPropertyBlock(block);}
 public void Preview(Transform viewer){Initialize();opacity=1;Position(viewer);SetOpacity();}
}
}
