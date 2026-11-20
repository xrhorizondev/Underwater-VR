using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Underwater {
    public sealed class ScubaGlove : MonoBehaviour {
        SkinnedMeshRenderer skin;
        Mesh mesh;
        Transform controllerFit;
        public Transform Wrist=>controllerFit;
        Material material;
        Vector3 fitPosition;Vector3[] restVertices,triggerDelta,gripDelta,fittedVertices;int[][] fingerVertices;
        SeaLife contactFish,ownFish;bool fittedMesh;int tipVertex;

        public static ScubaGlove Create(Transform rig, bool left) {
            var glove = new GameObject(left ? "Left glove" : "Right glove").AddComponent<ScubaGlove>();
            glove.transform.SetParent(rig,false);
            var visual = new GameObject("Neoprene hand").transform;
            glove.controllerFit=new GameObject("Controller hand fit").transform;
            glove.controllerFit.SetParent(glove.transform,false);
            visual.SetParent(glove.controllerFit,false);
            visual.localRotation=Quaternion.Euler(0,0,left ? 90 : -90);
            glove.SetControllerFit(Vector3.zero,Vector3.zero,left);
            glove.Load(visual,left);
            return glove;
        }

        public void SetControllerFit(Vector3 position,Vector3 rotation,bool left){
            controllerFit.localPosition=new Vector3(left?-position.x:position.x,position.y,position.z);
            fitPosition=controllerFit.localPosition;
            // OpenXR grip points along the curled knuckle line. The authored
            // open hand extends along mesh +Z, which belongs along grip -Y.
            controllerFit.localRotation=Quaternion.Euler(rotation.x,left?-rotation.y:rotation.y,left?-rotation.z:rotation.z)*Quaternion.Euler(90,0,0);
        }
        static Vector3 Vector(BinaryReader reader) => new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());

        Transform palmSpace;Vector3 palmCenter;
        public Vector3 PalmPosition=>palmSpace?palmSpace.TransformPoint(palmCenter):transform.position;
        Vector3 tipPoint,tipTrigger,tipGrip,tipHold;float triggerPose,gripPose,holdPose,holdBlend,holdClosure=1;
        SeaLife posedFish;
        public Vector3 FingertipPosition=>palmSpace?palmSpace.TransformPoint(fittedMesh?fittedVertices[tipVertex]:tipPoint+tipTrigger*triggerPose+tipGrip*gripPose+tipHold*holdPose):transform.position;
        public float HoldingPoseWeight=>holdPose;
        Transform gripAnchor;
        public Transform GripAnchor=>gripAnchor?gripAnchor:transform;
        void Load(Transform visual,bool left) {
            var data=Resources.Load<TextAsset>(left ? "GloveLeft" : "GloveRight");
            using var reader=new BinaryReader(new MemoryStream(data.bytes));
            int count=reader.ReadInt32();
            var indices=new int[reader.ReadInt32()];
            var vertices=new Vector3[count];
            var normals=new Vector3[count];
            var uv=new Vector2[count];
            var colors=new Color[count];
            var trigger=new Vector3[count];
            var grip=new Vector3[count];
            for(int i=0;i<count;i++){
                vertices[i]=Vector(reader);normals[i]=Vector(reader);
                uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());
                colors[i]=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                trigger[i]=Vector(reader);grip[i]=Vector(reader);
            }
            for(int i=0;i<indices.Length;i++) indices[i]=reader.ReadInt32();
            restVertices=vertices;triggerDelta=trigger;gripDelta=grip;fittedVertices=new Vector3[count];
            var labels=Resources.Load<TextAsset>(left?"GloveLeftContact":"GloveRightContact").bytes;
            var groups=new System.Collections.Generic.List<int>[5];for(int i=0;i<5;i++)groups[i]=new System.Collections.Generic.List<int>();
            for(int i=0;i<System.BitConverter.ToInt32(labels,0);i++)if(labels[i+4]>0)groups[labels[i+4]-1].Add(i);
            fingerVertices=new int[5][];for(int i=0;i<5;i++)fingerVertices[i]=groups[i].ToArray();
            mesh=new Mesh {name="Fitted dive glove",vertices=vertices,normals=normals,uv=uv,colors=colors,triangles=indices};
            mesh.RecalculateBounds();palmSpace=visual;palmCenter=mesh.bounds.center;
            int tip=0;for(int i=1;i<vertices.Length;i++)if(vertices[i].z>vertices[tip].z)tip=i;tipVertex=tip;tipPoint=vertices[tip];tipTrigger=trigger[tip];tipGrip=grip[tip];
            gripAnchor=new GameObject("Palm fish grip").transform;gripAnchor.SetParent(visual,false);
            var palmSamples=new System.Collections.Generic.List<float>();for(int i=0;i<vertices.Length;i++)if(Mathf.Abs(vertices[i].x-palmCenter.x)<.035f&&vertices[i].z>-.045f&&vertices[i].z<.02f&&normals[i].y<-.4f)palmSamples.Add(vertices[i].y);
            palmSamples.Sort();float palmY=palmSamples.Count>0?palmSamples[palmSamples.Count/3]:palmCenter.y-.025f;
            gripAnchor.localPosition=new Vector3(palmCenter.x,palmY,-.015f);
            gripAnchor.localRotation=Quaternion.LookRotation(Vector3.forward,Vector3.down);
            mesh.AddBlendShapeFrame("Index trigger",100,trigger,null,null);
            mesh.AddBlendShapeFrame("Grip",100,grip,null,null);
            // A soft opposing-thumb hold, rather than a fully clenched fist.
            var hold=new Vector3[count];for(int i=0;i<count;i++)hold[i]=trigger[i]*.65f+grip[i]*.68f;
            tipHold=hold[tip];mesh.AddBlendShapeFrame("Fish hold",100,hold,null,null);
            skin=visual.gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh=mesh;skin.localBounds=new Bounds(Vector3.zero,Vector3.one*.45f);
            material=new Material(Resources.Load<Shader>("NeopreneGlove"));
            skin.sharedMaterial=material;skin.shadowCastingMode=ShadowCastingMode.Off;
        }

        public void Animate(float trigger,float grip,float dt,SeaLife heldFish=null) {
            ownFish=heldFish&&heldFish.IsHeld?heldFish:null;
            bool holding=heldFish&&heldFish.IsHeld;
            if(holding&&posedFish!=heldFish){
                posedFish=heldFish;var touch=heldFish.GetComponent<FishTouchBody>();
                var size=touch?Vector3.Scale(touch.bodyBounds.size,heldFish.transform.lossyScale):Vector3.one*.08f;
                float thickness=Mathf.Min(Mathf.Abs(size.x),Mathf.Abs(size.y));
                holdClosure=Mathf.Lerp(1,.45f,Mathf.InverseLerp(.06f,.28f,thickness));
            }
            if(!holding)posedFish=null;
            holdBlend=Mathf.MoveTowards(holdBlend,holding?1:0,Mathf.Max(0,dt)*8);
            SetPose(0,trigger*(1-holdBlend),dt);SetPose(1,grip*(1-holdBlend),dt);SetPose(2,holdBlend*holdClosure,dt);
        }
        void SetPose(int index,float value,float dt) {
            float previous=(index==0?triggerPose:index==1?gripPose:holdPose)*100;
            float next=Mathf.MoveTowards(previous,Mathf.Clamp01(value)*100,dt*700);
            if(index==0)triggerPose=next*.01f;else if(index==1)gripPose=next*.01f;else holdPose=next*.01f;
            skin.SetBlendShapeWeight(index,next);
        }
        public void SetContactFish(SeaLife fish){contactFish=fish;}
        void LateUpdate(){ResolveContact();}
        public void ResolveContact(){
            controllerFit.localPosition=fitPosition;
            var fish=ownFish&&ownFish.IsHeld?ownFish:contactFish&&contactFish.IsHeld?contactFish:null;
            var touch=fish?fish.GetComponent<FishTouchBody>():null;
            if(touch&&fish!=ownFish){float reach=Vector3.Scale(touch.GripRadii,touch.transform.lossyScale).magnitude+.3f;if((GripAnchor.position-touch.transform.TransformPoint(touch.GripCenter)).sqrMagnitude>reach*reach)touch=null;}
            if(!touch){if(fittedMesh){mesh.vertices=restVertices;mesh.RecalculateNormals();mesh.RecalculateBounds();skin.localBounds=new Bounds(Vector3.zero,Vector3.one*.45f);fittedMesh=false;}skin.SetBlendShapeWeight(0,triggerPose*100);skin.SetBlendShapeWeight(1,gripPose*100);skin.SetBlendShapeWeight(2,holdPose*100);return;}
            bool holding=fish==ownFish;
            // The free hand stops visually at the held fish; its tracked root
            // and the diver's locomotion/collision rig remain untouched.
            if(!holding&&touch.InsideGrip(GripAnchor.position,.025f)){
                var safe=touch.ProjectGripSurface(GripAnchor.position,.03f);controllerFit.position+=safe-GripAnchor.position;
            }
            var toFish=touch.transform.worldToLocalMatrix*palmSpace.localToWorldMatrix;var toHand=toFish.inverse;
            var center=touch.GripCenter;var scale=touch.transform.lossyScale;var r=touch.GripRadii+new Vector3(.003f/Mathf.Abs(scale.x),.003f/Mathf.Abs(scale.y),.003f/Mathf.Abs(scale.z));
            System.Array.Copy(restVertices,fittedVertices,restVertices.Length);
            for(int finger=0;finger<5;finger++){
                // Contact fitting must follow the same size-adaptive soft hold
                // as the visible blendshape, rather than replacing it with a fist.
                var delta=finger==0?triggerDelta:gripDelta;
                float target=Mathf.Clamp01((finger==0?triggerPose:gripPose)+holdPose*(finger==0?.65f:.68f));
                float curl=0;
                // Stop each authored finger chain at first contact, separately.
                for(int step=1;step<=14;step++){
                    float candidate=target*step/14;bool hit=false;
                    foreach(int vertex in fingerVertices[finger]){var p=toFish.MultiplyPoint3x4(restVertices[vertex]+delta[vertex]*candidate)-center;if(InEllipsoid(p,r)){hit=true;break;}}
                    if(hit){float low=curl,high=candidate;for(int refine=0;refine<5;refine++){float middle=(low+high)*.5f;bool blocked=false;foreach(int vertex in fingerVertices[finger]){if(InEllipsoid(toFish.MultiplyPoint3x4(restVertices[vertex]+delta[vertex]*middle)-center,r)){blocked=true;break;}}if(blocked)high=middle;else low=middle;}curl=low;break;}curl=candidate;
                }
                foreach(int vertex in fingerVertices[finger]){
                    var v=restVertices[vertex]+delta[vertex]*curl;var p=toFish.MultiplyPoint3x4(v)-center;
                    // A final surface constraint also protects contact during
                    // fast wrist turns while the fish is catching up.
                    if(InEllipsoid(p,r)){var u=new Vector3(p.x/r.x,p.y/r.y,p.z/r.z);if(u.sqrMagnitude<.000001f)u=Vector3.up;v=toHand.MultiplyPoint3x4(center+Vector3.Scale(u.normalized,r));}
                    fittedVertices[vertex]=v;
                }
            }
            // Protect palm/wrist vertices too if the other hand is driven deep
            // through the fish; no dynamic collider or force reaches the rig.
            for(int i=0;i<fittedVertices.Length;i++){var p=toFish.MultiplyPoint3x4(fittedVertices[i])-center;if(InEllipsoid(p,r)){var u=new Vector3(p.x/r.x,p.y/r.y,p.z/r.z);if(u.sqrMagnitude<.000001f)u=Vector3.up;fittedVertices[i]=toHand.MultiplyPoint3x4(center+Vector3.Scale(u.normalized,r));}}
            mesh.vertices=fittedVertices;mesh.RecalculateNormals();mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(.025f);skin.localBounds=bounds;skin.SetBlendShapeWeight(0,0);skin.SetBlendShapeWeight(1,0);skin.SetBlendShapeWeight(2,0);fittedMesh=true;
        }
        static bool InEllipsoid(Vector3 p,Vector3 r)=>p.x*p.x/(r.x*r.x)+p.y*p.y/(r.y*r.y)+p.z*p.z/(r.z*r.z)<1;
        public int PenetratingVertices(FishTouchBody body){int count=0;var baked=new Mesh();skin.BakeMesh(baked);foreach(var p in baked.vertices)if(body.InsideGrip(palmSpace.TransformPoint(p),-.0005f))count++;if(Application.isPlaying)Destroy(baked);else DestroyImmediate(baked);return count;}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
