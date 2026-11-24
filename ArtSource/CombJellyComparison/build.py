import bpy,numpy as np,math,json,sys
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/CombJellyComparison';asset=root/'Assets/UnderwaterArt/CombJellyComparison'
bpy.ops.wm.open_mainfile(filepath=str(out/'Imported.blend'))
o=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
v=np.array([v.co[:] for v in o.data.vertices]);p=np.array([p.vertices[:] for p in o.data.polygons]);sel=(v[:,1]>.22)&(v[:,2]>.025)
cross=sel[p].any(1)&~sel[p].all(1)
if cross.any():raise RuntimeError('Selection cuts triangles '+str(cross.sum()))
o.data.vertices.foreach_set('select',sel);o.data.edges.foreach_set('select',np.zeros(len(o.data.edges),dtype=bool));o.data.polygons.foreach_set('select',np.zeros(len(o.data.polygons),dtype=bool));bpy.context.scene.tool_settings.mesh_select_mode=(True,False,False);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.separate(type='SELECTED');bpy.ops.object.mode_set(mode='OBJECT')
selected=next(a for a in bpy.data.objects if a.type=='MESH' and a!=o);bpy.data.objects.remove(o,do_unlink=True);o=selected;o.name='CombJelly_Full';bpy.context.view_layer.objects.active=o
v=np.array([v.co[:] for v in o.data.vertices]);center=(v.min(0)+v.max(0))/2;scale=.55/(v[:,2].max()-v[:,2].min());v=(v-center)*scale;o.data.vertices.foreach_set('co',v.ravel());o.data.update()
# Preserve authored UV and row placement; correct opaque blue scan into translucent tissue.
src=next(i for i in bpy.data.images if i.name.startswith('Color_'));src.scale(2048,2048);pixels=np.empty(2048*2048*4,np.float32);src.pixels.foreach_get(pixels);a=pixels.reshape(-1,4);rgb=a[:,:3];lum=rgb.max(1);warm=np.maximum(rgb[:,0],rgb[:,1]);rows=np.clip((warm-.16)*2.5,0,1);brightness=np.clip((lum-.38)*1.8,0,1)
new=np.empty_like(a);new[:,:3]=np.clip(rgb*.48+np.array([.22,.19,.28])+brightness[:,None]*.12,0,1);new[:,3]=np.clip(.035+rows*.70+brightness*.1,.035,.83)
im=bpy.data.images.new('CombJelly_TranslucentTissue',2048,2048,alpha=True);im.pixels.foreach_set(new.ravel());im.filepath_raw=str(asset/'Tissue.png');im.file_format='PNG';im.save()
mat=bpy.data.materials.new('Reference translucent tissue');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();output=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');tex=n.new('ShaderNodeTexImage');tex.image=im;mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);mat.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.35;bs.inputs['Roughness'].default_value=.28;bs.inputs['Metallic'].default_value=0;bs.inputs['Transmission Weight'].default_value=.18;mat.node_tree.links.new(bs.outputs[0],output.inputs[0]);o.data.materials.clear();o.data.materials.append(mat)
for p in o.data.polygons:p.use_smooth=True
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='CombJelly_PulseRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0]);specs=[('Body',(0,0,-.10),(0,0,.25)),('LobeLeft',(0,0,.04),(0,-.22,-.23)),('LobeRight',(0,0,.04),(0,.22,-.23)),('OralLeft',(0,-.035,0),(0,-.055,-.19)),('OralRight',(0,.035,0),(0,.055,-.19))]
for name,h,t in specs:
 b=rig.data.edit_bones.new(name);b.head=h;b.tail=t
 if name!='Body':b.parent=rig.data.edit_bones['Body']
bpy.ops.object.mode_set(mode='OBJECT')
def attach(obj):
 for name,_,_ in specs:obj.vertex_groups.new(name=name)
 for vert in obj.data.vertices:
  x,y,z=vert.co;low=max(0,min(1,(.08-z)/.26));outer=max(0,min(1,(abs(y)-.045)/.09));lobe=low*outer*.9;oral=low*(1-outer)*.5
  obj.vertex_groups['Body'].add([vert.index],max(.001,1-lobe-oral),'REPLACE');obj.vertex_groups['LobeLeft' if y<0 else 'LobeRight'].add([vert.index],lobe,'REPLACE');obj.vertex_groups['OralLeft' if y<0 else 'OralRight'].add([vert.index],oral,'REPLACE')
 mod=obj.modifiers.new('Gentle lobe pulse','ARMATURE');mod.object=rig;obj.parent=rig
attach(o)
rig.animation_data_create()
for f in range(1,122,5):
 t=(f-1)/120*2*math.pi
 for name,_,_ in specs:
  pb=rig.pose.bones[name];pb.rotation_mode='XYZ';sign=-1 if 'Left' in name else 1
  pb.rotation_euler=(sign*.075*math.sin(t+(.4 if 'Oral' in name else 0)),.025*math.sin(t),0) if name!='Body' else (0,.012*math.sin(t),0)
  pb.keyframe_insert(data_path='rotation_euler',frame=f,group=name)
rig.animation_data.action.name='GentlePulse';s=bpy.context.scene;s.frame_start=1;s.frame_end=121;s.render.fps=30;s.frame_set(1)
# Reduce a geometry copy, preserving UV islands and source surface detail.
low=o.copy();low.data=o.data.copy();bpy.context.collection.objects.link(low);low.name='CombJelly_Reduced';bpy.context.view_layer.objects.active=low;dec=low.modifiers.new('UV preserving reduction','DECIMATE');dec.ratio=min(1,22000/len(low.data.polygons));bpy.ops.object.modifier_move_up(modifier=dec.name);bpy.ops.object.modifier_apply(modifier=dec.name)
counts={a.name:sum(len(p.vertices)-2 for p in a.data.polygons) for a in [o,low]};(out/'Geometry.json').write_text(json.dumps(counts,indent=2));print('COUNTS',counts)
# Keep a clean authoring scene with both variants and shared rig.
bpy.ops.wm.save_as_mainfile(filepath=str(out/'CombJelly_Comparison.blend'))
for obj,name in [(o,'Full'),(low,'Reduced')]:
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
 sys.argv=['export','--','--output',str(asset/(name+'.fbx')),'--max-triangles',str(counts[obj.name]+10),'--max-texture','2048','--require-animation']
 exec(compile(Path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py').read_text(),'export','exec'))
# Side-by-side reference render; rest positions are changed only for preview.
o.location.y=-.31;low.location.y=.31
s.world=bpy.data.worlds.new('Deep blue');s.world.color=(.015,.025,.045);s.render.engine='CYCLES';s.cycles.samples=24;s.render.resolution_x=1400;s.render.resolution_y=850;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(2.5,0,.03));c=bpy.context.object;c.rotation_euler=(Vector((0,0,0))-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=1.25;s.camera=c
bpy.ops.object.light_add(type='AREA',location=(1,-1,1));l=bpy.context.object;l.data.energy=70;l.data.size=3;l.rotation_euler=(Vector((0,0,0))-l.location).to_track_quat('-Z','Y').to_euler()
s.render.filepath=str(out/'Comparison.png');bpy.ops.render.render(write_still=True)
