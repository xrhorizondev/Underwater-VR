# One-week completion plan: 12–18 November 2026

Preserve the current authored scene, geometry, controls and existing settings. Make focused additions; do not regenerate the reef. Finish a complete playable eight-zone dive using existing assets and modest procedural additions. Bespoke photorealistic creatures and guaranteed device FPS are not assumed.

## Inspection findings
- Existing: eight connected habitats, depth fog/lighting, wrecks, fish schools, two turtles, manta, jellyfish, Lumen, distant trench squid, three teleport shells per zone, swimming/paddling, two-second proximity fish grab, collision motor and water ceiling.
- Partial: SeaLife uses generic proximity reactions. Turtle head/blink, manta roll, octopus escape, moray mouth, dolphin looping, grouper response, shark chase caution and angler brightness need implementation or verification. Several named encounters are absent from the saved scene.
- Danger thresholds use distance to object origin (.65/.48/.3 m), rather than 15–20 cm from hazardous surfaces. Tail warning has no renderer on its empty hazard object. Warning/consequence timing and stereo red-edge feedback need work.
- Audio: breathing exists; warning clip fields exist but assigned speech and spatial creature cues need checking. Soundscape continuously loops music, unlike the document's ambience-first direction. Breathing and score synthesis run synchronously at startup: profiling candidate, not a confirmed cause.
- Performance: existing Android shader/LOD/distance optimizations. Prior Quest installation succeeded, but Guardian prevented valid active-app FPS measurements. Startup and turns need device CPU/GPU/frame-time profiling.
- Boundaries: existing solids and ceiling do not establish a complete lateral perimeter. Add simple ReefSolid perimeter collision without closing passages or teleport destinations.

## Schedule
| Day | Date | Deliverable |
| --- | --- | --- |
| 1 | Nov 12 | Baseline GitHub checkpoint; add side/perimeter collision; profile cold start and full head turns; fix measured bottlenecks; regression checks and Quest test build. |
| 2 | Nov 13 | Shared touch and three-stage hazard system: surface distances, independent switches, tail-only highlight, warning indicator, stereo edge cue, haptics; moon jelly has no consequence. |
| 3 | Nov 14 | Reef/rocky encounters: clownfish scatter/return and grab polish, anemone response, turtle head/blink, manta roll, octopus color/escape, lionfish retreat. |
| 4 | Nov 15 | Wreck encounters: moray mouth, calm grouper/fact, dolphin pod loop; complete missing creatures with existing assets where possible. |
| 5 | Nov 16 | Twilight/trench: Lumen reveal/follow, comb jelly avoidance, angler brightness, distant moving squid; continuous lighting/transition and safe visual references. |
| 6 | Nov 17 | Jelly/open water/finale: shark distance caution, recurring turtle/squid, ray body versus tail response, calm persistent ending; positional ambience and short voice cues. |
| 7 | Nov 18 | Full eight-zone headset walkthrough, accessibility/teleports, collision/menu/grab regressions, startup and turn performance pass, fixes, final APK and GitHub checkpoint. |

## Today's proposed acceptance criteria (implementation awaits user approval)
1. Recoverable baseline pushed before edits; work in small named commits with a push after each validated milestone.
2. Swimming, room-scale movement and teleport cannot escape lateral boundaries; arches/wreck corridors and all 24 teleport anchors remain usable; head ceiling stays Y=6.5.
3. Record cold-start and left/right/full-turn CPU/GPU/frame-time evidence on the target headset. At 72 Hz the frame budget is 13.9 ms; report actual results and any remaining spikes. Do not claim stable FPS from editor tests.
4. Address measured startup/turn bottlenecks without removing existing scene content or broadly lowering visual quality. Investigate synchronous audio, material/shader first use, texture residency and dense reef visibility.
5. Validate build in an isolated copy if Unity is open. Return focused changes and a test APK; keep existing controls and scene placement.

## Working constraints
Use concise updates and targeted reads to reduce tokens. Existing project skill records reusable workflow knowledge; model weights cannot be trained here. Hardware testing requires the target headset to be connected and Guardian/tracking ready. If a specific creature asset is unavailable, use a convincing lightweight existing/procedural representation and report the fidelity limit. Prioritize all eight zones and required behavior within the week.

## Updated reference and Day 4 acceptance — 13 November
Use EXPERIENCE_REFERENCE.md, transcribed from the user's complete brief, to guide remaining implementation. Day 4: one safe moray with mouth-open/retreat/recovery; one calm grouper near the wreck; a three-dolphin pod with individual swim animation and a smooth diver-centered curiosity loop; optional fading creature facts, name plus narration under 12 words; preserve stern/corridor access, all eight menu destinations and 24 shells. Validate earlier interactions and return a Quest test build. Record the reference's carryover details separately; do not treat current gentle manta rolling or the existing octopus color/escape as complete replacements for the more specific touch/startle behaviors.
