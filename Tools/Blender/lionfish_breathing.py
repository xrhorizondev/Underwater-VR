import bpy,math,json,sys,runpy,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');reports=[]
def smooth(a,b,t):
 f=min(1.,max(0.,(t-a)/(b-a)));return f*f*(3-2*f)
for stem in ['Lionfish_Animated','Lionfish_Reduced']:
 bpy.ops.wm.open_mainfile(filepath=str(out/(stem+'.blend')));scene=bpy.context.scene;fish=next(o for o in scene.objects if o.type=='MESH');rig=next(o for o in scene.objects if o.type=='ARMATURE');mesh=fish.data
 bind=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',bind);bind=bind.reshape(-1,3);lo=bind.min(0);hi=bind.max(0);length=float(hi[1]-lo[1]);height=float(hi[2]-lo[2]);ts=(bind[:,1]-lo[1])/length;zs=(bind[:,2]-lo[2])/height;region=bind[(ts>.57)&(ts<.8)&(zs>.35)&(zs<.7)];center=np.median(region,axis=0);width=float(np.percentile(region[:,0],90)-np.percentile(region[:,0],10));uv=np.empty(len(mesh.loops)*2,dtype=np.float32);mesh.uv_layers.active.data.foreach_get('uv',uv);uvhash=hashlib.sha256(uv.tobytes()).hexdigest()
 bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
 for name,x,t in [('ChestBreath',center[0],.65),('GillLeft',center[0]-width*.25,.83),('GillRight',center[0]+width*.25,.83)]:
  e=rig.data.edit_bones.new(name);e.head=(float(x),float(lo[1]+t*length),float(center[2]));e.tail=(float(x),float(lo[1]+(t-.1)*length),float(center[2]));e.parent=rig.data.edit_bones['Root']
 bpy.ops.object.mode_set(mode='OBJECT');names={g.index:g.name for g in fish.vertex_groups};new={n:fish.vertex_groups.new(name=n) for n in ['ChestBreath','GillLeft','GillRight']}
 for v in mesh.vertices:
  p=bind[v.index];t=float(ts[v.index]);z=float(zs[v.index]);relative=float((p[0]-center[0])/max(width,.01));body=.62*smooth(.22,.42,t)*(1-smooth(.68,.83,t))*smooth(.15,.35,z)*(1-smooth(.68,.83,z));gill=.85*smooth(.62,.71,t)*(1-smooth(.79,.86,t))*smooth(.25,.4,z)*(1-smooth(.68,.8,z))*smooth(.05,.3,abs(relative));side=smooth(-.1,.1,relative);total=body+gill
  if total> .95:body*=.95/total;gill*=.95/total;total=.95
  if total<1e-6:continue
  old=[(names[g.group],g.weight) for g in v.groups if g.group in names]
  for n,w in old:fish.vertex_groups[n].add([v.index],float(w*(1-total)),'REPLACE')
  for n,w in [('ChestBreath',body),('GillLeft',gill*(1-side)),('GillRight',gill*side)]:
   if w>1e-6:new[n].add([v.index],float(w),'REPLACE')
 rig.animation_data_clear();scene.frame_start=1;scene.frame_end=73;scene.render.fps=24
 for frame in range(1,74):
  phase=(frame-1)/72*math.tau*2;breath=(1-math.cos((frame-1)/72*math.tau*3))*.5
  for name,amp,offset,axis in [('Body',.045,0,2),('TailBase',.13,-.5,2),('TailFan',.22,-1,2),('DorsalFin',.16,.7,0),('LowerFin',.13,-.7,0)]:
   pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.rotation_euler[axis]=amp*math.sin(phase+offset)
   if name=='TailFan':pb.rotation_euler[0]=.055*math.sin(phase-1)
   pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
  rig.pose.bones['ChestBreath'].scale=(1+.018*breath,1,1+.009*breath);rig.pose.bones['ChestBreath'].keyframe_insert(data_path='scale',frame=frame)
  for name,sign in [('GillLeft',1),('GillRight',-1)]:
   pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,sign*.07*breath);pb.keyframe_insert(data_path='rotation_euler',frame=frame)
 rig.animation_data.action.name='Lionfish_SwimAndBreathe'
 def evaluated(frame):
  scene.frame_set(frame);bpy.context.view_layer.update();e=fish.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,dtype=np.float32);m.vertices.foreach_get('co',a);e.to_mesh_clear();return a.reshape(-1,3)
 edges=np.empty(len(mesh.edges)*2,dtype=np.int32);mesh.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2);rest=np.linalg.norm(bind[edges[:,0]]-bind[edges[:,1]],axis=1);valid=rest>length*1e-5;first=evaluated(1);seam=float(np.linalg.norm(first-evaluated(73),axis=1).max());minimum=1;maximum=1;head_motion=0
 for f in range(1,74,3):
  a=evaluated(f);ratio=np.linalg.norm(a[edges[:,0]]-a[edges[:,1]],axis=1)[valid]/rest[valid];minimum=min(minimum,float(ratio.min()));maximum=max(maximum,float(ratio.max()));head_motion=max(head_motion,float(np.linalg.norm(a[ts>.89]-bind[ts>.89],axis=1).max()))
 if seam>1e-6 or minimum<.6 or maximum>1.6 or head_motion>1e-5:raise RuntimeError(f'Breathing deformation invalid {stem} {minimum} {maximum} {head_motion}')
 check=np.empty(bind.size,dtype=np.float32);mesh.vertices.foreach_get('co',check);assert np.array_equal(bind,check.reshape(-1,3));checkuv=np.empty(uv.size,dtype=np.float32);mesh.uv_layers.active.data.foreach_get('uv',checkuv);assert hashlib.sha256(checkuv.tobytes()).hexdigest()==uvhash
 report=dict(model=stem,triangles=len(mesh.polygons),bones=9,loop_error=seam,max_edge_ratio=maximum,min_edge_ratio=minimum,head_eye_region_motion=head_motion,bind_geometry_unchanged=True,uvs_unchanged=True,phases_checked=25);reports.append(report)
 scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig;target=stem+'_Breathing';bpy.ops.wm.save_as_mainfile(filepath=str(out/(target+'.blend')));sys.argv=['export','--','--output',str(out/(target+'.fbx')),'--max-triangles','500000','--max-bones','10','--max-materials','2','--max-texture','8192','--require-animation'];runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
 scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.view_settings.exposure=1;scene.world=bpy.data.worlds.new('Front inspection');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.33,.38,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8;bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=height*1.25;look=Vector((float(center[0]),float((lo[1]+hi[1])/2),float((lo[2]+hi[2])/2)));cam.location=look+Vector((0,length*2,0));cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler()
 rig.data.pose_position='REST';scene.render.filepath=str(out/(target+'_FrontRest.png'));bpy.ops.render.render(write_still=True);rig.data.pose_position='POSE'
 for f in [1,7,13,19]:scene.frame_set(f);scene.render.filepath=str(out/(target+'_Front'+str(f)+'.png'));bpy.ops.render.render(write_still=True)
(out/'BreathingValidation.json').write_text(json.dumps(reports,indent=2));print('GILL_BODY_BREATHING_VALIDATED',json.dumps(reports))
