import bpy
from mathutils import Vector
from pathlib import Path
R=Path('D:/GIT Project/Underwater-VR');O=R/'Renders/SquidNew';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidNewInspection.blend'))
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Studio');s.world.color=(.03,.03,.03)
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO';d.ortho_scale=1.15
for name,pos in [('OriginalSide',(3,0,0)),('OriginalQuarter',(2,-1,2))]:
 c.location=pos;c.rotation_euler=(-c.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(O/(name+'.png'));bpy.ops.render.render(write_still=True)
