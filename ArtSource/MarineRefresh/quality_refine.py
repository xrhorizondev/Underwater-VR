"""Preserve scan depth in tangent maps, restore authored dolphin resolution, add anatomical lionfish eyes."""
import bpy,math,json,sys,runpy
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
root=Path('D:/GIT Project/Underwater-VR');base=root/'ArtSource/MarineRefresh';out=root/'Assets/UnderwaterArt/MarineRefresh/Lionfish'
bpy.context.preferences.filepaths.save_version=0
# Capture full scan in the same canonical coordinates as the optimized body.
bpy.ops.wm.open_mainfile(filepath=str(base/'Lionfish_Imported.blend'))
rotation=Matrix.Rotation(math.pi/2,4,'Z');verts=[];faces=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH' or o.name=='Object_4':continue
 offset=len(verts);verts.extend(rotation@(o.matrix_world@v.co) for v in o.data.vertices);faces.extend(tuple(offset+i for i in p.vertices) for p in o.data.polygons)
lo=Vector(tuple(min(v[i] for v in verts) for i in range(3)));hi=Vector(tuple(max(v[i] for v in verts) for i in range(3)));center=(lo+hi)/2;factor=2/(hi.y-lo.y);verts=[(v-center)*factor for v in verts]
bpy.ops.wm.open_mainfile(filepath=str(base/'Lionfish_OptimizedBeforeQuality.blend'));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.frame_set(0);rig=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH')
# Geometric normals from the original 546k-triangle surface, not brightness-derived bump.
highdata=bpy.data.meshes.new('Source scan detail');highdata.from_pydata(verts,[],faces);highdata.update();high=bpy.data.objects.new('Source scan detail',highdata);scene.collection.objects.link(high)
for p in highdata.polygons:p.use_smooth=True
scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.035;scene.render.bake.max_ray_distance=.065;scene.render.bake.margin=8
manifest=json.loads((out/'materials.json').read_text());targets=[]
for i,m in enumerate(body.data.materials):
 image=bpy.data.images.new('Lionfish scan normals '+str(i),1024,1024,alpha=False);image.colorspace_settings.name='Non-Color';node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;m.node_tree.nodes.active=node;targets.append((m,image,node))
bpy.ops.object.select_all(action='DESELECT');high.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=body
# Disable deformation for an exact bind-space projection.
for mod in body.modifiers:
 if mod.type=='ARMATURE':mod.show_render=False;mod.show_viewport=False
bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT')
for i,(m,image,node) in enumerate(targets):
 name='Lionfish_ScanSurface_Normal_'+str(i)+'.png';image.filepath_raw=str(out/'textures'/name);image.file_format='PNG';image.save();image.pack();manifest['materials'][i]['normalTexture']='textures/'+name
 # Replace the source unlit/emission output with genuinely lit skin.
 tree=m.node_tree;output=next(n for n in tree.nodes if n.type=='OUTPUT_MATERIAL');base_image=next(n.image for n in tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image!=image and 'Normal' not in n.image.name)
 bsdf=tree.nodes.new('ShaderNodeBsdfPrincipled');bsdf.inputs['Roughness'].default_value=.5;color=tree.nodes.new('ShaderNodeTexImage');color.image=base_image;tree.links.new(color.outputs['Color'],bsdf.inputs['Base Color']);normal=tree.nodes.new('ShaderNodeNormalMap');tree.links.new(node.outputs['Color'],normal.inputs['Color']);tree.links.new(normal.outputs['Normal'],bsdf.inputs['Normal']);tree.links.new(bsdf.outputs[0],output.inputs['Surface'])
for mod in body.modifiers:
 if mod.type=='ARMATURE':mod.show_render=True;mod.show_viewport=True
bpy.data.objects.remove(high,do_unlink=True)
# Eyes occupy the original scan's eye sockets on both sides. A small iris atlas
# supplies a dark round pupil, bronze iris and a dark rim; no painted highlights.
mat=bpy.data.materials.new('Lionfish_Eyes');mat.use_nodes=True;bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.18
size=128;pixels=np.zeros((size,size,4),dtype=np.float32);pixels[:,:,3]=1
for y in range(size):
 v=(y+.5)/size;z=-math.cos(v*math.pi)
 for x in range(size):
  angle=(x+.5)/size*2*math.pi;iris=.5+.5*math.sin(angle*38+math.sin(angle*11)*.7)
  rgb=(.008,.012,.014) if z>.78 else ((.17+iris*.13,.115+iris*.08,.045+iris*.04) if z>.24 else (.035,.027,.018))
  pixels[y,x,:3]=rgb
image=bpy.data.images.new('Lionfish eye atlas',size,size,alpha=True);image.pixels.foreach_set(pixels.ravel());image.filepath_raw=str(out/'textures/Lionfish_Eyes.png');image.file_format='PNG';image.save();image.pack();node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color'])
bvh=BVHTree.FromPolygons([v.co.copy() for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons]);eyes=[]
for side in [-1,1]:
 hit,normal,_,_=bvh.ray_cast(Vector((side*2,-.70,.115)),Vector((-side,0,0)))
 assert hit is not None
 normal=Vector((side,0,0))*.7+normal*.3;normal.normalize()
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=hit-normal*.009);eye=bpy.context.object;eye.name='Lionfish_Eye_'+('Left' if side<0 else 'Right');eye.rotation_euler=normal.to_track_quat('Z','Y').to_euler();eye.scale=(.035,.035,.023);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);eye.data.materials.append(mat)
 for p in eye.data.polygons:p.use_smooth=True
 eye.parent=rig;group=eye.vertex_groups.new(name='Body');group.add(list(range(len(eye.data.vertices))),1,'REPLACE');mod=eye.modifiers.new('Follow head','ARMATURE');mod.object=rig;eyes.append(eye);print('EYE_SOCKET',side,list(hit),list(normal),flush=True)
# Join eyes into the existing skinned draw rather than adding gameplay objects.
bpy.ops.object.select_all(action='DESELECT');body.select_set(True)
for eye in eyes:eye.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
manifest['materials'].append({'name':'Lionfish_Eyes','baseColor':[1,1,1,1],'baseColorTexture':'textures/Lionfish_Eyes.png','normalTexture':None,'roughness':.18})
manifest['notes'].append('Original full scan baked to tangent normals; two socket-fitted anatomical eyes added and weighted to Body.')
(out/'materials.json').write_text(json.dumps(manifest,indent=2));scene.frame_set(0);bpy.ops.object.select_all(action='SELECT');bpy.ops.wm.save_as_mainfile(filepath=str(base/'Lionfish_OptimizedBeforeQuality.blend'))
sys.argv=['export','--','--output',str(out/'Lionfish.fbx'),'--max-triangles','21500','--max-bones','48','--max-materials','3','--max-texture','1024','--require-animation'];runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
# Preserve native source pixels without round-tripping through a loaded image cache.
import shutil
dolphin=root/'Assets/UnderwaterArt/MarineRefresh/Dolphin';dm=json.loads((dolphin/'materials.json').read_text());source=base/'Source/Dolphin/textures'
for role,original in [('baseColorTexture','LPP_TT_checker_2048x2048_UV_GRID_BaseColor.png'),('normalTexture','LPP_TT_checker_2048x2048_UV_GRID_Normal.png')]:
 shutil.copyfile(source/original,dolphin/dm['materials'][0][role])
for name,original in [('Dolphin_Roughness.png','LPP_TT_checker_2048x2048_UV_GRID_Roughness.png'),('Dolphin_Occlusion.png','LPP_TT_checker_2048x2048_UV_GRID_AO.png')]:
 shutil.copyfile(source/original,dolphin/'textures'/name)
print('LIONFISH_QUALITY_COMPLETE',flush=True)
