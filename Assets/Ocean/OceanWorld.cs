using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace Underwater {
public class OceanWorld : MonoBehaviour {
 public static readonly string[] Names={"Sunlit Reef","Rocky Ledge","The Wreck","Twilight Drift","The Trench","Jellyfish Drift","Open Water Drift","Wreck & Rays"};
 public static readonly Vector3[] Centers={new(0,0,0),new(0,-8,32),new(0,-17,64),new(0,-30,96),new(0,-48,128),new(0,-30,160),new(0,-15,192),new(0,-6,224)};
 public static Material Mat(Color c){var m=new Material(Shader.Find("Standard"));m.color=c;return m;}
 public static GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,Color c,Transform parent=null){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=Mat(c);return g;}
 void Start(){if(FindFirstObjectByType<Diver>()==null)Create();}
 public static void Create(){
 Random.InitState(731); RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
 var sun=new GameObject("Filtered sunlight").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(32,-25,0);sun.intensity=1.15f;
 for(int z=0;z<8;z++){
 var root=new GameObject(Names[z]);root.transform.position=Centers[z];
 Shape("Seabed",PrimitiveType.Cube,new(0,-4,0),new(45,2,36),new Color(.28f,.31f,.25f),root.transform);
 for(int r=0;r<20;r++)Shape("Reef rock",PrimitiveType.Sphere,new(Random.Range(-19f,19f),-2.8f,Random.Range(-15f,15f)),new(Random.Range(1f,4f),Random.Range(1f,3f),Random.Range(1f,4f)),new Color(.16f,.24f,.25f),root.transform);
 if(z<3||z==7)for(int c=0;c<45;c++){var coral=Shape("Coral",PrimitiveType.Capsule,new(Random.Range(-18f,18f),-2,Random.Range(-15f,15f)),new(.2f,Random.Range(.4f,1.1f),.2f),Color.Lerp(new Color(.9f,.3f,.28f),new Color(.5f,.3f,.7f),Random.value),root.transform);coral.AddComponent<SeaLife>().kind="Coral";}
 for(int t=0;t<3;t++){var p=Shape("Natural teleport shell",PrimitiveType.Sphere,new(-2+t*2,-2.9f,t*5-5),new(.5f,.12f,.5f),new Color(.25f,.8f,.78f),root.transform);p.AddComponent<TeleportShell>();}
 if(z==2||z==7){for(int h=0;h<9;h++){var rib=Shape("Rusted hull rib",PrimitiveType.Cube,new(-6,-1,h*1.6f-7),new(.3f,5,.3f),new Color(.32f,.16f,.09f),root.transform);rib.transform.localRotation=Quaternion.Euler(0,0,-25);Shape("Broken deck",PrimitiveType.Cube,new(-3,-2,h*1.6f-7),new(6,.2f,1.2f),new Color(.26f,.18f,.12f),root.transform);}}
 string[][] life={new[]{"Clownfish","Clownfish","Turtle","Manta","Fire coral"},new[]{"Octopus","Lionfish"},new[]{"Moray","Grouper","Dolphin","Dolphin"},new[]{"Comb jelly","Comb jelly","Lumen"},new[]{"Anglerfish","Anglerfish","Anglerfish","Giant squid"},new[]{"Moon jelly","Moon jelly","Box jelly"},new[]{"Turtle","Reef shark","Giant squid"},new[]{"Stingray","Stingray","Grouper"}};
 for(int i=0;i<life[z].Length;i++)Creature(life[z][i],Centers[z]+new Vector3((i%3-1)*2.5f,0,i*3-3));
 // Continuous sloping seabed between habitats.
 if(z<7){Vector3 a=Centers[z]+Vector3.down*3,b=Centers[z+1]+Vector3.down*3;var bridge=Shape("Sloping reef passage",PrimitiveType.Cube,(a+b)/2,new(10,1,Vector3.Distance(a,b)),new Color(.18f,.25f,.25f));bridge.transform.rotation=Quaternion.LookRotation(b-a);}
 }
 var rig=new GameObject("Diver XR Origin");rig.AddComponent<Diver>();
 var head=new GameObject("Head");head.tag="MainCamera";head.transform.SetParent(rig.transform);head.transform.localPosition=new(0,1.6f,0);var cam=head.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.27f,.34f);cam.nearClipPlane=.05f;cam.farClipPlane=90;head.AddComponent<AudioListener>();
 }
 static void Creature(string kind,Vector3 pos){
 bool jelly=kind.Contains("jelly"),ray=kind.Contains("ray")||kind=="Manta",giant=kind=="Giant squid";
 Color color=kind=="Clownfish"?new Color(1,.4f,.05f):jelly?new Color(.45f,.7f,.85f):kind=="Turtle"?new Color(.28f,.48f,.24f):new Color(.25f,.48f,.55f);
 var g=new GameObject(kind);g.transform.position=pos;var body=Shape("Body",PrimitiveType.Sphere,Vector3.zero,giant?new(4,3,12):ray?new(2,.15f,1.2f):jelly?new(.65f,.35f,.65f):new(.6f,.4f,1),color,g.transform);
 var life=g.AddComponent<SeaLife>();life.kind=kind;
 if(kind=="Fire coral"||kind=="Lionfish"||kind=="Box jelly"||kind=="Moon jelly")life.danger=true;
 life.consequence=kind!="Moon jelly";life.grabbable=kind=="Clownfish";life.observationOnly=giant;
 for(int i=0;i<(jelly?8:2);i++)Shape(jelly?"Tentacle":"Fin",PrimitiveType.Capsule,new(jelly?(i-3.5f)*.07f:(i==0?-.45f:.45f),jelly?-.55f:0,-.1f),jelly?new(.035f,.5f,.035f):new(.45f,.06f,.3f),color,g.transform);
 if(kind=="Lionfish")for(int i=0;i<9;i++){var spine=Shape("Venomous spine",PrimitiveType.Capsule,new((i-4)*.11f,.3f,0),new(.035f,.45f,.035f),i%2==0?Color.white:new Color(.4f,.12f,.06f),g.transform);spine.transform.localRotation=Quaternion.Euler(0,0,(i-4)*-12);}
 if(ray){var tail=Shape("Tail barb",PrimitiveType.Capsule,new(0,0,-1.1f),new(.055f,.7f,.055f),color,g.transform);tail.transform.localRotation=Quaternion.Euler(90,0,0);if(kind=="Stingray"){var hazard=tail.AddComponent<SeaLife>();hazard.kind="Ray tail";hazard.danger=true;hazard.stationary=true;}}
 if(kind=="Clownfish")for(int i=0;i<3;i++)Shape("White stripe",PrimitiveType.Sphere,new(0,0,(i-1)*.27f),new(.61f,.41f,.06f),Color.white,g.transform);
 if(kind=="Lumen"||kind=="Anglerfish"){var light=new GameObject("Organic lure").AddComponent<Light>();light.transform.SetParent(g.transform,false);light.transform.localPosition=new(0,.5f,.5f);light.color=Color.cyan;light.range=5;light.intensity=1.5f;Shape("Lure",PrimitiveType.Sphere,new(0,.5f,.5f),Vector3.one*.12f,Color.cyan,g.transform);}
 if(giant)g.transform.position+=Vector3.right*17;
 }
}

}


