# Quest smoothness test — 28 November 2026

Runtime fixes:
- Reduced shark obstacle-avoidance turn limit from 20 to 18 degrees/second.
- Companion fish now eases yaw at up to 32 degrees/second and pitch at 12, preventing combined turns above the existing 40.1-degree/second regression limit.
- Moved one obstructed shell landing from (0, -11.786321, 32) to (0.8, -11.756709, 32). All 24 shell landings and eight habitat destinations passed after repair.
- Includes the preceding eye-render scale, shared heading and school-turn changes described in SMOOTHNESS_20261128.md. Existing surface-support grab remains.

Validation maintenance:
- Added a release runner that records independent suite results and fails the run if any suite fails.
- Updated legacy assertions for current textured fish eyes, hidden replacement meshes, the current glove palm center, jelly hazards, lionfish contact hull and layer-9 trigger sensors.
- Octopus reaction tests now touch within its current 4.5 cm contact threshold and inspect the current shader motion response.
- The off-scene grip fixture omits the live reef's bounded seabed constraint, matching the focused hand tests.

Fresh validation and build results are recorded in Renders/ReleaseValidation/Results.txt and Builds/Smoothness-Test-20261128/. Focused headset FPS and physical grip feel still need a worn-headset test; automated checks cannot establish either.

Final release validation: Unity 6000.4.5f1 exited 0; all ten suites passed. This includes 56 rendered-hand contact cases, 168 catch/follow cases, independent hands and release/escape latch, 227 schooling fish and 27 featured creatures, all 24 shell landings and eight habitat destinations, perimeter/water ceiling, warnings, animal models/animation, hand bypass, manta stability, coral LODs and shaders. Evidence: Logs/ReleaseValidation-Passed-20261128.log and Renders/ReleaseValidation/.

Quest APK: Builds/Smoothness-Test-20261128/quest.apk (249,174,509 bytes). Build Unity exit 0; report status passed / buildResult Succeeded; manifest and ARM64 artifact checks passed. Explicit apksigner verification exited 0 (v2 signature verified), resolving the toolkit warning that apksigner was not on PATH. Existing obsolete Unity API and compiler command-line warnings remain. APK was built for user installation/testing; no fresh focused device FPS result is claimed.
