# Marine texture and animation repair

## Saved changes

- Added two socket-fitted, bone-weighted lionfish eyes with dark pupils and bronze irises. Both sides, front and three-quarter views were inspected. Eyes share the existing skinned renderer; no new gameplay collider or movement owner was added.
- Baked tangent-space normal maps from the original detailed lionfish scan onto its optimized UVs. Added adjustable surface depth, set conservatively for the scan. Lionfish now has 20,576 triangles, 15 bones, three materials and a continuous 4-second source swim loop.
- Fixed imported legacy animation clocks stopping under renderer-based culling. The small bone rigs now advance continuously; renderer mesh culling remains enabled.
- Reviewed all animal material groups, including the trench squid: 27 unique texture assets and eight normal maps. Improved mipmaps, trilinear/anisotropic filtering and high-quality compression, keeping small-animal textures capped at 1024 and featured creatures at their available native size up to 2048. Procedurally shaded animals retain their existing shaders.
- Restored the supplied dolphin's genuine 2048 color/normal textures and connected its original roughness and cavity-occlusion maps. No invented high-resolution detail was added to low-resolution source photos.

## Validation

- Isolated Unity 6000.4.5f1 validation and real Play-mode animation runs exited 0.
- All 30 imported animated animals advanced and visibly deformed during the live check. Lionfish local deformation reached 0.08278 m, with advancing clip time and no sampled loop seam.
- Imported-model regression passed: animated geometry, sand envelopes, warning/contact stages, box jellies, rays and prepared marine assets.
- Habitat validation passed, including 60,000 jelly checks and the two-minute shark patrol.
- 30-second live sand validation: 308 guarded animals, 307 moving, 85,008 clearance checks. The harness waits two seconds for startup before sampling; its immediate-start run sampled an angler before startup clearance had settled.
- Render checks found no error shaders; lionfish front/both-side views and live two-sided animation preview are included here.
- No Quest hardware performance test or APK build was performed. Continuously advancing bone rigs and higher-resolution featured textures need device profiling before making a frame-rate claim.

See texture-audit.json for the per-material inventory, live-motion.txt for live skeleton measurements, sand-playmode.json for the live sand result, and Lionfish_Live.gif for the captured animation preview. The preview switches sides halfway through.
