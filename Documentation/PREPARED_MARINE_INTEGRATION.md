# Prepared marine models

The six reduced Blender models replace the visible geometry on existing creatures in `Assets/Scenes/Underwater_Realistic.unity`.

| Asset | Instances | Triangles per model | Animation |
| --- | ---: | ---: | --- |
| Loggerhead turtle | 2 | 6,199 | Original swim |
| Manta ray | 1 | 7,600 | Original feeding sequence |
| Octopus | 1 | 9,499 | Original skeletal and morph animation |
| Reef shark | 1 | 5,296 | Original swim |
| Moon jellyfish | 11 | 4,638 | Original pulse, source travel removed |
| Box jellyfish | 3 | 5,070 | Newly authored pulse |

FBX assets, local textures and Unity materials are in `Assets/UnderwaterArt/PreparedMarine`. Editable Blender sources, preparation reports and animation previews remain in `D:/GIT Project/model/Prepared_FBX`. The original archives and GLB are preserved.

Each visual is a child of its original gameplay object. Original actor placement, swimming controllers, solid bodies, touch colliders, warning surfaces, consequence settings, narration and fact configuration are retained. Superseded renderers are disabled; their objects remain available to existing references. Animation deforms the imported child and does not take over navigation.

All instances reference their imported FBX animation clips directly. Duplicate embedded clips were removed; the final scene is 10,716,696 bytes. Deformation, visibility bounds and turtle-eye checks passed again after this correction.

`PreparedTurtleHead` maps the existing TurtleSwimmer head-turn property to the imported head bone, including a guard against accumulating rotation when animation is culled. A dedicated eye material consumes the existing blink property. The octopus shader has an opt-in texture path for the new skinned mesh, preserving its existing encounter tint response; older users of that shader keep its previous defaults. Jelly warning colors also reach the new renderers. The eye validator accepts both the legacy eye mesh and the new two-sided rigged eye geometry.

Unity 6000.4.5f1 checks run in the isolated project:

- Actual skin deformation on all 19 instances, including animated morphs.
- Visibility bounds sampled over each clip, with additional padding and vertex-containment checks.
- Exact comparisons of original gameplay settings, actor transforms and existing collider settings during installation.
- Turtle head-control response, culled-frame stability, blink material and geometry on both sides.
- Jelly proximity/caution stages and box-only consequences; octopus encounter tint reaching its new renderer.
- Existing habitat regression checks for swimming, collision, navigation, touch, hazards and open-water behavior.

Unity review renders are in `Renders/PreparedMarineUnity`. Filtered validation results are in `Documentation/PREPARED_MARINE_VALIDATION.txt`.

Headset appearance, stereo transparency, animated visual-to-touch alignment and FPS still need a device check. In particular, the moon jelly retains a 206-bone source rig. Triangle reduction and editor tests do not establish Quest performance. Existing warning proxies are retained, so their alignment against the changed jelly silhouettes deserves attention. The source manta feeding sequence retains its original timing and endpoint behavior.
