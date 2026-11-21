import bpy, json
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'ArtSource/BoxJellyReplacement/Source.glb'))
report={'objects':[], 'actions':[], 'materials':[]}
for o in bpy.context.scene.objects:
 d={'name':o.name,'type':o.type,'dimensions':list(o.dimensions),'scale':list(o.scale)}
 if o.type=='MESH':
  o.data.calc_loop_triangles(); d.update(triangles=len(o.data.loop_triangles),vertices=len(o.data.vertices),shape_keys=bool(o.data.shape_keys))
 if o.type=='ARMATURE': d['bones']=len(o.data.bones)
 if o.animation_data: d['animation']={'action':str(o.animation_data.action),'tracks':[t.name for t in o.animation_data.nla_tracks]}
 report['objects'].append(d)
for a in bpy.data.actions: report['actions'].append({'name':a.name,'range':list(a.frame_range)})
for m in bpy.data.materials:
 report['materials'].append({'name':m.name,'nodes':[(n.type,n.name) for n in m.node_tree.nodes]})
for im in bpy.data.images:
 if im.type=='IMAGE': report.setdefault('images',[]).append({'name':im.name,'size':list(im.size)})
(root/'ArtSource/BoxJellyReplacement/inspection.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/BoxJellyReplacement/BoxJelly.blend'))
print('INSPECTED_BOX_JELLY',json.dumps(report))
