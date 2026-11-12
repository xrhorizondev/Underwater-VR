using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
// Original seamless ambient score, baked offline to avoid synthesis work in VR.
public static class ReefMusicBake {
 const int Rate=24000,Seconds=48;
 const string Path="Assets/Ocean/Resources/ReefOceanMusic.wav";
 [MenuItem("Underwater/Bake ocean background music")]
 public static void Run(){
  double[][] chords={new[]{130.8128,195.9977,293.6648,329.6276},new[]{110.0,164.8138,261.6256,329.6276},new[]{87.3071,130.8128,195.9977,293.6648},new[]{97.9989,146.8324,220.0,293.6648}};
  double[] melody={523.2511,659.2551,587.3295,391.9954,440.0,523.2511,587.3295,391.9954};
  using(var writer=new BinaryWriter(File.Create(Path))){int bytes=Rate*Seconds*4;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)2);writer.Write(Rate);writer.Write(Rate*4);writer.Write((short)4);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);
   for(int i=0;i<Rate*Seconds;i++){double t=(double)i/Rate;for(int ear=0;ear<2;ear++){
    double value=0,pan=ear==0?-.18:.18;
    for(int chord=0;chord<4;chord++){double distance=Math.Abs(t-(chord*12+6));distance=Math.Min(distance,Seconds-distance);double envelope=distance<12?.5+.5*Math.Cos(Math.PI*distance/12):0;
     for(int note=0;note<4;note++){double f=Math.Round(chords[chord][note]*Seconds)/Seconds;double shimmer=.85+.15*Math.Sin(2*Math.PI*(t/12+note*.21+pan));value+=envelope*shimmer*(Math.Sin(2*Math.PI*f*t)+.16*Math.Sin(4*Math.PI*f*t))*.065;}
    }
    for(int note=0;note<8;note++){double age=(t-note*6+Seconds)%Seconds;if(age<5){double envelope=(1-Math.Exp(-age*5))*Math.Exp(-age*.85);double f=Math.Round(melody[note]*Seconds)/Seconds;value+=Math.Sin(2*Math.PI*f*t+Math.Sin(2*Math.PI*t/Seconds)*.4)*envelope*.055*(1+pan*(note%2==0?1:-1));}}
    value+=Math.Sin(2*Math.PI*49*t)*.012;writer.Write((short)Math.Round(Math.Clamp(value,-.85,.85)*32767));
   }}
  }
  AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);var importer=(AudioImporter)AssetImporter.GetAtPath(Path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
  var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Path);var samples=new float[clip.samples*clip.channels];if(!clip.GetData(samples,0))throw new Exception("Music samples not readable");double power=0;float peak=0;foreach(float sample in samples){power+=sample*sample;peak=Mathf.Max(peak,Mathf.Abs(sample));}double rms=Math.Sqrt(power/samples.Length);if(rms<.04||peak>.9||clip.channels!=2)throw new Exception("Music loudness/channel validation failed");
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);if(listeners.Length!=1||!listeners[0].enabled)throw new Exception("Expected one enabled audio listener");Debug.Log($"OCEAN_MUSIC_VALIDATED seconds={clip.length} channels={clip.channels} rms={rms:F4} peak={peak:F4} listener=enabled");
 }
}
}
