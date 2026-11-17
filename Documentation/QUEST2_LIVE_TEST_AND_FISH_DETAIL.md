# Quest 2 starting-reef live test and rendering correction

## Captured result

Wearer put on the headset and turned at the starting reef. The corrected background-work/light build was running and focused, not merely the Quest shell. Raw log: Logs/Quest2-LiveTest.log.

- Instrumented app PID 12522, device log time 12:09:40–12:10:15: 36 one-second VrApi samples, average 63.44 FPS, minimum 55, maximum 72, against a 72 Hz display. Average GPU utilization 96.4%.
- Recorder-free app PID 12837, 12:10:54–12:11:05: 12 one-second samples, average 69.5 FPS, minimum 59, maximum 73. Performance varied with the view. No REEF_FRAMES messages in that normal-play process.
- Startup and relaunch samples were excluded from these windows. The earlier recorded 430 ms loading hitch and explicit activity-launch interruption are not treated as steady head-turn measurements.
- The provider's GPU timing is raw/inconsistent with Unity's documented units; the diagnosis uses the coincident VrApi FPS/utilization and view dependence. Unity FrameTimingManager did not supply a valid GPU time. Main-thread recorder values include pacing/wait time.

The previous correction reduced unnecessary CPU work and restored the angler baseline, but did not establish stable FPS. This live test confirms a remaining rendering bottleneck.

## Focused correction

The Android scene audit estimated 1,434,042 visible triangles in the forward view, including 665,184 from Living Fish Skin. Schooling fish had detailed fitted-eye meshes at every distance.

- Android builds now install geometry levels on all 227 schooling fish. Full source meshes remain within at least 2.2 m, with the range increased for larger fish, and always while held. Medium meshes cover the next distance band; distant meshes cover the remaining view.
- Geometry reduction preserves every fitted-eye vertex and tag, body UVs, smooth normals, animation bounds, material and renderer. Existing hand/reef colliders and schooling/grab controllers are unchanged.
- Generated meshes are cached per source mesh in the Android build callback. The saved source scene and authored model assets are untouched. Lumen, anglerfish, turtles, rays, octopus and wreck creatures retain their existing detailed representations.
- Resolution, reef geometry/LOD settings, fish count, texture resolution, lights and interaction behavior are unchanged. No foveation experiment was introduced.
- Callback version advanced to 13 and the player is rebuilt with CODEX_UNITY_CLEAN_BUILD=1 to avoid a stale optimized build scene.

## Validation

Unity 6000.4.5f1, isolated .utmp/DayOneProject, Logs/FishDetail-validation.log, exit 0.

- All 227 fish passed source-eye vertex/tag, UV and normal coverage checks; near and held selection returned the full source mesh.
- Total schooling-fish triangle counts: 967,928 full versus 212,331 distant, a 78.1% reduction for that distance level.
- Estimated forward-view total: 1,434,042 before versus 918,805 after, a 35.9% reduction; fish contribution 665,184 versus 149,947. Side/back estimates stayed effectively unchanged. These are scene-audit estimates, not GPU counter measurements or a claim of FPS improvement.
- Existing fitted-eye, fish motion and grabbing checks passed.

## Delivery and next test

Builds/FishDetailFix/quest.apk — 76,208,967 bytes. Unity and artifact reports passed; APK signature verification passed. SHA-256: 390227536F38F605AC84AA3D8F1465ECEDB73CA7DA4252A78672D57E40C1B3BB. Installed on Quest 2.

Repeat test, PID 13960: the wearer reported “still same.” The short focused log window included 72 FPS samples and a 51 FPS dip, with GPU utilization reaching 94–95%. The focused session ended before a sustained 30-second turning sample. This pass is not accepted as a fix on the basis of the 72 FPS samples.

## Follow-up: unused Android depth texture

ReefOptics.OnEnable requested a camera depth texture. Disabling its grading effect did not clear that request; the only depth-texture consumer found in the project is the desktop OceanGrade shader. Android now strips ReefOptics during scene processing, clears camera depthTextureMode, and prevents ReefOptics from requesting depth on Android. The runtime performance component also explicitly clears the unused request. The normal scene depth buffer remains; the unneeded sampled camera-depth texture is removed. Desktop rendering is unchanged.

Build callback version 14. Logs/AndroidDepthFix-audit.log, Unity exit 0, ANDROID_DESKTOP_DEPTH_REMOVED cameras=1. Visual comparison of full and distant fish meshes was rendered and inspected in Renders/FishDetailFix; source models, close/held detail, renderer/materials and collision remain preserved.

Builds/AndroidDepthFix/quest.apk — 76,209,195 bytes. Unity buildResult Succeeded/status passed; manifest/ARM64 and separate APK signature verification passed. SHA-256: B01E45E6B3BEA8199A1ED172D3F4857405B7921B164098740F5A8C1D3CE066A4. Installed on Quest 2 with app data preserved. No valid focused app sample was available immediately afterward; latest-build FPS acceptance remains pending. New capture path: Logs/Quest2-AndroidDepthLiveTest.log.

Telemetry interpretation reference: https://developers.meta.com/vr/documentation/native/android/ts-logcat-stats/ (GPU utilization and compositor layers). Test conclusions above come from the saved app-specific logs and wearer feedback.

Repeat the starting-reef cold launch and left/right/full head turns in normal play. Capture app-specific VrApi samples, then inspect close/held fish and the transition to distant fish. Stable device performance is pending this new-build comparison. The separately deferred model feedback remains open.

## Wearer-confirmed result

The wearer subsequently reported “Smoother now” on the latest AndroidDepthFix build. App PID 15559 was verified in XR_SESSION_STATE_FOCUSED after launch. Saved app-specific logs: Logs/Quest2-AndroidDepthConfirmed.log.

The most recent 30 one-second samples, device log time 12:31:52–12:32:21, averaged 72.07 FPS, minimum 72 and maximum 73, against 72 Hz. No REEF_FRAMES recorder messages or captured fatal/null-reference errors were found. This supports the reported improvement for this sampled session; it does not establish every habitat, cold-start spike or long-session thermal performance. Additional model feedback and the full eight-zone walkthrough remain outstanding.
