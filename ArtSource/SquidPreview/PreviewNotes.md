# Approved squid preview

Created in Blender 5.3 from the supplied recording's silhouette/motion cues, not a scan or species-accurate reconstruction. Authoring recipe: Tools/Blender/build_squid_preview.py.

- 6,288 triangles, 30 bones, one skinned visual, two material slots.
- Eight arms plus two longer feeding tentacles; closed tips; visible eyes on both sides.
- Broad translucent fin membrane; natural muted silver/copper skin; two 1024-pixel texture maps.
- Two-second Swim cycle, offset arm/fin motion, no root motion. The source clip is frames 1–49 at 24 FPS. Blender FBX round-trip import offsets it to frames 2–50 and names the baked action SquidRig|Scene; name the imported Unity clip Swim and use its actual imported bounds.
- Export audit and FBX round-trip passed: one mesh, two materials, 30 bones, preserved UVs, measurable deformation, zero tested loop/root-motion error. See ReferenceSquid.audit.json and RoundtripValidation.json.
- ThreeQuarter.png, Opposite.png, Side.png and animated Swim.gif/Swim.mp4 are in Renders/SquidPreview.

The user approved this preview. Both Unity giant-squid encounters now share the replacement visual while preserving separate SquidSwimmer controllers and full-body collision ownership. See Documentation/BLENDER_SQUID_RESULTS.md for import normalization, animation/bounds validation and delivery status. Preview budgets do not establish headset FPS.
