import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');src=root/'ArtSource/SuppliedSharkDolphin';out=root/'Renders/SuppliedSharkDolphin';out.mkdir(parents=True,exist_ok=True)
reports=[]
for kind in ['Shark','Dolphin']:
 bpy.ops.wm.open_mainfile(filepath=str(src/f'{kind}_Reduced.blend'))
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=900;scene.render.resolution_y=600;scene.render.resolution_percentage=100
 rig=next(o for o in scene.objects if o.type=='ARMATURE');meshes=[o for o in scene.objects if o.type=='MESH']
 texture=root/'Assets/UnderwaterArt/SuppliedSharkDolphin'/kind/'textures'/('Shark_Skin.jpeg' if kind=='Shark' else 'LPP_TT_checker_2048x2048_UV_GRID_BaseColor.png')
 img=bpy.data.images.load(str(texture))
 for o in meshes:
  for mat in o.data.materials:
   if not mat:continue
   bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bsdf.inputs['Roughness'].default_value=.5
   tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img;mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
 scene.world=bpy.data.worlds.new('Preview world');scene.world.color=(.15,.15,.15)
 bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO'
 report={'kind':kind,'bones':[(b.name,list(rig.matrix_world@b.head_local)) for b in rig.data.bones if any(s in b.name.lower() for s in ['head','root','body','spine'])],'poses':[]}
 for i,frame in enumerate([scene.frame_start,int(scene.frame_start+(scene.frame_end-scene.frame_start)*.25),int(scene.frame_start+(scene.frame_end-scene.frame_start)*.5),scene.frame_end]):
  scene.frame_set(frame);dg=bpy.context.evaluated_depsgraph_get();pts=[]
  for o in meshes:
   e=o.evaluated_get(dg);m=e.to_mesh();pts.extend(e.matrix_world@v.co for v in m.vertices);e.to_mesh_clear()
  lo=Vector(tuple(min(p[a] for p in pts) for a in range(3)));hi=Vector(tuple(max(p[a] for p in pts) for a in range(3)));center=(hi+lo)*.5;size=(hi-lo).length
  report['poses'].append({'frame':frame,'center':list(center),'size':list(hi-lo)})
  cam.location=center+Vector((1,-1,.5)).normalized()*size*2;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size*1.1
  if i==0:
   bpy.ops.object.light_add(type='AREA',location=center+Vector((0,-1,2))*size);bpy.context.object.data.energy=1300;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=size*2;bpy.context.object.rotation_euler=(center-bpy.context.object.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(out/f'{kind}_{i}.png');bpy.ops.render.render(write_still=True)
 reports.append(report)
(out/'poses.json').write_text(json.dumps(reports,indent=2))
