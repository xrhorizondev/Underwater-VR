import bpy,sys,runpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh';bpy.ops.wm.open_mainfile(filepath=str(base/'Shark_Imported.blend'));scene=bpy.context.scene;scene.frame_set(0);bpy.context.view_layer.update();source=next(o for o in scene.objects if o.type=='MESH' and o.name!='Icosphere');dg=bpy.context.evaluated_depsgraph_get();data=bpy.data.meshes.new_from_object(source.evaluated_get(dg));world=source.matrix_world.copy();materials=list(source.data.materials)
for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
mesh=bpy.data.objects.new('GreatWhite_Anatomy',data);scene.collection.objects.link(mesh);rotation=Matrix.Identity(4);points=[rotation@(world@v.co) for v in mesh.data.vertices];lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(lo+hi)/2;scale=3/(hi.y-lo.y)
for v,p in zip(mesh.data.vertices,points):v.co=(p-center)*scale
for p in data.polygons:p.use_smooth=True
arm=bpy.data.armatures.new('GreatWhite_Rig');rig=bpy.data.objects.new('GreatWhite_Rig',arm);scene.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
 b=arm.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=arm.edit_bones[parent]
 return b
bone('Root',(0,0,0),(0,0,.15));bone('Body',(0,-.5,0),(0,0,0),'Root');bone('TailBase',(0,0,0),(0,.5,0),'Body');bone('TailMid',(0,.5,0),(0,1,0),'TailBase');bone('TailTip',(0,1,0),(0,1.5,0),'TailMid')
for side in [-1,1]:bone('Pectoral_'+str(side),(side*.15,-.4,-.05),(side*.55,-.25,-.12),'Body')
bpy.ops.object.mode_set(mode='OBJECT');mesh.parent=rig;modifier=mesh.modifiers.new('Swim','ARMATURE');modifier.object=rig;groups={b.name:mesh.vertex_groups.new(name=b.name) for b in arm.bones}
for v in data.vertices:
 x,y,z=v.co;tail=max(0,min(1,(y+.05)/.8));mid=max(0,min(1,(y-.4)/.6));tip=max(0,min(1,(y-.9)/.5));fin=max(0,min(1,(abs(x)-.23)/.3))*max(0,1-abs(y+.4)/.7)*(1-tail);remaining=1-fin;weights={'Body':remaining*(1-tail),'TailBase':remaining*tail*(1-mid),'TailMid':remaining*tail*mid*(1-tip),'TailTip':remaining*tail*mid*tip,'Pectoral_'+str(-1 if x<0 else 1):fin};total=sum(weights.values())
 for name,w in weights.items():
  if w>0:groups[name].add([v.index],w/total,'REPLACE')
rig.animation_data_create();action=bpy.data.actions.new('GreatWhite_Swim_Loop');rig.animation_data.action=action;scene.render.fps=30;scene.frame_start=0;scene.frame_end=90
for frame in range(0,91,2):
 phase=frame/90*2*math.pi
 for pb in rig.pose.bones:
  angle=0;axis=Vector((0,0,1))
  if pb.name=='Body':angle=math.sin(phase)*.015
  elif pb.name in {'TailBase','TailMid','TailTip'}:index=['TailBase','TailMid','TailTip'].index(pb.name);angle=math.sin(phase-index*.55)*(.045+.025*index)
  elif pb.name.startswith('Pectoral'):axis=Vector((0,1,0));angle=math.sin(phase+.7)*.025*(-1 if pb.name.endswith('-1') else 1)
  pb.rotation_mode='QUATERNION';q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion(axis,angle)@q;pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
track=rig.animation_data.nla_tracks.new();track.strips.new(action.name,0,action);rig.animation_data.action=None;scene.frame_set(0);bpy.context.preferences.filepaths.save_version=0;bpy.ops.object.select_all(action='SELECT');bpy.ops.wm.save_as_mainfile(filepath=str(base/'Shark.blend'))
sys.argv=['export','--','--output',str(root/'Assets/UnderwaterArt/MarineRefresh/Shark/Shark.fbx'),'--max-triangles','4000','--max-bones','16','--max-materials','1','--max-texture','1024','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__');print('GREAT_WHITE_CLEAN_SWIM_EXPORTED')

