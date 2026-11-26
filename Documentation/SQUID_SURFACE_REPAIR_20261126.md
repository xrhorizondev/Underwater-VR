# Squid arm surface repair — 26 November 2026

Replaced the ten arm/tentacle tubes in the existing squid visual. Retained the mantle, fins, both eyes, 30-bone rig, Swim animation, materials and gameplay transforms. Roots now extend into the head and blend from Root to arm motion; tips are capped. Continuous per-corner UVs replace the stretched pale atlas swatch strips. Added radial and length segments and a finer taper.

Unity asset: Assets/UnderwaterArt/BlenderSquid/ReferenceSquid.fbx (existing .meta preserved).
Authoring source: ArtSource/SquidSurfaceRepair/ReferenceSquid.blend.
Prior asset remains in ArtSource/SquidUndersideRepair/ReferenceSquid.fbx.

Validation: 7,266 triangles, two materials, 30 bones; export audit passed. FBX round trip: zero opaque boundary edges after welding UV splits, normalized skin weights, 49 finite animation samples, zero loop endpoint vertex error. Inspected front, side and opposite views plus another swim phase. Blender import offsets the exported action to frames 2–50; validation uses the imported action range.

Unity scene rendering and headset appearance have not been verified in this pass. No build or installation performed. Gameplay, scene settings and unrelated edits were preserved.
