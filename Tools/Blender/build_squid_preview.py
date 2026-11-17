"""Reference-inspired animated squid. Preview only; never writes into Unity Assets."""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector
import numpy as np

OUT = Path('D:/GIT Project/Underwater-VR/ArtSource/SquidPreview')
RENDERS = Path('D:/GIT Project/Underwater-VR/Renders/SquidPreview')
OUT.mkdir(parents=True, exist_ok=True)
RENDERS.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
asset = bpy.data.collections.new('SquidAsset')
bpy.context.scene.collection.children.link(asset)

def image(name, data):
    im = bpy.data.images.new(name, width=data.shape[1], height=data.shape[0], alpha=True)
    im.pixels.foreach_set(data.astype(np.float32).ravel())
    im.filepath_raw = str(OUT / (name+'.png'))
    im.file_format = 'PNG'
    im.save()
    return im

rng=np.random.default_rng(731)
v,u=np.mgrid[0:1024,0:1024]/1023
dorsal=(np.sin(u*math.tau)*.5+.5)**.65
mottle=np.sin(u*58+np.sin(v*25)*2)*np.cos(v*74+u*31)
grain=rng.random((1024,1024))
base=np.empty((1024,1024,4),dtype=np.float32)
ventral=np.array([.63,.68,.65]); back=np.array([.29,.25,.25])
base[:,:,:3]=ventral[None,None,:]*(1-dorsal[:,:,None])+back[None,None,:]*dorsal[:,:,None]
base[:,:,:3]*=(.92+.10*mottle+.06*grain)[:,:,None]
spots=(grain>.91)&(np.sin(u*180+v*11)*np.cos(v*195)>.05)
base[:,:,:3]+=spots[:,:,None]*np.array([.15,.13,.10])
base[:,:,3]=1
swatches=[(.52,.60,.57),(.10,.30,.32),(.007,.012,.015),(.76,.73,.62),(.95,.98,.99)]
for i,color in enumerate(swatches):
    base[950:,i*204:min(1024,(i+1)*204),:3]=color
skin_image=image('SquidSkin',base)
fin_data=base.copy()
veins=.5+.5*np.sin(u*90+v*20)
fin_data[:,:,:3]=base[:,:,:3]*.8+np.array([.05,.12,.13])
fin_data[:,:,3]=.25+.38*(1-u)**.5+.06*veins
fin_image=image('SquidFin',fin_data)

def material(name, texture, alpha=False):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    nodes=mat.node_tree.nodes;principled=nodes.get('Principled BSDF')
    tex=nodes.new('ShaderNodeTexImage');tex.image=texture
    mat.node_tree.links.new(tex.outputs['Color'],principled.inputs['Base Color'])
    principled.inputs['Roughness'].default_value=.34 if not alpha else .42
    principled.inputs['Metallic'].default_value=.04
    if alpha:
        mat.node_tree.links.new(tex.outputs['Alpha'],principled.inputs['Alpha'])
        if hasattr(mat,'surface_render_method'):mat.surface_render_method='DITHERED'
    return mat
skin=material('Natural silver and muted copper skin',skin_image)
fin_mat=material('Thin translucent fin membrane',fin_image,True)

def mesh_object(name, vertices, faces, uvs, mat, weights):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    layer=mesh.uv_layers.new(name='UVMap')
    for polygon in mesh.polygons:
        polygon.use_smooth=True
        for loop in polygon.loop_indices:layer.data[loop].uv=uvs[mesh.loops[loop].vertex_index]
    obj=bpy.data.objects.new(name,mesh);asset.objects.link(obj);obj.data.materials.append(mat)
    groups={}
    for index, values in enumerate(weights):
        for bone,weight in values.items():
            if weight<=0:continue
            if bone not in groups:groups[bone]=obj.vertex_groups.new(name=bone)
            groups[bone].add([index],weight,'REPLACE')
    return obj

def mantle_width(y):
    t=max(0,min(1,(y+.25)/4.35))
    return .04+.79*(math.sin(math.pi*(t*.84+.10)))**.8*(1-t)**.22

verts=[];uvs=[];faces=[];weights=[]
rings=30;sides=32
for j in range(rings+1):
    t=j/rings;y=-.25+t*4.35;width=mantle_width(y)
    for i in range(sides+1):
        a=i/sides*math.tau
        verts.append((math.cos(a)*width,y,math.sin(a)*width*.82+.03))
        uvs.append((i/sides,t*.9));weights.append({'Mantle':.7,'Root':.3})
for j in range(rings):
    for i in range(sides):
        a=j*(sides+1)+i;faces.append((a,a+1,a+sides+2,a+sides+1))
cap=len(verts);verts.append((0,4.10,.03));uvs.append((.5,.9));weights.append({'Mantle':.7,'Root':.3})
for i in range(sides):faces.append((rings*(sides+1)+i,rings*(sides+1)+i+1,cap))
mesh_object('Tapered mantle',verts,faces,uvs,skin,weights)

def ellipsoid(name, center, scale, swatch=None, segments=20, rings=12):
    vertices=[];uvs=[];faces=[]
    for j in range(rings+1):
        theta=j/rings*math.pi
        for i in range(segments+1):
            phi=i/segments*math.tau
            n=(math.sin(theta)*math.cos(phi),math.cos(theta),math.sin(theta)*math.sin(phi))
            vertices.append(tuple(center[k]+n[k]*scale[k] for k in range(3)))
            uvs.append(((swatch+.5)/5,.97) if swatch is not None else (i/segments,j/rings*.9))
    for j in range(rings):
        for i in range(segments):
            a=j*(segments+1)+i;faces.append((a,a+1,a+segments+2,a+segments+1))
    return mesh_object(name,vertices,faces,uvs,skin,[{'Root':1}]*len(vertices))
ellipsoid('Head and collar',(0,-.63,0),(.51,.68,.43))
ellipsoid('Siphon',(0,-.58,-.35),(.14,.45,.11),0,12,6)
for side,label in [(-1,'Left'),(1,'Right')]:
    ellipsoid('Eye'+label,(side*.466,-.78,.09),(.13,.24,.235),0,16,8)
    ellipsoid('Iris'+label,(side*.573,-.80,.10),(.048,.15,.15),1,12,6)
    ellipsoid('Pupil'+label,(side*.613,-.805,.102),(.018,.10,.115),2,12,6)
    ellipsoid('Catchlight'+label,(side*.631,-.84,.151),(.008,.027,.026),4,8,4)

bone_specs=[('Root',(0,-1.1,0),(0,-.1,0),None),('Mantle',(0,-.1,0),(0,3.9,0),'Root')]
for side,label in [(-1,'L'),(1,'R')]:
    for station in range(3):
        y=.45+station*1.28
        bone_specs.append((f'Fin{label}{station}',(side*.42,y,0),(side*1.1,y,0),'Mantle'))
    verts=[];uvs=[];faces=[];weights=[]
    for j in range(29):
        t=j/28;y=-.10+t*4.02;edge=math.sin(t*math.pi)**.72*.70
        for i in range(6):
            q=i/5;x=side*(mantle_width(y)*.92+edge*q)
            verts.append((x,y,.015+q*.06*math.sin(t*math.pi*2)))
            uvs.append((q,t*.9))
            station=max(0,min(2,(y-.45)/1.28));lo=int(station);hi=min(2,lo+1);mix=station-lo
            w={'Mantle':1-q*.92};w[f'Fin{label}{lo}']=q*.92*(1-mix)
            if hi!=lo:w[f'Fin{label}{hi}']=q*.92*mix
            weights.append(w)
    for j in range(28):
        for i in range(5):
            a=j*6+i;face=(a,a+1,a+7,a+6);faces.append(face if side>0 else tuple(reversed(face)))
    mesh_object('Fin skirt '+label,verts,faces,uvs,fin_mat,weights)

for arm in range(10):
    feeding=arm>=8
    a=(arm if not feeding else (1 if arm==8 else 5))*math.tau/8+.18
    radial=Vector((math.cos(a),0,math.sin(a)))
    length=3.55 if feeding else 2.25+.3*math.sin(arm*1.7)
    points=[]
    for j in range(15 if feeding else 12):
        t=j/(14 if feeding else 11)
        spread=.30+math.sin(t*math.pi*.7)*(.25 if feeding else .55)
        p=radial*spread+Vector((math.sin(t*4+arm)*.12*t,-1.02-length*t,-.08+math.sin(t*2.5+arm)*.13*t))
        points.append(p)
    divisions=3 if feeding else 2
    names=[]
    for segment in range(divisions):
        name=f'Arm{arm:02d}_{segment}';names.append(name)
        start=points[round(segment*(len(points)-1)/divisions)]
        end=points[round((segment+1)*(len(points)-1)/divisions)]
        bone_specs.append((name,start,end,'Root' if segment==0 else names[segment-1]))
    verts=[];uvs=[];faces=[];weights=[];sides=8
    for j,p in enumerate(points):
        t=j/(len(points)-1)
        radius=(.06 if feeding else .105)*(1-t)**.72+.009
        if feeding:radius+=.085*math.exp(-((t-.92)/.10)**2)
        tangent=(points[min(j+1,len(points)-1)]-points[max(0,j-1)]).normalized()
        across=tangent.cross(radial).normalized();up=tangent.cross(across).normalized()
        f=t*divisions;segment=min(divisions-1,int(f));mix=max(0,min(1,f-segment))
        for i in range(sides+1):
            angle=i/sides*math.tau;verts.append(p+(across*math.cos(angle)+up*math.sin(angle))*radius)
            # Pale inner arm stripe suggests rows of suckers without individual meshes.
            uvs.append((.7,.97) if 2<=i<=4 else (i/sides,t*.9))
            w={names[segment]:1}
            if segment<divisions-1:w={names[segment]:1-mix,names[segment+1]:mix}
            weights.append(w)
    for j in range(len(points)-1):
        for i in range(sides):
            b=j*(sides+1)+i;faces.append((b,b+1,b+sides+2,b+sides+1))
    cap=len(verts);verts.append(points[-1]);uvs.append((.5,.5));weights.append({names[-1]:1})
    last=(len(points)-1)*(sides+1)
    for i in range(sides):faces.append((last+i,last+i+1,cap))
    mesh_object(('Feeding tentacle ' if feeding else 'Arm ')+str(arm),verts,faces,uvs,skin,weights)

rig_data=bpy.data.armatures.new('SquidRig');rig=bpy.data.objects.new('SquidRig',rig_data);asset.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,head,tail,parent in bone_specs:
    bone=rig_data.edit_bones.new(name);bone.head=head;bone.tail=tail
    if parent:bone.parent=rig_data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
for obj in list(asset.objects):
    if obj.type=='MESH':
        mod=obj.modifiers.new('Squid skeletal motion','ARMATURE');mod.object=rig;obj.parent=rig
# Two material slots on one skinned visual: body/eyes/arms and fin membrane.
bpy.ops.object.select_all(action='DESELECT')
parts=[o for o in asset.objects if o.type=='MESH']
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
bpy.context.object.name='ReferenceSquidVisual'
for frame in range(1,50,4):
    phase=(frame-1)/48*math.tau
    for bone in rig.pose.bones:
        bone.rotation_mode='XYZ';x=y=z=0
        if bone.name.startswith('Fin'):
            index=int(bone.name[-1]);side=1 if 'L' in bone.name else -1
            x=.025*math.sin(phase-index*.8);y=side*.16*math.sin(phase-index*.85)
        elif bone.name.startswith('Arm'):
            arm=int(bone.name[3:5]);segment=int(bone.name[-1]);x=.09*math.sin(phase+arm*.5-segment*.8);z=.07*math.cos(phase+arm*.5-segment*.8)
        elif bone.name=='Mantle':x=.012*math.sin(phase)
        bone.rotation_euler=(x,y,z);bone.keyframe_insert(data_path='rotation_euler',frame=frame)
rig.animation_data.action.name='Swim'
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=49;scene.render.fps=24;scene.frame_set(1)
scene.unit_settings.system='METRIC'
# Render-only studio objects are outside SquidAsset and excluded from FBX.
world=bpy.data.worlds.new('Deep blue studio');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.013,.045,.075,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.45
def light(name,location,power,color,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=location;obj.rotation_euler=(Vector((0,0,0))-obj.location).to_track_quat('-Z','Y').to_euler()
light('Soft key',(1,-1,7),1000,(.74,.88,1),6)
light('Warm fill',(-4,-2,2),650,(1,.77,.61),5)
light('Fin rim',(2,5,1),1200,(.40,.72,1),4)
camera_data=bpy.data.cameras.new('Preview camera');camera=bpy.data.objects.new('Preview camera',camera_data);scene.collection.objects.link(camera);scene.camera=camera
camera_data.type='ORTHO';camera_data.ortho_scale=10.7
def camera_at(position):
    camera.location=position;camera.rotation_euler=(Vector((0,-.05,0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera_at((9,-9,7));scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1100;scene.render.resolution_y=780;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ReferenceSquid.blend'))
for obj in bpy.context.selected_objects:obj.select_set(False)
for obj in asset.objects:obj.select_set(True)
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--require-animation']
import runpy
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
for label,position,frame in [('ThreeQuarter',(9,-9,7),13),('Opposite',(-9,-7,5),29),('Side',(10,0,2),1)]:
    camera_at(position);scene.frame_set(frame);scene.render.filepath=str(RENDERS/(label+'.png'));bpy.ops.render.render(write_still=True)
camera_at((9,-9,7));scene.render.resolution_x=640;scene.render.resolution_y=456;scene.cycles.samples=8
for frame in range(1,49,2):
    scene.frame_set(frame);scene.render.filepath=str(RENDERS/f'Swim_{frame:03d}.png');bpy.ops.render.render(write_still=True)
print('SQUID_PREVIEW_COMPLETE '+str(OUT))
