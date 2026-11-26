import bpy
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'Renders/SquidFullBody/Animation';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidFullBody/SquidQuest.blend'))
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_cavity=True;s.render.resolution_x=900;s.render.resolution_y=630;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Preview studio');s.world.color=(.025,.035,.05)
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO';d.ortho_scale=5.5;focus=Vector((-.09,.31,.018));c.location=focus+Vector((5,-4,3));c.rotation_euler=(focus-c.location).to_track_quat('-Z','Y').to_euler()
for i,f in enumerate(range(1,73,2)):
 s.frame_set(f);s.render.filepath=str(O/f'{i:03}.png');bpy.ops.render.render(write_still=True)
