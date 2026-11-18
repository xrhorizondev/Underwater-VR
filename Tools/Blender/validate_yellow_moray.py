import bpy
import hashlib
import json
import math
from pathlib import Path

ROOT = Path('D:/GIT Project/Underwater-VR/ArtSource/YellowMoray')
SOURCE = ROOT / 'YellowMoray.fbx'
OUTPUT = ROOT / 'RoundtripValidation.json'

def set_frame(frame):
    base = math.floor(frame)
    bpy.context.scene.frame_set(base, subframe=frame - base)
    bpy.context.view_layer.update()

def sample_mesh(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(deps)
    return [tuple(v.co) for v in evaluated.data.vertices]

def max_delta(a, b):
    return max((math.dist(x, y) for x, y in zip(a, b)), default=0)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE), use_anim=True)
meshes = [obj for obj in bpy.data.objects if obj.type == 'MESH']
rigs = [obj for obj in bpy.data.objects if obj.type == 'ARMATURE']
actions = list(bpy.data.actions)
materials = sorted({mat.name for mesh in meshes for mat in mesh.data.materials if mat})
report = {
    'source': str(SOURCE),
    'source_sha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    'blender': bpy.app.version_string,
    'meshes': [obj.name for obj in meshes],
    'materials': materials,
    'armatures': [{'name': rig.name, 'bones': len(rig.data.bones)} for rig in rigs],
    'mesh_checks': [],
    'clips': [],
    'issues': [],
    'tolerances': {'loop_vertex_m': 0.0001, 'root_translation_m': 0.000001, 'minimum_deformation_m': 0.000001},
}
if len(meshes) != 1:
    report['issues'].append(f'Expected one mesh; found {len(meshes)}')
if len(materials) != 1:
    report['issues'].append(f'Expected one material; found {len(materials)}')
if len(rigs) != 1 or sum(len(r.data.bones) for r in rigs) != 15:
    report['issues'].append('Expected one armature with 15 bones')
for mesh in meshes:
    skins = [m for m in mesh.modifiers if m.type == 'ARMATURE' and m.object in rigs]
    check = {'name': mesh.name, 'vertices': len(mesh.data.vertices), 'uv_layers': [uv.name for uv in mesh.data.uv_layers], 'skin_armatures': [m.object.name for m in skins], 'vertex_groups': len(mesh.vertex_groups), 'material_slots': len(mesh.data.materials)}
    report['mesh_checks'].append(check)
    if len(skins) != 1 or not mesh.data.uv_layers:
        report['issues'].append(f'Missing skin or UVs: {mesh.name}')

for rig in rigs:
    rig.animation_data_create()
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    roots = [pb for pb in rig.pose.bones if pb.parent is None]
    for action in actions:
        rig.animation_data.action = action
        if hasattr(action, 'slots') and len(action.slots):
            rig.animation_data.action_slot = action.slots[0]
        start, end = map(float, action.frame_range)
        frames = [start + (end - start) * i / 4 for i in range(5)]
        vertex_samples = []
        root_samples = []
        object_samples = []
        for frame in frames:
            set_frame(frame)
            vertex_samples.append({mesh.name: sample_mesh(mesh) for mesh in meshes})
            root_samples.append({pb.name: tuple(pb.matrix.translation) for pb in roots})
            object_samples.append(tuple(rig.matrix_world.translation))
        deformation = max((max_delta(vertex_samples[0][mesh.name], s[mesh.name]) for s in vertex_samples[1:] for mesh in meshes), default=0)
        loop_error = max((max_delta(vertex_samples[0][mesh.name], vertex_samples[-1][mesh.name]) for mesh in meshes), default=0)
        root_frames = sorted({start, end, *range(math.ceil(start), math.floor(end) + 1)})
        root_samples, object_samples = [], []
        for frame in root_frames:
            set_frame(frame)
            root_samples.append({pb.name: tuple(pb.matrix.translation) for pb in roots})
            object_samples.append(tuple(rig.matrix_world.translation))
        root_translation = {pb.name: max(math.dist(root_samples[0][pb.name], s[pb.name]) for s in root_samples[1:]) for pb in roots}
        object_translation = max(math.dist(object_samples[0], value) for value in object_samples[1:])
        clip = {'name': action.name, 'range': [start, end], 'mesh_sample_frames': frames, 'root_frames_checked': len(root_frames), 'max_deformation_m': deformation, 'loop_endpoint_error_m': loop_error, 'root_bone_translation_delta_m': root_translation, 'armature_translation_delta_m': object_translation}
        report['clips'].append(clip)
        if deformation <= report['tolerances']['minimum_deformation_m']:
            report['issues'].append('No mesh deformation: ' + action.name)
        if not action.name.endswith('Recover') and loop_error > report['tolerances']['loop_vertex_m']:
            report['issues'].append('Loop endpoint mismatch: ' + action.name)
        if object_translation > report['tolerances']['root_translation_m'] or any(value > report['tolerances']['root_translation_m'] for value in root_translation.values()):
            report['issues'].append('Root translation changed: ' + action.name)
expected = {'Idle', 'Swim', 'React', 'Retreat', 'Recover'}
actual = {action.name.rsplit('|', 1)[-1] for action in actions}
if len(actions) != 5 or actual != expected:
    report['issues'].append('Expected five moray takes; found ' + ', '.join(sorted(actual)))
report['status'] = 'failed' if report['issues'] else 'passed'
OUTPUT.write_text(json.dumps(report, indent=2), encoding='utf-8')
print('MORAY_ROUNDTRIP ' + json.dumps(report))

if report['issues']: raise RuntimeError('; '.join(report['issues']))
