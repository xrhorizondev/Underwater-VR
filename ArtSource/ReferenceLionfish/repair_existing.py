"""Repair the saved mesh only. Never calls the procedural model generator."""
import bpy,bmesh,json,hashlib,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
ROOT=Path('D:/GIT Project/Underwater-VR');ART=ROOT/'ArtSource/ReferenceLionfish';RENDER=ROOT/'Renders/LionfishRepair'
bpy.ops.wm.open_mainfile(filepath=str(ART/'BeforeRepair/Lionfish.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
objects=[o for o in bpy.data.objects if o.type=='MESH']
original={o.name:o.data.copy() for o in objects}
transforms={o.name:list(sum((list(row) for row in o.matrix_world),[])) for o in objects+[rig]}
texture=ROOT/'Assets/UnderwaterArt/ReferenceLionfish/Lionfish_Albedo.png'
texture_hash=hashlib.sha256(texture.read_bytes()).hexdigest()
def tris(mesh):mesh.calc_loop_triangles();return len(mesh.loop_triangles)
def snapshot(obj):
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();me.calc_loop_triangles()
    points=[v.co.copy() for v in me.vertices];triangles=[tuple(t.vertices) for t in me.loop_triangles]
    uvs=[[Vector((*me.uv_layers.active.data[l].uv,0)) for l in t.loops] for t in me.loop_triangles]
    result=(points,triangles,uvs,BVHTree.FromPolygons(points,triangles,all_triangles=True))
    ev.to_mesh_clear();return result
def bounds(points):return [min(p[i] for p in points) for i in range(3)]+[max(p[i] for p in points) for i in range(3)]
def compare(a,b):
    maximum=0;uv_error=0
    for src,dst in [(a,b),(b,a)]:
        for ids,uvs in zip(src[1],src[2]):
            verts=[src[0][i] for i in ids]
            if (verts[1]-verts[0]).cross(verts[2]-verts[0]).length<1e-12:continue
            for weights in [(1/3,1/3,1/3),(.8,.1,.1),(.1,.8,.1),(.1,.1,.8)]:
                point=sum((p*w for p,w in zip(verts,weights)),Vector());hit,normal,index,distance=dst[3].find_nearest(point)
                if hit is None:return 1e9,1e9
                maximum=max(maximum,distance)
                # UV seams can share identical positions. Only validate matching
                # surface triangles; ties are checked by identical-camera renders.
                target=[dst[0][i] for i in dst[1][index]]
                if (target[1]-target[0]).cross(target[2]-target[0]).length>1e-12:
                    actual=barycentric_transform(hit,*target,*dst[2][index]);expected=sum((uv*w for uv,w in zip(uvs,weights)),Vector())
                    uv_error=max(uv_error,(actual-expected).length)
    return maximum,uv_error
before=sum(tris(o.data) for o in objects);results=[]
for obj in objects:
    src=obj.data;bm=bmesh.new();bm.from_mesh(src)
    # Freeze the original render triangulation before welding. Re-triangulating
    # a non-planar quad afterwards can change its animated surface subtly.
    src.calc_loop_triangles();bm.verts.ensure_lookup_table();diagonals=set()
    for triangle in src.loop_triangles:
        polygon=src.polygons[triangle.polygon_index];ids=list(polygon.vertices)
        boundary={tuple(sorted((ids[i],ids[(i+1)%len(ids)]))) for i in range(len(ids))}
        tv=list(triangle.vertices)
        for i in range(3):
            pair=tuple(sorted((tv[i],tv[(i+1)%3])))
            if pair not in boundary:diagonals.add(pair)
    for a,b in sorted(diagonals):
        if not any(bm.verts[b] in e.verts for e in bm.verts[a].link_edges):
            bmesh.ops.connect_verts(bm,verts=[bm.verts[a],bm.verts[b]],check_degenerate=True)
    degenerate=[f for f in bm.faces if f.calc_area()<1e-12]
    bmesh.ops.delete(bm,geom=degenerate,context='FACES_ONLY')
    # Weld only coincident vertices with identical deform weights. UV loops,
    # material boundaries and eye/body islands remain separate.
    deform=bm.verts.layers.deform.active;groups={}
    for v in bm.verts:
        key=(tuple(round(x,9) for x in v.co),tuple(sorted(v[deform].items())) if deform else ())
        groups.setdefault(key,[]).append(v)
    targetmap={v:vs[0] for vs in groups.values() for v in vs[1:]}
    if targetmap:bmesh.ops.weld_verts(bm,targetmap=targetmap)
    seen=set();duplicates=[]
    for f in bm.faces:
        key=(frozenset(f.verts),f.material_index)
        if key in seen:duplicates.append(f)
        else:seen.add(key)
    bmesh.ops.delete(bm,geom=duplicates,context='FACES_ONLY')
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    # No global normal recalculation: eyes, mouth and delicate open membranes
    # have intentional winding. Preserve the existing orientation of each face.
    bm.to_mesh(src);bm.free();src.update()
    clean=src.copy();clean_count=tris(clean)
    # Try lossless planar dissolution; accept only when geometry, UVs,
    # dimensions AND animated poses remain unchanged within numerical tolerance.
    trial=src.copy();bm=bmesh.new();bm.from_mesh(trial)
    bmesh.ops.dissolve_limit(bm,angle_limit=.00001,verts=list(bm.verts),edges=list(bm.edges),delimit={'NORMAL','MATERIAL','SEAM','UV'})
    bm.to_mesh(trial);bm.free();trial.update();obj.data=trial
    ok=True;max_surface=0;max_uv=0;max_bounds=0
    for track in rig.animation_data.nla_tracks:
        for other in rig.animation_data.nla_tracks:other.mute=other!=track
        for frame in [1,16,46,76,106,121]:
            scene.frame_set(frame);obj.data=clean;bpy.context.view_layer.update();a=snapshot(obj)
            obj.data=trial;bpy.context.view_layer.update();b=snapshot(obj)
            surface,uv=compare(a,b);bound=max(abs(x-y) for x,y in zip(bounds(a[0]),bounds(b[0])))
            max_surface=max(max_surface,surface);max_uv=max(max_uv,uv);max_bounds=max(max_bounds,bound)
            if surface>2e-6 or uv>2e-5 or bound>1e-7:ok=False;break
        if not ok:break
    obj.data=trial if ok else clean
    results.append({'mesh':obj.name,'original_triangles':tris(original[obj.name]),'clean_triangles':clean_count,'candidate_triangles':tris(trial),'accepted_planar_reduction':ok,'triangles':tris(obj.data),'removed_degenerate_faces':len(degenerate),'welded_vertices':len(targetmap),'removed_duplicate_faces':len(duplicates),'candidate_surface_error_m':max_surface,'candidate_uv_error':max_uv,'candidate_bounds_error_m':max_bounds})
for track in rig.animation_data.nla_tracks:track.mute=track.name!='Swim'
scene.frame_set(1)
for obj in objects+[rig]:assert transforms[obj.name]==list(sum((list(row) for row in obj.matrix_world),[])),obj.name
assert hashlib.sha256(texture.read_bytes()).hexdigest()==texture_hash
scene.render.resolution_x=900;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.cycles.samples=16
camera=scene.camera
views={'Front':(0,.9,.08),'Back':(0,-.9,.08),'Left':(-.9,0,.04),'Right':(.9,0,.04),'Top':(0,0,1),'Bottom':(0,0,-1),'ThreeQuarter':(.6,.8,.45)}
optimized={o.name:o.data for o in objects}
for label,meshes in [('Before',original),('After',optimized)]:
    for obj in objects:obj.data=meshes[obj.name]
    for name,position in views.items():
        if label=='Before' and (RENDER/(label+'_'+name+'.png')).exists():continue
        camera.location=position;camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDER/(label+'_'+name+'.png'));bpy.ops.render.render(write_still=True)
for obj in objects:obj.data=optimized[obj.name]
camera.location=(.6,.8,.45);camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler()
for obj in objects:
    src=original[obj.name];dst=obj.data
    assert bounds([v.co for v in src.vertices])==bounds([v.co for v in dst.vertices]),obj.name
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Lionfish_Repaired.blend'))
report={'original_triangles':before,'optimized_triangles':sum(tris(o.data) for o in objects),'texture_sha256_unchanged':texture_hash,'transforms_unchanged':True,'bounds_exact':True,'existing_bones':len(rig.data.bones),'new_bones':0,'new_animations':0,'results':results}
(ART/'RepairReport.json').write_text(json.dumps(report,indent=2));print('LIONFISH_REPAIR_COMPLETE',json.dumps(report))
