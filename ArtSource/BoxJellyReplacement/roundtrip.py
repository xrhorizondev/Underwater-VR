import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR')
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.render.fps=30
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/UnderwaterArt/ImportedBoxJelly/BoxJelly.fbx'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); action=rig.animation_data.action
start,end=map(int,action.frame_range);mesh=[o for o in bpy.context.scene.objects if o.type=='MESH']
def points(frame):
 bpy.context.scene.frame_set(frame);dg=bpy.context.evaluated_depsgraph_get()
 return [o.matrix_world@v.co for o in mesh for v in o.evaluated_get(dg).data.vertices]
a=points(start);b=points(start+(end-start)//4);c=points(end)
ca=sum(a,Vector())/len(a);cb=sum(b,Vector())/len(b)
[o.data.calc_loop_triangles() for o in mesh]
report={'triangles':sum(len(o.data.loop_triangles) for o in mesh),'bones':len(rig.data.bones),'meshes':len(mesh),'clip':action.name,'frames':[start,end],'max_deformation':max(((p-ca)-(q-cb)).length for p,q in zip(a,b)),'loop_seam':max((p-q).length for p,q in zip(a,c))}
print('ROUNDTRIP_METRICS',report)
assert report['triangles']==3544 and report['max_deformation']>.01
(root/'ArtSource/BoxJellyReplacement/roundtrip.json').write_text(json.dumps(report,indent=2));print('FBX_ROUNDTRIP_PASSED',report)



