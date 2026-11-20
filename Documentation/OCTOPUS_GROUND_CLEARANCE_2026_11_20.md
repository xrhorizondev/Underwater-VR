# Octopus sand clearance — 20 November 2026

The octopus controller previously grounded only the actor's center at sand height plus 0.1 m. Its animated arm tips could therefore pass below the sand even when the body collider remained clear.

Added `OctopusGroundClearance` to the existing octopus actor. The editor bake samples the complete 301-frame crawl into 349 conservative support cells. A 100 × 100 terrain field stores upper sand heights from the actual transformed seabed collider triangles. Both grids use 0.2 m cells. Runtime movement reads these arrays; it does not bake the skinned mesh, sample its vertices or issue extra terrain raycasts.

The controller looks 0.35 m ahead, raises the body before advancing onto a higher bank, and pauses horizontal travel until the arms have clearance. Rise and descent limits are 0.65 and 0.35 m/s. A final check rejects a collision-slide deflection if it would bury the arms. The saved starting height is raised by 0.38749 m; the original skeletal/morph animation, mesh, material and collider settings remain intact.

This is conservative whole-body clearance, not independent arm contact or terrain IK. The baked data covers the authored animation and the actor's fixed world orientation/scale. Movement stops at the boundary of its 20 × 20 m sand patch. Re-run the bake after changing the animation, actor rotation/scale or seabed geometry. It does not provide animated limb collision against rocks; existing body collision remains responsible for those obstacles.

## Validation

- `Underwater.Editor.OctopusGroundValidation.Run` bakes the data and saved starting height. `Validate` checks it without saving simulated movement. `CheckDelivery` also runs prepared-creature regression validation and the live animation capture.
- Unity 6000.4.5f1 exited 0 for the final validation. Checked 41,936,180 animated vertices at five positions, including phases between bake samples. Minimum conservative sand gap: 0.0508976 m.
- Eight escape/return headings, 60 simulated seconds each, including collision sliding: no sampled root-clearance or solid-body collision failures; maximum travel 9.144338 m, maximum vertical speed 0.6500244 m/s.
- Existing prepared marine model, shader, reaction and hazard checks passed. Five seconds of actual Unity play mode confirmed bone/morph motion: 73.2653 degrees maximum bone variation and morph ranges 12.51853/41.72843. Inspected the updated live render at `Renders/OctopusUnity/PlayMode.png` and sampled animation views.
- Logs: `Logs/OctopusGround-validation.log`, `OctopusGround-directions.log`, `OctopusGround-delivery.log`. The known Unity Editor Search indexing exception and unavailable desktop XR display did not prevent checks. These checks do not establish stable headset FPS or wearer visual acceptance.

## Delivery

The pre-clearance checkpoint was pushed as `24a573a0eb7ba99c7501e6504a549049f5e893e3` to `https://github.com/xrhorizondev/Underwater-VR`, branch `feature/bug-fix`, after explicit payload approval. This repair follows that checkpoint, together with the lure-lighting correction described in `ANGLER_LURE_2026_11_20.md`.

`Builds/OctopusGround-20261120/quest.apk`: 108,164,570 bytes. SHA-256: `B1C5A90683FB71F957891550924057D54B10037CFF2716DB8BFE4C828D02B1D5`. Unity exit 0, build result Succeeded, manifest/ARM64 artifact checks passed. Explicit Android SDK APK v2 signature verification passed, superseding the toolkit's missing-apksigner warning. The build retained the existing recoverable licensing diagnostics.

Installed successfully on Quest 2 with app data preserved and requested launch. Installation and launch request do not establish that the app is focused or that headset review has passed. Only intended scene/scripts/previews were returned from the isolated project; source ProjectSettings and Packages were preserved.
