# Animation and follow-up corrections — 13 November 2026

## Git checkpoint

Before these changes, committed and pushed the validated Day 3/headset-review state as 5f69601 to https://github.com/xrhorizondev/Underwater-VR, branch feature/bug-fix. The pre-existing ProjectSettings/ProjectSettings.asset edit remains uncommitted. Changes described below are subsequent local work.

## Changes

- Used the supplied 5.47-second stingray recording as the motion reference. Increased fin stroke depth, added front-to-back phase delay, stronger tail motion and gentle turn banking. All three rays retain collision-aware swimming. Vertex animation also deforms their shadows; mesh bounds accommodate the stroke.
- Replaced the giant squid's short center-sphere navigation with full-length overlapping capsule clearance covering mantle and animated arms. Horizontal steering stays level; sampled seabed clearance and a retained minimum altitude prevent repeated descent into terrain or vertical contact jitter. The original seabed colliders remain on ReefSolid (layer 8).
- Reattached all ten tentacles on each of 28 jellyfish to the inner bell. Bell and tentacles now pulse around the same jelly-local origin and phase. Rebuilt warning surfaces/outlines against the adjusted geometry.
- Replaced angular anglerfish lure stalks with a smooth 33-sample cubic curve. Retained the visible glowing bulb at its endpoint.
- Removed Lumen's newly added cruiser and both lure/light objects; restored its original SeaLife follow controller and nonstationary setting. The original follow condition applies in zones 3 and later.
- Restored WaterCeiling.shader from pre-review commit d1e269e. Kept the expanded 700-by-880-metre surface mesh instead of reverting to the small original mesh.

## Validation

Unity 6000.4.5f1, isolated .utmp/DayOneProject; final validation exit 0, Logs/animation-final-validation.log.

- Squid: six minutes of full-body clearance checks, 150.88 m horizontal travel, final position (18.96, -26.86, 139.77); stayed below the underwater-depth limit and did not overlap ReefSolid.
- Three rays: six minutes of navigation each, minimum travel and clearance checks passed; stroke properties remained active.
- 28 jellyfish: anchor-to-bell geometry and shared pulse-origin checks passed.
- Lumen: original controller active, no cruiser or lure objects. Expanded surface dimensions verified.
- Previous fish schooling, palm/two-second release, collision, animal body collision, hazard, shell, destination, perimeter and ceiling regressions passed. Twelve anglerfish cruisers retain movement; squid and Lumen now have their intended separate owners.
- Rendered/inspected Renders/Ray_Stroke_0.png through Ray_Stroke_2.png, Jelly_Attached.png, Review_Angler_Lure.png and Review_Water_Surface.png.
- Quest toolkit build exit 0; report status passed and buildResult Succeeded. Manifest/ARM64 payload checks and separate APK v2 signature verification passed. Existing deprecated API/package warnings remain.

## Delivery

Builds/DayThreeAnimationFix/quest.apk — 61,177,567 bytes; ARM64 IL2CPP, existing Android configuration and local test signing.

SHA-256: 94F8F8B8EB6855A26852D6D908373E84D9E43C33BCF14EA8CB478342D4B24ADA.

Updated the connected Quest 3 with adb install -r, preserving app data, and launched UnityPlayerGameActivity successfully. Wearer confirmation of ray motion, squid clearance during the actual dive, jellyfish attachment and restored following remains necessary. Automated checks do not establish device FPS.
