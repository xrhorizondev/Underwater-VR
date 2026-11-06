using System.Collections.Generic;
using UnityEngine;

namespace Underwater.Editor {
    // Vertex clustering preserves the colony envelope at small projected sizes.
    internal static class CoralDistanceMesh {
        public static Mesh Create(Mesh source) {
            Vector3[] positions = source.vertices;
            Vector2[] uvs = source.uv;
            Color[] colors = source.colors;
            var bins = new Dictionary<Vector3Int,int>();
            var points = new List<Vector3>();
            var texcoords = new List<Vector2>();
            var tints = new List<Color>();
            var counts = new List<int>();
            var remap = new int[positions.Length];
            float cell = Mathf.Max(.001f,source.bounds.size.magnitude / 24f);
            for(int i=0;i<positions.Length;i++){
                var key=Vector3Int.FloorToInt((positions[i]-source.bounds.min)/cell);
                if(!bins.TryGetValue(key,out int id)){
                    id=points.Count;bins.Add(key,id);
                    points.Add(Vector3.zero);counts.Add(0);
                    texcoords.Add(uvs.Length==positions.Length?uvs[i]:Vector2.zero);
                    tints.Add(colors.Length==positions.Length?colors[i]:Color.white);
                }
                remap[i]=id;points[id]+=positions[i];counts[id]++;
            }
            for(int i=0;i<points.Count;i++) points[i]/=counts[i];
            var faces=new List<int>();
            var triangles=source.triangles;
            for(int k=0;k<triangles.Length;k+=3){
                int a=remap[triangles[k]],b=remap[triangles[k+1]],c=remap[triangles[k+2]];
                if(a==b||b==c||a==c)continue;
                faces.Add(a);faces.Add(b);faces.Add(c);
            }
            if(faces.Count==0||faces.Count>=triangles.Length)return source;
            var result=new Mesh{name=source.name+" distance"};
            result.SetVertices(points);result.SetUVs(0,texcoords);
            result.SetColors(tints);result.SetTriangles(faces,0);
            result.RecalculateNormals();result.RecalculateTangents();
            result.bounds=source.bounds;
            return result;
        }
    }
}
