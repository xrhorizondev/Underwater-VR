import bpy,bmesh,json
from pathlib import Path
R=Path('D:/GIT Project/Underwater-VR');bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidFullBody/SuppliedFullBody_Working.blend'));o=next(o for o in bpy.data.objects if o.type=='MESH');bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.00001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update();bpy.context.view_layer.update();print('BOUNDS',[(min(v.co[i] for v in o.data.vertices),max(v.co[i] for v in o.data.vertices)) for i in range(3)],flush=True)
for y in [-.1,-.25,-.4,-.6,-.8,-1,-1.2,-1.4,-1.6,-1.8,-2]:
 # Intersect triangle polygons with transverse planes; connected intersection loops.
 edges={};adj={};coords={}
 for p in o.data.polygons:
  hits=[]
  for a,b in p.edge_keys:
   va=o.data.vertices[a].co;vb=o.data.vertices[b].co
   if (va.y-y)*(vb.y-y)<0:
    key=tuple(sorted((a,b)));co=va+(vb-va)*((y-va.y)/(vb.y-va.y));coords[key]=co;hits.append(key)
  if len(hits)==2:
   a,b=hits;adj.setdefault(a,set()).add(b);adj.setdefault(b,set()).add(a)
 remaining=set(adj);parts=[]
 while remaining:
  a=remaining.pop();todo=[a];part=[a]
  while todo:
   for b in adj[todo.pop()]:
    if b in remaining:remaining.remove(b);part.append(b);todo.append(b)
  if len(part)>5:
   from mathutils import Vector
   center=sum((coords[k] for k in part),Vector())/len(part);parts.append([len(part),round(center.x,3),round(center.z,3)])
 print('SLICE',y,sorted(parts,reverse=True),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(R/'ArtSource/SquidFullBody/SuppliedFullBody_Clean.blend'))
