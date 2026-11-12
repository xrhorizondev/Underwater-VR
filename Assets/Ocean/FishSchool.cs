using UnityEngine;

namespace Underwater {
public class FishSchool : MonoBehaviour {
 public Vector3 habitatCenter;
 public float habitatRadius=5;
 public FishAgent[] fish;
 Vector3[] positions,velocities,flocking;float clock,decisionTimer;public int FlockingPasses {get;private set;}
 public void Initialize(){
  fish=GetComponentsInChildren<FishAgent>();positions=new Vector3[fish.Length];velocities=new Vector3[fish.Length];flocking=new Vector3[fish.Length];decisionTimer=0;FlockingPasses=0;
  for(int i=0;i<fish.Length;i++)fish[i].Initialize(i+Mathf.Abs(Mathf.RoundToInt(habitatCenter.z*3)%53));
 }
 void Start(){Initialize();}
 void Update(){if(Application.platform==RuntimePlatform.Android&&Diver.Instance&&(Diver.Instance.transform.position-habitatCenter).sqrMagnitude>45*45)return;Simulate(Mathf.Min(Time.deltaTime,.05f));}
 public void Simulate(float dt){
  if(positions==null)Initialize();clock+=dt;decisionTimer-=dt;bool decide=decisionTimer<=0;if(decide){decisionTimer=.1f;FlockingPasses++;}
  for(int i=0;i<fish.Length;i++){positions[i]=fish[i].transform.position;velocities[i]=fish[i].Velocity.normalized;}
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
   var diver=Diver.Instance;if(Application.isPlaying&&diver&&diver.Head){Vector3 away=positions[i]-diver.Head.position;float distance=away.magnitude;bool scooping=diver.GrabRequested&&fish[i].IsGrabbable&&(positions[i]-diver.RightPalm).sqrMagnitude<.5f*.5f;if(distance<1.3f&&!scooping)steer+=away.normalized*(1.3f-distance)*3;}
   fish[i].Step(dt,clock,steer);
  }
 }
}
}


