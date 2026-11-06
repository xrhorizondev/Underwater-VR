# Underwater immersion revision

Replaced ring billboards with 480-triangle 3D air-bubble meshes. The shader approximates reflections from bright surface water and dark reef with view-dependent Fresnel edges and a small directional highlight. It does not use expensive screen-copy refraction or claim physically traced refraction.

Bubbles emit from alternating regulator-side outlets during a 1.8-second exhalation in a four-second breathing cycle. Most are 8–25 mm; occasional larger bubbles are 25–38 mm. World-space motion, mild turbulence, changing aspect ratio and gradual size growth make the plume uneven. A near-eye fade keeps bubbles from flashing across the lenses. The pool is capped at 64; teleporting clears it. Reef collisions kill contacting particles.

Fine suspended matter is a separate 100-particle pool with slow world-space drift. There is no continuous shower of air bubbles in open water. Water haze is stronger, sunlight is filtered toward blue, shadows have lower contrast, and reef surfaces lose warm light with distance. Nearby colors remain comparatively clear.

Regulator audio uses a four-second breath envelope and short resonant bubbling transients synchronized with exhalation; an 1100 Hz low-pass filter applies to the ambient source only. The tracked camera is unchanged: no forced sway or image distortion.

Validation: rendered headset-view approximation and bubble-detail images in Renders. Simulated five seconds of emission; observed 27 particles at peak, below the 64 cap, and verified teleport cleanup. Desktop preview uses no ReefOptics post effect and a 95-degree camera. It is not a stereoscopic on-headset validation or an FPS measurement.

Water-motion pass: suspended matter now uses a varying 0.075–0.165 m/s lateral current, subtle vertical surge and single-octave turbulence. The 140-particle pool is prefilled at startup. Surface ripple animation is faster, surface displacement is stronger, and caustic patterns travel four times faster with increased contrast. Soft vegetation sway also moves faster. Camera tracking is unchanged. Preview frames at t=0 and t=4 seconds are saved in Renders/Water_Motion_Start.png and Water_Motion_After4s.png. These are editor renders, not headset FPS measurements.
