import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Quaternion
from math import sin, cos, pi

ROOT=Path('D:/GIT Project/Underwater-VR')
OUT=ROOT/'Assets/UnderwaterArt/ReferenceLionfish'
ART=ROOT/'ArtSource/ReferenceLionfish'
RENDER=ROOT/'Renders/ReferenceLionfish'
S=.32
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
collection=bpy.data.collections.new('LionfishAsset');scene.collection.children.link(collection)
reference=bpy.data.images.load(str(ART/'Reference.png'))
atlas=bpy.data.images.new('Lionfish_Albedo',width=1024,height=1024,alpha=True)

def source_uv(p,mode='side',t=0):
    x,y,z=p
    if mode=='front':px,py=255+x*430,300-z*410
    elif mode=='band':px,py=240+t*225,715
    elif mode=='mouth':px,py=90,600
    elif mode=='body':
        samples=[(.462,115,785,798),(.38,185,734,826),(.22,260,662,846),(.02,355,687,846),(-.18,445,714,829),(-.36,530,752,815),(-.44,550,762,806)]
        px,top,bottom=550,762,806
        for a,b in zip(samples,samples[1:]):
            if a[0]>=y>=b[0]:
                q=(a[0]-y)/(a[0]-b[0]);px,top,bottom=[a[j]*(1-q)+b[j]*q for j in range(1,4)];break
        width,center,height=profile(min(.46,max(-.44,y)));q=max(0,min(1,(z-center)/height*.5+.5));py=bottom*(1-q)+top*q
        if 165<px<198 and 724<py<763:px+=34
    else:px,py=110+(.46-y)/1.24*610,775-z*445
    return (px/1536,1-py/1024)
def packed_uv(uv,mode):
    px=uv[0]*1536;py=(1-uv[1])*1024
    if mode=='front':return (.675+(px-10)/490*.315,.02+(500-py)/490*.96)
    return (.01+(px-70)/680*.65,.02+(980-py)/480*.96)
def material(name,fin=False):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=False
    nodes=m.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');tex=nodes.new('ShaderNodeTexImage');tex.image=reference;uv=nodes.new('ShaderNodeUVMap');uv.uv_map='Reference';m.node_tree.links.new(uv.outputs['UV'],tex.inputs['Vector']);m.node_tree.links.new(tex.outputs['Color'],em.inputs['Color']);m.node_tree.links.new(em.outputs[0],out.inputs['Surface']);target=nodes.new('ShaderNodeTexImage');target.image=atlas;nodes.active=target
    return m
skinmat=material('Lionfish skin');finmat=material('Lionfish fin cutout',True)
class Builder:
    def __init__(self,name,material):self.name=name;self.material=material;self.v=[];self.f=[];self.uv=[];self.atlas=[];self.w=[];self.eye=[];self.modes=[]
    def vertex(self,p,weights,mode='side',t=0,eye=0):
        if mode=='body' and p[1]>.265:mode='front'
        i=len(self.v);self.v.append(tuple(Vector(p)*S));uv=source_uv(p,mode,t)
        if eye:uv=((182+(p[1]-.32)/.027*9)/1536,1-(744+(p[2]-.08)/.030*10)/1024)
        self.uv.append(uv);self.atlas.append(packed_uv(uv,mode));self.w.append(weights);self.eye.append(eye);self.modes.append(mode);return i
    def face(self,*v):self.f.append(v)
    def finish(self):
        mesh=bpy.data.meshes.new(self.name);mesh.from_pydata(self.v,[],self.f);mesh.update();obj=bpy.data.objects.new(self.name,mesh);collection.objects.link(obj);obj.data.materials.append(self.material)
        for name,data in [('Reference',self.uv),('UVMap',self.atlas)]:
            layer=mesh.uv_layers.new(name=name)
            for polygon in mesh.polygons:
                modes={self.modes[i] for i in polygon.vertices}
                seam='mouth' if 'mouth' in modes else ('body' if 'front' in modes and len(modes)>1 else None)
                for index in polygon.loop_indices:
                    v=mesh.loops[index].vertex_index
                    if seam:
                        uv=source_uv(Vector(self.v[v])/S,seam);layer.data[index].uv=uv if name=='Reference' else packed_uv(uv,seam)
                    else:layer.data[index].uv=data[v]
        mesh.uv_layers.active=mesh.uv_layers['UVMap'];mesh.uv_layers['UVMap'].active_render=True
        color=mesh.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='POINT')
        for i,e in enumerate(self.eye):color.data[i].color=(1,1,1,e)
        for name in set(k for weights in self.w for k in weights):
            group=obj.vertex_groups.new(name=name)
            for i,weights in enumerate(self.w):
                if weights.get(name,0)>0:group.add([i],weights[name],'REPLACE')
        for polygon in mesh.polygons:polygon.use_smooth=True
        return obj
skin=Builder('Lionfish body eyes spines and opercula',skinmat)
web=Builder('Lionfish thin fin membranes',finmat)

stations=[(.46,.064,-.04,.024),(.38,.112,.012,.175),(.22,.136,.012,.245),(.02,.112,0,.205),(-.18,.076,-.006,.145),(-.36,.028,-.01,.052),(-.44,.02,-.01,.038)]
def profile(y):
    for a,b in zip(stations,stations[1:]):
        if a[0]>=y>=b[0]:
            t=(a[0]-y)/(a[0]-b[0]);t=t*t*(3-2*t);return tuple(a[j]*(1-t)+b[j]*t for j in range(1,4))
    return stations[-1][1:]
def body_weights(y):
    t=max(0,min(1,(-y-.18)/.30));return {'Body':1-t,'TailBase':t}
rows=23;sides=16
for row in range(rows):
    y=.46-row/(rows-1)*.9;width,center,height=profile(y)
    for j in range(sides):
        theta=j/sides*2*pi;p=(cos(theta)*width,y,center+sin(theta)*height);weights=body_weights(y)
        if row<5 and sin(theta)<-.12:
            jaw=max(0,1-row/4)*min(1,-sin(theta)*1.6);weights={'Body':1-jaw,'Jaw':jaw}
        skin.vertex(p,weights,'body')
for row in range(rows-1):
    for j in range(sides):a=row*sides+j;b=row*sides+(j+1)%sides;skin.face(a,b,b+sides,a+sides)
# Open mouth with a real lip ring and recessed interior; no painted-on mouth.
mouth=[]
for ring in range(3):
    ids=[];y=.462-ring*.046;r=1 if ring<2 else .25
    for j in range(sides):
        a=j/sides*2*pi;weights={'Jaw':1} if sin(a)<0 else {'Body':1};ids.append(skin.vertex((cos(a)*.064*r,y,-.04+sin(a)*.024*r),weights,'body' if ring==0 else 'mouth'))
    mouth.append(ids)
for ring in range(2):
    for j in range(sides):skin.face(mouth[ring][j],mouth[ring+1][j],mouth[ring+1][(j+1)%sides],mouth[ring][(j+1)%sides])
skin.face(*reversed(mouth[-1]))

def tube(points,radii,bone=None,mode='band',weights_fn=None,sides=4):
    rings=[]
    for i,p in enumerate(points):
        p=Vector(p);t=i/(len(points)-1);direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)]);direction.normalize();axis=direction.cross(Vector((0,0,1)))
        if axis.length<.01:axis=direction.cross(Vector((1,0,0)))
        axis.normalize();other=direction.cross(axis).normalized();ids=[]
        weights=weights_fn(t) if weights_fn else {'Body':1-t,bone:t} if bone else {'Body':1}
        for j in range(sides):ids.append(skin.vertex(p+(axis*cos(j/sides*2*pi)+other*sin(j/sides*2*pi))*radii[i],weights,mode,t))
        rings.append(ids)
    for a,b in zip(rings,rings[1:]):
        for j in range(sides):skin.face(a[j],a[(j+1)%sides],b[(j+1)%sides],b[j])
    # Tube roots are buried in the body or fin hub: omit invisible end caps.
    skin.face(*rings[-1])

bone_defs=[('Root',None,(0,0,0),(0,.12,0)),('Body','Root',(0,0,0),(0,.28,0)),('TailBase','Body',(0,-.18,0),(0,-.38,0)),('TailMid','TailBase',(0,-.38,0),(0,-.57,0)),('TailTip','TailMid',(0,-.57,0),(0,-.78,0)),('Jaw','Body',(0,.30,-.02),(0,.46,-.04))]
for sign in (-1,1):
    side='L' if sign<0 else 'R';bone_defs.append(('Gill'+side,'Body',(sign*.10,.28,.03),(sign*.13,.10,.025)))
    root=Vector((sign*.115,.15,-.015))
    for j in range(3):bone_defs.append((f'Pectoral{side}{j}','Body' if j==0 else f'Pectoral{side}{j-1}',tuple(root+Vector((sign*.12*j,-.04*j,0))),tuple(root+Vector((sign*.12*(j+1),-.04*(j+1),0)))))
    def fw(t,side=side):
        if t<.25:return {'Body':1-t/.25,f'Pectoral{side}0':t/.25}
        s=(t-.25)/.75*2;i=min(1,int(s));w=s-i;return {f'Pectoral{side}{i}':1-w,f'Pectoral{side}{i+1}':w}
    rays=[]
    for j in range(13):
        t=j/12;tip=Vector((sign*(.19+.36*sin(pi*t)**.7),.29-.66*t,.22*cos(pi*t)-.02));points=[]
        for k in range(5):
            u=k/4;p=root.lerp(tip,u);p.z+=sin(pi*u)*.028;points.append(p)
        tube(points,[.006*(1-k/4)+.0006 for k in range(5)],mode='band',weights_fn=fw,sides=3);rays.append(points)
    for j in range(12):
        for k in range(3):
            indices=[]
            for ray,step in [(j,k),(j+1,k),(j+1,k+1),(j,k+1)]:
                u=step/4;indices.append(web.vertex(rays[ray][step],fw(u),'front'))
            web.face(*indices)
    # Overlapping, independently breathing gill covers.
    g=[]
    for ring in range(4):
        y=.27-ring*.05;ids=[]
        for j in range(7):
            a=-1.12+j/6*2.24;z=.015+sin(a)*(.15-ring*.013);x=sign*(.111+cos(a)*(.024-ring*.002));ids.append(skin.vertex((x,y,z),{'Gill'+side:ring/3,'Body':1-ring/3}))
        g.append(ids)
    for a,b in zip(g,g[1:]):
        for j in range(6):skin.face(a[j],a[j+1],b[j+1],b[j])
    # Geometric eyes, both sides; same skin atlas, gloss tagged in vertex alpha.
    for lat in range(7):
        theta=lat/6*pi
        for lon in range(12):
            a=lon/12*2*pi;skin.vertex((sign*(.131+.020*cos(theta)),.32+.027*sin(theta)*cos(a),.08+.030*sin(theta)*sin(a)),{'Body':1},eye=1)
    start=len(skin.v)-84
    for lat in range(6):
        for j in range(12):a=start+lat*12+j;b=start+lat*12+(j+1)%12;skin.face(a,b,b+12,a+12)
    # Short orbital tentacle and paired chin barbels.
    tube([(sign*.105,.325,.112),(sign*.105,.36,.18),(sign*.095,.37,.245)],[.008,.004,.0005],'Body',sides=3)
    tube([(sign*.025,.45,-.075),(sign*.032,.43,-.13),(sign*.025,.40,-.18)],[.007,.004,.0005],'Jaw',sides=3)
    bone_defs.append(('Pelvic'+side,'Body',(sign*.06,.18,-.13),(sign*.15,-.08,-.28)))
    pv=[]
    for j in range(5):
        t=j/4;tip=Vector((sign*(.07+.11*sin(pi*t)),.15-.34*t,-.13-.20*sin(pi*t)));points=[Vector((sign*.065,.20,-.10)).lerp(tip,k/3) for k in range(4)];tube(points,[.004,.003,.002,.0005],'Pelvic'+side,sides=3);pv.append(points)
    for j in range(4):web.face(*[web.vertex(p,{'Pelvic'+side:1}) for p in [pv[j][0],pv[j][-1],pv[j+1][-1],pv[j+1][0]]])

dorsal=[]
for j in range(13):
    y=.31-j/12*.57;_,center,height=profile(y);base=Vector((0,y,center+height*.95));top=Vector((0,y-.09,[.44,.55,.61,.64,.62,.59,.55,.51,.47,.43,.40,.37,.34][j]));points=[base.lerp(top,k/4)+Vector((0,-.025*sin(pi*k/4),0)) for k in range(5)];bone='Dorsal'+str(min(2,j//5));tube(points,[.008,.006,.004,.002,.0004],bone,sides=3);dorsal.append(points)
for j in range(12):web.face(*[web.vertex(p,{'Body':.25,'Dorsal'+str(min(2,j//5)):.75}) for p in [dorsal[j][0],dorsal[j][2],dorsal[j+1][2],dorsal[j+1][0]]])
for j in range(3):bone_defs.append(('Dorsal'+str(j),'Body',(0,.22-j*.20,.17),(0,.16-j*.20,.43)))
bone_defs.append(('Anal','TailBase',(0,-.20,-.10),(0,-.35,-.25)))

def fan(base,tips,bone):
    rays=[]
    def weights(t):
        if bone!='TailTip':return {bone:1}
        if t<.4:
            q=t/.4;return {'Body':.2*(1-q),'TailBase':.8*(1-q),'TailMid':q}
        q=(t-.4)/.6;return {'TailMid':1-q,'TailTip':q}
    for tip in tips:
        points=[Vector(base).lerp(Vector(tip),k/3) for k in range(4)];tube(points,[.0035,.003,.002,.0005],bone,weights_fn=weights,sides=3);rays.append(points)
    for j in range(len(rays)-1):
        for k in range(3):web.face(*[web.vertex(rays[r][step],weights(step/3)) for r,step in [(j,k),(j,k+1),(j+1,k+1),(j+1,k)]])
fan((0,-.42,-.01),[(0,-.46-.34*cos(-1.05+j/12*2.1),-.01+.245*sin(-1.05+j/12*2.1)) for j in range(13)],'TailTip')
fan((0,-.13,-.07),[(0,-.20-j/7*.25,-.12-.16*sin(j/7*pi)) for j in range(8)],'Anal')
objects=[skin.finish(),web.finish()]
# Correct outward winding before skinning and baking.
for obj in objects:
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')

rigdata=bpy.data.armatures.new('Lionfish anatomy');rig=bpy.data.objects.new('LionfishRig',rigdata);collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,parent,head,tail in bone_defs:
    b=rigdata.edit_bones.new(name);b.head=Vector(head)*S;b.tail=Vector(tail)*S
    if parent:b.parent=rigdata.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
for obj in objects:
    obj.parent=rig;mod=obj.modifiers.new('Anatomical swim','ARMATURE');mod.object=rig
# Bake the reference projections into one UV atlas, not a screen-facing card.
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:obj.select_set(True)
bpy.context.view_layer.objects.active=objects[0];scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.bake.use_clear=False;scene.render.bake.margin=3
bpy.ops.object.bake(type='EMIT')
pixels=list(atlas.pixels)
for i in range(0,len(pixels),4):pixels[i+3]=1 if max(pixels[i:i+3])>.004 else 0
atlas.pixels.foreach_set(pixels);atlas.filepath_raw=str(OUT/'Lionfish_Albedo.png');atlas.file_format='PNG';atlas.save();atlas.pack()
for obj in objects:obj.data.uv_layers.remove(obj.data.uv_layers['Reference'])
for m in [skinmat,finmat]:
    n=m.node_tree.nodes;n.clear();out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');tex=n.new('ShaderNodeTexImage');tex.image=atlas;m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.45;bs.inputs['Metallic'].default_value=0
    if m==finmat:m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);m.surface_render_method='DITHERED'
    else:
        attr=n.new('ShaderNodeVertexColor');attr.layer_name='Color';rough=n.new('ShaderNodeMapRange');rough.inputs['From Min'].default_value=0;rough.inputs['From Max'].default_value=1;rough.inputs['To Min'].default_value=.5;rough.inputs['To Max'].default_value=.12;m.node_tree.links.new(attr.outputs['Alpha'],rough.inputs['Value']);m.node_tree.links.new(rough.outputs['Result'],bs.inputs['Roughness'])
    m.node_tree.links.new(bs.outputs[0],out.inputs['Surface'])

scene.render.fps=30;scene.frame_start=1;scene.frame_end=121
rig.animation_data_create()
def rotate(name,axis,angle):
    bone=rig.pose.bones[name];bone.rotation_mode='QUATERNION';local=bone.bone.matrix_local.to_quaternion().inverted()@Vector(axis);bone.rotation_quaternion=Quaternion(local,math.radians(angle));bone.keyframe_insert(data_path='rotation_quaternion')
for clip in ['Idle','Swim','Breath']:
    action=bpy.data.actions.new(clip);rig.animation_data.action=action
    for frame in range(1,122,2):
        scene.frame_set(frame);phase=(frame-1)/120*2*pi;strength=1 if clip=='Swim' else .35
        for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion()
        if clip!='Breath':
            for index,name in enumerate(['Body','TailBase','TailMid','TailTip']):rotate(name,(0,0,1),[1,3,6,10][index]*strength*sin(phase*2-index*.65))
            for side,sign in [('L',-1),('R',1)]:
                for j in range(3):rotate(f'Pectoral{side}{j}',(0,1,0),sign*(3+j*2)*strength*sin(phase*(2 if clip=='Swim' else 1)-j*.7))
                rotate('Pelvic'+side,(0,1,0),sign*3*strength*sin(phase*2+.6))
            for j in range(3):rotate('Dorsal'+str(j),(0,1,0),2*strength*sin(phase*2-j*.7))
            rotate('Anal',(0,1,0),4*strength*sin(phase*2-1))
        rotate('Jaw',(1,0,0),-1.8-1.6*sin(phase*2))
        rotate('GillL',(0,0,1),-(.8+.7*sin(phase*2+pi)))
        rotate('GillR',(0,0,1),.8+.7*sin(phase*2+pi))
    track=rig.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,1,action);strip.action_frame_start=1;strip.action_frame_end=121;track.mute=True
rig.animation_data.action=None
for track in rig.animation_data.nla_tracks:track.mute=False
# NLA clips exported separately. Preview a combined locomotion + breathing pose.
for track in rig.animation_data.nla_tracks:
    track.strips[0].blend_type='REPLACE';track.mute=track.name!='Swim'

scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Inspection world');scene.world.color=(.045,.045,.045)
def area(name,location,energy,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size;obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=location;obj.rotation_euler=(Vector((0,0,.03))-obj.location).to_track_quat('-Z','Y').to_euler()
area('Key',(.9,1.1,1.2),60,1);area('Fill',(-.8,.3,.6),30,1);area('Rim',(0,-1,.8),40,.8)
camera=bpy.data.objects.new('Inspection camera',bpy.data.cameras.new('Inspection camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=.55
camera.location=(.6,.8,.45);camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler()
def render(name,position,frame=1):
    scene.frame_set(frame);camera.location=position;camera.rotation_euler=(Vector((0,-.035,.03))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(RENDER/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.context.view_layer.objects.active=rig
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig]+objects:obj.select_set(True)
scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(ART/'Lionfish.blend'))
for name,pos in [('ThreeQuarter',(.6,.8,.45)),('Side',(.8,0,.12)),('Front',(0,.9,.14)),('Top',(0,0,1))]:render(name,pos)
for frame in [1,31,61,91]:render('Swim_'+str(frame),(.6,.8,.4),frame)
report={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),'bones':len(rigdata.bones),'materials':2,'texture':[1024,1024],'clips':['Idle','Swim','Breath'],'length_m':1.28*S,'eye_vertices':sum(1 for e in skin.eye if e),'breathing':'Independent jaw and paired opercula; locomotion has no root translation'}
(ART/'ModelReport.json').write_text(json.dumps(report,indent=2));print('REFERENCE_LIONFISH_CREATED',json.dumps(report))
