# Day 6 — jellyfish, open water and finale

Built on the validated Day 5 scene and the governing EXPERIENCE_REFERENCE.md. Existing world geometry, controls, settings, hazards and earlier creature work are preserved.

## Implemented

- New procedural reef shark with a smooth swept body, asymmetric tail, fins, eyes, gill detail and lateral tail animation. It patrols the open-water side of the route, uses reef-clearance steering and moves away from a nearby diver. This is locally authored geometry, not a scanned shark asset.
- Shark caution is pursuit-only: swim toward it faster than 0.9 m/s for at least 0.75 seconds, within 14 m, in Open Water Drift. Stationary watching, slow approach, retreat and habitat teleport do not count as pursuit. Cue: “Let's not chase it, just watch.” An 18-second cooldown prevents rapid repeats. It has no damage, consequence or grab path.
- Reused the existing complete squid mesh/animation/navigation for a separate distant open-water passage. Placed laterally at X=31, Z=196, heading along the route with a 3 m patrol radius to keep the full animal farther from the central swim path. The trench squid remains intact. Both are observation encounters.
- Added optional short facts/narration to both turtles and the passing squid. Turtle captions now respect the facts setting instead of forcing an automatic caption. New narration uses offline system-voice placeholders, consistent with previous placeholder narration.
- All three rays provide a modest, recovering wing-stroke response when a hand reaches their safe body/wing footprint. Existing independent tail/barb surfaces, warnings and consequences remain separate.
- Added a calm finale grouper using the detailed fitted-eye grouper mesh/material and slow reef-aware patrol, without duplicating the wreck encounter controller.
- Added a one-time calm ending cue after approximately 20 seconds in Wreck & Rays: “We can stay here a while.” Menu time does not advance it; early departure resets the wait. The habitat stays playable, menus and travel remain available, and the cue cannot repeat during the current app session. A new launch begins a new session.
- Existing moon-jelly proximity/caution without consequence and box-jelly full danger stages were preserved and regression-checked. Existing positional ambience and nearby animal audio remain; new brief narration is offline-baked with no runtime speech generation or additional continuous music.

## Validation

Unity 6000.4.5f1, isolated .utmp/DayOneProject. Logs/DaySix-validation.log, Unity exit 0.

- Stationary watching and slow shark approach remain silent; sustained fast pursuit triggers caution; cooldown prevents repeat.
- Shark simulated for 180 seconds, 107.98 m travel, with reef-clearance checks.
- Open-water squid simulated for 180 seconds, 77.09 m travel, with complete-body clearance checks.
- Ray body/wing recognition, tail exclusion, touch response and recovery passed.
- Ending menu pause, early-departure reset, completion and non-repetition passed.
- Day 5 reveal/angler/comb-jelly checks and earlier Day 1–4 regressions passed, including all 24 shells, eight menu destinations, danger stages, collision, squid clearance, fish grab/schooling, dolphin spacing and wreck passage.
- Fitted-eye validation covers 243 fish and both turtles. The new shark's separate eyes are part of its own mesh.
- Inspected captures in Renders/DaySix; moved the passing squid farther from the route after the first render and repeated validation.

## Delivery

Builds/DaySix/quest.apk includes Day 5 and Day 6: 75,731,795 bytes, ARM64 IL2CPP. Unity exited successfully and its report records status passed/buildResult Succeeded. Artifact manifest and ARM64 checks passed; Android apksigner separately verified the APK v2 signature. SHA-256: E077A88B266D38F9D656FB82A7CD1CB97E8B5672A9E74D63FF03A615B676F013.

Deprecated Unity API/package warnings and licensing refresh diagnostics remain; there were no blocking build diagnostics. No device installation or wearer acceptance has been performed.

Use HEADSET_CHECKLIST_DAY_5_6.md for the point-by-point wearer review. Actual headset appearance, comfort, audio balance and CPU/GPU/frame-time profiling remain pending. Known earlier brief details such as the full manta barrel-roll response and exact octopus wake choreography remain final-polish audit items, not silently accepted as complete.

Changes remain local. Pre-existing ProjectSettings edits are preserved; no isolated settings were returned. Next milestone is the full eight-zone acceptance and measured performance pass.
