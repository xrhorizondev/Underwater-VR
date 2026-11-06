import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
root=Path(__file__).resolve().parents[1]
folder=root/'Assets/UnderwaterArt/Scans'
name=sys.argv[sys.argv.index('--')+1]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(folder/(name+'.glb')))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.object.join();obj=bpy.context.object
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
doc=json.loads((root/('Tools/'+name+'-scene.json')).read_text())
q=doc['models'][0]['rotation'];b=Matrix.Rotation(math.pi/2,4,'X');rot=b@Quaternion((q[3],q[0],q[1],q[2])).to_matrix().to_4x4()@b.inverted()
for v in obj.data.vertices:v.co=rot@v.co
coords=[v.co for v in obj.data.vertices];lo=Vector([min(v[i] for v in coords) for i in range(3)]);hi=Vector([max(v[i] for v in coords) for i in range(3)])
offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));size=max(hi-lo)
for v in obj.data.vertices:v.co=(v.co-offset)/size
# Remove the flat museum mounting plinth below the coral tissue.
import bmesh
bm=bmesh.new();bm.from_mesh(obj.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=0.00001,plane_co=(0,0,0.14),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
for v in bm.verts:v.co.z-=0.14
bm.to_mesh(obj.data);bm.free()
for im in bpy.data.images:
    if im.type=='IMAGE' and im.size[0]>0:
        im.filepath_raw=str(folder/(name+'_scan.png'));im.file_format='PNG';im.save()
        break
for p in obj.data.polygons:p.use_smooth=True
original=obj.data.copy()
for lod,ratio in [(0,.22),(1,.065),(2,.018)]:
    obj.data=original.copy();obj.name=name+'_LOD'+str(lod)
    mod=obj.modifiers.new('Preserve scanned branches','DECIMATE');mod.ratio=ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.export_scene.fbx(filepath=str(folder/(obj.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='RELATIVE')
    print(obj.name,len(obj.data.polygons))


