# T2-03 — GGX and local lighting

Baseline and batch authorization: [T2-02](02-visual-correctness.md). Primary source: BBFSS beta `306221668207a5b38c825ed6a28f66fbefed192a`, including `vk_ggx_patch.h`, `vk_ggx_lobes.h`, `vk_local_falloff.h` and `vk_spirv_index.h`. This contains the full/hybrid GGX and finite-core inverse-square work described by roadmap commits `d2e40c3b0096` and `c7a7a51e7f62`.

| Setting | Default | Application |
| --- | --- | --- |
| Directional shading | Original | Live Original / GGX hybrid / GGX full |
| Local-light GGX | Off | Live, upstream expression guards |
| Specular antialiasing | Off | Live, independent switch |
| Environment GGX tails | Off | Live, independent switch |
| Inverse-square local lights | Off | Live, exact known lamp family and expression guards |
| Local light intensity | 1 | Restart; range 0.1–4, baked SPIR-V coefficients |
| Local light source size | 0.05 | Restart; range 0.01–1, finite inverse-square core |
| Local light sharpness | 1 | Restart; range 1–8, independent lobe coefficient |

The new immutable variant cache makes discrete shader choices live, exceeding upstream's startup-only selection without rewriting shader code each frame. Scalar coefficient sliders remain startup-only to avoid unbounded pipeline variants while dragging. The existing restart footer includes these three scalars. Launcher edits apply at next launch; in-game rows are live where indicated. Settings serialize through the shared preset serializer; provided RTX 3070 profiles are untouched.

Directional GGX requires the known directional shader plus matching instruction shapes. Other lobe changes require the exact upstream material expressions; unknown shapes retain Original. Patch/check failure retains the original module/key. Switching to Original with the independent switches OFF selects the original shader module. There are no new lighting textures or changes to the NR model, composition, upscaler or frame-generation implementation.

The supplement imports GGX/lobe/falloff tests. Malformed/unrelated-shader guard tests can run without private captures. Full directional image replay requires externally supplied game shader/input captures and is reported separately from these tests. See [batch validation](BATCH-TEST.md).

Owner acceptance: use one fixed view for each of skin, cloth, polished weapon, wet paving, distant lamps and a dark interior. Compare Original → hybrid → full → Original, then enable each independent switch separately. Keep inverse-square OFF during the first comparison; test it afterward near a lamp for clipping. Repeat with NR OFF/ON, and save/apply a custom preset.

Decision: CONDITIONAL GO for acceptance testing. Visual quality, gameplay frame time and capture-based full directional replay are not established by compilation or guard tests.
