# Coral detail, smooth swimming and reef-facing startup

Coral LOD groups now hold their authored highest-detail silhouette throughout the camera-visible range. This applies to scanned colonies, living coral and background coral accents, including Quest build-generated groups. Other scenery keeps its existing distance selection. The camera still uses ordinary frustum/far-plane culling. This removes coral geometry swaps but increases visible geometry cost; it is not a measured FPS guarantee.

Added acceleration-limited yaw and damped pitch to schooling fish, manta, stingrays, generic cruisers, turtles and lionfish. Existing coordinated dolphin and shark steering remains. Turns are clearance-tested before committing. Obstacle escape in schools accelerates smoothly rather than switching the displacement direction instantly. Manta navigation uses a continuous orbital tangent, anticipates seabed height, limits bank/pitch and avoids slope-sliding vertical impulses.

Schools choose and retain a horizontal bypass side for each hand. They slow while entering the bypass and resume their existing cohesion after passing or hand removal. The original grab ownership, latch and release behavior is preserved.

Startup facing now aligns the first valid tracked headset heading to reef +Z through DiverCollisionMotor, preserving the eye's world position. Tracking-origin recentering permits a new alignment. The saved rig rotation is neutral again. Looking around during exploration remains free; facing is not forced continuously.

Isolated Unity 6000.4.5f1 checks exited 0:

- 440 coral groups retain LOD0 across sampled viewing sizes.
- Six initial headset headings face reef +Z without moving the tracked eye.
- Synthetic 12-fish hand bypass used both sides, stayed at least 0.779 m from the palm and regrouped to a 1.031 m mean spread after removal.
- Manta 120-second simulation travelled 47.415 m, with maximum vertical speed 0.0512 m/s and turn rate 16.275 degrees/s; no reef penetration.
- All 227 school fish and 27 featured creatures passed the movement audit. School fish had zero penetrations/backward steps, maximum frame step 0.02472 m and longest stationary interval 1 second. Imported animation loop endpoints matched.
- Collision sweeps, room-scale motion, blocked teleport, seabed contact, original grab latch and all eight habitat clearances passed.

Evidence: `Logs/ReefComfort-20261128.log`, `Logs/ReefComfort-AllMotion-20261128.log`, `Renders/ReefComfort/Validation.txt` and `Renders/ReefComfort/AllFishMotion.txt`. The broader pre-existing audit reached a separate `Spine warnings missing` hazard assertion; its original full entry point is retained, and a movement-only entry point now permits independent motion verification.

Quest APK: `Builds/ReefComfort-20261128/quest.apk` (249,148,657 bytes). Unity exit 0; build result Succeeded; manifest/ARM64 checks and explicit APK signature verification passed. Installed on Quest 2 with app data preserved, cold-launched successfully and verified the installed base APK SHA-256 matches the local build. Process remained running after startup. Frame diagnostics were requested, but no focused-frame samples were available during the check, so no hardware frame-rate or perceived-smoothness claim is made. Startup evidence: `Logs/ReefComfort-QuestStartup-20261128.log`.
