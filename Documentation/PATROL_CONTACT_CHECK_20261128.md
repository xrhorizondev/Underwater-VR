# Patrol scene references and contact check

The source scene contained five serialized ReefLargeAnimalPatrol components whose owning GameObjects did not include them in m_Component. This was introduced by the preceding selective scene merge. Unity therefore reported that each animal did not reference its MonoBehaviour and attempted an import repair. Restored all five owning component references directly in the source scene. Added a permanent regression check for duplicate scene IDs and reciprocal component links before Unity opens the scene, so import auto-repair cannot conceal this failure.

Temporary diagnostics existed only in the isolated test project. A deliberately injected rock produced a red HIT / BLOCKED label, marker, screenshot and warning identifying the animal and collider. Both subsequent six-minute simultaneous patrol runs, at 30 and 72 simulation steps/second, had zero unexpected rock contacts or stalls. The full animal movement audit passed twice, squid movement passed twice, and all ten release suites passed. Unity exited 0. The repaired scene opened without the reported component-reference messages.

Each of the five patrol animals was then deliberately obstructed for two seconds: it did not move through the obstruction, continued its swim animation, and resumed five seconds of travel without jumps after removal. Recovery Unity exit 0. These checks did not reproduce the user's intermittent movement fault during normal patrols; they are not a guarantee that every headset condition has been reproduced.

Removed the temporary diagnostic script and its meta file after testing. Hit markers and labels were unsaved and removed by scene reload; none are in the source scene or clean build. Diagnostic images/reports remain as evidence under Renders/PatrolContactCheck/. Build input scene was copied from the repaired source and hash-matched before building.

Evidence: Logs/PatrolContactCheck-20261128.log, Logs/PatrolContactRecovery-20261128.log and Renders/PatrolContactCheck/.

Clean Quest APK: Builds/PatrolVerified-20261128/quest.apk (249,177,629 bytes). Build Unity exit 0; buildResult Succeeded; ARM64/manifest artifact checks passed. Explicit apksigner verification exited 0 and verified the v2 signature, resolving the toolkit PATH warning. No scene repair/component-reference messages appeared in the clean build log. Existing obsolete API/compiler warnings remain. APK not installed by this run.
