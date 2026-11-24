using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
public static class MorayComparisonScene {
 const string Folder="Assets/UnderwaterArt/MorayComparison/",Scene="Assets/Scenes/Moray_Animation_Comparison.unity";
 [MenuItem("Underwater/Open animated moray comparison")]
 public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);}
 static Vector3[] Points(SkinnedMeshRenderer skin){var mesh=skin.sharedMesh;var v=mesh.vertices;var w=mesh.boneWeights;var m=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();for(int i=0;i<v.Length;i++){var p=v[i];var a=w[i];v[i]=m[a.boneIndex0].MultiplyPoint3x4(p)*a.weight0+m[a.boneIndex1].MultiplyPoint3x4(p)*a.weight1+m[a.boneIndex2].MultiplyPoint3x4(p)*a.weight2+m[a.boneIndex3].MultiplyPoint3x4(p)*a.weight3;}return v;}
 public static void Create(){
 AssetDatabase.Refresh();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 foreach(string name in new[]{"Albedo","Normal","ORM"}){var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+name+".png");ti.maxTextureSize=4096;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Uncompressed;if(name=="Normal")ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();}
 var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"MoraySkin.mat");if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Folder+"MoraySkin.mat");}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Albedo.png");mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Normal.png"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.25f);mat.SetFloat("_Glossiness",.08f);mat.SetFloat("_SpecularHighlights",0);mat.SetFloat("_GlossyReflections",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
 var cam=new GameObject("Moray comparison camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(3,0,0);cam.transform.LookAt(Vector3.zero);cam.orthographic=true;cam.orthographicSize=.83f;cam.nearClipPlane=.01f;cam.farClipPlane=15;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.04f,.055f);
 RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.58f,.58f);var light=new GameObject("Soft shared key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(35,-65,0);
 int index=0;
 foreach(string kind in new[]{"Straight","Curled"})foreach(string variant in new[]{"Full","Reduced"}){
 string path=Folder+kind+"_"+variant+".fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Legacy;imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.meshCompression=ModelImporterMeshCompression.Off;var clips=imp.defaultClipAnimations;foreach(var c in clips){c.name="FullBodySwim";c.loopTime=true;c.wrapMode=WrapMode.Loop;}imp.clipAnimations=clips;imp.SaveAndReimport();
 var root=new GameObject(kind+" "+variant);root.transform.rotation=Quaternion.Euler(0,180,0);root.transform.position=new Vector3(0,kind=="Straight"?.37f:-.31f,variant=="Full"?-.64f:.64f);
 var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.transform.SetParent(root.transform,false);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
 var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__"));var a=model.GetComponent<Animation>();if(!a)a=model.AddComponent<Animation>();a.AddClip(clip,clip.name);a.clip=clip;a.playAutomatically=true;a.wrapMode=WrapMode.Loop;a.cullingType=AnimationCullingType.AlwaysAnimate;model.AddComponent<MorayComparisonPlayback>().clip=clip;
 var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMaterial=mat;skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;skin.updateWhenOffscreen=true;
 clip.SampleAnimation(model,0);var points=Points(skin);var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);model.transform.position+=root.transform.position-b.center;
 bool first=true;Bounds envelope=default;for(int f=0;f<=24;f++){clip.SampleAnimation(model,clip.length*f/24);foreach(var p in Points(skin)){var v=(skin.rootBone?skin.rootBone:skin.transform).InverseTransformPoint(p);if(first){envelope=new Bounds(v,Vector3.zero);first=false;}else envelope.Encapsulate(v);}}envelope.Expand(.03f);skin.localBounds=envelope;clip.SampleAnimation(model,0);
 Label(kind.ToUpper()+" | "+variant.ToUpper()+"\n"+(skin.sharedMesh.triangles.Length/3).ToString("N0",System.Globalization.CultureInfo.InvariantCulture)+" triangles",new Vector3(0,kind=="Straight"?.66f:-.03f,root.transform.position.z),cam,.0055f);index++;
 }
 Label("FULL-BODY MORAY SWIM  |  shared original textures  |  4-second loop",new Vector3(0,-.73f,0),cam,.0035f);
 AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Scene);Validate();Capture();Debug.Log("MORAY_COMPARISON_CREATED");
 }
 public static void Validate(){
 EditorSceneManager.OpenScene(Scene);var models=UnityEngine.Object.FindObjectsByType<MorayComparisonPlayback>(FindObjectsSortMode.None);if(models.Length!=4)throw new Exception("Expected four morays");var report=new System.Text.StringBuilder();
 foreach(var model in models){var clip=model.clip;var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();if(skin.bones.Length!=22||!skin.sharedMaterial.mainTexture)throw new Exception("Rig/texture binding invalid");clip.SampleAnimation(model.gameObject,0);var first=Points(skin);clip.SampleAnimation(model.gameObject,1);var second=Points(skin);float moved=first.Zip(second,(a,b)=>(a-b).magnitude).Max();clip.SampleAnimation(model.gameObject,clip.length);var last=Points(skin);float seam=first.Zip(last,(a,b)=>(a-b).magnitude).Max();if(moved<.005f||seam>.0001f)throw new Exception("Frozen swim or loop seam: "+moved+" / "+seam);
 var animated=skin.bones.Count(b=>{clip.SampleAnimation(model.gameObject,0);var q=b.localRotation;clip.SampleAnimation(model.gameObject,1);return Quaternion.Angle(q,b.localRotation)>.1f;});if(animated<20)throw new Exception("Full body animation incomplete");
 report.AppendLine(model.transform.parent.name+": "+skin.sharedMesh.triangles.Length/3+" triangles; "+skin.bones.Length+" bones, "+animated+" animated; maximum displacement="+moved+" m; loop seam="+seam+" m");clip.SampleAnimation(model.gameObject,0);
 }
 Directory.CreateDirectory("Renders/MorayComparison");File.WriteAllText("Renders/MorayComparison/Validation.txt",report.ToString());Debug.Log("MORAY_IMPORT_VALIDATED\n"+report);
 }
 public static void Capture(){
 EditorSceneManager.OpenScene(Scene);var cam=Camera.main;var models=UnityEngine.Object.FindObjectsByType<MorayComparisonPlayback>(FindObjectsSortMode.None);Directory.CreateDirectory("Renders/MorayComparison/Animation");
 for(int f=0;f<48;f++){foreach(var m in models)m.clip.SampleAnimation(m.gameObject,f/12f);Render(cam,"Renders/MorayComparison/Animation/Frame"+f.ToString("D3")+".png");}
 foreach(var m in models)m.clip.SampleAnimation(m.gameObject,0);
 foreach(float angle in new[]{145f,0f,90f}){foreach(var m in models)m.transform.parent.rotation=Quaternion.Euler(0,angle,0);Render(cam,"Renders/MorayComparison/View"+angle+".png");}
 }
 static void Label(string text,Vector3 p,Camera cam,float size){var g=new GameObject(text);g.transform.position=p;g.transform.rotation=cam.transform.rotation;var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.88f,.94f,1);}
 static void Render(Camera c,string path){var rt=new RenderTexture(1600,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}


