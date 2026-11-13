# T2-04 — Exposure, tonemapping, bloom and SSS

Baseline/batch scope: [T2-02](02-visual-correctness.md). Sources: BBFSS beta `306221668207a5b38c825ed6a28f66fbefed192a`, with native exposure policy originating on `bbfss`. All new effects default OFF/Original and persist through the common serializer/preset system.

| Control | Application / fallback |
| --- | --- |
| Native auto exposure | Live, bounded around authored area exposure (-1 / +0.5 EV); recalibration button and native status. Verified-entry/caller/resource guards; unsupported cases retain authored exposure. |
| Tonemapper | Live Original / GT7 / ACES; independent exposure 0.25–4 for alternatives. Existing HDR scene curve takes precedence and disables alternative tone insertion. |
| Bloom | Live Original / Froyok; blend 0–1 and spread 0–1 live. Unsupported/missing frame input or memory headroom retains native bloom. |
| Native skin SSS | Live, guarded allocated native material/view resources; visible Off / Waiting / Active / Unavailable status. |

Native hooks were adapted to BloodborneX rather than replacing its hooks. Exposure uses `Prepare(..., preserve_context=true)` and BloodborneX's 144-byte saved XMM frame/return-address layout. Fixtures exercise this ABI. SSS inspects the original render-view entry before the existing Graphics hook is installed, then shares that hook; the frame tick restores owned values when disabled. Loading/cinematic generation invalidates exposure calibration without replacing existing loading optimizations. No game file is patched on disk.

Order: authored/native exposure → scene/NR/SR and native postprocessing → Original/Modern DoF recombine → Froyok pyramid → final composite / Original or alternative tone curve → existing HDR/presenter. There is no second NR evaluation or new NR color conversion. If the expected DoF recombine/composite is absent, Froyok falls back for that frame.

Froyok's upstream replacement was adapted for live fallback: the original stock bloom additions remain selectable in the IR. A negative runtime blend selects those exact original operands; successful generation supplies the alternative texture and a nonnegative blend. Its additional 16-byte push block lives at offset 144; Original's 128-byte game ABI and the existing tone block at 128 remain stable. A versioned shader-cache namespace prevents old shader metadata from being reused with the new compiler passes, while old caches are retained. Bloom metadata has its own tagged serializer tail.

GPU resources are lazy, reused across frames and retained safely across toggles. Resolution changes defer old owned image destruction through the scheduler. The existing NR, HDR and frame-generation lifecycle APIs are preserved.

Licenses retained: GPL notices, `licenses/FROYOK-BLOOM.txt`, and GT7's Polyphony Digital MIT notice in the source and `licenses/GT7-TONEMAPPER.txt`. Packaging copies engine notices to the portable package and full source archive.

Validation and exact candidate: [BATCH-TEST.md](BATCH-TEST.md). Owner checks: Original baseline first; switch each effect separately; auto-exposure dark/bright transitions and recalibration; bright lamps with bloom; SDR GT7/ACES; HDR scene-curve precedence; SSS character skin; loading/cutscene transitions; NR OFF/ON and multipass. No gameplay visual or HDR-display acceptance is claimed.

Decision: CONDITIONAL GO for owner testing of the combined batch.
