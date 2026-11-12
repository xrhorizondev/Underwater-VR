using UnityEngine;
using UnityEngine.XR;
namespace Underwater {
public class Diver:MonoBehaviour {
 ScubaGlove leftGlove,rightGlove;SeaLife heldFish;bool gripDown,grabSpent;SeaLife[] lifeCache;ReefAtmosphere atmosphereCache;DiverCollisionMotor motor;public static Diver Instance;public Transform Head{get;private set;} public int Zone{get;private set;}
 public bool currentAssist=true,consequences=true;Transform left,right;Vector3 velocity,previousLeft,previousRight;float idle,flash,captionUntil;string caption="";int menuZone;float nextMenuStep;bool previousBack,previousMenuSelect;bool menu=true,previousMenu,previousTeleport,xrStarted;float yaw,pitch;AudioSource voice;Camera cam;TextMesh headsetCaption;
 void Start(){lifeCache=FindObjectsByType<SeaLife>(FindObjectsSortMode.None);atmosphereCache=FindFirstObjectByType<ReefAtmosphere>();Instance=this;Head=transform.Find("Head");motor=GetComponent<DiverCollisionMotor>();if(!motor)motor=gameObject.AddComponent<DiverCollisionMotor>();motor.Initialize();cam=Head.GetComponent<Camera>();voice=Head.gameObject.AddComponent<AudioSource>();voice.spatialBlend=0;
 leftGlove=ScubaGlove.Create(transform,true);rightGlove=ScubaGlove.Create(transform,false);left=leftGlove.transform;right=rightGlove.transform;
 var text=new GameObject("Diver caution caption");text.transform.SetParent(Head,false);text.transform.localPosition=new(0,-.2f,1);headsetCaption=text.AddComponent<TextMesh>();headsetCaption.anchor=TextAnchor.MiddleCenter;headsetCaption.alignment=TextAlignment.Center;headsetCaption.characterSize=.012f;headsetCaption.fontSize=48;
 CreateAmbience();gameObject.AddComponent<ReefSoundscape>();if(!GetComponent<DiverImmersion>())gameObject.AddComponent<DiverImmersion>();if(!GetComponent<DiverBubbles>())gameObject.AddComponent<DiverBubbles>();if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--smoke")>=0)StartCoroutine(Smoke()); }
 System.Collections.IEnumerator Smoke(){Application.runInBackground=true;menu=false;yield return new WaitForSeconds(2);Debug.Log("SMOKE: habitats=8 creatures="+FindObjectsByType<SeaLife>(FindObjectsSortMode.None).Length+" teleports="+FindObjectsByType<TeleportShell>(FindObjectsSortMode.None).Length);var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../RuntimePreview.png"),tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;Destroy(rt);Destroy(tex);yield return new WaitForSeconds(1);Application.Quit();}
 void Update(){
 var l=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);var r=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);var h=InputDevices.GetDeviceAtXRNode(XRNode.Head);bool xr=h.isValid;
 if(xr){if(!xrStarted){xrStarted=true;menu=false;}if(h.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 hp))Head.localPosition=hp;if(h.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion hr))Head.localRotation=hr;Track(l,left);Track(r,right);}
 else{if(Input.GetMouseButton(1)&&!menu){yaw+=Input.GetAxis("Mouse X")*2;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,-80,80);Head.localRotation=Quaternion.Euler(pitch,yaw,0);}left.position=Head.TransformPoint(new(-.24f,-.22f,.5f));right.position=Head.TransformPoint(new(.24f,-.22f,.5f));}
 AnimateGlove(leftGlove,l,xr);AnimateGlove(rightGlove,r,xr);
 motor.TrackHead();Vector2 stick=Vector2.zero;l.TryGetFeatureValue(CommonUsages.primary2DAxis,out stick);Vector3 input=Head.forward*(stick.y+Input.GetAxisRaw("Vertical"))+Head.right*(stick.x+Input.GetAxisRaw("Horizontal"));
 input+=Vector3.up*((Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0));
 if(xr&&l.TryGetFeatureValue(CommonUsages.gripButton,out bool paddle)&&paddle){Vector3 delta=(left.localPosition-previousLeft)+(right.localPosition-previousRight);input-=transform.TransformDirection(delta)*15;}
 previousLeft=left.localPosition;previousRight=right.localPosition;
 if(menu){input=Vector3.zero;velocity=Vector3.zero;}bool active=input.sqrMagnitude>.005f;idle=active?0:idle+Time.deltaTime;
 Vector3 target=Vector3.ClampMagnitude(input,1)*1.4f;if(currentAssist&&idle>20&&!menu&&(Zone==4||Zone==6))target=Head.forward*.08f;
 velocity=Vector3.MoveTowards(velocity,target,Time.deltaTime*.65f);velocity=motor.Move(velocity*Time.deltaTime)/Mathf.Max(Time.deltaTime,.0001f);
 
 float nearest=float.MaxValue;for(int i=0;i<8;i++){float d=Vector3.Distance(transform.position,OceanWorld.Centers[i]);if(d<nearest){nearest=d;Zone=i;}}
 if(!atmosphereCache){Color water=Zone==4?new Color(.008f,.025f,.045f):Zone==3?new Color(.025f,.08f,.13f):new Color(.035f,.27f,.34f);RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,water,Time.deltaTime);cam.backgroundColor=RenderSettings.fogColor;RenderSettings.fogDensity=Mathf.Lerp(RenderSettings.fogDensity,Zone==4?.085f:Zone==3?.055f:.032f,Time.deltaTime);RenderSettings.ambientLight=Color.Lerp(RenderSettings.ambientLight,Zone==4?new Color(.045f,.07f,.1f):Zone==3?new Color(.1f,.16f,.2f):new Color(.4f,.5f,.53f),Time.deltaTime);
 }
 bool menuPressed=l.TryGetFeatureValue(CommonUsages.menuButton,out bool mb)&&mb;
 if(Input.GetKeyDown(KeyCode.Escape)||menuPressed&&!previousMenu)SetMenu(!menu);previousMenu=menuPressed;
 bool teleportPressed=r.TryGetFeatureValue(CommonUsages.primaryButton,out bool tp)&&tp;
 if(!menu&&(Input.GetKeyDown(KeyCode.T)||teleportPressed&&!previousTeleport))TeleportRay();previousTeleport=teleportPressed;
  if(xr&&menu){r.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 choice);if(Mathf.Abs(choice.y)>.65f&&Time.unscaledTime>nextMenuStep){menuZone=(menuZone+(choice.y<0?1:7))%8;nextMenuStep=Time.unscaledTime+.3f;}if(teleportPressed&&!previousMenuSelect){if(motor.TryTeleport(OceanWorld.Centers[menuZone]))SetMenu(false);else Say("That landing spot is obstructed.",null);}}
 previousMenuSelect=teleportPressed;
 bool back=l.TryGetFeatureValue(CommonUsages.secondaryButton,out bool backDown)&&backDown;if(back&&!previousBack&&menu)SetMenu(false);previousBack=back;
 headsetCaption.text=xr&&menu?"HABITATS\n"+OceanWorld.Names[menuZone]+"\nRight stick: select | A: dive\nY / left menu: back":xr&&Time.time<captionUntil?caption:"";
 bool grab=!menu&&(Input.GetMouseButton(0)||r.TryGetFeatureValue(CommonUsages.gripButton,out bool grip)&&grip);UpdateGrab(grab,right,lifeCache);
 flash=Mathf.MoveTowards(flash,0,Time.deltaTime*3);
 }
 void SetMenu(bool open){menu=open;velocity=Vector3.zero;ReleaseGrab();GetComponent<ReefSoundscape>()?.Cue(false);}
 void AnimateGlove(ScubaGlove glove,InputDevice device,bool xr){device.TryGetFeatureValue(CommonUsages.trigger,out float trigger);device.TryGetFeatureValue(CommonUsages.grip,out float grip);if(!xr&&Input.GetMouseButton(0)){trigger=1;grip=1;}glove.Animate(trigger,grip,Time.deltaTime);}
 public void UpdateGrab(bool pressed, Transform hand, SeaLife[] candidates){
  if(!pressed){ReleaseGrab();return;}
  gripDown=true;
  if(grabSpent)return;
  float nearest=.65f*.65f;
  foreach(var fish in candidates){
   if(!fish||!fish.isActiveAndEnabled||!fish.grabbable||fish.IsHeld)continue;
   float distance=(fish.transform.position-hand.position).sqrMagnitude;
   if(distance>=nearest)continue;
   nearest=distance;
   heldFish=fish;
  }
  if(heldFish){heldFish.Grab(hand);grabSpent=true;}
 }
 void ReleaseGrab(){
  if(!gripDown)return;
  if(heldFish)heldFish.Release();
  heldFish=null;gripDown=false;grabSpent=false;
 }
 void OnDisable(){ReleaseGrab();} void Track(InputDevice d,Transform t){if(d.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p))t.localPosition=p;if(d.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))t.localRotation=q;}
 public float DistanceToHands(Vector3 p)=>Mathf.Min(Vector3.Distance(left.position,p),Vector3.Distance(right.position,p));
 public void Say(string text,AudioClip clip){caption=text;captionUntil=Time.time+3;if(clip)voice.PlayOneShot(clip);}
 public void Hurt(AudioClip clip){if(!consequences)return;flash=.65f;Say("Whoa.",clip);InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0,.25f,.12f);}
 void TeleportRay(){if(Physics.Raycast(Head.position,Head.forward,out RaycastHit hit,12)&&hit.collider.GetComponent<TeleportShell>()){if(motor.TryTeleport(hit.point+Vector3.up*.15f)){velocity=Vector3.zero;GetComponent<ReefSoundscape>()?.Cue(true);}else Say("That landing spot is obstructed.",null);}}
 void CreateAmbience(){
  var clip=Resources.Load<AudioClip>("ReefBreathing");if(!clip){Debug.LogError("Missing baked ReefBreathing audio. Run Underwater/Apply side boundaries and bake startup audio.");return;}var sound=new GameObject("Regulator breathing and water ambience");sound.transform.SetParent(Head,false);var a=sound.AddComponent<AudioSource>();a.clip=clip;a.loop=true;a.volume=.45f;a.spatialBlend=0;var filter=sound.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=1100;a.Play();
 }
 void OnGUI(){if(flash>0){GUI.color=new Color(1,0,0,flash);GUI.DrawTexture(new Rect(0,0,Screen.width,20),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,Screen.height-20,Screen.width,20),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,0,20,Screen.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(Screen.width-20,0,20,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
 if(Time.time<captionUntil)GUI.Box(new Rect(Screen.width/2-230,Screen.height-95,460,42),caption);
 if(!menu)return;GUILayout.BeginArea(new Rect(24,24,430,650),GUI.skin.box);GUILayout.Label("UNDERWATER EXPLORATION");GUILayout.Label(Namesafe());GUILayout.Space(10);GUILayout.Label("WASD swim • Q / E descend / ascend\nHold right mouse to look • Left mouse scoop fish\nT: gaze at a shell to teleport • Esc: menu\nVR: left stick swim • grip to paddle / hold fish\nRight primary button: gaze teleport");if(GUILayout.Button("Back / Continue diving"))SetMenu(false);currentAssist=GUILayout.Toggle(currentAssist,"Gentle current after 20 seconds idle");consequences=GUILayout.Toggle(consequences,"Enable contact consequences");GUILayout.Label("Explore a habitat");for(int i=0;i<8;i++){int zone=i;if(GUILayout.Button(OceanWorld.Names[i])){if(motor.TryTeleport(OceanWorld.Centers[zone])){velocity=Vector3.zero;menu=false;}else Say("That landing spot is obstructed.",null);}}GUILayout.EndArea();}
 string Namesafe()=>OceanWorld.Names[Zone];
}
}
