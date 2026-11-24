import bpy,numpy as np
from pathlib import Path
from mathutils import Vector
out=Path('D:/GIT Project/Underwater-VR/ArtSource/MorayComparison')
bpy.ops.wm.open_mainfile(filepath=str(out/'Imported.blend'));o=next(o for o in bpy.data.objects if o.type=='MESH');v=np.array([v.co[:] for v in o.data.vertices]);p=np.array([p.vertices[:] for p in o.data.polygons])
for name,sel in [('Straight',(v[:,2]>-.04)&(v[:,2]<.07)),('Curled',(v[:,1]<.055)&(v[:,2]>.075))]:
 cross=sel[p].any(1)&~sel[p].all(1);print(name,'triangles',sel[p].all(1).sum(),'cross',cross.sum(),'bounds',v[sel].min(0),v[sel].max(0))
try:import scipy;print('SCIPY',scipy.__version__)
except:print('NO SCIPY')

