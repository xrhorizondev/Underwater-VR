import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidCrownRepair';V=R/'Renders/SquidCrownRepair'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/'ReferenceSquid.fbx'));obj=next(o for o in bpy.data.objects if o.type=='MESH');rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');s=bpy.context.scene
bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);opaque_open=sum(e.is_boundary and any(f.material_index==0 for f in e.link_faces) for e in bm.edges);print('BOUNDARIES',[[list(v.co) for v in e.verts] for e in bm.edges if e.is_boundary and any(f.material_index==0 for f in e.link_faces)],flush=True);assert opaque_open==0,opaque_open;bm.free()
# A plane through the shafts must cut ten disconnected tubes, not joined sheets.
def sections(y):
 adj={}
 for p in obj.data.polygons:
  hits=[]
  for a,b in p.edge_keys:
   pa=obj.data.vertices[a].co;pb=obj.data.vertices[b].co
   if (pa.y-y)*(pb.y-y)<0:hits.append(tuple(sorted((tuple(round(x,6) for x in pa),tuple(round(x,6) for x in pb)))))
  if len(hits)==2:
   a,b=hits;adj.setdefault(a,set()).add(b);adj.setdefault(b,set()).add(a)
 remain=set(adj);count=0
 while remain:
  a=remain.pop();todo=[a];count+=1
  while todo:
   for b in adj[todo.pop()]:
    if b in remain:remain.remove(b);todo.append(b)
 return count
counts={str(y):sections(y) for y in [-2.2,-2.6]};assert all(n==10 for n in counts.values()),counts
weights=max(abs(sum(g.weight for g in v.groups)-1) for v in obj.data.vertices);assert weights<1e-5,weights
start,end=map(int,rig.animation_data.action.frame_range);first=None;last=None;movement=0;stretch=0;rest=[v.co.copy() for v in obj.data.vertices];edges=[tuple(e.vertices) for e in obj.data.edges]
for f in range(start,end+1):
 s.frame_set(f);ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();pts=[v.co.copy() for v in me.vertices];assert all(math.isfinite(x) for p in pts for x in p)
 if first is None:first=pts
 if f==end:last=pts
 movement=max(movement,max((a-b).length for a,b in zip(pts,first)))
 for a,b in edges:
  length=(rest[a]-rest[b]).length
  if length>.005:stretch=max(stretch,(pts[a]-pts[b]).length/length)
 ev.to_mesh_clear()
seam=max((a-b).length for a,b in zip(first,last));assert seam<1e-4,seam;assert stretch<2,stretch
report=dict(opaqueOpenEdges=opaque_open,shaftSections=counts,sampledFrames=end-start+1,loopSeam=seam,maxWeightError=weights,maxDeformation=movement,maxEdgeStretch=stretch,status='passed');(V/'RoundTrip.json').write_text(json.dumps(report,indent=2));print('OLD_SQUID_ROUNDTRIP_PASSED',json.dumps(report),flush=True)
# Inspect actual exported geometry from below, both sides, and the front.
mat=obj.data.materials[0];mat.use_nodes=True;im=bpy.data.images.load(str(R/'Assets/UnderwaterArt/BlenderSquid/SquidSkin.png'));node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;mat.node_tree.nodes.active=node;mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_cavity=True;s.render.resolution_x=1100;s.render.resolution_y=800;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Studio');s.world.color=(.025,.04,.055)
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO'
for label,pos,focus,scale in [('Whole',(9,-8,6),(0,-.3,0),10),('Opposite',(-9,-8,5),(0,-.3,0),10),('RootsBelow',(3,-3,-3),(0,-1.25,0),2.8),('Front',(0,-8,0),(0,-1.6,0),3.5),('Tentacles',(5,-4,2),(0,-2.6,0),5)]:
 for f in [1,13,25,37]:
  s.frame_set(f);d.ortho_scale=scale;c.location=pos;c.rotation_euler=(Vector(focus)-c.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(V/f'{label}_{f:02}.png');bpy.ops.render.render(write_still=True)



