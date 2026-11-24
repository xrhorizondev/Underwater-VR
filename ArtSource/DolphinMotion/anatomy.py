import bpy,numpy as np
bpy.ops.wm.open_mainfile(filepath=r'D:/GIT Project/Underwater-VR/ArtSource/MarineRefresh/Dolphin.blend')
o=next(o for o in bpy.data.objects if o.type=='MESH')
for name in ['BODY01','Head','Head_Root','Kluv','eye L','eye R','eyelish_down L','eyelish_up L','TONGUE04']:
 group=o.vertex_groups.get(name)
 if not group:continue
 v=np.array([v.co[:] for v in o.data.vertices if any(g.group==group.index and g.weight>.2 for g in v.groups)])
 print(name,len(v), 'center',v.mean(0) if len(v) else [], 'bounds',(v.min(0),v.max(0)) if len(v) else [])
