import bpy,json
import numpy as np
from pathlib import Path
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel')
bpy.ops.wm.open_mainfile(filepath=str(out/'Inspection.blend'))
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');mesh=obj.data
co=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',co);co=co.reshape(-1,3)
# Source glTF coordinates become Blender X-thickness, Y-horizontal, Z-height.
world=co@np.array(obj.matrix_world)[:3,:3].T+np.array(obj.matrix_world)[:3,3]
selected=(world[:,1]>0)&(world[:,2]<-.035)
polys=np.empty(len(mesh.polygons)*3,dtype=np.int32);mesh.polygons.foreach_get('vertices',polys);polys=polys.reshape(-1,3)
mask=selected[polys].all(axis=1);cross=selected[polys].any(axis=1)&~mask
if cross.any():raise RuntimeError('Extraction boundary intersects polygons; revise selection')
bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
mesh.polygons.foreach_set('select',np.zeros(len(mesh.polygons),dtype=bool));mesh.edges.foreach_set('select',np.zeros(len(mesh.edges),dtype=bool));mesh.vertices.foreach_set('select',selected);bpy.context.scene.tool_settings.mesh_select_mode=(True,False,False);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.separate(type='SELECTED');bpy.ops.object.mode_set(mode='OBJECT')
new=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o!=obj);bpy.data.objects.remove(obj,do_unlink=True);new.name='Selected right corner lionfish';new.data.name='Original untouched lionfish geometry'
for o in list(bpy.context.scene.objects):
 if o.type not in {'MESH'}:bpy.data.objects.remove(o,do_unlink=True)
bpy.context.view_layer.objects.active=new;bpy.ops.object.select_all(action='DESELECT');new.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
new.data.calc_loop_triangles();pts=[new.matrix_world@v.co for v in new.data.vertices];report=dict(vertices=len(new.data.vertices),polygons=len(new.data.polygons),triangles=len(new.data.loop_triangles),bounds=[[min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]],source_triangles=1891380,crossing_faces=int(cross.sum()))
(out/'selected_count.json').write_text(json.dumps(report,indent=2));print('SELECTED_MODEL',json.dumps(report));bpy.ops.wm.save_as_mainfile(filepath=str(out/'SelectedOriginal.blend'))
