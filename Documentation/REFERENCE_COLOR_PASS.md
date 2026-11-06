# Reference lighting and color pass

The user-provided coral photographs guided the palette and lighting. They are visual references only; no reference photograph is redistributed as a scene texture.

Changes:
- Removed the previous green cast from water, ambient light, and caustics.
- Introduced a cyan-to-blue water background, brighter surface sunlight, and blue depth fog.
- Balanced neutral-warm direct light against cool shadow fill.
- Added six living-coral color variants: gold, rose, cream, muted green, red, and purple.
- Added irregular red, pink, ochre, and green encrusting patches while retaining scanned surface detail.
- Reduced the lime tint in brain coral and the milky green contribution from light shafts.
- Matched the surface horizon to the water background to avoid a visible edge.

The previous opening-view render is retained in Renders/BeforeColorMatch/01_Sunlit_Reef.png. Updated renders use the same camera viewpoints for comparison. Materials, shaders, and the saved scene contain the changes; the builder reproduces them.

This pass matches the references' color direction and lighting balance within the existing scene. It does not reproduce their geometry, marine species, or photographic composition.

Validation: Unity scene generation and Windows build succeeded with no C# or shader compilation errors. Updated player startup exited with code 0, reporting 45 interactive creatures and 24 teleport anchors. No null/missing-reference errors were found. OpenXR reported no headset available; desktop fallback was tested. Runtime evidence: Logs/reference-color-smoke.log and Renders/06_Runtime.png.
