# Diver and encounter revision — 18 November 2026

## Changes

- Shark stays in the document's zone 7, Open Water Drift, outside the central travel route at (19, -11, 194). Broad patrol uses gradual speed changes, limited heading changes and subtle banking. Fast pursuit still earns only a caution; watching, slow approach and contact do not cause punishment.
- One squid actor remains. The original trench squid is reused for the later open-water sighting. The recurring component relocates it only when it is over 45 m from the viewer and entering the appropriate encounter habitat. Existing procedural squid visuals are retained.
- 239 safe small fish are scoopable: small schooling fish and small anglerfish, in addition to the existing clownfish. Fish retain their swimmers and surface-touch volumes. Dangerous/observation-only creatures are excluded.
- Left and right hands each hold one fish independently, with the existing two-second escape and grip-release requirement before another catch. The school scoop response now recognizes either palm. Menu opening releases both hands.
- Existing articulated glove meshes now use darker neoprene styling, connected forearm sleeves, and a lightweight fitted torso/BCD strap representation. This is a partial first-person body, not a full skeletal/avatar tracking system.
- A separate mirrored controller-to-hand offset adjusts visuals, palms, fingertips and grip anchors together. Default fit is provisional; physical alignment requires wearer feedback. Offset changes persist locally.
- Optional peripheral mask rim has no lens plane, camera/FOV change or physics. It is OFF by default: the desktop comparison does not establish stereo comfort, and a clear view is preferable until reviewed in the headset.
- Deferred creature-model replacements have not been performed.

## Hand fitting in VR

Open the habitat menu with the left menu button. Hold the right trigger to enter Hand Fit.

- Right stick up/down: glove height.
- Right stick left/right: reach forward/back.
- Hold left trigger while moving right stick left/right: mirrored hand width.
- Left stick up/down: pitch; left/right: mirrored yaw.
- Release the right trigger when aligned; leave the menu to save the fit.

Calibrate while holding both physical controllers naturally. Tracking poses and the collision motor remain the travel owners; the suit is visual only.

## Validation

Unity 6000.4.5f1, isolated project .utmp/DayOneProject.

Logs/DiverRevision-validation.log and Logs/DiverRevision-final.log: Unity exit 0, DIVER_REVISION_VALIDATED. Validated one squid, recurrence into both encounter habitats, full-body squid clearance, 239 eligible scoop targets, independent left/right grabbing and release, two-second escape latch, original nearest-clownfish selection, no outfit colliders, existing hazard/encounter/collision/teleport/navigation/finale regressions, and all 19 prepared model animation/material/bounds checks.

Shark simulated 180 seconds: 107.98 m travel, maximum heading change approximately 18.01 degrees/second, no reef penetration or position jumps. This is simulation evidence, not a claim of perceived realism or device FPS.

Diver appearance was rendered and inspected, then the torso and sleeve joins were refined. Final previews and APK results are recorded below. No headset acceptance or new device performance claim is made; testing remains paused at the user's request.

Final APK: Builds/DiverRevision/quest.apk, 92,313,045 bytes. Unity exit 0; buildResult Succeeded; APK manifest/ARM64 checks passed; explicit SDK apksigner verification exit 0. SHA-256: EC2052A798AA781E800008727E12D7BCCCF0F6E627AC42DCAD576525444DB00E. Source/build scene and runtime file hashes match. ProjectSettings and Packages have no diff. Previews: Renders/DiverRevision. The toolkit's apksigner-discovery warning is superseded by the explicit SDK verification. Existing obsolete-API warnings remain. Unity-generated blank YAML values carry trailing spaces; code-only diff whitespace checks passed. This APK is prepared locally and has not replaced the currently installed headset build; device testing stays paused.

User subsequently requested updating and playing in VR. Verified APK hash and installed with adb install -r successfully on Quest 2; app data preserved. Activity launch returned Status ok, COLD. Startup evidence: Logs/DiverRevision-headset-startup.log. This records deployment, not acceptance of hand fit or full gameplay.
