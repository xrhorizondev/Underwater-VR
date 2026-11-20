import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path('D:/GIT Project/Underwater-VR');OUT=ROOT/'ArtSource/OctopusRepair'
def load_source():
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'Source.glb'));return max([o for o in bpy.data.objects if o.type=='MESH'],key=lambda o:len(o.data.vertices))
source=load_source();basis=[tuple(v.co) for v in source.data.vertices];shapes={k.name:[tuple(v.co) for v in k.data] for k in source.data.shape_keys.key_blocks};weights=[{source.vertex_groups[g.group].name:round(g.weight,7) for g in v.groups} for v in source.data.vertices]
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Octopus.blend'));obj=bpy.data.objects['Octopus_Mesh'];indices=json.loads((OUT/'RetainedVertexIndices.json').read_text());error=0
for key in obj.data.shape_keys.key_blocks:
 for i,old in enumerate(indices):error=max(error,math.dist(tuple(key.data[i].co),shapes[key.name][old]))
for i,old in enumerate(indices):
 actual={obj.vertex_groups[g.group].name:round(g.weight,7) for g in obj.data.vertices[i].groups}
 if actual!=weights[old]:raise RuntimeError('Original bone weights changed')
if error>1e-6:raise RuntimeError('Original morph coordinates changed')
# Export/import validation exercises FBX rig AND morph animation, not just source poses.
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/'Octopus.fbx'))
obj=next(o for o in bpy.data.objects if o.type=='MESH');rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');scene=bpy.context.scene
if not obj.data.shape_keys or len(obj.data.shape_keys.key_blocks)!=3:raise RuntimeError('Source morphs lost in FBX')
def points():
 bpy.context.view_layer.update();o=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());return [o.matrix_world@v.co for v in o.data.vertices]
start_frame,end_frame=map(int,rig.animation_data.action.frame_range)
scene.frame_set(start_frame);first=points();maxmove=0;maxedge=0;meaningful_edge=0;root=rig.matrix_world.translation.copy();rootdrift=0;shape_ranges={k.name:[k.value,k.value] for k in obj.data.shape_keys.key_blocks[1:]}
for frame in range(start_frame,end_frame+1,5):
 scene.frame_set(frame);p=points();maxmove=max(maxmove,max((a-b).length for a,b in zip(first,p)));rootdrift=max(rootdrift,(rig.matrix_world.translation-root).length)
 if not all(math.isfinite(c) for v in p for c in v):raise RuntimeError('Nonfinite animated vertices')
 for edge in obj.data.edges:
  a,b=edge.vertices;rest=(first[a]-first[b]).length
  if rest>.0001:maxedge=max(maxedge,(p[a]-p[b]).length/rest)
  if rest>.005:meaningful_edge=max(meaningful_edge,(p[a]-p[b]).length/rest)
 for k in obj.data.shape_keys.key_blocks[1:]:shape_ranges[k.name][0]=min(shape_ranges[k.name][0],k.value);shape_ranges[k.name][1]=max(shape_ranges[k.name][1],k.value)
if maxmove<.01 or rootdrift>.0001:raise RuntimeError('Motion missing or animation root travels')
if not any(v[1]-v[0]>.01 for v in shape_ranges.values()):raise RuntimeError('Morph animation not exported')
scene.frame_set(end_frame);end=points();loop_error=max((a-b).length for a,b in zip(first,end))
if loop_error>.00001:raise RuntimeError('Octopus loop endpoint mismatch '+str(loop_error)+' fps='+str(scene.render.fps)+' actions='+str([(a.name,list(a.frame_range)) for a in bpy.data.actions]))
report={'status':'passed','originalMorphCoordinateError':error,'originalWeightsUnchanged':True,'framesSampled':len(range(start_frame,end_frame+1,5)),'bones':len(rig.data.bones),'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'maxDeformationMeters':maxmove,'maxEdgeLengthRatio':maxedge,'maxEdgeRatioAbove5mm':meaningful_edge,'loopEndpointDifferenceMeters':loop_error,'rootDriftMeters':rootdrift,'shapeRanges':shape_ranges};(OUT/'AnimationValidation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
