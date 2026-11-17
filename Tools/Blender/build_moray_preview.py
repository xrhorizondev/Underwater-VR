"""Original reference-inspired moray; writes authoring/preview files, never Unity Assets."""
import bpy, bmesh, math, sys, runpy, json
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path('D:/GIT Project/Underwater-VR')
OUT=ROOT/'ArtSource/MorayPreview'; RENDERS=ROOT/'Renders/MorayPreview'
OUT.mkdir(parents=True,exist_ok=True);RENDERS.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
asset=bpy.data.collections.new('MorayAsset');bpy.context.scene.collection.children.link(asset)

def save_image(name,pixels,noncolor=False):
    image=bpy.data.images.new(name,width=1024,height=1024,alpha=True)
    if noncolor:image.colorspace_settings.name='Non-Color'
    image.pixels.foreach_set(pixels.astype(np.float32).ravel())
    image.filepath_raw=str(OUT/(name+'.png'));image.file_format='PNG';image.save()
    return image

rng=np.random.default_rng(1217);v,u=np.mgrid[0:1024,0:1024]/1023
cu=(u+.009*np.sin(v*63+np.sin(u*47)))*38;cv=(v+.007*np.sin(u*51+np.sin(v*37)))*112
cellx=np.floor(cu);celly=np.floor(cv)
jitter=np.sin(cellx*127.1+celly*311.7)*43758.54;jitter-=np.floor(jitter)
dx=cu-cellx-.5+(jitter-.5)*.8
dy=cv-celly-.5+(np.sin(jitter*73)*.5)*.75
radius=.18+.19*jitter
dist=np.sqrt((dx*(1+.3*np.sin(celly)))**2+dy**2)
spots=np.clip((radius-dist)*28,0,1)
noise=rng.random((1024,1024));dorsal=np.sin(u*math.tau)*.5+.5
back=np.array([.20,.225,.105]);belly=np.array([.36,.36,.19])
pixels=np.ones((1024,1024,4),dtype=np.float32)
base=belly[None,None,:]*(1-dorsal[:,:,None])+back[None,None,:]*dorsal[:,:,None]
base*=((.86+.16*noise)+.08*np.sin(u*29+np.sin(v*46)))[:,:,None]
spotcolor=np.array([.66,.64,.38])
pixels[:,:,:3]=base*(1-spots[:,:,None]*.86)+spotcolor*spots[:,:,None]*.86
pixels[830:900,:,:3]*=.60
swatches=[(.08,.027,.022),(.29,.12,.085),(.78,.75,.57),(.33,.31,.13),(.004,.007,.005),(.90,.94,.84)]
for i,color in enumerate(swatches):pixels[910:,i*170:min(1024,(i+1)*170),:3]=color
albedo=save_image('MoraySkin',pixels)
height=spots*.035+noise*.006
gy,gx=np.gradient(height)
normal=np.dstack((-gx*9,-gy*9,np.ones_like(gx)));normal/=np.linalg.norm(normal,axis=2)[:,:,None]
nmap=np.ones_like(pixels);nmap[:,:,:3]=normal*.5+.5;nmap[910:,:,:3]=(.5,.5,1)
normal_image=save_image('MorayNormal',nmap,True)
material=bpy.data.materials.new('Moray spotted olive skin');material.use_nodes=True
nodes=material.node_tree.nodes;shader=nodes.get('Principled BSDF')
color=nodes.new('ShaderNodeTexImage');color.image=albedo
material.node_tree.links.new(color.outputs['Color'],shader.inputs['Base Color'])
normal_tex=nodes.new('ShaderNodeTexImage');normal_tex.image=normal_image
nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.25
material.node_tree.links.new(normal_tex.outputs['Color'],nm.inputs['Color'])
material.node_tree.links.new(nm.outputs['Normal'],shader.inputs['Normal'])
shader.inputs['Roughness'].default_value=.48
shader.inputs['Specular IOR Level'].default_value=.32

def swatch(index):return ((index+.5)/6,.95)
def profile(y,stations,values):
    for i in range(len(stations)-1):
        if y<=stations[i+1]:
            t=max(0,min(1,(y-stations[i])/(stations[i+1]-stations[i])));t=t*t*(3-2*t)
            return values[i]*(1-t)+values[i+1]*t
    return values[-1]

SPINES=12
def center(y):
    t=max(0,(y+.6)/2.45)
    return Vector((.10*math.sin(t*math.pi*2)*t,y,.025*math.sin(t*math.pi)))
def body_radius(y):
    return profile(y,[-.6,-.3,.25,.8,1.25,1.65,1.85],[.14,.15,.14,.105,.065,.029,.002])
def body_weights(y):
    f=max(0,min(SPINES-1,(y+.6)/2.45*(SPINES-1)));a=int(f);b=min(SPINES-1,a+1);mix=f-a
    return {f'Spine{a:02d}':1-mix,**({f'Spine{b:02d}':mix} if a!=b else {})}
def uv_body(a,y):return (a/math.tau,(y+1.25)/3.15*.78)

def mesh(name,vertices,faces,uvs,weights):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free();data.update()
    layer=data.uv_layers.new(name='UVMap')
    for p in data.polygons:
        p.use_smooth=True
        for loop in p.loop_indices:layer.data[loop].uv=uvs[data.loops[loop].vertex_index]
    obj=bpy.data.objects.new(name,data);asset.objects.link(obj);obj.data.materials.append(material)
    groups={}
    for i,weights_for_vertex in enumerate(weights):
        for name,weight in weights_for_vertex.items():
            if weight<=0:continue
            if name not in groups:groups[name]=obj.vertex_groups.new(name=name)
            groups[name].add([i],weight,'REPLACE')
    return obj

vertices=[];faces=[];uvs=[];weights=[];rings=48;sides=20
for j in range(rings+1):
    y=-.6+j/rings*2.45;c=center(y);radius=body_radius(y)
    for i in range(sides+1):
        a=i/sides*math.tau;vertices.append(c+Vector((math.cos(a)*radius,0,math.sin(a)*radius*.95)))
        uvs.append(uv_body(a,y));weights.append(body_weights(y))
for j in range(rings):
    for i in range(sides):
        a=j*(sides+1)+i;faces.append((a,a+1,a+sides+2,a+sides+1))
cap=len(vertices);vertices.append(center(1.85));uvs.append(uv_body(0,1.85));weights.append(body_weights(1.85))
for i in range(sides):faces.append((rings*(sides+1)+i,rings*(sides+1)+i+1,cap))
mesh('Continuous tapered body',vertices,faces,uvs,weights)

def jaw_shell(name,lower=False):
    vs=[];fs=[];uv=[];ws=[];rings=14;sides=16
    ys=[-1.22,-1.18,-1.04,-.84,-.60]
    widths=[.055,.095,.140,.153,.14];heights=[.045,.070,.105,.126,.121]
    for j in range(rings+1):
        y=-1.22+j/rings*.62;w=profile(y,ys,widths);h=profile(y,ys,heights)
        for i in range(sides+1):
            a=i/sides*math.pi
            z=(.012+h*math.sin(a)) if not lower else (-.018-h*.67*math.sin(a))
            vs.append((math.cos(a)*w,y,z));uv.append(uv_body(a if not lower else a+math.pi,y));ws.append({'Jaw' if lower else 'Head':1})
    for j in range(rings):
        for i in range(sides):
            a=j*(sides+1)+i;face=(a,a+1,a+sides+2,a+sides+1);fs.append(tuple(reversed(face)) if lower else face)
    # Flat interior palate, closed at the back, with its own mouth atlas region.
    for j in range(rings):
        ids=[]
        for k in (j,j+1):
            y=-1.22+k/rings*.62;w=profile(y,ys,widths)
            for side in (-1,1):
                ids.append(len(vs));vs.append((side*w,y,-.018 if lower else .012));uv.append(swatch(0));ws.append({'Jaw' if lower else 'Head':1})
        fs.append((ids[1],ids[3],ids[2],ids[0]) if lower else (ids[0],ids[2],ids[3],ids[1]))
    for row in (0,rings):
        cap=len(vs);y=-1.235 if row==0 else -.60
        vs.append((0,y,-.018 if lower else .012));uv.append(swatch(0));ws.append({'Jaw' if lower else 'Head':1})
        for i in range(sides):fs.append((row*(sides+1)+i,row*(sides+1)+i+1,cap))
    mesh(name,vs,fs,uv,ws)
jaw_shell('Blunt head and upper jaw');jaw_shell('Hinged lower jaw',True)

def ellipsoid(name,origin,scale,tint,bone='Head',rings=8,sides=12):
    vs=[];fs=[];uv=[]
    for j in range(rings+1):
        a=j/rings*math.pi
        for i in range(sides+1):
            b=i/sides*math.tau
            vs.append((origin[0]+math.sin(a)*math.cos(b)*scale[0],origin[1]+math.cos(a)*scale[1],origin[2]+math.sin(a)*math.sin(b)*scale[2]));uv.append(swatch(tint))
    for j in range(rings):
        for i in range(sides):
            a=j*(sides+1)+i;fs.append((a,a+1,a+sides+2,a+sides+1))
    return mesh(name,vs,fs,uv,[{bone:1}]*len(vs))
for side,label in [(-1,'Left'),(1,'Right')]:
    ellipsoid('Eye'+label,(side*.142,-.93,.065),(.014,.021,.021),3)
    ellipsoid('Pupil'+label,(side*.153,-.933,.066),(.006,.012,.015),4, rings=6,sides=10)
    ellipsoid('Glint'+label,(side*.159,-.939,.072),(.002,.003,.003),5,rings=4,sides=6)
    # Small raised nostril at the broad snout, not a mammal-like nose.
    ellipsoid('Nostril'+label,(side*.048,-1.183,.052),(.010,.018,.010),4,rings=4,sides=8)
    ellipsoid('Gill opening'+label,(side*.145,-.49,.018),(.004,.015,.023),4,bone='Spine00',rings=4,sides=8)

for lower in (False,True):
    vs=[];fs=[];uv=[];ws=[]
    for side in (-1,1):
        for i in range(8):
            y=-1.12+i*.056;w=profile(y,[-1.22,-1.18,-1.04,-.84,-.60],[.012,.073,.131,.153,.14])*.79
            z=-.018 if lower else .012;direction=1 if lower else -1;length=.017+.011*math.sin((i+1)/9*math.pi)
            start=len(vs)
            for j in range(5):
                a=j/5*math.tau;vs.append((side*w+math.cos(a)*.0045,y+math.sin(a)*.0045,z));uv.append(swatch(2));ws.append({'Jaw' if lower else 'Head':1})
            tip=len(vs);vs.append((side*w*.96,y+.005,z+direction*length));uv.append(swatch(2));ws.append({'Jaw' if lower else 'Head':1})
            for j in range(5):fs.append((start+j,start+(j+1)%5,tip))
    mesh('Lower teeth' if lower else 'Upper teeth',vs,fs,uv,ws)

for bottom in (False,True):
    vs=[];fs=[];uv=[];ws=[];rings=42
    begin=.08 if bottom else -.52
    for j in range(rings+1):
        t=j/rings;y=begin+(1.85-begin)*t;c=center(y);r=body_radius(y)*.95
        height=(.035 if bottom else .055)*math.sin(t*math.pi)**.6
        for offset in (-.0015,.0015):
            for edge in (0,1):
                vs.append(c+Vector((offset,0,(-1 if bottom else 1)*(r+height*edge-.004))))
                uv.append((t,.82+edge*.045));ws.append(body_weights(y))
    for j in range(rings):
        a=j*4;fs.extend([(a,a+4,a+5,a+1),(a+2,a+3,a+7,a+6),(a+1,a+5,a+7,a+3)])
    mesh('Ventral tail ridge' if bottom else 'Continuous dorsal ridge',vs,fs,uv,ws)

armature=bpy.data.armatures.new('MorayRig');rig=bpy.data.objects.new('MorayRig',armature);asset.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armature.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=armature.edit_bones[parent]
bone('Root',(0,-.6,0),(0,-.4,0))
bone('Head',(0,-.6,0),(0,-1.1,0),'Root')
bone('Jaw',(0,-.63,-.018),(0,-.43,-.018),'Head')
for i in range(SPINES):
    bone(f'Spine{i:02d}',center(-.6+i/SPINES*2.45),center(-.6+(i+1)/SPINES*2.45),'Root' if i==0 else f'Spine{i-1:02d}')
bpy.ops.object.mode_set(mode='OBJECT')
parts=[o for o in asset.objects if o.type=='MESH']
for obj in parts:
    modifier=obj.modifiers.new('Moray skinning','ARMATURE');modifier.object=rig;obj.parent=rig
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bpy.context.object.name='MorayVisual'

actions={}
for label,length in [('Idle',72),('Swim',48),('React',48)]:
    rig.animation_data_create();rig.animation_data.action=None
    for frame in range(1,length+2,4):
        phase=(frame-1)/length*math.tau
        for b in rig.pose.bones:
            b.rotation_mode='XYZ';x=y=z=0
            if b.name.startswith('Spine'):
                index=int(b.name[-2:]);t=index/(SPINES-1)
                amp=(.005+.020*t) if label=='Idle' else (.030+.140*t*t)
                z=amp*math.sin(phase-index*.72)
                if label=='React':z*=.2+math.sin(phase*.5)**2
            elif b.name=='Jaw':
                angle=13+3*math.sin(phase) if label=='Idle' else 13+4*math.sin(phase*2)
                if label=='React':angle=13+27*math.sin(phase*.5)**4
                x=math.radians(angle)
            b.rotation_euler=(x,y,z);b.keyframe_insert(data_path='rotation_euler',frame=frame)
    action=rig.animation_data.action;action.name=label;actions[label]=action
    track=rig.animation_data.nla_tracks.new();track.name=label
    strip=track.strips.new(label,1,action);strip.extrapolation='NOTHING';track.mute=True

scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.fps=24
scene.frame_start=1;scene.frame_end=49;rig.animation_data.action=actions['Swim'];scene.frame_set(1)
world=bpy.data.worlds.new('Blue moray studio');world.use_nodes=True;scene.world=world
world.node_tree.nodes['Background'].inputs[0].default_value=(.012,.032,.045,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.4
def light(name,location,energy,color,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.color=color;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=location
    obj.rotation_euler=(Vector((0,.1,0))-obj.location).to_track_quat('-Z','Y').to_euler()
light('Soft key',(1,-1,3),210,(.82,.92,1),3)
light('Warm head fill',(-2,-2,1),100,(1,.85,.62),2)
light('Tail rim',(-1,2,2),260,(.45,.72,1),3)
data=bpy.data.cameras.new('Preview camera');camera=bpy.data.objects.new('Preview camera',data);scene.collection.objects.link(camera);scene.camera=camera
data.type='ORTHO';data.ortho_scale=3.7
def view(position,target=(0,.25,0),scale=3.7):
    camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=scale
view((3,-2,1.7));scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1100;scene.render.resolution_y=680;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
bpy.ops.file.pack_all()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MorayEel.blend'))
# Export all three named clips as separate NLA takes, without a mixed active action.
rig.animation_data.action=None
for track in rig.animation_data.nla_tracks:track.mute=False
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'MorayEel.fbx'),'--collection','MorayAsset','--max-triangles','5000','--max-bones','24','--max-materials','1','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=actions['Swim']
for name,position,frame in [('ThreeQuarter',(3,-2,1.7),13),('Opposite',(-3,-2,1.1),29),('Side',(4,.1,.5),1)]:
    view(position);scene.frame_set(frame);scene.render.filepath=str(RENDERS/(name+'.png'));bpy.ops.render.render(write_still=True)
rig.animation_data.action=actions['React'];scene.frame_set(25);view((2,-2,.65),(0,-.94,.01),.85)
scene.render.filepath=str(RENDERS/'HeadAndJaw.png');bpy.ops.render.render(write_still=True)
rig.animation_data.action=actions['Swim'];view((3,-2,1.7))
scene.render.resolution_x=640;scene.render.resolution_y=396;scene.cycles.samples=8
for frame in range(1,49,2):
    scene.frame_set(frame);scene.render.filepath=str(RENDERS/f'Swim_{frame:03d}.png');bpy.ops.render.render(write_still=True)
rig.animation_data.action=actions['Idle']
view((2,-2,.65),(0,-.94,.01),.85)
for frame in range(1,73,3):
    scene.frame_set(frame);scene.render.filepath=str(RENDERS/f'Idle_{frame:03d}.png');bpy.ops.render.render(write_still=True)
print('MORAY_PREVIEW_COMPLETE '+str(OUT))
