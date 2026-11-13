# T2-06 — RT/VSM feasibility: CONDITIONAL GO for a separate prototype

Research only, as required by the roadmap. Compared LanternEngine `main` `573f83fd366d70bf26ce788edb9775a1b863676a` `vk_rt_shadow.{cpp,h}` against BBFSS beta `306221668207a5b38c825ed6a28f66fbefed192a` `vk_rt_shadow.{cpp,h}` and `vk_sun_vsm.{cpp,h}`. Both heads were rechecked during this batch. No RT/VSM implementation or controls were added.

| Concern | LanternEngine RT | BBFSS beta RT/VSM | BloodborneX conflict / dependency |
| --- | --- | --- | --- |
| Geometry capture | Captures camera/shadow draw positions into owned buffers, retained world geometry | Separates caster/world-copy capture from BLAS construction; VSM-only mode can avoid building acceleration structures | Current object/camera motion addresses and shader variants must be extended, never replaced |
| RT scene | Per-entry BLAS, TLAS, ray-query shadow passes | Shared scene for RT sun, lamps and optional bounce; additional caster/culling bookkeeping | Requires Vulkan buffer device address, acceleration structure/deferred-host-operation and ray-query feature/device setup and lifetime ownership |
| Sun VSM | No competing beta VSM lifecycle in this selected implementation | Persistent caster tracking, bounds/readback, virtual page table, physical depth atlas and dirty-page rendering | G05 PCSS, G09 filtering and G10 map resolution need mutually exclusive ownership per draw |
| Local lights | Ray-traced lamp option / point ray shader | Shared RT geometry and light integration | G11 current point shadows must remain a fallback, not be multiplied by a second shadow contribution |
| Histories | RT raw masks and scene resources | VSM residency plus RT and optional GI histories | Reset coherently on cuts/loading/resize and with NR/SR/FG |

Resource evidence: Lantern RT declares a 192 MiB scratch buffer and up to 8 million captured positions, with BLAS/TLAS/world-copy storage additional and dependent on visible geometry. BBFSS VSM declares a 4096² 32-bit depth atlas (64 MiB), `12<<20` items ×8 bytes (96 MiB), plus page state, draw/bounds/readback buffers and retained casters. Its explicit initial atlas/item headroom check alone covers roughly **160 MiB**; this is not total residency. Animated/instanced geometry and alpha-tested casters materially affect correctness and cost.

The RTX 3070 supports the hardware class needed for RT, but device support does not establish a workable NR+FG+RT budget. There is no same-scene gameplay timing/VRAM evidence here. VSM is also not a small shader-only import: capture completeness, removal, page residency and draw ordering must be validated. Porting both owners would create competing geometry and acceleration lifecycles.

Minimum separate prototype: choose one capture/scene owner; first implement capability checks and OFF-by-default capture with bounded memory and retirement counters. Compare captured static/animated/cut-out/instanced casters against the existing shadow passes. Then prototype one sun mode, with exact fallback to G05/G09/G10 and no point-light/GI changes. Measure NR OFF/ON before choosing RT sun versus VSM. Reuse upstream caster and RT shader tests. Do not make a VSM allocation failure silently enable an even more expensive RT fallback on an 8 GB card.

Decision: **CONDITIONAL GO for a separately authorized, measured prototype; NO-GO for shipping RT/VSM in this batch.** Required unblocking evidence: actual memory headroom and same-scene timings, capability audit, and a single capture-owner design. The roadmap requires a separate instruction to implement RT/VSM. Current Tier 1 shadows and updater/package identity remain unchanged.
