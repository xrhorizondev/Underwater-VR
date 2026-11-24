import bpy
bpy.ops.wm.open_mainfile(filepath=r'D:/GIT Project/Underwater-VR/ArtSource/MarineRefresh/Dolphin.blend')
for o in bpy.data.objects:
 if o.type=='ARMATURE':
  print('RIG',o.name)
  for b in o.data.bones:print('BONE',b.name,'parent',b.parent.name if b.parent else '',list(b.head_local),list(b.tail_local))
 if o.type=='MESH':print('MESH',o.name,len(o.data.vertices),list(o.dimensions),'shapes',[k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else [])
