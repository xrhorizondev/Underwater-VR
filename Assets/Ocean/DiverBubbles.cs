using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater {
// A small, bounded mesh-particle system. No screen grabs or camera distortion.
public class DiverBubbles:MonoBehaviour {
 Transform head;ParticleSystem bubbles;Material material;Mesh sphere;float budget;Vector3 lastPosition;uint serial;
 void Start(){Initialize(transform.Find("Head"));}
 public void Initialize(Transform viewpoint){
  if(bubbles)return;head=viewpoint;if(!head)return;var shader=Resources.Load<Shader>("DiverBubbles");if(!shader){Debug.LogError("Diver bubble shader missing");enabled=false;return;}
  material=new Material(shader);sphere=MakeBubble();var go=new GameObject("Regulator | rising air bubbles");go.transform.SetParent(transform,false);bubbles=go.AddComponent<ParticleSystem>();bubbles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=bubbles.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=64;main.startSpeed=0;main.startLifetime=4;main.startSize=.025f;main.startRotation3D=true;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
  var emission=bubbles.emission;emission.enabled=false;var shape=bubbles.shape;shape.enabled=false;
  var color=bubbles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.06f),new GradientAlphaKey(.9f,.7f),new GradientAlphaKey(0,1)});color.color=gradient;
  var size=bubbles.sizeOverLifetime;size.enabled=true;size.separateAxes=true;size.x=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.85f,1,1.2f));size.y=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.8f),new Keyframe(.3f,.65f),new Keyframe(.65f,.85f),new Keyframe(1,.7f)));size.z=size.x;
  var noise=bubbles.noise;noise.enabled=true;noise.strength=.045f;noise.frequency=1.5f;noise.scrollSpeed=.35f;noise.quality=ParticleSystemNoiseQuality.Low;noise.octaveCount=1;
  var rotation=bubbles.rotationOverLifetime;rotation.enabled=true;rotation.separateAxes=true;rotation.x=new ParticleSystem.MinMaxCurve(-.45f,.45f);rotation.z=new ParticleSystem.MinMaxCurve(-.6f,.6f);
  var collision=bubbles.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.quality=ParticleSystemCollisionQuality.Low;collision.collidesWith=ReefSphereBody.SolidMask;collision.enableDynamicColliders=false;collision.lifetimeLoss=1;collision.radiusScale=.5f;collision.maxCollisionShapes=32;
  var renderer=bubbles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=sphere;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;bubbles.Play();lastPosition=head.position;
 }
 Mesh MakeBubble(){const int rings=12,segments=20;var vertices=new Vector3[(rings+1)*(segments+1)];var normals=new Vector3[vertices.Length];var triangles=new int[rings*segments*6];int index=0;
  for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){float a=y*Mathf.PI/rings,b=x*Mathf.PI*2/segments;int v=y*(segments+1)+x;normals[v]=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));vertices[v]=normals[v]*.5f;}
  for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=y*(segments+1)+x,b=a+segments+1;triangles[index++]=a;triangles[index++]=a+1;triangles[index++]=b;triangles[index++]=a+1;triangles[index++]=b+1;triangles[index++]=b;}
  var mesh=new Mesh{name="Air bubble 480 triangles"};mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
 }
 void Update(){Tick(Mathf.Min(Time.deltaTime,.1f),Time.time);}
 public void Tick(float dt,float clock){
  if(!head||!bubbles)return;if((head.position-lastPosition).sqrMagnitude>36){bubbles.Clear();budget=0;}lastPosition=head.position;
  float phase=clock%4;float envelope=phase<1.8f?Mathf.Sin(phase/1.8f*Mathf.PI):0;budget+=dt*24*envelope;
  Vector3 forward=Vector3.ProjectOnPlane(head.forward,Vector3.up);if(forward.sqrMagnitude<.01f)forward=transform.forward;forward.Normalize();Vector3 side=Vector3.Cross(Vector3.up,forward);
  while(budget>=1){budget--;serial++;float sign=serial%2==0?1:-1;Vector3 position=head.position+forward*.36f+side*(sign*Random.Range(.10f,.18f))-Vector3.up*.28f;float size=serial%5==0?Random.Range(.035f,.05f):Random.Range(.014f,.032f);
   if(Physics.CheckSphere(position,size,ReefSphereBody.SolidMask,QueryTriggerInteraction.Ignore))continue;
   bubbles.Emit(new ParticleSystem.EmitParams{position=position,velocity=Vector3.up*Random.Range(.28f,.48f)+side*sign*.035f+forward*.04f,startSize=size,startLifetime=Random.Range(3.2f,4.5f),startColor=Color.white,rotation3D=new Vector3(0,Random.Range(0,360),Random.Range(-12,12)),randomSeed=serial},1);
  }
 }
 public int ParticleCount=>bubbles?bubbles.particleCount:0;
 public void PreviewStep(float dt,float time){Tick(dt,time);bubbles.Simulate(dt,false,false);}
 void OnDestroy(){if(Application.isPlaying){if(material)Destroy(material);if(sphere)Destroy(sphere);}else {if(material)DestroyImmediate(material);if(sphere)DestroyImmediate(sphere);}}
}
}

