using UnityEngine;

namespace Underwater {
public class FishSchool : MonoBehaviour {
 public Vector3 habitatCenter;
 public float habitatRadius=5;
 public float simulationRadius=45;
 public bool reefEncounter;float scatterUntil;Vector3 threat;
 public void Scatter(Vector3 source){threat=source;scatterUntil=clock+2.5f;}
 public FishAgent[] fish;
 Vector3[] positions,velocities,flocking;int[] leftLanes,rightLanes;Vector3 flow;float clock,decisionTimer;public int FlockingPasses {get;private set;}
 public void Initialize(){
  fish=GetComponentsInChildren<FishAgent>();positions=new Vector3[fish.Length];velocities=new Vector3[fish.Length];flocking=new Vector3[fish.Length];leftLanes=new int[fish.Length];rightLanes=new int[fish.Length];flow=transform.forward;decisionTimer=0;FlockingPasses=0;
  for(int i=0;i<fish.Length;i++)fish[i].Initialize(i+Mathf.Abs(Mathf.RoundToInt(habitatCenter.z*3)%53));
 }
 void Start(){Initialize();}
 void Update(){if(Diver.Instance&&Diver.Instance.MenuOpen)return;if(Application.platform==RuntimePlatform.Android&&Diver.Instance&&(Diver.Instance.transform.position-habitatCenter).sqrMagnitude>simulationRadius*simulationRadius)return;Simulate(Mathf.Min(Time.deltaTime,.05f));}
 public void Simulate(float dt){
  var diver=Diver.Instance;bool active=Application.isPlaying&&diver&&!diver.MenuOpen;
  Simulate(dt,active?(Vector3?)diver.LeftPalm:null,active?(Vector3?)diver.RightPalm:null);
 }
 public void Simulate(float dt,Vector3? leftPalm,Vector3? rightPalm){
  if(positions==null)Initialize();clock+=dt;decisionTimer-=dt;bool decide=decisionTimer<=0;if(decide){decisionTimer=.1f;FlockingPasses++;}
  Vector3 mean=Vector3.zero;for(int i=0;i<fish.Length;i++){positions[i]=fish[i].transform.position;velocities[i]=fish[i].Velocity.normalized;mean+=velocities[i];}
  mean=Vector3.ProjectOnPlane(mean,Vector3.up);if(mean.sqrMagnitude>.01f)flow=Vector3.RotateTowards(flow,mean.normalized,30*Mathf.Deg2Rad*dt,0);
  Vector3 guide=habitatCenter+new Vector3(Mathf.Sin(clock*.085f)*habitatRadius*.65f,Mathf.Sin(clock*.13f)*.25f,Mathf.Cos(clock*.085f)*habitatRadius*.65f);
  for(int i=0;i<fish.Length;i++){
   if(decide){Vector3 separation=Vector3.zero,alignment=Vector3.zero,cohesion=Vector3.zero;int neighbours=0;
   float personalSpace=Mathf.Max(.25f,fish[i].bodyLength*1.2f);
   for(int j=0;j<fish.Length;j++)if(i!=j){Vector3 delta=positions[i]-positions[j];float sq=delta.sqrMagnitude;
    if(sq<5.5f&&sq>.00001f){neighbours++;alignment+=velocities[j];cohesion+=positions[j];if(sq<personalSpace*personalSpace)separation+=delta/(sq+.025f);}
   }
   Vector3 neighbourSteer=separation*1.5f;
   if(neighbours>0)neighbourSteer+=alignment/neighbours*.9f+(cohesion/neighbours-positions[i]).normalized*.25f;
   flocking[i]=neighbourSteer;}
   Vector3 steer=fish[i].transform.forward*.8f+(guide-positions[i]).normalized*.65f+flocking[i];
   Vector3 home=habitatCenter-positions[i];if(home.magnitude>habitatRadius)steer+=home.normalized*(home.magnitude-habitatRadius)*1.3f;
   var diver=Diver.Instance;if(Application.isPlaying&&diver&&diver.Head){Vector3 away=positions[i]-diver.Head.position;float distance=away.magnitude;bool scooping=diver.GrabRequested&&fish[i].IsGrabbable&&Mathf.Min((positions[i]-diver.RightPalm).sqrMagnitude,(positions[i]-diver.LeftPalm).sqrMagnitude)<.5f*.5f;if(distance<1.3f&&!scooping)steer+=away.normalized*(1.3f-distance)*3;}
   if(reefEncounter){
 if(Application.isPlaying&&diver&&!diver.MenuOpen){bool scoop=diver.GrabRequested&&Mathf.Min((positions[i]-diver.RightPalm).sqrMagnitude,(positions[i]-diver.LeftPalm).sqrMagnitude)<.5f*.5f;
 if(!scoop&&diver.Head&&(positions[i]-diver.Head.position).sqrMagnitude<1.1f*1.1f)Scatter(diver.Head.position);}
 if(clock<scatterUntil)steer+=(positions[i]-threat).normalized*3;
 else steer+=(habitatCenter-positions[i]).normalized*1.4f;
}
float handSpeed=1;steer+=HandBypass(i,leftPalm,ref leftLanes[i],ref handSpeed);steer+=HandBypass(i,rightPalm,ref rightLanes[i],ref handSpeed);
fish[i].Step(dt,clock,steer,handSpeed);
  }
 }
 Vector3 HandBypass(int index,Vector3? palm,ref int lane,ref float speed){
  if(!palm.HasValue){lane=0;return Vector3.zero;}
  var offset=positions[index]-palm.Value;float radius=.65f+fish[index].bodyLength*.5f;
  if(Mathf.Abs(offset.y)>.5f||offset.sqrMagnitude>(radius+1.1f)*(radius+1.1f)){lane=0;return Vector3.zero;}
  var forward=Vector3.ProjectOnPlane(flow,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;var side=Vector3.Cross(Vector3.up,forward);
  float along=Vector3.Dot(offset,forward),across=Vector3.Dot(offset,side);
  if(lane==0)lane=Mathf.Abs(across)>.04f?(across>0?1:-1):(index%2==0?1:-1);
  float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+1.1f,offset.magnitude));
  // Commit to a horizontal side while approaching, then let cohesion join
  // the school again behind the hand. No vertical scattering or teleports.
  float pass=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.1f,radius,along));weight*=pass;
  if(Mathf.Abs(across)<radius&&along<.15f)speed=Mathf.Min(speed,Mathf.Lerp(1,.22f,weight));
  return side*lane*weight*4+Vector3.ProjectOnPlane(offset,Vector3.up).normalized*weight*.4f;
 }
}
}


