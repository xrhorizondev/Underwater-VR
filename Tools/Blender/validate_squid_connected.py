import bpy,bmesh,json,math
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/SquidConnectedRepair'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(out/'ReferenceSquid.fbx'))
obj=next(o for o in bpy.data.objects if o.type=='MESH');rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');scene=bpy.context.scene
# FBX splits UV/normal corners. Weld a topology-only copy for the watertight audit.
bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
open_opaque=sum(e.is_boundary and any(f.material_index==0 for f in e.link_faces) for e in bm.edges);assert open_opaque==0,open_opaque
bm.free();worst_weight=max(abs(sum(g.weight for g in v.groups)-1) for v in obj.data.vertices);assert worst_weight<1e-5
samples=[]; first,last=map(int,rig.animation_data.action.frame_range)
for frame in range(first,last+1):
 scene.frame_set(frame);evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();coords=[v.co.copy() for v in mesh.vertices];assert all(math.isfinite(x) for p in coords for x in p)
 if frame==first:start=coords
 if frame==last:end=coords
 samples.append(max(p.length for p in coords));evaluated.to_mesh_clear()
seam=max((a-b).length for a,b in zip(start,end));assert seam<1e-4,seam
report={'opaqueOpenEdgesAfterWeld':open_opaque,'maxWeightError':worst_weight,'animationPhases':49,'loopVertexError':seam,'bones':len(rig.data.bones),'status':'passed'}
(out/'RoundTrip.json').write_text(json.dumps(report,indent=2));print('ROUNDTRIP',json.dumps(report))



