import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
R=Path('D:/GIT Project/Underwater-VR');O=R/'ArtSource/SquidFullBody';O.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='C:/Users/mobin/Downloads/tripo_pbr_model_fa5f9eb1-7c78-40c7-aafb-7952c52852f3_meshopt.glb')
o=next(o for o in bpy.data.objects if o.type=='MESH');bpy.context.view_layer.objects.active=o
bm=bmesh.new();bm.from_mesh(o.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not(v.co.y>.055 and v.co.z>.145)],context='VERTS')
remaining=set(bm.verts);parts=[]
while remaining:
 v=remaining.pop();part={v};todo=[v]
 while todo:
  for e in todo.pop().link_edges:
   n=e.other_vert(todo[-1]) if False else None
   for n in e.verts:
    if n in remaining:remaining.remove(n);part.add(n);todo.append(n)
 parts.append(part)
print('PARTS',sorted([len(p) for p in parts],reverse=True)[:30],flush=True)
for p in parts:
 if len(p)<100:bmesh.ops.delete(bm,geom=list(p),context='VERTS')
bm.to_mesh(o.data);bm.free();o.data.update();o.name='SuppliedFullBodySquid';o.data.calc_loop_triangles();print('EXTRACT',len(o.data.vertices),len(o.data.loop_triangles),[list(v) for v in o.bound_box],flush=True)
# Canonical Blender axis: mantle towards +Y, arms trail towards -Y, dorsal +Z.
for v in o.data.vertices:
 x,y,z=v.co;v.co=(x*11.5,-(y-.30)*11.5,(z-.20)*11.5)
o.data.update()
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SuppliedFullBody_Source.blend'))
m=o.modifiers.new('High detail reduction','DECIMATE');m.ratio=min(1,60000/len(o.data.loop_triangles));bpy.ops.object.modifier_apply(modifier=m.name)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SuppliedFullBody_Working.blend'))
s=bpy.context.scene;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='TEXTURE';s.display.shading.show_cavity=True;s.render.resolution_x=1000;s.render.resolution_y=700;s.render.resolution_percentage=100;s.world=bpy.data.worlds.new('Studio');s.world.color=(.025,.035,.05)
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);s.collection.objects.link(c);s.camera=c;d.type='ORTHO';d.ortho_scale=5.4
for name,pos in [('ExtractSide',(7,0,2)),('ExtractOpposite',(-7,0,2)),('ExtractQuarter',(5,-5,3))]:
 c.location=pos;c.rotation_euler=(-c.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(R/'Renders/SquidNew'/(name+'.png'));bpy.ops.render.render(write_still=True)
