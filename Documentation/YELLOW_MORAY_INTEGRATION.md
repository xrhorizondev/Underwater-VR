# Yellow moray — original low-poly animated Unity asset

Visual references: the user's Sketchfab Yellow Moray Eel link by MotionCow (https://sketchfab.com/3d-models/yellow-moray-eel-d3d42d74c04444a98f477df5c15d7f84) and Recording 2026-11-18 125803.mp4. This is an independently authored approximation derived from the project's earlier original moray recipe. No proprietary Sketchfab mesh, texture or animation was downloaded or incorporated. The original preview asset remains unchanged in ArtSource/MorayPreview.

## Delivery

- ArtSource/YellowMoray/YellowMoray.blend: editable Blender source, rig, packed textures and all five actions.
- Assets/UnderwaterArt/YellowMoray/YellowMoray.fbx: imported animated model.
- Assets/UnderwaterArt/YellowMoray/YellowMorayEncounter.prefab: reusable encounter with existing safe behavior, collision and fact narration.
- Assets/Scenes/Underwater_Realistic.unity: replaced only the existing moray's visible model at the zone 3 wreck stern opening. The original world placement, interaction controller, collision settings, narration and navigation ownership remain. Legacy renderers are disabled, with their referenced objects retained.

4052 triangles, 15 rig bones, one skinned mesh/material, 1024-pixel albedo and normal maps. The welded neck, weighted jaw root, eye meshes, pupils, teeth, nostrils, gill openings and tapered fin ridges are retained. Low-poly topology is authored directly; no aggressive whole-model decimation was applied.

Clips: Idle (3 s), Swim (2 s), React (2 s), Retreat (2 s), Recover (3 s). All are in-place. Existing WreckCreature/ReefSphereBody movement retreats and returns the moray when approached; YellowMorayMotion crossfades its visual clips. Swim is included for reuse, while the placed wreck encounter remains anchored to its established home.

The reference's fine anatomical/skin detail is approximated at this budget; this is not a byte-for-byte or exact visual replica.

## Validation

Blender 5.3.0 export audit and FBX round trip passed: ArtSource/YellowMoray/YellowMoray.audit.json and RoundtripValidation.json. All five clips deform after reimport; root/armature translation stays fixed. Loop endpoints pass for looping clips; Recover is intentionally a non-looping transition.

Unity 6000.4.5f1 isolated validation exited 0. Logs/YellowMoray-final-validation.log: triangles=4052, length=2.427698 m, clips=5, all sampled skinned world vertices contained in visibility bounds, one valid textured material, reusable prefab present. Existing moray reaction/retreat/recovery and wreck/habitat regressions passed. Placement/collider preservation was checked during installation.

World-space measurements use bone matrices and bind poses to avoid the FBX renderer/bone unit-scale mismatch seen with BakeMesh. The visual matches the original encounter's approximately 2.42 m envelope. Mesh bounds are sampled across all clips and padded proportionally.

Visual review: front, opposite, side, three-quarter, mouth reaction and multiple phases of every action; Renders/YellowMoray/Swim.gif and Swim.mp4; scene render Renders/YellowMorayUnity/WreckMoray.png.

Headset appearance, actual touch alignment and standalone Quest performance remain unverified for this model. No updated APK has been installed as part of this import request.

Reproduction: Tools/Blender/build_yellow_moray.py, validate_yellow_moray.py and render_yellow_moray_review.py. Unity integration: Assets/Ocean/Editor/YellowMorayIntegration.cs.

## Motion and realism refinement — 18 November
Added an explicit runtime animation clock/sample path, used in both play mode and validation, with 0.18 s pose blends on state changes. This removes reliance on legacy autoplay/culling for idle playback. Idle is now four seconds with a small travelling body/tail wave, gentle head motion and a 0–5 degree jaw-breath cycle. Encounter root movement remains owned by the existing controller.

Refined flattened brow, recessed eye depth, warmer head tones, mottled skin, spatial roughness, softer highlights and irregular ivory teeth. Budget remains 4052 triangles, 15 bones and one material. Added a 1024-pixel MoraySurface smoothness atlas. Prior packed source is preserved as ArtSource/YellowMoray/YellowMoray-before-realism.blend.

Blender export/FBX round-trip and Unity material, bounds, scale and encounter checks passed. Logs/YellowMoray-realism-Unity.log: Unity exit 0. A separate actual Unity play-mode run passed for five seconds: tail rotation variation 11.092 degrees and jaw variation 2.500 degrees, state Idle; Logs/YellowMoray-live-motion.log, exit 0. That editor run logged an unrelated Unity Editor Search database exception and unavailable desktop OpenXR display; it continued and verified animation through the desktop fallback. These are not headset performance results.

Updated preview: Renders/YellowMoray/Idle.gif; scene and prefab copied back with matching hashes. No new APK has been installed for this revision.

## Reference animation refinement — 2026-11-18
Refined original Blender reconstruction against the supplied orbit recording: fuller body, narrower head progression, reduced neutral curvature, slower six-second idle and three-second swim. Preserved five named gameplay clips. The reference recording does not expose original animation data; these are authored approximations, not recovered MotionCow takes.
Corrected mouth palate/back-cap weights to blend into Spine00 consistently with the jaw shell. Reduced accumulated spine rotation and reaction gape. Runtime permits rotation only, preserving imported local translations and scales; capped local rotation and smooth transition blending remain enabled. One 1024 atlas material, 15 bones, under 5,000 triangles. Source backup retained before regeneration.
