"""Repair the approved squid without rebuilding its silhouette or textures."""
import bpy, math, runpy, sys
from pathlib import Path

root=Path('D:/GIT Project/Underwater-VR')
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/SquidPreview/ReferenceSquid.blend'))
mesh=bpy.data.objects.get('ReferenceSquidVisual')
rig=bpy.data.objects.get('SquidRig')
assert mesh and rig
groups={g.index:g.name for g in mesh.vertex_groups}
# Fin attachments must use the same root/mantle blend as the body. The
# previous 100% mantle root separated from the 70% mantle body as it flexed.
fixed=0
for vertex in mesh.data.vertices:
    weights={groups[g.group]:g.weight for g in vertex.groups}
    fin=sum(w for n,w in weights.items() if n.startswith('Fin'))
    if not any(n.startswith('Fin') for n in weights) and weights.get('Mantle',0)<.99:
        continue
    # Only membrane vertices: body vertices already have a Root weight.
    if 'Root' in weights or weights.get('Mantle',0)+fin<.99:
        continue
    base=1-fin
    mesh.vertex_groups['Mantle'].add([vertex.index],base*.7,'REPLACE')
    mesh.vertex_groups['Root'].add([vertex.index],base*.3,'REPLACE')
    fixed+=1
# Seal the cycle explicitly, including curve tangents. Preserve all authored
# motion but avoid FBX loop interpolation changing direction at the seam.
action=rig.animation_data.action
curves=[]
for layer in action.layers:
    for strip in layer.strips:
        for bag in strip.channelbags:
            curves.extend(bag.fcurves)
for curve in curves:
    start=curve.evaluate(1)
    curve.keyframe_points.insert(49,start,options={'REPLACE'})
    for key in curve.keyframe_points:
        key.interpolation='BEZIER'
        key.handle_left_type='AUTO_CLAMPED'
        key.handle_right_type='AUTO_CLAMPED'
    curve.update()
# Mantle opening is covered by the existing head, but its edge needs the
# same rigid collar motion to avoid a dark moving crack.
for vertex in mesh.data.vertices:
    weights={groups[g.group]:g.weight for g in vertex.groups}
    if 'Mantle' not in weights or any(n.startswith('Fin') for n in weights):
        continue
    y=vertex.co.y
    if y<.25:
        blend=max(0,min(.7,(y+.25)/.5*.7))
        mesh.vertex_groups['Mantle'].add([vertex.index],blend,'REPLACE')
        mesh.vertex_groups['Root'].add([vertex.index],1-blend,'REPLACE')
out=root/'ArtSource/SquidAnimationRepair'
out.mkdir(exist_ok=True)
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ReferenceSquid.blend'))
sys.argv=['export_quest_asset.py','--','--output',str(out/'ReferenceSquid.fbx'),'--collection','SquidAsset','--max-triangles','8000','--max-bones','32','--max-texture','2048','--require-animation']
runpy.run_path('C:/Users/mobin/.codex/skills/blender-marine-models/scripts/export_quest_asset.py',run_name='__main__')
print('SQUID_REPAIR fin_attachment_vertices='+str(fixed)+' curves='+str(len(curves)))
