using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Random=UnityEngine.Random;

namespace Underwater.Editor {
public static partial class RealisticReefBuilder {
 static Material[] reefSpecies;
 static Material turtleCoat,turtleEye,turtleKeratin,turtleClaw;
 static readonly string[] SpeciesNames={"Yellow tang","Blue tang","Copperband butterflyfish","Royal gramma","Lyretail anthias","Sergeant major"};
 static void VarietyMaterials(){
  reefSpecies=new Material[6];
  for(int i=0;i<6;i++){var m=new Material(fishPalette[0]);m.color=Color.white;m.SetFloat("_Species",i+1);reefSpecies[i]=Save(m,Art+"Materials/Reef species "+SpeciesNames[i]+".mat");}
  turtleCoat=TurtleMaterial("Loggerhead scutes and skin",new Color(.78f,.72f,.61f),.22f);
  turtleCoat.mainTexture=Tex("Loggerhead_albedo.png");turtleCoat.SetTexture("_BumpMap",Tex("Loggerhead_normal.png"));
  turtleEye=TurtleMaterial("Loggerhead iris",new Color(.12f,.075f,.024f),.62f);
  turtleKeratin=TurtleMaterial("Loggerhead beak and pupil",new Color(.025f,.021f,.015f),.3f);
  turtleClaw=TurtleMaterial("Loggerhead small claws",new Color(.34f,.25f,.14f),.18f);
 }
 static Material TurtleMaterial(string name,Color color,float gloss){var m=new Material(Shader.Find("Underwater/Loggerhead Skin"));m.color=color;m.SetFloat("_Glossiness",gloss);return Save(m,Art+"Materials/"+name+".mat");}
 static GameObject VarietyFish(int variant,Vector3 position,float size,Transform parent){
  variant%=6;int kind=variant;
  var mesh=Mesh("Reef species mesh "+kind,()=>{
   var m=UnityEngine.Object.Instantiate(Mesh("PhotographicFishCanonical",CanonicalFish));var v=m.vertices;
   for(int i=0;i<v.Length;i++){
    Vector3 q=v[i];float yScale=kind==0||kind==1?1.5f:kind==2?1.8f:kind==3?.85f:kind==4?1.05f:1.25f;
    float xScale=kind<3?.7f:kind==3?.8f:1;
    q.y*=yScale;q.x*=xScale;
    // Compress the trunk while retaining a narrow tail peduncle and recognizable fins.
    if(kind<3){float trunk=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.4f,-.18f,q.z));q.z*=Mathf.Lerp(1,.87f,trunk);}
    if(kind==4&&q.z<-.34f)q.z-=Mathf.Abs(q.y)*.35f;
    v[i]=q;
   }
   m.vertices=v;m.RecalculateNormals();m.RecalculateTangents();m.bounds=new Bounds(Vector3.zero,new Vector3(.65f,1.2f,1.3f));return m;
  });
  var f=Model(SpeciesNames[kind],mesh,reefSpecies[kind],position,Vector3.one*size,parent);
  var agent=f.AddComponent<FishAgent>();agent.bodyLength=size;agent.cruiseSpeed=size*Random.Range(kind<3?.9f:1.2f,kind<3?1.35f:1.7f);return f;
 }
 static void CreateLoggerhead(Vector3 position,float angle){
  var root=new GameObject("Loggerhead sea turtle");root.transform.SetParent(world);root.transform.position=position;root.transform.rotation=Quaternion.Euler(0,angle,0);
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/LoggerheadTurtle.fbx");
  if(!asset)throw new System.Exception("Loggerhead turtle asset missing");
  var temp=(GameObject)PrefabUtility.InstantiatePrefab(asset);var parts=temp.GetComponentsInChildren<MeshFilter>();
  for(int i=0;i<parts.Length;i++){
   var part=parts[i];int index=i;
   var mesh=Mesh("Loggerhead part "+index,()=>{var m=new Mesh{name=part.name,indexFormat=IndexFormat.UInt32};m.CombineMeshes(new[]{new CombineInstance{mesh=part.sharedMesh,transform=part.transform.localToWorldMatrix}},true,true);m.RecalculateBounds();var b=m.bounds;b.Expand(new Vector3(.25f,.6f,.25f));m.bounds=b;return m;});
   Material mat=part.name.Contains("Continuous")?turtleCoat:part.name.Contains("iris")?turtleEye:part.name.Contains("claw")?turtleClaw:turtleKeratin;
   Model(part.name,mesh,mat,Vector3.zero,Vector3.one,root.transform);
  }
  UnityEngine.Object.DestroyImmediate(temp);
  root.AddComponent<TurtleSwimmer>();var life=root.AddComponent<SeaLife>();life.kind="Turtle";life.stationary=true;
 }
}
}
