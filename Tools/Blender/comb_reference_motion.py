"""Refine the existing comb jelly's lobe rig; retain its geometry and UVs."""
import bpy,math,sys,runpy,json
from pathlib import Path
root=Path('D:/GIT Project/Underwater-VR');out=root/'ArtSource/CombReferenceMotion';out.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/CombJellyComparison/CombJelly_Comparison.blend'))
rig=bpy.data.objects['CombJelly_PulseRig'];mesh=bpy.data.objects['CombJelly_Reduced']
for obj in list(bpy.data.objects):
    if obj not in (rig,mesh):bpy.data.objects.remove(obj,do_unlink=True)
rig.animation_data_clear();bpy.context.scene.frame_set(1)
for bone in rig.pose.bones:bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.scale=(1,1,1);bone.location=(0,0,0)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for label,side in [('Left',-1),('Right',1)]:
    for name,head,tail,parent in [
        ('Mid',(0,side*.10,-.075),(0,side*.18,-.17),'Lobe'+label),
        ('Tip',(0,side*.18,-.17),(0,side*.22,-.26),'Lobe'+label+'Mid')]:
        bone=rig.data.edit_bones.new('Lobe'+label+name);bone.head=head;bone.tail=tail;bone.parent=rig.data.edit_bones[parent]
for name,head,tail in [('BodyCrown',(0,0,.02),(0,0,.22)),('BodyWaist',(0,0,-.10),(0,0,.09))]:
    bone=rig.data.edit_bones.new(name);bone.head=head;bone.tail=tail;bone.parent=rig.data.edit_bones['Body']
bpy.ops.object.mode_set(mode='OBJECT')
original={g.index:g.name for g in mesh.vertex_groups}
for label in ['Left','Right']:
    for suffix in ['Mid','Tip']:
        mesh.vertex_groups.new(name='Lobe'+label+suffix)
for vertex in mesh.data.vertices:
    weights={original[g.group]:g.weight for g in vertex.groups if g.group in original}
    for label in ['Left','Right']:
        base='Lobe'+label;amount=weights.get(base,0)
        if amount<1e-6:continue
        # Preserve the old body/lobe blend at the attachment, bend progressively toward the lip.
        progress=max(0,min(1,(.035-vertex.co.z)/.30))
        mid=max(0,min(1,(progress-.18)/.42));tip=max(0,min(1,(progress-.57)/.33))
        mid=mid*mid*(3-2*mid);tip=tip*tip*(3-2*tip)
        mesh.vertex_groups[base].add([vertex.index],amount*(1-mid),'REPLACE')
        mesh.vertex_groups[base+'Mid'].add([vertex.index],amount*mid*(1-tip),'REPLACE')
        mesh.vertex_groups[base+'Tip'].add([vertex.index],amount*mid*tip,'REPLACE')
for name in ['BodyCrown','BodyWaist']:mesh.vertex_groups.new(name=name)
for vertex in mesh.data.vertices:
    weight=next((g.weight for g in vertex.groups if mesh.vertex_groups[g.group].name=='Body'),0)
    blend=max(0,min(1,(vertex.co.z+.01)/.16));blend=blend*blend*(3-2*blend)
    mesh.vertex_groups['Body'].add([vertex.index],0,'REPLACE')
    mesh.vertex_groups['BodyCrown'].add([vertex.index],weight*blend,'REPLACE')
    mesh.vertex_groups['BodyWaist'].add([vertex.index],weight*(1-blend),'REPLACE')
fps=30;frames=144
for frame in range(frames+1):
    t=frame/frames*math.tau
    for bone in rig.pose.bones:
        bone.rotation_euler=(0,0,0)
        bone.scale=(1,1,1)
        if bone.name.startswith('Lobe'):
            side=1 if 'Left' in bone.name else -1
            delay=.42 if bone.name.endswith('Mid') else .82 if bone.name.endswith('Tip') else 0
            # Inward cup, then a softer opening; delayed joints make the rim roll rather than flap rigidly.
            wave=(1-math.cos(t-delay))*.5-(1-math.cos(-delay))*.5
            degrees=12 if bone.name.endswith('Mid') else 16 if bone.name.endswith('Tip') else 9
            bone.rotation_euler.x=side*math.radians(degrees)*wave
            bone.rotation_euler.z=math.radians(1.2)*math.sin(t-delay)
        elif bone.name.startswith('Oral'):
            side=1 if 'Left' in bone.name else -1
            bone.rotation_euler.x=side*math.radians(6)*math.sin(t-.55)
        elif bone.name=='BodyCrown':
            breath=math.cos(t)
            bone.scale=(1+.10*breath,1-.06*breath,1+.16*breath)
            bone.rotation_euler.x=.06*math.sin(t)
            bone.rotation_euler.z=.03*math.sin(t+.4)
        elif bone.name=='BodyWaist':
            breath=math.cos(t-.55)
            bone.scale=(1+.08*breath,1-.04*breath,1+.12*breath)
            bone.rotation_euler.x=-.04*math.sin(t-.5)
        bone.keyframe_insert(data_path='rotation_euler',frame=frame+1,group=bone.name)
        bone.keyframe_insert(data_path='scale',frame=frame+1,group=bone.name)
rig.animation_data.action.name='CombLobeWave';scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=frames+1;scene.render.fps=fps;scene.frame_set(1)
collection=bpy.data.collections.new('CombReferenceAsset');scene.collection.children.link(collection)
for obj in [mesh,rig]:
    for old in list(obj.users_collection):old.objects.unlink(obj)
    collection.objects.link(obj)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'CombReference.blend'))
sys.argv=['export','--','--output',str(out/'CombReference.fbx'),'--collection','CombReferenceAsset','--max-triangles','22010','--max-bones','12','--max-texture','2048','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('COMB_REFERENCE_EXPORTED bones='+str(len(rig.data.bones))+' frames='+str(frames+1))
