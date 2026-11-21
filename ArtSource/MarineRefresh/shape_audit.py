import bpy,math,json,gc
import numpy as np
from pathlib import Path
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh';reports={}
def geometry(kind,original):
 bpy.ops.wm.open_mainfile(filepath=str(base/(kind+('_Imported' if original else '')+'.blend')))
 scene=bpy.context.scene
 if not(original and kind=='Shark'):
  for o in scene.objects:
   if o.animation_data:o.animation_data_clear()
   if o.type=='ARMATURE':
    for p in o.pose.bones:p.matrix_basis=Matrix.Identity(4)
 scene.frame_set(0);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();vertices=[];faces=[]
 rotation=Matrix.Rotation(math.pi/2,4,'Z') if original and kind=='Lionfish' else Matrix.Identity(4)
 for obj in scene.objects:
  if obj.type!='MESH' or obj.name=='Icosphere' or (original and kind=='Dolphin' and obj.name.startswith('Cube')) or (original and kind=='Lionfish' and obj.name=='Object_4'):continue
  evaluated=obj.evaluated_get(dg);mesh=evaluated.to_mesh();offset=len(vertices);matrix=rotation@obj.matrix_world
  vertices.extend(matrix@v.co for v in mesh.vertices);faces.extend(tuple(offset+i for i in p.vertices) for p in mesh.polygons);evaluated.to_mesh_clear()
 coords=np.array([tuple(v) for v in vertices],dtype=np.float64);lo=coords.min(axis=0);hi=coords.max(axis=0);axis=2 if kind=='CombJelly' else 1;length=hi[axis]-lo[axis];coords=(coords-(lo+hi)/2)/length
 return [Vector(p) for p in coords],faces,coords
for kind in ['Dolphin','Shark','CombJelly','Lionfish']:
 original,faces,source_array=geometry(kind,True);reduced,lowfaces,low_array=geometry(kind,False);low_bvh=BVHTree.FromPolygons(reduced,lowfaces,all_triangles=False)
 stride=max(1,len(original)//100000);errors=[]
 for p in original[::stride]:
  hit=low_bvh.find_nearest(p)
  if hit:errors.append(hit[3])
 # The support of the original and optimized surfaces in many directions
 # catches even thin fin, fluke and spine silhouettes.
 directions=[]
 for i in range(258):
  z=1-2*(i+.5)/258;angle=i*2.399963229728653;radius=math.sqrt(max(0,1-z*z));directions.append((math.cos(angle)*radius,math.sin(angle)*radius,z))
 support_errors=[]
 for d in directions:
  direction=np.array(d);support_errors.append(abs(float((source_array@direction).max()-(low_array@direction).max())))
 report={'originalTriangles':sum(len(f)-2 for f in faces),'optimizedTriangles':sum(len(f)-2 for f in lowfaces),'reductionPercent':100*(1-sum(len(f)-2 for f in lowfaces)/sum(len(f)-2 for f in faces)),'sampledOriginalVertices':len(errors),'surfaceErrorPercentOfLength':{'mean':float(np.mean(errors))*100,'p95':float(np.percentile(errors,95))*100,'p99':float(np.percentile(errors,99))*100,'max':float(max(errors))*100},'silhouetteDirections':len(directions),'maxSilhouetteSupportErrorPercentOfLength':float(max(support_errors))*100}
 reports[kind]=report;print('MARINE_SHAPE_AUDIT',kind,json.dumps(report),flush=True);(base/'shape_audit.json').write_text(json.dumps(reports,indent=2));del original,reduced,faces,lowfaces,source_array,low_array,low_bvh;gc.collect()
print('MARINE_SHAPE_AUDIT_COMPLETE')
