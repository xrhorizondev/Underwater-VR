using UnityEngine;

public sealed class LionfishComparisonViews : MonoBehaviour {
 public Transform[] fish;
 Vector3[] positions,pivots;Quaternion[] rotations;
 void Awake(){positions=new Vector3[fish.Length];pivots=new Vector3[fish.Length];rotations=new Quaternion[fish.Length];for(int i=0;i<fish.Length;i++){positions[i]=fish[i].position;rotations[i]=fish[i].rotation;pivots[i]=fish[i].GetComponentInChildren<SkinnedMeshRenderer>().bounds.center;}}
 public void View(float angle){for(int i=0;i<fish.Length;i++){fish[i].SetPositionAndRotation(positions[i],rotations[i]);fish[i].RotateAround(pivots[i],Vector3.up,angle);}}
 void Update(){if(Input.GetKeyDown(KeyCode.F1))View(0);if(Input.GetKeyDown(KeyCode.F2))View(90);if(Input.GetKeyDown(KeyCode.F3))View(45);}
 void OnGUI(){float x=Screen.width*.5f-165;if(GUI.Button(new Rect(x,14,105,30),"Side (F1)"))View(0);if(GUI.Button(new Rect(x+112,14,105,30),"Front (F2)"))View(90);if(GUI.Button(new Rect(x+224,14,105,30),"3/4 (F3)"))View(45);}
}
