import bpy,json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=r'C:/Users/mobin/Downloads/tripo_pbr_model_17fc3553-ce0b-4655-81cc-86ef0dcb7559_meshopt.glb')
for o in bpy.data.objects:
 if o.type=='MESH': print('MESH',o.name,len(o.data.vertices),sum(len(p.vertices)-2 for p in o.data.polygons),list(o.dimensions),[m.name for m in o.data.materials])
for im in bpy.data.images: print('IMAGE',im.name,list(im.size))
bpy.ops.wm.save_as_mainfile(filepath=r'D:/GIT Project/Underwater-VR/ArtSource/CombJellyComparison/Imported.blend')
