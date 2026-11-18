# Approved Blender squid — Unity replacement

Current source state (2026-11-18): User approved the Blender preview again. Replaced the old visual on the single RecurringSquid actor; no duplicate encounter added. Animation, visibility bounds, 180-second full-body patrol clearance and prior scene regression checks passed. Logs/Squid-approved-replacement.log, Unity exit 0. Source scene updated; headset APK has not been rebuilt for this replacement.

The user approved Renders/SquidPreview/Swim.gif. Both existing giant-squid encounters now use one shared FBX visual, retaining separate SquidSwimmer patrols, facts and collision ownership. Source scene: Assets/Scenes/Underwater_Realistic.unity.

- Assets/UnderwaterArt/BlenderSquid/ReferenceSquid.fbx: one skinned renderer per instance, two shared materials, 30 bones and looping Swim clip.
- Blender authoring: 6,288 triangles. Unity import removes degenerate faces and reports 6,064 triangles. Authoring sources and recipe remain in ArtSource/SquidPreview and Tools/Blender/build_squid_preview.py.
- Corrected FBX centimetre/metre skinning transforms and oriented the visual child to the existing mantle-first navigation direction. Visual length remains 22 m, spanning local Z -15 to +7; the gameplay roots are not rotated to compensate for import orientation.
- Shared 1024-pixel body/fin atlases with Android ASTC 6x6 overrides. Body shader is opaque; the separate fin submesh is transparent with stereo-instancing support. No new realtime lights or desktop post effects.
- Old procedural MeshFilter/MeshRenderer components have been removed from both scene actors, rather than merely disabled. The FBX is the sole squid visual. Existing navigation and solid-collision layers remain. Clearance padding was conservatively increased by approximately 1.64 m to cover the broader animated fins. Saved spawns are lifted clear of the seabed using the existing full-body placement method.
- Skin visibility bounds are sampled over twelve animation phases in root-bone space. This asset's import includes a 100x skeleton transform; multiplying baked vertices by the skin transform without normalization gives misleading bounds. Actual rendered bounds and animated vertex containment were checked instead of assuming the imported box is correct.
- Disabled legacy squid renderers no longer receive unused shader-phase property updates. The earlier distant-fish detail and unused Android-depth fixes remain intact.

## Validation

Unity 6000.4.5f1, isolated .utmp/DayOneProject. Logs/BlenderSquid-validation.log, Unity exit 0.

Both instances reference the same mesh and passed clip/30-bone/two-material checks. Sampled deformation was about 1.14 m at world scale. Animated vertices remained inside the corrected visibility box. Each squid simulated 180 seconds with full-body reef clearance: open-water travel 76.75 m; trench travel 80.36 m. Earlier Day 1–6 interaction, hazard, collision, fish eye/grab, teleport and wreck-passage checks passed.

Inspected Renders/UnitySquid/OpenWater.png and Trench.png. The reference-inspired silhouette is an approximation, not a species-accurate reconstruction or scanned asset. Device stereo-fin sorting, final appearance and frame time require a headset check; editor validation does not establish device FPS.

## Delivery

Builds/BlenderSquid/quest.apk — 77,333,225 bytes, ARM64 IL2CPP. Toolkit completed successfully; Unity report passed/Succeeded. Manifest and ARM64 checks passed; Android apksigner separately verified the signature. SHA-256: DFF1AE9B898128628F895AB821DD821A37D197DE9A679BC6F52DBB64FEA41EED. Existing obsolete API/package and recoverable licensing diagnostics remain; no blocking C# or shader errors were found.

Installed successfully on the connected Quest 2 using adb install -r, preserving app data. Device package lastUpdateTime: 2026-11-17 14:21:03. No isolated settings were returned. Source changes remain local until a requested Git checkpoint. Device visual/FPS acceptance remains pending.

Follow-up editor cleanup: removed the old mesh components, made fact cards use the child renderer when needed, and saved Assets/UnderwaterArt/BlenderSquid/GiantSquid.prefab alongside ReferenceSquid.fbx and its textures/materials. Logs/SquidReplacement-validation.log exited 0 with both navigation/deformation and prior regressions passing. This cleanup is saved in the Unity project; the previously installed APK predates this component cleanup but already displays the same FBX replacement.

