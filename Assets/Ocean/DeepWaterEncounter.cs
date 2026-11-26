using UnityEngine;
namespace Underwater {
// Visual curiosity responses; navigation remains with the existing fish controllers.
public sealed class DeepWaterEncounter:MonoBehaviour {
 public enum Species { Lumen,Angler,CombJelly,MoonJelly }
 public Species species;
 Renderer skin;Renderer[] bulbs;Light glow;ReefSphereBody body;MaterialPropertyBlock block;
 Vector3 home;float clock,remaining;bool ready,wasTouching;
 public float Brightness {get;private set;}
 public void Initialize(){if(ready)return;ready=true;skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();body=GetComponent<ReefSphereBody>();home=transform.position;bulbs=System.Array.FindAll(GetComponentsInChildren<Renderer>(),r=>r.name=="Emissive lure bulb"||r.name=="Lure glow halo");var tip=System.Array.Find(bulbs,r=>r.name=="Emissive lure bulb");glow=species==Species.Angler?(tip?tip.GetComponent<Light>():null):GetComponent<Light>();}
 void Update(){var d=Diver.Instance;if(!d||d.MenuOpen||!ReefSimulationRange.Active(transform))return;float distance=d.DistanceToHands(transform.position);Step(Mathf.Min(Time.deltaTime,.05f),d.Zone,d.Head?d.Head.position:transform.position+Vector3.forward*10,distance);}
 public void Step(float dt,int zone,Vector3 viewer,float handDistance){Initialize();clock+=dt;remaining=Mathf.Max(0,remaining-dt);
  bool touching=handDistance<.28f;if(species==Species.Angler&&touching&&!wasTouching)remaining=2.4f;wasTouching=touching;
  float target=species==Species.Lumen?(zone>=3?.7f:.035f):species==Species.Angler?(remaining>0?1.3f:.07f):0;
  Brightness=Mathf.MoveTowards(Brightness,target,dt*(target>Brightness?.75f:.55f));
  // Fish skin stays natural. Curiosity brightness belongs to a lure, never
  // a cyan emission wash over the companion's texture.
  if(skin&&species!=Species.CombJelly&&species!=Species.MoonJelly){skin.GetPropertyBlock(block);block.SetColor("_BioEmission",Color.clear);skin.SetPropertyBlock(block);}
  if(species==Species.Angler){float response=Mathf.InverseLerp(.07f,1.3f,Brightness);if(glow)glow.intensity=.35f+response*.65f;foreach(var bulb in bulbs){bulb.GetPropertyBlock(block);block.SetColor("_Color",new Color(.3f,1.4f,1.8f)*Mathf.Lerp(1,1.5f,response));bulb.SetPropertyBlock(block);}}
  if((species==Species.CombJelly||species==Species.MoonJelly)&&body){var goal=home+new Vector3(Mathf.Sin(clock*.2f+home.x)*.35f,Mathf.Sin(clock*.3f)*.15f,0);body.MoveSliding(Vector3.ClampMagnitude(goal-transform.position,.22f)*dt);}
 }
}
}
