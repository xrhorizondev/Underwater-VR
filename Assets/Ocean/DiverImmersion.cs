using UnityEngine;
using UnityEngine.Rendering;
namespace Underwater {
public class DiverImmersion:MonoBehaviour {
 Transform head;ParticleSystem motes;Material material;float budget,waterClock;Vector3 previous;
 void Start(){Initialize(transform.Find("Head"));}
 public void Initialize(Transform viewpoint){if(motes)return;head=viewpoint;var shader=Resources.Load<Shader>("SuspendedMatter");if(!head||!shader)return;material=new Material(shader);var go=new GameObject("Water | suspended fine particles");go.transform.SetParent(transform,false);motes=go.AddComponent<ParticleSystem>();motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
 var main=motes.main;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=140;main.startSpeed=0;main.startLifetime=14;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;var emission=motes.emission;emission.enabled=false;var shape=motes.shape;shape.enabled=false;
 var noise=motes.noise;noise.enabled=true;noise.strength=.075f;noise.frequency=.32f;noise.scrollSpeed=.12f;noise.quality=ParticleSystemNoiseQuality.Low;noise.octaveCount=1;var flow=motes.velocityOverLifetime;flow.enabled=true;flow.space=ParticleSystemSimulationSpace.World;flow.x=.12f;flow.y=.012f;flow.z=.045f;
 var fade=motes.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(1,.7f),new GradientAlphaKey(0,1)});fade.color=gradient;
 var renderer=motes.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;motes.Play();previous=head.position;for(int i=0;i<70;i++){Tick(.125f);motes.Simulate(.125f,false,false);}motes.Play();
 }
 void Update(){Tick(Mathf.Min(Time.deltaTime,.1f));}
 public void Tick(float dt){if(!motes)return;waterClock+=dt;var flow=motes.velocityOverLifetime;flow.x=.12f+Mathf.Sin(waterClock*.4f)*.045f;flow.y=Mathf.Sin(waterClock*.55f)*.02f;flow.z=.045f+Mathf.Cos(waterClock*.31f)*.035f;if((head.position-previous).sqrMagnitude>36)motes.Clear();previous=head.position;budget+=dt*11;while(budget>=1){budget--;Vector3 offset=Random.insideUnitSphere*4;if(offset.magnitude<.8f)continue;motes.Emit(new ParticleSystem.EmitParams{position=head.position+offset,velocity=Vector3.zero,startSize=Random.Range(.006f,.018f),startLifetime=14,startColor=Color.white},1);}}
 public void PreviewStep(float dt){Tick(dt);motes.Simulate(dt,false,false);}
 void OnDestroy(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
}
}

