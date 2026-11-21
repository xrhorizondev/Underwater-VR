import bpy,json,math,sys,runpy,re
import numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh'
def export(kind,budget):
 bpy.context.preferences.filepaths.save_version=0
 for material in bpy.data.materials:
  if not material.node_tree:continue
  for node in material.node_tree.nodes:
   if node.type=='TEX_IMAGE' and node.image and max(node.image.size)>1024:
    im=node.image;im.scale(1024,1024);im.filepath_raw=str(root/'Assets/UnderwaterArt/MarineRefresh'/kind/'textures'/(re.sub('[^a-zA-Z0-9]+','_',im.name)+'_Auxiliary.png'));im.file_format='PNG';im.save()
    if im.packed_file:im.unpack(method='REMOVE')
    im.pack()
 bpy.ops.object.select_all(action='SELECT');bpy.ops.wm.save_as_mainfile(filepath=str(base/(kind+'.blend')))
 sys.argv=['export','--','--output',str(root/'Assets/UnderwaterArt/MarineRefresh'/kind/(kind+'.fbx')),'--max-triangles',str(budget),'--max-bones','48','--max-materials','3','--max-texture','1024','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
# Preserve the original shark motion and close its slightly mismatched source seam.
bpy.ops.wm.open_mainfile(filepath=str(base/'Shark.blend'));scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');end=scene.frame_end;samples=[]
for frame in range(end+1):
 scene.frame_set(frame);samples.append({p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones})
for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
action=bpy.data.actions.new('GreatWhite_Swim_Loop');rig.animation_data.action=action
for frame,pose in enumerate(samples):
 t=max(0,min(1,(frame-(end-12))/12));blend=t*t*(3-2*t)
 for p in rig.pose.bones:
  a,q,s=pose[p.name];a0,q0,s0=samples[0][p.name];p.rotation_mode='QUATERNION';p.location=a.lerp(a0,blend);p.rotation_quaternion=q.slerp(q0,blend);p.scale=s.lerp(s0,blend)
  p.keyframe_insert(data_path='location',frame=frame,group=p.name);p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert(data_path='scale',frame=frame,group=p.name)
track=rig.animation_data.nla_tracks.new();track.strips.new(action.name,0,action);rig.animation_data.action=None;scene.frame_set(0);export('Shark',4000)
# Lionfish is an unlit photographic scan: extract the emission-linked base atlases.
bpy.ops.wm.open_mainfile(filepath=str(base/'Lionfish.blend'));out=root/'Assets/UnderwaterArt/MarineRefresh/Lionfish';manifest=json.loads((out/'materials.json').read_text())
for row in manifest['materials']:
 m=bpy.data.materials[row['name']];images=[n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image];im=images[0];name='Lionfish_'+im.name+'_BaseColor.png';im.filepath_raw=str(out/'textures'/name);im.file_format='PNG';im.save();im.pack();row['baseColorTexture']='textures/'+name
(out/'materials.json').write_text(json.dumps(manifest,indent=2));export('Lionfish',12500)
# Seal duplicate UV-boundary positions and bake a genuine four-channel tissue PNG.
bpy.ops.wm.open_mainfile(filepath=str(base/'CombJelly.blend'));scene=bpy.context.scene;mesh=next(o for o in scene.objects if o.type=='MESH');bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00015);bpy.ops.object.mode_set(mode='OBJECT')
mesh.data.normals_split_custom_set([(0,0,0)]*len(mesh.data.loops));attr=mesh.data.color_attributes['CombRows']
for v in mesh.data.vertices:
 x,y,z=v.co;angle=math.atan2(y,x);rows=max(0,math.cos(angle*8))**18;rows*=max(0,min(1,(z+.48)/.12));attr.data[v.index].color=(rows,(z+.5),angle/(2*math.pi)+.5,1)
mat=mesh.data.materials[0];tree=mat.node_tree;output=next(n for n in tree.nodes if n.type=='OUTPUT_MATERIAL');link=output.inputs['Surface'].links[0].from_socket;color=tree.nodes.new('ShaderNodeVertexColor');color.layer_name='CombRows';em=tree.nodes.new('ShaderNodeEmission');tree.links.new(color.outputs['Color'],em.inputs['Color']);tree.links.new(em.outputs[0],output.inputs['Surface']);mask=bpy.data.images.new('FreshCombMask',1024,1024,alpha=True);target=tree.nodes.new('ShaderNodeTexImage');target.image=mask;tree.nodes.active=target;scene.render.engine='CYCLES';scene.cycles.samples=1;bpy.ops.object.bake(type='EMIT',margin=8)
maskpixels=np.array(mask.pixels[:],dtype=np.float32).reshape(-1,4)[:,0];tree.links.new(link,output.inputs['Surface']);tree.nodes.remove(color);tree.nodes.remove(em);tree.nodes.remove(target)
source=next(n for n in tree.nodes if n.type=='TEX_IMAGE' and n.image and ('Color_' in n.image.name or n.image.name=='TransparentTissue'));source.image.scale(1024,1024);rgba=np.array(source.image.pixels[:],dtype=np.float32).reshape(-1,4);rgba[:,3]=.10+.65*np.clip(maskpixels,0,1)
image=bpy.data.images.new('TransparentTissue',1024,1024,alpha=True);image.alpha_mode='STRAIGHT';image.pixels.foreach_set(rgba.ravel());image.update();image.filepath_raw=str(root/'Assets/UnderwaterArt/MarineRefresh/CombJelly/textures/CombJelly_TransparentTissue.png');image.file_format='PNG';image.save();image.pack();source.image=image
bsdf=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED');tree.links.new(source.outputs['Alpha'],bsdf.inputs['Alpha']);bsdf.inputs['Roughness'].default_value=.16;bsdf.inputs['Specular IOR Level'].default_value=.55;mat.surface_render_method='DITHERED'
scene.frame_set(0);export('CombJelly',6500);print('MARINE_POLISH_COMPLETE')


