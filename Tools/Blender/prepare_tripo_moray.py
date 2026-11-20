"""Rig the user-supplied Tripo mesh; author in-place reference-style motion."""
import bpy,bmesh,math,json,sys,runpy
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'ArtSource/TripoMoray';RENDERS=ROOT/'Renders/TripoMoray'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Imported.blend'))
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH');world=mesh.matrix_world.copy();mesh.parent=None;mesh.matrix_world=world
bpy.context.view_layer.objects.active=mesh;mesh.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
before=sum(len(p.vertices)-2 for p in mesh.data.polygons)
mod=mesh.modifiers.new('Quest geometry budget','DECIMATE');mod.ratio=18000/before;mod.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier=mod.name)
mesh.name='Supplied moray skin'
for p in mesh.data.polygons:p.use_smooth=True
print('TRIPO_OPTIMIZED',before,'->',sum(len(p.vertices)-2 for p in mesh.data.polygons),flush=True)
# Inspect transverse sections in the source's world axes: head -Y, dorsal +Z.
v=np.array([tuple(p.co) for p in mesh.data.vertices]);ymin,ymax=v[:,1].min(),v[:,1].max();length=ymax-ymin
sections=[]
for t in np.linspace(0,1,25):
 y=ymin+t*length;pts=v[np.abs(v[:,1]-y)<length*.035]
 sections.append([float(y),float(np.median(pts[:,0])),float(np.median(pts[:,2]))])
(OUT/'Sections.json').write_text(json.dumps(sections,indent=2))
def center(y):return Vector((float(np.interp(y,[s[0] for s in sections],[s[1] for s in sections])),y,float(np.interp(y,[s[0] for s in sections],[s[2] for s in sections]))))
hingeY=-.405;hinge=center(hingeY)
# Follow the descending lip line, stopping at the mouth corner. The old
# horizontal plane continued through the cheek behind the actual mouth.
right=[[-.49,.0448],[-.47,.042],[-.4525,.0404],[-.435,.0406],[-.4306,.0401],[-.4175,.0355],[-.405,.0313]]
left=[[-.49,.043],[-.47875,.03445],[-.47,.0311],[-.46125,.02675],[-.4525,.0241],[-.44375,.021],[-.435,.01835],[-.42625,.0166],[-.4175,.0157],[-.405,.0145]]
widths=[]
for y in np.linspace(ymin,hingeY,24):
 pts=v[np.abs(v[:,1]-y)<.006];widths.append([float(y),float(np.percentile(pts[:,0],2)),float(np.percentile(pts[:,0],98))])
trace={'left':left,'right':right,'widths':widths,'hingeY':hingeY};(OUT/'MouthTrace.json').write_text(json.dumps(trace,indent=2))
def lip_z(y,x):
 lo=np.interp(y,[p[0] for p in widths],[p[1] for p in widths]);hi=np.interp(y,[p[0] for p in widths],[p[2] for p in widths]);t=float(np.clip((x-lo)/max(.0001,hi-lo),0,1))
 return float(np.interp(y,[p[0] for p in left],[p[1] for p in left])*(1-t)+np.interp(y,[p[0] for p in right],[p[1] for p in right])*t)
mouthZ=lip_z(hingeY,hinge.x)
def lip_distance(p):return p.z-lip_z(p.y,p.x)
# Separate the existing closed lip line, leaving the hinge/neck connected.
bm=bmesh.new();bm.from_mesh(mesh.data)
headfaces=[f for f in bm.faces if max(p.co.y for p in f.verts)<hingeY]
geom=list(set(headfaces+[e for f in headfaces for e in f.edges]+[p for f in headfaces for p in f.verts]))
for p in bm.verts:p.co.z-=lip_z(p.co.y,p.co.x)
cut=bmesh.ops.bisect_plane(bm,geom=geom,dist=.000001,plane_co=(0,0,0),plane_no=(0,0,1),clear_inner=False,clear_outer=False)
edges=[e for e in cut['geom_cut'] if isinstance(e,bmesh.types.BMEdge)]
bmesh.ops.split_edges(bm,edges=edges)
for p in bm.verts:p.co.z+=lip_z(p.co.y,p.co.x)
bm.to_mesh(mesh.data);bm.free();mesh.data.update()
# A small skeleton follows the existing curved body; it never straightens or
# substitutes the supplied mesh. Smooth neighboring weights avoid hard hinges.
asset=bpy.data.collections.new('TripoMorayAsset');bpy.context.scene.collection.children.link(asset)
for c in list(mesh.users_collection):c.objects.unlink(mesh)
asset.objects.link(mesh)
data=bpy.data.armatures.new('TripoMorayRig');rig=bpy.data.objects.new('TripoMorayRig',data);asset.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
def bone(name,a,b,parent=None):
 q=data.edit_bones.new(name);q.head=a;q.tail=b
 if parent:q.parent=data.edit_bones[parent]
base=ymin+length*.22
bone('Root',center(base),center(base)+Vector((0,.05,0)))
bone('Head',center(base),center(ymin+length*.03),'Root')
bone('Jaw',Vector((hinge.x,hingeY,mouthZ)),Vector((hinge.x,hingeY-.12,mouthZ)),'Head')
stations=np.linspace(base,ymax-length*.025,13)
for i in range(12):bone(f'Spine{i:02d}',center(stations[i]),center(stations[i+1]),'Root' if i==0 else f'Spine{i-1:02d}')
bpy.ops.object.mode_set(mode='OBJECT')
groups={b.name:mesh.vertex_groups.new(name=b.name) for b in data.bones}
for p in mesh.data.vertices:
 y=p.co.y;f=float(np.clip((y-base)/(stations[-1]-base)*12,0,11));lo=min(11,int(f));mix=f-lo
 weights={f'Spine{lo:02d}':1-mix}
 if lo<11:weights[f'Spine{lo+1:02d}']=mix
 if y<base:
  head=float(np.clip((base-y)/(length*.035),0,1));weights={'Head':head,'Spine00':1-head}
  # On the split line, neighboring faces distinguish upper and lower lip.
  lower=lip_distance(p.co)<-.000001
  if abs(lip_distance(p.co))<.000002:
   connected=[lip_distance(mesh.data.vertices[i].co) for e in mesh.data.edges if p.index in e.vertices for i in e.vertices if i!=p.index]
   lower=bool(connected) and np.mean(connected)<0
  if lower and y<hingeY:
   jaw=float(np.clip((hingeY-y)/(length*.025),0,1));weights={n:w*(1-jaw) for n,w in weights.items()};weights['Jaw']=jaw
 for name,w in weights.items():
  if w>0:groups[name].add([p.index],w,'REPLACE')
modifier=mesh.modifiers.new('Moray skeletal motion','ARMATURE');modifier.object=rig;mesh.parent=rig
actions={}
for label,seconds,strength in [('Idle',4,.07),('Swim',3,.085),('React',2,.055),('Retreat',2.5,.075),('Recover',3,.06)]:
 frames=round(seconds*24);rig.animation_data_create();rig.animation_data.action=None
 for frame in range(1,frames+2,2):
  t=min(1,(frame-1)/frames);phase=t*math.tau
  for b in rig.pose.bones:
   b.rotation_mode='XYZ';x=z=0
   if b.name.startswith('Spine'):
    i=int(b.name[-2:]);z=strength*(.3+.7*(i/11)**1.4)*math.sin(phase-i*.48)
    x=strength*.12*math.sin(phase-i*.48+.6)
   elif b.name=='Head':z=.003*math.sin(phase)
   elif b.name=='Jaw':
    # Small respiratory gape; startle opens farther, then blends back.
    angle=(4+4*math.sin(phase)) if label!='React' else 4+18*math.sin(math.pi*t)**2
    x=-math.radians(angle)
   b.rotation_euler=(x,0,z);b.keyframe_insert(data_path='rotation_euler',frame=frame)
 action=rig.animation_data.action;action.name=label;actions[label]=action
 track=rig.animation_data.nla_tracks.new();track.name=label;track.mute=True;strip=track.strips.new(label,1,action);strip.extrapolation='NOTHING'
scene=bpy.context.scene;scene.render.fps=24;scene.frame_start=1;scene.frame_end=145
# Keep the original material and all three original PBR maps in the authoring file.
bpy.ops.file.pack_all();rig.animation_data.action=None
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TripoMoray.blend'))
for track in rig.animation_data.nla_tracks:track.mute=False
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'TripoMoray.fbx'),'--collection','TripoMorayAsset','--max-triangles','20000','--max-bones','16','--max-texture','4096','--max-materials','1','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('TRIPO_MORAY_RIGGED clips='+str(list(actions)),flush=True)
