using UnityEngine;
namespace Underwater {
// Small steering flex added after the swim clip. Navigation remains on the actor.
[DefaultExecutionOrder(20)]
public sealed class SharkBodyMotion:MonoBehaviour {
 public Transform actor,body,tailBase,leftFin,rightFin;
 Vector3 previous;float bend,clock;bool ready;
 public float Bend=>bend;
 public void Initialize(){if(ready)return;ready=true;if(!actor)actor=transform;previous=actor.forward;}
 public void Step(float dt){Initialize();dt=Mathf.Clamp(dt,.001f,.05f);var shark=actor.GetComponent<OpenWaterShark>();float effort=shark?Mathf.Clamp01(shark.ActualSpeed/1.05f):1;clock+=dt*Mathf.Lerp(.3f,.55f,effort)*Mathf.PI*2;float yaw=Vector3.SignedAngle(previous,actor.forward,Vector3.up)/dt;previous=actor.forward;bend=Mathf.Lerp(bend,Mathf.Clamp(yaw*.18f,-5,5),1-Mathf.Exp(-dt*2.5f));Apply(body,bend+Mathf.Sin(clock)*.8f*effort);Apply(tailBase,bend*.6f+Mathf.Sin(clock-.8f)*5.5f*effort);}
 void Apply(Transform bone,float angle){if(bone)bone.localRotation=Quaternion.AngleAxis(angle,bone.parent.InverseTransformDirection(actor.up))*bone.localRotation;}
 void LateUpdate(){var d=Diver.Instance;if(d&&d.MenuOpen||!ReefSimulationRange.Active(actor?actor:transform)){if(actor)previous=actor.forward;return;}Step(Time.deltaTime);}
}
}
