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
Quest 2 connected and test APK installed successfully. Prior installed build: visible reef samples around 56–61 FPS at 72 Hz with GPU utilization about 89–98%. Updated build: a 59-second sample from 11:05:18–11:06:16 device log time reported 72/72 FPS throughout, with recorded head-turn frames. A headset screenshot confirmed textured reef geometry renders in both eyes. Views were user-controlled and not matched camera paths, so these are observed samples rather than a controlled benchmark.
The first updated run reported a 281.2 ms loading/startup boundary interval; do not claim that startup hitch is eliminated. Later steady windows were generally about 14–15 ms at 72 Hz. Initial diagnostic GPU values were incorrectly scaled by 1000 due to provider units; those values must not be interpreted as milliseconds. The final diagnostic keeps that XR value raw and separates the first loading interval.
The next level-view turning sample reproduced the GPU problem: roughly 20 ms intervals in dense reef views. Thus the 72 FPS sample alone did not establish a fix. The final GPU-specific culling/shader optimization compiled, built and was installed; its all-habitat/level-view headset validation is pending. Final APK size: 55,378,601 bytes. Do not call Day 1 fully complete until this check passes; Day 2 has not started.

## Reuse
Use Underwater/Apply side boundaries and bake startup audio for the focused editor authoring pass (Underwater.Editor.ReefDayOne.Apply in batch). Validate without authoring via Underwater.Editor.ReefDayOne.Validate. Do not regenerate the full reef. If the source editor is open, run in an isolated copy and return only the intended scene/audio assets. QuestReefBuild now performs mobile material substitution at build time; ReefSoundscape now loads baked resources.
For headset sampling, start the app with Android boolean extra reefTravelTest=true to run the existing eight-zone diagnostic and return to the starting location. Wear the headset and finish Guardian; asleep/system-only frames are not valid app performance data. Normal launches do not enable automated travel.
