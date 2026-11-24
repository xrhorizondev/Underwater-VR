# Animated moray comparison

Open Assets/Scenes/Moray_Animation_Comparison.unity and press Play, or use Underwater > Open animated moray comparison.

The supplied GLB contained a reference-sheet layout. Extracted the straight side-view body and the large curled body matching the supplied screenshots. The source GLB was not modified. Source UVs, both eyes, mouth geometry, and original 4096 color/normal atlases are retained. Mesh seam duplicates were welded before rigging; original surface shape is retained in the full variants.

| Model | Full triangles | Reduced triangles | Reduction |
| --- | ---: | ---: | ---: |
| Straight | 199,596 | 24,000 | 87.98% |
| Curled | 421,844 | 24,000 | 94.31% |

Each variant has 22 smooth-weighted spine bones, one material, and a four-second FullBodySwim loop. The wave travels from the head toward the tail with increasing amplitude, retaining each authored straight/curled resting shape. No world travel is baked into the animation. Original and reduced pairs use identical rigs and animation.

Blender export audits passed. Unity 6000.4.5f1 imported all four FBXs successfully. Imported bone motion, matching cycle endpoints, triangle counts, texture bindings, several animation phases, front/angled and opposite-side views were checked. Geometry samples in the rest pose showed maximum one-way surface differences of 0.063 mm (straight) and 0.187 mm (curled) at one-metre body length; this is a sampled distance check, not a guarantee of identical pixels or a complete Hausdorff bound. Fine triangulation and shading differences remain possible.

Sources: Straight.blend and Curled.blend; build.py reproduces extraction, rigging, reduction, and audited exports from Imported.blend. Source snapshots retain both meshes sharing their rig; exported FBXs contain only the selected variant and its rig.

Removed four temporary scenes as requested: Lionfish_Preview, Lionfish_Comparison, Lionfish_Bilateral_Comparison, CombJelly_Comparison. Retained Underwater_Realistic and UnderwaterExploration. Gameplay morays were not replaced. No APK or Quest device-performance claim is made.

Preview: Renders/MorayComparison/MoraySwim.mp4
Import report: Renders/MorayComparison/Validation.txt
Surface report: ArtSource/MorayComparison/SurfaceDeviation.json
