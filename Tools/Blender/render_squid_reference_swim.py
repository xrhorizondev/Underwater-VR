import bpy
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'Renders/SquidReferenceSwim/Preview';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidReferenceSwim/ReferenceSquid.blend'));s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_cavity=True;s.render.resolution_x=960;s.render.resolution_y=660;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Preview studio');s.world.color=(.025,.04,.055)
for mat in bpy.data.objects['ReferenceSquidVisual'].data.materials:
 if mat.use_nodes:
  nodes=[n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and (('Skin' in n.image.name and 'fin' not in mat.name.lower()) or ('Fin' in n.image.name and 'fin' in mat.name.lower()))]
  if nodes:mat.node_tree.nodes.active=nodes[-1]
c=s.camera;c.data.type='ORTHO';c.data.ortho_scale=10;focus=Vector((0,-.25,0));c.location=focus+Vector((9,-7,6));c.rotation_euler=(focus-c.location).to_track_quat('-Z','Y').to_euler()
for i,f in enumerate(range(1,97,2)):
 s.frame_set(f);s.render.filepath=str(O/f'{i:03}.png');bpy.ops.render.render(write_still=True)
