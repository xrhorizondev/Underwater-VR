using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
public static class CombJellyComparisonScene {
 const string Folder="Assets/UnderwaterArt/CombJellyComparison/";
 const string Scene="Assets/Scenes/CombJelly_Comparison.unity";
 [MenuItem("Underwater/Open comb jelly comparison")]
 public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);}
 public static void Create(){
 AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 var tex=(TextureImporter)AssetImporter.GetAtPath(Folder+"Tissue.png");tex.alphaSource=TextureImporterAlphaSource.FromInput;tex.alphaIsTransparency=true;tex.maxTextureSize=2048;tex.textureCompression=TextureImporterCompression.Uncompressed;tex.SaveAndReimport();
 var shader=Shader.Find("Underwater/Reference Comb Tissue");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Tissue shader failed");
 var mat=new Material(shader){name="Translucent tissue and iridescent comb rows"};mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Tissue.png");AssetDatabase.CreateAsset(mat,Folder+"Tissue.mat");
 var cam=new GameObject("Comparison Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(2.5f,0,.02f);cam.transform.LookAt(Vector3.zero);cam.orthographic=true;cam.orthographicSize=.43f;cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.backgroundColor=new Color(.008f,.018f,.031f);cam.clearFlags=CameraClearFlags.SolidColor;
 RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
 var report=new System.Text.StringBuilder();int i=0;
 foreach(var name in new[]{"Full","Reduced"}){
 var path=Folder+name+".fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Legacy;imp.importAnimation=true;imp.importCameras=false;imp.importLights=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.isReadable=true;imp.meshCompression=ModelImporterMeshCompression.Off;
 var specs=imp.defaultClipAnimations;foreach(var spec in specs){spec.name="GentlePulse";spec.loopTime=true;spec.wrapMode=WrapMode.Loop;}imp.clipAnimations=specs;imp.SaveAndReimport();
 var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.name=name+" detail comb jelly";
 var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length!=1)throw new Exception("Expected one skin per specimen");var skin=skins[0];skin.sharedMaterial=mat;skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;skin.updateWhenOffscreen=true;var bounds=skin.localBounds;bounds.Expand(.12f);skin.localBounds=bounds;
 // Blender X remains X; upright Z is exported as Unity Y, lateral Y becomes Z.
 model.transform.position=new Vector3(0,0,i==0?-.31f:.31f);
 var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var a=model.GetComponent<Animation>();if(!a)a=model.AddComponent<Animation>();a.AddClip(clip,clip.name);a.clip=clip;a.playAutomatically=true;a.wrapMode=WrapMode.Loop;a.cullingType=AnimationCullingType.AlwaysAnimate;model.AddComponent<CombJellyComparisonPlayback>().clip=clip;
 var m0=new Mesh();var m1=new Mesh();clip.SampleAnimation(model,0);skin.BakeMesh(m0);clip.SampleAnimation(model,1);skin.BakeMesh(m1);float moved=0;var va=m0.vertices;var vb=m1.vertices;for(int n=0;n<va.Length;n++)moved=Mathf.Max(moved,(va[n]-vb[n]).magnitude);if(moved<.001f)throw new Exception("Frozen pulse "+name);clip.SampleAnimation(model,0);skin.BakeMesh(m1);float seam=0;clip.SampleAnimation(model,clip.length);skin.BakeMesh(m0);va=m0.vertices;vb=m1.vertices;for(int n=0;n<va.Length;n++)seam=Mathf.Max(seam,(va[n]-vb[n]).magnitude);if(seam>.001f)throw new Exception("Pulse seam "+seam);
 int count=skin.sharedMesh.triangles.Length/3;report.AppendLine(name+": "+count+" triangles; "+skin.bones.Length+" bones; clip "+clip.length+" sec; max motion "+moved+" m; seam "+seam+" m");UnityEngine.Object.DestroyImmediate(m0);UnityEngine.Object.DestroyImmediate(m1);clip.SampleAnimation(model,0);
 Label(name.ToUpper()+" DETAIL\n"+count.ToString("N0")+" triangles",new Vector3(0,.34f,i==0?-.31f:.31f),cam,.0024f);i++;
 }
 Label("COMB JELLY  |  translucent tissue + flowing comb shimmer",new Vector3(0,-.34f,0),cam,.0018f);
 AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);Directory.CreateDirectory("Renders/CombJellyComparison");File.WriteAllText("Renders/CombJellyComparison/Validation.txt",report.ToString());
 foreach(float time in new[]{0f,1f,2f,3f}){foreach(var a in UnityEngine.Object.FindObjectsByType<Animation>(FindObjectsSortMode.None))a.clip.SampleAnimation(a.gameObject,time);Capture(cam,"Renders/CombJellyComparison/Phase"+time+".png");}
 Debug.Log("COMB_JELLY_COMPARISON_VALIDATED\n"+report);
 }
 public static void CaptureAdditional(){
 EditorSceneManager.OpenScene(Scene);var c=Camera.main;var models=UnityEngine.Object.FindObjectsByType<CombJellyComparisonPlayback>(FindObjectsSortMode.None);Directory.CreateDirectory("Renders/CombJellyComparison/Animation");
 for(int f=0;f<48;f++){foreach(var m in models)m.clip.SampleAnimation(m.gameObject,f/12f);Capture(c,"Renders/CombJellyComparison/Animation/Frame"+f.ToString("D3")+".png");}
 foreach(var m in models)m.clip.SampleAnimation(m.gameObject,0);
 foreach(float angle in new[]{35f,90f,180f}){foreach(var m in models)m.transform.rotation=Quaternion.Euler(0,angle,0);Capture(c,"Renders/CombJellyComparison/View"+angle+".png");}
 Debug.Log("COMB_JELLY_EXTRA_VIEWS_CAPTURED");
 }
 static void Label(string text,Vector3 pos,Camera cam,float size){var g=new GameObject(text);g.transform.position=pos;g.transform.rotation=cam.transform.rotation;var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.8f,.9f,1);}
 static void Capture(Camera c,string path){var rt=new RenderTexture(1600,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}

