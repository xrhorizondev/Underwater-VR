import bpy,math,sys,runpy
from pathlib import Path
from mathutils import Vector,Quaternion
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh'
bpy.ops.wm.open_mainfile(filepath=str(base/'Dolphin.blend'));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.animation_data_create()
for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
action=bpy.data.actions.new('Dolphin_Coordinated_Swim');rig.animation_data.action=action;scene.frame_start=0;scene.frame_end=60;scene.render.fps=30
chain={'TALE04':(0,.045),'TALE03':(.4,.060),'TALE02':(.8,.080),'TALE01':(1.2,.10)}
for frame in range(61):
 phase=frame/60*math.tau
 for pb in rig.pose.bones:
  pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion();pb.location=(0,0,0);pb.scale=(1,1,1);angle=0;axis=Vector((1,0,0))
  if pb.name=='BODY01':angle=math.sin(phase)*.012
  elif pb.name in chain:
   lag,amplitude=chain[pb.name];angle=math.sin(phase-lag)*amplitude
  elif pb.name in {'TALE_L','TALE_R'}:angle=math.sin(phase-1.45)*.025
  elif pb.name in {'FLIPPER_L02','FLIPPER_R02'}:axis=Vector((0,1,0));angle=math.sin(phase+1)*.018*(-1 if '_R' in pb.name else 1)
  if angle:
   q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion(axis,angle)@q
  pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name);pb.keyframe_insert(data_path='location',frame=frame,group=pb.name);pb.keyframe_insert(data_path='scale',frame=frame,group=pb.name)
track=rig.animation_data.nla_tracks.new();track.strips.new(action.name,0,action);rig.animation_data.action=None;scene.frame_set(0);bpy.ops.object.select_all(action='SELECT');bpy.ops.wm.save_as_mainfile(filepath=str(base/'Dolphin.blend'))
sys.argv=['export','--','--output',str(root/'Assets/UnderwaterArt/MarineRefresh/Dolphin/Dolphin.fbx'),'--max-triangles','8500','--max-bones','48','--max-materials','3','--max-texture','1024','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('DOLPHIN_COORDINATED_SWIM_EXPORTED',flush=True)
