# Fish-holding hand pose — 18 November 2026

Added a dedicated Fish hold blendshape to both articulated dive gloves, combining opposing thumb/grip curl with index curl into a soft hold. Successful catches crossfade from controller finger poses to the holding pose over approximately 0.125 seconds. Larger fish-body cross sections use a looser curl; smaller bodies use a tighter curl. Release, automatic two-second escape, or lost hand tracking returns control to normal finger animation smoothly.

The pose changes finger vertices only: tracked wrist pose, palm grip anchor, captured fish surface and relative fish orientation remain unchanged. Fingertip contact queries include the new pose deformation. Existing trigger and fist poses remain available when no fish is held.

Validation: Unity 6000.4.5f1, isolated .utmp/DayOneProject, Logs/FishGrabPose-validation.log, exit 0. Both hands passed gradual blending, actual mesh deformation (about 0.0776 m maximum vertex movement), larger-body looser curl, stable wrist/contact anchor, ordinary release and escape-pose release. The glove travel checks, 64 contact-grab cases and existing diver/habitat/creature/hazard/navigation/model regressions passed. Rendered preview inspected: Renders/FishGrabPose/HoldingFish.png.

A size-based pose cannot guarantee zero finger/body intersections for every creature geometry and every hand angle. Worn-headset comfort and appearance still require review.

Delivery: Unity build exit 0, buildResult Succeeded; manifest/ARM64 checks and explicit SDK signature verification passed. APK Builds/FishGrabPose/quest.apk (92,316,417 bytes), SHA-256 43D8AE850422BC6A2A805645CEFC90965C7F4512122D7F454A7B55BE223F0EA8. Updated on Quest 2 with app data preserved and cold-launched successfully. The toolkit's signature-discovery warning is superseded by explicit SDK verification. Existing Unity obsolete-API warnings remain.
