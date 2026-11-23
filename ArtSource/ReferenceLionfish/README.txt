REFERENCE LIONFISH
Authoring: Lionfish.blend; repeatable build_lionfish.py.
Unity prefab: Assets/UnderwaterArt/ReferenceLionfish/ReferenceLionfish.prefab
FBX and 1024-square albedo atlas are beside the prefab.

3,056 source triangles; Unity removes degenerate faces: 2,957 rendered triangles.
20 bones, two skinned meshes/materials. No subdivision. Geometric eyes,
13 dorsal spines, paired pectoral fans, pelvic/anal fins and branching tail.
Optimization removed buried tube caps and reduced spine cross-sections/body
sampling. Fin outlines, eye geometry and animation joints are retained.

Idle, Swim and Breath loop for four seconds. Mouth and opercula breathe in
opposing phases; tail and pectoral joints have delayed strokes.
ReferenceLionfishSwim overrides the three breathing bones independently.
Leave motionRoot empty for swimming in place. Assign the existing navigation
actor to motionRoot to synchronize fin speed with measured world travel;
cruiseSpeed defaults to 0.2 metres/second. This script never moves the actor.
Head faces +Z in the prefab. Native body length approximately 0.41 metres.

One opaque skin material and one two-sided alpha-tested fin material;
depth-writing cutouts avoid transparent blending/sorting. Vertex alpha marks
the glossy eyes. This is a reference-based low-poly recreation: fine markings
are softer than the supplied reference and projections have texture seams.

Validation: Blender geometry audit, seamless clip endpoints, animated jaw/gill
bones, FBX re-import, inspection of both sides/front, Unity clip/material import,
shader compilation and animated bounds sampled at six phases per clip.
This asset has not been performance-tested in a standalone Quest build.
Existing scene placement and existing lionfish actors were not changed.

EXISTING-MESH REPAIR: Lionfish_Repaired.blend / Lionfish_Repaired.fbx.
Original source retained in BeforeRepair. No generator was rerun.
Source vertices: 2229 -> 1757 (21.2% fewer); triangles: 3056 -> 2957 (3.2% fewer).
Unity already discarded these degenerate triangles, so rendered triangles remain 2957.
Original texture file, UV layout, material assignments and custom corner normals preserved.
Unity original/repaired comparison: vertex displacement 0, bounds difference 0 across 18 poses.
A 50% reduction was rejected because it changed surfaces; no new bones/animation added.
Only the existing preview FBX was updated. Scene and prefab transforms were not edited.
See RepairReport.json and Renders/LionfishRepair for seven-view comparisons.
