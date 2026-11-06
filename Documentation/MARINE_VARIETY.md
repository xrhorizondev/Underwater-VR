# Marine variety and turtle replacement — 2026-10-27

Six new colored fish variants are present near the starting reef and in the existing schools:

- Yellow tang: deep yellow body and compressed profile.
- Blue tang: blue body, dark side band, yellow tail.
- Copperband butterflyfish: deeper body with copper bands and an eye spot.
- Royal gramma: purple front and yellow rear.
- Lyretail anthias: orange-pink coloration and extended tail tips.
- Sergeant major: pale/yellow body with dark vertical bands.

These are species-inspired artistic adaptations of the existing photographic fish mesh, with different proportions and shader markings. They are not independent scientific scans of each species. Existing clownfish, lionfish, and deep-water interactive fish are retained. Total steering population remains 227 fish in nine groups.

Both simplified turtles are replaced by the Innerscene CC0 loggerhead model. Its continuous shell/skin mesh, beak, small eyes, nostrils and flipper claws replace the old ellipsoids and flat flippers. Original source and licence are in ASSET_CREDITS.md. The swimming shader applies a 4.8-second front-flipper stroke/recovery/glide cycle and subtle rear-flipper motion. The turtle controller follows a slow, bounded path with gentle turns and forward obstacle probing. The shell and head are excluded from the primary flipper deformation.

Checks: all six fish names and two TurtleSwimmer components are present; no missing scripts or materials; all 24 teleport anchors remain. Twenty seconds of steering simulation passed for all 227 fish. Maximum observed pitch was 10 degrees, angular speed 51.42 degrees/second, and step length 0.0247 metres at 30 steps/second. This is not an exhaustive collision or headset performance test.

Previews: Renders/07_Fish_Detail.png and Renders/08_Loggerhead_Closeup.png. Runtime capture uses --fish-preview or --turtle-preview and the included Blender encoding script.

Final runtime verification: the Windows build completed without C# or shader compilation errors. Both exported-player capture runs exited with code 0 and produced 144 frames each. No null-reference or missing-reference errors were found. Fish and turtle MP4 previews were encoded from those actual Unity frames. OpenXR reported no headset available; validation used desktop fallback.
