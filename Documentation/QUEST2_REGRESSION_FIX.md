# Starting-reef work and fish-light correction

Wearer report: FPS problems at the starting reef, especially during head turns; fish lights no longer working. Model feedback will be supplied separately and is outside this correction.

## Findings and changes

- The earlier Android resolution, shadows, coral LOD/material processing and opt-in diagnostics remain in place. This correction does not lower visual quality or enable the previously rejected foveation experiment.
- Later encounter components did not consistently follow the existing Android distance limits. Deep-water reactions, cruisers, wreck actors and the shark now skip their heavy work outside 48 m. The long squid uses 72 m to cover its full visible body. Colliders/renderers stay active; nearby movement continues normally. Lumen continues following from farther away after the twilight encounter begins.
- Fish-body shader updates skip distant unheld fish. The position baseline is refreshed while skipped, preventing a false speed/turn spike when they resume.
- Optional fact cards are created only when revealed. Hidden cards no longer have their position, text colour and material block rewritten every frame. This avoids creating the expanded collection of cards/text meshes at startup.
- The Day 5 angler controller had reduced the existing bulb colour from (.3, 1.4, 1.8) to roughly (.042, .315, .42), and its point-light baseline from .35 to about .165. Restored the original visible baseline. Touch still brightens only the selected fish, with recovery to that baseline.
- Cached the actual bulb renderers once instead of checking names on every renderer each frame. Shark warnings also wait for existing narration to finish.

## Evidence and limits

Unity 6000.4.5f1 in the isolated .utmp/DayOneProject. Logs/RegressionFix-validation.log, Unity exit 0.

- Starting-view range audit: all 48 candidate later-habitat components in this pass are outside their simulation range. Their Update callbacks still perform a cheap range check; their heavier navigation/reaction work is skipped.
- All twelve anglers passed rendered-property checks for the restored baseline and stronger touch glow. Inspected Renders/RegressionFix/Angler_Baseline.png and Angler_Touch.png.
- Day 1–6 behavior, hazard, collision, shell, schooling, grab, creature-motion and ending regressions passed.
- No scene/model changes, render-resolution reduction, broad LOD reduction or normal-play profiler was introduced.

The Quest was asleep and the app was not running during investigation. Available recent telemetry was not sufficient for a valid app/head-turn comparison. The wearer chose to test later. These are verified code/work-count and light-response corrections; restored 72 FPS has not been established on the headset.

## Delivery

Builds/RegressionFix/quest.apk — 75,732,499 bytes, ARM64 IL2CPP. Unity build report passed/Succeeded; manifest and ARM64 artifact checks passed; Android apksigner separately verified the APK v2 signature. SHA-256: 15E8D0244EF0B5E6AE798F220BB7AB5910752656E391C641DBF8D927F7EE6711. Deprecated API/package and recoverable licensing diagnostics remain, without blocking C# or shader build errors.

Installed successfully on Quest 2 with adb install -r, preserving app data; device package lastUpdateTime 2026-11-17 11:23:32. The headset was left for the wearer's later test; no new app/head-turn performance capture was claimed.

Later wearer check: cold-start at Sunlit Reef and repeat left/right/full head turns, then visit The Trench and check persistent angler bulbs plus individual touch brightening. If the issue also concerns Lumen, check its body glow in Twilight Drift; the no-lure preference remains unchanged. Additional model issues are deferred until described.
