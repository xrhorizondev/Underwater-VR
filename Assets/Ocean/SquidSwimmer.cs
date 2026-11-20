using UnityEngine;
namespace Underwater {
// A long squid must clear the reef with its mantle AND its animated arms.
public sealed class SquidSwimmer:MonoBehaviour {
 static readonly float[] Stations={7,4,1,-2,-5,-8,-12,-16};
 static readonly float[] Radii={1.4f,1.95f,2,1.95f,2.1f,2.7f,3.5f,3.9f};
 public float speed=.45f,radius=7;
 public bool holdDepth;
 public float clearancePadding;
 public void ResetPatrol(Vector3 position,Quaternion rotation,float patrolRadius,bool keepDepth=false){initialized=false;clock=0;floorTimer=0;radius=patrolRadius;holdDepth=keepDepth;transform.SetPositionAndRotation(position,rotation);Initialize();}
 public static float RequiredPadding(Vector3 p){float needed=float.PositiveInfinity;for(int i=0;i<Stations.Length-1;i++){float z=Mathf.Clamp(p.z,Stations[i+1],Stations[i]);needed=Mathf.Min(needed,(p-new Vector3(0,0,z)).magnitude-Mathf.Max(Radii[i],Radii[i+1]));}return Mathf.Max(0,needed);}
 Vector3 home;Quaternion homeRotation;float clock,floorTimer,floorHeight,minimumHeight;Renderer skin;MaterialPropertyBlock block;bool initialized;
 public bool ClearPose(Vector3 position,Quaternion rotation){for(int i=0;i<Stations.Length-1;i++){
  Vector3 a=position+rotation*(Vector3.forward*Stations[i]),b=position+rotation*(Vector3.forward*Stations[i+1]);
  if(Physics.CheckCapsule(a,b,Mathf.Max(Radii[i],Radii[i+1])+clearancePadding,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))return false;
 }return true;}
 float SeabedHeight(Vector3 position,Quaternion rotation){float required=float.NegativeInfinity;for(int i=0;i<Stations.Length;i++){
  Vector3 offset=rotation*(Vector3.forward*Stations[i]),p=position+offset;
  if(Physics.SphereCast(p+Vector3.up*80,Radii[i]+clearancePadding+.15f,Vector3.down,out var hit,160,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))required=Mathf.Max(required,p.y+80-hit.distance-offset.y+.2f);
 }return float.IsNegativeInfinity(required)?position.y:required;}
 public void PlaceAboveSeabed(){var p=transform.position;transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);p.y=Mathf.Max(p.y,SeabedHeight(p,transform.rotation));for(int i=0;i<160&&!ClearPose(p,transform.rotation);i++)p.y+=.25f;transform.position=p;}
 public void Initialize(){if(initialized)return;initialized=true;PlaceAboveSeabed();home=transform.position;homeRotation=transform.rotation;skin=GetComponent<Renderer>();if(skin&&!skin.enabled)skin=null;block=new MaterialPropertyBlock();floorHeight=home.y;minimumHeight=home.y;}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen||!ReefSimulationRange.Active(transform,72))return;Step(Mathf.Min(Time.deltaTime,.05f));}
 public void Step(float dt){Initialize();clock+=dt;floorTimer-=dt;
  if(holdDepth){
   // Hovering squid can translate in either direction with their fins. Keep
   // the long body across the trench instead of orbiting into its shoulders
   // and permanently ratcheting upward after each terrain clearance probe.
   float phase=clock*speed/Mathf.Max(1,radius);
   var target=home+homeRotation*Vector3.forward*(Mathf.Sin(phase)*radius)+Vector3.up*(.15f*Mathf.Sin(clock*.12f));
   var nextPosition=Vector3.MoveTowards(transform.position,target,speed*dt);
   var pose=homeRotation*Quaternion.Euler(0,Mathf.Sin(phase)*2,0);
   if(ClearPose(nextPosition,pose))transform.SetPositionAndRotation(nextPosition,pose);
   return;
  }
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
