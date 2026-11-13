using UnityEngine;
namespace Underwater {
// A long squid must clear the reef with its mantle AND its animated arms.
public sealed class SquidSwimmer:MonoBehaviour {
 static readonly float[] Stations={7,4,1,-2,-5,-8,-12,-16};
 static readonly float[] Radii={1.4f,1.95f,2,1.95f,2.1f,2.7f,3.5f,3.9f};
 public float speed=.45f,radius=7;
 Vector3 home;float clock,floorTimer,floorHeight,minimumHeight;Renderer skin;MaterialPropertyBlock block;bool initialized;
 public bool ClearPose(Vector3 position,Quaternion rotation){for(int i=0;i<Stations.Length-1;i++){
  Vector3 a=position+rotation*(Vector3.forward*Stations[i]),b=position+rotation*(Vector3.forward*Stations[i+1]);
  if(Physics.CheckCapsule(a,b,Mathf.Max(Radii[i],Radii[i+1]),ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))return false;
 }return true;}
 float SeabedHeight(Vector3 position,Quaternion rotation){float required=float.NegativeInfinity;for(int i=0;i<Stations.Length;i++){
  Vector3 offset=rotation*(Vector3.forward*Stations[i]),p=position+offset;
  if(Physics.SphereCast(p+Vector3.up*80,Radii[i]+.15f,Vector3.down,out var hit,160,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))required=Mathf.Max(required,p.y+80-hit.distance-offset.y+.2f);
 }return float.IsNegativeInfinity(required)?position.y:required;}
 public void PlaceAboveSeabed(){var p=transform.position;transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);p.y=Mathf.Max(p.y,SeabedHeight(p,transform.rotation));for(int i=0;i<160&&!ClearPose(p,transform.rotation);i++)p.y+=.25f;transform.position=p;}
 public void Initialize(){if(initialized)return;initialized=true;PlaceAboveSeabed();home=transform.position;skin=GetComponent<Renderer>();block=new MaterialPropertyBlock();floorHeight=home.y;minimumHeight=home.y;}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen)return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public void Step(float dt){Initialize();clock+=dt;floorTimer-=dt;
  var goal=home+new Vector3(Mathf.Sin(clock*.055f)*radius,0,Mathf.Cos(clock*.055f)*radius);var direction=Vector3.ProjectOnPlane(goal-transform.position,Vector3.up);if(direction.sqrMagnitude<.001f)direction=transform.forward;
  var rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),8*dt);
  var next=transform.position+rotation*Vector3.forward*speed*dt;
  if(floorTimer<=0){floorTimer=.2f;floorHeight=SeabedHeight(next,rotation);minimumHeight=Mathf.Max(minimumHeight,floorHeight+.2f);}
  float targetY=Mathf.Max(home.y+Mathf.Sin(clock*.12f)*.35f,minimumHeight);next.y=Mathf.MoveTowards(transform.position.y,targetY,.35f*dt);
  if(ClearPose(next,rotation)){transform.SetPositionAndRotation(next,rotation);}
  else {var rise=transform.position+Vector3.up*.35f*dt;if(ClearPose(rise,transform.rotation)){transform.position=rise;minimumHeight=Mathf.Max(minimumHeight,rise.y+.05f);}}
  if(skin){skin.GetPropertyBlock(block);block.SetFloat("_SwimPhase",clock);skin.SetPropertyBlock(block);}
 }
}
}
