import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(root/'Tools/CoastRock/CoastRock.gltf'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
if len(objects)>1:bpy.ops.object.join()
obj=bpy.context.object;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
lo=Vector([min(v.co[i] for v in obj.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in obj.data.vertices) for i in range(3)])
print('Coast scan bounds',list(lo),list(hi),'polygons',len(obj.data.polygons))
center=(lo+hi)*.5
for v in obj.data.vertices:v.co-=center
mod=obj.modifiers.new('Game resolution','DECIMATE');mod.ratio=min(1,45000/len(obj.data.polygons));bpy.ops.object.modifier_apply(modifier=mod.name)
for p in obj.data.polygons:p.use_smooth=True
obj.name='CoastRockScan'
bpy.ops.export_scene.fbx(filepath=str(root/'Assets/UnderwaterArt/Scans/CoastRock.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
