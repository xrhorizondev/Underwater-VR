# Underwater Exploration VR — reference brief

Source: user-provided design brief, 13 November 2026. This is the governing experience reference alongside ONE_WEEK_PLAN.md. Preserve subsequent explicit wearer feedback when resolving details; record deviations rather than silently changing the brief.

A first-person scuba dive across a connected reef system, descending from bright shallows to real depth before ascending back. Most touches reward curiosity; dangerous elements warn before contact. No fictional plot: the arc is the real diver's own excitement.

## Navigation

- Free-swim with thumbstick or hand paddling.
- Gentle current only after 20+ seconds idle in open water; cancel immediately on movement input.
- Continuous swim-through transitions: coral arch, wreck corridor and darkening thermocline. Habitat menu travel remains available.
- Accessibility: two or three fixed teleport points per zone, including controller-independent gaze travel where supported.

## Interactions

1. Safe touch: coral sway, anemone retract/reopen, schools scatter/reform, turtle turns toward the hand. Physical response is normally the reward; avoid unnecessary UI. Standout turtle/ray/dolphin encounters may show an optional name plus one spoken fact, under 12 words, fading 0.4–0.6 m beside the creature.
2. Curiosity: puffer inflation, safe moray startle/retreat, octopus color change/jet escape, dolphin pod looping. Resting octopus startles awake on approach, opens eyes and flexes arms/suckers. No punishment.
3. Danger, strictly ordered: hand within approximately 15–20 cm of the surface gives a soft red outline; then a warning icon and natural diver-voice warning under 10 words; contact after warning gives brief haptics, red screen-edge feedback and a vocal reaction under one second. Proximity, caution and consequence remain independently tunable. Moon jellies have no consequence.
4. Separate proximity grabbing: selected docile clownfish/small reef fish can be scooped, held and wriggle before release after a couple of seconds. Do not substitute raycast touch for this interaction.

## Habitat sequence

| Zone | Environment and inhabitants | Required behavior |
| --- | --- | --- |
| 1 — Sunlit Reef | Bright shallows, light rays, warm coral, high visibility; clownfish with anemone, turtle, manta, one fire-coral patch | Clownfish scatter and scoop; manta loops and reacts to wing touch with a barrel roll; turtle lazily swims, turns its head toward a hand and blinks; fire coral resembles adjacent safe coral and supplies the first full caution. |
| 2 — Rocky Ledge | Darker craggy ledge and crevices; resting small octopus and lionfish | Octopus wakes suddenly on reaching/approach, opens eyes, flexes arms/suckers, changes color and escapes. Lionfish receives the strongest early warning polish: beautiful stripes are tempting but spines warn before contact. |
| 3 — The Wreck | Half-collapsed wreck, light shafts through broken hull, rust and coral on metal; moray, grouper, dolphin pod | Moray is a safe mouth-open startle and retreat; grouper is calm with optional information (may foreshadow later bioluminescence without implying the grouper glows); dolphins loop around the diver. Exit by swimming down through the stern into darker water. |
| 4 — Twilight Drift | Genuinely dim blue-grey light beyond sunlight; comb jellies and Lumen | Comb jellies are information-only. Lumen brightens and loosely follows through the remaining dive, leading into near darkness. Retain the wearer's explicit recent preference for no visible lure on the companion unless revised. |
| 5 — The Trench | Near-dark, cold, sparse, vast; heavy fog/short visibility instead of pure black | Distant passing giant squid is spectacle/information only and too far to touch. Small anglerfish briefly brighten individually on touch. Prioritize lighting/particle work here. |
| 6 — Jellyfish Drift | Ascending, slow-drifting and backlit; moon and box jellies | Moon jelly: proximity glow and caution only; box jelly: full three stages. Teach a gradient of danger. |
| 7 — Open Water Drift | Out beyond the reef wall into blue space; recurring turtle, distant shark and passing squid | Turtle is a calm emotional high point. Shark only cautions on fast pursuit: “Let's not chase it, just watch.” No touch punishment. Squid remains distant information/spectacle. |
| 8 — Wreck & Rays | Coral-colonized sunken hull, shafts of light, rays near sand, grouper; calm before ending | Ray body/wings are safe; tail/barb alone is dangerous. Near-tail reach triggers caution. Preserve calm, freely touchable bodies and a persistent ending. |

## Planning interpretation

Day 4 implements zone 3's moray, calm grouper/fact and dolphin pod, with continuous stern access and previous regression checks. Global optional fact-card infrastructure is introduced here for reuse.

Carryover acceptance details to audit during later polish: touch-triggered full manta barrel roll (current gentle rolling is not the complete response), octopus resting/eye/sucker startle detail, turtle hand-directed attention, and pufferfish placement if included. Days 5–7 cover remaining twilight/trench, jelly/open-water/finale behavior and the complete headset walkthrough.
