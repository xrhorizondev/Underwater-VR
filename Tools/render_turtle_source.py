import bpy,math
from mathutils import Vector
from pathlib import Path
r=Path('C:/XR Horizon/Projects/Under Water')
bpy.ops.wm.open_mainfile(filepath=str(r/'Tools/TurtleSource.blend'))
for o in bpy.context.scene.objects:
 if o.type=='MESH' and ('iris' in o.name or 'pupil' in o.name):
  print('EYE_CENTER',o.name,list(sum((o.matrix_world@v.co for v in o.data.vertices),Vector())/len(o.data.vertices)))
bpy.ops.object.camera_add(location=(1.7,-1.8,1.3));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,.05))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.9;bpy.context.scene.camera=camera
for pos,energy,size in [((1,-2,3),400,4),((-2,1,2),250,3)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.render.resolution_x=900;s.render.resolution_y=700;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Studio');s.world.color=(.15,.15,.15);s.render.filepath=str(r/'Renders/Turtle_Source_Check.png');bpy.ops.render.render(write_still=True)
