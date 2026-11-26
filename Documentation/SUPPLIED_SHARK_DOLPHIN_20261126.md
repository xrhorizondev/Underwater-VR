# Supplied shark and dolphin replacement

Replaced the visible shark and all three dolphins in `Underwater_Realistic.unity` using the two user-supplied archives. Original FBX and editable Blender copies are in `ArtSource/SuppliedSharkDolphin`. Unused overlapping shark bodies/eyes and dolphin cube objects were excluded. Existing travel controllers, encounters, colliders and actor placement remain in use. Previous visual renderers and animation drivers are disabled.

The supplied swim clips run through `ReefImportedAnimation`, using actual travel speed and staggered dolphin phases. `DolphinSwimController.suppliedSwim` selects supplied playback while retaining navigation. New animated skin bounds and seabed envelopes were sampled over 65 poses.

`FeaturedCreatureAudio` fades spatial audio in within 6 m, stops outside that range, and reduces volume during narration. Dolphins use the existing original `DolphinNearby` whistles. Sharks use the newly generated original `SharkNearbySwim` water sound. The pooled fish audio excludes these actors to avoid duplicate playback.

| Creature | Cleaned original triangles | Reduced triangles | Reduction |
| --- | ---: | ---: | ---: |
| Shark | 421,880 | 365,816 | 13.29% |
| Dolphin | 31,820 | 30,652 | 3.67% |

Reduction targets the body, preserving the eyes and teeth. Animated surface comparisons across eight sampled poses measured maximum one-way source-to-reduced surface deviations of approximately 1.43 mm for the shark and 3.37 mm for the dolphin at source scale. These are sampled measurements, not a guarantee for every pose. FBX round trips retained one rig and one action per asset, with no unweighted vertices.

Validation: Unity 6000.4.5f1 completed with exit code 0 in an isolated project. All four saved replacements reloaded with their clips and audio. A 60-second simulation confirmed visible pose changes and continued travel: dolphins 60.43–61.34 m, shark 47.05 m. Side, front and three-quarter Unity renders were inspected in `Renders/SuppliedSharkDolphin/Unity`. Detailed reduction checks and Unity logs are in the same review folder.

The toolkit installation check passed. Read-only Quest profile validation was blocked because the isolated editor target is StandaloneWindows64 rather than Android. No APK was built or installed; headset playback/audio and Quest frame time remain unverified. The supplied shark remains a high-detail mesh despite this conservative reduction.
