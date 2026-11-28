using System;
using System.Collections.Generic;
using UnityEngine;
namespace Underwater {
// WreckCreature calls Step once: collision-controlled travel first, one bone pose second.
public sealed class DolphinSwimController:MonoBehaviour {
 public enum SwimMode {Together,Independent,Regrouping}
 public bool compactPatrol;public Vector3 compactCenter;public Vector2 compactRadii=new Vector2(4.5f,4);public float compactSpeed=.58f,compactStartAngle;float compactPhase;
 public int podIndex;public Animation animationPlayer;public SkinnedMeshRenderer skin;public DolphinSwimController[] pod;public ReefImportedAnimation suppliedSwim;
 public float referenceSpeed=.95f;public float minimumSeparation=3.9f;public float bodyLength=3.4f;public float recoveryDelay=.75f;public int ClearanceRecoveries{get;private set;}public bool RecoveryActive=>recoveryTime>0;public float RequiredTurnLift{get;private set;}public bool RotationBlocked{get;private set;}
 public SwimMode Mode{get;private set;}public float ActualSpeed{get;private set;}public float Clock{get;private set;}
 public float StrokePhase{get;private set;}public float StrokeRate{get;private set;}public float Effort{get;private set;}public float TurnRate{get;private set;}
 public bool Gliding{get;private set;}public float SteeringAcceleration=>yawAcceleration;public float Blink{get;private set;}public float MouthAngle{get;private set;}public float FormationWeight{get;private set;}=1;
 static readonly string[] TailNames={"TALE04","TALE03","TALE02","TALE01"},Sides={"L","R"};static readonly float[] TailAmplitudes={3,4.8f,7,9.5f};
 ReefSphereBody body;ReefAnimalClearance ground;ReefLargeAnimalPatrol patrol;WreckCreature encounter;readonly ReefRayNavigation navigation=new ReefRayNavigation();
 [SerializeField,HideInInspector] Transform[] authoredBones;[SerializeField,HideInInspector] Quaternion[] authoredRotations;[SerializeField,HideInInspector] Vector3[] authoredPositions,authoredScales;int blinkLeft=-1,blinkRight=-1;Vector3 focus,course;readonly Collider[] recoveryContacts=new Collider[32];Vector3 recoveryDirection;float stalledTime,recoveryTime;float speed=.65f,bank,yawVelocity,yawAcceleration,pitchVelocity,bankVelocity,gaitTime=4,drive=1,finClock,finTravel;bool ready;Transform[] bones;Quaternion[] rest;Vector3[] positions,scales;readonly Dictionary<string,Transform> named=new Dictionary<string,Transform>();
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();patrol=GetComponent<ReefLargeAnimalPatrol>();ground=GetComponent<ReefAnimalClearance>();encounter=GetComponent<WreckCreature>();focus=encounter.habitatCenter;animationPlayer.enabled=false;animationPlayer.playAutomatically=false;bones=animationPlayer.GetComponentsInChildren<Transform>();rest=new Quaternion[bones.Length];positions=new Vector3[bones.Length];scales=new Vector3[bones.Length];bool authored=authoredBones!=null&&authoredBones.Length==bones.Length&&authoredRotations!=null&&authoredRotations.Length==bones.Length;for(int i=0;i<bones.Length;i++){named[bones[i].name]=bones[i];rest[i]=authored&&authoredBones[i]==bones[i]?authoredRotations[i]:bones[i].localRotation;positions[i]=authored&&authoredBones[i]==bones[i]?authoredPositions[i]:bones[i].localPosition;scales[i]=authored&&authoredBones[i]==bones[i]?authoredScales[i]:bones[i].localScale;}if(!authored){authoredBones=bones;authoredRotations=(Quaternion[])rest.Clone();authoredPositions=(Vector3[])positions.Clone();authoredScales=(Vector3[])scales.Clone();}blinkLeft=skin.sharedMesh.GetBlendShapeIndex("BlinkLeft");blinkRight=skin.sharedMesh.GetBlendShapeIndex("BlinkRight");StrokePhase=podIndex*.8f;finClock=podIndex*.7f;course=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;if(course.sqrMagnitude<.1f)course=Vector3.forward;speed=referenceSpeed*.9f;gaitTime=3.8f+podIndex*.9f;compactPhase=compactStartAngle;}
 public void Step(float dt,Vector3? viewer=null,Vector3? hand=null){
  Initialize();if(dt<=0)return;Clock+=dt;if(patrol){var old=transform.forward;patrol.Step(dt);ActualSpeed=patrol.ActualSpeed;TurnRate=Vector3.SignedAngle(old,transform.forward,Vector3.up)/dt;StepVisual(dt,ActualSpeed,TurnRate,viewer);return;}if(compactPatrol){StepCompact(dt,viewer,hand);return;}float length=Mathf.Max(1,bodyLength),seed=31.7f+podIndex*17.31f;
  if(hand.HasValue&&encounter.HandDistance(hand.Value)<.16f)encounter.React();
  // Correlated course drift, not a point moving around a timed circle.
  float drift=(Mathf.PerlinNoise(seed,Clock*.024f)-.5f)*3.6f;
  course=Quaternion.AngleAxis(drift*dt,Vector3.up)*course;
  var offset=transform.position-focus;var ahead=offset+course*(length*2.4f);
  float rx=Mathf.Max(10,length*3.8f),rz=Mathf.Max(9,length*3.3f);
  float edge=Mathf.Sqrt(ahead.x*ahead.x/(rx*rx)+ahead.z*ahead.z/(rz*rz));
  float returnWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1.05f,edge));
  var inward=new Vector3(-ahead.x/(rx*rx),0,-ahead.z/(rz*rz)).normalized;
  var desired=Vector3.Lerp(course,inward,returnWeight*.86f);if(desired.sqrMagnitude<.01f)desired=transform.forward;desired.Normalize();
  Vector3 alignment=Vector3.zero,center=Vector3.zero,separation=Vector3.zero;int neighbours=0;float nearest=float.MaxValue;
  if(pod!=null)foreach(var other in pod)if(other&&other!=this&&other.enabled&&other.gameObject.activeInHierarchy){
   var away=transform.position-other.transform.position;float distance=away.magnitude;nearest=Mathf.Min(nearest,distance);
   if(distance<length*5){float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(length*2,length*5,distance));alignment+=other.transform.forward*weight;center+=other.transform.position;neighbours++;}
   float safe=Mathf.Max(minimumSeparation+length*.5f,body.radius+other.GetComponent<ReefSphereBody>().radius+.18f);
   // Anticipate convergence instead of sliding sideways at a separation boundary.
   var future=away+(transform.forward*speed-other.transform.forward*other.ActualSpeed)*Mathf.Min(6,length*1.6f/referenceSpeed);
   float urgency=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(safe,safe+length*.8f,Mathf.Min(distance,future.magnitude)));
   if(distance>.01f&&urgency>0)separation+=away/distance*urgency;
  }
  float affinity=.15f+.22f*Mathf.PerlinNoise(seed+19,Clock*.012f);FormationWeight=neighbours>0?affinity:0;
  Mode=neighbours==0?SwimMode.Independent:nearest>length*2.7f?SwimMode.Regrouping:alignment.sqrMagnitude>.01f&&Vector3.Dot(transform.forward,alignment.normalized)>.35f?SwimMode.Together:SwimMode.Independent;
  if(neighbours>0){var to=center/neighbours-transform.position;var cohesion=to.magnitude>length*2?Vector3.ProjectOnPlane(to,Vector3.up).normalized*.22f:Vector3.zero;desired=(desired+alignment.normalized*affinity+cohesion*affinity+separation*2.4f).normalized;}
  if(viewer.HasValue){var away=transform.position-viewer.Value;float reach=length*.9f;float avoidance=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(reach*.55f,reach,away.magnitude));if(away.sqrMagnitude>.001f)desired=(desired+Vector3.ProjectOnPlane(away,Vector3.up).normalized*avoidance*.85f).normalized;}
  float boundaryReturn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,1.5f,edge));
  desired=Vector3.Lerp(desired,inward,boundaryReturn*.95f);
  if(desired.sqrMagnitude<.01f)desired=transform.forward;
  // Look ahead several body lengths; filter the avoidance course into curvature.
  var navigated=navigation.Direction(transform,body,desired,Mathf.Max(speed,length*.46f),dt);
  float urgencyNear=0;
  if(Physics.SphereCast(body.WorldCenter,body.radius,transform.forward,out var danger,length*2.7f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))urgencyNear=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(length*.35f,length*2.2f,danger.distance));
  urgencyNear=Mathf.Max(urgencyNear,Mathf.Clamp01(separation.magnitude*.7f));
  urgencyNear=Mathf.Max(urgencyNear,boundaryReturn*.65f);
  var flat=Vector3.ProjectOnPlane(navigated,Vector3.up).normalized;if(flat.sqrMagnitude<.01f)flat=course;
  float desiredDepth=focus.y+(Mathf.PerlinNoise(seed+8,Clock*.018f)-.5f)*length*.9f;
  float vertical=Mathf.Clamp((desiredDepth-transform.position.y)*.06f,-.09f,.09f);
  var look=transform.position+flat*(length*2.6f);if(ground.RequiredHeight(look,transform.rotation,out float floorAhead))vertical=Mathf.Max(vertical,Mathf.Clamp((floorAhead+length*.24f-transform.position.y)/(length*2.1f),0,.24f));
  vertical=Mathf.Clamp(vertical+navigated.y*.25f,-.18f,.25f);
  if(transform.position.y>6-ground.animatedBody.extents.y)vertical=Mathf.Min(vertical,-.12f);
  gaitTime-=dt;if(gaitTime<=0){Gliding=!Gliding;gaitTime=Gliding?1.8f+Mathf.PerlinNoise(seed+40,Clock*.11f)*2.3f:3.4f+Mathf.PerlinNoise(seed+60,Clock*.13f)*3.8f;}
  float driveTarget=Gliding&&urgencyNear<.35f&&separation.sqrMagnitude<.4f?0:1;drive=Mathf.MoveTowards(drive,driveTarget,dt*.65f);
  float wanted=referenceSpeed*(.91f+Mathf.PerlinNoise(seed+50,Clock*.032f)*.12f)*Mathf.Lerp(.80f,1.06f,drive);
  if(encounter.Reacting)wanted+=referenceSpeed*.08f;
  if(separation.sqrMagnitude>.15f)wanted*=.90f;
  speed=Mathf.MoveTowards(speed,wanted,length*.035f*dt);
  var previousForward=transform.forward;float headingError=Vector3.SignedAngle(Vector3.ProjectOnPlane(previousForward,Vector3.up),flat,Vector3.up);
  float radius=length*Mathf.Lerp(1.9f,.65f,urgencyNear);float maxYaw=Mathf.Max(3,speed/radius*Mathf.Rad2Deg);
  float wantedYaw=Mathf.Clamp(headingError/2.2f,-maxYaw,maxYaw);float maxAcceleration=Mathf.Lerp(3,9,urgencyNear);
  float requestedAcceleration=Mathf.Clamp((wantedYaw-yawVelocity)*1.25f,-maxAcceleration,maxAcceleration);
  yawAcceleration=Mathf.MoveTowards(yawAcceleration,requestedAcceleration,Mathf.Lerp(5,14,urgencyNear)*dt);yawVelocity+=yawAcceleration*dt;
  var forwardFlat=Quaternion.AngleAxis(yawVelocity*dt,Vector3.up)*Vector3.ProjectOnPlane(previousForward,Vector3.up).normalized;
  float pitch=Mathf.Asin(Mathf.Clamp(previousForward.y,-1,1))*Mathf.Rad2Deg;float targetPitch=Mathf.Atan(vertical)*Mathf.Rad2Deg;
  pitch=Mathf.SmoothDampAngle(pitch,targetPitch,ref pitchVelocity,2.4f,4,dt);
  var forward=forwardFlat*Mathf.Cos(pitch*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(pitch*Mathf.Deg2Rad);
  float targetBank=Mathf.Clamp(-yawVelocity*.35f,-5,5);bank=Mathf.SmoothDampAngle(bank,targetBank,ref bankVelocity,2,3,dt);
  var rotation=Quaternion.LookRotation(forward,Vector3.up)*Quaternion.AngleAxis(bank,Vector3.forward);var before=body.WorldCenter;
    RequiredTurnLift=0;RotationBlocked=false;
  if(ground.RequiredHeight(transform.position,rotation,out float height)&&height>transform.position.y+.00005f){RequiredTurnLift=height-transform.position.y;body.Move(Vector3.up*Mathf.Min(height-transform.position.y+.002f,referenceSpeed*.16f*dt));}
  else {var previousRotation=transform.rotation;transform.rotation=rotation;if(!body.Clear(transform.position)){transform.rotation=previousRotation;RotationBlocked=true;}}
  var movement=transform.forward*speed*dt;
  // Slow forward travel before a close crossing; keep steering smoothly while yielding.
  if(pod!=null)foreach(var other in pod)if(other&&other!=this&&other.enabled){var away=transform.position-other.transform.position;float gap=away.magnitude;float safe=minimumSeparation*.82f;if(Vector3.Dot(movement,away)<0&&gap<safe+1.2f)movement*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(safe,safe+1.2f,gap));}
  if(ground.RequiredHeight(transform.position+movement,transform.rotation,out float h)&&h>transform.position.y+movement.y+.008f)movement=Vector3.up*Mathf.Min(h-transform.position.y+.01f,referenceSpeed*.16f*dt);
  // A blocked animal slows and steers; it does not skate along a wall.
  if(recoveryTime>0)RecoverClearance(dt);else body.Move(movement);ActualSpeed=(body.WorldCenter-before).magnitude/dt;TurnRate=Vector3.SignedAngle(previousForward,transform.forward,Vector3.up)/dt;
  stalledTime=ActualSpeed<speed*.05f?stalledTime+dt:0;if(recoveryTime<=0&&stalledTime>recoveryDelay){recoveryTime=1.2f;recoveryDirection=Vector3.zero;stalledTime=0;ClearanceRecoveries++;}
  navigation.RecordMovement((body.WorldCenter-before).magnitude,speed*dt,dt);StepVisual(dt,ActualSpeed,TurnRate,viewer);
 }
 void StepCompact(float dt,Vector3? viewer,Vector3? hand){
  if(hand.HasValue&&encounter.HandDistance(hand.Value)<.16f)encounter.React();
  gaitTime-=dt;if(gaitTime<=0){Gliding=!Gliding;gaitTime=Gliding?2.4f:4.5f+podIndex*.4f;}drive=Mathf.MoveTowards(drive,Gliding?0:1,dt*.65f);
  float wanted=compactSpeed*Mathf.Lerp(.9f,1.04f,drive);int members=1;
  if(pod!=null)foreach(var peer in pod)if(peer&&peer!=this&&peer.enabled&&peer.gameObject.activeInHierarchy){members++;if(peer.compactPatrol){float gap=Mathf.Repeat(peer.compactPhase-compactPhase,Mathf.PI*2)*(compactRadii.x+compactRadii.y)*.5f;if(gap<minimumSeparation+4)wanted*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(minimumSeparation*.95f,minimumSeparation+4,gap));}}
  if(viewer.HasValue&&(viewer.Value-transform.position).sqrMagnitude<6.25f)wanted*=.65f;
  Mode=members>1?SwimMode.Together:SwimMode.Independent;FormationWeight=members>1?.2f:0;
  speed=Mathf.MoveTowards(speed,wanted,.06f*dt);float previousPhase=compactPhase;
  var tangent=new Vector3(compactRadii.x*Mathf.Cos(compactPhase),0,-compactRadii.y*Mathf.Sin(compactPhase));compactPhase=Mathf.Repeat(compactPhase+speed*dt/Mathf.Max(.1f,tangent.magnitude),Mathf.PI*2);
  var target=compactCenter+new Vector3(compactRadii.x*Mathf.Sin(compactPhase),Mathf.Sin(compactPhase+podIndex)*.08f,compactRadii.y*Mathf.Cos(compactPhase));
  tangent=new Vector3(compactRadii.x*Mathf.Cos(compactPhase),Mathf.Cos(compactPhase+podIndex)*.08f,-compactRadii.y*Mathf.Sin(compactPhase)).normalized;
  var before=body.WorldCenter;var oldForward=transform.forward;var oldRotation=transform.rotation;var rotation=Quaternion.LookRotation(tangent,Vector3.up);
  transform.rotation=rotation;if(!body.Clear(transform.position))transform.rotation=oldRotation;
  var delta=target-transform.position;body.Move(Vector3.ClampMagnitude(delta,speed*dt+.001f));
  ActualSpeed=(body.WorldCenter-before).magnitude/dt;if(ActualSpeed<.005f)compactPhase=previousPhase;
  TurnRate=Vector3.SignedAngle(oldForward,transform.forward,Vector3.up)/dt;StepVisual(dt,ActualSpeed,TurnRate,viewer);
 }
 void RecoverClearance(float dt){
  recoveryTime=Mathf.Max(0,recoveryTime-dt);
  if(recoveryDirection.sqrMagnitude<.01f){
   var center=body.WorldCenter;int count=Physics.OverlapSphereNonAlloc(center,body.radius+.16f,recoveryContacts,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore);Vector3 away=Vector3.zero;
   for(int i=0;i<count;i++){var surface=recoveryContacts[i].ClosestPoint(center);var normal=center-surface;float gap=normal.magnitude-body.radius;if(normal.sqrMagnitude>.000001f&&gap<.16f)away+=normal.normalized*(.16f-gap);}
   if(RequiredTurnLift>0)away+=Vector3.up*.08f;
   recoveryDirection=away.sqrMagnitude>.000001f?away.normalized:Vector3.up;
  }
  // Briefly make room at a blocked boundary before resuming forward travel.
  // ReefSphereBody still performs every sweep and seabed constraint.
  body.Move(recoveryDirection*Mathf.Min(referenceSpeed*.2f,.2f)*dt);
 }
 public void StepVisual(float dt,float actualSpeed,float turnRate,Vector3? viewer=null){
 if(suppliedSwim){suppliedSwim.MatchTravel(actualSpeed,compactPatrol?compactSpeed:referenceSpeed);return;}
 Initialize();finClock+=dt;float travel=Mathf.Clamp01(actualSpeed/referenceSpeed);finTravel=Mathf.MoveTowards(finTravel,travel,dt*.65f);
 // Keep a low-amplitude travelling wave during a glide, rather than freezing a bent tail.
 float moving=travel*Mathf.Lerp(.55f,1,drive);Effort=Mathf.MoveTowards(Effort,moving,dt*.8f);float rate=actualSpeed<.015f?0:Mathf.Clamp(.22f+actualSpeed/Mathf.Max(1,bodyLength*1.1f),.22f,.9f)*Mathf.Lerp(.72f,1,drive);StrokeRate=Mathf.MoveTowards(StrokeRate,rate,dt*.4f);StrokePhase+=dt*StrokeRate*Mathf.PI*2;
 float blinkTime=Mathf.Repeat(Clock+podIndex*1.37f,5.4f+podIndex*.37f);Blink=blinkTime<.25f?Mathf.Sin(blinkTime/.25f*Mathf.PI):0;
 MouthAngle=-10+1.8f*(.5f+.5f*Mathf.Sin(Clock*1.15f+podIndex))+(encounter&&encounter.Reacting?3:0);
 ApplyPose(StrokePhase,Effort,Mathf.Clamp(turnRate/28,-1,1),Blink,MouthAngle,viewer);
 }
 public void ApplyPose(float phase,float effort,float turn,float blink,float jawAngle,Vector3? viewer=null){
 Initialize();if(blinkLeft>=0)skin.SetBlendShapeWeight(blinkLeft,blink*100);if(blinkRight>=0)skin.SetBlendShapeWeight(blinkRight,blink*100);for(int i=0;i<bones.Length;i++){bones[i].localRotation=rest[i];bones[i].localPosition=positions[i];bones[i].localScale=scales[i];}
 float wave=Mathf.Sin(phase);Rotate("BODY01",transform.right,wave*1.25f*effort);Rotate("BODY01",transform.up,turn*1.2f);
 for(int i=0;i<4;i++){Rotate(TailNames[i],transform.right,Mathf.Sin(phase-i*.48f)*TailAmplitudes[i]*effort);Rotate(TailNames[i],transform.up,-turn*(i+1)*.8f);}Rotate("TALE_L",transform.right,Mathf.Sin(phase-1.8f)*2*effort);Rotate("TALE_R",transform.right,Mathf.Sin(phase-1.8f)*2*effort);
 Rotate("Head_Root",transform.right,-wave*.6f*effort);Rotate("Head_Root",transform.up,turn*.7f);Rotate("Head",transform.right,Mathf.Sin(Clock*.75f+podIndex)*.35f);Rotate("Kluv",transform.right,jawAngle);Rotate("TONGUE04",transform.right,-jawAngle*.7f+Mathf.Sin(Clock*1.15f+podIndex)*.4f);Rotate("TONGUE03",transform.right,2);Rotate("TONGUE02",transform.right,2);Rotate("TONGUE01",transform.right,2);
 foreach(string sideName in Sides){var fin=Find("FLIPPER_"+sideName+"02");float sideSign=fin?Mathf.Sign(transform.InverseTransformPoint(fin.position).x):sideName=="L"?1:-1;float activity=Mathf.Max(effort,finTravel),trim=finClock*.65f+(sideName=="L"?0:.4f);
  // Pectoral control continues through glides; differential trim steers the two sides.
  Rotate("FLIPPER_"+sideName+"02",transform.forward,sideSign*(Mathf.Sin(trim)*3.2f*activity+Mathf.Sin(phase+.8f)*2.2f*effort)-turn*6.5f);
  Rotate("FLIPPER_"+sideName+"01",transform.forward,sideSign*(Mathf.Sin(trim-.65f)*1.6f*activity+Mathf.Sin(phase+.3f)*.8f*effort));
  Rotate("FLIPPER_"+sideName+"01",transform.right,Mathf.Sin(trim+.4f)*.9f*activity);
  var eye=Find("eye "+sideName);float look=0,pitch=0;if(eye&&viewer.HasValue){var to=transform.InverseTransformDirection(viewer.Value-eye.position).normalized;look=Mathf.Clamp(Mathf.Atan2(to.x,to.z)*Mathf.Rad2Deg,-5,5);pitch=Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(to.y,-1,1))*Mathf.Rad2Deg,-3,3);}Rotate("eye "+sideName,transform.up,look);Rotate("eye "+sideName,transform.right,pitch);
  Rotate("eyelish_up "+sideName,transform.forward,-sideSign*blink*18);Rotate("eyelish_down "+sideName,transform.forward,sideSign*blink*14);
 }
 }
 Transform Find(string name){named.TryGetValue(name,out var bone);return bone;}
 void Rotate(string name,Vector3 worldAxis,float degrees){var bone=Find(name);if(bone&&bone.parent)bone.localRotation=Quaternion.AngleAxis(degrees,bone.parent.InverseTransformDirection(worldAxis))*bone.localRotation;}
}
}












