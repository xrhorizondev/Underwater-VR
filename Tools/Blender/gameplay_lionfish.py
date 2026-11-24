import bpy,math,json,sys,runpy
import numpy as np
from pathlib import Path
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'Lionfish_BilateralReduced.blend'));scene=bpy.context.scene;fish=next(o for o in scene.objects if o.type=='MESH');rig=next(o for o in scene.objects if o.type=='ARMATURE')
for frame in range(1,74):
 phase=(frame-1)/72*math.tau*2
 for name,amp,lag in [('Body',.075,0),('TailBase',.15,-.5),('TailFan',.24,-1)]:
  pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler=(.02*math.sin(phase+lag+.4),0,amp*math.sin(phase+lag));pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
rig.animation_data.action.name='Lionfish_CoordinatedSwimAndBreathing';mesh=fish.data;co=np.array([v.co[:] for v in mesh.vertices]);edges=np.array([e.vertices[:] for e in mesh.edges]);rest=np.linalg.norm(co[edges[:,0]]-co[edges[:,1]],axis=1);valid=rest>1e-5;maximum=1;minimum=1;poses=[]
for frame in range(1,74,3):
 scene.frame_set(frame);bpy.context.view_layer.update();e=fish.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();v=np.array([x.co[:] for x in m.vertices]);e.to_mesh_clear();poses.append(v);r=np.linalg.norm(v[edges[:,0]]-v[edges[:,1]],axis=1)[valid]/rest[valid];maximum=max(maximum,float(r.max()));minimum=min(minimum,float(r.min()))
loop=float(np.linalg.norm(poses[0]-poses[-1],axis=1).max());assert loop<1e-6 and maximum<1.65 and minimum>.55
mesh.calc_loop_triangles();report=dict(triangles=len(mesh.loop_triangles),vertices=len(mesh.vertices),bones=9,max_edge_ratio=maximum,min_edge_ratio=minimum,loop_error=loop,phases_checked=25,body_tail_wave='progressive lateral bend with small vertical component',geometry_unchanged=True)
(out/'GameplayAnimationValidation.json').write_text(json.dumps(report,indent=2));scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.wm.save_as_mainfile(filepath=str(out/'Lionfish_Gameplay.blend'));sys.argv=['export','--','--output',str(out/'Lionfish_Gameplay.fbx'),'--max-triangles','25000','--max-bones','10','--max-materials','2','--max-texture','8192','--require-animation'];runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__');print('GAMEPLAY_LIONFISH_ANIMATION_PASSED',json.dumps(report))
