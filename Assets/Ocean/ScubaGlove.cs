using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Underwater {
    public sealed class ScubaGlove : MonoBehaviour {
        SkinnedMeshRenderer skin;
        Mesh mesh;
        Material material;

        public static ScubaGlove Create(Transform rig, bool left) {
            var glove = new GameObject(left ? "Left glove" : "Right glove").AddComponent<ScubaGlove>();
            glove.transform.SetParent(rig,false);
            var visual = new GameObject("Neoprene hand").transform;
            visual.SetParent(glove.transform,false);
            visual.localRotation=Quaternion.Euler(0,0,left ? 90 : -90);
            glove.Load(visual,left);
            return glove;
        }

        static Vector3 Vector(BinaryReader reader) => new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());

        Transform palmSpace;Vector3 palmCenter;
        public Vector3 PalmPosition=>palmSpace?palmSpace.TransformPoint(palmCenter):transform.position;
        Vector3 tipPoint,tipTrigger,tipGrip;float triggerPose,gripPose;
        public Vector3 FingertipPosition=>palmSpace?palmSpace.TransformPoint(tipPoint+tipTrigger*triggerPose+tipGrip*gripPose):transform.position;
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
            mesh=new Mesh {name="Fitted dive glove",vertices=vertices,normals=normals,uv=uv,colors=colors,triangles=indices};
            mesh.RecalculateBounds();palmSpace=visual;palmCenter=mesh.bounds.center;
            int tip=0;for(int i=1;i<vertices.Length;i++)if(vertices[i].z>vertices[tip].z)tip=i;tipPoint=vertices[tip];tipTrigger=trigger[tip];tipGrip=grip[tip];
            gripAnchor=new GameObject("Palm fish grip").transform;gripAnchor.SetParent(visual,false);
            var palmSamples=new System.Collections.Generic.List<float>();for(int i=0;i<vertices.Length;i++)if(Mathf.Abs(vertices[i].x-palmCenter.x)<.035f&&vertices[i].z>-.045f&&vertices[i].z<.02f&&normals[i].y<-.4f)palmSamples.Add(vertices[i].y);
            palmSamples.Sort();float palmY=palmSamples.Count>0?palmSamples[palmSamples.Count/3]:palmCenter.y-.025f;
            gripAnchor.localPosition=new Vector3(palmCenter.x,palmY,-.015f);
            gripAnchor.localRotation=Quaternion.LookRotation(Vector3.forward,Vector3.down);
            mesh.AddBlendShapeFrame("Index trigger",100,trigger,null,null);
            mesh.AddBlendShapeFrame("Grip",100,grip,null,null);
            skin=visual.gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh=mesh;skin.localBounds=new Bounds(Vector3.zero,Vector3.one*.45f);
            material=new Material(Resources.Load<Shader>("NeopreneGlove"));
            skin.sharedMaterial=material;skin.shadowCastingMode=ShadowCastingMode.Off;
        }

        public void Animate(float trigger,float grip,float dt) {
            SetPose(0,trigger,dt);SetPose(1,grip,dt);
        }
        void SetPose(int index,float value,float dt) {
            float previous=skin.GetBlendShapeWeight(index);
            float next=Mathf.MoveTowards(previous,Mathf.Clamp01(value)*100,dt*700);
            if(index==0)triggerPose=next*.01f;else gripPose=next*.01f;
            if(previous!=next)skin.SetBlendShapeWeight(index,next);
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
