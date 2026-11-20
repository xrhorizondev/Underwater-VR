import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/TripoMoray';out.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='C:/Users/mobin/Downloads/tripo_pbr_model_60e08e23-d8e9-4584-969a-cd2d402f57c5_meshopt.fbx')
report={'objects':[],'actions':[],'images':[]}
for obj in bpy.context.scene.objects:
    item={'name':obj.name,'type':obj.type,'bounds':[list(obj.matrix_world@Vector(c)) for c in obj.bound_box]}
    if obj.type=='MESH':item.update(vertices=len(obj.data.vertices),triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),materials=[m.name for m in obj.data.materials],groups=[g.name for g in obj.vertex_groups],modifiers=[m.type for m in obj.modifiers])
    if obj.type=='ARMATURE':item['bones']=[b.name for b in obj.data.bones]
    report['objects'].append(item)
for action in bpy.data.actions:report['actions'].append({'name':action.name,'range':list(action.frame_range)})
for image in bpy.data.images:
    report['images'].append({'name':image.name,'size':list(image.size),'path':image.filepath,'packed':bool(image.packed_file)})
    if image.has_data:
        image.filepath_raw=str(out/(image.name.split('.')[0]+'.png'));image.file_format='PNG';image.save()
(out/'Inspection.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Imported.blend'))
print(json.dumps(report))
# A neutral studio view of the exact supplied mesh before any edits.
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];center=sum(points,Vector())/len(points);size=max((p-center).length for p in points)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=1100;scene.render.resolution_y=750;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('Studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.15,.18,1);world.node_tree.nodes['Background'].inputs[1].default_value=.6
for name,offset,power in [('Key',(1,-2,3),900),('Fill',(-2,1,1),600)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power*max(1,size*size);data.shape='DISK';data.size=size*3;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=center+Vector(offset)*size;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',data);scene.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO';data.ortho_scale=size*2.3;camera.location=center+Vector((2,-3,1.5))*size;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
renders=root/'Renders/TripoMoray';renders.mkdir(exist_ok=True);scene.render.filepath=str(renders/'Original.png');bpy.ops.render.render(write_still=True)
