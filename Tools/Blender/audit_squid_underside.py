import bpy, json
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR')
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/SquidAnimationRepair/ReferenceSquid.blend'))
obj=bpy.data.objects['ReferenceSquidVisual']
import bmesh
bm=bmesh.new();bm.from_mesh(obj.data)
boundary=[e for e in bm.edges if e.is_boundary]
print('SQUID_BOUNDARY',len(boundary))
groups=[];remaining=set(boundary)
while remaining:
    edge=remaining.pop();edges={edge};verts=set(edge.verts);todo=list(edge.verts)
    while todo:
        v=todo.pop()
        for e in v.link_edges:
            if e in remaining:
                remaining.remove(e);edges.add(e)
                for n in e.verts:
                    if n not in verts:verts.add(n);todo.append(n)
    xyz=[v.co for v in verts]
    groups.append({'edges':len(edges),'min':[min(v[i] for v in xyz) for i in range(3)],'max':[max(v[i] for v in xyz) for i in range(3)]})
print('SQUID_OPEN_COMPONENTS',json.dumps(groups))
print('SQUID_VOLUME',bm.calc_volume(signed=True))
bm.free()
