import bpy,numpy as np,json
from pathlib import Path
bpy.ops.wm.open_mainfile(filepath=r'D:/GIT Project/Underwater-VR/ArtSource/CombJellyComparison/Imported.blend')
o=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
v=np.array([v.co[:] for v in o.data.vertices]);print('BOUNDS',v.min(0),v.max(0));print('QUANTILES',np.quantile(v,[0,.1,.25,.5,.75,.9,1],axis=0))
# Render supplied collection to identify screenshot specimen.
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=8;s.render.resolution_x=1000;s.render.resolution_y=700;s.render.resolution_percentage=100
s.world=bpy.data.worlds.new('Preview');s.world.color=(.07,.07,.07)
bpy.ops.object.camera_add(location=(2,0,0));c=bpy.context.object;c.rotation_euler=((__import__('mathutils').Vector((0,0,0))-c.location).to_track_quat('-Z','Y').to_euler());c.data.type='ORTHO';c.data.ortho_scale=1.15;s.camera=c
bpy.ops.object.light_add(type='AREA',location=(1,0,1));bpy.context.object.data.energy=100;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=3
s.render.filepath=r'D:/GIT Project/Underwater-VR/ArtSource/CombJellyComparison/Source.png';bpy.ops.render.render(write_still=True)

