---
name: underwater-vr
description: Fix and validate collision, water bounds, navigation, sound, and Quest performance in this Underwater-VR Unity project.
---
Use Unity version from ProjectSettings/ProjectVersion.txt. The enabled scene is Assets/Scenes/Underwater_Realistic.unity. Preserve existing settings edits.

DiverCollisionMotor owns swimming, tracked-head sweeps and teleport clearance. ReefSolid is layer 8. Water mesh is at Y=8; keep the head at or below Y=6.5 across swimming, tracking and teleport. Never move the rig directly for gameplay travel.

Quest left menu toggles habitats; right system menu is reserved. World-space caption menu uses right stick, A to travel, Y to resume. Menus stop velocity, grabbing and gaze teleports.

ReefRepair.Run repairs saved rock placement and solid geometry, saves the scene, then runs collision and ceiling checks. Ground rocks using transformed mesh vertices against ReefGeometry.Floor, preserving arches and authored coral placement. Do not add colliders to transparent water/particles or every render LOD.

QuestReefBuild optimizes build scene LODs and geometry. QuestReefPerformance applies runtime graphics limits. Validate head-turn performance on device before claiming stable FPS. ReefSoundscape generates original audio once at startup.

If Unity has the source project open, copy Assets, Packages, ProjectSettings and CodexBuild to an isolated workspace and run Unity there; do not remove its lock. Use the unity-local-build toolkit with profiles matching built-in rendering (URP is not used), ARM64 IL2CPP and local test signing. Verify actual Unity exit code and artifact report. Return only the repaired scene and intended artifacts, not unrelated settings.
