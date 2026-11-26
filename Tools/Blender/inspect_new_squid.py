import bpy,json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
p='C:/Users/mobin/Downloads/tripo_pbr_model_fa5f9eb1-7c78-40c7-aafb-7952c52852f3_meshopt.glb'
bpy.ops.import_scene.gltf(filepath=p)
for o in bpy.data.objects:
 if o.type=='MESH':
  o.data.calc_loop_triangles();print('MESH',o.name,len(o.data.vertices),len(o.data.loop_triangles),'bounds', [list(v) for v in o.bound_box], 'materials',[m.name for m in o.data.materials])
for i in bpy.data.images:print('IMAGE',i.name,list(i.size))
bpy.ops.wm.save_as_mainfile(filepath='D:/GIT Project/Underwater-VR/ArtSource/SquidNewInspection.blend')
