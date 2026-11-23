# Requested creature corrections

Corrected the head direction of all four supplied fish meshes and their distance meshes. A side-on render with a red +Z marker verified each head points forward. Source import helpers now use the same verified alignment.

All 34 supplied fish now have SeaLife grab support and fitted trigger-only FishTouchBody volumes. Scoop-and-hold and two-second automatic release checks passed for every added fish. Existing FishAgent/SeaLife movement owners pause while held.

Lumen uses natural textured skin without cyan body emission. DeepWaterEncounter keeps fish-body emission zero through curiosity reactions. A startup contact rule ignores small-fish colliders against the diver capsule while keeping their hand queries active. Large-animal collision is retained; a motor test confirmed a small fish does not displace the rig and a large animal solid still blocks movement.

All 53 moon, comb, and box jellies have one simple solid bell collider on the animal layer. Existing box-jelly warning/contact triggers were retained. No mesh colliders were added to jelly tissue, particles, or water.

The shark retains its existing model and rig. A native looping clip adds subtle body yaw, progressively stronger lateral motion down three tail joints, and pectoral-fin motion. Culling and seabed envelopes include the full animation cycle. Four sampled, weighted-vertex renders verify the visible deformation.

The ship's saved transform was preserved byte for byte, including the user's position (-20.32, -42.83, 125.45), rotation, and 1.3447 scale. The expansion helper also preserves an existing ship pose when rerun.

Validation: Unity 6000.4.5f1 in an isolated copy; correction, contact/visual checks, and final saved-scene validation all exited 0. All 34 grab/release cases, jelly bell colliders, body/tail shark motion, companion emission, zero small-fish rig displacement, and retained large-solid collision passed. Existing scene materials/scripts and 24 teleport anchors validated.

This correction pass updates the Unity project. The earlier APK has not been rebuilt with these corrections; headset FPS and physical interaction should be checked with the next build.
