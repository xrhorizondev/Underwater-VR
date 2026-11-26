using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class SquidFullBodyReview {
 const string Folder="Assets/UnderwaterArt/SquidFullBody",Output="Renders/SquidFullBody/";
 public static void Create(){
  Directory.CreateDirectory(Output);AssetDatabase.Refresh();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.48f,.55f);
  var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/SquidSkin.mat");if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Folder+"/SquidSkin.mat");}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/SquidAlbedo.png");mat.SetFloat("_Glossiness",.28f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
  foreach(string tex in new[]{"SquidAlbedo"}){var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"/"+tex+".png");ti.maxTextureSize=1024;ti.mipmapEnabled=true;var a=ti.GetPlatformTextureSettings("Android");a.overridden=true;a.maxTextureSize=1024;a.format=TextureImporterFormat.ASTC_6x6;ti.SetPlatformTextureSettings(a);ti.SaveAndReimport();}
  var report=new StringBuilder();var instances=new GameObject[2];var skins=new SkinnedMeshRenderer[2];var players=new Animation[2];
  for(int i=0;i<2;i++){
   string name=i==0?"SquidDetail":"SquidQuest",path=Folder+"/"+name+".fbx";
   var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Legacy;imp.importAnimation=true;imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;
   var clips=imp.defaultClipAnimations;if(clips.Length!=1)throw new Exception("Expected one Swim clip: "+path);
   foreach(var c in clips){c.name="Swim";c.loopTime=true;c.wrapMode=WrapMode.Loop;}imp.clipAnimations=clips;imp.SaveAndReimport();
   var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=i==0?"Full body - detail comparison":"Full body - Quest reduced copy";
   var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMaterial=mat;skin.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skin.receiveShadows=false;
   var anim=go.GetComponent<Animation>();if(!anim||!anim.clip)throw new Exception("Missing animation");anim.wrapMode=WrapMode.Loop;anim.playAutomatically=true;anim.cullingType=AnimationCullingType.BasedOnRenderers;
   Bounds envelope=default;Vector3[] start=null,end=null;float movement=0;bool first=true;
   for(int f=0;f<=48;f++){
    anim.clip.SampleAnimation(go,anim.clip.length*f/48);var mesh=new Mesh();skin.BakeMesh(mesh);var points=mesh.vertices;
    if(f==0)start=points;if(f==48)end=points;
    for(int v=0;v<points.Length;v++){if(first){envelope=new Bounds(points[v],Vector3.zero);first=false;}else envelope.Encapsulate(points[v]);if(f==12)movement=Mathf.Max(movement,Vector3.Distance(points[v],start[v]));if(!float.IsFinite(points[v].x)||!float.IsFinite(points[v].y)||!float.IsFinite(points[v].z))throw new Exception("Nonfinite deformation");}
    UnityEngine.Object.DestroyImmediate(mesh);
   }
   float seam=start.Zip(end,(a,b)=>Vector3.Distance(a,b)).Max();if(seam>.002f||movement<.005f)throw new Exception("Invalid animation seam/motion "+seam+" / "+movement);
   envelope.Expand(.15f);float bakeScale=skin.transform.lossyScale.x;skin.localBounds=new Bounds(envelope.center/bakeScale,envelope.size/bakeScale);skin.updateWhenOffscreen=false;anim.clip.SampleAnimation(go,0);
   Debug.Log("SQUID_BOUNDS "+name+" mesh="+skin.sharedMesh.bounds+" sampled="+envelope+" world="+skin.bounds+" scale="+skin.transform.lossyScale+" rootScale="+skin.rootBone.lossyScale);
   int triangles=skin.sharedMesh.triangles.Length/3;if(i==1&&triangles>8000)throw new Exception("Quest triangle budget exceeded");
   PrefabUtility.SaveAsPrefabAsset(go,Folder+"/"+name+".prefab");
   go.transform.position=new Vector3(0,i==0?1.3f:-1.3f,0);instances[i]=go;skins[i]=skin;players[i]=anim;
   report.AppendLine(name+": triangles="+triangles+", bones="+skin.bones.Length+", materials="+skin.sharedMaterials.Length+", clip="+anim.clip.name+", seconds="+anim.clip.length+", maximum deformation="+movement+", loop seam="+seam+", sampled frames=49");
  }
  var focus=new GameObject("Review focus").transform;focus.position=Vector3.zero;
  var camera=new GameObject("Review camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.055f,.085f);camera.nearClipPlane=.03f;camera.farClipPlane=60;camera.fieldOfView=43;
  camera.transform.position=new Vector3(8,2,-4);camera.transform.LookAt(focus);
  var controls=camera.gameObject.AddComponent<Underwater.SquidReviewControls>();controls.focus=focus;controls.players=players;controls.distance=9;
  foreach(var p in new[]{new Vector3(2,4,-2),new Vector3(-3,2,3)}){var l=new GameObject("Studio light",typeof(Light)).GetComponent<Light>();l.type=LightType.Directional;l.intensity=.85f;l.transform.rotation=Quaternion.LookRotation(-p);}
  Label("DETAIL COMPARISON",new Vector3(-2.7f,2.15f,0));Label("QUEST REDUCED COPY",new Vector3(-2.7f,-.45f,0));
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/Squid_FullBody_Review.unity");AssetDatabase.SaveAssets();
  // Whole-body views and four animation phases of the reduced copy.
  foreach(float phase in new[]{0f,.25f,.5f,.75f}){foreach(var a in players)a.clip.SampleAnimation(a.gameObject,a.clip.length*phase);Capture(camera,Output+"Comparison_"+(int)(phase*100)+".png");}
  instances[0].SetActive(false);camera.transform.position=new Vector3(6,-.5f,1);camera.transform.LookAt(instances[1].transform.position);
  Capture(camera,Output+"ReducedSide.png");camera.transform.position=new Vector3(-6,-.5f,1);camera.transform.LookAt(instances[1].transform.position);Capture(camera,Output+"ReducedOpposite.png");
  instances[0].SetActive(true);File.WriteAllText(Output+"UnityValidation.txt",report.ToString()+"Separate review scene saved. Main underwater scene untouched. No Quest hardware performance test.\n");
  Debug.Log("SQUID_FULL_BODY_REVIEW_PASSED\n"+report);
 }
 static void Label(string text,Vector3 p){var go=new GameObject(text,typeof(TextMesh));go.transform.position=p;go.transform.rotation=Camera.main.transform.rotation;var t=go.GetComponent<TextMesh>();t.text=text;t.fontSize=40;t.characterSize=.025f;t.color=new Color(.7f,.85f,.95f);}
 static void Capture(Camera c,string path){foreach(var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.transform.rotation=c.transform.rotation;var rt=new RenderTexture(1400,1000,24);c.targetTexture=rt;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1400,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1400,1000),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
}
}
