"""Extracted supplied squid: UV-preserving reduction, analytic skinning, looping swim and audited FBX."""
import bpy,bmesh,math,json,runpy,sys
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidFullBody';A=R/'Assets/UnderwaterArt/SquidFullBody';A.mkdir(exist_ok=True);V=R/'Renders/SquidFullBody';V.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(O/'SuppliedFullBody_Clean.blend'))
obj=next(o for o in bpy.data.objects if o.type=='MESH');obj.name='SquidDetailVisual'
asset=bpy.data.collections.new('SquidAsset');bpy.context.scene.collection.children.link(asset)
for c in list(obj.users_collection):c.objects.unlink(obj)
asset.objects.link(obj)
# Shared 1024 albedo retains baked eye/sucker detail. Generated source normal map is
# deliberately omitted because it represents the whole reference sheet at high density.
mat=obj.data.materials[0];mat.name='SquidSkin';tex=next(n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image.name.startswith('Color_'))
tex.scale(1024,1024);tex.filepath_raw=str(A/'SquidAlbedo.png');tex.file_format='PNG';tex.save()
for node in list(mat.node_tree.nodes):
 if node.type not in {'OUTPUT_MATERIAL','BSDF_PRINCIPLED'}:mat.node_tree.nodes.remove(node)
p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=tex;mat.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color']);p.inputs['Roughness'].default_value=.65;p.inputs['Metallic'].default_value=0
# Morphological chains follow the supplied asymmetric arm silhouettes.
endpoints=[(-.12,-1.48,.14),(-.48,-1.32,.01),(.19,-1.34,-.08),(-.28,-1.32,-.30),(-.41,-1.31,-.13),(.1,-1.48,-.30),(-.23,-1.34,.02),(-.04,-1.43,0),(.11,-2.01,-.49),(-.18,-2.01,-.20)]
mids=[(-.08,-.85,.29),(-.44,-.85,.11),(.15,-.85,.1),(-.36,-.85,-.25),(-.35,-.85,-.07),(.12,-.85,-.17),(-.25,-.85,.12),(-.04,-.85,.05),(.10,-1.0,-.20),(-.13,-1.0,-.15)]
paths=[]
for end,mid in zip(endpoints,mids):
 root=Vector((-.11,-.12,.07));mid=Vector(mid);end=Vector(end)
 paths.append([root,root.lerp(mid,.7),mid.lerp(end,.45),end])
data=bpy.data.armatures.new('SquidRig');rig=bpy.data.objects.new('SquidRig',data);asset.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);obj.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
 b=data.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=data.edit_bones[parent]
bone('Root',(-.1,0,.06),(-.1,.3,.06));bone('Mantle',(-.1,.25,.06),(-.1,2.45,.06),'Root')
bone('Fin_L',(-.1,1.65,.08),(-.55,2.15,.08),'Mantle');bone('Fin_R',(-.1,1.65,.08),(.40,2.15,.08),'Mantle')
for i,path in enumerate(paths):
 for j in range(3):bone(f'Arm{i:02d}_{j}',path[j],path[j+1],f'Arm{i:02d}_{j-1}' if j else 'Root')
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False);obj.select_set(True);bpy.context.view_layer.objects.active=obj
for b in data.bones:obj.vertex_groups.new(name=b.name)
def assign(v,weights):
 total=sum(weights.values())
 for name,w in weights.items():
  if w>1e-7:obj.vertex_groups[name].add([v.index],w/total,'REPLACE')
for v in obj.data.vertices:
 p=v.co
 if p.y>=-.08:
  mantle=max(0,min(1,(p.y-.1)/.4));weights={'Root':1-mantle,'Mantle':mantle}
  # Modest fin deformation only at broad distal mantle margins.
  fin=max(0,min(.85,(abs(p.x+.1)-.20)/.25))*max(0,min(1,(p.y-1.35)/.35))
  if fin>0:weights={k:w*(1-fin) for k,w in weights.items()};weights['Fin_L' if p.x<-.1 else 'Fin_R']=fin
  assign(v,weights);continue
 distances=[]
 for i,path in enumerate(paths):
  for j in range(3):
   a,b=path[j:j+2];d=b-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));dist=(p-(a+d*t)).length;distances.append((dist,i,j,t))
 _,i,j,t=min(distances);blend=max(0,min(1,(-p.y-.08)/.35));weights={'Root':1-blend};weights[f'Arm{i:02d}_{j}']=blend*(1-t*.55 if j<2 else 1)
 if j<2:weights[f'Arm{i:02d}_{j+1}']=blend*t*.55
 assign(v,weights)
mod=obj.modifiers.new('Squid skeletal swim','ARMATURE');mod.object=rig;obj.parent=rig
for poly in obj.data.polygons:poly.use_smooth=True
s=bpy.context.scene;s.frame_start=1;s.frame_end=73;s.render.fps=24;s.unit_settings.system='METRIC'
for f in range(1,74,3):
 phase=(f-1)/72*math.tau
 for pb in rig.pose.bones:
  pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.scale=(1,1,1)
  if pb.name=='Mantle':pb.scale=(1+.022*math.sin(phase),1,1+.025*math.sin(phase));pb.rotation_euler.x=.007*math.sin(phase)
  elif pb.name.startswith('Fin_'):pb.rotation_euler.y=(1 if pb.name.endswith('L') else -1)*.12*math.sin(phase+.6)
  elif pb.name.startswith('Arm'):
   i=int(pb.name[3:5]);j=int(pb.name[-1]);amp=(.025,.055,.09)[j]*(.65 if i>=8 else 1);pb.rotation_euler.x=amp*math.sin(phase-i*.47-j*.85);pb.rotation_euler.z=amp*.6*math.cos(phase-i*.47-j*.85)
  pb.keyframe_insert(data_path='rotation_euler',frame=f);pb.keyframe_insert(data_path='scale',frame=f)
rig.animation_data.action.name='Swim';s.frame_set(1)
# Decimate a copy in bind pose; UV seams, original colour and bone weights survive.
rig.data.pose_position='REST';low=obj.copy();low.data=obj.data.copy();asset.objects.link(low);low.name='SquidQuestVisual';low.modifiers.clear();bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
low.data.calc_loop_triangles();dec=low.modifiers.new('UV preserving Quest copy','DECIMATE');dec.ratio=7800/len(low.data.loop_triangles);dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
arm=low.modifiers.new('Squid skeletal swim','ARMATURE');arm.object=rig
# Normalize/prune to four influences after decimation.
for v in low.data.vertices:
 values=sorted([(g.group,g.weight) for g in v.groups],key=lambda x:-x[1])[:4];total=sum(w for _,w in values)
 for g in low.vertex_groups:g.remove([v.index])
 for g,w in values:low.vertex_groups[g].add([v.index],w/total,'REPLACE')
rig.data.pose_position='POSE';bpy.context.view_layer.update()
# Save both variants and pack the shared texture in the authoring file.
tex.pack();bpy.ops.wm.save_as_mainfile(filepath=str(O/'Squid_Animated_Variants.blend'))
for mesh,name,budget in [(obj,'SquidDetail',65000),(low,'SquidQuest',8000)]:
 other=low if mesh==obj else obj;asset.objects.unlink(other)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'.blend')))
 sys.argv=['export_quest_asset.py','--','--output',str(A/(name+'.fbx')),'--collection','SquidAsset','--max-triangles',str(budget),'--max-bones','40','--max-materials','1','--require-animation']
 runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__');asset.objects.link(other)
print('SQUID_VARIANTS_EXPORTED',flush=True)

