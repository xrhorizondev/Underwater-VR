using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Underwater {
public class FishPreviewCapture:MonoBehaviour {
 IEnumerator Start(){
  bool turtleMode=Array.IndexOf(Environment.GetCommandLineArgs(),"--turtle-preview")>=0;
  if(!turtleMode&&Array.IndexOf(Environment.GetCommandLineArgs(),"--fish-preview")<0)yield break;
  Application.runInBackground=true;
  yield return null;
  var diver=FindFirstObjectByType<Diver>();if(diver){diver.enabled=false;foreach(Transform child in diver.transform)if(child.name.Contains("glove"))child.gameObject.SetActive(false);}
  var camera=GetComponent<Camera>();camera.transform.position=new Vector3(-.5f,.1f,-3.5f);camera.transform.LookAt(new Vector3(0,-.2f,3));
  FishAgent hero=null;foreach(var school in FindObjectsByType<FishSchool>(FindObjectsSortMode.None))if(school.name.Contains("School | 7")){hero=school.GetComponentsInChildren<FishAgent>()[3];break;}camera.fieldOfView=48;
  TurtleSwimmer turtle=null;if(turtleMode){foreach(var candidate in FindObjectsByType<TurtleSwimmer>(FindObjectsSortMode.None))if(candidate.transform.position.z<50)turtle=candidate;camera.fieldOfView=42;}
  Time.captureDeltaTime=1f/24;
  string folder=Path.Combine(Application.dataPath,turtleMode?"../TurtleMotionFrames":"../FishMotionFrames");Directory.CreateDirectory(folder);
  var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};
  var image=new Texture2D(960,540,TextureFormat.RGB24,false);
  for(int frame=0;frame<144;frame++){
   yield return null;
   if(turtle){camera.transform.position=turtle.transform.position+new Vector3(-1.65f,.85f,1.1f);camera.transform.LookAt(turtle.transform.position+Vector3.up*.03f);}
   else if(hero){camera.transform.position=hero.transform.position+new Vector3(-1.7f,.18f,-2.3f);camera.transform.LookAt(hero.transform.position);}
   camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
   File.WriteAllBytes(Path.Combine(folder,$"frame_{frame:0000}.png"),image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;
  }
  Time.captureDeltaTime=0;Destroy(image);rt.Release();Destroy(rt);
  Debug.Log((turtleMode?"TURTLE":"FISH")+"_PREVIEW_COMPLETE frames=144 fps=24 seconds=6");Application.Quit();
 }
}
}
