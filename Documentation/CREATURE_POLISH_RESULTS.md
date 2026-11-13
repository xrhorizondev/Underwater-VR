# Creature polish — 13 November 2026

## Changes

- Three resting rays use G.lerf's low-poly rig and looping swim clip, with natural muted skin and a tail warning attached to a tail bone. The large entry manta retains its existing detailed body; persistent heading and damped banking remove conflicting rotation and periodic rocking.
- Lumen uses one steering/travel owner. It swims a loose orbit around a slowly moving diver focus, accelerates gradually, respects reef collision, and retains its unlit appearance and grab behaviour.
- Fish eyes are drawn after species pigments. The placement was corrected using a close-up render.
- Octopus replaced with the CC0 3,999-triangle Grab3D mesh, with curled arms/suckers, palette skin and animated arm/colour response. Grouper replaced with Quaternius's 1,588-triangle mesh. Both are stylized low-poly alternatives, not the exact Sketchfab models. Moray dorsal anatomy refined; its jaw/retreat reaction remains.
- A four-voice pool places sounds at nearby animals, including schooling fish and dolphins. Six-metre range, distance fade, gradual owner switching and narration ducking. New dolphin cue is synthesized, not a species field recording.

## Validation

Unity 6000.4.5f1 isolated project; full existing Day 1–4 regression checks passed. Source ProjectSettings edits preserved.

- 120-second follower simulation: 39.57 metres travelled, maximum turn 40.01 degrees/second, no reef penetration.
- Actual rigged ray deformation sampled: body 3,384 vertices, maximum sampled wing displacement 0.1054 in baked mesh coordinates. Three rays each passed six-minute navigation checks.
- Squid six-minute full-body clearance: 150.88 metres horizontal travel; no sand/reef penetration.
- Dolphin nearest separation 3.18 metres; all 25 wreck corridor samples clear.
- 227 fish / nine shoals, grabbing, danger-stage separation, 28 attached jellyfish, bubbles and expanded original water surface passed existing checks.
- Close-up renders reviewed for octopus, grouper, ray and fish eyes.

## Headset review

Final build: `Builds/CreaturePolish/quest.apk`, 73,087,829 bytes. Unity and artifact reports passed; APK Signature Scheme v2 verified separately with Android apksigner. SHA256: `6BDB784E2C078D65C734B49ABACE908E6D3ADF1393F619B813E71644AF292FF0`.

Installed with data preserved on Quest 3 `2G0YC1ZG3H02FT` and launched successfully (PID 29389). Startup logs showed no Unity/AndroidRuntime exceptions or missing audio/animation errors. OpenXR emitted its existing pre-session performance-metrics warning. Final ray visibility bounds are approximately 1.78 × 0.98 × 1.84 metres, rather than the oversized FBX import bounds.

Check Lumen while swimming and turning your head, watch the entry manta through a turn, view ray wings and tail warning, approach/leave the dolphins and a fish school to hear the fade, and inspect octopus/grouper style at arm's length. Device startup checking cannot establish perceived audio quality or stable headset FPS.
