using UnityEngine;
namespace Underwater {
// Owns only the new encounters; swimming and hazards keep their existing owners.
public sealed class ReefEncounter:MonoBehaviour {
 public enum Response {Anemone,Octopus,Lionfish}
 public Response response;public Transform[] tentacles;public Renderer[] skin;
 ReefSphereBody body;Vector3 home,escape;Vector3[] rest;Quaternion[] rotations;MaterialPropertyBlock tint;Color[] colors;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 float clock,reaction=-100,cooldown;bool initialized;
 public float Reaction=>Mathf.Clamp01(1-(clock-reaction)/(response==Response.Octopus?6:3));
 public void Initialize(){if(initialized)return;initialized=true;home=transform.position;body=GetComponent<ReefSphereBody>();tint=new MaterialPropertyBlock();
  if(tentacles==null)tentacles=new Transform[0];rest=new Vector3[tentacles.Length];rotations=new Quaternion[tentacles.Length];for(int i=0;i<rest.Length;i++){rest[i]=tentacles[i].localScale;rotations[i]=tentacles[i].localRotation;}
  if(skin==null||skin.Length==0)skin=GetComponentsInChildren<Renderer>();colors=new Color[skin.Length];for(int i=0;i<skin.Length;i++)colors[i]=skin[i].sharedMaterial.color;
 }
 void Start(){Initialize();}
 void Update(){var d=Diver.Instance;if(!d||d.MenuOpen)return;float distance=d.DistanceToHands(transform.position);Vector3 source=(d.LeftPalm-transform.position).sqrMagnitude<(d.RightPalm-transform.position).sqrMagnitude?d.LeftPalm:d.RightPalm;Step(Mathf.Min(Time.deltaTime,.05f),source,distance);}
 public void Step(float dt,Vector3 source,float distance){Initialize();clock+=dt;
  if(distance<(response==Response.Anemone?.65f:1.1f)&&clock>=cooldown){reaction=clock;cooldown=clock+(response==Response.Octopus?9:5);escape=Vector3.ProjectOnPlane(transform.position-source,Vector3.up).normalized;if(escape.sqrMagnitude<.01f)escape=transform.forward;}
  float amount=Reaction;
  if(response==Response.Anemone){for(int i=0;i<tentacles.Length;i++){var scale=rest[i];scale.y*=Mathf.Lerp(1,.28f,amount);tentacles[i].localRotation=rotations[i]*Quaternion.Euler(Mathf.Sin(clock*1.4f+i)*4*(1-amount),0,0);tentacles[i].localScale=Vector3.Lerp(tentacles[i].localScale,scale,1-Mathf.Exp(-dt*5));}return;}
  if(body){
   Vector3 goal=home;if(response==Response.Lionfish)goal+=new Vector3(Mathf.Sin(clock*.18f)*1.6f,Mathf.Sin(clock*.3f)*.15f,Mathf.Cos(clock*.18f)*1.1f);
   Vector3 direction=amount>0?escape:(goal-transform.position).normalized;float speed=amount>0?(response==Response.Octopus?1f:.45f)*amount:response==Response.Lionfish?.2f:Mathf.Min(.3f,(home-transform.position).magnitude);
   direction=navigation.Direction(transform,body,direction,speed,dt);Vector3 delta=direction*speed*dt;
   if(response==Response.Octopus){Vector3 next=transform.position+delta;if(Physics.Raycast(next+Vector3.up*.7f,Vector3.down,out var floor,2,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore)&&floor.normal.y>.45f)delta.y=Mathf.Clamp(floor.point.y+.1f-transform.position.y,-dt*.65f,dt*.65f);else delta=Vector3.zero;}
   navigation.RecordMovement(body.MoveSliding(delta).magnitude,speed*dt,dt);
  }
  if(response==Response.Octopus){for(int i=0;i<skin.Length;i++){if(skin[i].sharedMaterial.shader.name!="Underwater/Octopus Skin")continue;skin[i].GetPropertyBlock(tint);tint.SetColor("_Color",Color.Lerp(colors[i],new Color(.55f,.16f,.35f),amount));skin[i].SetPropertyBlock(tint);}for(int i=0;i<tentacles.Length;i++)tentacles[i].localRotation=rotations[i]*Quaternion.Euler(Mathf.Sin(clock*3+i)*(2+16*amount),0,0);}
 }
}
}
