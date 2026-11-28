# Balanced coral LOD and ongoing Quest performance validation

Removed the previous all-distance coral LOD0 lock. All 440 coral groups, including scanned Secale, now use native distance selection with spatial dither crossfades. Nominal full/medium transition distances are 10/24 m plus a group-size margin; final culling is around 90 m plus that margin, under water fog. Fade bands span 20% of each transition's projected size and reverse smoothly with viewer motion. Source and Quest reef shaders now support the LOD crossfade variant, including stereo instancing.

Only groups near a transition enable the discard-based fade shader path. Other groups use the normal opaque path. Near detail is verified within at least 6 m of group bounds for the test projection. The hand, grab, school steering, manta motion and startup-facing changes are unchanged.

The original full-detail profile showed recurring 28 ms frames at 72 Hz. Early crossfade test builds still missed budget, so those are not treated as a successful FPS fix. The latest 64-view conservative geometry audit, including overlapping transition meshes, estimates 84.2% fewer coral triangles overall and a peak of 1,162,452 versus 7,122,095 with all corals forced to full detail. This estimate is not an FPS measurement.

Enabled the existing Android OpenXR XR Performance Settings feature and request SustainedHigh CPU/GPU performance hints at startup. These are runtime hints, not a guarantee. Image resolution and MSAA remain unchanged. The diagnostic `reef-opaque-lod` marker can temporarily disable fades for comparison; normal operation uses fades. Frame diagnostics now record rendered triangles where the runtime exposes that counter; unavailable draw-call counters are not treated as zero draw calls.

Validation: isolated Unity 6000.4.5f1 exit 0; coral near-detail, transition ordering, fade shader compilation, geometry savings, collision and all eight habitat destination checks passed. Reviewed close/distant coral renders in `Renders/ReefLodBalance/`. Logs: `Logs/ReefLodBalance-20261128.log`. Latest APK: `Builds/ReefLodBalance-20261128/quest.apk`, 249,174,773 bytes; Unity exit 0, build result Succeeded, manifest/ARM64 checks and explicit signature verification passed. Installed and cold-launched on Quest 2; final focused headset performance is still being measured.

## Headset result and remaining limit

The runtime accepted both sustained-performance requests. The user tested the wider smooth-transition version with Unity frame profiling disabled and reported **Clearly smoother now**. Smooth fades are restored; the temporary opaque comparison marker and frame-test request marker were removed. The installed APK was verified against the final local artifact by SHA-256.

Native VrApi measurements with Unity profiling off still show about 33–38 app FPS against a 72 Hz display in the sampled dense reef views, with GPU utilization around 98–99%. This is a perceptual improvement, not a completed stable-72-FPS fix. Further GPU optimization remains necessary for that target. Evidence: `Logs/ReefLodBalance-Native-NoProfiler-20261128.log`. Earlier Unity profiling and opaque A/B measurements are retained separately; their frame rates must not be represented as passing results.
