import bpy, sys, runpy, json
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR')
ART=ROOT/'ArtSource/ReferenceLionfish'
OUT=ROOT/'Assets/UnderwaterArt/ReferenceLionfish/Lionfish.fbx'
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
meshes=[o for o in bpy.data.objects if o.type=='MESH']
report={}
def pose(frame):
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
    return {b.name:(b.head.copy(),b.matrix.to_quaternion().copy()) for b in rig.pose.bones}
def vertices(frame):
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();pts=[]
    for obj in meshes:
        ev=obj.evaluated_get(dg);me=ev.to_mesh();pts.extend([obj.matrix_world@v.co for v in me.vertices]);ev.to_mesh_clear()
    return pts
for track in rig.animation_data.nla_tracks:
    for other in rig.animation_data.nla_tracks:other.mute=other!=track
    a=pose(1);b=pose(121);error=max((a[n][0]-b[n][0]).length+abs(a[n][1].rotation_difference(b[n][1]).angle) for n in a)
    assert error<1e-4,(track.name,error)
    a=pose(16);b=pose(46)
    angular={n:abs(a[n][1].rotation_difference(b[n][1]).angle) for n in ['Jaw','GillL','GillR','TailTip','PectoralL2']}
    for n in ['Jaw','GillL','GillR']:assert angular[n]>.01,(track.name,n,angular)
    points=vertices(16)+vertices(46)
    assert all(all(abs(x)<2 for x in p) for p in points)
    assert (a['Root'][0]-b['Root'][0]).length<1e-7
    report[track.name]={'loop_error':error,'motion_radians':angular,'root_translation':0}
for t in rig.animation_data.nla_tracks:t.mute=False
sys.argv=['export','--','--output',str(OUT),'--collection','LionfishAsset','--max-triangles','3200','--max-bones','20','--max-materials','2','--max-texture','1024','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT))
imported=[o for o in bpy.data.objects if o.type=='MESH'];arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
clips=[a.name for a in bpy.data.actions]
assert len(clips)==3,clips
assert len(arm.data.bones)==20
assert len(imported)==2
report['round_trip']={'clips':clips,'bones':len(arm.data.bones),'meshes':len(imported),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in imported)}
(ART/'AnimationValidation.json').write_text(json.dumps(report,indent=2))
print('LIONFISH_VALIDATION_PASSED',json.dumps(report))
