# Lead review corrections

- Hand touch and grabbing now share the same palm anchor. Head/controller poses refresh before rendering, including held-fish follow. Old v2 hand-fit preferences are superseded by clean v3 defaults. Calibration requires both grips and right trigger to prevent accidental changes in the habitat menu.
- Imported fish retain their models and textures: smooth close-up subdivision, 2048-pixel streamed atlases with ASTC 4x4, restrained scale/pupil sheen, and a reversible natural-color shader correction for the blue-purple butterflyfish. Medium/distant meshes remain lightweight. All 34 remain grabbable.
- Existing 30 school fish are distributed across ten smaller schools, rather than adding more fish. Twelve shared-asset coral colonies fill selected shallow/ledge/wreck seabed gaps off the navigation lane. Dark/open habitats remain sparse. Ship and anchor placements preserved.
- Grouper stroke rate and strength use actual collision-limited travel speed. The second grouper now uses the same slow speed/stroke range, with its extra competing body-motion driver disabled.
- Turtle clips match actual travel; 158 neck vertices across both turtles were blended between neck/head bones. Head turns ease to a roughly seven-degree limit. Legacy bone clocks remain active so the head does not accumulate stale rotations.

Validation: Unity 6000.4.5f1 compilation and saved-scene checks passed; twenty-second turtle/grouper movement simulations and head continuity/palm-anchor tests passed. Eighty moving-hold/contact cases and eighty side/belly hand-follow cases passed, including both hands. Test clones now omit the live reef's bounded seabed field when positioned in the isolated test space. Imported textured-eye fish are validated as textured models rather than incorrectly requiring procedural pupil tags.

A broader legacy regression subsequently reported a separate angler cruiser navigation failure (distance 5.277522 m). No unrelated angler navigation changes were made. Headset fit, visual quality and FPS need a new-build retest. The existing APK has not been rebuilt with this pass.
