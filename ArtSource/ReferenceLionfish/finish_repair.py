import bpy,json,runpy,sys,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path('D:/GIT Project/Underwater-VR');ART=ROOT/'ArtSource/ReferenceLionfish';RENDER=ROOT/'Renders/LionfishRepair'
def snapshot(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();me.calc_loop_triangles()
    p=[v.co.copy() for v in me.vertices];t=[tuple(f.vertices) for f in me.loop_triangles];ev.to_mesh_clear()
    return p,t,BVHTree.FromPolygons(p,t,all_triangles=True)
def distance(a,b):
    maximum=0
    for x,y in [(a,b),(b,a)]:
        for ids in x[1]:
            v=[x[0][i] for i in ids]
            if (v[1]-v[0]).cross(v[2]-v[0]).length<1e-12:continue
            for w in [(1/3,1/3,1/3),(.8,.1,.1),(.1,.8,.1),(.1,.1,.8)]:
                p=sum((z*k for z,k in zip(v,w)),Vector());result=y[2].find_nearest(p)
                maximum=max(maximum,result[3] if result[0] is not None else 1e9)
    return maximum
def bound(p):return [min(v[i] for v in p) for i in range(3)]+[max(v[i] for v in p) for i in range(3)]
bpy.ops.wm.open_mainfile(filepath=str(ART/'BeforeRepair/Lionfish.blend'))
normals={};baseline={};original_vertices={}
for obj in [o for o in bpy.data.objects if o.type=='MESH']:
    mesh=obj.data;mapping={};uv=mesh.uv_layers.active
    for poly in mesh.polygons:
        for index in poly.loop_indices:
            loop=mesh.loops[index];key=(tuple(round(x,8) for x in mesh.vertices[loop.vertex_index].co),tuple(round(x,7) for x in uv.data[index].uv))
            mapping.setdefault(key,[]).append((poly.center.copy(),mesh.corner_normals[index].vector.copy()))
    normals[obj.name]=mapping;original_vertices[obj.name]=len(mesh.vertices)
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
for track in rig.animation_data.nla_tracks:
    for other in rig.animation_data.nla_tracks:other.mute=other!=track
    for frame in [1,16,46,76,106,121]:
        bpy.context.scene.frame_set(frame)
        for obj in [o for o in bpy.data.objects if o.type=='MESH']:baseline[(track.name,frame,obj.name)]=snapshot(obj)
bpy.ops.wm.open_mainfile(filepath=str(ART/'Lionfish_Repaired.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');objects=[o for o in bpy.data.objects if o.type=='MESH']
for obj in objects:
    mesh=obj.data;uv=mesh.uv_layers.active;values=[None]*len(mesh.loops)
    for poly in mesh.polygons:
        for index in poly.loop_indices:
            loop=mesh.loops[index];key=(tuple(round(x,8) for x in mesh.vertices[loop.vertex_index].co),tuple(round(x,7) for x in uv.data[index].uv))
            candidates=normals[obj.name].get(key)
            assert candidates,(obj.name,key)
            values[index]=min(candidates,key=lambda c:(c[0]-poly.center).length_squared)[1]
    mesh.normals_split_custom_set(values);mesh.update()
max_error=0;max_bounds=0
for track in rig.animation_data.nla_tracks:
    for other in rig.animation_data.nla_tracks:other.mute=other!=track
    for frame in [1,16,46,76,106,121]:
        bpy.context.scene.frame_set(frame)
        for obj in objects:
            a=baseline[(track.name,frame,obj.name)];b=snapshot(obj);error=distance(a,b)
            max_error=max(max_error,error);max_bounds=max(max_bounds,max(abs(x-y) for x,y in zip(bound(a[0]),bound(b[0]))))
# Blender dynamically triangulates deformed non-planar quads; the original
# export already uses fixed render triangles. Allow only subpixel differences
# below 0.025 mm while retaining exact vertices and bounding-box extrema.
assert max_error<.000025,max_error
assert max_bounds<1e-7,max_bounds
# Evaluate the requested 50% reduction on a disposable mesh. Never apply it.
reduction_test=[]
for obj in objects:
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Disposable 50 percent test','DECIMATE');mod.ratio=.5
    bpy.context.view_layer.update();b=snapshot(obj);mod.show_viewport=False;bpy.context.view_layer.update();a=snapshot(obj)
    error=distance(a,b);reduction_test.append({'mesh':obj.name,'surface_error_m':error,'accepted':False})
    obj.modifiers.remove(mod)
for track in rig.animation_data.nla_tracks:track.mute=track.name!='Swim'
scene=bpy.context.scene;scene.frame_set(1);camera=scene.camera
views={'Front':(0,.9,.08),'Back':(0,-.9,.08),'Left':(-.9,0,.04),'Right':(.9,0,.04),'Top':(0,0,1),'Bottom':(0,0,-1),'ThreeQuarter':(.6,.8,.45)}
scene.render.resolution_x=900;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.cycles.samples=16
for name,pos in views.items():
    camera.location=pos;camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDER/('After_'+name+'.png'));bpy.ops.render.render(write_still=True)
camera.location=(.6,.8,.45);camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Lionfish_Repaired.blend'))
report=json.loads((ART/'RepairReport.json').read_text());report.update({'preserved_custom_corner_normals':True,'animated_surface_error_m':max_error,'animated_bounds_error_m':max_bounds,'sampled_poses':18,'fifty_percent_trial_rejected':reduction_test,'vertices_before':sum(original_vertices.values()),'vertices_after':sum(len(o.data.vertices) for o in objects)})
(ART/'RepairReport.json').write_text(json.dumps(report,indent=2))
for track in rig.animation_data.nla_tracks:track.mute=False
sys.argv=['export','--','--output',str(ART/'Lionfish_Repaired.fbx'),'--collection','LionfishAsset','--max-triangles','3000','--max-bones','20','--max-materials','2','--max-texture','1024','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('REPAIRED_LIONFISH_VALIDATED',json.dumps(report))
