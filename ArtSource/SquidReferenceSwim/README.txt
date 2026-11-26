SQUID REFERENCE SWIM - 26 NOVEMBER 2026

First pushed the previous repaired project state to origin/feature/bug-fix:
ae3cd54abc13d4695bda911c7174b47c80c29568.
The new animation was then authored and applied locally for review.

Watched https://www.youtube.com/shorts/caNhgitUqy4 in the browser. The visible
reference shows continuous lateral fin undulation, mantle contraction, and
streamlined trailing arms with small movements. Replaced the original splayed
arm waving cycle with a four-second Swim clip: three travelling fin waves and
two mild mantle contractions per loop, a more compact arm pose, and small delayed
tip motion. This is a visual interpretation of that short, not motion capture.

The repaired old squid is retained. Exactly identical mesh vertices, faces, UVs
and rest-bone matrices were verified against SquidCrownRepair. All 1,638 rigid
crown/eye vertices remain motionless relative to the body over 97 frames. Root
translation is zero; existing Unity navigation still owns world travel.
7,950 triangles, 30 bones, two original materials.

FBX round trip: zero opaque open edges, 10 shaft contours at both test stations,
97 frames checked, normalized weights and identical loop endpoints. Unity
6000.4.5f1 exit 0: imported clip duration four seconds; main-scene squid and prefab
passed deformation and animated visibility bounds at all 97 samples.

Updated ReferenceSquid.fbx and its importer clip range, GiantSquid.prefab,
main-scene squid bounds, and Squid_Old_Repaired_Review.unity. No gameplay travel,
collider, placement, project settings, mesh or texture changes.
No Quest hardware build, performance or stereo test.

Source: ReferenceSquid.blend here. Regenerate with
Tools/Blender/animate_squid_reference_swim.py. Validation scripts:
validate_squid_reference_swim.py and audit_squid_swim_invariants.py.
Unity isolated-project entry point: Underwater.Editor.SquidReferenceSwimReview.Run.
Preview: Renders/SquidReferenceSwim/SwimPreview.gif (4 seconds, 12 preview FPS).
Reports: Invariants.json, RoundTrip.json and Unity/UnityValidation.txt there.

Quest APK built and signature-verified on 26 November 2026. Current build evidence:
Renders/SquidReferenceSwim/QuestBuild/BuildRecord.json. APK is in
Builds/Quest/SquidReferenceSwim-20261126/quest.apk (local artifact, excluded from Git).
Source and scene SHA-256 match the isolated build input. No headset test performed.
