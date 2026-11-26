"""Reference swim: travelling fin waves, mantle pulses, compact trailing arms.
Visual reference: https://www.youtube.com/shorts/caNhgitUqy4 (viewed 2026-11-26).
Geometry, repaired collar, rest rig and all gameplay travel are unchanged.
"""
import bpy,math,sys,runpy,json
from pathlib import Path
from mathutils import Vector,Quaternion
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidReferenceSwim';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidCrownRepair/ReferenceSquid.blend'))
rig=bpy.data.objects['SquidRig'];obj=bpy.data.objects['ReferenceSquidVisual'];scene=bpy.context.scene
rig.animation_data_clear()
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
scene.render.fps=24;scene.frame_start=1;scene.frame_end=97
rest={b.name:b.matrix_local.to_3x3() for b in rig.data.bones}
for frame in range(1,98):
 phase=(frame-1)/96*math.tau;global_pose={}
 # The neck and arm crown remain fixed to Root; world movement belongs to Unity.
 root=rig.pose.bones['Root'];root.rotation_quaternion=(1,0,0,0);root.location=(0,0,0);root.scale=(1,1,1);global_pose['Root']=rest['Root']
 mantle=rig.pose.bones['Mantle'];pump=math.sin(2*phase)+.22*math.sin(4*phase);radial=1+.026*pump
 mantle.rotation_quaternion=(1,0,0,0);mantle.scale=(radial,1+.003*pump,radial);global_pose['Mantle']=rest['Mantle']
 for side,label in [(-1,'L'),(1,'R')]:
  for station in range(3):
   pb=rig.pose.bones[f'Fin{label}{station}'];wave=.24*math.sin(3*phase-station*1.05)+.035*math.sin(6*phase-station*2.1)
   axis=rest[pb.name].inverted()@Vector((0,1,0));pb.rotation_quaternion=Quaternion(axis,side*wave)
 for arm in range(10):
  for segment in range(3 if arm>=8 else 2):
   name=f'Arm{arm:02d}_{segment}';pb=rig.pose.bones[name];bone=rig.data.bones[name];direction=bone.tail_local-bone.head_local
   # Draw the splayed shafts into a calmer streamlined bundle using rotation only.
   desired=Vector((direction.x*.48,direction.y,direction.z*.48));inward=direction.rotation_difference(desired)
   delay=arm*.31-segment*.9;amp=(.012 if segment==0 else .036)*(.65 if arm>=8 else 1)
   wave=Quaternion(Vector((1,0,0)),amp*math.sin(phase+delay))@Quaternion(Vector((0,0,1)),amp*.65*math.sin(phase+delay+.6))
   desired_global=(wave@inward).to_matrix()@rest[name];parent=bone.parent.name;relative_rest=rest[parent].inverted()@rest[name]
   q=(relative_rest.inverted()@global_pose[parent].inverted()@desired_global).to_quaternion()
   if q.w<0:q.negate()
   pb.rotation_quaternion=q;global_pose[name]=desired_global
 for pb in rig.pose.bones:
  pb.keyframe_insert(data_path='rotation_quaternion',frame=frame);pb.keyframe_insert(data_path='scale',frame=frame);pb.keyframe_insert(data_path='location',frame=frame)
rig.animation_data.action.name='Swim'
for layer in rig.animation_data.action.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for curve in bag.fcurves:
    for key in curve.keyframe_points:key.interpolation='LINEAR'
scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(O/'ReferenceSquid.blend'))
sys.argv=['export','--','--output',str(O/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
(O/'MotionNotes.json').write_text(json.dumps({'reference':'https://www.youtube.com/shorts/caNhgitUqy4','observed':'Undulating lateral fins, mantle contraction, compact arms with small trailing motion','durationSeconds':4,'frames':97,'fps':24,'finWavesPerLoop':3,'mantlePulsesPerLoop':2,'rootMotion':False,'geometryChanged':False,'restRigChanged':False},indent=2));print('REFERENCE_SWIM_EXPORTED',flush=True)
