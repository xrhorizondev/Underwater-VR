import bpy,json,math
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh';reports={}
for kind in ['Dolphin','Shark','CombJelly','Lionfish']:
 bpy.ops.wm.open_mainfile(filepath=str(base/(kind+'_Imported.blend')))
 # Ignore importer preview geometry, cameras and the lionfish reference cube.
 for o in list(bpy.data.objects):
  if o.type=='MESH' and (o.name=='Icosphere' or (kind=='Lionfish' and o.name=='Object_4')):bpy.data.objects.remove(o,do_unlink=True)
 if kind=='Dolphin':
  for im in bpy.data.images:
   p=base/'Source/Dolphin/textures'/Path(im.filepath.replace('\\','/')).name
   if p.exists():im.filepath=str(p);im.reload()
 scene=bpy.context.scene;scene.frame_set(2 if kind=='Dolphin' else 0)
 meshes=[o for o in scene.objects if o.type=='MESH'];points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(lo+hi)/2;size=hi-lo
 info={'world_min':list(lo),'world_max':list(hi),'bones':[],'objects':len(meshes),'materials':[m.name for m in bpy.data.materials]}
 for o in scene.objects:
  if o.type=='ARMATURE':info['bones']=[{'name':b.name,'head':list(o.matrix_world@b.head_local)} for b in o.data.bones]
 reports[kind]=info
 # Fast geometry reduction only for reference previews; original saved separately.
 if kind in ['CombJelly','Lionfish']:
  for o in meshes:
   o.data.calc_loop_triangles();count=len(o.data.loop_triangles)
   if count>500:
    bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Preview reduction','DECIMATE');mod.ratio=min(1,18000/sum(len(m.data.polygons) for m in meshes));bpy.ops.object.modifier_apply(modifier=mod.name)
 scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Review water');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.11,.14,1);scene.view_settings.view_transform='AgX'
 cam_data=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',cam_data);scene.collection.objects.link(cam);scene.camera=cam;cam_data.type='ORTHO';cam_data.ortho_scale=max(size)*1.35
 light=bpy.data.lights.new('Key','AREA');light.energy=700*max(size)**2;light.shape='DISK';light.size=max(size)*3;lamp=bpy.data.objects.new('Key',light);scene.collection.objects.link(lamp);lamp.location=center+Vector((1,-2,3)).normalized()*max(size)*2;lamp.rotation_euler=(center-lamp.location).to_track_quat('-Z','Y').to_euler()
 for name,direction in [('A',(1,-1,.5)),('B',(-1,1,.5))]:
  cam.location=center+Vector(direction).normalized()*max(size)*3;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(root/'Renders/MarineRefresh'/(kind+'_Source_'+name+'.png'));bpy.ops.render.render(write_still=True)
 print('RENDERED_REFERENCE',kind)
(base/'reference_inspection.json').write_text(json.dumps(reports,indent=2))
