import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'Renders/ReferenceLionfish'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/UnderwaterArt/ReferenceLionfish/Lionfish.fbx'))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
scene.render.resolution_x=800;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Inspection world');scene.world.color=(.045,.045,.045)
for loc,power in [((.9,1.1,1.2),60),((-.8,.3,.6),30),((0,-1,.8),40)]:
    data=bpy.data.lights.new('Soft light','AREA');data.energy=power;data.size=1
    light=bpy.data.objects.new('Soft light',data);scene.collection.objects.link(light);light.location=loc;light.rotation_euler=(Vector((0,0,.03))-light.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=.55
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');action=next(a for a in bpy.data.actions if a.name.endswith('|Swim'));rig.animation_data.action=action
lo,hi=action.frame_range
for name,pos,fraction in [('FBX_Right',(.6,.8,.45),.125),('FBX_Left',(-.6,.8,.45),.375),('FBX_Front',(0,.9,.14),.125)]:
    scene.frame_set(int(lo+(hi-lo)*fraction));camera.location=pos;camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('FBX_VISUAL_INSPECTION_READY')
scene.render.resolution_x=640;scene.render.resolution_y=560;scene.cycles.samples=8
camera.location=(.6,.8,.45);camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler()
for index in range(16):
    frame=lo+(hi-lo)*index/32;scene.frame_set(int(frame),subframe=frame-int(frame))
    scene.render.filepath=str(OUT/('Preview_%02d.png'%index));bpy.ops.render.render(write_still=True)
