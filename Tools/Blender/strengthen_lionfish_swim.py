import bpy,math,json,sys,runpy,hashlib
import numpy as np
from pathlib import Path
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');reports=[]
for stem in ['Lionfish_Animated','Lionfish_Reduced']:
 bpy.ops.wm.open_mainfile(filepath=str(out/(stem+'.blend')));scene=bpy.context.scene;fish=next(o for o in scene.objects if o.type=='MESH');rig=next(o for o in scene.objects if o.type=='ARMATURE');mesh=fish.data
 bind=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',bind);bind=bind.reshape(-1,3);length=float(np.ptp(bind[:,1]));edges=np.empty(len(mesh.edges)*2,dtype=np.int32);mesh.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2);rest=np.linalg.norm(bind[edges[:,0]]-bind[edges[:,1]],axis=1);valid=rest>length*1e-5;uv=np.empty(len(mesh.loops)*2,dtype=np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);uvhash=hashlib.sha256(uv.tobytes()).hexdigest()
 next(m for m in fish.modifiers if m.type=='ARMATURE').use_deform_preserve_volume=False;rig.animation_data_clear();scene.frame_start=1;scene.frame_end=73;scene.render.fps=24
 for frame in range(1,74):
  phase=(frame-1)/72*math.tau*2
  for name,amp,offset,axis in [('Body',.025,0,2),('TailBase',.13,-.5,2),('TailFan',.22,-1,2),('DorsalFin',.16,.7,0),('LowerFin',.13,-.7,0)]:
   pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.rotation_euler[axis]=amp*math.sin(phase+offset)
   if name=='TailFan':pb.rotation_euler[0]=.055*math.sin(phase-1)
   pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
 rig.animation_data.action.name='Lionfish_VisibleSwim'
 def evaluated(frame):
  scene.frame_set(frame);bpy.context.view_layer.update();e=fish.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=np.empty(len(m.vertices)*3,dtype=np.float32);m.vertices.foreach_get('co',v);e.to_mesh_clear();return v.reshape(-1,3)
 first=evaluated(1);last=evaluated(73);seam=float(np.linalg.norm(first-last,axis=1).max());maximum=1;minimum=1;visible=0
 for frame in range(1,74,3):
  v=evaluated(frame);d=np.linalg.norm(v[edges[:,0]]-v[edges[:,1]],axis=1)[valid]/rest[valid];maximum=max(maximum,float(d.max()));minimum=min(minimum,float(d.min()));visible=max(visible,float(np.linalg.norm((v-first)[:,1:],axis=1).max()))
 if seam>1e-6 or maximum>1.5 or minimum<.65:raise RuntimeError(f'Unsafe deformation {stem} {maximum} {minimum}')
 check=np.empty(bind.size,dtype=np.float32);mesh.vertices.foreach_get('co',check);assert np.array_equal(bind,check.reshape(-1,3));checkuv=np.empty(uv.size,dtype=np.float32);mesh.uv_layers.active.data.foreach_get('uv',checkuv);assert hashlib.sha256(checkuv.tobytes()).hexdigest()==uvhash
 report=dict(model=stem,triangles=len(mesh.polygons),loop_error=seam,max_edge_ratio=maximum,min_edge_ratio=minimum,side_view_motion_metres=visible,bind_geometry_unchanged=True,uvs_unchanged=True,phases_checked=25,cycle_seconds=1.5);reports.append(report)
 scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.wm.save_as_mainfile(filepath=str(out/(stem+'.blend')))
 sys.argv=['export','--','--output',str(out/(stem+'.fbx')),'--max-triangles','500000','--max-bones','8','--max-materials','2','--max-texture','8192','--require-animation'];runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
(out/'VisibleSwimValidation.json').write_text(json.dumps(reports,indent=2));print('VISIBLE_SWIM_VALIDATED',json.dumps(reports))
