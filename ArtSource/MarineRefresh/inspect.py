import bpy,json,traceback
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh';report={}
for kind,path in [('Dolphin',base/'Source/Dolphin/source/LPP.fbx'),('Shark',base/'Source/Shark.glb'),('CombJelly',base/'Source/CombJelly.glb'),('Lionfish',base/'Source/Lionfish.glb')]:
 try:
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
  if path.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(path))
  else:bpy.ops.import_scene.gltf(filepath=str(path))
  data={'objects':[],'actions':[],'materials':[],'images':[]}
  for o in bpy.context.scene.objects:
   d={'name':o.name,'type':o.type,'dimensions':list(o.dimensions),'location':list(o.location),'rotation':list(o.rotation_euler),'scale':list(o.scale),'hidden':o.hide_render}
   if o.type=='MESH':o.data.calc_loop_triangles();d.update(triangles=len(o.data.loop_triangles),vertices=len(o.data.vertices),materials=[m.name for m in o.data.materials if m],modifiers=[m.type for m in o.modifiers])
   if o.type=='ARMATURE':d['bones']=[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local)} for b in o.data.bones]
   data['objects'].append(d)
  for a in bpy.data.actions:data['actions'].append({'name':a.name,'range':list(a.frame_range)})
  for m in bpy.data.materials:
   data['materials'].append({'name':m.name,'nodes':[(n.type,n.name,getattr(n.image,'name',None) if n.type=='TEX_IMAGE' else '') for n in m.node_tree.nodes] if m.node_tree else []})
  for im in bpy.data.images:
   if im.type=='IMAGE':data['images'].append({'name':im.name,'size':list(im.size),'path':im.filepath})
  report[kind]=data;bpy.ops.wm.save_as_mainfile(filepath=str(base/(kind+'_Imported.blend')));print('INSPECTED',kind)
 except Exception as e:report[kind]={'error':str(e)};traceback.print_exc()
(base/'inspection.json').write_text(json.dumps(report,indent=2))
print('MARINE_INSPECTION_COMPLETE')

