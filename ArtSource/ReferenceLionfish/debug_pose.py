import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('D:/GIT Project/Underwater-VR')
def report(label):
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 print(label,[(b.name,[round(v,4) for v in b.location],[round(v,4) for v in b.scale],round(b.rotation_quaternion.angle,4)) for b in rig.pose.bones])
 for obj in [o for o in bpy.data.objects if o.type=='MESH']:
  dg=bpy.context.evaluated_depsgraph_get();ev=obj.evaluated_get(dg);me=ev.to_mesh();print(obj.name,'BOUNDS', [round(min(v.co[i] for v in me.vertices),4) for i in range(3)],[round(max(v.co[i] for v in me.vertices),4) for i in range(3)],'UVS',[(u.name,u.active_render) for u in obj.data.uv_layers]);ev.to_mesh_clear()
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/ReferenceLionfish/Lionfish.blend'));bpy.context.scene.frame_set(16);report('NATIVE')
bpy.context.scene.render.resolution_percentage=65;bpy.context.scene.render.filepath=str(root/'Renders/ReferenceLionfish/Native16.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'Assets/UnderwaterArt/ReferenceLionfish/Lionfish.fbx'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');action=next(a for a in bpy.data.actions if a.name.endswith('|Swim'));rig.animation_data.action=action;bpy.context.scene.frame_set(13);report('IMPORTED')
