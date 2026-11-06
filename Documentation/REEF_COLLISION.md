# Reef collision update

The generated Underwater_Realistic scene includes reef mesh colliders and conservative box volumes around coral colonies on ReefSolid (layer 8). Coral volumes deliberately keep swimmers out of branch gaps. Arch stones have separate colliders so the swim-through remains open.

244 moving fish, turtles and rays use body-sized swept spheres; fish steer before contact and stop at obstacles while turning. Held fish use the same movement constraint, and release from their last safe position. Spawn positions are checked after all reef colliders are installed.

Diver swimming uses a CharacterController with no step climbing. Room-scale horizontal motion sweeps the capsule, vertical head motion sweeps a head sphere, and blocked shell/menu teleport destinations are rejected. Physical hands are tracked directly; they are not solid physics hands.

Validation: wall sweeps, diver wall sliding, blocked/clear teleport, horizontal room-scale wall contact and floor contact passed. 227 schooling fish across nine groups simulated for 20 seconds with no overlap against reef colliders. Maximum movement per 1/30-second step: 0.0247 m. Headset hardware testing remains outstanding.
