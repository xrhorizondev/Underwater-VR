# Quest 3 / 3S optimization

The Android build processes Underwater_Realistic automatically; the saved desktop scene retains its full detail. Use Underwater > Quest 3 > Configure Android optimization, then build the realistic scene for Android. Tools/RunQuestValidation.ps1 provides isolated Validate and Build commands.

## Applied profile
- ARM64 IL2CPP, Vulkan, OpenXR single-pass multiview, Meta Quest Support.
- Android texture overrides: ASTC 6x6, 1024 maximum, mipmaps and 256 MB runtime streaming budget.
- No camera HDR or desktop post effect; 4x MSAA remains for VR edge quality.
- One pixel light, hard sun shadows at 16 m, low shadow-map resolution, no cascades. Only rock/arch meshes cast shadows; coral color, normal textures and caustics remain.
- Scanned coral uses its existing middle/lowest LODs. Highest-detail render meshes are removed from Android build scenes; colony colliders remain unchanged.
- 55 m camera far plane, 52 m bounds-based renderer culling checked four times per second. Colliders stay active.
- Schools beyond 45 m and unheld SeaLife beyond 48 m skip simulation. Particle systems are limited to 45 particles each.
- Diver caches creature/atmosphere references instead of allocating scene-wide searches every frame.
- Mobile reef shader replaces expensive multi-octave procedural encrustation with a texture-based tint. Water, sky and particle shaders include stereo-instancing macros.

## Validation and limits
Scene mesh triangle sum across all LODs fell from 21,395,541 to 10,505,616 (50.9%). This sums meshes across the entire eight-habitat scene, not triangles visible in a frame or GPU timing. Collision regression checks passed. All 227 schooling fish simulated 20 seconds without overlap or unstable movement.

No Quest was connected during development. FPS, thermals and visual stereo output have not been measured on hardware. Profile a standalone APK on Quest 3/3S, especially the first dense reef, overhead water, jellyfish and teleport transitions. For 72 Hz, CPU and GPU frame times must each stay below 13.9 ms, with headroom for sustained play. A PC/Link result does not validate standalone performance. Reduce render scale or shadow distance further only after checking the GPU frame time; use fewer nearby fish if simulation is the limiting CPU cost.

Unity/Meta reference: https://developers.meta.com/vr/documentation/unity/unity-openxr-settings-quest/

Android release build completed successfully: Builds/Quest3/Underwater-Quest3.apk (45,238,564 bytes). Shader compilation and IL2CPP/Gradle packaging passed. Unused lighting/shadow/fog variants are stripped; Standard is retained through a runtime material reference instead of forcing every variant into the package.
