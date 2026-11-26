using UnityEngine;
namespace Underwater {
// Standalone review controls. The asset animation never moves its gameplay root.
public sealed class SquidReviewControls : MonoBehaviour {
 public Transform focus; public Animation[] players; public float distance=8;
 float yaw=65,pitch=18; bool paused;
 void Update(){
  if(Input.GetKeyDown(KeyCode.Space)){paused=!paused;foreach(var p in players)foreach(AnimationState s in p)s.speed=paused?0:1;}
  if(Input.GetKey(KeyCode.LeftArrow))yaw-=45*Time.deltaTime;
  if(Input.GetKey(KeyCode.RightArrow))yaw+=45*Time.deltaTime;
  if(Input.GetKey(KeyCode.UpArrow))pitch=Mathf.Clamp(pitch+30*Time.deltaTime,-75,75);
  if(Input.GetKey(KeyCode.DownArrow))pitch=Mathf.Clamp(pitch-30*Time.deltaTime,-75,75);
  if(Input.GetKey(KeyCode.Equals)||Input.GetKey(KeyCode.KeypadPlus))distance=Mathf.Max(2,distance-3*Time.deltaTime);
  if(Input.GetKey(KeyCode.Minus)||Input.GetKey(KeyCode.KeypadMinus))distance=Mathf.Min(20,distance+3*Time.deltaTime);
  if(focus){transform.position=focus.position+Quaternion.Euler(pitch,yaw,0)*Vector3.back*distance;transform.LookAt(focus);}
  foreach(var label in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.transform.rotation=transform.rotation;
 }
 void OnGUI(){GUI.Label(new Rect(16,12,1000,28),"SQUID FULL-BODY REVIEW   |   Arrows: orbit   +/-: zoom   Space: pause");}
}
}
