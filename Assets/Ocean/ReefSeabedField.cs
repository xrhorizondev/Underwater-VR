using UnityEngine;
namespace Underwater {
// Conservative heights rasterized from the saved sand triangles, not an
// analytic approximation or rock colliders. Shared by every animal.
[CreateAssetMenu(menuName="Underwater/Seabed clearance field")]
public sealed class ReefSeabedField:ScriptableObject {
 public const float MissingHeight=10000;
 public Vector2 origin;public float cell=1;public int width,depth;public float[] heights;
 public bool UpperHeight(float minX,float maxX,float minZ,float maxZ,out float height){
  height=float.NegativeInfinity;if(heights==null||width==0||depth==0)return false;
  int x0=Mathf.FloorToInt((minX-origin.x)/cell),x1=Mathf.FloorToInt((maxX-origin.x)/cell),z0=Mathf.FloorToInt((minZ-origin.y)/cell),z1=Mathf.FloorToInt((maxZ-origin.y)/cell);
  if(x0<0||z0<0||x1>=width||z1>=depth)return false;
  for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){float y=heights[x+z*width];if(y>=MissingHeight||float.IsInfinity(y)||float.IsNaN(y))return false;height=Mathf.Max(height,y);}
  return true;
 }
}
}
