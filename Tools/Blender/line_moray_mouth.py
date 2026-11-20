"""Build a recessed, weighted mouth from the actual separated lip boundary."""
import bpy,math,sys,runpy,json,numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'ArtSource/TripoMoray';RENDERS=ROOT/'Renders/TripoMoray'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'TripoMoray.blend'))
rig=bpy.data.objects['TripoMorayRig'];skin=bpy.data.objects['Supplied moray skin'];hinge=rig.data.bones['Jaw'].head_local
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=None
for bone in rig.pose.bones:bone.rotation_euler=(0,0,0)
up=bpy.data.objects.new('DorsalUp',None);bpy.data.collections['TripoMorayAsset'].objects.link(up);up.parent=rig;up.location=rig.data.bones['Root'].head_local+Vector((0,0,.2))
trace=json.loads((OUT/'MouthTrace.json').read_text());widths=trace['widths'];left=trace['left'];right=trace['right']
def plane(y,x=None):
 lo=np.interp(y,[p[0] for p in widths],[p[1] for p in widths]);hi=np.interp(y,[p[0] for p in widths],[p[2] for p in widths]);t=.5 if x is None else float(np.clip((x-lo)/max(.0001,hi-lo),0,1))
 return float(np.interp(y,[p[0] for p in left],[p[1] for p in left])*(1-t)+np.interp(y,[p[0] for p in right],[p[1] for p in right])*t)
def weights(index):return {skin.vertex_groups[g.group].name:g.weight for g in skin.data.vertices[index].groups}
edges={}
for face in skin.data.polygons:
 for a,b in zip(face.vertices,list(face.vertices[1:])+[face.vertices[0]]):edges.setdefault(tuple(sorted((a,b))),[]).append(face)
boundary=[]
for (a,b),faces in edges.items():
 p,q=skin.data.vertices[a].co,skin.data.vertices[b].co
 if len(faces)==1 and max(p.y,q.y)<hinge.y+.0001 and abs(p.z-plane(p.y,p.x))<.00002 and abs(q.z-plane(q.y,q.x))<.00002:boundary.append((a,b,faces[0].center.z-plane(faces[0].center.y,faces[0].center.x)>0))
assert len(boundary)>10,'Missing lip perimeter'
samples=[skin.data.vertices[i].co.copy() for a,b,_ in boundary for i in [a,b]]
def profile(y):
 near=sorted(samples,key=lambda p:abs(p.y-y))[:12];return min(p.x for p in near),max(p.x for p in near)
vs=[];faces=[];colors=[];ws=[]
def vertex(p,color,w):vs.append(tuple(p));colors.append(color);ws.append(w);return len(vs)-1
def face(ids):faces.append(tuple(ids))
rim=(.27,.13,.075,1);lining=(.12,.035,.028,1);deep=(.035,.008,.009,1)
for a,b,upper in boundary:
 p,q=skin.data.vertices[a].co.copy(),skin.data.vertices[b].co.copy();sign=1 if upper else -1
 inner=[];outer=[]
 for index,pos in [(a,p),(b,q)]:
  lo,hi=profile(pos.y);mid=(lo+hi)*.5;ix=mid+(pos.x-mid)*.63;inside=Vector((ix,pos.y,plane(pos.y,ix)+sign*.0025))
  outer.append(vertex(pos,rim,weights(index)));inner.append(vertex(inside,lining,weights(index)))
 face((outer[0],outer[1],inner[1],inner[0]))
 # Roof/floor sit inside the skin, each driven by its own jaw ownership.
 cy=hinge.y-.012;lo,hi=profile(cy);center=Vector(((lo+hi)*.5,cy,plane(cy)+sign*.005))
 c=vertex(center,deep,{'Head' if upper else 'Jaw':1});face((inner[0],inner[1],c))
# Small, staggered teeth share the mouth material through vertex colors.
snout=min(p.y for p in samples)
for upper in [True,False]:
 for side in [-1,1]:
  for i in range(6):
   t=(i+.7+(0 if upper else .4))/7.5;y=snout+.007+t*(hinge.y-snout-.014);lo,hi=profile(y);x=(lo+hi)*.5+side*(hi-lo)*.15
   z=plane(y,x)+(1 if upper else -1)*.0025;w={'Head' if upper else 'Jaw':1};ids=[]
   for k in range(5):
    angle=k*math.tau/5;ids.append(vertex((x+math.cos(angle)*.0009,y+math.sin(angle)*.0009,z),(.72,.64,.43,1),w))
   tip=vertex((x,y-.0006,z+(-1 if upper else 1)*.0045),(.83,.77,.57,1),w)
   for k in range(5):face((ids[k],ids[(k+1)%5],tip))
data=bpy.data.meshes.new('Lip lining palate and teeth');data.from_pydata(vs,[],faces);uv=data.uv_layers.new(name='UVMap')
for poly in data.polygons:
 for li in poly.loop_indices:uv.data[li].uv=(data.vertices[data.loops[li].vertex_index].co.y*10,data.vertices[data.loops[li].vertex_index].co.x*10)
attr=data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
for loop in data.loops:attr.data[loop.index].color=colors[loop.vertex_index]
mat=bpy.data.materials.new('Moray lip lining and ivory teeth');mat.use_nodes=True;nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF');col=nodes.new('ShaderNodeVertexColor');col.layer_name='Color';mat.node_tree.links.new(col.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.64;mat.use_backface_culling=False;data.materials.append(mat)
obj=bpy.data.objects.new('Weighted mouth lining',data);bpy.data.collections['TripoMorayAsset'].objects.link(obj)
for name in set(n for w in ws for n in w):
 group=obj.vertex_groups.new(name=name)
 for i,w in enumerate(ws):
  if w.get(name,0)>0:group.add([i],w[name],'REPLACE')
attr=skin.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
for c in attr.data:c.color=(1,1,1,1)
bpy.ops.object.select_all(action='DESELECT');skin.select_set(True);obj.select_set(True);bpy.context.view_layer.objects.active=skin;bpy.ops.object.join()
for attribute in list(skin.data.color_attributes):
 if attribute.name!='Color':skin.data.color_attributes.remove(attribute)
skin.data.color_attributes.active_color_index=0;skin.data.color_attributes.render_color_index=0
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TripoMoray.blend'))
for track in rig.animation_data.nla_tracks:track.mute=False
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'TripoMoray.fbx'),'--collection','TripoMorayAsset','--max-triangles','20000','--max-bones','16','--max-texture','4096','--max-materials','2','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
for track in rig.animation_data.nla_tracks:track.mute=True
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1000;scene.render.resolution_y=750;scene.render.resolution_percentage=100
world=bpy.data.worlds.new('Mouth review');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.05,.06,.07,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4
cy=(snout+hinge.y)*.5;lo,hi=profile(cy);center=Vector(((lo+hi)*.5,cy,plane(cy)))
for name,pos in [('Key',(.4,-.5,.8)),('Fill',(-.4,-.5,.3))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=12;d.size=.5;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',d);scene.collection.objects.link(camera);scene.camera=camera;d.type='ORTHO';d.ortho_scale=.14
def render(name,pos,angle):
 rig.animation_data.action=None
 for bone in rig.pose.bones:bone.rotation_euler=(0,0,0)
 rig.pose.bones['Jaw'].rotation_euler.x=-math.radians(angle);bpy.context.view_layer.update();camera.location=center+Vector(pos);camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDERS/(name+'.png'));bpy.ops.render.render(write_still=True)
for label,pos in [('Side',(.5,0,.03)),('Front',(0,-.5,.02)),('Opposite',(-.5,-.1,.03))]:
 for angle in [0,8,22]:render('Mouth_'+label+'_'+str(angle),pos,angle)
scene.render.resolution_x=640;scene.render.resolution_y=480;scene.cycles.samples=8
for i in range(16):render('MouthCycle_'+str(i).zfill(2),(.4,-.3,.08),11-11*math.cos(i*math.tau/16))
(OUT/'MouthLining.json').write_text(json.dumps({'boundaryEdges':len(boundary),'mouthMaterialSlots':2,'teeth':24,'addedTriangles':sum(len(f)-2 for f in faces),'hingeY':hinge.y},indent=2))
print('MOUTH_LINING_COMPLETE')
