# Runtime CPU optimization — no player build

- Fish flocking decisions are cached for approximately 0.1 seconds. Forward movement, steering toward home, diver avoidance and swept collisions still run every frame. Velocity normalization is performed once per fish rather than once for every neighbor pair.
- Quest distance culling is spread over approximately 18 frames, with immediate full refresh after a teleport. Static renderer bounds are cached; moving animals and particles retain live bounds. Visibility flags are written only when changing.
- Water animation time still updates every frame. Depth-dependent lighting and fog settings update at 10 Hz, and the active camera reference is cached.
- Non-reactive SeaLife shares materials instead of cloning each renderer material. Hazard glow materials update only when entering or leaving the glow state. Color access checks shader support.

Validation: Unity compile passed. Fish-motion simulation passed for 227 fish over 20 seconds. Swept fish movement, diver wall sliding, blocked teleport, room-scale wall contact and seabed contact checks passed. Flocking cadence test performed 90 neighbor passes over 720 simulated frames (87.5% fewer passes than every frame). This is a work-count reduction, not a measured FPS improvement. No headset profiling was performed and no APK/player build was created.
