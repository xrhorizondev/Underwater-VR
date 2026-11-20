import bpy,bmesh,json,collections
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/OctopusRepair';out.mkdir(exist_ok=True)
reports=[]
for mode in ['source','prepared']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if mode=='source':bpy.ops.import_scene.gltf(filepath=str(out/'Source.glb'))
 else:bpy.ops.wm.open_mainfile(filepath=str(root/'.utmp/ModelPrep/ready/Octopus/Octopus.blend'))
 obj=max([o for o in bpy.data.objects if o.type=='MESH'],key=lambda o:len(o.data.vertices));mesh=obj.data
 bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table();unseen=set(bm.verts);components=[]
 while unseen:
  v=unseen.pop();group=[v];queue=[v]
  while queue:
   for e in queue.pop().link_edges:
    n=e.other_vert(v) if False else None
    for n in e.verts:
     if n in unseen:unseen.remove(n);group.append(n);queue.append(n)
  faces=set(f for v in group for f in v.link_faces);components.append({'vertices':len(group),'triangles':sum(len(f.verts)-2 for f in faces),'boundary':sum(1 for e in set(e for v in group for e in v.link_edges) if e.is_boundary),'center':list(sum((v.co for v in group),group[0].co*0)/len(group))})
 report={'mode':mode,'name':obj.name,'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),'shapeKeys':[k.name for k in mesh.shape_keys.key_blocks] if mesh.shape_keys else [],'components':sorted(components,key=lambda c:-c['triangles']),'transforms':str(obj.matrix_world),'actions':[a.name for a in bpy.data.actions]};reports.append(report)
 print(mode,'triangles',report['triangles'],'component sizes',collections.Counter(c['triangles'] for c in components),'largest',report['components'][:12],flush=True);bm.free()
(out/'SourceAudit.json').write_text(json.dumps(reports,indent=2))
