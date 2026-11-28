# Smoothness follow-up — 28 November 2026

- Quest eye render scale changed from 1.0 to 0.85 (27.75% fewer eye pixels, with some loss of image sharpness); 4x MSAA and existing balanced coral LOD transitions retained. Baseline frame target is 72. Stereo FOV remains controlled by the headset projection.
- Shared animal heading now eases yaw proportionally near its goal and retains the turning side when a goal crosses directly behind it. Normal schooling yaw reduced from 36 to 26 degrees/second; emergency navigation keeps its existing limits and collision safeguards.
- Reviewed small-fish grabbing: current surface-support contact, size-adaptive finger fitting and tracked wrist alignment are retained. A universal snap rotation would override the existing relative catch orientation. Previous contact results are documented in HAND_FISH_20261128.md; those are historical, not new passing tests.
- Fresh ContactFitValidation.ValidateHands attempt: Unity 6000.4.5f1 exited 1 before executing validation, even with licensing-service access. Evidence: Logs/SmoothGrab-20261128.log. No new movement/contact pass or APK is claimed.
- Connected Quest detected via ADB. Recent native logs contained frame-retirement waits, without usable focused app FPS samples. Earlier measured 33–38 FPS is the baseline, not a measurement of these changes. Changes still require compilation, movement/contact regressions and focused headset profiling before stable 72 FPS can be claimed.
- Targeted git diff whitespace check passed. Existing unrelated workspace edits preserved.
