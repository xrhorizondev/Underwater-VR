using UnityEngine;
namespace Underwater {
// Slow, heavy forward swimming with gradual course changes rather than chasing an orbit.
public sealed class GrouperSwimController:MonoBehaviour {
 public float cruiseSpeed=.30f,roamRadius=4.5f;
 public float ActualSpeed{get;private set;}public float TurnRate{get;private set;}
 ReefSphereBody body;ReefAnimalClearance ground;SeaLife life;WreckCreature encounter;
 readonly ReefRayNavigation navigation=new ReefRayNavigation();
 Vector3 home,goal;float clock,nextCourse,speed=.2f,yawRate,yawAcceleration,pitchRate;int courseIndex;bool ready;
 public void Initialize(){if(ready)return;ready=true;home=transform.position;body=GetComponent<ReefSphereBody>();ground=GetComponent<ReefAnimalClearance>();life=GetComponent<SeaLife>();encounter=GetComponent<WreckCreature>();goal=home+transform.forward*roamRadius;nextCourse=10;}
 void Update(){if(GetComponent<WreckCreature>()?.enabled==true)return;var d=Diver.Instance;if(d&&d.MenuOpen||!ReefSimulationRange.Active(transform))return;Step(Mathf.Min(Time.deltaTime,.05f),d&&d.Head?(Vector3?)d.Head.position:null);}
 public void Step(float dt,Vector3? viewer=null,Vector3? hand=null){
  Initialize();if(dt<=0||life&&life.IsHeld)return;clock+=dt;
  if(hand.HasValue&&encounter&&encounter.HandDistance(hand.Value)<.055f)encounter.React();
  if(clock>nextCourse||(goal-transform.position).sqrMagnitude<1){courseIndex++;float angle=courseIndex*2.399963f+home.z*.13f;goal=home+new Vector3(Mathf.Sin(angle)*roamRadius,Mathf.Sin(angle*.7f)*.2f,Mathf.Cos(angle)*roamRadius);nextCourse=clock+12+Mathf.PerlinNoise(courseIndex,home.z)*6;}
  var desired=(goal-transform.position).normalized;var radial=Vector3.ProjectOnPlane(transform.position-home,Vector3.up);
  if(radial.magnitude>roamRadius*1.6f)desired=Vector3.Lerp(desired,-radial.normalized,.8f).normalized;
  if(viewer.HasValue){var away=transform.position-viewer.Value;float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,2.5f,away.magnitude));if(away.sqrMagnitude>.001f)desired=(desired+away.normalized*weight*1.6f).normalized;}
  var navigated=navigation.Direction(transform,body,desired,Mathf.Max(speed,.45f),dt,true);
  var flat=Vector3.ProjectOnPlane(navigated,Vector3.up).normalized;if(flat.sqrMagnitude<.01f)flat=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
  float error=Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward,Vector3.up),flat,Vector3.up);
  float wantedYaw=Mathf.Clamp(error*.65f,-16,16);float acceleration=Mathf.Clamp((wantedYaw-yawRate)*2,-14,14);yawAcceleration=Mathf.MoveTowards(yawAcceleration,acceleration,30*dt);yawRate+=yawAcceleration*dt;
  var heading=Quaternion.AngleAxis(yawRate*dt,Vector3.up)*Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
  float pitch=Mathf.Asin(Mathf.Clamp(transform.forward.y,-1,1))*Mathf.Rad2Deg;pitch=Mathf.SmoothDampAngle(pitch,Mathf.Asin(Mathf.Clamp(navigated.y,-.12f,.12f))*Mathf.Rad2Deg,ref pitchRate,1.4f,5,dt);
  var forward=heading*Mathf.Cos(pitch*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(pitch*Mathf.Deg2Rad);var rotation=Quaternion.LookRotation(forward,Vector3.up);
  var before=transform.position;var previous=transform.rotation;
  if(!ground||!ground.RequiredHeight(transform.position,rotation,out var height)||height<=transform.position.y+.003f){transform.rotation=rotation;if(!body.Clear(transform.position))transform.rotation=previous;}
  float wanted=cruiseSpeed*(.9f+.1f*Mathf.Sin(clock*.17f+home.z));wanted*=Mathf.Lerp(.55f,1,Mathf.Clamp01((Vector3.Dot(transform.forward,navigated)+1)*.5f));if(encounter&&encounter.Reacting)wanted*=.6f;speed=Mathf.MoveTowards(speed,wanted,.10f*dt);
  var delta=transform.forward*speed*dt;if(ground&&ground.RequiredHeight(transform.position+delta,transform.rotation,out var floor)&&floor>transform.position.y+delta.y)delta.y=Mathf.Min(floor-transform.position.y,.12f*dt);
  body.Move(delta);ActualSpeed=(transform.position-before).magnitude/dt;TurnRate=Quaternion.Angle(previous,transform.rotation)/dt;navigation.RecordMovement((transform.position-before).magnitude,speed*dt,dt);
 }
}
}
