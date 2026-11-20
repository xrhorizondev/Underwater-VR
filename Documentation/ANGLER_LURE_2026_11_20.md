# Anglerfish lure lighting — 20 November 2026

The touch response previously wrote cyan `_BioEmission` onto the entire anglerfish body. Its point light was also centered inside the body. All 12 anglerfish now keep body bioluminescence at zero, including touch and recovery. Lumen's separate body-reveal behavior remains intact.

Relocated each existing point light to its overhead lure bulb and limited its range to 0.65 m. The bulb and a small, camera-facing additive halo brighten smoothly on touch and return to their resting level. The body retains ordinary shaded skin and reflected light. The halo uses one shared two-triangle mesh and one shared material, with cubic mesh bounds covering all camera-facing orientations. Its shader includes fog, instancing and stereo macros.

Light count is unchanged: one shadowless vertex point light per anglerfish. The halo adds one small transparent renderer per fish; no fullscreen bloom, extra light or runtime texture generation is introduced. Full-route Quest performance and wearer stereo/visual acceptance remain pending.

## Checks and reproduction

`Underwater.Editor.AnglerLureCorrection.Run` applies the saved scene/material changes, checks all 12 anglers through idle, touch and recovery, runs the existing encounter regressions, and captures idle/touch views. It is safe to repeat against the already-corrected scene. Updated the existing lure regression to read the point light from the bulb rather than the fish root.

Unity 6000.4.5f1 exited 0. `ANGLER_LURE_VALIDATED` confirmed zero body bioluminescence, bulb-local lights, touch/recovery and no added lights. `REGRESSION_FIX_VALIDATED` passed, including the existing deep-water behavior checks. Inspected both rendered views in `Renders/AnglerLure`. Log: `Logs/AnglerLure-final.log`. Source and isolated build scene/scripts/assets match.

## Review build

`Builds/AnglerLure-20261120/quest.apk` includes the octopus sand-clearance fix too. Size: 108,176,850 bytes. SHA-256: `6A3634FF822EEE8E2608A98BAC126E990957BAD0ED24A855B14996CCDBC1AD31`.

Unity exit 0/Succeeded; ARM64 and manifest artifact validation passed. Explicit Android SDK APK v2 signature verification passed, superseding the toolkit's missing-apksigner warning. Existing recoverable Unity licensing diagnostics remain. Installed successfully on Quest 2 with app data preserved, and requested launch. Installation/launch request does not establish headset focus, visual acceptance or stable FPS. Source ProjectSettings and Packages were preserved.
