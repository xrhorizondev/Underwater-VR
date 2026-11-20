import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');folder=root/'ArtSource/TripoMoray'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(folder/'TripoMoray.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');skin=next(o for o in bpy.context.scene.objects if o.type=='MESH');scene=bpy.context.scene
if rig.animation_data:
 for track in rig.animation_data.nla_tracks:track.mute=True
def points():
 bpy.context.view_layer.update();obj=skin.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=obj.to_mesh();v=[obj.matrix_world@p.co for p in mesh.vertices];obj.to_mesh_clear();return v
report={'triangles':sum(len(p.vertices)-2 for p in skin.data.polygons),'bones':len(rig.data.bones),'clips':{}}
for action in list(bpy.data.actions):
 curves=[c for layer in action.layers for strip in layer.strips for bag in strip.channelbags for c in bag.fcurves]
 if not any('pose.bones' in c.data_path for c in curves):continue
 label=action.name.split('|')[-1];rig.animation_data.action=action;first,last=action.frame_range
 if len(action.slots):rig.animation_data.action_slot=action.slots[0]
 scene.frame_set(round(first));start=points();scene.frame_set(round((first+last)/2));middle=points();scene.frame_set(round(last));end=points()
 deformation=max((a-b).length for a,b in zip(start,middle));seam=max((a-b).length for a,b in zip(start,end))
 if deformation<.0001:raise RuntimeError('Static animation '+label)
 if label in ['Idle','Swim','Retreat'] and seam>.003:raise RuntimeError('Animation seam '+label+' '+str(seam))
 report['clips'][label]={'seconds':(last-first)/24,'deformation':deformation,'loopEndpointDifference':seam}
if set(report['clips'])!={'Idle','Swim','React','Retreat','Recover'}:raise RuntimeError('Missing clips')
if report['triangles']>20000 or report['bones']>16:raise RuntimeError('Quest asset budget exceeded')
report['status']='passed';(folder/'RoundtripValidation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
