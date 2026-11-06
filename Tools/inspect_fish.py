import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='C:/XR Horizon/Projects/Under Water/Assets/UnderwaterArt/Scans/PhotographicFish.fbx')
for o in bpy.context.scene.objects:
 print('FISH_OBJECT',o.name,o.type,'scale',list(o.scale),'dimensions',list(o.dimensions),'parent',o.parent)
