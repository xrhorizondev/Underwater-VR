using UnityEngine;
namespace Underwater {
// Owns this moray's travel and bone posing; no per-frame mesh baking.
public sealed class CurledMorayEncounter:MonoBehaviour {
 public enum Phase {Resting,Rising,Uncurling,Swimming,Curling,Settling}
 public Animation animationPlayer;public AnimationClip restClip,swimClip;public SkinnedMeshRenderer skin;
 public Collider[] touchVolumes;public Bounds restBounds,travelBounds;
 public float swimSpeed=.48f,escapeDistance=2.7f,contactPadding=.025f;
 public Phase State{get;private set;}=Phase.Resting;
 public Vector3 Destination{get;private set;}public int CompletedEscapes{get;private set;}
 public float LastTravel{get;private set;} public float AnimationClock=>clock; public string LastPlanFailure{get;private set;} public bool CanEscape(Vector3 source){Initialize();return Plan(source);}
 ReefSphereBody body;ReefAnimalClearance clearance;ReefCreatureInfo info;Transform[] bones;
 Quaternion[] restPose,targetPose;Vector3[] bonePositions,boneScales;
 float stateTime,clock,cooldown,travelTime,blockedTime;bool ready,contactLatch;
 Vector3 cruiseGoal,riseGoal;Quaternion flightRotation;ReefRayNavigation navigation=new ReefRayNavigation();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();clearance=GetComponent<ReefAnimalClearance>();info=GetComponent<ReefCreatureInfo>();animationPlayer.enabled=false;animationPlayer.playAutomatically=false;bones=skin.bones;restPose=new Quaternion[bones.Length];targetPose=new Quaternion[bones.Length];bonePositions=new Vector3[bones.Length];boneScales=new Vector3[bones.Length];restClip.SampleAnimation(animationPlayer.gameObject,0);for(int i=0;i<bones.Length;i++){restPose[i]=bones[i].localRotation;bonePositions[i]=bones[i].localPosition;boneScales[i]=bones[i].localScale;}clearance.animatedBody=restBounds;body.centerOffset=restBounds.center;}
 void Update(){var diver=Diver.Instance;if(!diver||diver.MenuOpen||!ReefSimulationRange.Active(transform))return;Step(Mathf.Min(Time.deltaTime,.05f),diver.LeftPalm,diver.RightPalm,diver.LeftFingertip,diver.RightFingertip);if(!UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).isValid&&Input.GetMouseButtonDown(0)&&diver.Head){var camera=diver.Head.GetComponent<Camera>();if(camera)TryDesktopTouch(camera.ScreenPointToRay(Input.mousePosition));}}
 public bool TryDesktopTouch(Ray ray,float maxDistance=2f){
  Initialize();var diver=Diver.Instance;if(diver&&diver.MenuOpen)return false;
  if(!Physics.Raycast(ray,out var hit,maxDistance,(1<<2)|(1<<8)|(1<<9),QueryTriggerInteraction.Collide))return false;
  if(touchVolumes!=null)foreach(var volume in touchVolumes)if(volume&&hit.collider==volume)return RequestTouch(hit.point);
  return false;
 }
 public float HandDistance(Vector3 hand){float d=float.PositiveInfinity;if(touchVolumes!=null)foreach(var c in touchVolumes)if(c&&c.enabled&&c.gameObject.activeInHierarchy)d=Mathf.Min(d,Vector3.Distance(hand,c.ClosestPoint(hand)));return d;}
 public void Step(float dt,Vector3? left=null,Vector3? right=null,Vector3? leftTip=null,Vector3? rightTip=null){
 Initialize();if(dt<=0)return;clock+=dt;stateTime+=dt;cooldown=Mathf.Max(0,cooldown-dt);
 float nearest=float.PositiveInfinity;Vector3 source=transform.position;
 if(left.HasValue){float d=HandDistance(left.Value);if(d<nearest){nearest=d;source=left.Value;}}if(right.HasValue){float d=HandDistance(right.Value);if(d<nearest){nearest=d;source=right.Value;}}if(leftTip.HasValue){float d=HandDistance(leftTip.Value);if(d<nearest){nearest=d;source=leftTip.Value;}}if(rightTip.HasValue){float d=HandDistance(rightTip.Value);if(d<nearest){nearest=d;source=rightTip.Value;}}
 if(nearest>.10f)contactLatch=false;if(nearest<=contactPadding&&!contactLatch){contactLatch=RequestTouch(source);}
 var before=transform.position;
 switch(State){
 case Phase.Resting:Pose(0);break;
 case Phase.Rising:
  Pose(0);MoveToward(riseGoal,.35f,dt);if((transform.position-riseGoal).magnitude<.025f){clearance.animatedBody=travelBounds;body.centerOffset=travelBounds.center;SetPhase(Phase.Uncurling);}else if(stateTime>6)BeginRecovery();break;
 case Phase.Uncurling:
  Pose(Mathf.SmoothStep(0,1,stateTime/1.6f));var turn=Quaternion.RotateTowards(transform.rotation,flightRotation,55*dt);if(BoxClear(transform.position,turn,travelBounds))transform.rotation=turn;if(stateTime>=1.6f&&Quaternion.Angle(transform.rotation,flightRotation)<3)SetPhase(Phase.Swimming);else if(stateTime>6)BeginRecovery();break;
 case Phase.Swimming:
  Pose(1);travelTime+=dt;var delta=cruiseGoal-transform.position;
  if(delta.magnitude<.12f){SetPhase(Phase.Curling);break;}
  var direction=navigation.Direction(transform,body,delta.normalized,swimSpeed,dt);var rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,Vector3.up).normalized,Vector3.up),55*dt);
  if(BoxClear(transform.position,rotation,travelBounds))transform.rotation=rotation;
  // Travel follows the head's +Z axis while vertical correction keeps the whole body clear.
  var movement=transform.forward*swimSpeed*dt;movement.y=Mathf.Clamp(delta.y,-swimSpeed*.6f*dt,swimSpeed*.6f*dt);SafeMove(movement,dt);
  float moved=(transform.position-before).magnitude;blockedTime=moved<dt*.025f?blockedTime+dt:0;navigation.RecordMovement(moved,swimSpeed*dt,dt);
  if(blockedTime>2.5f||travelTime>22)BeginRecovery();break;
 case Phase.Curling:
  Pose(1-Mathf.SmoothStep(0,1,stateTime/2));if(stateTime>=2){clearance.animatedBody=restBounds;body.centerOffset=restBounds.center;if(GroundAt(transform.position,transform.rotation,restBounds,out var landing))Destination=landing;else Destination=transform.position;SetPhase(Phase.Settling);}break;
 case Phase.Settling:
  Pose(0);MoveToward(Destination,.22f,dt);if((transform.position-Destination).magnitude<.025f||stateTime>8){CompletedEscapes++;cooldown=5;SetPhase(Phase.Resting);}break;
 }
 if(State!=Phase.Resting)LastTravel+=(transform.position-before).magnitude;
 }
 public bool RequestTouch(Vector3 source){Initialize();if(State!=Phase.Resting||cooldown>0)return false;if(!Plan(source)&&!Plan(transform.position+transform.forward*.5f)){cooldown=2;return false;}LastTravel=0;travelTime=0;blockedTime=0;navigation=new ReefRayNavigation();if(info)info.Reveal();SetPhase(Phase.Rising);return true;}
 bool Plan(Vector3 source){ LastPlanFailure="No clear landing/route";
 Vector3 away=Vector3.ProjectOnPlane(transform.position-source,Vector3.up);if(away.sqrMagnitude<.001f)away=-transform.forward;away.Normalize();
 if(!GroundAt(transform.position,transform.rotation,travelBounds,out var startGround)){LastPlanFailure="No start ground";return false;}
 Vector3 rise=transform.position;rise.y=Mathf.Max(rise.y,startGround.y+.28f);
 if(!SweepClear(transform.position,rise,transform.rotation,restBounds)){LastPlanFailure="Rise path blocked";return false;}
 float[] angles={0,15,-15,30,-30,45,-45,60,-60,75,-75,90,-90,105,-105,120,-120,135,-135,150,-150,165,-165,180};
 for(int ring=0;ring<3;ring++)foreach(float angle in angles){var dir=Quaternion.AngleAxis(angle,Vector3.up)*away;var rot=Quaternion.LookRotation(dir,Vector3.up);var p=transform.position+dir*(escapeDistance+ring*.65f);
  if(!GroundAt(p,rot,restBounds,out var landing)||!GroundAt(p,rot,travelBounds,out var flying))continue;flying.y=Mathf.Max(flying.y+.28f,rise.y);var high=rise;high.y=flying.y;
  if(!BoxClear(high,rot,travelBounds)||!BoxClear(flying,rot,travelBounds)||!BoxClear(landing,rot,restBounds))continue;
  if(!SweepClear(rise,high,transform.rotation,restBounds)||!SweepClear(high,flying,rot,travelBounds)||!SweepClear(flying,landing,rot,restBounds))continue;
  // Validate intermediate turn headings before a curled body straightens beside rock.
  bool turnClear=true;for(int j=0;j<=6;j++)if(!BoxClear(high,Quaternion.Slerp(transform.rotation,rot,j/6f),travelBounds)){turnClear=false;break;}if(!turnClear)continue;
  Destination=landing;cruiseGoal=flying;riseGoal=high;flightRotation=rot;LastPlanFailure="";return true;
 }
 return false;
 }
 bool GroundAt(Vector3 p,Quaternion rotation,Bounds envelope,out Vector3 grounded){var old=clearance.animatedBody;clearance.animatedBody=envelope;bool ok=clearance.RequiredHeight(p,rotation,out float y);clearance.animatedBody=old;grounded=new Vector3(p.x,y+.006f,p.z);return ok;}
 bool BoxClear(Vector3 p,Quaternion rotation,Bounds bounds){var scale=transform.lossyScale;var ext=Vector3.Scale(bounds.extents,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));return !Physics.CheckBox(p+rotation*Vector3.Scale(bounds.center,scale),ext*.98f,rotation,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);}
 bool SweepClear(Vector3 from,Vector3 to,Quaternion rotation,Bounds bounds){if(!BoxClear(to,rotation,bounds))return false;var delta=to-from;if(delta.sqrMagnitude<.000001f)return true;var scale=transform.lossyScale;var ext=Vector3.Scale(bounds.extents,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))*.98f;return !Physics.BoxCast(from+rotation*Vector3.Scale(bounds.center,scale),ext,delta.normalized,out _,rotation,delta.magnitude,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);}
 void MoveToward(Vector3 goal,float speed,float dt){SafeMove(Vector3.ClampMagnitude(goal-transform.position,speed*dt),dt);}
 void SafeMove(Vector3 delta,float dt){var b=clearance.animatedBody;var scale=transform.lossyScale;var ext=Vector3.Scale(b.extents,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))*.98f;float d=delta.magnitude;if(d>.000001f&&Physics.BoxCast(transform.position+transform.TransformVector(b.center),ext,delta/d,out var hit,transform.rotation,d+.012f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))delta=delta/d*Mathf.Max(0,hit.distance-.012f);body.MoveSliding(delta);}
 void BeginRecovery(){if(State==Phase.Rising){clearance.animatedBody=restBounds;body.centerOffset=restBounds.center;if(GroundAt(transform.position,transform.rotation,restBounds,out var ground))Destination=ground;else Destination=transform.position;SetPhase(Phase.Settling);return;}SetPhase(Phase.Curling);}
 void SetPhase(Phase phase){State=phase;stateTime=0;}
 void Pose(float amount){
 // Sample exactly one travel/rest pair and blend only 22 bones. Root travel remains collision-controlled.
 restClip.SampleAnimation(animationPlayer.gameObject,Mathf.Repeat(clock,restClip.length));for(int i=0;i<bones.Length;i++)restPose[i]=bones[i].localRotation;
 if(amount>0){swimClip.SampleAnimation(animationPlayer.gameObject,Mathf.Repeat(clock,swimClip.length));for(int i=0;i<bones.Length;i++)targetPose[i]=bones[i].localRotation;}
 for(int i=0;i<bones.Length;i++){bones[i].localPosition=bonePositions[i];bones[i].localScale=boneScales[i];bones[i].localRotation=amount>0?Quaternion.Slerp(restPose[i],targetPose[i],amount):restPose[i];}
 }
}
}





