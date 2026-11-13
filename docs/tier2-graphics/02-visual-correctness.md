# T2-02 — Visual correctness and shader polish

Baseline: local `main` at `e0f47880f1c88a31b7288ce852d9ce4e88dc90e9`, with the uncommitted T2-01 source supplement and `1.0.4-nr.rc6` candidate preserved. The owner explicitly authorized T2-02–06 as one acceptance batch on 12 November 2026. No Git refs, installation or earlier release outputs are modified.

Sources: BBFSS `beta` at `306221668207a5b38c825ed6a28f66fbefed192a`; AO projection correction `bf6aa2cbc23b6e644ce7e4699b13bd7283fd7ca1`. LanternEngine `main` rechecked at `573f83fd366d70bf26ce788edb9775a1b863676a`.

Implemented, all optional artistic changes default OFF:

| Control | Behavior |
| --- | --- |
| Smooth AO/GI normals | Live, depth/normal-compatible neighborhood filtering in Regular/HQ GTAO and SSGI; history reset at the display boundary. AO strength is still applied once by the existing native hook. |
| Sky seam | Live, exact Dream sky shader/geometry/format guards plus upstream GPU geometry validation. Resources created only at the first matching enabled draw. |
| Sky zenith | Live, requires seam correction and the verified 2048×512 panorama. Cached filtered mips are keyed by texture identity/content. |
| POM cavity | Live, upstream exact hash/word-count/checksum guards. Unrecognized compiler output is untouched. |
| Motion blur quality | Live, exact native blur hash and descriptor/profile guards. Existing motion-blur master and upscaler hold-off rules remain authoritative; no vector/FG rewrite. |

The AO projection defect was present: `BeginRenderingFull` reduced the AO target alongside scene attachments, although SSAO and both GTAO variants share a native-target projection. The minimal upstream exclusion is applied automatically to that pass. Full-resolution HQ AO still operates in the internal scene resolution through its existing path.

Shader changes use an additive `Tier2Variants` cache. The recording owner latches settings at a display boundary. Original SPIR-V and modules remain available; enhanced modules/pipelines are immutable and reused. Preparation workers defer only recognized targets to the recording owner. Runtime variant keys are excluded from serialized game-pipeline records; disk shader binaries stay original. The first selection can compile and hitch under the existing asynchronous pipeline policy. No queue drain or shader-cache deletion occurs on toggles.

Optional lightshaft tilt is deferred: it is an artistic control, not required for this milestone, and adding a new coordinate convention to the current G07 path is unnecessary for this acceptance batch.

Changes are reproduced by `engine/tier2-batch.patch` and its guarded apply helper, after T2-01. See [batch validation and acceptance](BATCH-TEST.md) for the build, actual test results and limits.

Acceptance: compare Original/new normals at DLAA and DLSS Quality/Performance; inspect edges for AO halos; rotate the camera under the Dream sky; inspect POM stone close up and moving blur with NR OFF/ON. Repeat switches, return to Original, then relaunch to verify persistence. Gameplay visual acceptance is pending.

Decision: CONDITIONAL GO for owner testing; optional tilt deferred.
