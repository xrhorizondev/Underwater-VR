# Original animated moray preview

Visual reference: https://sketchfab.com/3d-models/moray-eel-low-poly-d9dc9235efb54283878c45cc857a6d49 by Nyilonelycompany. This is an independently authored approximation using the broad moray silhouette and olive/cream-spotted palette. No Sketchfab mesh or texture was downloaded or incorporated. It is not an exact replica of the referenced commercial asset.

Authoring recipe: Tools/Blender/build_moray_preview.py, Blender 5.3.0.

- 4,596 triangles, 15 bones, one skinned visual mesh and one material.
- Two 1024-pixel textures: baked olive/spotted colour and a subtle normal map. The .blend packs its textures; the separate PNGs accompany the FBX.
- Small eyes on both sides, blunt head, nostrils, teeth, separate hinged jaw, continuous dorsal ridge and tapered tail. The skin and mouth geometry are an approximation for low-poly use.
- Idle: 3-second breathing/jaw cycle; Swim: 2-second body-wave cycle; React: 2-second stronger jaw-open reaction. No navigation/root translation is baked into the rig.
- All three takes survive FBX round trip with UVs/material, deforming mesh and constant root translation. Sampled endpoint errors are zero. See MorayEel.audit.json and RoundtripValidation.json. Names receive the MorayRig prefix on FBX reimport; use the actual imported clip ranges rather than assuming a universal frame offset.
- Side, opposite and three-quarter views and the mouth interior were rendered and inspected. Animated previews: Renders/MorayPreview/Swim.gif and Idle.gif; MP4 versions accompany them. HeadAndJaw.png shows the reaction pose.

No Unity asset, scene, existing moray encounter or headset build has been changed. Unity import/rig/clip/collision and standalone Quest performance checks remain a separate step if the user approves this preview. Mesh budgets and rendered previews do not establish device FPS.
