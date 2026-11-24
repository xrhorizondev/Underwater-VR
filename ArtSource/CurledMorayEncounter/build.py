import bpy,math,sys,json
from pathlib import Path
from mathutils import Vector,Quaternion
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/CurledMorayEncounter';assets=root/'Assets/UnderwaterArt/CurledMorayEncounter'
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/MorayComparison/Curled.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=bpy.data.objects['Curled_Reduced'];bpy.data.objects.remove(bpy.data.objects['Curled_Full'],do_unlink=True);mesh.name='CurledMoray_24000';rig.name='MorayEncounterRig';rig.animation_data_clear();bones=sorted(rig.data.bones,key=lambda b:b.name)
# Straighten the imported curled spine without changing the mesh or weights.
goal=Vector((0,-1,0));desired={};basis={}
for bone in bones:
 rest=bone.matrix_local.to_quaternion();tangent=(bone.tail_local-bone.head_local).normalized();desired[bone.name]=tangent.rotation_difference(goal)@rest
 parent=bone.parent
 basis[bone.name]=rest.inverted()@parent.matrix_local.to_quaternion()@desired[parent.name].inverted()@desired[bone.name] if parent else rest.inverted()@desired[bone.name]
restDesired={};restBasis={}
for bone in bones:
 tangent=(bone.tail_local-bone.head_local).normalized();flat=Vector((tangent.x,tangent.y,0)).normalized();rest=bone.matrix_local.to_quaternion();restDesired[bone.name]=tangent.rotation_difference(flat)@rest;parent=bone.parent
 restBasis[bone.name]=rest.inverted()@parent.matrix_local.to_quaternion()@restDesired[parent.name].inverted()@restDesired[bone.name] if parent else rest.inverted()@restDesired[bone.name]
for clip in ['CurledRest','SwimStraight']:
 rig.animation_data_create();rig.animation_data.action=None
 for frame in range(1,122,3):
  phase=(frame-1)/120*math.tau
  for j,bone in enumerate(bones):
   t=j/(len(bones)-1);pb=rig.pose.bones[bone.name];pb.rotation_mode='QUATERNION'
   if clip=='CurledRest':pb.rotation_quaternion=restBasis[bone.name]@Quaternion(restDesired[bone.name].inverted()@Vector((0,0,1)),(.004+.012*t)*math.sin(phase))
   else:
    worldAxis=Vector((0,0,1));localAxis=desired[bone.name].inverted()@worldAxis
    pb.rotation_quaternion=basis[bone.name]@Quaternion(localAxis,(.015+.085*t)*math.sin(phase-t*math.tau*1.2))
   pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=bone.name)
 action=rig.animation_data.action;action.name=clip;rig.animation_data.action=None;track=rig.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,1,action)
 # Strips overlap intentionally: FBX exports each named action separately.
for track in rig.animation_data.nla_tracks:track.mute=False
bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=121;bpy.context.scene.render.fps=30
# Snapshot source file keeps the two named NLA clips; exporter audits both.
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(out/'CurledMoray_Encounter.blend'))
sys.argv=['export','--','--output',str(assets/'CurledMoray.fbx'),'--max-triangles','24000','--max-bones','24','--max-texture','4096','--require-animation']
exec(compile(Path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py').read_text(),'export','exec'))
(out/'RestSpine.json').write_text(json.dumps({b.name:list(basis[b.name]) for b in bones},indent=2))


