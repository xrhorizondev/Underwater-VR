import bpy,math
from pathlib import Path
from mathutils import Matrix
r=Path('C:/XR Horizon/Projects/Under Water');bpy.ops.wm.open_mainfile(filepath=str(r/'Tools/MantaSource.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'Continuous' in o.name)
for mat in body.data.materials:
 bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 for channel,name in [('Base Color','MantaSource_albedo'),('Normal','MantaSource_normal')]:
  socket=bs.inputs[channel]
  if not socket.is_linked:continue
  node=socket.links[0].from_node
  if node.type=='NORMAL_MAP':node=node.inputs['Color'].links[0].from_node
  if node.type=='TEX_IMAGE':
   im=node.image;im.filepath_raw=str(r/'Assets/UnderwaterArt/Textures'/(name+'.png'));im.file_format='PNG';im.save();print('TEXTURE',name)
rot=Matrix.Rotation(-math.pi/2,4,'Z');bpy.ops.object.select_all(action='DESELECT')
for o in [o for o in bpy.context.scene.objects if o.type=='MESH']:
 world=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=rot@(world@v.co)
 o.matrix_world=Matrix.Identity(4);bpy.context.view_layer.objects.active=o
 mod=o.modifiers.new('Quest mesh budget','DECIMATE');mod.ratio=.28 if o==body else .18;bpy.ops.object.modifier_apply(modifier=mod.name)
 for p in o.data.polygons:p.use_smooth=True
 o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(r/'Assets/UnderwaterArt/Scans/GiantManta.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
print('TRIANGLES',sum(len(o.data.polygons) for o in bpy.context.selected_objects))
