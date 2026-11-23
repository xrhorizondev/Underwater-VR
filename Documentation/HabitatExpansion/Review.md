# Fish habitats and deep ship

Added 30 small fish in five schools of six: Sunlit Reef (clownfish/angelfish), Rocky Ledge (butterflyfish/angelfish), The Wreck (seabass/butterflyfish), Open Water Drift (seabass), Wreck & Rays (seabass/butterflyfish). Existing fish and grab interactions remain. Additional schools scatter and reform on approach.

Added the supplied ship on the trench seabed, 16 metres long, offset from the central navigation route. Original 1,028-triangle silhouette retained; a single static solid mesh collider protects swimming and teleport clearance. No new lights or particles.

Fish lengths: clownfish 10 cm, flame angelfish 12 cm, butterflyfish 16 cm, seabass 28 cm. Corrected butterflyfish source orientation before normalization: its source length is X, not Z.

Performance measures:
- Five small schools, bounded at six fish each; flocking decisions at 10 Hz rather than every frame.
- Android school simulation pauses beyond 28 metres, and swimming pauses while the menu is open.
- Shared meshes/textures/materials and a single-pass Vulkan/stereo shader with instanced tail properties.
- Shadows and reflection/light probes disabled for added fish and ship.
- Fish switch detail meshes at distance, then LOD-cull when too small to see. Close geometry is unchanged.
- New texture atlases capped at 1024 pixels with mipmaps, streaming, and Android ASTC 6x6 compression.

| Model | Close triangles | Medium | Distant |
|---|---:|---:|---:|
| Flame angelfish | 2,568 | 701 | 342 |
| Butterflyfish | 1,202 | 538 | 285 |
| Clownfish | 1,605 | 1,148 | 925 |
| Seabass | 1,532 | 491 | 254 |

The selected source meshes are already low poly; no Blender decimation was necessary. Total additional school geometry at full detail is 49,425 triangles spread over five habitats; it is not all submitted near the diver simultaneously.

Editor checks passed: all 30 fish moved and retained solid clearance over 30 seconds; flocking cadence, distance reductions, material settings, ship budget, scene scripts/materials/teleport count, and collision motor checks passed. Rendered school placement and dark-trench ship inspected.

Quest build and device measurements are recorded separately after completion. Automated habitat travel does not replace a physical head-turn comfort check.
