# rc7 Picture controls follow-up

Owner request: reorganize related controls, explain technical names, provide an advanced-effects master/defaults, investigate bloom and local-light sliders, and replace the existing `releases/1.0.4-nr.rc7/BloodbornePC` binaries in place. No new release version/folder or archives are requested. Work remains on `main`, HEAD `e0f47880f1c88a31b7288ce852d9ce4e88dc90e9`, preserving the existing uncommitted Tier 2 work.

## Controls

Both launchers and the in-game Picture page group depth of field, motion blur, bloom/exposure, ambient/indirect light, reflections, material highlights/local lamps, shadows, and fog/sky. Their existing styling and layout components are reused.

- DoF master reveals Original/Modern style; Modern reveals blur strength. The native switch and style retain their truthful restart labels.
- Motion-blur quality follows its native master.
- Froyok mode reveals bloom amount; nonzero amount reveals glow spread. Original bloom has its own native intensity. Zero Froyok amount intentionally produces no Froyok glow.
- Alternative tone exposure appears only for GT7/ACES. The existing HDR scene curve retains precedence.
- AO quality follows GTAO/native AO, GI strength follows indirect lighting, reflection strength follows improved reflections, and shadow/fog/sky children follow their relevant switches.
- Natural/inverse-square local-light falloff reveals lamp brightness and source size. Highlight sharpness appears with compatible local-light shading. These three coefficients are now **live**.
- SSGI is named **Indirect lighting**, SSS **Skin light scattering**, POM **Surface depth and cavity shading**, PCSS **Soft sun shadows**, with explanations and relevant limitations. Runtime exposure/skin/bloom state uses readable descriptions.

**Advanced lighting and effects** replaces the Tier 2 heading. Its live bypass retains saved selections while bypassing the new material/postprocessing enhancements, including Modern DoF when initialized. Native game switches and previous Tier 1 AO/GI/shadow controls remain independent. The new key `graphics_advanced_enabled` defaults to true for backward compatibility with the owner's existing selected effects; individual opt-ins retain their existing defaults.

Every new option has a Default action in game. Launcher toggles/choices offer Restore default in their context menu, sliders retain their existing default action, and both interfaces have Restore advanced defaults. The group reset preserves native effects, Tier 1 choices, NR and native Picture tuning. Preset-reference rendering cannot invoke runtime reset/recalibration actions or mutate live settings.

## Bloom correction

Bloom generation previously depended on a specific native DoF-recombine shader. Read-only inspection of the owner's shader cache found the final `0x5c02eaf8` composite and its scene input at texture 0, but not the expected `0x9e1cb278` DoF recombine. Thus bloom could silently retain the native path despite changing its sliders.

Bloom now generates at the verified final composite from its actual scene input, after any DoF/postprocessing that supplies that input. It no longer needs the missing producer pass or Modern DoF mode. The native display boundary retires the result **after** binding/recording the consumer draw, instead of discarding it in the pre-bind settings latch. Known linear HDR source formats are supported; unsupported resources retain native bloom and report Unavailable. Disabled mode allocates no new working images. Amount/spread remain live and original bloom operands remain intact in fallback.

## Local-light correction

The old sliders changed saved configuration while shaders read only their startup snapshots. The guarded falloff/highlight rewrite now replaces only its own injected constants with loads from an additional 16-byte push block at offset 128 on the known lamp shaders. Original game push members and descriptor bindings remain unchanged. Coefficients update per draw without shader recompilation or cache churn. Unknown layouts retain original coefficients. Intensity/source size apply to inverse-square lights; sharpness controls compatible gloss lobes, not shadow softness or sun brightness.

The original prepared-pipeline fast path is available when advanced shader variants are inactive. Enhanced shader combinations remain immutable and recording-owner selected. NR code, model/forwarder bytes, immutable RTX profiles and updater logic are unchanged.

## Validation

PASS: native engine, WPF and Avalonia builds; settings persistence and advanced reset; preset selection/library/UI isolation; native exposure and skin-scattering fixtures, including master-off restoration; bloom producer/consumer lifecycle; existing bloom/tone IR fallback tests. The new GPU test changes intensity, source size and sharpness, then restores baseline on **one unchanged Vulkan pipeline** on the RTX 3070. Its rewritten SPIR-V validates. Fourteen cached native lamp shaders (78 standard/3 special falloff sites) also convert and validate with the engine's `uniformBufferStandardLayout` profile. Cache inputs are read-only; game-derived test outputs stay in the ignored workspace.

Launcher updater, NR-runtime/optimization and localization checks pass. Existing NR shader/style regression checks are retained. The guarded 38-file source supplement replays from its preimage. See the in-place update record for deployed binary hashes and preservation checks.

NOT TESTED: actual gameplay/visual acceptance of this follow-up, every material/map, live controller/mouse appearance, HDR display, extended sessions or FPS/VRAM comparisons. An offline shader IR text dump stopped at an existing debug type assertion; its partial resource trace and cache presence informed the bloom diagnosis, but it is not reported as a successful full capture replay.

## Delivery and acceptance

Update only the existing rc7 portable binaries and compatible cache identity files, backing up replaced build files in the ignored workspace. Preserve `lantern.ini`, presets, game/save data, logs and the user's NR model. No new version, release directory, commit, push or publication. Existing Windows/source ZIPs remain the earlier rc7 snapshot; use the updated portable folder, not those archives, for this retest.

Run `C:\Projects\Bloodborne Engine\BloodborneX\releases\1.0.4-nr.rc7\BloodbornePC\BloodbornePC.exe`. Check parent/child visibility and defaults, bypass/re-enable advanced effects, Froyok amount 0 → 0.3 → 0 (then spread 0 → 1 with amount above zero), and lamp brightness/source size while standing near a lamp. Keep the camera fixed for comparisons. Existing saved slider values are preserved, including a zero bloom amount.

The requested environment-bounce/reflection idea is discussion only. No probe GI, RT, new reflection mode or unrelated lighting feature is implemented in this follow-up.

## Compact strength controls follow-up

The next owner request audits Picture/effect toggles for useful live strength controls. The in-game page and both launchers now use two columns. Toggle/strength pairs occupy one cell: checkbox, name, strength. Unchecked parents disable their strength without deleting its saved value. Dependent technique selectors remain available as disabled controls when their prerequisites are off. Long labels are clipped within their cell and retain descriptive tooltips. Restart-dependent native switches retain an asterisk and an explicit restart legend.

Effect strengths display **1–100**, mapping linearly to their existing native ranges. This is a UI scale, not a percentage shared by all effects: e.g. AO maps to 0–3, GI to 0–12, and local-light brightness to 0.1–4. Existing INI keys/values and preset format remain compatible, and opening the page does not quantize or rewrite them. A drag uses 0.1 displayed units per pixel. Keyboard arrows adjust the field; in game Ctrl-click enters a number, and launchers have an adjacent editable number box (Enter or focus loss commits; Up/Down adjusts). Values clamp to 1–100. Right-click restores defaults. Basic colour tuning and unrelated NR/upscaler/camera controls retain their established units.

| Toggle or family | Strength audit result |
| --- | --- |
| Modern DoF | Existing blur intensity paired with the native DoF switch; editable only with Modern mode and its required parent enabled. Original native DoF has no verified independent scalar. |
| Improved motion blur | **New live blur strength** (`graphics_motion_blur_strength`, native 0–1). Blends unblurred and blurred HDR colour with the original gain/alpha behavior; full strength retains the previous shader path. Native motion-blur startup and upscaler suppression requirements remain. |
| Automatic exposure | **New live adaptation strength** (`graphics_exposure_strength`, 0–1). Scales the applied EV correction around authored neutral exposure, retaining metering/calibration and the original adaptation limits. |
| Sky smoothing | **New live smoothing strength** (`graphics_sky_smoothing_strength`, 0–1). Changes only the verified panorama filter blend and invalidates its cached result when adjusted. Seam correction remains a prerequisite. |
| Soft sun shadows / PCSS | **New live softness** (`graphics_pcss_softness`, 0–2 times the existing source angle). Existing atlas/bias/penumbra bounds remain. PCSS initialization still requires its labeled restart. |
| Sun-shadow transition filtering | **New live cascade-transition blend** (`graphics_shadow_filter_strength`, 0–1 times the verified overlap). This changes the cascade blend width, not shadow darkness. Requires initialized directional filtering. |
| AO / GTAO | Existing AO strength shared by native and improved AO; quality is discrete. |
| Indirect lighting / improved reflections | Existing GI/reflection strengths paired with their respective switches. Native reflections alone have no verified independent intensity hook. |
| Natural local-light falloff | Existing live brightness paired with toggle; source size and highlight sharpness remain separately adjustable. |
| Contact shadows / point-light shadows | Existing live strength paired with each switch. Native local-shadow master controls game-authored shadows, a separate path. |
| Sun shafts / lamp fog | Existing live intensity paired with each switch. Native distance fog remains separately adjustable. |
| Bloom / tone mapping | Existing amount, spread, original bloom intensity and tone exposure use the compact 1–100 scale. Technique selectors remain discrete. |
| GGX models, environment highlight tails, specular AA, POM, reconstructed-normal smoothing | Remain technique/quality toggles in this revision. Current guarded rewrites replace complete expressions or normal reconstruction; they have no verified live scalar binding. Continuous blending is technically possible, but requires additional shader ABI/control-flow work and visual validation. No decorative or recompiling-on-drag slider is substituted. |
| Native skin scattering | Remains a toggle. The verified native hook owns resource/enable flags; material-profile blur strength is not verified. Guessing a native offset is unsuitable for a live control. |
| Sky seam / shadow resolutions / model detail | Correctness fix or discrete geometry/resource choice, without a meaningful effect intensity. |
| Picture tuning / advanced master / vignette / other native game switches | Masters or binary native/debug functionality. No new unverified native parameters are written. |

All five new controls default to the previous behavior. Advanced reset includes the new exposure, motion-blur and sky strengths; it preserves Tier 1 PCSS/filter strengths, which have their own individual defaults. New keys participate in save/load, snapshots and validated custom presets.

### Launcher startup repair

The initial in-place strength update compiled but failed launcher startup: the ad-hoc single-file publish omitted `IncludeNativeLibrariesForSelfExtract`, leaving WPF native DLLs beside the staged executable while only the executable was deployed. Reproduced `System.DllNotFoundException` during WPF window creation. The WPF project now enables native-library embedding by default, matching the normal release helper. The corrected self-contained executable was deployed to the same rc7 folder. Its existing off-screen startup/prompt screenshot check now renders successfully and exits 0; the public `BloodbornePC.exe` entry is checked through that same path. No game is started, and the test redirects the game INI to an owned temporary fixture. Portable configuration preservation remains checked. This is launcher startup validation, not gameplay acceptance.

Validation: native engine and WPF/Avalonia compile; settings round-trip/reset and preset UI checks; native exposure hook exercised at zero/half/full strength; RTX 3070 GPU checks for motion blur zero/half/full/restoration and unchanged zero-motion behavior; panorama minimum/full filtering and refusal/cache cases; existing directional-shadow GPU suite. Existing local-light, bloom lifecycle, NR shader, launcher runtime/optimization, localization and updater regressions are also checked. No gameplay, controller certification, HDR-display or performance acceptance is claimed. The existing rc7 portable folder is updated in place, with protected-file hashes checked again; no archives or release folders are created.

## Picture style and preset overwrite revision

This supersedes the earlier compact/heavy-drag presentation. Launcher effect cells retain two columns; within each cell the label and strength area divide available space equally. Labels wrap rather than ellipsize, and decimal fields are 84 pixels wide (including `100.0`). Pointer handling uses the platform's normal slider behavior; the custom 0.1-unit/pixel drag handlers are removed. Arrow adjustment, direct numeric input, clamping to 1–100 and default actions remain.

In game, Picture effects return to the normal single-column ImGui layout used by Neural Rendering: reset button, real slider on the left, label on the right. Strengths retain their normalized 1–100 scale and ordinary dragging, with a visible 20-pixel scaled grab handle. Related effect grouping, conditional options, runtime status and exposure recalibration are preserved. The tab names are **Neural Rendering** and **Presets**; stable tab IDs and settings keys are unchanged. Both launcher layouts also spell out Neural Rendering.

Save current now offers a **Save to** selector: create a new custom preset or replace an existing custom library entry. Each saved custom entry also has an **Overwrite** button, opening the same current-settings editor with its existing key inclusion set and name. Confirmation freezes the selected snapshot and target; Cancel leaves the file unchanged. An overwrite preserves the library path, uses existing validated atomic writes, and refreshes the list without creating a duplicate or applying settings to the game. The two provided RTX 3070 profiles remain immutable and are absent from the overwrite-target list. The backend additionally rejects missing targets, external exports, invalid documents, symlinks and non-library paths.

Validation includes engine and both launcher builds; native settings/preset tests extended for owned atomic overwrite, invalid-input preservation, target restrictions and unchanged live values; actual headless UI clicks through per-entry overwrite, cancel/confirm, Save-current target selection, retained key scope and stable preset count. Existing preset selection/default/group/header checks still pass. Delivery remains the existing rc7 portable folder; configuration, user presets and model files are hash-checked before/after deployment. No game is started for these checks and no gameplay appearance or controller acceptance is claimed.
