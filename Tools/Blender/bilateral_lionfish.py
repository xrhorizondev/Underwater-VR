import bpy,bmesh,json,sys,runpy,math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
out=Path(r'D:/GIT Project/Underwater-VR/ArtSource/CornerModel');bpy.ops.wm.open_mainfile(filepath=str(out/'Lionfish_Animated_Breathing.blend'));fish=next(o for o in bpy.context.scene.objects if o.type=='MESH');rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');scene=bpy.context.scene
co=np.array([v.co[:] for v in fish.data.vertices]);lo=co.min(0);hi=co.max(0);t=(co[:,1]-lo[1])/(hi[1]-lo[1]);z=(co[:,2]-lo[2])/(hi[2]-lo[2]);head=co[(t>.86)&(t<.96)&(z>.35)&(z<.65)];plane=float((np.percentile(head[:,0],2)+np.percentile(head[:,0],98))/2);print('BILATERAL_PLANE',plane)
# Add only the developed side-fin/gill patch; preserve the complete old fish.
bm=bmesh.new();bm.from_mesh(fish.data)
limits=[(0,plane+.026,True),(1,float(lo[1]+.28*(hi[1]-lo[1])),True),(1,float(lo[1]+.88*(hi[1]-lo[1])),False)]
for axis,value,keep_positive in limits:
 normal=Vector((0,0,0));normal[axis]=1;point=Vector((0,0,0));point[axis]=value;bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=point,plane_no=normal);bmesh.ops.delete(bm,geom=[v for v in bm.verts if (v.co[axis]<value-1e-7 if keep_positive else v.co[axis]>value+1e-7)],context='VERTS')
for v in bm.verts:v.co.x=2*plane-v.co.x
bmesh.ops.reverse_faces(bm,faces=list(bm.faces));extra=bpy.data.meshes.new('Matching opposite pectoral and gill patch');bm.to_mesh(extra);bm.free();patch=bpy.data.objects.new('Added opposite-side anatomy',extra);bpy.context.collection.objects.link(patch);patch.matrix_world=fish.matrix_world.copy()
for m in fish.data.materials:extra.materials.append(m)
bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);patch.select_set(True);bpy.context.view_layer.objects.active=fish;bpy.ops.object.join()
# Rebuild continuous weights across the new join, so the two covers cannot
# pull duplicated seam vertices apart during breathing.
def smooth(a,b,t):
 f=min(1.,max(0.,(t-a)/(b-a)));return f*f*(3-2*f)
fish.vertex_groups.clear();groups={n:fish.vertex_groups.new(name=n) for n in ['Root','Body','TailBase','TailFan','DorsalFin','LowerFin','ChestBreath','GillLeft','GillRight']}
for v in fish.data.vertices:
 p=v.co;t=float((p.y-lo[1])/(hi[1]-lo[1]));z=float((p.z-lo[2])/(hi[2]-lo[2]))
 if t>=.7:w={'Root':1.}
 elif t>=.4:a=(.7-t)/.3;w={'Root':1-a,'Body':a}
 elif t>=.2:a=(.4-t)/.2;w={'Body':1-a,'TailBase':a}
 else:a=min(1,(.2-t)/.16);w={'TailBase':1-a,'TailFan':a}
 fin=.5*smooth(.62,1,z)*smooth(.2,.35,t)*(1-smooth(.58,.75,t));lower=.4*(1-smooth(0,.32,z))*smooth(.2,.35,t)*(1-smooth(.55,.7,t))
 for n in w:w[n]*=1-fin-lower
 w['DorsalFin']=fin;w['LowerFin']=lower
 relative=(p.x-plane)/.06;body=.62*smooth(.22,.42,t)*(1-smooth(.68,.83,t))*smooth(.15,.35,z)*(1-smooth(.68,.83,z));gill=.85*smooth(.62,.71,t)*(1-smooth(.79,.86,t))*smooth(.25,.4,z)*(1-smooth(.68,.8,z))*smooth(.05,.3,abs(relative));side=smooth(-.1,.1,relative);total=body+gill
 if total>.95:body*=.95/total;gill*=.95/total;total=.95
 for n in w:w[n]*=1-total
 w['ChestBreath']=body;w['GillLeft']=gill*(1-side);w['GillRight']=gill*side
 for n,weight in w.items():
  if weight>1e-7:groups[n].add([v.index],float(weight),'REPLACE')
reports=[]
for reduced in [False,True]:
 stem='Lionfish_BilateralReduced' if reduced else 'Lionfish_Bilateral';bpy.context.view_layer.objects.active=fish
 if reduced:
  dec=fish.modifiers.new('Bilateral reduced copy','DECIMATE');dec.ratio=20000/len(fish.data.polygons);dec.use_collapse_triangulate=True;dec.delimit={'UV','SEAM','MATERIAL'};bpy.ops.object.modifier_move_up(modifier=dec.name);bpy.ops.object.modifier_apply(modifier=dec.name)
 mesh=fish.data;bind=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',bind);bind=bind.reshape(-1,3);edges=np.empty(len(mesh.edges)*2,dtype=np.int32);mesh.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2);rest=np.linalg.norm(bind[edges[:,0]]-bind[edges[:,1]],axis=1);valid=rest>1e-5*(hi[1]-lo[1])
 def evaluated(f):
  scene.frame_set(f);bpy.context.view_layer.update();e=fish.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh();a=np.empty(len(m.vertices)*3,dtype=np.float32);m.vertices.foreach_get('co',a);e.to_mesh_clear();return a.reshape(-1,3)
 first=evaluated(1);seam=float(np.linalg.norm(first-evaluated(73),axis=1).max());maximum=1;minimum=1
 for f in range(1,74,3):
  a=evaluated(f);ratios=np.linalg.norm(a[edges[:,0]]-a[edges[:,1]],axis=1)[valid]/rest[valid];maximum=max(maximum,float(ratios.max()));minimum=min(minimum,float(ratios.min()))
 if seam>1e-6 or minimum<.55 or maximum>1.65:raise RuntimeError(f'Bilateral animation check failed: {minimum}, {maximum}')
 tree=KDTree(len(bind))
 for i,v in enumerate(bind):tree.insert(v,i)
 tree.balance();errors=[tree.find(Vector((2*plane-v[0],v[1],v[2])))[2] for v in bind[::23]];report=dict(model=stem,polygons=len(mesh.polygons),triangles=sum(len(p.vertices)-2 for p in mesh.polygons),vertices=len(mesh.vertices),bones=9,loop_error=seam,max_edge_ratio=maximum,min_edge_ratio=minimum,max_sampled_mirror_error=max(errors),symmetry_plane=plane);reports.append(report)
 scene.frame_set(1);bpy.ops.object.select_all(action='DESELECT');fish.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.wm.save_as_mainfile(filepath=str(out/(stem+'.blend')));sys.argv=['export','--','--output',str(out/(stem+'.fbx')),'--max-triangles','900000','--max-bones','10','--max-materials','2','--max-texture','8192','--require-animation'];runpy.run_path(r'C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
 scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.view_settings.exposure=1;scene.world=bpy.data.worlds.new('Bilateral review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.33,.38,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.8
 cam=scene.camera
 if not cam:bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam
 cam.data.type='ORTHO';cam.data.ortho_scale=float(hi[1]-lo[1])*1.1;look=Vector((plane,float((lo[1]+hi[1])/2),float((lo[2]+hi[2])/2)))
 for view,offset in [('Front',(0,1,0)),('Side',(1,0,.1)),('OtherSide',(-1,0,.1)),('Quarter',(1,.7,.2))]:
  cam.location=look+Vector(offset);cam.rotation_euler=(look-cam.location).to_track_quat('-Z','Y').to_euler();scene.frame_set(13);scene.render.filepath=str(out/(stem+'_'+view+'.png'));bpy.ops.render.render(write_still=True)
(out/'BilateralValidation.json').write_text(json.dumps(reports,indent=2));print('BILATERAL_LIONFISH_VALIDATED',json.dumps(reports))
