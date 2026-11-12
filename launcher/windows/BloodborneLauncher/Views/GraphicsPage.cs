using System.IO;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>Upscaling and the game's own effects (patches applied at start).</summary>
    public sealed class GraphicsPage : LauncherPage
    {
        private readonly TextBlock _upscalerHint, _dlssHint, _frameGenHint;
        private TextBlock? _motionBlurHint;
        private readonly SettingRow _presetCard, _renderScaleCard, _sharpenCard, _sharpnessCard, _mipBiasCard;
        private readonly SettingRow _frameGenFramesCard, _reflexCard, _frameGenCapCard;

        public GraphicsPage() : base(L.T("Graphics"),
            L.T("Stored in lantern.ini; the upscaler can also be changed in game (Insert or L3 + R3)."))
        {
            Add(Card(SymbolRegular.Gauge24, L.T("Best settings for this PC"), Hardware.Detect().Summary,
                SmallButton(L.T("Recommend…"), SymbolRegular.Sparkle24, (_, _) => MainWindow.Instance!.OfferBestSettings(firstRun: false)), out _));

            Group(L.T("Upscaler"));
            // FSR 4.1.1 runs through fsr4vk (in the package) or an fsr4_411 set built from the player's
            // own AMD DLL: offered when either is there. Without both the setting becomes FSR 4, the
            // model it would fall back to (#22).
            var fsr411 = Fsr4VkFound() || Directory.Exists(Fsr411Dir());
            if (!fsr411 && Ini.Get("upscaler") == "fsr411") Ini.Set("upscaler", "fsr4");
            IniCombo(SymbolRegular.Sparkle24, L.T("Upscaler"), null,
                fsr411 ? Choices.Upscalers : Choices.Upscalers.Where(c => c.Value != "fsr411").ToArray(), "upscaler", changed: UpdateUpscaler);
            _upscalerHint = (TextBlock)((SettingRow)Body.Children[^1]).Tag;

            IniCombo(SymbolRegular.ZoomIn24, L.T("Quality preset"), L.T("How far below the output resolution the game renders"),
                Choices.Presets, "preset", changed: UpdateUpscaler);
            _presetCard = (SettingRow)Body.Children[^1];
            // The Custom preset's ratio (render_scale), the engine's PresetScale: 1.0 renders at the
            // output (DLAA with DLSS), 3.0 is Ultra Performance.
            SliderRow(SymbolRegular.SlideSize24, L.T("Render scale (Custom preset)"),
                L.T("Output size divided by this is the render size: 1.00 renders at the output, 1.30 a little below, 3.00 like Ultra Performance. FSR 4 and DLSS use the nearest preset's model"),
                1.0, 3.0, 0.05, () => ParseDouble(Ini.Get("render_scale", "1.30"), 1.3), v => Ini.Set("render_scale", Fmt(v)), format: "x0.00", valueWidth: 56, defaultValue: 1.3);
            _renderScaleCard = (SettingRow)Body.Children[^1];

            IniToggle(SymbolRegular.Eyedropper24, L.T("Sharpening (RCAS)"), L.T("Works with every option above, TAA included: turn it up if the picture looks soft"), "sharpen");
            _sharpenCard = (SettingRow)Body.Children[^1];
            SliderRow(SymbolRegular.SlideSize24, L.T("Sharpening strength"), L.T("Up to 1: the upscaler's own sharpening. Above 1: an extra pass"), 0, 2, 0.05,
                () => ParseDouble(Ini.Get("sharpness"), 0.5), v => Ini.Set("sharpness", Fmt(v)), defaultValue: 0.5);
            _sharpnessCard = (SettingRow)Body.Children[^1];
            SliderRow(SymbolRegular.Image24, L.T("Texture mip bias"),
                L.T("Added to the texture detail the upscaler asks for: below 0 sharper textures (more shimmer), above 0 softer. 0 is the upscaler's own"),
                -2, 1, 0.05, () => ParseDouble(Ini.Get("mip_bias", "0"), 0), v => Ini.Set("mip_bias", Fmt(v)), format: "+0.00;-0.00;0.00", valueWidth: 56, defaultValue: 0);
            _mipBiasCard = (SettingRow)Body.Children[^1];
            IniToggle(SymbolRegular.ArrowMove24, L.T("Object motion vectors"), L.T("Less ghosting on characters; costs about 10% FPS"), "object_motion");

            var dlssMore = Advanced(L.T("DLSS options"));
            IniCombo(SymbolRegular.BrainCircuit24, L.T("DLSS model"),
                L.T("Which NVIDIA model DLSS runs. Also in game (Insert or L3 + R3), where it changes at once"), Choices.DlssModels, "dlss_preset", dlssMore);
            ToggleRow(SymbolRegular.BrightnessHigh24, L.T("DLSS auto exposure"),
                L.T("DLSS works out each frame's exposure itself, for the game's finished frame too. Can calm ghosting in dark scenes"),
                () => Ini.Get("dlss_auto_exposure", "0") == "1", v => Ini.Set("dlss_auto_exposure", v ? "1" : "0"), dlssMore);
            ToggleRow(SymbolRegular.Image24, L.T("DLSS sharper textures"),
                L.T("One more mip level of texture detail under DLSS, as NVIDIA's guide asks. Off: the render-to-output ratio only"),
                () => Ini.Get("dlss_mip_bias", "1") != "0", v => Ini.Set("dlss_mip_bias", v ? "1" : "0"), dlssMore);
            Add(Card(SymbolRegular.DeveloperBoard24, "DLSS", "", null, out _dlssHint), dlssMore);

            Group(L.T("Frame generation"));
            IniCombo(SymbolRegular.FastForward24, L.T("Frame generation"),
                L.T("Extra frames between the game's own: smoother motion, a little more input lag. Needs an upscaler. Off by default"),
                Choices.FrameGeneration, "frame_gen", changed: UpdateUpscaler);
            _frameGenHint = (TextBlock)((SettingRow)Body.Children[^1]).Tag;
            IniCombo(SymbolRegular.Layer24, L.T("DLSS generated frames"),
                L.T("Frames shown per frame the game renders. More than 2x needs an RTX 50 GPU; the engine keeps to what the GPU allows"),
                Choices.FrameGenFrames, "frame_gen_frames");
            _frameGenFramesCard = (SettingRow)Body.Children[^1];
            IniCombo(SymbolRegular.Flash24, L.T("NVIDIA Reflex"),
                L.T("Keeps the GPU's queue short for lower input lag. Always on while DLSS Frame Generation runs"),
                Choices.Reflex, "reflex");
            _reflexCard = (SettingRow)Body.Children[^1];
            IniToggle(SymbolRegular.Timer24, L.T("Frame generation: even pacing (Uncapped)"),
                L.T("With the Uncapped frame rate: the game at the refresh rate divided by the frames shown per game frame, so every generated frame reaches the screen. The fixed rates (30 to 144) are left as they are"), "frame_gen_cap");
            _frameGenCapCard = (SettingRow)Body.Children[^1];

            // The engine's picture and camera controls (bbport_settings.cpp clamps the same ranges).
            Group(L.T("Ambient occlusion"));
            ToggleRow(SymbolRegular.DarkTheme24, L.T("GTAO (restart required)"),
                L.T("Replaces the game's SSAO. Keep Ambient occlusion (SSAO) enabled. Uses the existing AO strength with Picture tuning on. Unsupported shader layouts retain SSAO."),
                () => Ini.Get("graphics_gtao", "0") == "1", v => Ini.Set("graphics_gtao", v ? "1" : "0"));

            IniCombo(SymbolRegular.DarkTheme24, L.T("GTAO quality (restart required)"),
                L.T("Requires GTAO. HQ adds spatial denoising and full internal-resolution AO."),
                new[] { new Choice(L.T("Regular GTAO"), "0"), new Choice(L.T("High-quality GTAO"), "1") }, "graphics_ao_quality");
            Group(L.T("Screen-space lighting"));
            ToggleRow(SymbolRegular.Lightbulb24, L.T("SSGI (live in game)"), L.T("Screen-space indirect lighting. Experimental; adds GPU cost. Toggle live in the in-game overlay; launcher changes apply on the next launch."),
                () => Ini.Get("graphics_ssgi", "0") == "1", v => Ini.Set("graphics_ssgi", v ? "1" : "0"));
            IniSlider(SymbolRegular.Lightbulb24, L.T("Indirect lighting strength"), 0, 12, "graphics_gi_strength", fallback: 6);
            ToggleRow(SymbolRegular.Image24, L.T("Stochastic SSR (restart required)"), L.T("Keep the original screen-space reflections switch enabled. Unsupported shader layouts retain original reflections."),
                () => Ini.Get("graphics_modern_ssr", "0") == "1", v => Ini.Set("graphics_modern_ssr", v ? "1" : "0"));
            IniSlider(SymbolRegular.Image24, L.T("Reflection intensity"), 0, 3, "graphics_ssr_intensity");
            ToggleRow(SymbolRegular.WeatherSunny24, L.T("PCSS sun shadows (restart required)"), L.T("Distance-dependent soft directional shadows; no point-light shadows or contact shadows are enabled."),
                () => Ini.Get("graphics_pcss", "0") == "1", v => Ini.Set("graphics_pcss", v ? "1" : "0"));

            Group(L.T("Shadows and volumetric lighting"));
            ToggleRow(SymbolRegular.WeatherSunny24,L.T("Point-light shadows (live in game)"),L.T("Screen-space shadows on supported local lights. Off-screen occluders retain native lighting; additional GPU cost. Toggle live in the in-game overlay; launcher changes apply on the next launch."),
                () => Ini.Get("graphics_point_shadows","0") == "1", v => Ini.Set("graphics_point_shadows",v ? "1" : "0"));
            IniSlider(SymbolRegular.WeatherSunny24,L.T("Point shadow strength"),0,1,"graphics_point_shadow_strength",fallback: 0.85);
            ToggleRow(SymbolRegular.WeatherSunny24,L.T("Contact shadows (live in game)"),L.T("Screen-space contact shadows; requires valid scene depth. Toggle live in the in-game overlay; launcher changes apply on the next launch."),
                () => Ini.Get("graphics_contact_shadows","0") == "1", v => Ini.Set("graphics_contact_shadows",v ? "1" : "0"));
            ToggleRow(SymbolRegular.WeatherSunny24,L.T("Volumetric sunlight (live in game)"),L.T("Directional light scattering; additional GPU cost. Toggle live in the in-game overlay; launcher changes apply on the next launch."),
                () => Ini.Get("graphics_volumetric_light","0") == "1", v => Ini.Set("graphics_volumetric_light",v ? "1" : "0"));
            ToggleRow(SymbolRegular.WeatherSunny24,L.T("Point-light volumetric fog (live in game)"),L.T("Fog illuminated by local lights; additional GPU cost. Toggle live in the in-game overlay; launcher changes apply on the next launch."),
                () => Ini.Get("graphics_point_fog","0") == "1", v => Ini.Set("graphics_point_fog",v ? "1" : "0"));
            ToggleRow(SymbolRegular.WeatherSunny24,L.T("Directional filtering and cascade blend (restart required)"),L.T("Atlas-safe directional filtering and guarded 10% cascade overlap."),
                () => Ini.Get("graphics_directional_filter","0") == "1", v => Ini.Set("graphics_directional_filter",v ? "1" : "0"));
            IniSlider(SymbolRegular.WeatherSunny24,L.T("Contact shadow strength"),0,1,"graphics_contact_strength",fallback: 0.75);
            IniSlider(SymbolRegular.WeatherSunny24,L.T("Sunlight volume intensity"),0,3,"graphics_volumetric_intensity",fallback: 1);
            IniSlider(SymbolRegular.WeatherSunny24,L.T("Point fog intensity"),0,3,"graphics_point_fog_intensity",fallback: 1);
            IniCombo(SymbolRegular.Image24,L.T("Directional shadow resolution (restart required)"),L.T("Higher resolutions use more VRAM. Original preserves the game settings."),
                new[] { new Choice(L.T("Original"),"0"), new Choice(L.T("High (4096 atlas)"),"1"), new Choice(L.T("Ultra (8192 atlas)"),"2") },"graphics_shadow_resolution");
            IniCombo(SymbolRegular.Image24,L.T("Local shadow resolution (restart required)"),L.T("Higher resolutions use more VRAM. Original preserves the game settings."),
                new[] { new Choice(L.T("Original"),"0"), new Choice(L.T("High (point 1024 / spot 2048)"),"1"), new Choice(L.T("Ultra (point 2048 / spot 4096)"),"2") },"graphics_local_shadow_resolution");

            Group(L.T("Picture"));
            var picture = new StackPanel();
            IniToggle(SymbolRegular.Color24, L.T("Picture tuning"), L.T("Turns the settings below on, live. Also in the game's System menu (PC Graphics, PC Effects, PC Colour) and the Insert menu"), "graphics_controls");
            IniToggle(SymbolRegular.CircleHalfFill24, L.T("Vignette"), null, "graphics_vignette", picture);
            IniSlider(SymbolRegular.DarkTheme24, L.T("Ambient occlusion strength"), 0, 3, "graphics_ao_strength", picture);
            IniSlider(SymbolRegular.WeatherSunny24, L.T("Shadow distance"), 0.5, 4, "graphics_shadow_scale", picture);
            IniSlider(SymbolRegular.Lightbulb24, L.T("Bloom"), 0, 3, "graphics_bloom", picture);
            IniSlider(SymbolRegular.Color24, L.T("Saturation"), 0, 3, "graphics_saturation", picture);
            IniSlider(SymbolRegular.BrightnessHigh24, L.T("Brightness"), 0, 3, "graphics_brightness", picture);
            IniSlider(SymbolRegular.DarkTheme24, L.T("Contrast"), 0.8, 1.2, "graphics_contrast", picture, step: 0.01);
            IniSlider(SymbolRegular.Color24, L.T("Hue"), -180, 180, "graphics_hue", picture, step: 1, fallback: 0);
            IniSlider(SymbolRegular.WeatherFog24, L.T("Fog"), 0, 3, "graphics_fog", picture);
            Body.Children.Add(picture);

            Group(L.T("Camera"));
            IniToggle(SymbolRegular.Camera24, L.T("Camera tuning"), L.T("Turns the three settings below on"), "camera_controls");
            IniSlider(SymbolRegular.ZoomOut24, L.T("Field of view"), 1, 1.5, "camera_fov_scale");
            IniSlider(SymbolRegular.ArrowMaximize24, L.T("Distance"), 0.5, 1.5, "camera_distance_scale");
            IniSlider(SymbolRegular.ArrowBidirectionalUpDown24, L.T("Height"), 0.5, 1.5, "camera_height_scale");

            Group(L.T("Game effects"));
            Body.Children.Add(Text(L.T("Patches to the game, applied when it starts."), "Faint"));
            IniCombo(SymbolRegular.Cube24, L.T("Model detail"), null, Choices.ModelLod, "model_lod");
            IniCombo(SymbolRegular.Camera24, L.T("Follow camera"), L.T("Original keeps the game's camera distance, angle and follow speed at 60 FPS and above"), Choices.CameraTuning, "camera_tuning");
            foreach (var effect in Choices.Effects)
            {
                var motionBlur = effect.Key == "effect_motion_blur";
                IniToggle(EffectIcon(effect.Key), effect.Title, effect.Hint, effect.Key, changed: motionBlur ? UpdateUpscaler : null);
                if (motionBlur) _motionBlurHint = ((SettingRow)Body.Children[^1]).Hint;
            }

            Ws.PortChanged += UpdateUpscaler;
        }

        private void IniSlider(SymbolRegular icon, string title, double min, double max, string key, Panel? into = null,
            double step = 0.05, double fallback = 1.0) =>
            SliderRow(icon, title, null, min, max, step,
                () => ParseDouble(Ini.Get(key, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)), fallback),
                v => Ini.Set(key, v.ToString(key == "dlss_nr_white_point" ? "0.000" : "0.00", System.Globalization.CultureInfo.InvariantCulture)),
                format: key == "dlss_nr_white_point" ? "0.000" : "0.00", into: into, defaultValue: fallback);

        /// <summary>An RTX 40 or 50 card (desktop or laptop): the GPUs NVIDIA's frame generation runs on.</summary>
        private static bool DlssFrameGenGpu() =>
            Hardware.Detect() is { Rtx: true } hardware &&
            System.Text.RegularExpressions.Regex.IsMatch(hardware.Gpu, @"RTX\s*(PRO\s*)?[45]0\d\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>NVIDIA Streamline's runtime beside the engine (tools/fetch_streamline.sh): DLSS Frame Generation.</summary>
        private static bool StreamlineFound() =>
            File.Exists(Path.Combine(Paths.BinariesDir, "sl.interposer.dll")) && File.Exists(Path.Combine(Paths.BinariesDir, "sl.dlss_g.dll"));

        private static SymbolRegular EffectIcon(string key) => key switch
        {
            "skip_intro" => SymbolRegular.FastForward24,
            "debug_camera" => SymbolRegular.Camera24,
            "debug_menu" => SymbolRegular.Bug24,
            _ => SymbolRegular.Color24,
        };

        public override void OnShown() => UpdateUpscaler();

        private static bool DlssFound() =>
            File.Exists(Path.Combine(Paths.BinariesDir, "nvngx_dlss.dll")); // the engine has DLSS built in; NVIDIA's runtime sits beside it

        private void UpdateUpscaler()
        {
            var value = Ini.Get("upscaler");
            string? hint = value switch
            {
                "fsr4" => Directory.Exists(Path.Combine(Paths.EngineDir, "fsr4_shaders"))
                    ? L.T("Assets found. GPUs without the needed shader features fall back to FSR 3.1")
                    : L.T("fsr4_shaders is missing from the port folder: the game falls back to FSR 3.1"),
                "fsr411" when Fsr4VkFound() =>
                    L.T("Through fsr4vk (included, experimental): runs on any GPU with the needed Vulkan features. Prefer FSR 4 if it looks wrong"),
                "fsr411" => Fsr411Problem() is { } problem
                    ? L.F($"{problem}: the game falls back to FSR 4")
                    : L.T("Assets for the chosen resolution and preset found"),
                "dlss" => DlssFound()
                    ? L.T("Needs an NVIDIA GeForce RTX GPU; others fall back to FSR 3.1") +
                      (Ini.Get("preset", "0") == "0" ? L.T(". With Native AA this is DLAA: DLSS as anti-aliasing at full resolution") : "")
                    : L.T("DLSS files are missing: the game falls back to FSR 3.1"),
                "taa" => L.T("Anti-aliasing at the output resolution, without an FSR model. Softer than DLAA or FSR Native AA: add sharpening below"),
                _ => null,
            };
            SetHint(_upscalerHint, hint);
            _presetCard.IsEnabled = value is not ("taa" or "off");
            _renderScaleCard.Visibility = Ini.Get("preset", "0") == "5" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _renderScaleCard.IsEnabled = value is not ("taa" or "off");
            _sharpenCard.IsEnabled = value != "off";
            _sharpnessCard.IsEnabled = value != "off";
            _mipBiasCard.IsEnabled = value != "off";

            // Frame generation: DLSS's own rows only with DLSS chosen.
            var frameGen = Ini.Get("frame_gen", "off");
            _frameGenFramesCard.IsEnabled = frameGen == "dlss";
            _reflexCard.IsEnabled = frameGen == "dlss";
            _frameGenCapCard.IsEnabled = frameGen != "off";
            SetHint(_frameGenHint, frameGen switch
            {
                "dlss" when !StreamlineFound() => L.T("NVIDIA Streamline (sl.interposer.dll, sl.dlss_g.dll) is missing from the engine folder: the game uses no frame generation"),
                "dlss" when !DlssFrameGenGpu() => L.T("Needs an NVIDIA RTX 40 or 50 GPU with Hardware-accelerated GPU scheduling on; other GPUs: choose FSR 3.1"),
                "dlss" => L.T("RTX 40: 2x; RTX 50: up to 6x. Loaded when the game starts; in game you can switch it off and on (Insert or L3 + R3)"),
                "fsr3" => L.T("Any GPU. Not in menus, movies or with HDR output"),
                _ => null,
            });
            if (frameGen != "off" && value == "off")
                SetHint(_frameGenHint, L.T("Frame generation needs an upscaler (or TAA): choose one above"));

            // An upscaler takes the finished frame at outputs other than 1920x1080 and can smear the
            // game's motion blur; More > Engine can leave the blur out there (BB_UPSCALER_NO_BLUR).
            var (w, h) = Choices.OutputSize(Ini.Get("output_res", "1920x1080"));
            if (_motionBlurHint != null)
                SetHint(_motionBlurHint, value != "off" && (w, h) != (1920, 1080) && Ini.Flag("effect_motion_blur")
                    ? L.T("With an upscaler at an output other than 1920x1080 the blur can smear. More > Engine > No motion blur smear with upscalers leaves it out there")
                    : null);

            var ngx = Path.Combine(Paths.BinariesDir, "nvngx_dlss.dll");
            SetHint(_dlssHint, DlssFound()
                ? L.F($"NVIDIA DLSS {System.Diagnostics.FileVersionInfo.GetVersionInfo(ngx).FileVersion} is included (out\\nvngx_dlss.dll). Choose it as the upscaler above")
                : L.T("out\\nvngx_dlss.dll is missing from the engine folder"));
        }

        /// <summary>The fsr4vk provider the package ships (FSR 4.1.1 on any GPU, vk_fsr4vk.cpp).</summary>
        private static bool Fsr4VkFound() =>
            File.Exists(Path.Combine(Paths.EngineDir, "fsr4vk", "amd_fidelityfx_upscaler_vk.dll"));

        private static string Fsr411Dir()
        {
            var env = Environment.GetEnvironmentVariable("BB_FSR411_DIR");
            if (!string.IsNullOrEmpty(env)) return env;
            var local = Path.Combine(Paths.EngineDir, "fsr4_411");
            return Directory.Exists(local) ? local : Path.Combine(Paths.DataDir, "fsr4_411");
        }

        /// <summary>The FSR 4.1.1 model files the chosen output and preset need (fsr4_411\).</summary>
        private static string? Fsr411Problem()
        {
            var (w, h) = Choices.OutputSize(Ini.Get("output_res", "1920x1080"));
            var model = (w > 1920 || h > 1080 ? "t2160" : "t1080") + (Ini.Get("preset", "4") == "4" ? "_m1" : "_m0");
            var passes = new List<string> { "spd", "prepass", "pass0_post" };
            for (var i = 1; i <= 12; i++)
            {
                passes.Add($"pass{i}");
                passes.Add($"pass{i}_post");
            }
            passes.Add("postpass");
            passes.Add("rcas");

            var dir = Path.Combine(Fsr411Dir(), model);
            foreach (var name in passes)
            {
                var file = new FileInfo(Path.Combine(dir, name + ".spv"));
                if (!file.Exists || file.Length < 20 || file.Length % 4 != 0)
                    return L.F($"Missing or damaged fsr4_411\\{model}\\{name}.spv");
            }
            var init = new FileInfo(Path.Combine(dir, "initializer.bin"));
            return init.Exists && init.Length == 131072 ? null : L.F($"Missing or damaged fsr4_411\\{model}\\initializer.bin");
        }
    }
}
