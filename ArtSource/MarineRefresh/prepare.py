import bpy,math,json,sys,runpy,re
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
ROOT=Path('D:/GIT Project/Underwater-VR'); BASE=ROOT/'ArtSource/MarineRefresh'; EXPORTER='C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py'
kind=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'Dolphin'
bpy.ops.wm.open_mainfile(filepath=str(BASE/(kind+'_Imported.blend')))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.render.fps=30
out=ROOT/'Assets/UnderwaterArt/MarineRefresh'/kind;out.mkdir(parents=True,exist_ok=True);(out/'textures').mkdir(exist_ok=True)
# Remove non-anatomical authoring helpers / bubble props and duplicate scan LODs.
for o in list(bpy.data.objects):
 remove=o.type not in {'MESH','ARMATURE','EMPTY'}
 if o.type=='MESH':
  remove=o.name=='Icosphere' or (kind=='Dolphin' and o.name.startswith('Cube')) or (kind=='Lionfish' and o.name=='Object_4')
 if remove:bpy.data.objects.remove(o,do_unlink=True)
if kind=='Dolphin':
 for im in bpy.data.images:
  p=BASE/'Source/Dolphin/textures'/Path(im.filepath.replace('\\','/')).name
  if p.exists():im.filepath=str(p);im.reload()
# Export source image maps, preserving the UV atlas and baked eyes.
def image_from_socket(socket,visited=None):
 if not socket or not socket.is_linked:return None
 visited=visited or set()
 for link in socket.links:
  node=link.from_node
  if node in visited:continue
  visited.add(node)
  if node.type=='TEX_IMAGE':return node.image
  for input in node.inputs:
   im=image_from_socket(input,visited)
   if im:return im
 return None
manifest={'kind':kind,'materials':[],'notes':[]};saved={}
def save_image(im,role):
 if not im or im.size[0]==0:return None
 key=(im.name,role)
 if key in saved:return saved[key]
 name=kind+'_'+re.sub('[^a-zA-Z0-9]+','_',im.name)[:65]+'_'+role+'.png'
 if max(im.size)>1024:im.scale(1024,1024)
 im.filepath_raw=str(out/'textures'/name);im.file_format='PNG';im.save();im.pack();saved[key]='textures/'+name;return saved[key]
meshes=[o for o in scene.objects if o.type=='MESH'];rigs=[o for o in scene.objects if o.type=='ARMATURE']
used_mats=list(dict.fromkeys(m for o in meshes for m in o.data.materials if m))
for m in used_mats:
 bsdf=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 sockets=bsdf.inputs if bsdf else None
 data={'name':m.name,'baseColor':[1,1,1,1],'baseColorTexture':None,'normalTexture':None,'roughness':.4}
 if bsdf:
  data['baseColor']=list(sockets['Base Color'].default_value);data['baseColorTexture']=save_image(image_from_socket(sockets['Base Color']),'BaseColor');data['normalTexture']=save_image(image_from_socket(sockets['Normal']),'Normal');data['roughness']=sockets['Roughness'].default_value
 manifest['materials'].append(data)
# Keep source geometry in neutral bind pose while simplifying.
for rig in rigs:
 if rig.animation_data:
  rig.animation_data.action=None
  for t in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(t)
 for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
scene.frame_set(0);bpy.context.view_layer.update()
def decimate(o,target):
 o.data.calc_loop_triangles();count=len(o.data.loop_triangles)
 if count<=target:return
 bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Quest silhouette reduction','DECIMATE');mod.ratio=max(.0001,target/count);mod.use_collapse_triangulate=True
 while o.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
 bpy.ops.object.modifier_apply(modifier=mod.name);print('REDUCED',kind,o.name,count,'target',target,flush=True)
if kind=='Dolphin':
 # The many Cube objects in the source are floating water/bubble props, not teeth.
 manifest['notes'].append('Removed source bubble props; kept all five anatomical meshes, both eyes and original armature.')
 for o in meshes:
  target=4800 if o.name=='untitled1.002' else 700 if o.name=='Plane.005' else 600
  decimate(o,target)
 # Same atlas on both source material variants; one shared material suffices.
 main=used_mats[0]
 for o in meshes:
  o.data.materials.clear();o.data.materials.append(main)
  for p in o.data.polygons:p.material_index=0
 manifest['materials']=[next(m for m in manifest['materials'] if m['name']==main.name)]
# Join anatomy to keep each creature to one skinned renderer.
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();mesh=bpy.context.object;mesh.name=kind+'_Anatomy';meshes=[mesh]
if kind in {'CombJelly','Lionfish'}:
 bpy.context.view_layer.objects.active=mesh;bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00001);bpy.ops.object.mode_set(mode='OBJECT')
if kind=='CombJelly':decimate(mesh,6000)
if kind=='Lionfish':decimate(mesh,20000);manifest['notes'].append('Kept every source scan surface partition and removed the non-anatomical helper cube before reduction.')
# Canonical Blender: Z up, animal head -Y (FBX becomes Unity +Z).
rotation=Matrix.Rotation(math.pi/2,4,'X') if kind=='Shark' else Matrix.Rotation(math.pi/2,4,'Z') if kind=='Lionfish' else Matrix.Identity(4)
points=[rotation@(mesh.matrix_world@v.co) for v in mesh.data.vertices]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(lo+hi)/2
length=(hi.z-lo.z) if kind=='CombJelly' else (hi.y-lo.y);target_length=1 if kind=='CombJelly' else 2 if kind=='Lionfish' else 3
factor=target_length/length
if kind in {'CombJelly','Lionfish'}:
 old_world=mesh.matrix_world.copy();mesh.parent=None;mesh.matrix_world=Matrix.Identity(4)
 for v in mesh.data.vertices:v.co=((rotation@(old_world@v.co))-center)*factor
 if kind=='Lionfish':
  bpy.context.view_layer.objects.active=mesh;bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00015);bpy.ops.object.mode_set(mode='OBJECT');mesh.data.normals_split_custom_set([(0,0,0)]*len(mesh.data.loops))
 # All obsolete authoring empties can be omitted now.
 for o in list(bpy.data.objects):
  if o!=mesh:bpy.data.objects.remove(o,do_unlink=True)
else:
 root=bpy.data.objects.new(kind+'_Canonical',None);scene.collection.objects.link(root)
 # Preserve the imported mesh/armature binding and normalize through a visual root.
 objs=[o for o in scene.objects if o!=root]
 for o in objs:
  if o.parent is None:o.parent=root
 root.matrix_world=Matrix.Translation(-center*factor)@Matrix.Scale(factor,4)@rotation
bpy.context.view_layer.update()
# Static assets receive a compact weighted armature for genuine tissue/fin motion.
if kind in {'CombJelly','Lionfish'}:
 arm=bpy.data.armatures.new(kind+'_Rig');rig=bpy.data.objects.new(kind+'_Rig',arm);scene.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);mesh.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
 def bone(name,head,tail,parent='Root'):
  b=arm.edit_bones.new(name);b.head=head;b.tail=tail
  if parent:b.parent=arm.edit_bones[parent]
  return b
 bone('Root',(0,0,0),(0,0,.1),None)
 if kind=='CombJelly':
  bone('Bell',(0,0,.03),(0,0,.4))
  for side in range(4):
   angle=side*math.pi/2;x=math.cos(angle)*.1;y=math.sin(angle)*.1
   bone('Lobe_'+str(side),(x,y,.05),(x*1.5,y*1.5,-.22),'Bell');bone('LobeTip_'+str(side),(x*1.5,y*1.5,-.22),(x*1.5,y*1.5,-.48),'Lobe_'+str(side))
 else:
  bone('Body',(0,-.2,0),(0,.25,0));bone('TailBase',(0,.25,0),(0,.6,0),'Body');bone('TailTip',(0,.6,0),(0,.95,0),'TailBase')
  for side in [-1,1]:bone('Pectoral_'+str(side),(side*.1,-.3,-.05),(side*.7,-.15,-.1),'Body');bone('Pelvic_'+str(side),(side*.1,.1,-.07),(side*.35,.2,-.25),'Body')
  for i in range(7):
   y=-.5+i*.14;candidates=[v.co.z for v in mesh.data.vertices if abs(v.co.y-y)<.11 and abs(v.co.x)<.2]
   top=max(candidates,default=.4);bone('Dorsal_'+str(i),(0,y,.1),(0,y,max(.2,top)),'Body')
 bpy.ops.object.mode_set(mode='OBJECT');mesh.parent=rig;mesh.matrix_parent_inverse=Matrix.Identity(4);mod=mesh.modifiers.new('Swim deformation','ARMATURE');mod.object=rig
 groups={b.name:mesh.vertex_groups.new(name=b.name) for b in arm.bones}
 for v in mesh.data.vertices:
  x,y,z=v.co;weights={}
  def add(name,value):
   if value>0:weights[name]=weights.get(name,0)+value
  if kind=='CombJelly':
   amount=max(0,min(1,(.13-z)/.3));angle=math.atan2(y,x)%(2*math.pi);side=int(round(angle/(math.pi/2)))%4;tip=max(0,min(1,(-z-.16)/.26));add('Bell',1-amount);add('Lobe_'+str(side),amount*(1-tip));add('LobeTip_'+str(side),amount*tip)
  else:
   tail=max(0,min(1,(y-.2)/.55));tailtip=max(0,min(1,(y-.55)/.33));side=-1 if x<0 else 1;fin=max(0,min(1,(abs(x)-.17)/.28))*(1-tail);dorsal=max(0,min(1,(z-.22)/.25))*(1-fin)*(1-tail)
   index=max(0,min(6,round((y+.5)/.14)));add('Pectoral_'+str(side) if y<0 else 'Pelvic_'+str(side),fin);add('Dorsal_'+str(index),dorsal);remaining=max(0,1-fin-dorsal);add('Body',remaining*(1-tail));add('TailBase',remaining*tail*(1-tailtip));add('TailTip',remaining*tail*tailtip)
  total=sum(weights.values())
  if total<1e-7:weights={'Root':1};total=1
  for name,w in weights.items():groups[name].add([v.index],w/total,'REPLACE')
 rigs=[rig]
rig=rigs[0]
# Use the original shark Swim clip; author a clean in-place dolphin swim on its
# native body/flipper/tail bones, avoiding the source's animated bubble props.
if kind=='Shark':
 action=bpy.data.actions.get('Swim');rig.animation_data_create();rig.animation_data.action=action;end=int(action.frame_range[1]);scene.frame_start=0;scene.frame_end=end
else:
 rig.animation_data_create();action=bpy.data.actions.new(kind+'_Swim' if kind!='CombJelly' else 'CombJelly_Drift');rig.animation_data.action=action;end=60 if kind=='Dolphin' else 120;scene.frame_start=0;scene.frame_end=end
 for frame in range(0,end+1,2):
  phase=frame/end*2*math.pi
  for pb in rig.pose.bones:
   pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion();pb.scale=(1,1,1);pb.location=(0,0,0);angle=0;axis=Vector((1,0,0))
   if kind=='Dolphin':
    if pb.name.startswith('TALE') and not pb.name.endswith('_end'):angle=math.sin(phase+sum(ord(c) for c in pb.name)*.06)*(.11 if pb.name not in {'TALE_L','TALE_R'} else .06)
    elif pb.name=='BODY01':angle=math.sin(phase-.5)*.025
    elif pb.name.startswith('FLIPPER') and not pb.name.endswith('_end'):axis=Vector((0,1,0));angle=math.sin(phase+.8)*.09*(-1 if '_L' in pb.name else 1)
   elif kind=='CombJelly':
    if pb.name=='Bell':pb.scale=(1+math.sin(phase)*.025,1+math.sin(phase)*.025,1-math.sin(phase)*.018)
    elif pb.name.startswith('Lobe'):
     index=int(pb.name.rsplit('_',1)[1]);axis=Vector((math.sin(index*math.pi/2),-math.cos(index*math.pi/2),0));angle=math.sin(phase-index*.7+(.7 if 'Tip' in pb.name else 0))*(.07 if 'Tip' in pb.name else .045)
   else:
    axis=Vector((0,0,1))
    if pb.name in {'TailBase','TailTip'}:angle=math.sin(phase*2+(0 if pb.name=='TailBase' else .65))*(.07 if pb.name=='TailBase' else .12)
    elif pb.name.startswith('Pectoral'):axis=Vector((0,1,0));angle=math.sin(phase+(.4 if pb.name.endswith('1') else -.4))*.10*(-1 if pb.name.endswith('-1') else 1)
    elif pb.name.startswith('Pelvic'):axis=Vector((1,0,0));angle=math.sin(phase+.8)*.07
    elif pb.name.startswith('Dorsal'):axis=Vector((0,1,0));angle=math.sin(phase+int(pb.name.split('_')[1])*.4)*.035
   if angle:
    q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion(axis,angle)@q
   pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name);pb.keyframe_insert(data_path='scale',frame=frame,group=pb.name);pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
# A single unmuted NLA clip keeps FBX/Unity animation import predictable.
track=rig.animation_data.nla_tracks.new();track.name='Swim' if kind!='CombJelly' else 'Drift';track.strips.new(action.name,0,action);rig.animation_data.action=None
# Transparent tissue texture and comb-row mask, baked in the existing UV atlas.
if kind=='CombJelly':
 attr=mesh.data.color_attributes.new(name='CombRows',type='FLOAT_COLOR',domain='POINT')
 for v in mesh.data.vertices:
  x,y,z=v.co;angle=math.atan2(y,x);rows=max(0,math.cos(angle*8))**18;rows*=max(0,min(1,(z+.48)/.12));attr.data[v.index].color=(rows,rows,rows,1)
 mat=mesh.data.materials[0];old_tree=mat.node_tree;old_nodes=list(old_tree.nodes);output=next(n for n in old_nodes if n.type=='OUTPUT_MATERIAL');old_link=output.inputs['Surface'].links[0].from_socket
 color=old_tree.nodes.new('ShaderNodeVertexColor');color.layer_name='CombRows';em=old_tree.nodes.new('ShaderNodeEmission');old_tree.links.new(color.outputs['Color'],em.inputs['Color']);old_tree.links.new(em.outputs[0],output.inputs['Surface'])
 mask=bpy.data.images.new('CombRowsMask',1024,1024,alpha=True);target=old_tree.nodes.new('ShaderNodeTexImage');target.image=mask;old_tree.nodes.active=target
 scene.render.engine='CYCLES';scene.cycles.samples=1;bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);bpy.context.view_layer.objects.active=mesh;bpy.ops.object.bake(type='EMIT',margin=8)
 alpha=np.array(mask.pixels[:],dtype=np.float32).reshape(-1,4)[:,0]
 old_tree.links.new(old_link,output.inputs['Surface']);old_tree.nodes.remove(color);old_tree.nodes.remove(em);old_tree.nodes.remove(target)
 base_img=next(n.image for n in old_nodes if n.type=='TEX_IMAGE' and n.image and 'Color_' in n.image.name)
 rgba=np.array(base_img.pixels[:],dtype=np.float32).reshape(-1,4);rgba[:,3]=.10+.65*np.clip(alpha,0,1);base_img.pixels.foreach_set(rgba.ravel());base_img.update();base_img.filepath_raw=str(out/'textures/CombJelly_TransparentTissue.png');base_img.file_format='PNG';base_img.save();base_img.pack()
 manifest['materials'][0]['baseColorTexture']='textures/CombJelly_TransparentTissue.png';manifest['materials'][0]['baseColor']=[.68,.86,.95,.65];manifest['notes'].append('RGBA tissue alpha baked from eight comb-row masks: translucent body, stronger cilia rows. Body/lobe motion is skeletal; cilia shimmer is a stereo-safe shader.')
 # Use the same mask in the Blender material for visual review.
 bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');image_node=next(n for n in old_nodes if n.type=='TEX_IMAGE' and n.image==base_img);mat.node_tree.links.new(image_node.outputs['Alpha'],bsdf.inputs['Alpha']);bsdf.inputs['Roughness'].default_value=.22;mat.surface_render_method='DITHERED';bsdf.inputs['Base Color'].default_value=(.68,.86,.95,1)
# Resize auxiliary maps too, even when Unity uses a scalar for that channel.
for material in mesh.data.materials:
 for node in material.node_tree.nodes:
  if node.type=="TEX_IMAGE" and node.image and max(node.image.size)>1024:save_image(node.image,"Auxiliary")
# Ensure all textures referenced in the exported materials are packed, UVs and normals retained.
for p in mesh.data.polygons:p.use_smooth=True
scene.frame_set(0);bpy.context.view_layer.update();manifest['frames']=[scene.frame_start,scene.frame_end];manifest['fps']=scene.render.fps;manifest['boneCount']=len(rig.data.bones)
manifest['bones']=[{'name':b.name,'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local)} for b in rig.data.bones]
(out/'materials.json').write_text(json.dumps(manifest,indent=2))
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
 if o.type in {'MESH','ARMATURE','EMPTY'}:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/(kind+'.blend')))
sys.argv=['export','--','--output',str(out/(kind+'.fbx')),'--max-triangles',str({'Dolphin':8500,'Shark':4000,'CombJelly':6500,'Lionfish':20500}[kind]),'--max-bones','48','--max-materials','3','--max-texture','1024','--require-animation']
runpy.run_path(EXPORTER,run_name='__main__')
print('MARINE_PREPARED',kind,flush=True)



