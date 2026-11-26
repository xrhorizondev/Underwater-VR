import bpy,json
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR')
reports=[]
for kind,path in [('Shark','Shark/source/Realistic Shark.fbx'),('Dolphin','Dolphin/source/LPP.fbx')]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(root/'ArtSource/SuppliedSharkDolphin/OriginalFBX'/Path(path).name))
 report={'kind':kind,'actions':[(a.name,a.frame_range[:]) for a in bpy.data.actions],'objects':[]}
 for o in bpy.context.scene.objects:
  if o.type=='MESH':
   o.data.calc_loop_triangles()
   report['objects'].append({'name':o.name,'triangles':len(o.data.loop_triangles),'dimensions':list(o.dimensions),'bounds':[list(v) for v in o.bound_box]})
  if o.type=='ARMATURE':report['bones']=[b.name for b in o.data.bones]
 reports.append(report)
 bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/SuppliedSharkDolphin'/f'{kind}_Original.blend'))
(root/'ArtSource/SuppliedSharkDolphin/inspection.json').write_text(json.dumps(reports,indent=2))

