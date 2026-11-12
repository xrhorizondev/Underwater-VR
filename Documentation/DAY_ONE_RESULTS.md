# Day 1 results — 12 November 2026

## Outcome
The wearer confirmed the final normal-play build is “smooth enough now” after checking the previously stuck forward reef view. Quest 2 telemetry recorded 53 steady one-second samples from 12:15:24–12:16:16 device log time: mean 71.96 FPS, minimum 69, maximum 73, against a 72 Hz display. This is a successful sampled test, not a guarantee of zero spikes across every location or long session. Earlier instrumented tests still showed occasional 22–25 ms frames.

Day 1 collision work is complete and the major forward-view freeze is substantially improved. Day 2 remains separate: improve the three-stage danger system, surface-based distances, tail-only warnings and stereo feedback.

## Delivered changes
- Four invisible ReefSolid perimeter walls at X = ±44, Z = -16/248, plus a safety floor below the seabed. Outside teleports are rejected. The tracked-head ceiling remains Y = 6.5.
- Only two obstructed teleport shells were moved locally: one by 0.8 m, one by 0.4 m with a small floor-height correction. Surrounding rocks/coral were preserved.
- Breathing, music and interaction cues are baked PCM resources instead of main-thread sample generation at startup. Existing sound levels and music behavior were preserved; ambience-first redesign belongs to the later audio pass.
- Quest mobile materials are prepared in the Android scene build callback, using shared mobile clones rather than scanning renderers and swapping shaders on entry.
- Back-face culling applies only to closed rocks, seabed and scanned coral. Open fans, plates and grass remain two-sided. Separate UV/world-mapped shader variants avoid unused texture reads and varyings.
- Android coral LODs preserve close detail, use existing lower-detail meshes earlier, and cull tiny distant colonies sooner. Rocks/passages retain the 60 m camera range; eye scale remains .78. Source creature/colony placement and collision are preserved.
- The nearby water cloud directly seeds 96 settled particles instead of running 70 simulation calls (8.75 simulated seconds) during initialization. Android environmental emitters no longer prewarm their 22-second lifetimes. Nearby particles remain present immediately.
- Normal play has no per-frame Unity recorder, GPU query or periodic diagnostic log. Profiling is explicitly opt-in. Final app capture contained zero REEF_FRAMES periodic messages.

## Validation
Unity 6000.4.5f1 in an isolated project copy, preserving the open source editor.
- Unity collision/particle regression exit 0.
- Fish sweep, wall slide, blocked teleport, room-scale movement and seabed checks passed.
- Four perimeter swim/head sweeps and outside-teleport rejection passed.
- All eight habitat destinations and all 24 teleport shells clear; water ceiling passed at 6.5.
- Fish regression: 227 fish, nine shoals, 20 simulated seconds; maximum step .0247 m, pitch 10 degrees, turn 51.42 degrees/second.
- Nearest clownfish grab, escape and release passed.
- Particle seed count, initial water volume and ages passed with zero warm-up simulation steps.
- All four baked audio resources loaded.
- Earlier eight-zone headset travel test completed, with 215–216 frames per three-second sample in habitats 1–7; habitat 0 exposed the dense forward-view problem subsequently addressed. The final normal-play sample focused on that starting reef.

## Final artifact
Builds/QuestTest/quest.apk — 55,386,093 bytes, local test signing.
Unity build report Succeeded; toolkit/actual awaited Unity exit 0. APK manifest, ARM64 payload and APK v2 signature verified. Installed on connected Quest 2 without clearing app data.
SHA-256: E25DB005720558ACEEDFDBE6277AA6CC356A44BA94BF2EC1D571D63B16209BFA.

Prior APKs remain under Builds/DayOne and Builds/ReefViewTest for comparison. Builds/logs are ignored by Git; source and documentation checkpoints provide rollback history.

## Rejected experiment and limits
The user approved a moderate fixed-foveation test. Although the wearer found clarity acceptable, forward-view performance regressed badly in that test. The prior APK was restored, the experimental runtime/editor code was removed, and the isolated Android XR setting was reset. Source XR settings were never changed. Foveation is not part of this delivered change.
Earlier cold runs showed 281–309 ms initial spikes. Particle pre-simulation and other startup work were removed, but normal-play telemetry does not establish a worst-case cold-start frame bound. Do not claim every startup hitch has been eliminated. No visual/scene regeneration was performed.

Existing warnings remain: deprecated Unity discovery APIs and Android Active Input Handling = Both. Current legacy input calls were preserved. Build log filtering also counts recoverable licensing notices as errors; rely on the actual build result/report and artifact verification.

## Reusable workflow
- Focused authoring: Underwater/Apply side boundaries and bake startup audio, or batch Underwater.Editor.ReefDayOne.Apply. Validate only: Underwater.Editor.ReefDayOne.Validate. Do not regenerate the full reef.
- Unity may reuse a stale player scene after editor-only optimization callback edits. Set CODEX_UNITY_CLEAN_BUILD=1 when such changes need a comparison; the local build entry point then uses BuildOptions.CleanBuildCache. Confirm callback logs and APK hash.
- If Unity is open, build in an isolated copy and return only intended assets. The scratch toolkit launcher must await Unity's GUI process and inspect its actual exit code.
- To enable the Unity frame logger for one launch, place a reef-frame-test marker in Application.persistentDataPath before starting the app; it is consumed on startup. The habitat-travel diagnostic also enables metrics. Normal launches perform neither automatic travel nor recording.
- Habitat travel uses boolean intent extra reefTravelTest=true or a reef-travel-test marker. Remove the travel marker after testing. Wear the headset and finish Guardian; asleep/system-only data is not valid app performance evidence.
- Main-thread recorder values include pacing/wait time. Strict interval-budget counts are not dropped-frame counts. This Quest/OpenXR provider reports raw GPU values with units inconsistent with the API documentation, so diagnostics keep that value raw; correlate against VrApi. Missing FrameTimingManager GPU data is never a pass.
