using UnityEngine;
namespace Underwater {
// Conservative support cells cover every authored arm pose. The seabed field
// is baked from the actual sand triangles; no runtime mesh baking is needed.
public sealed class OctopusGroundClearance:MonoBehaviour {
 public Vector2 terrainOrigin;public float terrainCell=.2f,footCell=.2f,clearance=.025f;
 public int terrainWidth,terrainDepth;public float[] terrainHeights;
 public Vector3[] supports;public float prediction=.35f;
 public float RequiredHeight(Vector3 position){
  float required=float.NegativeInfinity;
  if(supports==null||terrainHeights==null||terrainWidth==0)return position.y;
  foreach(var support in supports){
   float x=position.x+support.x,z=position.z+support.z;
   int x0=Mathf.FloorToInt((x-.025f-terrainOrigin.x)/terrainCell),x1=Mathf.FloorToInt((x+footCell+.025f-terrainOrigin.x)/terrainCell);
   int z0=Mathf.FloorToInt((z-.025f-terrainOrigin.y)/terrainCell),z1=Mathf.FloorToInt((z+footCell+.025f-terrainOrigin.y)/terrainCell);
   if(x0<0||z0<0||x1>=terrainWidth||z1>=terrainDepth)return float.PositiveInfinity;
   float floor=float.NegativeInfinity;for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++)floor=Mathf.Max(floor,terrainHeights[ix+iz*terrainWidth]);
   required=Mathf.Max(required,floor-support.y+clearance);
  }
  return required;
 }
 public Vector3 Constrain(Vector3 proposed,float dt){
  var current=transform.position;float here=RequiredHeight(current);var flat=Vector3.ProjectOnPlane(proposed,Vector3.up);
  float next=RequiredHeight(current+flat);
  if(float.IsInfinity(next)){flat=Vector3.zero;next=here;}
  if(float.IsInfinity(here))return Vector3.zero;
  float ahead=flat.sqrMagnitude>.000001f?RequiredHeight(current+flat+flat.normalized*prediction):next;
  if(float.IsInfinity(ahead))ahead=next;
  float target=Mathf.Max(here,Mathf.Max(next,ahead));
  float y=Mathf.MoveTowards(current.y,target,(target>current.y?.65f:.35f)*Mathf.Max(0,dt));
  // Pause horizontal travel while lifting, so a rising bank cannot pass
  // through the arm tips before the body has acquired sufficient clearance.
  if(y<next){flat=Vector3.zero;y=Mathf.Max(here,y);}
  return flat+Vector3.up*(Mathf.Max(here,y)-current.y);
 }
 public float SandUpperHeight(Vector3 point){int x=Mathf.FloorToInt((point.x-terrainOrigin.x)/terrainCell),z=Mathf.FloorToInt((point.z-terrainOrigin.y)/terrainCell);return x>=0&&z>=0&&x<terrainWidth&&z<terrainDepth?terrainHeights[x+z*terrainWidth]:float.PositiveInfinity;}
}
}
