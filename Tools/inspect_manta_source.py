import bpy,math
from pathlib import Path
from mathutils import Vector
r=Path('C:/XR Horizon/Projects/Under Water')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(r/'Assets/UnderwaterArt/Scans/GiantManta.glb'))
for o in bpy.context.scene.objects:
 if o.type=='MESH': print('PART',o.name,len(o.data.vertices),tuple(round(x,3) for x in o.dimensions), 'CENTER',tuple(round(x,3) for x in o.location))
bpy.ops.wm.save_as_mainfile(filepath=str(r/'Tools/MantaSource.blend'))
bpy.ops.object.camera_add(location=(4,-5,4));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=6;bpy.context.scene.camera=camera
for pos,energy in [((2,-3,5),800),((-3,2,3),700)]:
 bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=5;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.render.resolution_x=1100;s.render.resolution_y=850;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Studio');s.world.color=(.2,.2,.2);s.render.filepath=str(r/'Renders/Manta_Source_Check.png');bpy.ops.render.render(write_still=True)
