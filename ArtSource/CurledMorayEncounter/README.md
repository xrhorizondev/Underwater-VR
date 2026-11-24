# Touch-triggered curled moray

Integrated in Assets/Scenes/Underwater_Realistic.unity. Uses the supplied curled moray's 24,000-triangle mesh, 22 bones, original shared atlas material, and new CurledRest / SwimStraight clips. The comparison models remain separate authoring assets.

Behavior: Resting -> Rising -> Uncurling -> Swimming -> Curling -> Settling -> Resting. Contact from either XR palm or fingertip is tested against trigger-only bone capsules, with 25 mm padding for hand surface thickness. Nearby head/hand presence alone does not trigger it. A contact latch requires release before another contact, and successful escapes have a five-second cooldown. Menu and simulation-range checks pause the encounter. The previous moray movement and animation drivers are disabled; other creatures retain their existing owners.

The eel chooses an away-biased destination 2.7–4.0 m away. Collision-aware sweeps and seabed envelopes check the route, turning space, curled landing footprint, and rise/descent before starting. Travel uses ReefSphereBody and conservative box sweeps. If there is no safe route, it stays curled. If motion stalls, it recovers by curling and settling at its current safe spot. There is no teleport back to the original location or XR rig movement.

The original root (-2.66, -18.88, 70.08) had overhead rock blocking uncurling. The initial resting placement is now approximately (-1.45, -21.47, 70.40), on a clear seabed patch about 1.25 m away horizontally in the same habitat. The source morphology was posed into a horizontal rest curl so the eel can lie on the seabed.

Unity 6000.4.5f1 isolated-project integration and validation exited 0. The main-scene copy matches the validated saved scene. A full contact cycle moved 3.89 m horizontally (5.09 m total path), reached every state, settled within 2.9 cm of its clearance target, and passed 588,148 sampled animated-vertex envelope checks. Additional right-palm and repeat fingertip cycles passed collision checks. A fully obstructed route was rejected without pose change or teleport. Cooldown and proximity rejection passed. The swim spine measured 1.70 m longitudinally with 0.12 m lateral extent, confirming it straightens for swimming.

Runtime work samples 22 bones; it does not bake/read mesh vertices. Render bounds cover rest, swim and transitions. No new lights or persistent preview cameras were added. These are editor tests, not a Quest hardware or FPS result. Wearer confirmation of touch timing and motion remains necessary.

Runtime: Assets/Ocean/CurledMorayEncounter.cs
Repeatable integration and checks: Assets/Ocean/Editor/CurledMorayIntegration.cs
Authoring source: ArtSource/CurledMorayEncounter/CurledMoray_Encounter.blend
Preview: Renders/CurledMorayEncounter/TouchEscape.mp4
Reports: Renders/CurledMorayEncounter/Validation.txt and EdgeValidation.txt

## PC Play-mode repair

The earlier idle clip was too subtle, and escape required physical hand/fingertip contact. PC virtual hands use fixed offsets, so clicking did not invoke that interaction. The updated controller supports a left-click on the moray from within two metres when no XR headset is active; it uses a physics ray and rejects occluding reef/animal colliders and out-of-range targets. XR contact remains supported. A failed route no longer latches the touch as successful; it can retry after cooldown while contact remains, with additional escape headings and a forward-relative fallback.

The resting full-body deformation is now clearly visible. Bounds were recalculated for the larger idle movement and every blend into the straight swim. Main-scene skin smoothness was reduced to 0.08, normal strength to 0.25, and the broad specular/environment reflection lobes disabled to reduce blue-white glare without repainting the original color atlas.

Actual Unity PC Play-mode checks passed through normal Update: resting vertex movement 0.149 m at the tail, menu freeze, the production desktop-ray touch handler with reach/occlusion rejection, and a full escape/curl/settle path of 5.70 m. This tested the click handler directly with a ray in Play mode; the OS mouse button itself was not injected. Validation: Renders/MorayPCProof/Validation.txt. Headset behavior still requires wearer confirmation.

PC controls: press Play, close the dive menu, focus Game view, approach within two metres, and left-click the eel. Esc/menu pauses the encounter.
