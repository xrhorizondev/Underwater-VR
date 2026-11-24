# Natural dolphin cruising

Main scene: Assets/Scenes/Underwater_Realistic.unity.

The dolphins use a gently varying heading field and early habitat/reef anticipation. There are no orbiting point targets, formation slots, or timed split/regroup schedule. Neighbour alignment and weak cohesion create loose association; separation anticipates converging travel. Group labels describe the observed relationship instead of commanding timed behavior. An isolated dolphin cruises independently.

The measured body length is 3.37 m. The current calm-cruise profile uses a reference speed of 0.28 body lengths/s, a normal turn radius near 1.9 body lengths, and a shorter avoidance radius when clearance becomes urgent. Speed, angular velocity, angular acceleration, pitch and banking change gradually. The reef controller moves the dolphin forward without wall sliding. These settings are artistic calm-cruising parameters, not fitted wild-dolphin measurements or a universal profile for every fish species.

Active fluking alternates with individual glide intervals. Propulsion fades in/out while the body continues travelling; stride length is related to body length. Body flex and fluke amplitudes are reduced for this cruising style. Mouth, eye tracking and staggered blinking retain their separate facial clocks. Large dolphin vertical fluking remains distinct from the project's eel body undulation and fish tail motion.

Research used for qualitative guidance:
- https://pubmed.ncbi.nlm.nih.gov/37345501/ — bottlenose dolphin fluke-and-glide gait.
- https://pubmed.ncbi.nlm.nih.gov/10504311/ — swimming kinematics and gliding at depth.
- https://www.wcupa.edu/sciences-mathematics/biology/fFish/documents/2002ICB.pdf — maneuverability and morphology.

Validation: a 180-second three-dolphin scene simulation passed with approximately 159/159/160 m travel, closest root spacing 5.49 m, maximum heading rate 9.01 deg/s, maximum heading acceleration 4.74 deg/s² and maximum linear acceleration 0.121 m/s². No stops or reef overlaps occurred; movement aligned with heading and remained within a 9 m habitat radius. A solitary dolphin completed a 45-second collision-clear patrol. Actual PC Play-mode updates verified body/facial animation, 28 captured quiet-tail moving frames, powered strokes, and menu pause. The saved scene matches the validated scene copy. Quest device feel and FPS have not been measured.

The original 7,300-triangle / 40-bone mesh, shared eye material and two blink shapes are retained. Stored neutral local transforms prevent procedural offsets accumulating after scene reload. No runtime mesh baking or added lights.

Runtime: Assets/Ocean/DolphinSwimController.cs
Profile and scene checks: Assets/Ocean/Editor/NaturalDolphinReview.cs
Rig/eye asset preparation: Assets/Ocean/Editor/DolphinMotionIntegration.cs
Actual Play-mode check: Assets/Ocean/Editor/DolphinLiveSyncCheck.cs
Reports and fixed-camera preview: Renders/NaturalDolphins/
Preview video: Renders/NaturalDolphins/NaturalCruise.mp4

Stop Play and reopen the main scene to load saved parameter changes, then press Play and close the dive menu.

## Pectoral-fin visibility correction

The previous 1–2 degree fin motion depended on propulsive effort, so the fins looked stationary during glides. The fins now use a separate slow trim clock driven by actual travel, plus stroke-related root articulation, distal flex, feathering and mirrored steering. Trim remains active through quiet-tail gliding and fades when travel stops. The steering term now lifts/lowers the sides differentially. This is small control-surface movement, not propulsion by pectoral flapping.

Geometry isolation checks held the tail pose/effort fixed and measured 6.1–8.6 cm peak fin-vertex movement across all three dolphins. Bounds were expanded from sampled poses. Actual PC Play-mode checks measured 5.2–7.3 degrees of fin-root motion on both sides, including quiet-tail travel, with existing body/facial motion and menu pause intact. Natural cruising/collision checks also passed. No device FPS claim is made.

Fin checks: Assets/Ocean/Editor/DolphinFinReview.cs
Reports and actual Play-mode close-up: Renders/DolphinFins/Validation.txt, Live/Validation.txt, FinMotion.mp4.

## Coral contact stall correction

The user's 13.77-second recording showed a dolphin frozen beside the wreck while other animals moved. A longer simulation with a nearby viewer reproduced a sustained stall at 468.2 simulated seconds beside `Living coral | 3`: the body sphere remained nominally clear, but only about 5.5 mm from the collider. There was no clearance-recovery path, so blocked travel drove animation effort toward zero and made the animal appear frozen. The renderer audit found the three animated skins active and the legacy static visuals disabled.

DolphinSwimController now detects lack of physical-centre progress for 0.75 seconds and performs a brief, swept back-off away from nearby solid surfaces, then resumes normal forward steering. The back-off lasts at most 1.2 seconds at no more than 0.2 m/s. ReefSphereBody retains all solid sweeps and seabed constraints. Tightened turn-lift handling also removes the earlier 6 mm dead band. No relocation/teleport, scene-placement change or XR rig movement was added; other species retain their existing behavior. The initial pivot explanation was ruled out during diagnosis; no generic body-pivot change remains.

Validation: with recovery disabled, the coral pin reproduced. With recovery enabled, all three completed 20 simulated minutes with roughly 1,052–1,064 m travel and a longest near-zero movement interval of 0.75 seconds. The actual Play-mode test fast-forwarded to 465 simulated seconds, resumed normal Update at the problematic location, recorded one clearance recovery, and measured 10.83 m subsequent physical-centre travel with a longest stop of 1.07 seconds. Reef collision checks passed. These are local editor/PC checks, not Quest device validation.

Reports: Renders/UserDolphinIssue/BaselineStall.txt, StallAudit.txt, RecoveryLive/Validation.txt.
Regression tools: Assets/Ocean/Editor/DolphinStallAudit.cs and DolphinStallLiveCheck.cs.
Restart Play mode to load the updated controller.

## Compact wreck patrol
The main-scene dolphins now use a smaller clearance-checked oval centred at (2, -13.5, 66), with radii 4.5 x 4 m and cruise speed 0.58 m/s. This replaces their wider roaming in this scene. The route has 0.35 m additional swept clearance, smooth continuous turns, gentle bobbing and spacing-based speed adjustment. Body, tail, flipper, eye and mouth animation remains active.
Validation: ten simulated minutes, no stops, maximum radius 4.5 m, closest pair spacing 6.48 m. Actual Play-mode movement, skin deformation, fin motion, blinking, mouth motion and menu pause passed. Reports: Renders/CompactDolphins/Validation.txt and Live/Validation.txt.


## Continuous glide stroke
The dolphin.mp4 recording showed stiff sliding with weak tail movement. The gait previously multiplied both tail phase speed and amplitude by drive, reducing both to zero during a glide. Moving dolphins now retain a low-amplitude continuous body/tail wave, with slower strokes in glides and smooth amplitude transitions. Turning spine bends no longer disappear with propulsion effort. The compact route and scene placement are unchanged.
Each dolphin passed a 60-second gait check including all complete three-second windows: minimum tail-joint rotation 9.32 degrees; minimum stroke rate 0.259 Hz. Actual Play-mode fin, body, eye, mouth and menu-pause checks passed, as did the ten-minute compact-route check. Reports: Renders/DolphinVideoIssue/.
