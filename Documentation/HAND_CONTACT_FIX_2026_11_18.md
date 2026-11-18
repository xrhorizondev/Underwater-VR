# Hand orientation and contact-grab correction — 18 November 2026

Reviewed the user's headset image and all 28 seconds of the attached recording through sampled frames. The normal hold displays upright fingers; catches rotate the fish into a universal palm pose; long rigid sleeves look detached. The code also combined left-grip grabbing with paddling.

## Changes

- Added a fixed 90-degree mesh-to-controller grip basis correction before the adjustable fit. Open-hand finger direction now extends forward for the upright controller-grip test instead of along the knuckle line. Existing mirrored palm orientation is retained. The default adjustable rotation is zero.
- Old saved hand-fit values are migrated once to the corrected basis, preventing previous pitch compensation from carrying over. Further fit adjustments remain available and persist under fit version 2. Actual comfort/alignment still requires a wearer check.
- A grab captures the nearest body-volume surface and the fish's relative rotation at that moment. Grab itself makes no position/rotation change. Subsequent motion follows that surface with a bounded, smoothed response and preserves the captured relative orientation. Both flanks, underside and top are supported; there is no forced palm-up pose.
- Release preserves the fish's last orientation; the swimmer resumes naturally. The two-second escape, per-hand hold ownership, grip-release latch, safe-fish eligibility and collision safeguards remain.
- Reduced selection reach from 12 cm to 4 cm outside the existing 3.5 cm hand volume, avoiding distant magnetic catches. Finger trigger pose joins the grip pose during a catch.
- Paddling requires both grips with no held/nearby scoop target. A one-handed grip no longer drives locomotion. Lost controller tracking hides that hand and releases its catch rather than retaining a stale grabbing pose.
- Shortened rigid forearm cylinders to wrist cuffs; existing glove/cuff geometry and the optional torso remain.

The pose distinction follows the OpenXR grip-space definition and the local OculusTouchControllerProfile grip/pointer bindings; the specific mesh basis correction is derived from the authored mesh axes and the user's recording. Reference: https://registry.khronos.org/OpenXR/specs/1.1-khr/html/xrspec.html

## Validation

Unity 6000.4.5f1, isolated .utmp/DayOneProject, Logs/HandContactRevision-final.log: exit 0.

64 contact cases passed across representative fish types and both hands: left/right flanks, belly/top, no initial grab snap, preserved relative orientation, moving/rotating hand contact, no release snap. Mirrored palm and forward-facing open-hand grip-basis assertions passed. Full existing diver/encounter/hazard/navigation/teleport/single-squid/model regressions also passed.

Existing grab fixtures were updated for the closer contact reach. Previous universal palm-up/belly-seat assertions were replaced with captured-surface and relative-pose checks, matching the requested behavior.

Rendered preview inspected: Renders/HandContactRevision/NormalControllerHold.png. This preview uses a simulated upright controller grip; it is not proof of exact alignment with the user's physical hand.

Final APK/deployment verification is recorded below. Existing obsolete Unity API warnings remain. No new worn-headset performance or comfort acceptance is claimed.

Final build: Unity exit 0; buildResult Succeeded; APK manifest and ARM64 checks passed. Explicit SDK apksigner verification passed. Builds/HandContactFix/quest.apk is 92,316,553 bytes. Installed successfully on Quest 2 with app data preserved; cold activity launch Status ok. Startup evidence: Logs/HandContactFix-startup.log. The old saved fit migrates once on this launch. Exact physical-hand alignment and perceived grab comfort remain for wearer review.
