import bpy
from pathlib import Path
from mathutils import Vector
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'Lionfish_Animated.blend'));scene=bpy.context.scene;fish=next(o for o in scene.objects if o.type=='MESH');points=[fish.matrix_world@v.co for v in fish.data.vertices];lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)]);center=(lo+hi)/2;span=hi.y-lo.y
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.location=center+Vector((1,.2,.08))*span*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=span*1.2
scene.render.engine='CYCLES';scene.cycles.samples=4;scene.render.resolution_x=720;scene.render.resolution_y=560;scene.render.resolution_percentage=100;scene.view_settings.exposure=1;scene.world=bpy.data.worlds.new('Preview world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.33,.38,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
folder=out/'PreviewFrames';folder.mkdir(exist_ok=True)
for i,frame in enumerate(range(1,73,2)):
 scene.frame_set(frame);scene.render.filepath=str(folder/f'{i:03d}.png');bpy.ops.render.render(write_still=True)
print('ANIMATION_PREVIEW_RENDERED')
