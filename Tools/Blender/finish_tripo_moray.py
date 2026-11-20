import bpy,bmesh,math,json,sys,runpy
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'ArtSource/TripoMoray';RENDERS=ROOT/'Renders/TripoMoray'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'TripoMoray.blend'))
rig=bpy.data.objects['TripoMorayRig'];skin=bpy.data.objects['Supplied moray skin'];mat=skin.data.materials[0]
# Correct an earlier draft's jaw sign without changing any body motion.
for action in bpy.data.actions:
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     if 'bones["Jaw"]' in curve.data_path and curve.array_index==0 and max(k.co.y for k in curve.keyframe_points)>.00001:
      for k in curve.keyframe_points:k.co.y=-k.co.y;k.handle_left.y=-k.handle_left.y;k.handle_right.y=-k.handle_right.y
      curve.update()
if not bpy.data.objects.get('DorsalUp'):
 up=bpy.data.objects.new('DorsalUp',None);bpy.data.collections['TripoMorayAsset'].objects.link(up);up.parent=rig;up.location=rig.data.bones['Root'].head_local+Vector((0,0,.2))
mouthZ=rig.data.bones['Jaw'].head_local.z;hinge=rig.data.bones['Jaw'].head_local.copy()
# A recessed throat closes the view through the mouth. Broad triangle fans
# can poke through a curved cheek, so keep the cavity entirely inside it.
attr=skin.data.color_attributes.get('Color')
if attr:
 remove=[p.index for p in skin.data.polygons if all(sum(attr.data[i].color[:3])<.3 for i in p.loop_indices)]
 bm=bmesh.new();bm.from_mesh(skin.data);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in remove],context='FACES');bm.to_mesh(skin.data);bm.free()
vs=[];fs=[];labels=[]
snout=min(v.co.y for v in skin.data.vertices);cy=(snout+hinge.y)*.5;cx=sum(v.co.x for v in skin.data.vertices if abs(v.co.y-cy)<.012)/sum(1 for v in skin.data.vertices if abs(v.co.y-cy)<.012)
for j in range(9):
 theta=math.pi*j/8
 for i in range(17):
  phi=math.tau*i/16;vs.append((cx+.019*math.sin(theta)*math.cos(phi),cy+(hinge.y-snout)*.43*math.cos(theta),mouthZ-.008+.019*math.sin(theta)*math.sin(phi)));labels.append('Head')
for j in range(8):
 for i in range(16):
  a=j*17+i;fs.append((a,a+1,a+18,a+17))
if vs:
 data=bpy.data.meshes.new('Dark mouth interior');data.from_pydata(vs,[],fs);data.materials.append(mat);uv=data.uv_layers.new(name='UVMap')
 for item in uv.data:item.uv=(.001,.001)
 cap=bpy.data.objects.new('Mouth interior',data);bpy.data.collections['TripoMorayAsset'].objects.link(cap)
 for name in ['Head','Jaw']:
  group=cap.vertex_groups.new(name=name);ids=[i for i,n in enumerate(labels) if n==name]
  if ids:group.add(ids,1,'REPLACE')
 for obj,color in [(skin,(1,1,1,1)),(cap,(.055,.028,.025,1))]:
  attr=obj.data.color_attributes.get('Color') or obj.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
  for c in attr.data:c.color=color
 bpy.ops.object.select_all(action='DESELECT');skin.select_set(True);cap.select_set(True);bpy.context.view_layer.objects.active=skin;bpy.ops.object.join()
 nodes=mat.node_tree.nodes
 if not any(n.type=='VERTEX_COLOR' for n in nodes):
  principled=nodes.get('Principled BSDF');old=principled.inputs['Base Color'].links[0].from_socket
  color=nodes.new('ShaderNodeVertexColor');color.layer_name='Color';mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
  mat.node_tree.links.new(old,mix.inputs[1]);mat.node_tree.links.new(color.outputs['Color'],mix.inputs[2]);mat.node_tree.links.new(mix.outputs[0],principled.inputs['Base Color'])
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=None;bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TripoMoray.blend'))
for track in rig.animation_data.nla_tracks:track.mute=False
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'TripoMoray.fbx'),'--collection','TripoMorayAsset','--max-triangles','20000','--max-bones','16','--max-texture','4096','--max-materials','1','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
for track in rig.animation_data.nla_tracks:track.mute=True
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.max_bounces=4;scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1000;scene.render.resolution_y=700;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('Moray review studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.05,.07,.09,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4
for name,pos,power in [('Key',(1,-1,2),110),('Fill',(-1,-.5,.5),60),('Rim',(0,1,1),90)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=1.5;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);scene.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO';data.ortho_scale=1.25
def render(label,pos,action,frame):
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);camera.location=pos;camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDERS/(label+'.png'));bpy.ops.render.render(write_still=True)
for label,pos in [('ThreeQuarter',(1,-1,.65)),('Opposite',(-1,-1,.5)),('Side',(1,0,.15)),('Front',(0,-2,.25))]:render(label,pos,'Idle',24)
render('React',(1,-1,.4),'React',25)
scene.render.resolution_x=640;scene.render.resolution_y=448;scene.cycles.samples=8
for action,step in [('Swim',6),('Idle',8)]:
 for i,frame in enumerate(range(1,step*12+1,step)):render(f'{action}_{i:02d}',(1,-1,.65),action,frame)
print('TRIPO_REVIEW_COMPLETE mouthInteriorTriangles='+str(len(fs)),flush=True)
