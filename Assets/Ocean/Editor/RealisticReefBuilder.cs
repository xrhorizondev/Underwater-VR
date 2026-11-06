using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Random=UnityEngine.Random;

namespace Underwater.Editor {
public static partial class RealisticReefBuilder {
 const string Art="Assets/UnderwaterArt/";
 static readonly Dictionary<string,Mesh> meshes=new();
 static Material sand,rock,scan,grass,brain,eye,turtle,silver,gold,blue,water,photoFish;
 static Material[] corals,scanPalette,fishPalette;
 static Transform world;
 static T Save<T>(T asset,string path) where T:UnityEngine.Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){EditorUtility.CopySerialized(asset,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(asset);return old;}AssetDatabase.CreateAsset(asset,path);return asset;}
 static Mesh Mesh(string name,Func<Mesh> make){if(meshes.TryGetValue(name,out var m))return m;m=Save(make(),Art+"Meshes/"+name+".asset");meshes[name]=m;return m;}
 static Texture2D Tex(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Textures/"+name);
 static Material Mat(string name,Color tint,string diffuse=null,string normal=null,string ao=null,float worldScale=0,float sway=0){var m=new Material(Shader.Find("Underwater/Reef Surface")){name=name,enableInstancing=true};m.SetColor("_Color",tint);m.SetFloat("_Glossiness",.12f);m.SetFloat("_CausticStrength",.65f);m.SetFloat("_Sway",sway);if(diffuse!=null)m.SetTexture("_MainTex",Tex(diffuse));if(normal!=null)m.SetTexture("_BumpMap",Tex(normal));if(ao!=null)m.SetTexture("_OcclusionMap",Tex(ao));m.SetFloat("_WorldMapping",worldScale>0?1:0);m.SetFloat("_WorldScale",worldScale);return Save(m,Art+"Materials/"+name+".mat");}
 static GameObject Model(string name,Mesh mesh,Material mat,Vector3 p,Vector3 scale,Transform parent=null){var g=new GameObject(name);g.transform.SetParent(parent?parent:world,false);g.transform.localPosition=p;g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
 static void ImportTextures(){foreach(string path in Directory.GetFiles(Art+"Textures")){if(path.EndsWith(".meta"))continue;if(AssetImporter.GetAtPath(path) is TextureImporter imp){imp.textureType=path.Contains("nor_gl")||path.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=!(path.Contains("rough")||path.Contains("_ao")||path.Contains("Caustics"));imp.wrapMode=TextureWrapMode.Repeat;imp.anisoLevel=8;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();}}
 foreach(string path in Directory.GetFiles(Art+"Scans","*.fbx")){if(AssetImporter.GetAtPath(path) is ModelImporter imp){imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importAnimation=false;imp.isReadable=(path.EndsWith("PhotographicFish.fbx")||path.EndsWith("LoggerheadTurtle.fbx"));imp.SaveAndReimport();}}
 }
 static void Materials(){
 photoFish=Mat("Photographic fish scales",new Color(.87f,.93f,.93f),"FishScan_albedo.png","FishScan_normal.png");photoFish.SetFloat("_Glossiness",.45f);photoFish.SetFloat("_Swim",.025f);photoFish.SetFloat("_BumpScale",.65f);
 fishPalette=new Material[3];Color[] fishColors={new(.64f,.94f,1f),new(1f,.62f,.15f),new(.24f,.68f,1f)};for(int i=0;i<3;i++){var m=new Material(Shader.Find("Underwater/Living Fish Skin"));m.mainTexture=Tex("FishScan_albedo.png");m.SetTexture("_BumpMap",Tex("FishScan_normal.png"));m.color=fishColors[i];fishPalette[i]=Save(m,Art+"Materials/Natural fish skin "+i+".mat");}
 sand=Mat("01 Calcareous reef sand",new Color(.88f,.84f,.76f),"coast_sand_02_diff_2k.jpg","coast_sand_02_nor_gl_2k.jpg","coast_sand_02_ao_2k.jpg");sand.SetFloat("_BumpScale",.8f);
 rock=Mat("02 Encrusted limestone",new Color(.45f,.42f,.37f),"coral_ground_02_diff_2k.jpg","coral_ground_02_nor_gl_2k.jpg","coral_ground_02_ao_2k.jpg",.65f);
 corals=new Material[]{Mat("03 Acropora ochre",new Color(.98f,.62f,.16f),"LivingCoral_albedo.png","LivingCoral_normal.png"),Mat("04 Montipora rose",new Color(.72f,.24f,.16f),"LivingCoral_albedo.png","LivingCoral_normal.png"),Mat("05 Gorgonian plum",new Color(.46f,.25f,.37f),"LivingCoral_albedo.png","LivingCoral_normal.png",null,0,.018f),Mat("06 Sponge amber",new Color(.83f,.47f,.14f),"LivingCoral_albedo.png","LivingCoral_normal.png"),Mat("07 Acropora teal",new Color(.3f,.72f,.62f),"LivingCoral_albedo.png","LivingCoral_normal.png"),Mat("08 Coral lavender",new Color(.52f,.37f,.59f),"LivingCoral_albedo.png","LivingCoral_normal.png")};
 scan=Mat("09 Smithsonian Acropora",new Color(.65f,.69f,.4f),null,"LivingCoral_normal.png");scan.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Scans/Acropora_scan.png");scan.SetFloat("_BumpScale",.18f);
 rock.SetFloat("_TextureSaturation",.3f);rock.SetFloat("_Encrustation",1);
 scanPalette=new Material[6];Color[] palette={new(.9f,.6f,.25f),new(.7f,.33f,.44f),new(.78f,.72f,.55f),new(.38f,.57f,.46f),new(.66f,.28f,.19f),new(.57f,.38f,.66f)};for(int i=0;i<6;i++){var m=new Material(scan);m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Scans/Secale_scan.png");m.color=palette[i];m.SetFloat("_TextureSaturation",.2f);scanPalette[i]=Save(m,Art+"Materials/Living scanned coral "+i+".mat");}
 grass=Mat("10 Seagrass",new Color(.21f,.36f,.19f),"LivingCoral_albedo.png",null,null,0,.07f);
 brain=Mat("11 Brain coral ridges",Color.white,"BrainCoral_albedo.png","BrainCoral_normal.png");brain.SetFloat("_BumpScale",.7f);brain.SetColor("_Color",new Color(.67f,.48f,.31f));
 brain.SetFloat("_TextureSaturation",.05f);
 eye=Mat("12 Obsidian eyes",new Color(.009f,.018f,.018f));eye.SetFloat("_Glossiness",.7f);
 turtle=Mat("13 Turtle carapace",new Color(.9f,.93f,.79f),"TurtleShell_albedo.png","TurtleShell_normal.png");
 silver=Mat("14 School silver",new Color(.48f,.61f,.62f),"Fish_albedo.png","Fish_normal.png");silver.SetFloat("_Glossiness",.48f);
 gold=Mat("15 Anthias orange",new Color(.85f,.52f,.29f),"Fish_albedo.png","Fish_normal.png");blue=Mat("16 Chromis blue",new Color(.4f,.65f,.72f),"Fish_albedo.png","Fish_normal.png");foreach(var fishMat in new[]{silver,gold,blue})fishMat.SetFloat("_Swim",.04f);
 water=Save(new Material(Shader.Find("Underwater/Water Surface From Below")){name="17 Surface refraction"},Art+"Materials/17 Surface refraction.mat");
 }
 [MenuItem("Underwater/Build realistic reef scene")]
 public static void Create(){
  Directory.CreateDirectory(Art+"Meshes");Directory.CreateDirectory(Art+"Materials");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Renders");meshes.Clear();AssetDatabase.Refresh();ImportTextures();Materials();VarietyMaterials();Random.InitState(28431);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);world=new GameObject("UNDERWATER | Living reef").transform;
  var light=new GameObject("Sunlight through the water").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1f,.97f,.9f);light.intensity=1.65f;light.transform.rotation=Quaternion.Euler(47,-28,0);light.shadows=LightShadows.Soft;light.shadowBias=.03f;light.shadowNormalBias=.25f;light.shadowStrength=.85f;RenderSettings.sun=light;
  var atmosphere=new GameObject("Water optics and depth atmosphere").AddComponent<ReefAtmosphere>();atmosphere.waterBackground=Save(new Material(Shader.Find("Underwater/Blue Water Column")),Art+"Materials/Blue water background.mat");atmosphere.sunlight=light;atmosphere.caustics=Tex("Caustics.png");atmosphere.Apply(null);
  QualitySettings.antiAliasing=4;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadowDistance=65;QualitySettings.shadowCascades=4;QualitySettings.pixelLightCount=4;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
  for(int c=0;c<8;c++){float start=-30+c*38;var ground=Model("Continuous seabed "+c,Mesh("Seabed_"+c,()=>ReefGeometry.Terrain(start,start+38)),sand,Vector3.zero,Vector3.one);ground.AddComponent<MeshCollider>().sharedMesh=ground.GetComponent<MeshFilter>().sharedMesh;}
  for(int z=0;z<8;z++)Habitat(z);AddScannedReef();
  // A continuous coral-covered limestone arch provides a natural swim-through.
  var arch=new GameObject("Ancient reef swim-through arch");arch.transform.SetParent(world);for(int i=0;i<11;i++){float a=Mathf.Lerp(.05f,Mathf.PI-.05f,i/10f);Vector3 p=new(Mathf.Cos(a)*5.3f,Mathf.Sin(a)*5.4f-3.2f,23);var g=Model("Eroded arch voussoir",Mesh("Limestone_"+(i%7),()=>ReefGeometry.Rock(i%7+17)),rock,p,new(1.8f,1.5f,2.2f),arch.transform);g.transform.rotation=Quaternion.Euler(i*16,i*29,20);Coral(p+Vector3.up*.9f,1.1f,i%6,arch.transform,i);}
  var surfaceMesh=new ReefGeometry();for(int z=0;z<=32;z++)for(int x=0;x<=32;x++)surfaceMesh.Vertex(new(-65+x*4,8,-45+z*4),new(x/8f,z/8f),Color.white);for(int z=0;z<32;z++)for(int x=0;x<32;x++){int j=z*33+x;surfaceMesh.Quad(j,j+1,j+34,j+33);}Model("Moving underside of ocean surface",Save(surfaceMesh.Mesh("Ocean surface"),Art+"Meshes/OceanSurface.asset"),water,Vector3.zero,Vector3.one);
  Turtle(new Vector3(1.7f,-.3f,7.5f),-42);Turtle(new Vector3(1,-14,196),-15);
  var mantaMat=Mat("Manta dorsal skin",new Color(.1f,.18f,.19f),"Fish_albedo.png","Fish_normal.png");
  var manta=Model("Manta ray gliding over the reef",Mesh("Manta",ReefGeometry.Ray),mantaMat,new(-3,2.6f,17),Vector3.one);manta.transform.rotation=Quaternion.Euler(0,105,0);var swim=manta.AddComponent<MarineSwimmer>();swim.radius=5;swim.speed=.075f;swim.isRay=true;UpgradeManta(manta);
  var interact=manta.AddComponent<SeaLife>();interact.kind="Manta";interact.stationary=true;
  School(new Vector3(-2,.8f,11),36,3.5f,gold,.35f);School(new Vector3(4,2,20),65,6,silver,.27f);School(new Vector3(-5,-.4f,7),18,2.2f,blue,.22f);
  foreach(int z in new[]{1,2,6,7})School(OceanWorld.Centers[z]+new Vector3(0,1,5),24,4,z==6?silver:gold,.35f);
  School(new Vector3(-.5f,-.3f,1),7,3,silver,.48f);
  CreateDiver();Particles(new Vector3(0,0,9),"Suspended plankton",new Vector3(25,9,33),200);
  foreach(int z in new[]{3,4,5})Particles(OceanWorld.Centers[z],"Marine snow "+z,new Vector3(22,12,30),110);
  var camera=Camera.main;atmosphere.Apply(camera);PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.runInBackground=true;
  InstallReefCollision();ValidateScene();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Underwater_Realistic.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Underwater_Realistic.unity",true)};AssetDatabase.SaveAssets();
  Debug.Log("REEF_SCENE_READY renderers="+UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length);
 }
 static void AddScannedReef(){
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/CoastRock.fbx");if(!asset)return;
 var material=Mat("Scanned coastal reef base",new Color(.58f,.57f,.51f),"coast_rocks_01_diff_2k.jpg","coast_rocks_01_nor_gl_2k.jpg","coast_rocks_01_arm_2k.jpg");
 material.SetFloat("_TextureSaturation",.3f);material.SetFloat("_Encrustation",.8f);
 for(int i=0;i<4;i++){var g=(GameObject)PrefabUtility.InstantiatePrefab(asset);g.name="Photogrammetry | weathered reef bed "+i;g.transform.SetParent(world);g.transform.position=new(i%2==0?-13:13,-3.1f,i<2?2:20);g.transform.localScale=new(.34f,.7f,.43f);g.transform.rotation=Quaternion.Euler(0,i*87,0);foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;}
 }
 static void Habitat(int zone){
  var root=new GameObject((zone+1).ToString("00")+" | "+OceanWorld.Names[zone]);root.transform.SetParent(world);float center=OceanWorld.Centers[zone].z;bool lush=zone<3||zone==7;
  int count=zone==0?90:zone==4?18:38;
  for(int i=0;i<count;i++){float z=center+Random.Range(-14f,17f);float side=i%2==0?-1:1;float x=side*Random.Range(5,25)+Mathf.Sin(z*.115f)*2;float y=ReefGeometry.Floor(x,z);float radius=Random.Range(1.2f,3.4f);Vector3 p=new(x,y+radius*.25f,z);int id=i%7;var g=Model("Porous reef outcrop",Mesh("Limestone_"+id,()=>ReefGeometry.Rock(id+17)),rock,p,new(radius,Random.Range(.45f,.9f)*radius,radius*Random.Range(.7f,1.5f)),root.transform);g.transform.rotation=Quaternion.Euler(Random.Range(-15,15),Random.Range(0,360),Random.Range(-15,15));g.AddComponent<MeshCollider>().sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;
   if(lush){int n=zone==0?5:3;for(int c=0;c<n;c++){float a=Random.value*Mathf.PI*2;Vector3 cp=p+new Vector3(Mathf.Cos(a)*radius*.6f,radius*.6f,Mathf.Sin(a)*radius*.6f);Coral(cp,Random.Range(.55f,1.3f),Random.Range(0,7),root.transform,i+c);}}
  }
  if(lush)for(int i=0;i<(zone==0?85:32);i++){float z=center+Random.Range(-16f,17f),x=Random.Range(-14f,14f);if(Mathf.Abs(x-Mathf.Sin(z*.115f)*2)<3.1f)continue;Vector3 p=new(x,ReefGeometry.Floor(x,z),z);if(i%3==0)Coral(p,Random.Range(.4f,.85f),i%7,root.transform,i);else Model("Seagrass patch",Mesh("Seagrass_"+(i%3),()=>ReefGeometry.Grass(i%3+50)),grass,p,Vector3.one*Random.Range(.7f,1.6f),root.transform);}
  for(int t=0;t<3;t++){Vector3 p=new(-1.8f+t*1.8f,0,center+t*5-5);p.y=ReefGeometry.Floor(p.x,p.z)+.12f;var shell=Model("Teleport | pale clam shell",Mesh("TeleportShell",()=>{var b=new ReefGeometry();b.Ellipsoid(Vector3.zero,new(.32f,.09f,.24f),14,24,.08f,1);return b.Mesh("Clam shell");}),sand,p,Vector3.one,root.transform);shell.AddComponent<MeshCollider>().sharedMesh=shell.GetComponent<MeshFilter>().sharedMesh;shell.AddComponent<TeleportShell>();}
  if(zone==2||zone==7)Wreck(new Vector3(-7,OceanWorld.Centers[zone].y-3,center),root.transform);
  if(zone==3||zone==5)for(int i=0;i<14;i++)Jelly(OceanWorld.Centers[zone]+new Vector3(Random.Range(-9,9),Random.Range(-1,4),Random.Range(-10,14)),zone==5&&i%5==0?"Box jelly":zone==5?"Moon jelly":"Comb jelly",root.transform);
  if(zone==3){var lumen=Fish("Lumen",OceanWorld.Centers[zone]+new Vector3(0,0,3),blue,.9f,root.transform);var life=lumen.AddComponent<SeaLife>();life.kind="Lumen";var glow=new GameObject("Lumen organic lure");glow.transform.SetParent(lumen.transform,false);glow.transform.localPosition=new(0,.5f,.4f);var l=glow.AddComponent<Light>();l.color=new Color(.12f,.75f,1);l.range=5;l.intensity=2;}
  if(zone==4){for(int i=0;i<12;i++){var f=Fish("Anglerfish",OceanWorld.Centers[zone]+new Vector3(Random.Range(-9,9),Random.Range(-3,4),Random.Range(-10,10)),blue,.5f,root.transform);var l=f.AddComponent<Light>();l.type=LightType.Point;l.color=Color.cyan;l.range=1.5f;l.intensity=.35f;}var b=new ReefGeometry();b.Ellipsoid(Vector3.zero,new(1.8f,1.8f,7),24,32);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;b.Tube(new[]{new Vector3(Mathf.Cos(a),Mathf.Sin(a),-5),new(Mathf.Cos(a)*2,Mathf.Sin(a)*2,-10),new(Mathf.Cos(a)*3,Mathf.Sin(a)*3,-16)},new[]{.3f,.18f,.01f},10,Color.white);}var squid=Model("Giant squid distant silhouette",Save(b.Mesh("Giant squid"),Art+"Meshes/GiantSquid.asset"),silver,new(14,-45,135),Vector3.one,root.transform);squid.transform.rotation=Quaternion.Euler(0,60,0);}
  if(zone==0){var colony=new GameObject("Clownfish coral territory");colony.transform.SetParent(root.transform);var shoal=colony.AddComponent<FishSchool>();shoal.habitatCenter=new Vector3(-3,-1.1f,3.5f);shoal.habitatRadius=1.8f;for(int i=0;i<5;i++){var f=Fish("Clownfish",new Vector3(-4+i*.5f,-1.1f+(i%2)*.2f,2.7f+i*.4f),gold,.28f,colony.transform);var life=f.AddComponent<SeaLife>();life.kind="Clownfish";life.grabbable=true;}var fire=Coral(new(-3,-2.5f,5),.85f,0,root.transform,12);var lifeFire=fire.AddComponent<SeaLife>();lifeFire.kind="Fire coral";lifeFire.danger=true;lifeFire.stationary=true;}
  if(zone==1){var lion=Fish("Lionfish",new(1,-8,30),corals[3],1.1f,root.transform);var life=lion.AddComponent<SeaLife>();life.kind="Lionfish";life.danger=true;for(int i=0;i<13;i++){var b=new ReefGeometry();float a=(i-6)*.18f;b.Tube(new[]{Vector3.zero,new Vector3(Mathf.Sin(a)*.65f,.6f,Mathf.Cos(a)*.2f)},new[]{.018f,.001f},6,Color.white);Model("Venomous fin ray",Save(b.Mesh("Lionfish spine "+i),Art+"Meshes/LionfishSpine"+i+".asset"),sand,Vector3.zero,Vector3.one,lion.transform);}}
  if(zone==7)for(int i=0;i<3;i++){Vector3 p=new(i*3-2,OceanWorld.Centers[zone].y-2.8f,center+i*4);var ray=Model("Resting stingray",Mesh("Manta",ReefGeometry.Ray),sand,p,Vector3.one*.7f,root.transform);var life=ray.AddComponent<SeaLife>();life.kind="Stingray";life.stationary=true;var tail=new GameObject("Tail-only caution zone");tail.transform.SetParent(ray.transform,false);tail.transform.localPosition=new(0,.05f,-1.5f);var hazard=tail.AddComponent<SeaLife>();hazard.kind="Ray tail";hazard.danger=true;hazard.stationary=true;}
 }
 static GameObject Coral(Vector3 p,float size,int type,Transform parent,int seed){
  type%=7;GameObject g;
  if((type==0||type==5||type==6)&&AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/Acropora_LOD0.fbx")){
   string species="Secale"; // Other museum scans retain visible mounting blocks.
   if(!AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/"+species+"_LOD0.fbx"))species="Acropora";
   Material scanMaterial=scanPalette[Math.Abs(seed)%scanPalette.Length];
   g=new GameObject("Scanned "+species+" | Smithsonian");g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=Vector3.one*size*1.8f;var group=g.AddComponent<LODGroup>();var lods=new LOD[3];for(int l=0;l<3;l++){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/"+species+"_LOD"+l+".fbx");var child=(GameObject)PrefabUtility.InstantiatePrefab(asset);child.transform.SetParent(g.transform,false);var rs=child.GetComponentsInChildren<Renderer>();foreach(var r in rs)r.sharedMaterial=scanMaterial;lods[l]=new LOD(l==0?.12f:l==1?.045f:.008f,rs);}group.SetLODs(lods);group.RecalculateBounds();
  }else{
   int variant=seed%3;Mesh m;Material mat;
   switch(type){case 1:m=Mesh("Plate_"+variant,()=>ReefGeometry.Plate(variant+1));mat=corals[seed%2==0?1:4];break;case 2:m=Mesh("Fan_"+variant,()=>ReefGeometry.Fan(variant+2));mat=corals[2];break;case 3:m=Mesh("Sponge_"+variant,()=>ReefGeometry.Sponge(variant+30));mat=corals[3];break;case 4:m=Mesh("Brain_"+variant,()=>{var b=new ReefGeometry();b.Ellipsoid(new(0,.35f,0),new(.65f,.55f,.6f),28,40,.07f,variant);return b.Mesh("Brain coral");});mat=brain;break;default:m=Mesh("Branches_"+variant,()=>ReefGeometry.Branching(variant+8));mat=corals[seed%2==0?5:0];break;}
   g=Model("Living coral | "+type,m,mat,p,Vector3.one*size,parent);
  }
  g.transform.localRotation=Quaternion.Euler(Random.Range(-8,8),Random.Range(0,360),Random.Range(-8,8));return g;
 }
 static GameObject Fish(string name,Vector3 p,Material mat,float scale,Transform parent,int variant=-1){
 if(name=="School member"&&variant>=0)return VarietyFish(variant,p,scale,parent);
 if(name=="School member"){
  var g=Model("Living reef fish",Mesh("PhotographicFishCanonical",CanonicalFish),fishPalette[mat==gold?1:mat==blue?2:0],p,Vector3.one*scale,parent);
  var a=g.AddComponent<FishAgent>();a.bodyLength=scale;a.cruiseSpeed=scale*Random.Range(1.2f,1.7f);return g;
 }

 var materialPath=Art+"Materials/Species "+name+".mat";var skin=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
 if(!skin){skin=new Material(fishPalette[0]);skin.color=name=="Clownfish"?new Color(.95f,.27f,.035f):name=="Lionfish"?new Color(.35f,.17f,.085f):name=="Lumen"?new Color(.32f,.63f,.72f):new Color(.22f,.3f,.32f);skin.SetFloat("_Pattern",name=="Clownfish"?1:name=="Lionfish"?2:0);skin=Save(skin,materialPath);}
 var speciesMesh=Mesh("Detailed "+name,()=>{var m=UnityEngine.Object.Instantiate(Mesh("PhotographicFishCanonical",CanonicalFish));var v=m.vertices;for(int i=0;i<v.Length;i++){v[i].y*=name=="Clownfish"?1.35f:name=="Anglerfish"?1.6f:1;v[i].x*=name=="Anglerfish"?1.4f:1;v[i].z*=name=="Clownfish"?.85f:name=="Anglerfish"?.8f:1;}m.vertices=v;m.RecalculateNormals();m.RecalculateTangents();m.bounds=new Bounds(Vector3.zero,new Vector3(.7f,.9f,1.2f));return m;});
 var f=Model(name,speciesMesh,skin,p,Vector3.one*scale,parent);if(name=="Clownfish"){var a=f.AddComponent<FishAgent>();a.bodyLength=scale;a.cruiseSpeed=scale*1.05f;}else f.AddComponent<FishBodyMotion>();return f;
 }
 static Mesh CanonicalFish(){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"Scans/PhotographicFish.fbx");
  var temp=(GameObject)PrefabUtility.InstantiatePrefab(source);
  var parts=temp.GetComponentsInChildren<MeshFilter>();var combine=new CombineInstance[parts.Length];
  for(int i=0;i<parts.Length;i++)combine[i]=new CombineInstance{mesh=parts[i].sharedMesh,transform=parts[i].transform.localToWorldMatrix};
  var mesh=new Mesh{name="Fish canonical: head +Z, tail -Z",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine,true,true);UnityEngine.Object.DestroyImmediate(temp);
  mesh.RecalculateBounds();var bounds=mesh.bounds;var vertices=mesh.vertices;float length=bounds.size.z;
  if(length<bounds.size.y||length<.01f)throw new Exception("Fish import orientation invalid: "+bounds.size);
  for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-bounds.center)/length;
  mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.bounds=new Bounds(Vector3.zero,new Vector3(.55f,.65f,1.2f));return mesh;
 }
 static void School(Vector3 p,int count,float radius,Material mat,float size){
  var root=new GameObject("School | "+count+" reef fish");root.transform.SetParent(world);
  var school=root.AddComponent<FishSchool>();school.habitatCenter=p;school.habitatRadius=radius+2;
  for(int i=0;i<count;i++){
   Vector3 spawn=p;Physics.SyncTransforms();
   for(int attempt=0;attempt<60;attempt++){spawn=p+new Vector3(Random.Range(-radius,radius),Random.Range(-radius*.18f,radius*.18f)+attempt*.015f,Random.Range(-radius*.6f,radius*.6f));bool clear=!Physics.CheckSphere(spawn,size*.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);foreach(Transform other in root.transform)if(Vector3.Distance(spawn,other.position)<size*1.5f)clear=false;if(clear)break;}
   int variant=count==7?i%6:mat==gold?4:mat==blue?1:5;var f=Fish("School member",spawn,mat,size*Random.Range(.82f,1.15f),root.transform,variant);
   f.transform.rotation=Quaternion.Euler(0,Random.Range(65,105),0);
  }
 }
 static void Turtle(Vector3 p,float angle){CreateLoggerhead(p,angle);}
 static void Jelly(Vector3 p,string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent);g.transform.position=p;var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"Materials/JellyTissue.mat");if(!mat)mat=Save(new Material(Shader.Find("Underwater/Translucent Jelly Tissue")),Art+"Materials/JellyTissue.mat");Model("Pulsing bell",Mesh("JellyBell",()=>{var b=new ReefGeometry();int rings=28,sides=64;for(int r=0;r<=rings;r++)for(int a=0;a<=sides;a++){float t=r/(float)rings*1.75f,angle=a*2*Mathf.PI/sides;float radius=Mathf.Sin(t)*.4f;float y=Mathf.Cos(t)*.24f+Mathf.Pow(r/(float)rings,8)*Mathf.Sin(angle*24)*.012f;b.Vertex(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius),new Vector2(a/(float)sides,r/(float)rings),Color.white);}for(int r=0;r<rings;r++)for(int a=0;a<sides;a++){int j=r*(sides+1)+a;b.Quad(j,j+1,j+sides+2,j+sides+1);}return b.Mesh("Jelly bell");}),mat,Vector3.zero,Vector3.one,g.transform);
 for(int i=0;i<10;i++){int id=i;var tent=Mesh("JellyTentacle"+i,()=>{var b=new ReefGeometry();var points=new Vector3[14];var r=new float[14];for(int j=0;j<14;j++){float t=j/13f;points[j]=new(Mathf.Sin(t*7+id)*.045f,-t*1.2f,Mathf.Cos(t*6+id)*.045f);r[j]=.004f*(1-t*.8f);}b.Tube(points,r,5,Color.white);return b.Mesh("Jelly tentacle");});float a=i*6.28f/10;Model("Trailing tentacle",tent,mat,new(Mathf.Cos(a)*.22f,-.1f,Mathf.Sin(a)*.22f),Vector3.one,g.transform);}var life=g.AddComponent<SeaLife>();life.kind=name;life.danger=name!="Comb jelly";life.consequence=name=="Box jelly";}
 static void Wreck(Vector3 p,Transform parent){var root=new GameObject("Coral colonized timber wreck");root.transform.SetParent(parent);root.transform.position=p;root.transform.rotation=Quaternion.Euler(0,20,-13);var rust=Mat("Wreck mineral deposits",new Color(.3f,.2f,.11f),"coral_ground_02_diff_2k.jpg","coral_ground_02_nor_gl_2k.jpg",null,.7f);
 for(int r=0;r<12;r++){int id=r;var m=Mesh("WreckRib"+r,()=>{var b=new ReefGeometry();Vector3[] points=new Vector3[15];float[] radii=new float[15];for(int j=0;j<15;j++){float a=Mathf.Lerp(-1.3f,1.3f,j/14f);points[j]=new(Mathf.Sin(a)*2.7f,(1-Mathf.Cos(a))*3,0);radii[j]=.12f;}b.Tube(points,radii,6,Color.white);return b.Mesh("Hull rib");});Model("Exposed hull rib",m,rust,new(0,0,r*1.1f-6),Vector3.one,root.transform);}
 for(int i=0;i<24;i++){float side=i%2==0?-1:1;var g=OceanWorld.Shape("Broken hull plank",PrimitiveType.Cube,new(side*2.3f,Random.Range(.2f,1.6f),Random.Range(-6,6)),new(.15f,.23f,Random.Range(1.7f,4.5f)),Color.white,root.transform);g.GetComponent<Renderer>().sharedMaterial=rust;g.transform.localRotation=Quaternion.Euler(Random.Range(-12,12),0,side*-28);}
 }
 static void CreateDiver(){var rig=new GameObject("Diver XR Origin");rig.transform.position=new(0,-1.3f,-9);rig.AddComponent<Diver>();var head=new GameObject("Head");head.tag="MainCamera";head.transform.SetParent(rig.transform,false);head.transform.localPosition=new(0,1.6f,0);var cam=head.AddComponent<Camera>();cam.nearClipPlane=.06f;cam.farClipPlane=110;cam.fieldOfView=64;cam.allowHDR=true;cam.allowMSAA=true;head.AddComponent<AudioListener>();head.AddComponent<FishPreviewCapture>();var optics=head.AddComponent<ReefOptics>();optics.shader=Shader.Find("Hidden/Underwater/Optics");}
 static void Particles(Vector3 p,string name,Vector3 bounds,int max){var obj=new GameObject(name);obj.transform.SetParent(world);obj.transform.position=p;var ps=obj.AddComponent<ParticleSystem>();var main=ps.main;main.startLifetime=22;main.startSpeed=.035f;main.startSize=new ParticleSystem.MinMaxCurve(.012f,.035f);main.maxParticles=max;main.simulationSpace=ParticleSystemSimulationSpace.World;main.prewarm=true;main.startColor=new Color(.55f,.8f,.8f,.35f);var emission=ps.emission;emission.rateOverTime=max/22f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=bounds;var noise=ps.noise;noise.enabled=true;noise.strength=.03f;noise.frequency=.15f;var renderer=ps.GetComponent<ParticleSystemRenderer>();var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"Materials/MarineSnow.mat");if(!mat)mat=Save(new Material(Shader.Find("Underwater/Marine Snow")),Art+"Materials/MarineSnow.mat");renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;}
 public static void ValidateScene(){int missing=0;foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);if(missing>0)throw new Exception("Missing scripts: "+missing);int teleports=UnityEngine.Object.FindObjectsByType<TeleportShell>(FindObjectsSortMode.None).Length;if(teleports!=24)throw new Exception("Teleport anchors missing: "+teleports);foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){foreach(var m in r.sharedMaterials)if(!m||!m.shader)throw new Exception("Missing material on "+r.name);}if(UnityEngine.Object.FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None).Length!=2)throw new Exception("Expected two detailed turtles");foreach(string species in SpeciesNames)if(!GameObject.Find(species))throw new Exception("Missing fish variety: "+species);Debug.Log("REEF_VALIDATED missingScripts=0 teleportAnchors=24 materials=valid turtleModels=2 fishVarieties=6");}
 public static void CreateAndRender(){Create();ReefCollisionValidation.Run();FishMotionValidation.Run();Render();}
 public static void Render(){
  if(!Camera.main)EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
  foreach(var fish in UnityEngine.Object.FindObjectsByType<MarineSwimmer>(FindObjectsSortMode.None)){if(fish.name=="Photographic reef fish"){var r=fish.GetComponentInChildren<Renderer>();Debug.Log("FISH_BOUNDS "+r.bounds.size.ToString("F6")+" lossyScale="+r.transform.lossyScale+" type="+r.GetType().Name);break;}}var cam=Camera.main;var atmosphere=UnityEngine.Object.FindFirstObjectByType<ReefAtmosphere>();
  foreach(var school in UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None)){school.Initialize();for(int f=0;f<60;f++)school.Simulate(1f/30);}
  foreach(var particles in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)){particles.Simulate(18,true,true);particles.Pause();}
  Capture(cam,atmosphere,"01_Sunlit_Reef",new Vector3(0,.2f,-9),new Vector3(0,-.8f,12));
  Capture(cam,atmosphere,"02_Coral_Garden",new Vector3(-3.5f,-.4f,1),new Vector3(-8,-1,9));
  Capture(cam,atmosphere,"03_Reef_Arch",new Vector3(1,-.2f,13),new Vector3(0,-.5f,25));
  Capture(cam,atmosphere,"04_Wreck",new Vector3(2,-15,53),new Vector3(-6,-18,64));
  Capture(cam,atmosphere,"05_Twilight",new Vector3(0,-28,82),new Vector3(0,-29,98));
  Capture(cam,atmosphere,"07_Fish_Detail",new Vector3(-.5f,-.2f,-2),new Vector3(0,-.2f,2));
  cam.fieldOfView=42;Capture(cam,atmosphere,"08_Loggerhead_Closeup",new Vector3(0,.65f,8.2f),new Vector3(1.7f,-.25f,7.5f));cam.fieldOfView=64;
  Debug.Log("REEF_RENDER_COMPLETE");
 }
 static void Capture(Camera cam,ReefAtmosphere atmosphere,string name,Vector3 pos,Vector3 target){cam.transform.position=pos;cam.transform.LookAt(target);atmosphere.Apply(cam);var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();Directory.CreateDirectory("Renders");File.WriteAllBytes("Renders/"+name+".png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
 public static void BuildWindows(){OceanBuild.ConfigureXR();EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Underwater_Realistic.unity",true)};var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Builds/RealisticWindows/Underwater Exploration.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Realistic reef build failed.");}
}
}













