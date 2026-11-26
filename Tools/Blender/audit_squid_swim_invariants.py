import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'Renders/SquidReferenceSwim';O.mkdir(exist_ok=True)
def snapshot(path):
 bpy.ops.wm.open_mainfile(filepath=str(path));o=bpy.data.objects['ReferenceSquidVisual'];r=bpy.data.objects['SquidRig'];return {'vertices':[tuple(v.co) for v in o.data.vertices],'faces':[(tuple(p.vertices),p.material_index) for p in o.data.polygons],'uvs':[tuple(v.uv) for v in o.data.uv_layers.active.data],'bones':[(b.name,tuple(x for row in b.matrix_local for x in row)) for b in r.data.bones]}
before=snapshot(R/'ArtSource/SquidCrownRepair/ReferenceSquid.blend');after=snapshot(R/'ArtSource/SquidReferenceSwim/ReferenceSquid.blend');assert before==after,'Animation changed geometry, UVs or rest rig'
o=bpy.data.objects['ReferenceSquidVisual'];r=bpy.data.objects['SquidRig'];root=o.vertex_groups['Root'].index;fixed=[v.index for v in o.data.vertices if any(g.group==root and g.weight>.99999 for g in v.groups)];first=None;drift=0
for f in range(1,98):
 bpy.context.scene.frame_set(f);ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();pts=[me.vertices[i].co.copy() for i in fixed]
 if first is None:first=pts
 drift=max(drift,max((a-b).length for a,b in zip(first,pts)));ev.to_mesh_clear();assert r.pose.bones['Root'].location.length<1e-8
assert drift<1e-6,drift
(O/'Invariants.json').write_text(json.dumps({'geometryUnchanged':True,'uvsUnchanged':True,'restRigUnchanged':True,'rigidCrownAndEyeVertices':len(fixed),'maxCrownAndEyeDrift':drift,'rootTranslation':0,'frames':97,'status':'passed'},indent=2));print('SWIM_INVARIANTS_PASSED',flush=True)
