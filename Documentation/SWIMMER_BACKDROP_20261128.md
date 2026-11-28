# Swimmer hands and intermittent background rocks

Replaced the glove fabric shading with warm skin, lighter palms, subtle pores and palm creases. Removed the separately appended neoprene cuff triangles while retaining the original hand vertices, controller fit and finger poses. The hand shader renders both sides; dynamic occlusion is disabled and offscreen skin updates remain enabled. Ordinary depth testing remains, so hands do not draw through solid scenery. Open, trigger, grip and hold mesh samples fit inside their renderer bounds.

The background seabed rock gardens now use widely jittered island centers, variable spread and density, 18 m minimum center spacing, broad empty regions and varied rock proportions. Rocks decreased from 1,257 to 485 across 106 islands; grass tufts decreased from 2,237 to 762. Coral accents were regenerated on the changed rock surfaces. Each rock is grounded against the saved sand using transformed mesh vertices, with simplified solid colliders retained.

Unity 6000.4.5f1 validation exited 0 in an isolated copy. Compared 357 protected animal, shell, reef arch and player-rig poses before and after: unchanged. Collision sweeps, room-scale protection, blocked teleports, seabed contact, original grab/release latch and all eight destination clearances passed. Final bare-hand render and visibility-bound checks also exited 0. Logs: `Logs/SwimmerBackdrop-20261128.log` and `Logs/SwimmerHands-20261128.log`. Before/after backdrop and palm/back hand renders were inspected in `Renders/SwimmerBackdrop/`.

The restored grab behavior, improved controller fit and 180-degree starting rig yaw remain. Physical hand visibility, stereo appearance and Quest performance still require a worn-headset check.

Quest build: `Builds/SwimmerBackdrop-20261128/quest.apk`, 249,137,157 bytes. Unity exited 0 with build result Succeeded; manifest and ARM64 artifact checks passed, and explicit apksigner verification passed. Installed with app data preserved on Quest 2 and cold-launched with Status ok.
