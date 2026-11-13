# Natural underwater audio and body bubbles — 13 November 2026

Removed continuous music from ReefSoundscape. Added a 32-second stereo water/bubble bed, 48-second distant blue-whale layer, and sparse 27-second reef-click layer. Existing regulator breathing and warning narration remain; ambience ducks during narration and resumes after app focus/pause. Layers are baked offline, with no runtime synthesis.

The whale layer is an edited NOAA/PMEL Northeast Pacific blue-whale recording. Original sample is published at 10x recorded speed; this version uses a band-limited, faded eight-second excerpt at one fifth of the sample's playback rate (2x recorded speed), with quiet spacing. Attribution, source URL and processing details are in Tools/NaturalAudio/README.md. Other ambience layers are original sound design, not species-specific wildlife recordings.

Moved regulator/body bubbles from the mostly off-screen side positions into the lower peripheral view: forward 0.36 m, sideways 0.10–0.18 m, below head 0.28 m. Reduced sideways drift and increased size/rim visibility while retaining eye-distance fading, reef collision, teleport clearing and the 64-particle cap.

## Validation

Unity 6000.4.5f1, isolated project; final validation exited 0 (Logs/natural-audio-final.log).

- Three stereo clips imported as PCM/decompress-on-load; no silence or clipping. RMS water 0.0313, whale 0.0920, reef 0.00684.
- Bubble simulation peaked at 27 particles and recorded 2,204 in-camera samples over five seconds. First-person and close bubble previews were rendered and inspected.
- Existing animation, squid full-body clearance, ray/jellyfish, fish schooling, grab/release, hazards, collision, bounds and teleport regressions passed.
- Quest toolkit build exited 0 and reports Succeeded. Manifest, ARM64 payload and separate APK v2 signature verification passed. Existing deprecated API/package warnings remain.

## Build and Day 3 status

Builds/DayThreeNaturalAudio/quest.apk — 71,454,646 bytes. ARM64 IL2CPP, existing Android configuration and local test signing.

All planned Day 3 development is implemented. Final acceptance awaits wearer confirmation of sound balance, audible playback and visible body bubbles in this build. Later-day wreck, trench and finale tasks remain separate. Device frame-rate stability has not been established by these editor checks.

Installed on the reconnected Quest 3 with adb install -r, preserving app data. UnityPlayerGameActivity launched successfully and process 24869 was verified running. Renders/Natural_Audio_Preview.mp3 provides a desktop preview of the approximate layered mix. Headset spatial playback differs from this stereo preview.

APK SHA-256: 46299B4396E3032473A71B7A212CF4D5B4000FE583F0B18CE9C66D44DA9C92C5.
