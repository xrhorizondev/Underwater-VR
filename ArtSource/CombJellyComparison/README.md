# Comb jelly comparison

Scene: Assets/Scenes/CombJelly_Comparison.unity. Open it and press Play. Alternatively use Underwater > Open comb jelly comparison.

Extracted the upper-right upright specimen matching the supplied screenshot from the provided multi-specimen GLB. No polygons crossed the extraction boundary. The supplied GLB remains untouched.

Full detail: 245,902 triangles. Reduced: 21,999 triangles (91.05% reduction). Both share a 2048 RGBA tissue texture, one custom stereo-aware transparent material, a five-bone rig, and a four-second GentlePulse clip. Travel is not baked into the rig. The shader adds moving comb shimmer at runtime. The offline preview primarily demonstrates lobe deformation.

The original blue metallic scan colors were softened into low-opacity lavender tissue while retaining the authored UV comb-row placement. Fine comb elements are preserved in geometry and texture; small surface differences remain after decimation. This is a correction of the supplied scan, not a new anatomical reconstruction.

Validation: Blender export audits passed. Unity 6000.4.5f1 isolated-project import and capture exited 0. Both imported clips deform vertices about 0.025 m with exactly matching loop endpoints. Front, 35-degree, side and rear renders were inspected. The imported triangle counts match export counts. No device performance or stereo transparency claim is made; these require Quest validation. The main gameplay scene and existing lionfish scenes were not modified.

Authoring: ArtSource/CombJellyComparison/CombJelly_Comparison.blend
Repeatable source: ArtSource/CombJellyComparison/build.py
Unity scene builder: Assets/Ocean/Editor/CombJellyComparisonScene.cs
Animation preview: Renders/CombJellyComparison/CombJelly_Pulse.mp4
Validation: Renders/CombJellyComparison/Validation.txt
