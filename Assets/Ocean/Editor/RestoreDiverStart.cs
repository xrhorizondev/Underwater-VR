using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Underwater.Editor {
public static class RestoreDiverStart {
 public static void Run(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/Underwater_Realistic.unity");var diver=UnityEngine.Object.FindFirstObjectByType<Diver>();var head=diver.transform.Find("Head");Debug.Log("DIVER_BEFORE root="+diver.transform.position+" headLocal="+head.localPosition);head.localPosition=new Vector3(0,1.6f,0);head.localRotation=Quaternion.identity;diver.transform.rotation=Quaternion.identity;var camera=head.GetComponent<Camera>();camera.fieldOfView=64;camera.ResetAspect();var motor=diver.GetComponent<DiverCollisionMotor>();motor.Initialize();if(!motor.TryTeleport(new Vector3(0,-1.3f,-9)))throw new Exception("Start position blocked");Physics.SyncTransforms();if(Vector3.Distance(diver.transform.position,new Vector3(0,-1.3f,-9))>.01f||Vector3.Distance(head.localPosition,new Vector3(0,1.6f,0))>.001f||head.position.y>6.5f)throw new Exception("Invalid start pose");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("DIVER_START_RESTORED root="+diver.transform.position+" head="+head.position+" habitat=SunlitReef");}
}}
