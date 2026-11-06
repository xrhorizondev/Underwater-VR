# Fish appearance and motion upgrade

The saved Underwater_Realistic scene now uses:

- A canonical fish mesh with consistent metre-scale coordinates and a forward-facing head. This fixes the imported-axis and scale mismatch in the previous deformation.
- Photographic scale and normal maps with restrained specular highlights; silver, warm-gold, and blue-grey school variants.
- Detailed mesh variants and markings for interactive fish. These reuse and reshape the photographic source mesh; they are artistic approximations, not separate anatomically accurate scans for every species.
- Progressive tail/body flexion with stable heads, subtle fin movement, independent phases, and speed-coupled tail beats.
- Nine steering groups containing 227 fish, including the five grabbable clownfish. Alignment, separation, cohesion, habitat bounds, forward obstacle probes, and diver avoidance replace the small circular tracks.
- Limited yaw, pitch, acceleration, and banking. Clownfish movement pauses while held, then resumes after release. Lumen and other individual interactive fish retain SeaLife positioning with separate body animation.

Validation: twenty seconds of simulated motion for all nine groups passed. Maximum per-step movement was 0.0261 metres at 30 steps/second, pitch was capped at 10 degrees, and observed angular speed remained below 51.43 degrees/second. This checks stable movement, not a guarantee of collision-free navigation in every reef configuration. Headset performance and controller interactions still need hardware testing.

The Windows player supports --fish-preview to capture 144 frames at 24 frames/second. Tools/encode_fish_preview.py uses Blender's video sequencer to package those actual Unity frames as Renders/Fish_Motion_Preview.mp4. The preview uses a following camera to show one nearby fish.

Final verification (2026-10-27): Windows build completed without C# or shader errors. The exported player captured all 144 frames and exited with code 0. No null-reference, missing-reference, or shader errors appeared in Logs/fish-preview.log. Sampled frames were inspected; the six-second MP4 is ready for visual review. OpenXR still reports no connected headset.

Color revision: lifted the photographic scale midtones, strengthened turquoise/blue/gold school tints, reduced metallic response, and added depth-scaled artistic bounced-light fill to keep fish colors visible on their shaded sides. Dark eyes and fin details are retained. The reef lighting and animation logic are unchanged by this color revision.
