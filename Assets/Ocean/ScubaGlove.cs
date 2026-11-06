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
            if(previous!=next)skin.SetBlendShapeWeight(index,next);
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
