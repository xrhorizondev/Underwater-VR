using System;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace Underwater.Editor {
public static class FishMotionValidation {
 public static void Run(){
  var schools=UnityEngine.Object.FindObjectsByType<FishSchool>(FindObjectsSortMode.None);
  if(schools.Length!=9)throw new Exception("Expected nine shoals, found "+schools.Length);
  foreach(var s in schools)s.Initialize();
  float maxPitch=0,maxStep=0,maxTurn=0;int count=0;
  foreach(var school in schools){
   count+=school.fish.Length;
   for(int frame=0;frame<600;frame++){
    var positions=new Vector3[school.fish.Length];var forward=new Vector3[positions.Length];
    for(int i=0;i<positions.Length;i++){positions[i]=school.fish[i].transform.position;forward[i]=school.fish[i].transform.forward;}
    school.Simulate(1f/30);
    for(int i=0;i<positions.Length;i++){
     var f=school.fish[i];var body=f.GetComponent<ReefSphereBody>();if(!body||!body.Clear(f.transform.position))throw new Exception("Fish overlaps reef: "+f.name);float step=Vector3.Distance(positions[i],f.transform.position);float pitch=Mathf.Abs(Mathf.Asin(f.transform.forward.y)*Mathf.Rad2Deg);float turn=Vector3.Angle(forward[i],f.transform.forward)*30;
     if(float.IsNaN(step)||step>.065f||pitch>11||turn>56)throw new Exception("Unstable fish motion: step="+step+", pitch="+pitch+", turn="+turn);
     maxStep=Mathf.Max(maxStep,step);maxPitch=Mathf.Max(maxPitch,pitch);maxTurn=Mathf.Max(maxTurn,turn);
    }
   }
  }
  Debug.Log($"FISH_MOTION_VALIDATED fish={count} shoals={schools.Length} simulatedSeconds=20 maxStep={maxStep:F4} maxPitch={maxPitch:F2} maxTurnDegreesPerSecond={maxTurn:F2}");
  // Simulation is a check, never the saved starting state.
  EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");
 }
}
}

