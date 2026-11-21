# Animal habitat and shark audit

Updated the saved Unity scene to place all 25 comb jellies in Zone 4 and all 25 restored Moon jellies in Zone 6. Moved 11 combs and 14 Moons; other animal transforms were preserved.

| Zone | Documented animals present |
| --- | --- |
| 1 Sunlit Reef | Clownfish, turtle, manta, fire coral |
| 2 Rocky Ledge | Octopus, lionfish |
| 3 Wreck | Moray, grouper, dolphin pod |
| 4 Twilight Drift | 25 comb jellies, Lumen |
| 5 Trench | Giant squid, 12 anglers |
| 6 Jellyfish Drift | 25 Moon jellies, 3 box jellies |
| 7 Open Water Drift | Turtle, shark |
| 8 Wreck & Rays | Stingrays, grouper |

Background reef fish were preserved. Habitat classification uses the nearest authored zone center; this is a placement audit, not a complete implementation of every interaction in the document.

## Shark animation and validation

The supplied great-white model retains a 3-second lateral swimming loop. Across 121 sampled poses: tail lateral sweep 0.37025 m, vertical sweep approximately 0.0000011 m, head movement 0.01298 m, loop seam 0 m. This matches the basic side-to-side propulsion pattern described by Hawaii DLNR: https://dlnr.hawaii.gov/sharks/about-sharks/anatomy/ . This does not certify a species-specific biomechanical simulation.

120 seconds of simulated shark patrol stayed in Zone 7, with minimum distance 12.84581 m from the zone center and maximum frame displacement 0.02001 m at 30 Hz. Slow approach did not warn; fast pursuit warned, cooldown worked, and contact remained non-punitive.

60,000 simulated jelly motion/clearance checks passed, with all combs and Moons remaining in their assigned habitats and above the sand clearance envelope. Unity exited successfully with code 0. No headset performance test was performed.

## Remaining document differences

- Supplied great-white model retained; document specifies a reef shark.
- Octopus is in Rocky Ledge but rests on sand rather than a rock perch.
- Squid remains fixed in the Trench per the earlier no-relocation request; the document additionally describes an Open Water glimpse.
- Restored Moon jellies remain information-only; the document also describes proximity glow/caution without consequence.
- Shark pursuit voice exists; dedicated pursuit glow is absent.
- Pufferfish is mentioned in interaction examples but has no scene actor or assigned habitat.

The machine-readable inventory and motion measurements are in audit.json.
