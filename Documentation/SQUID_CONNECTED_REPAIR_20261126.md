# Squid connected arm repair — 26 November 2026

The earlier tube-capping repair did not resolve the visible pipe rims. Rebuilt the head and ten arms as one connected surface using voxel union, transferred normalized skin weights, and used continuous UV mapping across the head/arm junctions. Retained the mantle, eyes, fins, 30-bone rig, Swim clip and scene transforms. Removed tiny detached remesh fragments at the tips.

Final Unity mesh: Assets/UnderwaterArt/BlenderSquid/ReferenceSquid.fbx. Existing meta preserved.
Authoring source: ArtSource/SquidConnectedRepair/ReferenceSquid.blend.
Reproduce: Tools/Blender/repair_squid_connected.py, then Tools/Blender/validate_squid_connected.py.

Validation: 7,446 triangles, two materials, 30 bones. Export budget audit passed. FBX round trip has zero opaque boundary edges after welding UV splits, normalized weights, 49 finite animation samples and zero loop endpoint error.

Unity 6000.4.5f1 review ran in .utmp/SuppliedReview and exited with code 0. Confirmed the scene renderer references the repaired FBX. Captured and visually inspected the head/arm junction at four animation phases, including the close underside angle; the open cylinder rims are gone. Captures and report: Renders/SquidConnectedRepair/. Editor SampleAnimation captures require explicit world bounds to avoid stale culling results; this override exists only in the review script. Scene was not saved by review. Hardware appearance/performance has not been tested; no build or installation performed.

This supersedes the previous SQUID_SURFACE_REPAIR_20261126.md validation limitation.

