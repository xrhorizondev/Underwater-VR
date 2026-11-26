# Quest clarity and reef flicker

- Native eye resolution (1.0 instead of 0.78) and 4x MSAA instead of 2x. Full texture mip limit and anisotropic filtering explicitly selected at runtime. No sharpening pass, camera depth-texture request or projection-FOV changes.
- 24 scene texture importers changed to trilinear filtering and at least 4x anisotropy, retaining mipmaps and zero mip bias. Existing Android texture sizes/compression preserved. Android previously used per-texture anisotropy; it was not globally disabled.
- Removed 67,268 duplicate or degenerate triangles from 649 unique reef meshes. Original assets and collision meshes retained. Visible mesh bounds, vertices, UVs, normals, materials and placement unchanged. Generated replacements are in `Assets/UnderwaterArt/ClarityOptimization/`. Build-only cleanup removes another 336 redundant triangles from generated distance meshes.
- Exact duplicate face audit: 31,016 source / 32,235 Quest before; two after in both, belonging to the anchor rather than the reef. No identical overlapping mesh instances or renderers controlled by competing LOD groups found.
- Stable LOD selection with 12% hysteresis, staggered checks and immediate teleport refresh. Uses the actual stereo projection scale. Close detail and long visibility ranges retained; no dither fade or simultaneous detail layers introduced.
- Quest reef lighting now respects material caustic strength and attenuates distant caustic/normal contrast to reduce shimmer.

Validation: isolated Unity 6000.4.5f1 exit 0; navigation/collision checks; 100 boundary oscillations without detail thrashing; actual rendered forced-cull test; changed shader compilation; manually inspected previews of all eight habitats. Reports in `Renders/ClarityOptimization/`; logs `Logs/clarity-*.log`.

Quest 2 detected. The initial app-specific baseline had SF=0 and no focused rendering, so its 72 FPS values are not valid performance evidence. A headset-on test is still needed to accept clarity, head-turn stability and GPU cost of the higher resolution/MSAA.

Build: `Builds/ClarityOptimization-20261125/Quest/quest.apk` — 272,278,650 bytes. Unity build/validation exit 0, ARM64 manifest and APK v2 signature verified. Installed with `adb install -r` (app data preserved); cold launch passed. Device startup logs in `Logs/clarity-device-startup.txt` and `Logs/clarity-device-settings.txt`. Current device SF=0 still prevents a valid rendered performance acceptance.
