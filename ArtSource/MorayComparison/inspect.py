import bpy,numpy as np
from pathlib import Path
from mathutils import Vector
out=Path('D:/GIT Project/Underwater-VR/ArtSource/MorayComparison')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=r'C:/Users/mobin/Downloads/tripo_pbr_model_276577f8-af31-4895-b835-2c508e7883a8_meshopt.glb')
for o in bpy.data.objects:
 if o.type=='MESH':
  bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);v=np.array([v.co[:] for v in o.data.vertices]);print('MESH',o.name,len(v),len(o.data.polygons),'BOUNDS',v.min(0),v.max(0))
for i in bpy.data.images:print('IMAGE',i.name,list(i.size))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Imported.blend'))
s=bpy.context.scene;s.world=bpy.data.worlds.new('Preview');s.world.color=(.1,.1,.1);s.render.engine='CYCLES';s.cycles.samples=8;s.render.resolution_x=1500;s.render.resolution_y=1100;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(2,0,0));c=bpy.context.object;c.rotation_euler=(Vector((0,0,0))-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=1.16;s.camera=c
bpy.ops.object.light_add(type='AREA',location=(1,-.3,1));bpy.context.object.data.energy=70;bpy.context.object.data.size=3
s.render.filepath=str(out/'Source.png');bpy.ops.render.render(write_still=True)
