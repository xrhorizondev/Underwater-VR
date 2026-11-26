import bpy,math,json
from mathutils import Vector
from pathlib import Path
R=Path('D:/GIT Project/Underwater-VR');O=R/'Renders/SquidNew';O.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'ArtSource/SquidNewInspection.blend'))
o=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;o.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
print('WORLD_BOUNDS',[list(v) for v in o.bound_box],flush=True)
m=o.modifiers.new('Inspection reduction','DECIMATE');m.ratio=100000/4921252;bpy.ops.object.modifier_apply(modifier=m.name)
bpy.ops.wm.save_as_mainfile(filepath=str(R/'ArtSource/SquidNewInspectionReduced.blend'))
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=12;s.cycles.use_denoising=True;s.render.resolution_x=900;s.render.resolution_y=800;s.render.resolution_percentage=100
s.world=bpy.data.worlds.new('Studio');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.03,.05,.07,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.6
for pos,power in [((1,-2,3),220),((-2,1,1),160)]:
 d=bpy.data.lights.new('Softbox','AREA');d.energy=power;d.size=3;l=bpy.data.objects.new('Softbox',d);s.collection.objects.link(l);l.location=pos;l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO';d.ortho_scale=1.2
for name,pos in [('Front',(0,-3,0)),('Side',(3,0,0)),('Quarter',(2,-3,1))]:
 c.location=pos;c.rotation_euler=(-c.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(O/(name+'.png'));bpy.ops.render.render(write_still=True)
