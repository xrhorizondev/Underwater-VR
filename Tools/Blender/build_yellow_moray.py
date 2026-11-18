"""Original yellow-olive moray based on the public visual reference; preserves prior source."""
import bpy, bmesh, math, sys, runpy, json
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path('D:/GIT Project/Underwater-VR')
OUT=ROOT/'ArtSource/YellowMoray'; RENDERS=ROOT/'Renders/YellowMoray'
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
back=np.array([.34,.32,.075]);belly=np.array([.66,.51,.15])
pixels=np.ones((1024,1024,4),dtype=np.float32)
base=belly[None,None,:]*(1-dorsal[:,:,None])+back[None,None,:]*dorsal[:,:,None]
base*=((.86+.16*noise)+.08*np.sin(u*29+np.sin(v*46)))[:,:,None]
spotcolor=np.array([.26,.255,.09])
pixels[:,:,:3]=base*(1-spots[:,:,None]*.26)+spotcolor*spots[:,:,None]*.26
mottle=np.sin(u*31+np.sin(v*47)*2.3)*np.sin(v*83+np.cos(u*53)*1.7)
pixels[:,:,:3]*=(.92+.15*mottle+.025*(noise-.5))[:,:,None]
warm=np.clip((.17-v)/.17,0,1)
pixels[:,:,:3]*=(1+warm[:,:,None]*np.array([.15,.035,-.12]))
pixels[830:900,:,:3]*=.72
swatches=[(.08,.027,.022),(.29,.12,.085),(.50,.45,.31),(.12,.20,.23),(.004,.007,.005),(.90,.94,.84)]
for i,color in enumerate(swatches):pixels[910:,i*170:min(1024,(i+1)*170),:3]=color
albedo=save_image('MoraySkin',pixels)
surface=np.zeros_like(pixels);surface[:,:,3]=np.clip(.23+.07*mottle+.07*warm,.12,.4);surface[910:,:,3]=.30
surface_image=save_image('MoraySurface',surface,True)
height=spots*.012+noise*.009
gy,gx=np.gradient(height)
normal=np.dstack((-gx*9,-gy*9,np.ones_like(gx)));normal/=np.linalg.norm(normal,axis=2)[:,:,None]
nmap=np.ones_like(pixels);nmap[:,:,:3]=normal*.5+.5;nmap[910:,:,:3]=(.5,.5,1)
normal_image=save_image('MorayNormal',nmap,True)
material=bpy.data.materials.new('Yellow moray fine-mottled skin');material.use_nodes=True
nodes=material.node_tree.nodes;shader=nodes.get('Principled BSDF')
color=nodes.new('ShaderNodeTexImage');color.image=albedo
material.node_tree.links.new(color.outputs['Color'],shader.inputs['Base Color'])
normal_tex=nodes.new('ShaderNodeTexImage');normal_tex.image=normal_image
nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.25
material.node_tree.links.new(normal_tex.outputs['Color'],nm.inputs['Color'])
material.node_tree.links.new(nm.outputs['Normal'],shader.inputs['Normal'])
shader.inputs['Roughness'].default_value=.64
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
    return Vector((.055*math.sin(t*math.pi*2)*t,y,.015*math.sin(t*math.pi)))
def body_radius(y):
    return profile(y,[-.6,-.3,.25,.8,1.25,1.65,1.85],[.19,.22,.205,.17,.11,.047,.003])
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
    vs=[];fs=[];uv=[];ws=[];rings=14;sides=10
    ys=[-1.22,-1.18,-1.04,-.84,-.60]
    widths=[.07,.12,.155,.18,.19];heights=[.058,.09,.14,.18,.1805]
    for j in range(rings+1):
        y=-1.22+j/rings*.62;w=profile(y,ys,widths);h=profile(y,ys,heights)
        for i in range(sides+1):
            a=i/sides*math.pi
            neck=max(0,min(1,(j/rings-.75)/.25));neck=neck*neck*(3-2*neck)
            z=(.012*(1-neck)+h*math.sin(a)**(.65+.35*neck)) if not lower else (-.018*(1-neck)-h*(.67+.33*neck)*math.sin(a))
            vs.append((math.cos(a)*w,y,z));uv.append(uv_body(a if not lower else a+math.pi,y));ws.append({'Jaw' if lower else 'Head':1-neck,'Spine00':neck})
    for j in range(rings):
        for i in range(sides):
            a=j*(sides+1)+i;face=(a,a+1,a+sides+2,a+sides+1);fs.append(tuple(reversed(face)) if lower else face)
    # Flat interior palate, closed at the back, with its own mouth atlas region.
    for j in range(rings):
        ids=[]
        for k in (j,j+1):
            y=-1.22+k/rings*.62;w=profile(y,ys,widths)
            for side in (-1,1):
                neck=max(0,min(1,(k/rings-.75)/.25));neck=neck*neck*(3-2*neck)
                ids.append(len(vs));vs.append((side*w,y,(-.018 if lower else .012)*(1-neck)));uv.append(swatch(0));ws.append({'Jaw' if lower else 'Head':1-neck,'Spine00':neck})
        fs.append((ids[1],ids[3],ids[2],ids[0]) if lower else (ids[0],ids[2],ids[3],ids[1]))
    for row in (0,):
        cap=len(vs);y=-1.235 if row==0 else -.60
        vs.append((0,y,(-.018 if lower else .012) if row==0 else 0));uv.append(swatch(0));ws.append({'Jaw' if lower else 'Head':1} if row==0 else {'Spine00':1})
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
    ellipsoid('Eye'+label,(side*.163,-.93,.08),(.006,.018,.017),3)
    ellipsoid('Pupil'+label,(side*.169,-.933,.081),(.003,.009,.011),4, rings=6,sides=10)
    ellipsoid('Glint'+label,(side*.172,-.939,.086),(.001,.0015,.0015),5,rings=4,sides=6)
    # Small raised nostril at the broad snout, not a mammal-like nose.
    ellipsoid('Nostril'+label,(side*.048,-1.183,.052),(.010,.018,.010),4,rings=4,sides=8)
    ellipsoid('Gill opening'+label,(side*.194,-.49,.018),(.004,.015,.023),4,bone='Spine00',rings=4,sides=8)

for lower in (False,True):
    vs=[];fs=[];uv=[];ws=[]
    for side in (-1,1):
        for i in range(8):
            y=-1.12+i*.056;w=profile(y,[-1.22,-1.18,-1.04,-.84,-.60],[.065,.115,.16,.19,.19])*.79
            z=-.018 if lower else .012;direction=1 if lower else -1;length=.009+.013*math.sin((i+1)/9*math.pi)+(.006 if i in (1,4) else 0)
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
        height=(.065 if bottom else .10)*math.sin(t*math.pi)**.6 + .10*math.exp(-((t-.84)/.13)**2)*(1-t)**.3
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
joined=bpy.context.object.data;bm=bmesh.new();bm.from_mesh(joined);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0006);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(joined);bm.free();joined.update()

actions={}
for label,length in [('Idle',144),('Swim',72),('React',72),('Retreat',72),('Recover',96)]:
    rig.animation_data_create();rig.animation_data.action=None
    for frame in range(1,length+2,4):
        phase=(frame-1)/length*math.tau
        for b in rig.pose.bones:
            b.rotation_mode='XYZ';x=y=z=0
            if b.name.startswith('Spine'):
                index=int(b.name[-2:]);t=index/(SPINES-1)
                amp=(.003+.035*t*t) if label=='Idle' else (.012+.10*t*t)
                if label=='Retreat':amp=.009+.06*t*t
                if label=='Recover':amp=(.009+.06*t*t)*(1-(frame-1)/length)+(.005+.020*t)*(frame-1)/length
                z=amp*math.sin((-phase if label=='Retreat' else phase)-index*.72)
                if label=='React':z*=.2+math.sin(phase*.5)**2
            elif b.name=='Head':
                x=math.radians(.25)*math.sin(phase);z=math.radians(.3)*math.sin(phase)
            elif b.name=='Jaw':
                angle=2.5+2.5*math.sin(phase) if label=='Idle' else 4+2*math.sin(phase*2)
                if label=='React':angle=5+17*math.sin(phase*.5)**4
                if label=='Retreat':angle=10+2*math.sin(phase)
                if label=='Recover':angle=9-5*(frame-1)/length
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
view((3,-2,1.7));scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1100;scene.render.resolution_y=680;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
bpy.ops.file.pack_all()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'YellowMoray.blend'))
# Export all three named clips as separate NLA takes, without a mixed active action.
rig.animation_data.action=None
for track in rig.animation_data.nla_tracks:track.mute=False
sys.argv=['export_quest_asset.py','--','--output',str(OUT/'YellowMoray.fbx'),'--collection','MorayAsset','--max-triangles','5000','--max-bones','24','--max-materials','1','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=actions['Swim']
for name,position,frame in [('ThreeQuarter',(3,-2,1.7),13),('Opposite',(-3,-2,1.1),29),('Side',(4,.1,.5),1)]:
    view(position);scene.frame_set(frame);scene.render.filepath=str(RENDERS/(name+'.png'));bpy.ops.render.render(write_still=True)
rig.animation_data.action=actions['React'];scene.frame_set(25);view((2,-2,.65),(0,-.94,.01),.85)
scene.render.filepath=str(RENDERS/'HeadAndJaw.png');bpy.ops.render.render(write_still=True)
scene.render.resolution_x=640;scene.render.resolution_y=396;scene.cycles.samples=8
for label,action in actions.items():
    rig.animation_data.action=action;view((3,-2,1.7))
    end=int(action.frame_range[1])
    for index in range(8):
        frame=1+round((end-1)*index/8)
        scene.frame_set(frame);scene.render.filepath=str(RENDERS/f'{label}_{index:02d}.png');bpy.ops.render.render(write_still=True)
print('YELLOW_MORAY_COMPLETE '+str(OUT))


