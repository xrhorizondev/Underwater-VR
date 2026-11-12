# Underwater Exploration

Open this project in **Unity 6000.4.5f1**, open `Assets/Scenes/Underwater_Realistic.unity`, and press Play. Choose Start in the opening menu. The saved scene contains editable geometry, materials, lighting, creatures, and eight connected habitats. The earlier prototype remains in `Assets/Scenes/UnderwaterExploration.unity`.

Windows player: `Builds/RealisticWindows/Underwater Exploration.exe`. Keep the executable and its accompanying folders together.

## Scene

Photographic sand and reef textures, scanned coastal rock beds, Smithsonian coral geometry with three LODs, textured fish schools, animated caustics, depth-dependent fog and lighting, moving ocean surface, suspended particles, translucent jellyfish, a swim-through arch, and timber wrecks. Desktop rendering adds raymarched light shafts, screen-space shading, bloom, and tone mapping. Five actual Unity renders are in `Renders/`.

This is a detailed real-time scene, not a fully photorealistic production environment. The turtle uses a detailed authored loggerhead model with procedural swimming. Rays and some corals still use procedural geometry. Colored fish variants are artistic adaptations of the photographic fish mesh. The dense reef targets desktop graphics. Headset comfort, frame rate, and standalone Quest optimization still require hardware testing. Desktop post-processing is bypassed for stereo cameras; XR retains scene geometry, surface effects, caustics, and fog.

## Controls

- WASD: swim; Q/E: descend/ascend.
- Hold right mouse: look; left mouse: scoop a nearby grabbable fish.
- T: teleport to the shell under your gaze; Escape: habitat menu.
- XR: tracked head/controllers, left stick swimming, left grip paddling, right grip scooping, right primary button gaze teleport.

OpenXR uses the Oculus Touch profile. Desktop fallback works without a connected headset. The desktop habitat menu uses IMGUI; headset-only menu navigation needs a world-space UI. Swimming uses a collision capsule against the seabed, rocks, wreck and coral colonies. Fish use body-sized swept collision queries and reef avoidance; obstructed teleport landings are rejected.

## Rebuild

Use `Underwater > Build realistic reef scene` to regenerate the scene (this replaces edits to that scene). Batch methods are `Underwater.Editor.RealisticReefBuilder.CreateAndRender` and `.BuildWindows`. `Tools/RunReefValidation.ps1` runs these in an isolated project copy so the open editor is not interrupted. Generated meshes and materials are under `Assets/UnderwaterArt/`.

Asset sources and modifications are documented in `Documentation/ASSET_CREDITS.md`. No paid assets are required.

Fish appearance and animation have been upgraded with detailed scale materials, independent tail beats, and nine steering groups. See Documentation/FISH_UPGRADE.md and Renders/Fish_Motion_Preview.mp4.


