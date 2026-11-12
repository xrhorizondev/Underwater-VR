# Quest validation — 2026-11-11

APK: Builds/Quest3/quest.apk (56,259,779 bytes), Unity 6000.4.5f1, Android ARM64 IL2CPP/Vulkan. BuildPipeline result Succeeded; toolkit report status passed. APK manifest/ARM64 checks passed; apksigner verified APK v2 signature. Local test signing, not a store release.

Saved scene: audited existing reef, seabed, coral and wreck collision; lowered 548 suspended coral colonies onto rock/seabed supports. Surface Y=8; maximum headset Y=6.5. Swimming, tracked movement and teleport obey the ceiling. Left menu/Y resume; world-space habitat selection uses right stick/A. Menu blocks swimming momentum, grabbing and gaze teleport. Original generated ambient music and soft navigation/teleport sounds accompany existing regulator breathing.

Quest rendering: far plane reduced from 82 to 60 metres, existing .78 eye scale, shadow/HDR suppression, shader/LOD and distance mesh optimizations retained. Missing mobile shader now safely preserves the original shader. Collider checks passed: animal sweep, rig wall slide, blocked teleport, room-scale head protection, seabed and water ceiling. Fish regression: 227 fish, 9 schools, 20 simulated seconds; maximum step .0247 m, pitch 10 degrees, turn 51.42 degrees/second. Flocking: 90 decisions over 720 simulated frames.

Installed on connected Quest 2 successfully. Device Guardian/tracking setup prevented the test app from becoming active; no valid app FPS or head-turn benchmark was obtained. Stopped the test and restored unworn proximity state. Check head turns, controller navigation and audio in-headset after completing Guardian setup. No stable-FPS claim is made.

Build warnings: deprecated Unity discovery APIs, existing shader/import warnings, target switched only in isolated build copy. The first PowerShell toolkit launcher did not wait for the GUI executable; build completion was independently checked through its passed Unity report, APK structure/signature, and successful device install.

Reusable project guidance: .agents/skills/underwater-vr/SKILL.md. This stores workflow knowledge; it does not train model weights.
