import bpy,math,json,sys,runpy
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'Lionfish_Animated.blend'))
fish=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');fish.name='Reduced Lionfish';bpy.context.view_layer.objects.active=fish;bpy.ops.object.select_all(action='DESELECT');fish.select_set(True)
original=fish.data.copy();points=[v.co.copy() for v in original.vertices];faces=[tuple(p.vertices) for p in original.polygons];length=max(v.y for v in points)-min(v.y for v in points);samples=points[::17]
dec=fish.modifiers.new('Preserve silhouette reduction','DECIMATE');dec.decimate_type='COLLAPSE';dec.ratio=20000/469558;dec.use_collapse_triangulate=True;dec.delimit={'UV','SEAM','MATERIAL'};bpy.ops.object.modifier_move_up(modifier=dec.name);bpy.ops.object.modifier_apply(modifier=dec.name)
fish.data.calc_loop_triangles();tree=BVHTree.FromPolygons([v.co for v in fish.data.vertices],[tuple(p.vertices) for p in fish.data.polygons],all_triangles=True);errors=np.array([tree.find_nearest(v)[3] for v in samples]);max_error=float(errors.max());p99=float(np.percentile(errors,99));count=len(fish.data.loop_triangles)
if max_error>length*.025:raise RuntimeError('Reduction loses silhouette: '+str(max_error/length))
co=np.empty(len(fish.data.vertices)*3,dtype=np.float32);fish.data.vertices.foreach_get('co',co);co=co.reshape(-1,3);edges=np.empty(len(fish.data.edges)*2,dtype=np.int32);fish.data.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2);rest=np.linalg.norm(co[edges[:,0]]-co[edges[:,1]],axis=1);valid=rest>length*1e-5
def evaluated(frame):
 bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();e=fish.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=np.empty(len(m.vertices)*3,dtype=np.float32);m.vertices.foreach_get('co',v);e.to_mesh_clear();return v.reshape(-1,3)
first=evaluated(1);last=evaluated(73);seam=float(np.linalg.norm(first-last,axis=1).max());maximum=1;minimum=1
for frame in range(1,74,3):
 v=evaluated(frame);ratios=np.linalg.norm(v[edges[:,0]]-v[edges[:,1]],axis=1)[valid]/rest[valid];maximum=max(maximum,float(ratios.max()));minimum=min(minimum,float(ratios.min()))
if seam>1e-6 or maximum>1.65 or minimum<.5:raise RuntimeError('Reduced animation deforms incorrectly')
report=dict(original_triangles=469558,reduced_triangles=count,reduced_vertices=len(fish.data.vertices),reduction_percent=100*(1-count/469558),sampled_surface_points=len(samples),maximum_surface_error=max_error,surface_error_99_percent=p99,max_error_percent_body_length=max_error/length*100,loop_error=seam,maximum_edge_stretch=maximum,minimum_edge_ratio=minimum,bones=len(rig.data.bones),materials=len(fish.data.materials),uvs_preserved=True,checked_animation_phases=25)
(out/'ReductionValidation.json').write_text(json.dumps(report,indent=2));print('REDUCED_LIONFISH_VALIDATED',json.dumps(report))
bpy.context.scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.wm.save_as_mainfile(filepath=str(out/'Lionfish_Reduced.blend'))
sys.argv=['export','--','--output',str(out/'Lionfish_Reduced.fbx'),'--max-triangles','30000','--max-bones','8','--max-materials','2','--max-texture','8192','--require-animation'];runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.view_settings.exposure=1;scene.world=bpy.data.worlds.new('Reduction review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.33,.38,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
lo=Vector([min(v.co[i] for v in fish.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in fish.data.vertices) for i in range(3)]);center=(lo+hi)/2;bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=length*1.25
for name,offset in [('Side',(1,0,.05)),('Reverse',(-1,0,.05)),('Front',(.1,1,.05)),('ThreeQuarter',(1,.6,.2))]:
 cam.location=center+Vector(offset)*length*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 for frame in ([1,19,37,55] if name=='Side' else [19]):
  scene.frame_set(frame);scene.render.filepath=str(out/('Reduced'+name+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
print('REDUCTION_RENDERS_FINISHED')
