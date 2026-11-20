"""Restore original octopus skin, exact morphs and weights, including cup bases."""
import bpy,bmesh,math,json,sys,runpy,collections
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'ArtSource/OctopusRepair';OUT.mkdir(exist_ok=True);RENDERS=ROOT/'Renders/OctopusRepair';RENDERS.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'Source.glb'))
source=max([o for o in bpy.data.objects if o.type=='MESH'],key=lambda o:len(o.data.vertices));rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');data=source.data;scene=bpy.context.scene;scene.frame_set(1)
bm=bmesh.new();bm.from_mesh(data);bm.verts.ensure_lookup_table();unseen=set(bm.verts);groups=[]
while unseen:
 start=unseen.pop();component=[start];queue=[start]
 while queue:
  v=queue.pop()
  for edge in v.link_edges:
   n=edge.other_vert(v)
   if n in unseen:unseen.remove(n);component.append(n);queue.append(n)
 groups.append([v.index for v in component])
bm.free();names=[g.name for g in source.vertex_groups];keep=set();cups=collections.defaultdict(list);kept_cups=0
for component in groups:
 counts=collections.Counter()
 for index in component:
  for g in data.vertices[index].groups:counts[names[g.group]]+=g.weight
 dominant=counts.most_common(1)[0][0] if counts else ''
 # All primary surfaces and every eye/head island retain exact topology.
 if len(component)>=300 or 'Leg' not in dominant:keep.update(component);continue
 family=dominant.split('_jnt')[0];center=sum((data.vertices[i].co for i in component),Vector())/len(component)
 rootbone=next((b for b in rig.data.bones if b.name.startswith(family+'_jnt')),None);distance=(center-(rootbone.head_local if rootbone else Vector())).length
 cups[family].append((distance,component))
for family,items in cups.items():
 items.sort(key=lambda item:item[0])
 for i,(_,component) in enumerate(items):
  # Cup bases close openings in the arm skin. Keep those complete islands;
  # deleting or collapsing them exposes the inside of the arm during curls.
  keep.update(component);kept_cups+=1
polys=[p for p in data.polygons if all(i in keep for i in p.vertices)];used=sorted(set(i for p in polys for i in p.vertices));mapping={old:new for new,old in enumerate(used)}
mesh=bpy.data.meshes.new('Original octopus anatomy retained');mesh.from_pydata([data.vertices[i].co for i in used],[],[[mapping[i] for i in p.vertices] for p in polys]);mesh.update()
for layer in data.uv_layers:
 dest=mesh.uv_layers.new(name=layer.name)
 for old,new in zip(polys,mesh.polygons):
  for a,b in zip(old.loop_indices,new.loop_indices):dest.data[b].uv=layer.data[a].uv
for old,new in zip(polys,mesh.polygons):new.use_smooth=True
normals=[tuple(data.corner_normals[li].vector) for p in polys for li in p.loop_indices];mesh.normals_split_custom_set(normals)
obj=source.copy();obj.data=mesh;bpy.context.collection.objects.link(obj)
for name in names:obj.vertex_groups.new(name=name)
for old,new in mapping.items():
 for g in data.vertices[old].groups:obj.vertex_groups[g.group].add([new],g.weight,'REPLACE')
keys=data.shape_keys;action=keys.animation_data.action;slot=keys.animation_data.action_slot
for key in keys.key_blocks:
 target=obj.shape_key_add(name=key.name);target.value=key.value
 for old,new in mapping.items():target.data[new].co=key.data[old].co
obj.data.shape_keys.animation_data_create();obj.data.shape_keys.animation_data.action=action;obj.data.shape_keys.animation_data.action_slot=slot
texture=bpy.data.images.load(str(ROOT/'Assets/UnderwaterArt/PreparedMarine/Octopus/textures/Octopus_BaseColor.png'),check_existing=True)
material=bpy.data.materials.new('Octopus_Skin');material.use_nodes=True;bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.5;tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=texture;material.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);obj.data.materials.clear();obj.data.materials.append(material)
for other in list(bpy.data.objects):
 if other.type=='MESH' and other!=obj:bpy.data.objects.remove(other,do_unlink=True)
obj.name='Octopus_Mesh';rig.name='Octopus_Rig';asset=bpy.data.collections.new('OctopusAsset');scene.collection.children.link(asset)
for o in [obj,rig]:
 for c in list(o.users_collection):c.objects.unlink(o)
 asset.objects.link(o)
for layer in rig.animation_data.action.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for curve in bag.fcurves:
    if curve.data_path in ['location','rotation_quaternion','rotation_euler']:
     value=curve.evaluate(1)
     for k in curve.keyframe_points:k.co.y=value;k.handle_left.y=value;k.handle_right.y=value
for action in bpy.data.actions:
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:curve.keyframe_points.insert(301,curve.evaluate(1),options={'REPLACE'});curve.update()
scene.frame_start=1;scene.frame_end=301;scene.render.fps=24;scene.frame_set(1);bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Octopus.blend'))
# Exact retained-index correspondence is available for every base/morph vertex.
report={'sourceTriangles':58640,'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),'retainedSuckerIslands':kept_cups,'originalVerticesRetained':len(used),'primaryAnatomyDecimation':False,'morphTransferApproximation':False,'originalShapeKeys':[k.name for k in obj.data.shape_keys.key_blocks],'bones':len(rig.data.bones),'materials':1}
(OUT/'RetainedVertexIndices.json').write_text(json.dumps(used))
(OUT/'RepairReport.json').write_text(json.dumps(report,indent=2));print(json.dumps(report),flush=True)
sys.argv=['export','--','--output',str(OUT/'Octopus.fbx'),'--collection','OctopusAsset','--max-triangles','60000','--max-bones','80','--max-texture','1024','--max-materials','1','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
# Studio views use original motion, not a newly invented animation.
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('Octopus studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.025,.04,.05,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45
bpy.context.view_layer.update();points=[obj.matrix_world@Vector(c) for c in obj.bound_box];center=sum(points,Vector())/8;size=max((p-center).length for p in points)
for name,offset in [('Key',(1,-2,3)),('Fill',(-2,-1,1))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=180*size*size;d.size=size*3;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=center+Vector(offset)*size;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',d);scene.collection.objects.link(camera);scene.camera=camera;d.type='ORTHO';d.ortho_scale=size*2.3
def render(name,offset,frame):
 scene.frame_set(frame);camera.location=center+Vector(offset)*size;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDERS/(name+'.png'));bpy.ops.render.render(write_still=True)
for name,offset,frame in [('Front',(0,-3,1.5),1),('ThreeQuarter',(2,-3,2),100),('Opposite',(-2,-3,2),200),('Side',(3,0,1.5),150)]:render(name,offset,frame)
scene.render.resolution_x=640;scene.render.resolution_y=512;scene.cycles.samples=8
for i,frame in enumerate(range(1,301,15)):render('Crawl_'+str(i).zfill(2),(2,-3,2),frame)
