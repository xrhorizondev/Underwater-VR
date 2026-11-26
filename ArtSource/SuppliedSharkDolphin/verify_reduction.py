import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
root=Path('D:/GIT Project/Underwater-VR');src=root/'ArtSource/SuppliedSharkDolphin';reports=[]
for kind,body in [('Shark','white-shark_body'),('Dolphin','untitled1.002')]:
 bpy.ops.wm.open_mainfile(filepath=str(src/f'{kind}_Reduced.blend'))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');target=bpy.data.objects[body];reduced=target.data.copy()
 with bpy.data.libraries.load(str(src/f'{kind}_Original.blend'),link=False) as (data,loaded):loaded.objects=[body]
 original=loaded.objects[0].data;errors=[]
 if kind=='Dolphin': print('GROUPS_ORIGINAL',[(g.name,g.index) for g in loaded.objects[0].vertex_groups]);print('GROUPS_REDUCED',[(g.name,g.index) for g in target.vertex_groups])
 for frame in [bpy.context.scene.frame_start,9,17,25,33,41,49,bpy.context.scene.frame_end]:
  bpy.context.scene.frame_set(frame);dg=bpy.context.evaluated_depsgraph_get()
  target.data=reduced;bpy.context.view_layer.update();e=target.evaluated_get(dg);m=e.to_mesh();verts=[e.matrix_world @ v.co for v in m.vertices];polys=[list(p.vertices) for p in m.polygons];tree=BVHTree.FromPolygons(verts,polys);e.to_mesh_clear()
  target.data=original;bpy.context.view_layer.update();e=target.evaluated_get(dg);m=e.to_mesh();distances=[tree.find_nearest(e.matrix_world @ v.co)[3] for v in m.vertices];errors.append({'worstVertex':distances.index(max(distances)),'frame':frame,'maxSurfaceError':max(distances),'meanSurfaceError':sum(distances)/len(distances)});e.to_mesh_clear()
 reports.append({'kind':kind,'animatedReductionError':errors})
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'Assets/UnderwaterArt/SuppliedSharkDolphin'/kind/f'{kind}.fbx'))
 rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 reports[-1]['roundtrip']={'rigs':len(rigs),'meshes':len(meshes),'actions':len(bpy.data.actions),'vertices':sum(len(o.data.vertices) for o in meshes),'unweightedVertices':sum(sum(not v.groups for v in o.data.vertices) for o in meshes)}
(root/'Renders/SuppliedSharkDolphin/reduction-validation.json').write_text(json.dumps(reports,indent=2))



