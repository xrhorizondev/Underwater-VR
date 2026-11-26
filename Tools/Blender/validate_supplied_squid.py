"""Validate FBX round trip and render both variants from both sides and cycle phases."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');A=R/'Assets/UnderwaterArt/SquidFullBody';O=R/'Renders/SquidFullBody';reports=[]
for name in ['SquidDetail','SquidQuest']:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(A/(name+'.fbx')))
 obj=next(o for o in bpy.data.objects if o.type=='MESH');rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');s=bpy.context.scene;s.render.fps=24
 action=rig.animation_data.action;start,end=map(int,action.frame_range);samples=[];first=None;last=None;maxmove=0;maxedge=0
 for f in range(start,end+1):
  s.frame_set(f);ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();points=[v.co.copy() for v in me.vertices]
  assert all(math.isfinite(c) for p in points for c in p)
  if first is None:first=points
  if f==end:last=points
  maxmove=max(maxmove,max((p-q).length for p,q in zip(points,first)))
  ev.to_mesh_clear()
 seam=max((p-q).length for p,q in zip(first,last));assert seam<.001,seam
 assert maxmove>.005,maxmove
 weight_error=max(abs(sum(g.weight for g in v.groups)-1) for v in obj.data.vertices);assert weight_error<1e-4
 reports.append(dict(asset=name,samples=end-start+1,seam=seam,max_deformation=maxmove,max_weight_error=weight_error,bones=len(rig.data.bones),status='passed'))
 # Workbench uses actual exported UVs/texture, with neutral studio lighting.
 img=bpy.data.images.load(str(A/'SquidAlbedo.png'));mat=obj.data.materials[0];mat.use_nodes=True;node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=img;mat.node_tree.nodes.active=node;mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
 s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.render.resolution_x=1000;s.render.resolution_y=700;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Studio');s.world.color=(.025,.035,.05)
 # FBX importer reconstructs canonical Blender coordinates.
 coords=[obj.matrix_world@v.co for v in obj.data.vertices];center=sum(coords,Vector())/len(coords)
 center=Vector(((min(p.x for p in coords)+max(p.x for p in coords))/2,(min(p.y for p in coords)+max(p.y for p in coords))/2,(min(p.z for p in coords)+max(p.z for p in coords))/2))
 print('ROUNDTRIP_BOUNDS',name,list(center),flush=True)
 d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO';d.ortho_scale=5.5
 views=[('Side',(7,0,0)),('Opposite',(-7,0,0)),('Front',(0,-7,0)),('Quarter',(5,-4,3))]
 for label,offset in views:
  c.location=center+Vector(offset);c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler();s.frame_set(start+18);s.render.filepath=str(O/(name+'_'+label+'.png'));bpy.ops.render.render(write_still=True)
 if name=='SquidQuest':
  c.location=center+Vector((5,-4,3));c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler()
  for f in [1,19,37,55]:
   s.frame_set(f);s.render.filepath=str(O/f'Phase_{f:02}.png');bpy.ops.render.render(write_still=True)
(O/'RoundTrip.json').write_text(json.dumps(reports,indent=2));print('ROUNDTRIP_PASSED',json.dumps(reports),flush=True)
