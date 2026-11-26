import bpy,bmesh,json
bpy.ops.wm.open_mainfile(filepath='D:/GIT Project/Underwater-VR/ArtSource/SquidCrownRepair/ReferenceSquid.blend');o=bpy.data.objects['ReferenceSquidVisual'];names={g.index:g.name for g in o.vertex_groups};bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table();todo=set(bm.verts);parts=[]
while todo:
 v=todo.pop();part={v};q=[v]
 while q:
  for e in q.pop().link_edges:
   for n in e.verts:
    if n in todo:todo.remove(n);part.add(n);q.append(n)
 groups=sorted({names[g.group] for v in part for g in o.data.vertices[v.index].groups});parts.append({'n':len(part),'min':[round(min(v.co[i] for v in part),4) for i in range(3)],'max':[round(max(v.co[i] for v in part),4) for i in range(3)],'groups':groups})
print('COMPONENTS',json.dumps(parts),flush=True)
