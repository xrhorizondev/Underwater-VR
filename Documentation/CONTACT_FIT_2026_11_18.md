# Contact-fitted fish grip — 18 November 2026

Corrects the remaining clipping/edge-attachment visible in the user's native recording com.XRHorizon.UnderwaterExplorationVR-20261118-122948-0.mp4.

## Implementation

- Added a central fish-body grip surface that excludes the long tail and tall, thin dorsal fins from the seating calculation. The caught body settles against the palm with 8 mm clearance while preserving its captured relative orientation and bounded, smooth follow.
- Retained the original glove's index/middle/ring/little/thumb vertex groups from its GLB joint weights in small contact metadata resources. Tools/prepare_glove_contact.mjs regenerates these resources; no new creature or glove model replaces the existing assets.
- Each finger follows its authored curl independently and stops at first body contact. Refined contact limits remove coarse curl steps. Final surface constraints keep fitted hand vertices outside the body proxy during rapid wrist motion.
- The second, unoccupied hand stops visually at a held fish. Its tracked root, rig movement and collision motor are not moved. When the fish is released, normal controller finger animation, visual alignment and visibility bounds are restored.
- Mesh fitting updates rendered normals and visibility bounds. Fingertip queries use the fitted mesh position. Work is restricted to held/nearby held-fish contacts; ordinary distant hands use their existing blendshapes.

## Validation

Unity 6000.4.5f1, isolated .utmp/DayOneProject. Logs/ContactFit-final.log and Logs/ContactFit-release-final.log record the checks.

64 cases cover both hands and representative fish species from left/right/top/belly, moving and turning grips, rendered hand vertices outside the body proxy, second-hand contact blocking, unchanged tracked roots and restored open-hand visibility bounds after release. Earlier 64 no-snap/contact-follow cases and existing diver/creature/hazard/collision/teleport/single-squid/model regressions were also run. Blue-tang grip render inspected at Renders/ContactFit/BodyGrip.png.

The collision checks use a lightweight central body approximation; they do not establish perfect contact against every animated fin or all visual detail. No worn-headset comfort or FPS claim is made. The two-second fish escape and one-grip-per-hand behavior remain.

Final build and headset deployment evidence follows.

Catch acquisition now measures the same central body surface used for seating/finger fitting. Safe-fish fin/tail touch-volume overlap alone no longer determines the catch. The final acquisition/regression run is Logs/ContactFit-acquisition-final.log. Open-hand culling bounds are restored on release.

Final delivery: Unity validation exit 0, Unity build exit 0, buildResult Succeeded; APK manifest/ARM64 and explicit SDK signature verification passed. Builds/ContactFit/quest.apk: 92324601 bytes, SHA-256 5AB43787AF87A2230508202372FB255D8CBC0CFFED0462F37986D457C690BABE. Source/build runtime and scene hashes match; ProjectSettings and Packages have no changes. Installed on Quest 2 using adb install -r with app data preserved; activity cold-launched successfully. The toolkit's signature-discovery warning is superseded by explicit SDK verification; existing obsolete Unity API warnings remain.
