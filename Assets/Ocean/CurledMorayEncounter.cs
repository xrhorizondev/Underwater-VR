using UnityEngine;
namespace Underwater {
// A sheltering moray withdraws along the substrate without an aerial escape routine.
public sealed class CurledMorayEncounter:MonoBehaviour {
 public enum Phase {Resting,Rising,Uncurling,Swimming,Curling,Settling}
 public Animation animationPlayer;public AnimationClip restClip,swimClip;public SkinnedMeshRenderer skin;
 public Collider[] touchVolumes;public Bounds restBounds,travelBounds;
 public float swimSpeed=.48f,escapeDistance=2.7f,contactPadding=.025f;
 public Phase State{get;private set;}=Phase.Resting;
 public Vector3 Destination{get;private set;}public int CompletedEscapes{get;private set;}
 public float LastTravel{get;private set;}public float AnimationClock=>clock;public string LastPlanFailure{get;private set;}
 ReefSphereBody body;ReefAnimalClearance clearance;ReefCreatureInfo info;Transform[] bones;
 Vector3[] bonePositions,boneScales;Vector3 retreatStart;float clock,stateTime,cooldown,recoilSide=1;bool ready,contactLatch;
 public float ReactionWeight{get;private set;}
 public void Initialize(){if(ready)return;ready=true;body=GetComponent<ReefSphereBody>();clearance=GetComponent<ReefAnimalClearance>();info=GetComponent<ReefCreatureInfo>();animationPlayer.enabled=false;animationPlayer.playAutomatically=false;restClip.SampleAnimation(animationPlayer.gameObject,0);bones=skin.bones;bonePositions=new Vector3[bones.Length];boneScales=new Vector3[bones.Length];for(int i=0;i<bones.Length;i++){bonePositions[i]=bones[i].localPosition;boneScales[i]=bones[i].localScale;}clearance.animatedBody=restBounds;body.centerOffset=restBounds.center;}
 void Update(){var d=Diver.Instance;if(!d||d.MenuOpen||!ReefSimulationRange.Active(transform))return;Step(Mathf.Min(Time.deltaTime,.05f),d.LeftPalm,d.RightPalm,d.LeftFingertip,d.RightFingertip);if(d.Head&&(HandDistance(d.Head.position)<.205f||HandDistance(d.Head.position-Vector3.up*.35f)<.25f))RequestTouch(d.Head.position);if(!UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).isValid&&Input.GetMouseButtonDown(0)&&d.Head)TryDesktopTouch(d.Head.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition));}
 public bool TryDesktopTouch(Ray ray,float maxDistance=2){Initialize();if(Diver.Instance&&Diver.Instance.MenuOpen)return false;if(!Physics.Raycast(ray,out var hit,maxDistance,(1<<2)|(1<<8)|(1<<9),QueryTriggerInteraction.Collide))return false;if(touchVolumes!=null)foreach(var c in touchVolumes)if(c&&c==hit.collider)return RequestTouch(hit.point);return false;}
 public float HandDistance(Vector3 hand){float distance=float.PositiveInfinity;if(touchVolumes!=null)foreach(var c in touchVolumes)if(c&&c.enabled&&c.gameObject.activeInHierarchy)distance=Mathf.Min(distance,Vector3.Distance(hand,c.ClosestPoint(hand)));return distance;}
 public bool CanEscape(Vector3 source){Initialize();return Plan(source);}
 bool Plan(Vector3 source){
  // A short retreat into its curl is more appropriate than straightening and flying away.
  var away=Vector3.ProjectOnPlane(transform.position-source,Vector3.up).normalized;if(away.sqrMagnitude<.01f)away=-transform.forward;
  foreach(float distance in new[]{.22f,.15f,.08f}){var goal=transform.position+away*distance;if(!clearance.RequiredHeight(goal,transform.rotation,out var height))continue;goal.y=height;if(Mathf.Abs(goal.y-transform.position.y)>.06f)continue;var delta=goal-transform.position;if(Physics.SphereCast(body.WorldCenter,body.radius,delta.normalized,out _,delta.magnitude+.015f,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))continue;if(!body.Clear(goal))continue;Destination=goal;LastPlanFailure="";return true;}
  LastPlanFailure="Shelter retreat obstructed";return false;
 }
 public bool RequestTouch(Vector3 source){Initialize();if(State!=Phase.Resting||cooldown>0)return false;bool planned=Plan(source);if(!planned)Destination=transform.position;recoilSide=Vector3.Dot(transform.right,source-transform.position)>0?-1:1;retreatStart=transform.position;stateTime=0;LastTravel=0;State=Phase.Curling;info?.Reveal();return true;}
 public void Step(float dt,Vector3? left=null,Vector3? right=null,Vector3? leftTip=null,Vector3? rightTip=null){
  Initialize();if(dt<=0)return;clock+=dt;stateTime+=dt;cooldown=Mathf.Max(0,cooldown-dt);
  float nearest=float.PositiveInfinity;Vector3 source=transform.position;
  Check(left,ref nearest,ref source);Check(right,ref nearest,ref source);Check(leftTip,ref nearest,ref source);Check(rightTip,ref nearest,ref source);
  if(nearest>.1f)contactLatch=false;if(nearest<=contactPadding&&!contactLatch)contactLatch=RequestTouch(source);
  // Continue the authored breathing/curl motion throughout the reaction.
  restClip.SampleAnimation(animationPlayer.gameObject,Mathf.Repeat(clock,restClip.length));
  for(int i=0;i<bones.Length;i++){bones[i].localPosition=bonePositions[i];bones[i].localScale=boneScales[i];}
  ReactionWeight=State==Phase.Resting?0:Mathf.SmoothStep(0,1,stateTime/.25f)*(1-Mathf.SmoothStep(0,1,(stateTime-.45f)/1.65f));
  // A visible head withdrawal and travelling lateral flex remain in the sand plane.
  if(ReactionWeight>0)for(int i=0;i<bones.Length;i++){float along=i/Mathf.Max(1f,bones.Length-1);float bend=(i==0?9:Mathf.Lerp(2.2f,.4f,along))*recoilSide*ReactionWeight;var parent=bones[i].parent;if(parent)bones[i].localRotation=Quaternion.AngleAxis(bend,parent.InverseTransformDirection(transform.up))*bones[i].localRotation;}
  if(State==Phase.Resting)return;
  var before=transform.position;float t=Mathf.Clamp01(stateTime/1.35f);var target=Vector3.Lerp(retreatStart,Destination,Mathf.SmoothStep(0,1,t));
  body.Move(Vector3.ClampMagnitude(target-transform.position,.28f*dt));LastTravel+=(transform.position-before).magnitude;
  if(stateTime>=3.4f){State=Phase.Resting;CompletedEscapes++;cooldown=6;}
 }
 void Check(Vector3? hand,ref float nearest,ref Vector3 source){if(!hand.HasValue)return;float d=HandDistance(hand.Value);if(d<nearest){nearest=d;source=hand.Value;}}
}
}
