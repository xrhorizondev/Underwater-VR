"""Inset the proven old squid's attached roots, leaving all distal geometry intact."""
import bpy,bmesh,math,sys,runpy,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidEmbeddedRoots';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidOldRootRepair/ReferenceSquid.blend'));obj=bpy.data.objects['ReferenceSquidVisual'];rig=bpy.data.objects['SquidRig'];rig.data.pose_position='REST'
armgroups={g.index for g in obj.vertex_groups if g.name.startswith('Arm')};ids={v.index for v in obj.data.vertices if any(g.group in armgroups for g in v.groups)}
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();remaining=set(bm.verts);target=set()
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 if any(v.index in ids for v in part):target.update(v.index for v in part)
bm.free();changed=0
for v in obj.data.vertices:
 if v.index not in target:continue
 p=v.co.copy();inset=math.exp(-((p.y+1.35)/.30)**2) if -2.1<p.y<-.6 else 0
 if inset>.001:v.co.y+=.22*inset;v.co.x*=1-.20*inset;v.co.z*=1-.20*inset;changed+=1
 # Keep a longer cuff rigid to the body; move bending outside the attachment.
 weights={g.group:g.weight for g in v.groups if g.group in armgroups};total=sum(weights.values());root=max(0,min(1,(v.co.y+2.05)/.55));root=root*root*(3-2*root)
 if total<1e-8:root=1
 for g in list(v.groups):obj.vertex_groups[g.group].remove([v.index])
 if root>0:obj.vertex_groups['Root'].add([v.index],root,'REPLACE')
 if root<1:
  for g,w in weights.items():obj.vertex_groups[g].add([v.index],w/total*(1-root),'REPLACE')
# Put all ten first-joint pivots well inside the head, preserving child bones.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT');embedded=[]
for arm in range(10):
 b=rig.data.edit_bones[f'Arm{arm:02d}_0'];b.head.x*=.40;b.head.z*=.40;b.head.y+=.42;p=b.head;ellipsoid=(p.x/.51)**2+((p.y+.63)/.68)**2+(p.z/.43)**2;assert ellipsoid<.35;embedded.append({'arm':arm,'pivot':list(p),'headEllipsoidFraction':ellipsoid})
bpy.ops.object.mode_set(mode='OBJECT')
# Regenerate only the affected surface UVs with the same continuous atlas wrap.
uv=obj.data.uv_layers.active
for poly in obj.data.polygons:
 if not all(i in target for i in poly.vertices):continue
 vals=[]
 for li in poly.loop_indices:
  p=obj.data.vertices[obj.data.loops[li].vertex_index].co;vals.append((math.atan2(p.z,p.x)/math.tau+.5,(p.y+4.7)/4.8*.88))
 wrap=max(u for u,v in vals)-min(u for u,v in vals)>.5
 for li,(u,v) in zip(poly.loop_indices,vals):uv.data[li].uv=(u+1 if wrap and u<.5 else u,v)
rig.data.pose_position='POSE';bpy.context.scene.frame_set(1);obj.data.update();bpy.ops.wm.save_as_mainfile(filepath=str(O/'ReferenceSquid.blend'))
(O/'EmbeddedPivots.json').write_text(json.dumps({'changedRootVertices':changed,'pivots':embedded},indent=2))
sys.argv=['export','--','--output',str(O/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
