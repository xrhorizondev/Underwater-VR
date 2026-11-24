import bpy,json,numpy as np
from pathlib import Path
from mathutils.bvhtree import BVHTree
out=Path('D:/GIT Project/Underwater-VR/ArtSource/MorayComparison');report={}
for kind in ['Straight','Curled']:
 bpy.ops.wm.open_mainfile(filepath=str(out/(kind+'.blend')));full=bpy.data.objects[kind+'_Full'];low=bpy.data.objects[kind+'_Reduced'];tree=BVHTree.FromPolygons([v.co for v in low.data.vertices],[p.vertices[:] for p in low.data.polygons]);dist=[]
 for v in list(full.data.vertices)[::max(1,len(full.data.vertices)//10000)]:
  hit=tree.find_nearest(v.co)
  if hit:dist.append(hit[3])
 report[kind]=dict(samples=len(dist),maxSurfaceErrorMetres=max(dist),meanSurfaceErrorMetres=float(np.mean(dist)),p99SurfaceErrorMetres=float(np.quantile(dist,.99)))
 print(kind,report[kind],flush=True)
(out/'SurfaceDeviation.json').write_text(json.dumps(report,indent=2))

