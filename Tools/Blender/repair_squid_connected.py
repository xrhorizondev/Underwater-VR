"""Fuse head and arms into one surface; transfer the existing UVs and skin weights."""
import bpy,bmesh,json,runpy,sys,math
from pathlib import Path
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'ArtSource/SquidConnectedRepair';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/SquidSurfaceRepair/ReferenceSquid.blend'))
obj=bpy.data.objects['ReferenceSquidVisual'];rig=bpy.data.objects['SquidRig'];scene=bpy.context.scene
# Neutral geometry, with the rig disabled while rebuilding the skin.
rig.data.pose_position='REST';bpy.context.view_layer.update()
arm_groups={g.index for g in obj.vertex_groups if g.name.startswith('Arm')}
keep={v.index for v in obj.data.vertices if any(g.group in arm_groups for g in v.groups)}
# Find the head component by its characteristic bounds (exclude eyes and siphon).
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();remaining=set(bm.verts)
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 if any(v.index in keep for v in part) or len(part)>180 and min(v.co.y for v in part)<-1.3 and max(v.co.y for v in part)<.06 and max(v.co.y for v in part)>0:
  keep.update(v.index for v in part)
bm.free()
print('SELECTED_VERTS',len(keep))
source=obj.copy();source.data=obj.data.copy();bpy.context.collection.objects.link(source);source.name='Head arms union source';source.modifiers.clear()
bm=bmesh.new();bm.from_mesh(source.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index not in keep],context='VERTS');bm.to_mesh(source.data);bm.free()
source.data.calc_loop_triangles();tris=list(source.data.loop_triangles);coords=[v.co.copy() for v in source.data.vertices];tree=BVHTree.FromPolygons(coords,[tuple(t.vertices) for t in tris],all_triangles=True)
weights=[{g.group:g.weight for g in v.groups} for v in source.data.vertices];uv=source.data.uv_layers.active
work=source.copy();work.data=source.data.copy();bpy.context.collection.objects.link(work);bpy.ops.object.select_all(action='DESELECT');work.select_set(True);bpy.context.view_layer.objects.active=work
mod=work.modifiers.new('Continuous head and arm skin','REMESH');mod.mode='VOXEL';mod.voxel_size=.007;mod.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=work.modifiers.new('Gentle surface smoothing','SMOOTH');mod.factor=.5;mod.iterations=1;bpy.ops.object.modifier_apply(modifier=mod.name)
work.data.calc_loop_triangles();mod=work.modifiers.new('Quest triangle budget','DECIMATE');mod.ratio=min(1,3800/len(work.data.loop_triangles));bpy.ops.object.modifier_apply(modifier=mod.name)
# Nearest source triangle interpolates weights and UVs onto the connected surface.
for g in list(work.vertex_groups):work.vertex_groups.remove(g)
for g in source.vertex_groups:work.vertex_groups.new(name=g.name)
def sample(p):
 loc,normal,index,d=tree.find_nearest(p);tri=tris[index];a,b,c=[coords[i] for i in tri.vertices]
 bary=barycentric_transform(loc,a,b,c,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
 return tri,[max(0,x) for x in bary]
for v in work.data.vertices:
 tri,factors=sample(v.co);values={}
 for i,f in zip(tri.vertices,factors):
  for g,w in weights[i].items():values[g]=values.get(g,0)+f*w
 values=sorted(values.items(),key=lambda x:-x[1])[:4];total=sum(w for g,w in values)
 for g,w in values:
  if w>1e-8:work.vertex_groups[g].add([v.index],w/total,'REPLACE')
layer=work.data.uv_layers.active or work.data.uv_layers.new(name='UVMap')
for poly in work.data.polygons:
 poly.use_smooth=True
 values=[]
 for loop in poly.loop_indices:
  p=work.data.vertices[work.data.loops[loop].vertex_index].co
  values.append((math.atan2(p.z,p.x)/math.tau+.5,(p.y+4.7)/4.8*.88))
 wrap=max(u for u,v in values)-min(u for u,v in values)>.5
 for loop,(u,v) in zip(poly.loop_indices,values):layer.data[loop].uv=(u+1 if wrap and u<.5 else u,v)
# Replace precisely the selected old anatomy, preserving all other components.
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.index in keep],context='VERTS');bm.to_mesh(obj.data);bm.free()
bpy.data.objects.remove(source,do_unlink=True);bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);work.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.dissolve_degenerate(bm,dist=.00001,edges=list(bm.edges));remaining=set(bm.verts)
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 if len(part)<10:bmesh.ops.delete(bm,geom=list(part),context='VERTS')
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));assert not any(e.is_boundary and any(f.material_index==0 for f in e.link_faces) for e in bm.edges);bm.to_mesh(obj.data);bm.free()
rig.data.pose_position='POSE';scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ReferenceSquid.blend'))
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('CONNECTED_SQUID_EXPORTED')






