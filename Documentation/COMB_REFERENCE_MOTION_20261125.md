# Comb jelly reference animation

Reference: user's `Recording 2026-11-25 141653.mp4`, treated as visual footage. Replaced the small lobe twist with a 4.8-second flexible lobe cycle and gentle visual-body roll. Lobe bases, middle sections and tips bend with delayed phases; oral structures lag behind. Comb-row highlights travel along baked anatomical coordinates instead of world height.

All 25 scene comb jellies use the revised asset. Geometry/UVs remain 21,999 triangles with one tissue material. Rig grows from five to nine bones; added joints support rolling the lobe rims. Gameplay roots, locomotion and non-comb creatures retain their existing owners. Refit bone-following contact sensors and sampled render/seabed-clearance bounds for the new deformation. The clarity/MSAA/LOD optimizations remain in place.

Authoring: `ArtSource/CombReferenceMotion/CombReference.blend`; export audit passed. Integration: `Assets/UnderwaterArt/CombReferenceMotion/`; reproducible scripts `Tools/Blender/comb_reference_motion.py` and `Assets/Ocean/Editor/CombReferenceIntegration.cs`.

Unity 6000.4.5f1 isolated validation exit 0: 2,425 sampled poses; maximum displacement 0.2108 m; loop seam, bounds, root-placement, sensor and general collision checks passed. Inspected front, three-quarter, side and rear views. Preview: `Renders/CombReferenceMotion/CombLobeWave.gif`. Headset visual acceptance remains pending.

Quest artifact: `Builds/CombReferenceMotion-20261125/Quest/quest.apk` — 272,454,189 bytes; Unity exit 0; manifest/ARM64 and APK v2 signature verified. Installed on the connected Quest 2 with app data preserved.
