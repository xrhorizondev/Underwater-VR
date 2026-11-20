import bpy
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR');bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/TripoMoray/Imported.blend'))
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.render.resolution_x=1200;s.render.resolution_y=1000;s.render.resolution_percentage=100
w=bpy.data.worlds.new('Raw head');s.world=w;w.use_nodes=True;w.node_tree.nodes['Background'].inputs[0].default_value=(.04,.05,.06,1);w.node_tree.nodes['Background'].inputs[1].default_value=.4
center=Vector((-.17,-.435,.025))
for name,pos in [('Key',(.4,-.4,.8)),('Fill',(-.4,-.5,.3))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=5;d.size=.5;o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.location=pos;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO';d.ortho_scale=.21
for label,offset in [('RawSide',(.6,0,0)),('RawOpposite',(-.6,0,0)),('RawFront',(0,-.6,0))]:
 c.location=center+Vector(offset);c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(root/'Renders/TripoMoray'/f'{label}.png');bpy.ops.render.render(write_still=True)
