# Creature and grab-pose review — 20 November 2026

Review changes follow EXPERIENCE_REFERENCE.md and the wearer's squid screenshot and grab-pose feedback. They are ready for wearer review, not final visual or performance acceptance.

## Corrections

- Squid: retained the approved silhouette, 30 bones and two shared materials. Matched fin-root weights to the mantle/root blend, stabilized the mantle collar, and sealed the animation cycle. The FBX export audit passes at 6,288 authoring triangles (6,064 imported).
- The single recurring squid remains observation-only. Its trench spawn is `(16, -40.75, 132)` after full-body floor clearance. It hovers along the trench with gentle reversals and a fixed depth instead of orbiting into the sloping shoulders and permanently climbing. The same animal returns in open water; no duplicate was added. The brief specifies distant passing encounters in both habitats, rather than an animal resting on the seabed.
- Shark: continuous tangent patrol, gradual speed-coupled swim playback, and clearance-tested vertical escape directions. Previously flattening those directions reproduced a persistent ledge stall. Fast pursuit still gives advice only; watching and slow swimming remain silent.
- Grab pose: contact fitting now uses the existing size-dependent soft-hold pose instead of overriding it with nearly full finger closure. Each finger still stops at body contact. Both hands, captured fish orientation, tracked roots and the two-second escape remain covered by regression checks.
- Placement audit: checked all 243 schooling fish and other SeaLife actors against the habitat brief. Moved the manta's spawn from the shallow/ledge boundary into the sunlit reef. Removed grabbing from the twelve anglerfish; their individual touch-to-brighten behavior remains. Other required inhabitants already occupy their reference habitats. Full actor coordinates and school centers/radii/counts are in CREATURE_AUDIT_2026_11_20.txt; the brief does not prescribe exact counts for general shoals.
- Surface appearance: connected the existing turtle, manta and shark normal maps at moderate strength, 1024 pixels, mipmapped and Android ASTC 6x6. Retained the squid's 2048 skin atlas; softened excessive relief and made the translucent fins respond to ambient/directional light. This is a focused material pass, not replacement textures for every scene object. Existing fish/reef detail and distance optimizations remain.

## Validation

Unity 6000.4.5f1, isolated `.utmp/DayOneProject`, exit 0. Log: `Logs/CreatureReview-depth-final.log`.

- Squid: six-minute simulation, 58.13 m horizontal movement, final `(14.40, -40.85, 132)`, full-body reef clearance; imported animation loop endpoint difference zero. Four separately baked animation phases rendered for inspection.
- Shark: 180-second patrol, 108.00 m movement, maximum yaw 18.01 degrees/second; collision, pursuit threshold and cooldown checks passed.
- 56 held-fish contact cases cover both hands, representative safe species and four approach sides. Moving holds, free-hand blocking and unchanged tracked roots passed. Earlier grab, imported creature animation, hazard, diver collision, ceiling and teleport regressions passed.
- No additional geometry, realtime lights or per-frame texture generation. These checks do not establish headset FPS or stereo transparency acceptance.

Review images: `Renders/CreatureReview/Squid_0.png` through `Squid_3.png`, and `GrabPose.png`. Confirm the squid collar/fins through a complete swim cycle, the hand pose on small and larger fish, shark ledge recovery, and frame time while turning in all eight habitats before finalizing.

## Delivery

`Builds/CreatureReview-20261120/quest.apk` — 97,779,630 bytes. Unity exit 0, buildResult Succeeded, manifest and ARM64 artifact checks passed. Android SDK apksigner separately verified the APK v2 signature. SHA-256: `F5DCC99E2CB49C9A8F493E4F424FBDA0CD6A89AB01B5B4747C8E25063992E570`.

Installed successfully on the connected Quest 2 with `adb install -r`, preserving app data. Device package lastUpdateTime: 20 November 2026, 12:26:22. Activity launch was requested, but the Quest system logged that an open reprojected OS dialog blocked launch; no running app PID was available. Dismiss the headset system dialog and reopen the app for review. Changed runtime/scene/material source hashes match the isolated build. Existing recoverable licensing diagnostics and obsolete API warnings remain; the toolkit's missing-apksigner warning is superseded by the explicit SDK signature check. ProjectSettings and Packages were not returned from the isolated project.

Wearer visual acceptance and a representative full-route frame-time sample remain pending. Startup logs alone do not establish stable FPS.
