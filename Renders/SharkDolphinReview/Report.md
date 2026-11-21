# Shark and dolphin swimming review

The shark remains in Zone 7 Open Water with its supplied great-white model and 3-second side-to-side tail clip. Measured lateral sweep is 0.370 m; vertical sweep is negligible and the loop seam is zero. Its clip rate follows actual movement, with a 0.55 minimum multiplier to avoid an almost-frozen stroke during blockage. Live playback advanced by 10.98 clip seconds and the shark traveled 5.82 m during the recorded check. It does not animate automatically in Unity's edit view; the GIF is actual Play-mode output.

Dolphin changes:
- Reauthored the existing native tail chain as a progressively delayed vertical wave. Both flukes now share the same stroke, and pectoral movement is restrained.
- Reduced vertical tail sweep from 0.669 m to 0.360 m and left/right mismatch from 0.0146 m to 0.0059 m. Head motion remains negligible; loop seam is zero.
- Limited turns to 20 degrees/s rather than 28 degrees/s and added smoothly eased banking up to 8 degrees.
- Added gradual speed changes and linked stroke rate to actual travel speed. Close-pod rollback now allows a dolphin to separate instead of rejecting escape movement.
- Preserved meshes, materials, eyes, interaction ownership and habitat. The supplied model retains its original open-mouth resting geometry.

Validation:
- Two-minute simulation: shark traveled 72 m; each dolphin traveled approximately 84 m. None stopped in the sampled patrol. Dolphin minimum pod spacing was 5.60 m; maximum turn approximately 20.01 degrees/s.
- Actual Play-mode check: all four clip clocks advanced, meshes visibly deformed and actors traveled. Dolphin live travel was 7.36–7.48 m; the shark traveled 5.82 m. Recordings use a separate camera and do not save a new diver pose.
- Marine geometry, sand-envelope, warning and prepared-asset regression passed. Dolphin export remains 7,300 triangles and 40 bones. Isolated Unity runs exited 0.
- The diver's authored start was explicitly checked unchanged before saving. Original lionfish and repaired moray were preserved. No headset performance claim is made.

References for the basic propulsion directions: Hawaii DLNR shark anatomy (https://dlnr.hawaii.gov/sharks/about-sharks/anatomy/) and NOAA-hosted dolphin educational material (https://www.aoml.noaa.gov/ftp/od/library/NicodemusDolphins.pdf). These checks do not certify species-specific biomechanics.

Shark_Live.gif and Dolphin_Live.gif show actual playback; before.json and after.json contain the measurements.
