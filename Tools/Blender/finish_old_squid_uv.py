import bpy,bmesh,math,sys,runpy
from pathlib import Path
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidOldRootRepair'
bpy.ops.wm.open_mainfile(filepath=str(O/'ReferenceSquid.blend'));obj=bpy.data.objects['ReferenceSquidVisual'];armgroups={g.index for g in obj.vertex_groups if g.name.startswith('Arm')};ids={v.index for v in obj.data.vertices if any(g.group in armgroups for g in v.groups)}
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();remaining=set(bm.verts);target=set()
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 if any(v.index in ids for v in part):target.update(v.index for v in part)
bm.free();assert len(target)>1500;uv=obj.data.uv_layers.active
for poly in obj.data.polygons:
 if not all(i in target for i in poly.vertices):continue
 values=[]
 for li in poly.loop_indices:
  p=obj.data.vertices[obj.data.loops[li].vertex_index].co;values.append((math.atan2(p.z,p.x)/math.tau+.5,(p.y+4.7)/4.8*.88))
 wrap=max(u for u,v in values)-min(u for u,v in values)>.5
 for li,(u,v) in zip(poly.loop_indices,values):uv.data[li].uv=(u+1 if wrap and u<.5 else u,v)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'ReferenceSquid.blend'))
sys.argv=['export','--','--output',str(O/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
