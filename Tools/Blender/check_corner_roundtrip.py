import bpy,json
import numpy as np
from pathlib import Path
from mathutils.kdtree import KDTree
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'Lionfish_Animated.blend'))
fish=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');indices=np.arange(0,len(fish.data.vertices),29);rest=np.array([fish.matrix_world@v.co for v in fish.data.vertices])[indices]
def points(obj,frame):
 bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();e=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=np.empty(len(m.vertices)*3,dtype=np.float32);m.vertices.foreach_get('co',v);e.to_mesh_clear();return v.reshape(-1,3)@np.array(obj.matrix_world)[:3,:3].T+np.array(obj.matrix_world)[:3,3]
reference={f:points(fish,f)[indices] for f in [1,19,37,55,73]}
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(out/'Lionfish_Animated.fbx'));fish=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');tree=KDTree(len(fish.data.vertices))
for i,v in enumerate(fish.data.vertices):tree.insert(fish.matrix_world@v.co,i)
tree.balance();mapping=np.array([tree.find(p)[1] for p in rest]);max_error=0
offset=int(round(rig.animation_data.action.frame_range[0]))-1
for f in reference:max_error=max(max_error,float(np.linalg.norm(points(fish,f+offset)[mapping]-reference[f],axis=1).max()))
fish.data.calc_loop_triangles();report=dict(triangles=len(fish.data.loop_triangles),bones=len(rig.data.bones),actions=[a.name for a in bpy.data.actions],sampled_vertices=len(indices),sampled_frames=5,max_export_position_error=max_error)
assert report['triangles']==469558 and report['bones']==6 and bpy.data.actions and max_error<.002
(out/'FBXRoundTrip.json').write_text(json.dumps(report,indent=2));print('FBX_ROUNDTRIP_PASSED',json.dumps(report))
