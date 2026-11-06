import bpy,math
from pathlib import Path
from mathutils import Matrix,Vector
r=Path('C:/XR Horizon/Projects/Under Water')
bpy.ops.wm.open_mainfile(filepath=str(r/'Tools/TurtleSource.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and 'Continuous' in o.name)
for mat in body.data.materials:
 bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 for channel,name in [('Base Color','Loggerhead_albedo'),('Normal','Loggerhead_normal')]:
  socket=bs.inputs[channel]
  if not socket.is_linked:continue
  node=socket.links[0].from_node
  if node.type=='NORMAL_MAP':node=node.inputs['Color'].links[0].from_node
  if node.type=='TEX_IMAGE':
   im=node.image;im.filepath_raw=str(r/'Assets/UnderwaterArt/Textures'/(name+'.png'));im.file_format='PNG';im.save()
rot=Matrix.Rotation(-math.pi/2,4,'Z')
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in objects:
 world=o.matrix_world.copy();o.parent=None
 for v in o.data.vertices:v.co=rot@(world@v.co)-Vector((0,0,.12))
 o.matrix_world=Matrix.Identity(4)
 o.vertex_groups.clear()
 for mod in list(o.modifiers):o.modifiers.remove(mod)
 o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(r/'Assets/UnderwaterArt/Scans/LoggerheadTurtle.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
