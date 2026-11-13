# Detailed creatures and complete eye correction

13 November 2026, Unity 6000.4.5f1.

The earlier single-position painted fish eye did not follow the different head shapes. Eleven distinct body meshes now have surface-fitted iris and pupil geometry, merged into the body mesh using its existing material. Both sides of all 242 fish are covered, including the reshaped reef species, clownfish, lionfish, anglers, Lumen and grouper. Pigment/stripe logic cannot overwrite the eyes.

Both loggerhead turtles had pupil surfaces partly buried in their head skin. New eyes are fitted to the actual skin surface at approximately X ±0.09, Y 0.012, Z 0.65. They retain the head-turn and blink deformation. The old buried eye renderers are disabled.

The cartoon grouper is replaced by a wider version of the existing detailed fish anatomy with photographic albedo/normal maps and muted mottled colouring. The octopus has smooth geometry, natural skin, correctly placed eyes and 134 physical sucker rings (32,476 triangles including details). Its FBX orientation is baked into canonical coordinates so animation and surface detail use the correct axes. Each stingray has a closed rounded body, raised eyes, five paired gill slits and tapered tail, with smooth wing and tail motion (10,952 triangles). The moray gains fine skin mottling and small curved tooth rows.

## Checks

- Eye geometry validated on both sides of 242 fish and on both turtles.
- Reviewed close-up renders for all 11 fish body variants, both sides of both turtles, octopus and stingray.
- Full Day 1–4 regression suite passed: reef/seabed collision, tracked head and teleport clearance, danger stages, grabbing, jelly attachment, bubbles, fish schools and creature recovery.
- Squid: 360 seconds / 150.88 metres horizontal movement with full-body clearance.
- Companion: 120 seconds / 39.57 metres; maximum turning 40.01 degrees/second.
- Dolphin pod: minimum separation 3.41 metres; 25/25 wreck passage samples clear.

The fish eye additions use the same material and renderer as each fish body. No additional eye collider or runtime physics objects are added. Perceived realism and headset frame rate still require an in-headset check.

Images: `Renders/Realism`, including `Overview.png`.

## Build and installation

`Builds/RealismEyes/quest.apk`: 74,557,361 bytes. Unity and artifact reports passed; Android apksigner separately verified the v2 signature. SHA256: `052FCB89AA12D31D2FFC3D21E0ED89B7FAA3ADB90B4062450FE9293C0F70EB2D`.

Installed with data preserved and launched on Quest 3 `2G0YC1ZG3H02FT`, PID 32382. Startup logs showed no Unity/AndroidRuntime exceptions, missing meshes or shader errors. Existing optional OpenXR latency and mixed input-handling warnings remain; source project settings were preserved. Source scene matches the built scene apart from serialized whitespace.
