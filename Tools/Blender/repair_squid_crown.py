"""Cover the old squid's exposed root neck with a continuous arm-crown collar."""
import bpy,bmesh,math,runpy,sys,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidCrownRepair';O.mkdir(exist_ok=True);V=R/'Renders/SquidCrownRepair';V.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidEmbeddedRoots/ReferenceSquid.blend'));obj=bpy.data.objects['ReferenceSquidVisual'];rig=bpy.data.objects['SquidRig'];asset=bpy.data.collections['SquidAsset'];rig.data.pose_position='REST'
armgroups={g.index for g in obj.vertex_groups if g.name.startswith('Arm')};ids={v.index for v in obj.data.vertices if any(g.group in armgroups for g in v.groups)}
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();remaining=set(bm.verts);target=set()
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 if any(v.index in ids for v in part):target.update(v.index for v in part)
bm.free();assert len(target)>1500
head=obj.copy();head.data=obj.data.copy();head.name='Continuous arm crown';head.modifiers.clear();head.parent=None;asset.objects.link(head)
bm=bmesh.new();bm.from_mesh(head.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in target],context='VERTS');bm.to_mesh(head.data);bm.free()
# Closed ellipsoid cuff: broad enough to cover the exposed underside roots.
vertices=[];faces=[];segments=32;rings=16
vertices.append((0,-1.12+.68,-.12))
for j in range(1,rings):
 theta=math.pi*j/rings
 for i in range(segments):
  a=i/segments*math.tau;vertices.append((.58*math.sin(theta)*math.cos(a),-1.12+.68*math.cos(theta),-.12+.64*math.sin(theta)*math.sin(a)))
last=len(vertices);vertices.append((0,-1.12-.68,-.12))
for i in range(segments):faces.append((0,1+i,1+(i+1)%segments))
for j in range(rings-2):
 for i in range(segments):
  a=1+j*segments+i;b=1+j*segments+(i+1)%segments;faces.append((a,a+segments,b+segments,b))
for i in range(segments):faces.append((last,last-segments+i,last-segments+(i+1)%segments))
me=bpy.data.meshes.new('Root-covering collar');me.from_pydata(vertices,[],faces);me.materials.append(obj.data.materials[0]);me.uv_layers.new(name='UVMap');cuff=bpy.data.objects.new(me.name,me);asset.objects.link(cuff)
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();cuff.vertex_groups.new(name='Root').add(list(range(len(vertices))),1,'REPLACE')
bpy.ops.object.select_all(action='DESELECT');head.select_set(True);bpy.context.view_layer.objects.active=head
mod=head.modifiers.new('Unbroken body-to-tentacle surface','BOOLEAN');mod.solver='EXACT';mod.operation='UNION';mod.object=cuff;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cuff,do_unlink=True)
# Pin the new surface and the buried part of each shaft to the same body bone.
arm_groups={g.index for g in head.vertex_groups if g.name.startswith('Arm')}
for v in head.data.vertices:
 limb={g.group:g.weight for g in v.groups if g.group in arm_groups};total=sum(limb.values());root=max(0,min(1,(v.co.y+2.35)/.55));root=root*root*(3-2*root)
 if total<1e-8:root=1
 for g in list(v.groups):head.vertex_groups[g.group].remove([v.index])
 if root>0:head.vertex_groups['Root'].add([v.index],root,'REPLACE')
 if root<1:
  for g,w in limb.items():head.vertex_groups[g].add([v.index],w/total*(1-root),'REPLACE')
uv=head.data.uv_layers.active or head.data.uv_layers.new(name='UVMap')
for poly in head.data.polygons:
 poly.use_smooth=True;values=[]
 for li in poly.loop_indices:
  p=head.data.vertices[head.data.loops[li].vertex_index].co;values.append((math.atan2(p.z,p.x)/math.tau+.5,(p.y+4.7)/4.8*.88))
 wrap=max(u for u,v in values)-min(u for u,v in values)>.5
 for li,(u,v) in zip(poly.loop_indices,values):uv.data[li].uv=(u+1 if wrap and u<.5 else u,v)
bm=bmesh.new();bm.from_mesh(head.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));assert not any(e.is_boundary for e in bm.edges);bm.to_mesh(head.data);bm.free()
# Remove exactly the old head/arm component and insert the repaired union.
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index in target],context='VERTS');bm.to_mesh(obj.data);bm.free()
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);head.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.join();obj.data.update();rig.data.pose_position='POSE';bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'ReferenceSquid.blend'))
sys.argv=['export','--','--output',str(O/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','10000','--max-bones','32','--max-texture','2048','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__');print('CROWN_REPAIR_EXPORTED',flush=True)


