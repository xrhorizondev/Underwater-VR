import bpy,math,json,hashlib,runpy,sys
import numpy as np
from pathlib import Path
from mathutils import Vector
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'SelectedOriginal.blend'))
fish=next(o for o in bpy.context.scene.objects if o.type=='MESH');mesh=fish.data
coords=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',coords);coords=coords.reshape(-1,3);original=coords.copy();lo=coords.min(0);hi=coords.max(0);mid=(lo+hi)/2;length=hi[1]-lo[1];height=hi[2]-lo[2]
uv=np.empty(len(mesh.loops)*2,dtype=np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);uvhash=hashlib.sha256(uv.tobytes()).hexdigest()
bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0));rig=bpy.context.object;rig.name='Lionfish Swim Rig';bones=rig.data.edit_bones;bones.remove(bones[0])
def bone(name,a,b,parent=None):
 e=bones.new(name);e.head=a;e.tail=b;e.parent=bones.get(parent) if parent else None;return e
def p(t):return (mid[0],lo[1]+t*length,mid[2])
bone('Root',p(.9),p(1.02));bone('Body',p(.65),p(.4),'Root');bone('TailBase',p(.4),p(.2),'Body');bone('TailFan',p(.2),p(.01),'TailBase')
bone('DorsalFin',p(.53),(mid[0],lo[1]+.53*length,mid[2]+height*.3),'Root');bone('LowerFin',p(.47),(mid[0],lo[1]+.47*length,mid[2]-height*.25),'Root')
bpy.ops.object.mode_set(mode='OBJECT');groups={n:fish.vertex_groups.new(name=n) for n in ['Root','Body','TailBase','TailFan','DorsalFin','LowerFin']}
def smooth(a,b,t):
 f=min(1.,max(0.,(t-a)/(b-a)));return f*f*(3-2*f)
for i,v in enumerate(coords):
 t=(v[1]-lo[1])/length;z=(v[2]-lo[2])/height
 if t>=.7:w={'Root':1.}
 elif t>=.4:
  a=min(1,max(0,(.7-t)/.3));w={'Root':1-a,'Body':a}
 elif t>=.2:
  a=(.4-t)/.2;w={'Body':1-a,'TailBase':a}
 else:
  a=min(1,(.2-t)/.16);w={'TailBase':1-a,'TailFan':a}
 fin=.5*smooth(.62,1.,z)*smooth(.2,.35,t)*(1-smooth(.58,.75,t))
 lower=.4*(1-smooth(0,.32,z))*smooth(.2,.35,t)*(1-smooth(.55,.7,t))
 for n in w:w[n]*=1-fin-lower
 if fin:w['DorsalFin']=fin
 if lower:w['LowerFin']=lower
 for name,weight in w.items():
  if weight>1e-6:groups[name].add([i],float(weight),'REPLACE')
mod=fish.modifiers.new('Gentle swim deformation','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=True;fish.parent=rig
scene=bpy.context.scene;scene.render.fps=24;scene.frame_start=1;scene.frame_end=73
for frame in range(1,74):
 phase=(frame-1)/72*math.tau
 for name,amp,offset,axis in [('Body',.018,0,2),('TailBase',.07,-.5,2),('TailFan',.12,-1.0,2),('DorsalFin',.025,.7,1),('LowerFin',.035,-.7,1)]:
  pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.rotation_euler[axis]=amp*math.sin(phase+offset);pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
rig.animation_data.action.name='Lionfish_GentleSwim'
scene.frame_set(1);bpy.context.view_layer.update()
edges=np.empty(len(mesh.edges)*2,dtype=np.int32);mesh.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2);rest=np.linalg.norm(original[edges[:,0]]-original[edges[:,1]],axis=1);valid=rest>length*1e-5
def evaluated(frame):
 scene.frame_set(frame);bpy.context.view_layer.update();e=fish.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,dtype=np.float32);m.vertices.foreach_get('co',a);e.to_mesh_clear();return a.reshape(-1,3)
first=evaluated(1);last=evaluated(73);seam=float(np.max(np.linalg.norm(first-last,axis=1)));stretch=1.;compression=1.
for frame in range(1,74,3):
 a=evaluated(frame)
 if not np.isfinite(a).all():raise RuntimeError('Invalid animated vertex')
 d=np.linalg.norm(a[edges[:,0]]-a[edges[:,1]],axis=1)[valid]/rest[valid];stretch=max(stretch,float(d.max()));compression=min(compression,float(d.min()))
if seam>1e-6 or stretch>1.65 or compression<.5:raise RuntimeError(f'Deformation check failed {seam} {stretch} {compression}')
unchanged=np.empty(original.size,dtype=np.float32);mesh.vertices.foreach_get('co',unchanged);uvcheck=np.empty(uv.size,dtype=np.float32);mesh.uv_layers.active.data.foreach_get('uv',uvcheck)
assert np.array_equal(original,unchanged.reshape(-1,3)) and hashlib.sha256(uvcheck.tobytes()).hexdigest()==uvhash
report=dict(vertices=len(mesh.vertices),polygons=len(mesh.polygons),triangles=sum(len(p.vertices)-2 for p in mesh.polygons),bones=len(rig.data.bones),materials=len(mesh.materials),loop_vertex_error=seam,max_edge_stretch=stretch,min_edge_ratio=compression,bind_geometry_unchanged=True,uvs_unchanged=True,clip='Lionfish_GentleSwim',seconds=3,checked_phases=25)
(out/'AnimationValidation.json').write_text(json.dumps(report,indent=2))
for image in bpy.data.images:
 if image.source=='FILE' and image.has_data:
  image.filepath_raw=str(out/(image.name.replace('/','_')+'.png'));image.file_format='PNG';image.save();image.pack()
scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Lionfish_Animated.blend'))
# Preserve full geometry and texture resolution; this is deliberately not a Quest optimization pass.
sys.argv=['export','--','--output',str(out/'Lionfish_Animated.fbx'),'--max-triangles','500000','--max-bones','8','--max-materials','2','--max-texture','8192','--require-animation']
runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.view_settings.exposure=1
scene.world=bpy.data.worlds.new('Review world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.33,.38,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=length*1.25
center=Vector(mid)
for label,offset in [('Side',(1,0,.05)),('Reverse',(-1,0,.05)),('ThreeQuarter',(1,.6,.2))]:
 cam.location=center+Vector(offset)*length*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
 for frame in ([1,19,37,55] if label=='Side' else [19]):
  scene.frame_set(frame);scene.render.filepath=str(out/(label+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
print('LIONFISH_ANIMATION_VALIDATED',json.dumps(report))
