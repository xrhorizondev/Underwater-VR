import bpy,json,sys,runpy
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR'); src=root/'ArtSource/SuppliedSharkDolphin'
reports=[]
for kind in ['Shark','Dolphin']:
 bpy.ops.wm.open_mainfile(filepath=str(src/f'{kind}_Original.blend'))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH' and (o.name.startswith('Cube') or kind=='Shark' and o.name in ['Low-Poly-white-shark_body','white-shark_eyes.002']):bpy.data.objects.remove(o,do_unlink=True)
 action=next(a for a in bpy.data.actions if ('swimming_skeletal' in a.name if kind=='Shark' else a.name.startswith('Armature')))
 rig.animation_data.action=action
 for track in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(track)
 bpy.context.scene.frame_start=int(action.frame_range[0]);bpy.context.scene.frame_end=int(action.frame_range[1]);bpy.context.scene.frame_set(bpy.context.scene.frame_start)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];before=after=0
 for o in meshes:
  o.data.calc_loop_triangles();before+=len(o.data.loop_triangles)
  if ('body' in o.name.lower() or kind=='Dolphin' and o.name=='untitled1.002'):
   bpy.context.view_layer.objects.active=o
   dec=o.modifiers.new('Conservative 15 percent reduction','DECIMATE');dec.ratio=.85;dec.use_collapse_triangulate=True
   while o.modifiers[0] != dec: bpy.ops.object.modifier_move_up(modifier=dec.name)
   bpy.ops.object.modifier_apply(modifier=dec.name)
  o.data.calc_loop_triangles();after+=len(o.data.loop_triangles)
  for poly in o.data.polygons:poly.use_smooth=True
  for m in o.data.materials:
   if m and m.use_nodes:
    for n in m.node_tree.nodes:
     if n.type=='TEX_IMAGE':n.image=None
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.wm.save_as_mainfile(filepath=str(src/f'{kind}_Reduced.blend'))
 out=root/'Assets/UnderwaterArt/SuppliedSharkDolphin'/kind/f'{kind}.fbx'
 sys.argv=['export','--','--output',str(out),'--max-triangles','400000','--max-bones','100','--max-materials','8','--require-animation']
 runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
 reports.append({'kind':kind,'before':before,'after':after,'reductionPercent':100*(1-after/before),'clip':action.name,'frames':list(action.frame_range)})
(src/'reduction.json').write_text(json.dumps(reports,indent=2))

