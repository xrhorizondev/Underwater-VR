"""Repair the original squid: continuous head unions, separate tentacles, stable root weights."""
import bpy,bmesh,math,json,sys,runpy
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidOldRootRepair';O.mkdir(exist_ok=True);V=R/'Renders/SquidOldRootRepair';V.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidSurfaceRepair/ReferenceSquid.blend'))
obj=bpy.data.objects['ReferenceSquidVisual'];rig=bpy.data.objects['SquidRig'];asset=bpy.data.collections['SquidAsset'];rig.data.pose_position='REST';s=bpy.context.scene;s.frame_set(1)
armgroups={g.index for g in obj.vertex_groups if g.name.startswith('Arm')};arms={v.index for v in obj.data.vertices if any(g.group in armgroups for g in v.groups)}
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();remaining=set(bm.verts);headids=set()
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 if any(v.index in arms for v in part):arms.update(v.index for v in part)
 elif len(part)>180 and min(v.co.y for v in part)<-1.3 and 0<max(v.co.y for v in part)<.06:headids.update(v.index for v in part)
bm.free();assert headids,'Head component missing'
head=obj.copy();head.data=obj.data.copy();asset.objects.link(head);head.name='Seamless head and arms';head.modifiers.clear();head.parent=None
bm=bmesh.new();bm.from_mesh(head.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in headids],context='VERTS');bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(head.data);bm.free()
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index in arms|headids],context='VERTS');bm.to_mesh(obj.data);bm.free()
paths=[];parts=[]
for arm in range(10):
 feeding=arm>=8
 # Feeding tentacles previously reused an ordinary arm's angular attachment.
 # Place them between the neighbouring arms so their shafts stay independent.
 angle=(arm if not feeding else (.5 if arm==8 else 4.5))*math.tau/8+.18
 radial=Vector((math.cos(angle),0,math.sin(angle)));length=3.55 if feeding else 2.25+.3*math.sin(arm*1.7)
 rings=22 if feeding else 18;sides=10;divisions=3 if feeding else 2;points=[]
 for j in range(rings+1):
  t=j/rings;spread=.30+math.sin(t*math.pi*.7)*(.25 if feeding else .55)
  points.append(radial*spread+Vector((math.sin(t*4+arm)*.12*t,-1.02-length*t+.16*(1-t)**6,-.08+math.sin(t*2.5+arm)*.13*t)))
 paths.append(points);verts=[];faces=[];uvfaces=[]
 for j,p in enumerate(points):
  t=j/rings;radius=(.06 if feeding else .105)*(1-t)**.9+.003
  if feeding:radius+=.085*math.exp(-((t-.92)/.10)**2)
  tangent=(points[min(j+1,rings)]-points[max(0,j-1)]).normalized();across=tangent.cross(radial).normalized();up=tangent.cross(across).normalized()
  for i in range(sides):
   a=i/sides*math.tau;verts.append(p+(across*math.cos(a)+up*math.sin(a))*radius)
 for j in range(rings):
  for i in range(sides):
   faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i));uvfaces.append(((i/sides,j/rings*.88),((i+1)/sides,j/rings*.88),((i+1)/sides,(j+1)/rings*.88),(i/sides,(j+1)/rings*.88)))
 for end in [0,rings]:
  ci=len(verts);verts.append(points[end])
  for i in range(sides):faces.append((end*sides+i,end*sides+(i+1)%sides,ci));uvfaces.append(((.5,.4),(.51,.4),(.5,.41)))
 me=bpy.data.meshes.new(f'Independent arm {arm}');me.from_pydata(verts,[],faces);me.materials.append(obj.data.materials[0]);uv=me.uv_layers.new(name='UVMap')
 for p,coords in zip(me.polygons,uvfaces):
  p.use_smooth=True
  for li,co in zip(p.loop_indices,coords):uv.data[li].uv=co
 part=bpy.data.objects.new(me.name,me);asset.objects.link(part);parts.append(part)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
# Align the existing bones with the repaired rest positions; preserve all swim curves.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for arm,points in enumerate(paths):
 div=3 if arm>=8 else 2
 for j in range(div):
  bone=rig.data.edit_bones[f'Arm{arm:02d}_{j}'];bone.head=points[round(j*(len(points)-1)/div)];bone.tail=points[round((j+1)*(len(points)-1)/div)]
bpy.ops.object.mode_set(mode='OBJECT')
# Rebuild the union with a fine surface grid after separating the feeding axes.
# The previous union shared those axes with ordinary arms and fused their shafts.
bpy.ops.object.select_all(action='DESELECT');head.select_set(True)
for part in parts:part.select_set(True)
bpy.context.view_layer.objects.active=head;bpy.ops.object.join()
# Reconstruct attached roots, then apply a continuous UV wrap below.
rem=head.modifiers.new('Fine continuous root surface','REMESH');rem.mode='VOXEL';rem.voxel_size=.004;rem.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=rem.name)
head.data.calc_loop_triangles();dec=head.modifiers.new('Preserve original polygon budget','DECIMATE');dec.ratio=4200/len(head.data.loop_triangles);bpy.ops.object.modifier_apply(modifier=dec.name)
uv=head.data.uv_layers.active or head.data.uv_layers.new(name='UVMap')
for poly in head.data.polygons:
 values=[]
 for li in poly.loop_indices:
  p=head.data.vertices[head.data.loops[li].vertex_index].co
  values.append((math.atan2(p.z,p.x)/math.tau+.5,(p.y+4.7)/4.8*.88))
 wrap=max(u for u,v in values)-min(u for u,v in values)>.5
 for li,(u,v) in zip(poly.loop_indices,values):uv.data[li].uv=(u+1 if wrap and u<.5 else u,v)
for poly in head.data.polygons:poly.use_smooth=True
for g in list(head.vertex_groups):head.vertex_groups.remove(g)
for g in obj.vertex_groups:head.vertex_groups.new(name=g.name)
for v in head.data.vertices:
 p=v.co;root=max(0,min(1,(p.y+1.72)/.52));root=root*root*(3-2*root)
 if root>=.99999:head.vertex_groups['Root'].add([v.index],1,'REPLACE');continue
 nearest=(1e9,0,0)
 for arm,points in enumerate(paths):
  for j,(a,b) in enumerate(zip(points,points[1:])):
   d=b-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));dist=(p-a-d*t).length_squared
   if dist<nearest[0]:nearest=(dist,arm,(j+t)/(len(points)-1))
 _,arm,t=nearest;div=3 if arm>=8 else 2;f=t*div;j=min(div-1,int(f));mix=f-j;w={f'Arm{arm:02d}_{j}':1-mix if j<div-1 else 1}
 if j<div-1:w[f'Arm{arm:02d}_{j+1}']=mix
 for name,value in w.items():
  if value>0:head.vertex_groups[name].add([v.index],value*(1-root),'REPLACE')
 if root>0:head.vertex_groups['Root'].add([v.index],root,'REPLACE')
# Leave original mantle, fin skirt, siphon, eye geometry, materials and gameplay rig intact.
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);head.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.003);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
openedges=sum(e.is_boundary and any(f.material_index==0 for f in e.link_faces) for e in bm.edges);assert openedges==0,openedges
bm.to_mesh(obj.data);bm.free();obj.data.update();rig.data.pose_position='POSE';s.frame_set(1)
texture=bpy.data.images.load(str(R/'Assets/UnderwaterArt/BlenderSquid/SquidSkin.png'),check_existing=True);texture.pack();mat=obj.data.materials[0];tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=texture;mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
bpy.ops.wm.save_as_mainfile(filepath=str(O/'ReferenceSquid.blend'))
sys.argv=['export_quest_asset.py','--','--output',str(O/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','10000','--max-bones','32','--max-texture','2048','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('OLD_SQUID_ROOT_REPAIR_EXPORTED',flush=True)










