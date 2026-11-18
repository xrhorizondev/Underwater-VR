using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater {
// Visual-only suit: no rigidbody, colliders or influence on tracked travel.
public sealed class DiverOutfit:MonoBehaviour {
 public bool showBody=true,showMask;
 Transform head,torso,mask;ScubaGlove left,right;Material suit,trim;Mesh torsoMesh;
 public void Initialize(Transform viewpoint,ScubaGlove leftHand,ScubaGlove rightHand){
  head=viewpoint;left=leftHand;right=rightHand;
  suit=new Material(Shader.Find("Standard")){color=new Color(.018f,.025f,.03f)};suit.SetFloat("_Glossiness",.2f);
  trim=new Material(suit){color=new Color(.035f,.12f,.23f)};
  torso=new GameObject("Diver wetsuit torso").transform;torso.SetParent(transform,false);
  var chest=new GameObject("Tailored wetsuit chest");chest.transform.SetParent(torso,false);torsoMesh=TorsoMesh();chest.AddComponent<MeshFilter>().sharedMesh=torsoMesh;
  var chestRenderer=chest.AddComponent<MeshRenderer>();chestRenderer.sharedMaterial=suit;chestRenderer.shadowCastingMode=ShadowCastingMode.Off;chestRenderer.receiveShadows=false;
  Part("BCD left shoulder strap",PrimitiveType.Cube,torso,new Vector3(-.115f,.07f,.105f),new Vector3(.045f,.32f,.026f),trim);
  Part("BCD right shoulder strap",PrimitiveType.Cube,torso,new Vector3(.115f,.07f,.105f),new Vector3(.045f,.32f,.026f),trim);
  Part("BCD waist strap",PrimitiveType.Cube,torso,new Vector3(0,-.18f,.105f),new Vector3(.29f,.04f,.025f),trim);
  foreach(var hand in new[]{left,right}){
   var sleeve=Part("Short wetsuit cuff",PrimitiveType.Cylinder,hand.Wrist,new Vector3(0,0,-.155f),new Vector3(.066f,.025f,.066f),suit);
   sleeve.localRotation=Quaternion.Euler(90,0,0);
  }
  mask=new GameObject("Optional peripheral dive mask rim").transform;mask.SetParent(head,false);
  // Keep the central binocular view clear; no lens plane or camera/FOV change.
  Part("Mask top",PrimitiveType.Cube,mask,new Vector3(0,.13f,.12f),new Vector3(.4f,.012f,.015f),suit);
  Part("Mask left",PrimitiveType.Cube,mask,new Vector3(-.19f,0,.12f),new Vector3(.012f,.26f,.015f),suit);
  Part("Mask right",PrimitiveType.Cube,mask,new Vector3(.19f,0,.12f),new Vector3(.012f,.26f,.015f),suit);
  Tick(1);
 }
 static Transform Part(string name,PrimitiveType shape,Transform parent,Vector3 position,Vector3 scale,Material material){
  var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
  var collider=go.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
  var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  return go.transform;
 }
 void LateUpdate(){Tick(Time.deltaTime);}
 public void Tick(float dt){if(!head||!torso)return;torso.gameObject.SetActive(showBody);mask.gameObject.SetActive(showMask);
  var yaw=Quaternion.Euler(0,head.eulerAngles.y,0);torso.position=head.position+Vector3.down*.62f-yaw*Vector3.forward*.12f;
  torso.rotation=Quaternion.Slerp(torso.rotation,yaw,1-Mathf.Exp(-dt*5));
 }
 static Mesh TorsoMesh(){
  float[] y={-.36f,-.18f,.08f,.24f,.32f},width={.14f,.15f,.18f,.19f,.065f},depth={.09f,.10f,.11f,.10f,.065f};const int sides=20;
  var vertices=new Vector3[y.Length*sides+2];var triangles=new int[y.Length*sides*6];int t=0;
  for(int ring=0;ring<y.Length;ring++)for(int i=0;i<sides;i++){
   float a=i*Mathf.PI*2/sides;vertices[ring*sides+i]=new Vector3(Mathf.Cos(a)*width[ring],y[ring],Mathf.Sin(a)*depth[ring]);
   if(ring==0)continue;int p=(ring-1)*sides+i,q=(ring-1)*sides+(i+1)%sides,r=ring*sides+i,s=ring*sides+(i+1)%sides;
   triangles[t++]=p;triangles[t++]=r;triangles[t++]=q;triangles[t++]=q;triangles[t++]=r;triangles[t++]=s;
  }
  int bottom=y.Length*sides,top=bottom+1;vertices[bottom]=Vector3.up*y[0];vertices[top]=Vector3.up*y[y.Length-1];
  for(int i=0;i<sides;i++){int next=(i+1)%sides;triangles[t++]=bottom;triangles[t++]=i;triangles[t++]=next;triangles[t++]=top;triangles[t++]=(y.Length-1)*sides+next;triangles[t++]=(y.Length-1)*sides+i;}
  var mesh=new Mesh{name="Low-poly fitted wetsuit torso",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 void OnDestroy(){if(suit)Destroy(suit);if(trim)Destroy(trim);if(torsoMesh)Destroy(torsoMesh);}
}
}
