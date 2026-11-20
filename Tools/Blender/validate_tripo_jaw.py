import bpy,math,json
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR/ArtSource/TripoMoray')
bpy.ops.wm.open_mainfile(filepath=str(root/'TripoMoray.blend'))
rig=bpy.data.objects['TripoMorayRig'];skin=bpy.data.objects['Supplied moray skin']
rig.animation_data.action=None
for track in rig.animation_data.nla_tracks:track.mute=True
for bone in rig.pose.bones:bone.rotation_euler=(0,0,0)
def points():
 bpy.context.view_layer.update();obj=skin.evaluated_get(bpy.context.evaluated_depsgraph_get());return [v.co.copy() for v in obj.data.vertices]
start=points();jaw=rig.pose.bones['Jaw'];hinge=rig.data.bones['Jaw'].head_local
index=skin.vertex_groups['Jaw'].index;protected=[];weighted=[]
for v in skin.data.vertices:
 weight=sum(g.weight for g in v.groups if g.group==index)
 if weight>.0001:
  if v.co.y>hinge.y+.00001:raise RuntimeError('Jaw weights extend behind mouth corner')
  weighted.append(v.index)
 else:protected.append(v.index)
jaw.rotation_euler.x=-math.radians(22);end=points()
protected_motion=max((start[i]-end[i]).length for i in protected)
gape=max((start[i]-end[i]).length for i in weighted)
if protected_motion>.000001 or gape<.01:raise RuntimeError('Jaw isolation or opening failed')
report={'status':'passed','protectedVertices':len(protected),'jawVertices':len(weighted),'maxNonJawMovement':protected_motion,'maxJawOpeningMovement':gape,'hingeY':hinge.y,'jawVerticesBehindHinge':0}
(root/'JawIsolationValidation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
