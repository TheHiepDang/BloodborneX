# T2-05 — Probe GI feasibility: NO-GO for implementation in this batch

Inspected BBFSS beta `306221668207a5b38c825ed6a28f66fbefed192a`: `vk_ssgi.{cpp,h}`, `ssgi_probe.comp`, `ssgi_common.glsl` and the existing BloodborneX G03/G04 path. The owner authorized the remaining milestone batch; this roadmap milestone explicitly requires a measured GPU-budget gate before implementation.

Current placement: G03 consumes opaque scene color plus depth, G-buffer normals/albedo and camera reprojection before NR/SR, with half-resolution light history and denoising. G04 supplies stochastic SSR through its existing game-pass replacement. Display boundaries and scene-history resets coordinate current GI/SSR/NR/SR/FG histories. The new smooth-normal option preserves that path.

Upstream probes are an alternative branch of that same GI contribution, not a second overlay. They add a 16-pixel probe grid with 8×8 directional radiance, double-buffered geometry/radiance/three SH images, full-internal-resolution diffuse/specular histories, material/specular G-buffer capture, and optional glossy reflection histories. The beta class also accepts an `RtShadow` owner for its separate optional ray-bounce path; screen-space probes themselves must not be described as requiring hardware RT. Wet-ground response depends on correct material/reflection reconstruction.

Image-size estimates from upstream allocations (not measured residency or performance):

| Internal size | Probe image set | Additional half-resolution glossy images | Full-resolution glossy alternative |
| --- | --- | --- | --- |
| 1920×1080 | about 72 MiB | about 12.9 MiB | about 51.4 MiB |
| 960×540 | about 18 MiB | about 3.2 MiB | about 12.9 MiB |

Probe estimate: `2*ceil(w/16)*ceil(h/16)*(16+8*8*8+24) + 2*w*h*16` bytes. Glossy estimate: `26*ray_w*ray_h` bytes. These exclude existing scene/GI images, allocation alignment, temporary overlap during resize, NR model/history, FG and any RT structures. They cannot establish headroom on an 8 GB card. The extra GPU passes and ray work also need scene-matched timing.

Required baseline captures with NR OFF/ON are unavailable in this automated session: no actual gameplay scene was launched. Consequently the roadmap's measured frame-time/VRAM gate is unsatisfied. This is a specific prerequisite, not a renderer compile failure. Existing G03/G04 remain intact; no nonfunctional probe controls are exposed or experimental GI files packaged.

Smallest next step: in the current test build record a fixed dark interior and wet street at 1080p, same camera, NR OFF/ON, upscaler mode, FPS/frame time and VRAM. Then port probes alone into a mutually exclusive GI mode. Keep new images lazy; at a recording boundary reset both probe histories, reflection histories and NR/SR/FG scene histories on mode changes/cuts/resize. Disable/re-enable must never sample stale images. Preserve G03 as fallback. Only after a measured probes-only result consider glossy GI, then wet-ground response; explicitly arbitrate G04 to prevent duplicate reflection energy.

Decision: **NO-GO for implementation now; feasibility milestone completed.** No probe, glossy-GI or wet-ground performance/visual claim. This batch's test build exercises the existing GI plus T2-02–04, not a probe prototype.
