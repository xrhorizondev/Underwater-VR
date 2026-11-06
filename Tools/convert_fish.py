import bpy, math
from pathlib import Path
from mathutils import Vector, Matrix
root=Path(__file__).resolve().parents[1];folder=root/'Assets/UnderwaterArt/Scans'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(folder/'BarramundiFish.glb'))
objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in objs:o.select_set(True)
bpy.context.view_layer.objects.active=objs[0]
if len(objs)>1:bpy.ops.object.join()
obj=bpy.context.object;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for mat in obj.data.materials:
    if not mat or not mat.use_nodes:continue
    bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if not bs:continue
    for channel,key in [('Base Color','FishScan_albedo'),('Normal','FishScan_normal')]:
        socket=bs.inputs[channel]
        if not socket.is_linked:continue
        node=socket.links[0].from_node
        if node.type=='NORMAL_MAP':node=node.inputs['Color'].links[0].from_node
        if node.type=='TEX_IMAGE':
            im=node.image;im.filepath_raw=str(root/'Assets/UnderwaterArt/Textures'/(key+'.png'));im.file_format='PNG';im.save();print(key,im.size[:])
coords=[v.co for v in obj.data.vertices];lo=Vector([min(v[i] for v in coords) for i in range(3)]);hi=Vector([max(v[i] for v in coords) for i in range(3)])
print('Fish bounds',list(lo),list(hi))
center=(lo+hi)*.5;size=max(hi-lo);rot=Matrix.Rotation(-math.pi/2,4,'Z') if hi.x-lo.x>hi.y-lo.y else Matrix.Identity(4)
for v in obj.data.vertices:v.co=rot@((v.co-center)/size)
for p in obj.data.polygons:p.use_smooth=True
# Export a static mesh: orphaned skin weights produce invalid Unity bounds.
obj.vertex_groups.clear()
for modifier in list(obj.modifiers):obj.modifiers.remove(modifier)
obj.name='PhotographicFish'
bpy.ops.export_scene.fbx(filepath=str(folder/'PhotographicFish.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)

