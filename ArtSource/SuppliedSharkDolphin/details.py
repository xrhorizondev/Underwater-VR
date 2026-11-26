import bpy,json
from pathlib import Path
r=Path('D:/GIT Project/Underwater-VR/ArtSource/SuppliedSharkDolphin')
out=[]
for kind in ['Shark','Dolphin']:
 bpy.ops.wm.open_mainfile(filepath=str(r/f'{kind}_Original.blend'))
 rows=[]
 for o in bpy.context.scene.objects:
  if o.type=='ARMATURE' or o.type=='MESH' and not o.name.startswith('Cube'):
   rows.append(dict(name=o.name,type=o.type,parent=o.parent.name if o.parent else None,mods=[(m.type,m.object.name if m.type=='ARMATURE' and m.object else '') for m in o.modifiers],mats=[m.name for m in o.data.materials] if o.type=='MESH' else [],action=o.animation_data.action.name if o.animation_data and o.animation_data.action else None))
 out.append({'kind':kind,'objects':rows})
(r/'details.json').write_text(json.dumps(out,indent=2))
