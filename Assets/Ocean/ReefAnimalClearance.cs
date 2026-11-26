using UnityEngine;
namespace Underwater {
// All animation phases and shader displacement are baked into this local
// envelope in the editor. No per-frame vertex reads, mesh baking or raycasts.
[DefaultExecutionOrder(10000)]
public sealed class ReefAnimalClearance:MonoBehaviour {
 public ReefSeabedField seabed;public Bounds animatedBody;public float clearance=.035f;
 public Vector3[] groundSupportPoints;
 Vector3 lastPosition,lastScale;Quaternion lastRotation;bool hasSafePose;SeaLife life;
 void Awake(){life=GetComponent<SeaLife>();}
 public bool RequiredHeight(Vector3 position,Quaternion rotation,out float height){
  if(groundSupportPoints!=null&&groundSupportPoints.Length>0){
   height=float.NegativeInfinity;var pose=Matrix4x4.TRS(position,rotation,transform.lossyScale);
   foreach(var point in groundSupportPoints){var world=pose.MultiplyPoint3x4(point);if(!Physics.Raycast(world+Vector3.up*2,Vector3.down,out var hit,5,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))return false;height=Mathf.Max(height,hit.point.y-(world.y-position.y)+clearance);}return true;
  }
  var matrix=Matrix4x4.TRS(position,rotation,transform.lossyScale);var center=matrix.MultiplyPoint3x4(animatedBody.center);var e=animatedBody.extents;
  var x=matrix.MultiplyVector(new Vector3(e.x,0,0));var y=matrix.MultiplyVector(new Vector3(0,e.y,0));var z=matrix.MultiplyVector(new Vector3(0,0,e.z));
  var extent=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
  height=position.y;if(!seabed||!seabed.UpperHeight(center.x-extent.x,center.x+extent.x,center.z-extent.z,center.z+extent.z,out float floor))return false;
  height=floor-(center.y-position.y)+extent.y+clearance;return true;
 }
 public Vector3 ConstrainPosition(Vector3 proposed){
  if(RequiredHeight(proposed,transform.rotation,out float height)){proposed.y=Mathf.Max(proposed.y,height);return proposed;}
  return hasSafePose?lastPosition:transform.position;
 }
 public float Enforce(){
  if(hasSafePose&&transform.position==lastPosition&&transform.rotation==lastRotation&&transform.lossyScale==lastScale)return 0;
  var before=transform.position;var position=ConstrainPosition(before);
  if(position!=before){if(!life)life=GetComponent<SeaLife>();if(life&&life.IsHeld)life.Release();transform.position=position;}
  if(RequiredHeight(transform.position,transform.rotation,out float height)&&transform.position.y>=height-.00001f){lastPosition=transform.position;lastRotation=transform.rotation;lastScale=transform.lossyScale;hasSafePose=true;}
  else if(hasSafePose)transform.SetPositionAndRotation(lastPosition,lastRotation);
  return Mathf.Max(0,transform.position.y-before.y);
 }
 void LateUpdate(){
  // Validate every visible pose, including held fish and motion owners that
  // turn in LateUpdate. Never alter the tracked hand or diver rig.
  Enforce();
 }
}
}
