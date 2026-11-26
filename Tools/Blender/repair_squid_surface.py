"""Replace faceted arm tubes with sealed, continuously mapped tapered arms."""
import bpy,bmesh,math,runpy,sys,json,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR'); OUT=ROOT/'ArtSource/SquidSurfaceRepair'; OUT.mkdir(exist_ok=True)
RENDER=ROOT/'Renders/SquidSurfaceRepair';RENDER.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/SquidUndersideRepair/ReferenceSquid.blend'))
obj=bpy.data.objects['ReferenceSquidVisual'];rig=bpy.data.objects['SquidRig'];collection=bpy.data.collections['SquidAsset'];skin=obj.data.materials[0]
# Remove only the old arms, preserving mantle, fins, eyes, rig and animation.
arm_groups={g.index for g in obj.vertex_groups if g.name.startswith('Arm')}
indices={v.index for v in obj.data.vertices if any(g.group in arm_groups for g in v.groups)}
bm=bmesh.new();bm.from_mesh(obj.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.verts[i] for i in indices],context='VERTS');bm.to_mesh(obj.data);bm.free()
parts=[]
for arm in range(10):
 feeding=arm>=8; angle=(arm if not feeding else (1 if arm==8 else 5))*math.tau/8+.18
 radial=Vector((math.cos(angle),0,math.sin(angle))); length=3.55 if feeding else 2.25+.3*math.sin(arm*1.7)
 rings=18 if feeding else 14;sides=10;divisions=3 if feeding else 2
 points=[]
 for j in range(rings+1):
  t=j/rings;spread=.30+math.sin(t*math.pi*.7)*(.25 if feeding else .55)
  # Bury the root in the head and ease into the existing arm silhouette.
  points.append(radial*spread+Vector((math.sin(t*4+arm)*.12*t,-1.02-length*t+.16*(1-t)**6,-.08+math.sin(t*2.5+arm)*.13*t)))
 vertices=[];faces=[];uvs=[];weights=[]
 for j,p in enumerate(points):
  t=j/rings;radius=(.06 if feeding else .105)*(1-t)**.9+.003
  if feeding:radius+=.085*math.exp(-((t-.92)/.10)**2)
  tangent=(points[min(j+1,rings)]-points[max(0,j-1)]).normalized();across=tangent.cross(radial).normalized();up=tangent.cross(across).normalized()
  f=t*divisions;segment=min(divisions-1,int(f));mix=max(0,min(1,f-segment));root=max(0,1-t/.22);root=root*root*(3-2*root)
  w={f'Arm{arm:02d}_{segment}':1-mix if segment<divisions-1 else 1}
  if segment<divisions-1:w[f'Arm{arm:02d}_{segment+1}']=mix
  w={n:value*(1-root) for n,value in w.items()};w['Root']=root
  for i in range(sides):
   a=i/sides*math.tau;vertices.append(p+(across*math.cos(a)+up*math.sin(a))*radius);weights.append(w)
 for j in range(rings):
  for i in range(sides):
   faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
   # Per-corner UVs retain the wrap seam without splitting geometry. Avoid the atlas swatch strip.
   uvs.append(((i/sides,j/rings*.88),((i+1)/sides,j/rings*.88),((i+1)/sides,(j+1)/rings*.88),(i/sides,(j+1)/rings*.88)))
 for end,reverse in [(0,True),(rings,False)]:
  center=len(vertices);vertices.append(points[end]);weights.append({'Root':1} if end==0 else {f'Arm{arm:02d}_{divisions-1}':1})
  for i in range(sides):
   face=(end*sides+i,end*sides+(i+1)%sides,center);faces.append(tuple(reversed(face)) if reverse else face);uvs.append(((.5,.4),(.51,.4),(.5,.41)))
 mesh=bpy.data.meshes.new(f'Continuous arm {arm}');mesh.from_pydata(vertices,[],faces);mesh.materials.append(skin);uv=mesh.uv_layers.new(name='UVMap')
 for p,coords in zip(mesh.polygons,uvs):
  p.use_smooth=True
  for loop,coord in zip(p.loop_indices,coords):uv.data[loop].uv=coord
 part=bpy.data.objects.new(mesh.name,mesh);collection.objects.link(part)
 for idx,w in enumerate(weights):
  for name,value in w.items():
   if value>0:
    group=part.vertex_groups.get(name) or part.vertex_groups.new(name=name);group.add([idx],value,'REPLACE')
 parts.append(part)
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
for p in parts:p.select_set(True)
bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
opaque_open=sum(e.is_boundary and any(f.material_index==0 for f in e.link_faces) for e in bm.edges)
assert opaque_open==0,opaque_open
bm.to_mesh(obj.data);bm.free();obj.data.update()
# Ensure studio and export use the actual skin image, including its atlas eyes.
texture=bpy.data.images.load(str(ROOT/'Assets/UnderwaterArt/BlenderSquid/SquidSkin.png'),check_existing=True)
texture.pack()
tex=skin.node_tree.nodes.new('ShaderNodeTexImage');tex.image=texture
skin.node_tree.links.new(tex.outputs['Color'],skin.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
scene=bpy.context.scene;scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ReferenceSquid.blend'))
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
scene.render.resolution_x=900;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.cycles.samples=12
camera=scene.camera;camera.data.ortho_scale=9.3
for label,pos,frame in [('Front',(0,-9,4),1),('Side',(8,-3,1),13),('Opposite',(-7,-5,3),25),('Phase37',(6,-6,-3),37)]:
 scene.frame_set(frame);camera.location=pos;camera.rotation_euler=(Vector((0,-.65,0))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDER/(label+'.png'));bpy.ops.render.render(write_still=True)
print('SQUID_SURFACE_REPAIR_PASSED opaqueOpenEdges='+str(opaque_open))

