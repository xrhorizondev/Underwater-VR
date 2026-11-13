# Day 4 — wreck encounters, 13 November 2026

Implemented from Documentation/EXPERIENCE_REFERENCE.md, saved alongside the existing weekly plan. This is the user's governing experience reference. Detailed carryover and later-zone requirements are recorded there instead of being silently treated as finished.

## Implemented

- One moray in the wreck's stern area: idle breathing mouth motion, safe approach/hand startle, stronger mouth opening, retreat and recovery. No danger flag, consequence or grabbing.
- One calm grouper near the wreck: slow patrol, calm close-hand response and optional name/fact/narration.
- A three-dolphin pod: individual tail/fluke animation, slow patrol, smooth diver-centered looping in the wreck habitat and a livelier curiosity loop on close contact. Added pod spacing and diver avoidance, plus body proxies separate from ReefSolid navigation.
- Optional world-space creature facts: name plus one fact, fewer than 12 words including the name, half-second fade, placed about half a metre beyond the creature's visible bounds. Narration uses short offline-generated English placeholder clips, not a recording of the wearer. Look at a nearby creature for roughly 0.8 seconds or interact to reveal its fact. Narration has a shared cooldown and respects other speech.
- Toggle cards/narration with B while the habitat menu is open. Desktop menu also exposes the toggle. Swimming, A travel, Y resume, left menu, grabbing and system menu assignments remain intact.

The grouper adapts the existing photographic fish mesh. The moray and dolphins are original lightweight procedural representations, not scanned species models. Their visual fidelity remains a headset review item.

## Validation

Unity 6000.4.5f1, isolated project. Final validation exit 0, Logs/day-four-build-validation.log.

- Moray mouth opening, retreat and return passed; safe/non-grabbable ownership verified.
- Grouper and dolphins each simulated for 180 seconds. Grouper travelled approximately 30 m; individual dolphins approximately 128 m. Reef clearance/stall checks passed.
- Pod simulated together around a moving diver for 120 seconds with repeated curiosity triggers. Minimum root spacing 3.1856 m; no reef penetration.
- The wreck's low centerline contains existing fallen timbers. Found an existing upper swim-through above them, checked 25 body-sized positions through the stern, then swept the actual DiverCollisionMotor through that route. No wreck geometry or existing coral was moved to manufacture passage clearance.
- All three fact recordings assigned and under seven seconds; text/name counts remain under 12 words.
- Existing natural audio, visible body bubbles, full-length squid, rays/jellies, 227-fish motion, palm/two-second release, hazards, eight destinations, 24 shells, bounds and ceiling checks passed.
- Rendered and inspected Renders/Day_Four_Moray.png, Day_Four_Grouper.png and Day_Four_Dolphin.png. Corrected text overflow, dolphin mesh seams/anatomy and pod crowding found during review.
- Quest toolkit build exit 0; report status passed, buildResult Succeeded. Manifest/ARM64 payload and separate APK v2 signature verification passed. Deprecated Unity API/package warnings remain; no captured C# or shader build errors.

## Delivery

Builds/DayFour/quest.apk — 72,968,494 bytes, ARM64 IL2CPP, existing Android configuration and local test signing.

SHA-256: 0DE8F668F4E82E80085A70955B75E81E8C68A4A07E9AB4B96774EB95BED4D203.

Installed on the connected Quest 3 with adb install -r, preserving app data. UnityPlayerGameActivity launched successfully; process 26237 verified running. Wearer review of creature appearance, fact readability/narration and swimming through the wreck remains pending. Editor checks do not establish device FPS.

ProjectSettings/ProjectSettings.asset was preserved and no isolated settings were returned. Git checkpoint 5f69601 remains the previously pushed state; this Day 4 implementation and subsequent local fixes are not represented by that earlier checkpoint.

## Fact sources

- Moray mouth breathing: Aquarium of the Pacific, https://www.aquariumofpacific.org/onlinelearningcenter/species/california_moray
- Grouper ambush/suction feeding: NOAA Fisheries, Nassau Grouper framework, https://www.fisheries.noaa.gov/s3/2024-10/Nassau-Grouper-Section-7-Framework.pdf
- Dolphin air breathing: NOAA National Ocean Service, https://oceanservice.noaa.gov/facts/dolphin.html
