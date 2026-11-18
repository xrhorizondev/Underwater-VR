import bpy,math
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');out=root/'Renders/YellowMoray'
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/YellowMoray/YellowMoray.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['MorayRig'];camera=scene.camera
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
camera.location=(0,-3.5,.18);camera.rotation_euler=(Vector((0,-.93,.025))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=.75
scene.render.resolution_x=800;scene.render.resolution_y=650;scene.cycles.samples=12;scene.render.filepath=str(out/'Front.png');bpy.ops.render.render(write_still=True)
camera.location=(3,-2,1.7);camera.rotation_euler=(Vector((0,.25,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=3.7
scene.render.resolution_x=640;scene.render.resolution_y=396;scene.cycles.samples=4
for label in ['Idle','Swim']:
 rig.animation_data.action=bpy.data.actions[label]
 for index in range(24):
  scene.frame_set(1+index*(4 if label=='Idle' else 2));scene.render.filepath=str(out/f'{label}Review_{index:02d}.png');bpy.ops.render.render(write_still=True)
