"""Close opaque anatomy and weld UV seam geometry without changing the silhouette."""
import bpy,bmesh,runpy,sys,json
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR')
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/SquidAnimationRepair/ReferenceSquid.blend'))
obj=bpy.data.objects['ReferenceSquidVisual'];bm=bmesh.new();bm.from_mesh(obj.data)
before=len(bm.verts)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
opaque=[e for e in bm.edges if e.is_boundary and all(f.material_index==0 for f in e.link_faces)]
result=bmesh.ops.holes_fill(bm,edges=opaque,sides=0)
for face in result['faces']:face.material_index=0;face.smooth=True
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
remaining=sum(1 for e in bm.edges if e.is_boundary and any(f.material_index==0 for f in e.link_faces))
assert remaining==0,remaining
bm.to_mesh(obj.data);bm.free();obj.data.update()
out=root/'ArtSource/SquidUndersideRepair';out.mkdir(exist_ok=True)
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ReferenceSquid.blend'))
sys.argv=['export_quest_asset.py','--','--output',str(out/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('SQUID_UNDERSIDE_REPAIRED',json.dumps({'weldedVertices':before-len(obj.data.vertices),'sealedLoops':len(result['faces']),'opaqueOpenEdges':remaining}))
