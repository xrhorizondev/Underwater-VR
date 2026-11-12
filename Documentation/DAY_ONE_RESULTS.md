# Day 1 implementation and validation — 12 November 2026

## Changes
- Added four invisible ReefSolid perimeter walls at X = ±44 and Z = -16/248, plus a safety floor below the seabed. DiverCollisionMotor rejects teleports outside the assigned dive volume. Existing water ceiling remains Y = 6.5.
- Repaired only two pre-existing blocked teleport shells: wreck shell moved 0.8 m sideways; twilight shell moved 0.4 m sideways with a small floor-height adjustment. No rocks or coral were repositioned.
- Replaced runtime breathing/music/cue sample generation with editor-baked PCM resources. Existing sounds, volumes and continuous music behavior are preserved for this checkpoint; ambience-first changes belong to the later audio pass.
- Moved Quest reef material conversion from the first runtime Awake into the Android scene build callback. Shared source materials remain untouched by the new conversion; built mobile clones have instancing enabled.
- Enabled back-face culling only on closed rock, seabed and scanned coral meshes in Android build materials. Open fans/plates/grass remain two-sided. Compiled UV and world-mapped mobile shader paths separately to avoid unused texture reads/varyings; resolution and draw distance remain unchanged.
- Added fixed-size startup/head-turn metrics. Five-second logs include actual display refresh rate, frame intervals, turn intervals, main-thread timing and GPU data availability. The main-thread recorder includes pacing/wait time; it is not pure CPU workload. The XR provider's GPU value is logged raw because observed units disagree with the API documentation. Use VrApi for authoritative headset FPS/GPU correlation. Strict frame-interval budget counts are not dropped frames; small scheduling jitter is normal.

## Automated validation
Unity 6000.4.5f1, isolated project copy under .utmp/DayOneProject. Unity validation exit 0.
- Existing fish sweep, wall slide, blocked teleport, room-scale protection and seabed checks passed.
- All four perimeter swim/head sweeps and outside-teleport rejection passed.
- Eight habitat menu destinations and all 24 teleport shells clear.
- Water ceiling passed at 6.5.
- Fish regression: 227 fish, nine shoals, 20 simulated seconds; maximum step 0.0247 m, pitch 10 degrees, turn 51.42 degrees/second.
- Clownfish nearest-grab, escape and release regression passed.
- All four baked AudioClip resources load successfully.

## Build
Toolkit installation check passed. ARM64 IL2CPP/Vulkan Quest build passed with the actual Unity process awaited; toolkit exit 0 and Unity report buildResult Succeeded. Output: Builds/DayOne/quest.apk. Local test signing; no store release.
Manifest, ARM64 library and APK v2 signature verified. Toolkit launcher in scratch was corrected to wait for Unity's GUI executable on Windows. Source project settings/packages/XR assets were not changed.

Existing warnings: obsolete Unity discovery APIs; Android Active Input Handling = Both. Current scripts use legacy input APIs, so the backend was preserved pending a dedicated compatibility pass. The log filter also counts recoverable licensing notices as errors; the actual build report passed.

## Headset evidence
Quest 2 connected and the locally signed APK installed successfully.
- Prior installed build: visible reef samples around 56–61 FPS at 72 Hz, GPU utilization about 89–98%.
- First startup-work optimization: a 59-second user-controlled sample showed 72/72 FPS, including turns. A later eye-level sample reproduced about 20 ms frame intervals, so the first sample did not establish a full fix. Camera paths were not matched; these are observed samples, not a controlled benchmark.
- GPU pass: closed-surface culling and separate texture-mapping shader variants. All eight diagnostic teleports completed without errors. Habitat samples: 0 = 190 frames/3 s (worst 17.4 ms); 1 = 215 (17.3 ms); 2 = 216 (16.6 ms); 3 = 216 (15.4 ms); 4 = 216 (14.9 ms); 5 = 216 (15.0 ms); 6 = 216 (14.9 ms); 7 = 216 (17.7 ms). No zone sample exceeded two refresh intervals. Back in the dense reef, sustained turning still produced roughly 18–20 ms intervals, so refinement continued.
- Latest startup boundary: first focused frame at 4.575 s with a 14.7 ms interval. An earlier run reported a 281.2 ms loading-boundary interval. Cold-start consistency still needs a larger sample; do not claim that every startup hitch is eliminated.
- A headset screenshot confirmed textured geometry in both eyes. Exact framing varies with the wearer.

## Final refinement
Android-only LOD thresholds now switch existing scanned coral from middle to far detail at .18 screen fraction (previously .12), and procedural colonies from full to clustered detail at .14 (previously .075). Near touch-distance detail, colony positions, collision, eye scale .78 and the 60 m far plane are preserved.
Unity unexpectedly reused the prior APK for an incremental build after this editor-only callback edit. Added opt-in CODEX_UNITY_CLEAN_BUILD=1 support to the local build entry point and rebuilt with BuildOptions.CleanBuildCache. Confirmed optimization callback execution and a different APK SHA-256.
Final clean build: toolkit/Unity exit 0; manifest, ARM64 and APK v2 signature verified. APK size 55,378,429 bytes; SHA-256 1FA2C230CF828A50B4912021604F5E8820D730416E186D81890F329E444F025E. Installed and launched on Quest 2 in normal exploration mode. Final dense-reef headset comparison pending. Day 2 has not started.

## Reuse
Use Underwater/Apply side boundaries and bake startup audio for the focused editor authoring pass (Underwater.Editor.ReefDayOne.Apply in batch). Validate without authoring via Underwater.Editor.ReefDayOne.Validate. Do not regenerate the full reef. If the source editor is open, run in an isolated copy and return only the intended scene/audio assets. QuestReefBuild now performs mobile material substitution at build time; ReefSoundscape now loads baked resources.
For headset sampling, start the app with Android boolean extra reefTravelTest=true to run the existing eight-zone diagnostic and return to the starting location. Wear the headset and finish Guardian; asleep/system-only frames are not valid app performance data. Normal launches do not enable automated travel.
