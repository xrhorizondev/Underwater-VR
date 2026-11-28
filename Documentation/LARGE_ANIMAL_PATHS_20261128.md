# Large-animal patrol and distance fix

The Android 48 m movement/animation cutoff was shorter than the 160 m camera far plane. Large patrol animals and the squid now remain active to 200 m. Small-fauna distance budgets remain as authored; menus still pause the relevant simulation owners.

The manta, three dolphins and shark use broad elliptical patrol lanes. Authoring searches the entire circuit using each animal's animated body envelope, collision sphere and seabed constraints, with a one-metre rock margin and an underwater ceiling. Dolphins share a circuit with separated phases. The existing movement owners drive the patrol and their existing animation adapters; a single arc clock supplies both position and tangent, avoiding the inside-corner steering that can pin a large body against rocks. Runtime collision sweeps remain enabled. Imported swim clips retain at least a 0.35 travel-rate factor, so fins do not stop entirely if travel is interrupted.

Validation in Unity 6000.4.5f1 exited 0:
- Two six-minute patrol runs per large animal at 30 and 72 simulation steps/second, plus 720 whole-loop envelope checks per animal per run. No stalls or jumps; observed rock clearance at least 0.7 m, with maximum turns below 7.5 degrees/second.
- The complete 227-school-fish and 27-featured-animal movement/collision audit passed twice, from fresh scenes.
- Squid full-body movement passed two six-minute runs, with no collision/jump, non-static animation bones and active-range checks at 160 m.
- All ten release suites passed, including hand contact, grab latch, warnings, travel clearance, models/animation, school cadence, facing, manta, LOD and shaders.

Evidence: Logs/LargeAnimalPaths-Final-20261128.log, Logs/LargeAnimalPaths-Squid-20261128.log, Logs/LargeAnimalPaths-Release-20261128.log and Renders/LargeAnimalPaths/.

Only patrol actor/component blocks and relevant swim-rate settings were returned from the isolated scene. Other saved scene blocks and existing workspace edits were preserved. Worn-headset visual feedback and focused FPS still require device testing.

Quest test APK: Builds/LargeAnimalPaths-20261128/quest.apk (249,177,617 bytes). Unity build exit 0; buildResult Succeeded; manifest and ARM64 checks passed. Explicit apksigner verification exited 0 with the APK v2 signature verified. The toolkit's missing-apksigner-on-PATH warning was resolved by that explicit check. Existing obsolete Unity API/compiler warnings remain. This APK has not been installed by this run.
