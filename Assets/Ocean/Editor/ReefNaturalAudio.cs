using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Underwater.Editor {
public static class ReefNaturalAudio {
 const int Rate=24000;const string Folder="Assets/Ocean/Resources/";
 public static void Apply(){
  Bake("ReefWaterAmbience",32,0);if(!File.Exists("Tools/NaturalAudio/NOAA_NEP_BlueWhale_10x.wav")||!File.Exists(Folder+"ReefWhaleCalls.wav"))Bake("ReefWhaleCalls",48,1);Bake("ReefFishSounds",27,2);AssetDatabase.Refresh();
  foreach(string name in new[]{"ReefWaterAmbience","ReefWhaleCalls","ReefFishSounds"}){var importer=(AudioImporter)AssetImporter.GetAtPath(Folder+name+".wav");var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;sample.preloadAudioData=true;importer.defaultSampleSettings=sample;importer.SaveAndReimport();}
  Validate();Capture();
 }
 // Original nonmusical sound design: filtered water noise, bubbles, low cetacean-like calls and short reef clicks.
 static void Bake(string name,int seconds,int kind){int frames=seconds*Rate;var random=new System.Random(812+kind);float lowL=0,lowR=0,slowL=0,slowR=0;double phase=0;
  using(var writer=new BinaryWriter(File.Create(Folder+name+".wav"))){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+frames*4);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)2);writer.Write(Rate);writer.Write(Rate*4);writer.Write((short)4);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(frames*4);
   for(int i=0;i<frames;i++){float t=i/(float)Rate;lowL=Mathf.Lerp(lowL,(float)random.NextDouble()*2-1,.024f);lowR=Mathf.Lerp(lowR,(float)random.NextDouble()*2-1,.024f);slowL=Mathf.Lerp(slowL,lowL,.006f);slowR=Mathf.Lerp(slowR,lowR,.006f);float l=0,r=0;
    if(kind==0){float swell=.65f+.18f*Mathf.Sin(t*.43f)+.12f*Mathf.Sin(t*.91f);l=(lowL*.48f+slowL*.8f)*swell;r=(lowR*.48f+slowR*.8f)*swell;float age=t%5.7f;for(int b=0;b<8;b++){float a=age-.3f-b*.083f;if(a>0&&a<.16f){float g=Mathf.Sin(2*Mathf.PI*(480+b*69)*a*(1-a*.7f))*Mathf.Exp(-a*48)*.035f;l+=g;r+=g*.7f;}}}
    if(kind==1){float age=(t+2)%17;float envelope=age>2&&age<8?Mathf.Sin((age-2)/6*Mathf.PI):0;float f=44-age*1.5f;phase+=2*Math.PI*f/Rate;float call=((float)Math.Sin(phase)*.3f+(float)Math.Sin(phase*2)*.14f+(float)Math.Sin(phase*3)*.07f)*envelope*envelope;l=call+slowL*.02f;r=call*.85f+slowR*.02f;}
    if(kind==2){float age=t%3.71f;for(int b=0;b<4;b++){float a=age-.4f-b*.14f;if(a>0&&a<.085f){float click=(Mathf.Sin(2*Mathf.PI*(210+b*85)*a)+lowL)*Mathf.Exp(-a*100)*Mathf.Sin(Mathf.Min(1,a/.003f)*Mathf.PI/2)*.16f;l+=click;r+=click*(b%2==0?.55f:1);}}}
    float edge=Mathf.Min(1,Mathf.Min(t/.25f,(seconds-t)/.25f));writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(l*edge,-.95f,.95f)*32767));writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(r*edge,-.95f,.95f)*32767));
   }
  }
 }
 public static void Validate(){
  foreach(string name in new[]{"ReefWaterAmbience","ReefWhaleCalls","ReefFishSounds"}){var clip=Resources.Load<AudioClip>(name);if(!clip||clip.channels!=2||clip.length<20)throw new Exception("Invalid ambience "+name);var samples=new float[clip.samples*clip.channels];clip.GetData(samples,0);double energy=0;float peak=0;foreach(float sample in samples){energy+=sample*sample;peak=Mathf.Max(peak,Mathf.Abs(sample));}if(peak>.96f||energy<.001)throw new Exception("Silent/clipped ambience "+name);Debug.Log("NATURAL_AUDIO_CLIP "+name+" seconds="+clip.length+" rms="+Math.Sqrt(energy/samples.Length)+" peak="+peak);}
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var cam=Camera.main;var rig=cam.transform.parent;var bubbles=rig.GetComponent<DiverBubbles>();if(!bubbles)bubbles=rig.gameObject.AddComponent<DiverBubbles>();bubbles.Initialize(cam.transform);int peakCount=0,visible=0;for(int i=0;i<300;i++){bubbles.PreviewStep(1f/60,i/60f);peakCount=Mathf.Max(peakCount,bubbles.ParticleCount);foreach(var system in rig.GetComponentsInChildren<ParticleSystem>())if(system.name.StartsWith("Regulator")){var particles=new ParticleSystem.Particle[64];int count=system.GetParticles(particles);for(int j=0;j<count;j++){var p=cam.WorldToViewportPoint(particles[j].position);if(p.z>.25f&&p.x>.06f&&p.x<.94f&&p.y>.06f&&p.y<.94f)visible++;}}}
  if(peakCount==0||peakCount>64||visible<30)throw new Exception("Body bubbles not visible/bounded: "+visible);Debug.Log("BODY_BUBBLES_VALIDATED peak="+peakCount+" visibleSamples="+visible);ReefAnimationCorrections.Validate();
 }
 public static void Capture(){ImmersionPreview.Run();}
}
}
