import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=r'D:/GIT Project/Underwater-VR/ArtSource/CurledMorayEncounter/CurledMoray_Encounter.blend');rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=bpy.data.actions['SwimStraight'];bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
for b in list(rig.pose.bones)[::4]:print(b.name,list(b.head),list((b.tail-b.head).normalized()))
