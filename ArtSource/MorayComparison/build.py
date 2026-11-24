import bpy,bmesh,numpy as np,math,heapq,sys,json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/MorayComparison';assets=root/'Assets/UnderwaterArt/MorayComparison';out.mkdir(exist_ok=True);assets.mkdir(exist_ok=True)
reports=[]
for kind,seed in [('Straight',(.07,-.3,.018)),('Curled',(.08,.015,.20))]:
 bpy.ops.wm.open_mainfile(filepath=str(out/'Imported.blend'));o=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000008);bm.verts.ensure_lookup_table();seedVertex=min(bm.verts,key=lambda v:(v.co-Vector(seed)).length_squared);seen={seedVertex};todo=[seedVertex]
 while todo:
  vert=todo.pop()
  for edge in vert.link_edges:
   other=edge.other_vert(vert)
   if other not in seen:seen.add(other);todo.append(other)
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if v not in seen],context='VERTS');bm.to_mesh(o.data);bm.free();o.name=kind+'_Full';o.data.update()
 if len(o.data.polygons)<30000:raise RuntimeError('Extraction too small '+kind+str(len(o.data.polygons)))
 v=np.array([v.co[:] for v in o.data.vertices]);print('EXTRACTED',kind,len(v),len(o.data.polygons),v.min(0),v.max(0),flush=True)
 # Geodesic coordinates follow the curved body rather than flattening its authored pose.
 edges=np.array([e.vertices[:] for e in o.data.edges]);adj=[[] for _ in v]
 for a,b in edges:
  d=float(np.linalg.norm(v[a]-v[b]));adj[a].append((int(b),d));adj[b].append((int(a),d))
 head=int(v[:,1].argmin() if kind=='Straight' else v[:,1].argmax());dist=np.full(len(v),np.inf);dist[head]=0;queue=[(0.,head)]
 while queue:
  d,a=heapq.heappop(queue)
  if d>dist[a]+1e-10:continue
  for b,w in adj[a]:
   nd=d+w
   if nd<dist[b]:dist[b]=nd;heapq.heappush(queue,(nd,b))
 if not np.isfinite(dist).all():raise RuntimeError('Disconnected body')
 end=np.quantile(dist,.999);u=np.clip(dist/end,0,1);segments=22;centers=[]
 for j in range(segments+1):
  t=j/segments;mask=abs(u-t)<(.5/segments if j not in [0,segments] else .8/segments)
  centers.append(np.median(v[mask],axis=0))
 centers=np.array(centers)
 for _ in range(3):centers[1:-1]=(centers[:-2]+2*centers[1:-1]+centers[2:])/4
 # Canonical one-metre body length, keeping the curled specimen's original shape.
 length=np.linalg.norm(np.diff(centers,axis=0),axis=1).sum();origin=centers[0].copy();v=(v-origin)/length;centers=(centers-origin)/length;o.data.vertices.foreach_set('co',v.ravel());o.data.update()
 for p in o.data.polygons:p.use_smooth=True
 # Preserve full supplied color and normal atlases, shared by all four variants.
 for image in bpy.data.images:
  tag='Albedo' if image.name.startswith('Color_') else 'Normal' if image.name.startswith('NormalGL_') else 'ORM'
  image.filepath_raw=str(assets/(tag+'.png'));image.file_format='PNG';image.save()
 mat=o.data.materials[0];mat.name='MorayOriginalSurface';bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.48
 bpy.ops.object.armature_add();rig=bpy.context.object;rig.name=kind+'_SwimRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
 for j in range(segments):
  b=rig.data.edit_bones.new('Spine_%02d'%j);b.head=centers[j];b.tail=centers[j+1]
  if j:b.parent=rig.data.edit_bones['Spine_%02d'%(j-1)];b.use_connect=True
 bpy.ops.object.mode_set(mode='OBJECT')
 # Smooth skin weights by closest centerline segment; avoids weighting nearby curled sections together.
 centersKD=KDTree(len(v))
 # Geodesic u gives stable weights even where the curled tail is close to the head.
 for j in range(segments):o.vertex_groups.new(name='Spine_%02d'%j)
 for i,t in enumerate(u):
  f=max(0,min(segments-1,float(t)*segments-.5));j=int(f);a=f-j;o.vertex_groups[j].add([i],1-a,'REPLACE')
  if a>0:o.vertex_groups[min(j+1,segments-1)].add([i],a,'REPLACE')
 mod=o.modifiers.new('Full body eel deformation','ARMATURE');mod.object=rig;o.parent=rig
 # Axes are converted from a common world direction into each bone's local space.
 for frame in range(1,122,3):
  phase=(frame-1)/120*math.tau
  for j in range(segments):
   pb=rig.pose.bones['Spine_%02d'%j];t=j/(segments-1);axis=rig.data.bones[pb.name].matrix_local.to_3x3().inverted()@Vector((.55,0,1)).normalized();angle=(.025+.115*t)*math.sin(phase-t*math.tau*1.15)
   pb.rotation_mode='AXIS_ANGLE';pb.rotation_axis_angle=(angle,*axis);pb.keyframe_insert(data_path='rotation_axis_angle',frame=frame,group=pb.name)
 rig.animation_data.action.name='FullBodySwim';s=bpy.context.scene;s.frame_start=1;s.frame_end=121;s.render.fps=30;s.frame_set(1)
 # Make the neutral rest posture available without changing the original surface.
 fulltris=sum(len(p.vertices)-2 for p in o.data.polygons)
 low=o.copy();low.data=o.data.copy();bpy.context.collection.objects.link(low);low.name=kind+'_Reduced';bpy.context.view_layer.objects.active=low
 dec=low.modifiers.new('Silhouette and UV preserving reduction','DECIMATE');dec.ratio=min(1,24000/fulltris);bpy.ops.object.modifier_move_up(modifier=dec.name);bpy.ops.object.modifier_apply(modifier=dec.name)
 lowtris=sum(len(p.vertices)-2 for p in low.data.polygons)
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(kind+'.blend')))
 for obj,variant in [(o,'Full'),(low,'Reduced')]:
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
  sys.argv=['export','--','--output',str(assets/(kind+'_'+variant+'.fbx')),'--max-triangles',str(fulltris+10),'--max-bones','24','--max-texture','4096','--require-animation']
  exec(compile(Path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py').read_text(),'export','exec'))
 reports.append(dict(kind=kind,full=fulltris,reduced=lowtris,bones=segments,bodyLengthMetres=1,sourceLength=float(length),centerline=centers.tolist()))
 (out/'Geometry.json').write_text(json.dumps(reports,indent=2));print('MORAY_READY',kind,fulltris,lowtris,flush=True)

