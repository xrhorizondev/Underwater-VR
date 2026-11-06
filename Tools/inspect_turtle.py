import bpy
from pathlib import Path
root=Path('C:/XR Horizon/Projects/Under Water')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'Assets/UnderwaterArt/Scans/LoggerheadTurtle.glb'))
for o in bpy.context.scene.objects:
 if o.type=='MESH': print('PART',o.name,'verts',len(o.data.vertices),'bounds',list(o.dimensions),'location',list(o.location),'mats',[m.name for m in o.data.materials])
print('IMAGES',[(im.name,list(im.size)) for im in bpy.data.images])
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Tools/TurtleSource.blend'))
