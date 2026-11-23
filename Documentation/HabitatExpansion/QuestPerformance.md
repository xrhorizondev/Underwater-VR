# Quest 2 performance check

Date: 23 November 2026. Unity 6000.4.5f1, built-in renderer, Vulkan/OpenXR, ARM64 IL2CPP, local debug signing.

Final Unity build exit code: 0; build result Succeeded. APK manifest and ARM64 Unity/IL2CPP libraries checked directly. Android apksigner verified the APK's v2 signature. Installed successfully on the connected Quest 2.

APK: Builds/HabitatExpansion/Underwater-Quest2.apk
SHA256: 3AC806F450ED551980FF1D0F29AB9CD2610A6814EDE48F17A738E4315A88D73A

The existing installed app and updated app ran the same automated eight-habitat travel test at a 72 Hz display refresh. Both produced 216 frames per approximately three-second habitat sample (about 72 FPS), with zero intervals longer than two display refreshes in those samples. Updated five-second windows reported a 15 ms rounded-up 95th-percentile frame interval. Baseline habitat worst frame interval: 14.8 ms; updated: 15.2 ms. A later updated shallow-reef window reached 15.7 ms. No Unity runtime exceptions or unsupported shader messages were found in the captured updated-app log.

These are short automated samples. They do not guarantee FPS under all views, thermal conditions, or future additions. FrameTimingManager GPU timings were unavailable, so no GPU timing pass is claimed. Interval-over-budget counters use an exact 13.889 ms threshold and flag small timer jitter even when the app delivers 216 frames per three-second sample; they should not be read as dropped-frame counts.

The first build exposed a pre-existing Imported Marine Skin shader compilation error with Vulkan stereo multiview. Replaced VFACE with tangent-space view direction for two-sided normal orientation; the final build has no shader compilation errors. Existing project-wide obsolete Unity API warnings remain.

Physical head-turn check: pending; current telemetry contains no turn frames and the headset went to sleep after the unattended run.

See QuestBaseline.txt and QuestUpdated.txt for filtered telemetry, and UnityBuildReport.json / ArtifactValidation.json for build and package validation.
