import bpy,json,math,sys,runpy
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR'); out=root/'Assets/UnderwaterArt/ImportedBoxJelly'; renders=root/'Renders/BoxJellyReplacement'; renders.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/BoxJellyReplacement/BoxJelly.blend'))
# The unskinned Icosphere is a hidden source helper, not jelly anatomy.
for o in list(bpy.data.objects):
 if o.type=='MESH' and not any(m.type=='ARMATURE' for m in o.modifiers):
  print('SOURCE_HELPER',o.name,'hidden',o.hide_render); bpy.data.objects.remove(o,do_unlink=True)
for im in bpy.data.images:
 if im.type=='IMAGE':
  if max(im.size)>1024: im.scale(1024,1024)
  im.filepath_raw=str(out/'textures'/('Tentacles.png' if im.name=='Image_0' else 'Bell.png'));im.file_format='PNG';im.save();im.pack()
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print('RIG_TRACKS',[(t.name,t.mute,[(s.name,s.frame_start,s.frame_end) for s in t.strips]) for t in rig.animation_data.nla_tracks])
# Use one NLA clip, without duplicating the currently active imported Action.
rig.animation_data.action=None
for t in rig.animation_data.nla_tracks: t.mute=False
scene=bpy.context.scene;scene.frame_start=0;scene.frame_end=200;scene.render.fps=30;scene.frame_set(0)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/BoxJellyReplacement/BoxJelly.blend'))
sys.argv=['export','--','--output',str(out/'BoxJelly.fbx'),'--max-triangles','4000','--max-bones','80','--max-materials','4','--max-texture','1024','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Review water');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.025,.07,.1,1)
scene.view_settings.view_transform='Standard'
mesh=[o for o in scene.objects if o.type=='MESH']; deps=bpy.context.evaluated_depsgraph_get()
points=[o.matrix_world@v.co for o in mesh for v in o.evaluated_get(deps).data.vertices]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(lo+hi)/2;size=(hi-lo).length
cam_data=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',cam_data);scene.collection.objects.link(cam);scene.camera=cam;cam_data.type='ORTHO';cam_data.ortho_scale=max(hi-lo)*1.2
for label,direction,frame in [('front',(0,-1,0),0),('side',(1,0,0),50),('threequarter',(1,-1,.25),100)]:
 scene.frame_set(frame);cam.location=center+Vector(direction).normalized()*size*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(renders/(label+'.png'));bpy.ops.render.render(write_still=True)
print('BOX_JELLY_CONVERTED bounds',list(lo),list(hi))
