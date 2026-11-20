import bpy,math
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/TripoMoray/TripoMoray.blend'))
rig=bpy.data.objects['TripoMorayRig'];rig.animation_data.action=None
for t in rig.animation_data.nla_tracks:t.mute=True
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=1000;scene.render.resolution_y=750;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('Mouth studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.05,.06,.07,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4
center=Vector((-.177,-.461,.039))
for name,pos in [('Key',(.4,-.5,.8)),('Fill',(-.4,-.5,.3))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=12;d.size=.5;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',d);scene.collection.objects.link(camera);scene.camera=camera;d.type='ORTHO';d.ortho_scale=.14
out=root/'Renders/TripoMoray'
def render(name,pos,angle):
 for b in rig.pose.bones:b.rotation_euler=(0,0,0)
 rig.pose.bones['Jaw'].rotation_euler.x=-math.radians(angle);bpy.context.view_layer.update();camera.location=center+Vector(pos);camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
for label,pos in [('Side',(.5,0,.03)),('Front',(0,-.5,.02)),('Opposite',(-.5,-.1,.03))]:
 for angle in [0,8,22]:render('Mouth_'+label+'_'+str(angle),pos,angle)
scene.render.resolution_x=640;scene.render.resolution_y=480;scene.cycles.samples=8
for i in range(16):render('MouthCycle_'+str(i).zfill(2),(.4,-.3,.08),11-11*math.cos(i*math.tau/16))
