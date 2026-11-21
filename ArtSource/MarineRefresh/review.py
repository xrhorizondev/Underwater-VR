import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');report_path=root/'ArtSource/MarineRefresh/roundtrip.json';reports=json.loads(report_path.read_text()) if report_path.exists() else {};KINDS=[sys.argv[-1]] if '--' in sys.argv else ['Dolphin','Shark','CombJelly','Lionfish']
for kind in KINDS:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.render.fps=30;bpy.ops.import_scene.fbx(filepath=str(root/'Assets/UnderwaterArt/MarineRefresh'/kind/(kind+'.fbx')))
 scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');action=rig.animation_data.action;start,end=action.frame_range;meshes=[o for o in scene.objects if o.type=='MESH']
 def points(frame):
  scene.frame_set(int(math.floor(frame)),subframe=frame%1);dg=bpy.context.evaluated_depsgraph_get();return [o.matrix_world@v.co for o in meshes for v in o.evaluated_get(dg).data.vertices]
 a=points(start);ca=sum(a,Vector())/len(a);deformation=0;centermotion=0
 for phase in [.125,.25,.375,.5,.625,.75,.875]:
  b=points(start+(end-start)*phase);cb=sum(b,Vector())/len(b);deformation=max(deformation,max(((p-ca)-(q-cb)).length for p,q in zip(a,b)));centermotion=max(centermotion,(ca-cb).length)
 seam=max((p-q).length for p,q in zip(a,points(end)));[o.data.calc_loop_triangles() for o in meshes];triangles=sum(len(o.data.loop_triangles) for o in meshes)
 report={'triangles':triangles,'bones':len(rig.data.bones),'frames':[start,end],'clip':action.name,'maxDeformation':deformation,'loopSeam':seam,'centerMotion':centermotion};reports[kind]=report
 assert deformation>.005,(kind,report)
 print('MARINE_ROUNDTRIP',kind,report,flush=True)
# Visual QA uses authoring materials (FBX transport cannot encode all PBR/alpha nodes).
for kind in KINDS:
 bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/MarineRefresh'/(kind+'.blend')));scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Marine review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.025,.07,.10,1);scene.view_settings.view_transform='AgX'
 scene.frame_set(0);dg=bpy.context.evaluated_depsgraph_get();meshes=[o for o in scene.objects if o.type=='MESH'];p=[o.matrix_world@v.co for o in meshes for v in o.evaluated_get(dg).data.vertices];lo=Vector(tuple(min(v[i] for v in p) for i in range(3)));hi=Vector(tuple(max(v[i] for v in p) for i in range(3)));center=(lo+hi)/2;size=hi-lo;length=max(size)
 cam_data=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',cam_data);scene.collection.objects.link(cam);scene.camera=cam;cam_data.type='ORTHO';cam_data.ortho_scale=length*1.3
 for name,pos,power in [('Key',(1,-1,2),300),('Rim',(-1,1,1),200)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power*length**2;data.shape='DISK';data.size=length*2;lamp=bpy.data.objects.new(name,data);scene.collection.objects.link(lamp);lamp.location=center+Vector(pos).normalized()*length*2;lamp.rotation_euler=(center-lamp.location).to_track_quat('-Z','Y').to_euler()
 for view,direction,phase in [('Front',(0,-1,.15),0),('Side',(1,0,.12),.25),('OtherSide',(-1,0,.12),.5),('ThreeQuarter',(1,-1,.4),.75)]:
  scene.frame_set(round(scene.frame_end*phase));cam.location=center+Vector(direction).normalized()*length*3;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(root/'Renders/MarineRefresh'/(kind+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
(root/'ArtSource/MarineRefresh/roundtrip.json').write_text(json.dumps(reports,indent=2));print('MARINE_REVIEW_COMPLETE',flush=True)


