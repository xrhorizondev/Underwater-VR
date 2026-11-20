# Supplied Tripo moray replacement — 20 November 2026

## Latest correction: independently traced lip curves

The wearer's screenshot exposed that the opening still crossed the cheek above the actual mouth on one side. Compared close orthographic renders of the untouched `Imported.blend` (`RawSide.png`, `RawOpposite.png`, `RawFront.png`). The source has asymmetric painted/anatomical lip heights, so a single tilted plane cannot follow both sides. Replaced that plane with two independently traced curves, blended across the mouth width. `MouthTrace.json` stores those source-space curves. The hinge is behind the mouth corner, and the lower jaw now follows the lower curved lip while the upper cheek/eye remain intact.

Rebuilt the lining and tooth positions against that curved boundary. Kept the supplied skin/UVs, five clips, 15-bone rig and existing wreck interaction. Blender export, all-clip FBX round trip and jaw isolation passed: 787 jaw vertices, 13,831 protected vertices, zero non-jaw motion during isolated opening. Side/front/opposite closed/open renders were inspected, including the side shown in the screenshot. Unity isolated validation exited 0 (`Logs/BilateralLip-Unity.log`), retaining placement, colliders, state transitions and animation bounds. Current authoring/round-trip mesh: 18,532 triangles, two materials. Close-up: `Mouth_Opposite_22.png`; animated preview: `MouthCycle.gif`.

Review build: `Builds/MorayLipTrace-20261120/quest.apk`, 103,210,936 bytes. Unity exit 0/Succeeded; manifest/ARM64 checks and explicit SDK APK v2 signature verification passed. SHA-256: `A31C965962B43E3352FF0C072E9C32A8E181E2A81FA44620E80F4C8BB295096B`. Installed successfully on Quest 2 with app data preserved; launch requested. Source scene/model/prefab hashes match the build copy. Existing recoverable licensing diagnostics remain; explicit signature verification supersedes the toolkit's apksigner-discovery warning. This section supersedes the single-plane mouth drafts below. Wearer visual and FPS acceptance remain pending.

## Latest correction: lip alignment and mouth appearance

Re-inspected the head at close range after further wearer feedback. The prior cut did not follow the painted lip crease, and the ellipsoid throat protruded beneath the jaw. Removed that placeholder entirely and rebuilt from the uncut source. The opening now follows a shallower lip crease ending at the mouth corner. Added 96 boundary-edge lip connections, weighted inner roof/floor, a recessed lining and 24 small staggered teeth. Outer skin UVs and PBR maps remain; new inner-mouth UVs use a separate, dark tissue material with ivory teeth. This adds 408 triangles and one material slot rather than stretching skin texture across the interior. The Unity shader darkens tissue separately from teeth under underwater lighting.

Current validation: Blender export/round-trip and Unity validation exit 0 (`Logs/MouthLining-Unity.log`); all five clips and sampled culling bounds pass, and original encounter placement, colliders and behavior remain. Jaw-isolation validation checks 728 jaw/lining vertices and 13,856 protected vertices; protected movement is zero and no weighted vertex extends behind the hinge. Side/front/opposite views were inspected at 0, 8 and 22 degrees. Export now explicitly selects the mouth's Color layer. Unity palette validation (`Logs/MouthLining-color-import.log`, exit 0) verifies 15,405 colored vertices and distinct tissue/tooth values (mouth red range 0.208–0.922). Current mesh: 18,506 triangles, 15 bones, two material slots. Preview: `Renders/TripoMoray/MouthCycle.gif`. These visual changes still require wearer acceptance.

Reproduction for this version: `prepare_tripo_moray.py`, then `line_moray_mouth.py`; do not use the superseded placeholder builder `finish_tripo_moray.py`. Source backup: `BeforeMouthLining.blend`. Updated build: `Builds/MorayMouthLining-20261120/quest.apk`, 103,211,240 bytes, SHA-256 `DCACA32501C417C82FA20F01F42B03A92A845B6C77FCF497AEC88F463667B8C9`. Unity exit 0/Succeeded, manifest/ARM64 checks and explicit SDK APK v2 signature verification passed. Installed successfully on Quest 2 with app data preserved; launch requested. Source/model/material/shader hashes match the isolated build. Existing recoverable licensing diagnostics remain; explicit SDK verification supersedes the toolkit's missing-apksigner warning. Headset visual and FPS acceptance remain pending.

## Latest correction: jaw rig and visible idle

Wearer rejected the earlier cheek-length cut and weak idle. Rebuilt from the original uncut imported mesh. The angled lip separation now stops at the mouth corner; the jaw pivot and weights are confined to that short lower-lip region. `JawIsolationValidation.json` checks 397 jaw vertices and 13,711 protected vertices: opening the jaw 22 degrees moves protected vertices by exactly zero, and no jaw weights extend behind the hinge. The former authoring file is retained as `BeforeJawRepair.blend`.

Increased the travelling spine wave, with four-second Idle and three-second Swim loops. Unity play mode (`Logs/MorayJaw-live.log`, exit 0) measured 7.00 degrees of tail variation and 4.00 degrees of jaw variation over five seconds; the old result was about 1.49/2.00 degrees. The live regression now requires at least 5 degrees of tail motion for this model, preventing the earlier weak idle from passing. The existing runtime rotation limit remains. Reaction/retreat/recovery checks preserve bone lengths and scales.

Blender export, FBX round trip, jaw-isolation check and Unity import/regressions passed. Current mesh: 18,325 Unity triangles, 15 bones, one material, five clips. Loop endpoints match. New previews: `Renders/TripoMoray/Idle.gif`, `Swim.gif`, `React.png` and `WreckMoray.png`. The sections below record the initial import; the parameters and delivery in this correction supersede the earlier weak-idle/long-mouth draft. Headset visual acceptance remains pending.

Updated build: `Builds/MorayJawRig-20261120/quest.apk`, 103,085,444 bytes. Unity exit 0/Succeeded, manifest/ARM64 checks and explicit SDK APK v2 signature verification passed. SHA-256: `79067E04F3A52C02EF297220F851616B99677997F0F4830C29BFA6021E3FBE35`. Installed successfully on Quest 2 with app data preserved; launch requested. Source/build scene, rig and prefab hashes match. Existing recoverable licensing and Editor Search/OpenXR diagnostics remain. No headset FPS claim is made.

Replaced the wreck moray's visible model with the user's `tripo_pbr_model_60e08e23-d8e9-4584-969a-cd2d402f57c5_meshopt.fbx`. Its original base color, OpenGL normal and ORM textures are retained. The earlier original YellowMoray asset remains available, but its visual is removed from this encounter; there is one active replacement visual.

The [MotionCow Sketchfab reference](https://sketchfab.com/3d-models/yellow-moray-eel-d3d42d74c04444a98f477df5c15d7f84) exposes an animated `Take 001` in its viewer but no downloadable animation. The supplied FBX has no skeleton or animation. This integration authors reference-style movement on the supplied mesh; it does not contain MotionCow's original animation data.

## Asset and behavior

- Source mesh: 1,885,696 triangles. Prepared mesh: 18,431 triangles; Unity removes degenerates and imports 18,399. One skinned renderer/material, 15 bones, smooth neighboring skin weights. Original source is retained in `ArtSource/TripoMoray/Imported.blend`; editable optimized rig is `TripoMoray.blend`.
- Five in-place clips: Idle (6 s), Swim (3.5 s), React (2 s), Retreat (2.5 s), Recover (3 s). Body/tail waves travel down the spine. The closed source lip was separated and weighted for breathing/startle, with a recessed dark throat; it is an adaptation of the supplied closed-mouth geometry.
- Existing `YellowMorayMotion` selects and blends clips. The placed eel uses idle and safe reaction/retreat/recovery; Swim is included for free-swim reuse. Existing `WreckCreature` and `ReefSphereBody` still own world travel. Original wreck placement, controller settings, physical/touch colliders, fact narration and safe interaction are preserved.
- Original PBR maps use 2048-pixel import limits, mipmaps and Android ASTC 6x6. A single opaque shader reads the original ORM channels directly. No new realtime lights, colliders or runtime mesh deformation work was added.
- Assets: `Assets/UnderwaterArt/TripoMoray/`, including `TripoMorayEncounter.prefab`. Scene: `Assets/Scenes/Underwater_Realistic.unity`.

## Validation

Blender export audit and FBX round-trip passed. All five clips deform after reimport; loop endpoint differences are zero. Reports: `ArtSource/TripoMoray/TripoMoray.audit.json` and `RoundtripValidation.json`.

Unity 6000.4.5f1 isolated validation exited 0 (`Logs/TripoMoray-Unity-delivery.log`). Verified five deforming clips, sampled animation containment in skin bounds, valid material/shader, original placement/collider/controller preservation, retreat/recovery and prior wreck/navigation/underwater collision regressions.

Actual Unity play-mode check exited 0 (`Logs/TripoMoray-live.log`): five seconds of idle with 1.49 degrees of tail rotation variation and 2.00 degrees of jaw variation; 600 reaction/retreat/recovery steps retained bone translations and scales. Desktop OpenXR unavailability and an existing Unity Editor Search database exception were logged; these did not prevent the motion checks. This is not a device FPS measurement.

Inspected three-quarter, opposite, side, front and reaction renders. Preview: `Renders/TripoMoray/Swim.gif`; wreck view: `Renders/TripoMoray/WreckMoray.png`. Headset appearance, lip adaptation and full-route performance remain subject to wearer review.

Reproduction: `inspect_tripo_moray.py`, `prepare_tripo_moray.py`, `finish_tripo_moray.py`, `validate_tripo_moray.py` under `Tools/Blender/`; Unity integration: `Underwater.Editor.TripoMorayIntegration.Run`.

## Delivery

`Builds/TripoMoray-20261120/quest.apk` — 103,130,828 bytes. Unity exit 0, build report Succeeded, manifest and ARM64 checks passed. Android SDK apksigner separately verified the APK v2 signature. SHA-256: `FA5FB4B814D0658FE1B202EF45263D06D2701E9E006D6733793603E21C568400`.

Installed successfully on the connected Quest 2 with `adb install -r`, preserving app data; package update time: 20 November 2026, 14:51:34. App launch requested. Source scene/model/material/shader hashes match the isolated build. The build retained only recoverable licensing diagnostics; the toolkit's missing-apksigner warning is superseded by explicit SDK verification. No ProjectSettings or Packages changes were returned. Headset visual and FPS acceptance remain pending.
