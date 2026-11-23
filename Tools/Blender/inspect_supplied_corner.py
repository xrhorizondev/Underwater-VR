import bpy,json,struct,math
from pathlib import Path
from mathutils import Vector
src=Path(r'C:/Users/mobin/Downloads/tripo_pbr_model_245ce788-dee8-4fc9-80bd-4ca89aa0679f_meshopt.glb')
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');out.mkdir(parents=True,exist_ok=True)
data=src.read_bytes();size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);(out/'glb_structure.json').write_text(json.dumps({k:doc.get(k) for k in ['extensionsUsed','extensionsRequired','nodes','meshes','animations']},indent=2))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(src))
report=[];points=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH':
  o.data.calc_loop_triangles();world=[o.matrix_world@v.co for v in o.data.vertices];points+=world
  report.append(dict(name=o.name,vertices=len(o.data.vertices),polygons=len(o.data.polygons),triangles=len(o.data.loop_triangles),materials=[m.name for m in o.data.materials],bounds=[[min(p[i] for p in world) for i in range(3)],[max(p[i] for p in world) for i in range(3)]]))
(out/'inspection.json').write_text(json.dumps(report,indent=2));print('MODEL_INSPECTION',json.dumps(report))
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)]);center=(lo+hi)/2;span=max(hi-lo)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Inspection world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.17,.19,.22,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam
for name,offset in [('Front',(0,-2.1,.7)),('Back',(0,2.1,.7)),('Top',(0,-.01,2.1))]:
 cam.location=center+Vector(offset)*span;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=span*1.3
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Inspection.blend'))
