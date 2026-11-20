# Smooth companion flow — 20 November 2026

Lumen now trails a swimming diver, comes around the side after the diver stops, and hovers in front facing the camera. Swimming again returns it to a trailing position. The existing collision body and follower remain the only travel owner; the diver rig, fish model, materials and no-visible-lure preference are unchanged.

Motion is inferred from filtered horizontal head translation, with separate moving/stopped thresholds and a short idle delay. Looking around while swimming does not redefine the trailing direction. While resting, the front position follows camera direction gradually along an arc, rather than jumping across the diver. The nominal offsets are 2.2 m behind and 1.8 m ahead, slightly below eye level, with subtle vertical hovering. Acceleration is bounded at 0.7 m/s², maximum catch-up speed is 1.5 m/s, and turning is bounded at 40 degrees/second. Terrain avoidance retains its tested escape slope. Teleports do not become a false burst of swimming velocity or teleport the fish.

## Validation

Unity 6000.4.5f1, isolated project, exit 0. `Logs/CompanionFlow-validation.log`.

- At 30 Hz and 72 Hz, tested forward swimming, stopping, front arrival, facing the viewer, a 90-degree resting view turn, resumed swimming in the new direction, and a 64 m habitat teleport.
- Both rates passed trailing/front-position/facing assertions, bounded travel and turning, and collision checks. Minimum viewer distance in these scenarios was 1.28 m; maximum measured turn was 40.04 degrees/second (floating-point tolerance).
- Existing in-scene companion regression passed: 120-second moving-viewer simulation, 21.99 m travel, no reef intersection. Prior creature, underwater collision, hazard and navigation checks included by that validator passed.
- No new geometry, textures, materials, lights or per-frame allocations were introduced. Device appearance and frame-time acceptance still require wearer review.

## Delivery

Updated review APK: `Builds/CompanionFlow-20261120/quest.apk`, 97,780,686 bytes. Unity exit 0, report Succeeded, manifest/ARM64 checks passed, and explicit Android SDK APK v2 signature verification passed. SHA-256: `4A3E508D5B63C5240D244C3538B468BF22F42EA371AFD21DB24405D95005FE41`.

Installed successfully on the connected Quest 2 with app data preserved, package update time 20 November 2026, 13:08:20. Requested app launch for review. Runtime source hash matches the isolated build. Existing recoverable licensing/obsolete API diagnostics remain; the toolkit's missing-apksigner warning is superseded by explicit SDK verification. Wearer confirmation of the flow and device FPS remain pending.
