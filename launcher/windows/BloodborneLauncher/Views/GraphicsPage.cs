using System.Windows;
using System.Collections.Generic;
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

            Group(L.T("Picture"));
            var picture = new StackPanel();
            IniToggle(SymbolRegular.Color24, L.T("Picture tuning"), L.T("Turns the settings below on, live. Also in the game's System menu (PC Graphics, PC Effects, PC Colour) and the Insert menu"), "graphics_controls");
            IniToggle(SymbolRegular.CircleHalfFill24, L.T("Vignette"), null, "graphics_vignette", picture);
            IniSlider(SymbolRegular.Color24, L.T("Saturation"), 0, 3, "graphics_saturation", picture);
            IniSlider(SymbolRegular.BrightnessHigh24, L.T("Brightness"), 0, 3, "graphics_brightness", picture);
            IniSlider(SymbolRegular.DarkTheme24, L.T("Contrast"), 0.8, 1.2, "graphics_contrast", picture, step: 0.01);
            IniSlider(SymbolRegular.Color24, L.T("Hue"), -180, 180, "graphics_hue", picture, step: 1, fallback: 0);
            Body.Children.Add(picture);

            BuildPictureEffects();

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
                if (new[] { "effect_dof", "effect_motion_blur", "effect_ssao", "effect_ssr", "effect_dynamic_shadows" }.Contains(effect.Key)) continue;
                var motionBlur = effect.Key == "effect_motion_blur";
                IniToggle(EffectIcon(effect.Key), effect.Title, effect.Hint, effect.Key, changed: motionBlur ? UpdateUpscaler : null);
                if (motionBlur) _motionBlurHint = ((SettingRow)Body.Children[^1]).Hint;
            }

            Ws.PortChanged += UpdateUpscaler;
        }

        private readonly List<(Panel Panel, Func<bool> Show)> _graphicsVisibility = new();
        private readonly List<Action> _graphicsReload = new();
        private readonly Dictionary<string,string> _advancedGraphicsDefaults = new();
        private bool AdvancedGraphicsEnabled() => Ini.Get("graphics_advanced_enabled","1")!="0";
        private bool GraphicsFlag(string key,bool fallback) => Ini.Get(key,fallback?"1":"0")!="0";
        private double GraphicsNumber(string key,double fallback) => ParseDouble(Ini.Get(key,fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)),fallback);
        private void UpdateGraphicsVisibility() {
            foreach(var entry in _graphicsVisibility) entry.Panel.IsEnabled=entry.Show();
        }
        private void GraphicsDefaultMenu(FrameworkElement control,Action restore) {
            var menu=new ContextMenu(); var item=new System.Windows.Controls.MenuItem { Header=L.T("Restore default") };
            item.Click+=(_,_)=>{ restore(); Ws.SaveSoon(); UpdateGraphicsVisibility(); };
            menu.Items.Add(item); control.ContextMenu=menu;
        }
        private void BuildPictureEffects() {
            Group(L.T("Advanced lighting and effects"));
            var master=ToggleRow(SymbolRegular.Image24,L.T("Advanced lighting and effects"),L.T("Live master; retains your individual selections."),AdvancedGraphicsEnabled,v=>Ini.Set("graphics_advanced_enabled",v?"1":"0"),changed:UpdateGraphicsVisibility);
            _graphicsReload.Add(()=>master.IsChecked=AdvancedGraphicsEnabled());
            GraphicsDefaultMenu(master,()=>{ Ini.Set("graphics_advanced_enabled","1");master.IsChecked=true; });
            Add(Card(SymbolRegular.Image24,L.T("Restore advanced defaults"),L.T("Preserves native switches, Tier 1, NR and picture tuning."),SmallButton(L.T("Restore defaults"),SymbolRegular.ArrowReset24,(_,_)=> {
                Ini.Set("graphics_advanced_enabled","1");foreach(var item in _advancedGraphicsDefaults) Ini.Set(item.Key,item.Value);
                foreach(var reload in _graphicsReload) reload();Ws.SaveSoon();UpdateGraphicsVisibility();
            }),out _));
            Body.Children.Add(Text(L.T("Strength: 1–100. Drag or click; arrows adjust; type a number. * Restart required. Launcher changes apply next launch."),"Faint"));
            Group(L.T("Depth of field"));
            var grid0=new Grid();
            grid0.ColumnDefinitions.Add(new ColumnDefinition());grid0.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid0);
            grid0.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid0.Children.Add(cell);
                CompactEffect(cell,"effect_dof","Depth of field *","Blurs areas outside the camera focus. This native master switch is applied when the game starts.",false,()=>true,"graphics_dof_intensity","Focus blur strength",0.0,3.0,1.0,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("effect_dof",false) && GraphicsNumber("graphics_dof_mode",0.0) == 1));
                _advancedGraphicsDefaults["graphics_dof_intensity"]="1";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid0.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("effect_dof",false))));
                var choices=new[] {new Choice(L.T("Original"),"0"),new Choice(L.T("Modern bokeh"),"1")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Depth of field style *"),null,choices,()=>Ini.Get("graphics_dof_mode","0"),v=>Ini.Set("graphics_dof_mode",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_dof_mode","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_dof_mode","0");Reload(); });
                _advancedGraphicsDefaults["graphics_dof_mode"]="0";
            }
            Group(L.T("Motion blur"));
            var grid1=new Grid();
            grid1.ColumnDefinitions.Add(new ColumnDefinition());grid1.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid1);
            grid1.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid1.Children.Add(cell);
                CompactEffect(cell,"effect_motion_blur","Motion blur *","Native motion blur master. The existing upscaler blur-suppression policy still takes precedence.",false,()=>true,null,null,0,1,1,()=>true);
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid1.Children.Add(cell);
                CompactEffect(cell,"graphics_motion_blur_quality","Improved motion blur","More accurate long motion trails, with depth and velocity rejection.",false,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("effect_motion_blur",false)),"graphics_motion_blur_strength","Motion blur strength",0.0,1.0,1.0,()=>AdvancedGraphicsEnabled() && ((GraphicsFlag("effect_motion_blur",false)) && GraphicsFlag("graphics_motion_blur_quality",false)));
                _advancedGraphicsDefaults["graphics_motion_blur_quality"]="false";
                _advancedGraphicsDefaults["graphics_motion_blur_strength"]="1";
            }
            Group(L.T("Bloom and exposure"));
            var grid2=new Grid();
            grid2.ColumnDefinitions.Add(new ColumnDefinition());grid2.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid2);
            grid2.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid2.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>AdvancedGraphicsEnabled()));
                var choices=new[] {new Choice(L.T("Original bloom"),"0"),new Choice(L.T("Froyok bloom"),"1")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Bloom style"),null,choices,()=>Ini.Get("graphics_bloom_quality","0"),v=>Ini.Set("graphics_bloom_quality",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_bloom_quality","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_bloom_quality","0");Reload(); });
                _advancedGraphicsDefaults["graphics_bloom_quality"]="0";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid2.Children.Add(cell);
                CompactEffect(cell,null,"Original bloom intensity","Game-authored bloom intensity; enables Picture tuning. Froyok has its own blend control.",true,()=>true,"graphics_bloom","Original bloom intensity",0.0,3.0,1.0,()=>(GraphicsNumber("graphics_bloom_quality",0.0) == 0 || !AdvancedGraphicsEnabled()));
            }
            grid2.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,0);grid2.Children.Add(cell);
                CompactEffect(cell,null,"Froyok bloom amount","Live blend between the sharp scene and Froyok glow. Zero means no Froyok glow; one gives the fully blurred result.",true,()=>true,"graphics_froyok_blend","Froyok bloom amount",0.0,1.0,0.04,()=>AdvancedGraphicsEnabled() && (GraphicsNumber("graphics_bloom_quality",0.0) == 1));
                _advancedGraphicsDefaults["graphics_froyok_blend"]="0.04";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,1);grid2.Children.Add(cell);
                CompactEffect(cell,null,"Froyok glow spread","Live balance of small and large glow scales. Higher values spread bright light farther.",true,()=>true,"graphics_froyok_spread","Froyok glow spread",0.0,1.0,0.85,()=>AdvancedGraphicsEnabled() && (GraphicsNumber("graphics_bloom_quality",0.0) == 1));
                _advancedGraphicsDefaults["graphics_froyok_spread"]="0.85";
            }
            grid2.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,0);grid2.Children.Add(cell);
                CompactEffect(cell,"graphics_auto_exposure","Automatic exposure","Adapts around the area-authored exposure, bounded to -1 / +0.5 EV. Different from the DLSS model auto-exposure option.",false,()=>AdvancedGraphicsEnabled(),"graphics_exposure_strength","Exposure adaptation strength",0.0,1.0,1.0,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("graphics_auto_exposure",false)));
                _advancedGraphicsDefaults["graphics_auto_exposure"]="false";
                _advancedGraphicsDefaults["graphics_exposure_strength"]="1";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,1);grid2.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>AdvancedGraphicsEnabled()));
                var choices=new[] {new Choice(L.T("Original"),"0"),new Choice(L.T("GT7"),"1"),new Choice(L.T("ACES"),"2")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Tone mapping"),null,choices,()=>Ini.Get("graphics_tonemapper","0"),v=>Ini.Set("graphics_tonemapper",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_tonemapper","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_tonemapper","0");Reload(); });
                _advancedGraphicsDefaults["graphics_tonemapper"]="0";
            }
            grid2.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,3);Grid.SetColumn(cell,0);grid2.Children.Add(cell);
                CompactEffect(cell,null,"Tone mapping exposure","Live brightness before the selected alternative tone curve; Original ignores this control.",true,()=>true,"graphics_tonemap_exposure","Tone mapping exposure",0.25,4.0,1.0,()=>AdvancedGraphicsEnabled() && (GraphicsNumber("graphics_tonemapper",0.0) != 0));
                _advancedGraphicsDefaults["graphics_tonemap_exposure"]="1";
            }
            Group(L.T("Ambient shading and indirect light"));
            var grid3=new Grid();
            grid3.ColumnDefinitions.Add(new ColumnDefinition());grid3.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid3);
            grid3.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid3.Children.Add(cell);
                CompactEffect(cell,"effect_ssao","Ambient shading *","Ambient occlusion (SSAO) darkens creases and contact areas; it does not add bounced light.",true,()=>true,"graphics_ao_strength","Ambient shading strength",0.0,3.0,1.0,()=>(GraphicsFlag("effect_ssao",true)));
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid3.Children.Add(cell);
                CompactEffect(cell,"graphics_gtao","Improved ambient shading: GTAO *","Ground-truth ambient occlusion replaces native SSAO on compatible passes.",false,()=>(GraphicsFlag("effect_ssao",true)),null,null,0,1,1,()=>true);
            }
            grid3.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,0);grid3.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>(GraphicsFlag("effect_ssao",true) && GraphicsFlag("graphics_gtao",false))));
                var choices=new[] {new Choice(L.T("Regular GTAO"),"0"),new Choice(L.T("High quality GTAO"),"1")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Ambient shading quality *"),null,choices,()=>Ini.Get("graphics_ao_quality","0"),v=>Ini.Set("graphics_ao_quality",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_ao_quality","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_ao_quality","0");Reload(); });
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,1);grid3.Children.Add(cell);
                CompactEffect(cell,"graphics_ssgi","Indirect lighting","Screen-space global illumination estimates light bounced from visible surfaces. Off-screen objects cannot contribute; adds GPU cost.",false,()=>true,"graphics_gi_strength","Bounced-light strength",0.0,12.0,6.0,()=>(GraphicsFlag("graphics_ssgi",false)));
            }
            grid3.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,0);grid3.Children.Add(cell);
                CompactEffect(cell,"graphics_smooth_normals","Smoother indirect shading","Averages compatible surface normals for AO/GI to reduce shimmer; keeps depth edges separate.",false,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("effect_ssao",true) || GraphicsFlag("graphics_ssgi",false)),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_smooth_normals"]="false";
            }
            Group(L.T("Reflections"));
            var grid4=new Grid();
            grid4.ColumnDefinitions.Add(new ColumnDefinition());grid4.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid4);
            grid4.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid4.Children.Add(cell);
                CompactEffect(cell,"effect_ssr","Screen-space reflections *","Reflects visible on-screen geometry. Geometry outside the screen cannot appear in these reflections.",false,()=>true,null,null,0,1,1,()=>true);
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid4.Children.Add(cell);
                CompactEffect(cell,"graphics_modern_ssr","Improved reflections *","Stochastic screen-space reflections with temporal reconstruction; requires the native reflections master above.",false,()=>(GraphicsFlag("effect_ssr",false)),"graphics_ssr_intensity","Reflection strength",0.0,3.0,1.0,()=>(GraphicsFlag("effect_ssr",false) && GraphicsFlag("graphics_modern_ssr",false)));
            }
            grid4.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,0);grid4.Children.Add(cell);
                CompactEffect(cell,"graphics_env_tails","Broader environment highlights","Adds a softer GGX tail to existing environment-map highlights. Does not capture nearby characters or new reflection geometry.",false,()=>AdvancedGraphicsEnabled(),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_env_tails"]="false";
            }
            Group(L.T("Material highlights and local lights"));
            var grid5=new Grid();
            grid5.ColumnDefinitions.Add(new ColumnDefinition());grid5.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid5);
            grid5.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid5.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>AdvancedGraphicsEnabled()));
                var choices=new[] {new Choice(L.T("Original"),"0"),new Choice(L.T("Balanced highlights (hybrid GGX)"),"1"),new Choice(L.T("Physical highlights (full GGX)"),"2")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Sun and material highlights"),null,choices,()=>Ini.Get("graphics_ggx_mode","0"),v=>Ini.Set("graphics_ggx_mode",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_ggx_mode","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_ggx_mode","0");Reload(); });
                _advancedGraphicsDefaults["graphics_ggx_mode"]="0";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid5.Children.Add(cell);
                CompactEffect(cell,"graphics_local_ggx","Physical local-light highlights","Uses GGX highlights for compatible lamp-lit materials.",false,()=>AdvancedGraphicsEnabled(),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_local_ggx"]="false";
            }
            grid5.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,0);grid5.Children.Add(cell);
                CompactEffect(cell,"graphics_specular_aa","Reduce highlight shimmer","Specular antialiasing broadens tiny unstable highlights to reduce sparkle during movement.",false,()=>AdvancedGraphicsEnabled(),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_specular_aa"]="false";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,1);grid5.Children.Add(cell);
                CompactEffect(cell,"graphics_local_falloff","Natural light falloff","Inverse-square attenuation changes how existing lamps fade with distance. It does not add reflected or bounced light.",false,()=>AdvancedGraphicsEnabled(),"graphics_light_intensity","Local light brightness",0.1,4.0,1.0,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("graphics_local_falloff",false)));
                _advancedGraphicsDefaults["graphics_local_falloff"]="false";
                _advancedGraphicsDefaults["graphics_light_intensity"]="1";
            }
            grid5.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,0);grid5.Children.Add(cell);
                CompactEffect(cell,null,"Local light source size","Live finite source radius relative to the lamp range. Larger values soften the near-light intensity peak.",true,()=>true,"graphics_light_core","Local light source size",0.01,1.0,0.05,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("graphics_local_falloff",false)));
                _advancedGraphicsDefaults["graphics_light_core"]="0.05";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,1);grid5.Children.Add(cell);
                CompactEffect(cell,null,"Local highlight sharpness","Live gloss-lobe sharpness for compatible local lights. Does not change shadow-map softness.",true,()=>true,"graphics_light_sharpness","Local highlight sharpness",1.0,8.0,1.0,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("graphics_local_falloff",false) || GraphicsFlag("graphics_local_ggx",false)));
                _advancedGraphicsDefaults["graphics_light_sharpness"]="1";
            }
            grid5.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,3);Grid.SetColumn(cell,0);grid5.Children.Add(cell);
                CompactEffect(cell,"graphics_native_sss","Skin light scattering","Subsurface scattering lets light spread through compatible skin materials. Waits until native skin resources are available.",false,()=>AdvancedGraphicsEnabled(),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_native_sss"]="false";
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,3);Grid.SetColumn(cell,1);grid5.Children.Add(cell);
                CompactEffect(cell,"graphics_pom","Surface depth and cavity shading","Parallax occlusion mapping (POM) adds apparent depth and cavity shading to supported textures; it does not add mesh geometry.",false,()=>AdvancedGraphicsEnabled(),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_pom"]="false";
            }
            Group(L.T("Shadows"));
            var grid6=new Grid();
            grid6.ColumnDefinitions.Add(new ColumnDefinition());grid6.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid6);
            grid6.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid6.Children.Add(cell);
                CompactEffect(cell,"effect_dynamic_shadows","Shadows from local lights *","Native shadows cast by lamps and other dynamic lights.",true,()=>true,null,null,0,1,1,()=>true);
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid6.Children.Add(cell);
                CompactEffect(cell,"graphics_pcss","Soft sun shadows *","PCSS contact-hardening shadows grow softer farther from the caster.",false,()=>true,"graphics_pcss_softness","Sun shadow softness",0.0,2.0,1.0,()=>(GraphicsFlag("graphics_pcss",false)));
            }
            grid6.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,0);grid6.Children.Add(cell);
                CompactEffect(cell,"graphics_directional_filter","Smoother sun-shadow transitions *","Filters directional shadow edges and blends cascade boundaries.",false,()=>true,"graphics_shadow_filter_strength","Shadow transition blend",0.0,1.0,1.0,()=>(GraphicsFlag("graphics_directional_filter",false)));
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,1);grid6.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>true));
                var choices=new[] {new Choice(L.T("Original"),"0"),new Choice(L.T("High: 4096 atlas"),"1"),new Choice(L.T("Ultra: 8192 atlas"),"2")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Sun shadow resolution *"),null,choices,()=>Ini.Get("graphics_shadow_resolution","0"),v=>Ini.Set("graphics_shadow_resolution",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_shadow_resolution","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_shadow_resolution","0");Reload(); });
            }
            grid6.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,0);grid6.Children.Add(cell);
                _graphicsVisibility.Add((cell,()=>(GraphicsFlag("effect_dynamic_shadows",true))));
                var choices=new[] {new Choice(L.T("Original"),"0"),new Choice(L.T("High: point 1024 / spot 2048"),"1"),new Choice(L.T("Ultra: point 2048 / spot 4096"),"2")};
                var control=ComboRow(SymbolRegular.Image24,L.T("Local shadow resolution *"),null,choices,()=>Ini.Get("graphics_local_shadow_resolution","0"),v=>Ini.Set("graphics_local_shadow_resolution",v),cell,changed:UpdateGraphicsVisibility);
                void Reload() { var index=Choices.IndexOf(choices,Ini.Get("graphics_local_shadow_resolution","0"));if(control is Segmented segmented) segmented.Select(index,notify:false);else if(control is ComboBox box) box.SelectedIndex=index; }
                _graphicsReload.Add(Reload);GraphicsDefaultMenu(control,()=>{ Ini.Set("graphics_local_shadow_resolution","0");Reload(); });
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,1);grid6.Children.Add(cell);
                CompactEffect(cell,null,"Shadow distance","Live native shadow distance multiplier; enables Picture tuning.",true,()=>true,"graphics_shadow_scale","Shadow distance",0.5,4.0,1.0,()=>true);
            }
            grid6.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,3);Grid.SetColumn(cell,0);grid6.Children.Add(cell);
                CompactEffect(cell,"graphics_contact_shadows","Contact shadows","Adds short screen-space shadows where nearby surfaces meet.",false,()=>true,"graphics_contact_strength","Contact shadow strength",0.0,1.0,1.0,()=>(GraphicsFlag("graphics_contact_shadows",false)));
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,3);Grid.SetColumn(cell,1);grid6.Children.Add(cell);
                CompactEffect(cell,"graphics_point_shadows","Point-light shadows","Adds compatible point-light occlusion from the existing depth information.",false,()=>true,"graphics_point_shadow_strength","Point-light shadow strength",0.0,1.0,0.85,()=>(GraphicsFlag("graphics_point_shadows",false)));
            }
            Group(L.T("Fog, light shafts and sky"));
            var grid7=new Grid();
            grid7.ColumnDefinitions.Add(new ColumnDefinition());grid7.ColumnDefinitions.Add(new ColumnDefinition());
            Body.Children.Add(grid7);
            grid7.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,0);grid7.Children.Add(cell);
                CompactEffect(cell,null,"Distance fog intensity","Live native haze multiplier; enables Picture tuning.",true,()=>true,"graphics_fog","Distance fog intensity",0.0,3.0,1.0,()=>true);
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,0);Grid.SetColumn(cell,1);grid7.Children.Add(cell);
                CompactEffect(cell,"graphics_volumetric_light","Sunlight shafts","Directional volumetric lighting makes sunlight visible through fog.",false,()=>true,"graphics_volumetric_intensity","Sunlight shaft intensity",0.0,3.0,1.0,()=>(GraphicsFlag("graphics_volumetric_light",false)));
            }
            grid7.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,0);grid7.Children.Add(cell);
                CompactEffect(cell,"graphics_point_fog","Lamp light in fog","Point-light volumetric fog makes compatible lamps illuminate the haze.",false,()=>true,"graphics_point_fog_intensity","Lamp fog intensity",0.0,3.0,1.0,()=>(GraphicsFlag("graphics_point_fog",false)));
            }
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,1);Grid.SetColumn(cell,1);grid7.Children.Add(cell);
                CompactEffect(cell,"graphics_sky_seam","Correct sky seam","Corrects the verified Hunter's Dream panorama seam; unsupported sky geometry remains original.",false,()=>AdvancedGraphicsEnabled(),null,null,0,1,1,()=>true);
                _advancedGraphicsDefaults["graphics_sky_seam"]="false";
            }
            grid7.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            {
                var cell=new StackPanel { Margin=new Thickness(4) };
                Grid.SetRow(cell,2);Grid.SetColumn(cell,0);grid7.Children.Add(cell);
                CompactEffect(cell,"graphics_sky_zenith","Sky smoothing","Filters the verified Dream sky zenith. Requires sky seam correction.",false,()=>AdvancedGraphicsEnabled() && (GraphicsFlag("graphics_sky_seam",false)),"graphics_sky_smoothing_strength","Sky smoothing strength",0.0,1.0,1.0,()=>AdvancedGraphicsEnabled() && ((GraphicsFlag("graphics_sky_seam",false)) && GraphicsFlag("graphics_sky_zenith",false)));
                _advancedGraphicsDefaults["graphics_sky_zenith"]="false";
                _advancedGraphicsDefaults["graphics_sky_smoothing_strength"]="1";
            }
            UpdateGraphicsVisibility();
        }
        private void CompactEffect(StackPanel cell,string? key,string title,string help,bool factory,Func<bool> available,string? strengthKey,string? strengthTitle,double lo,double hi,double strengthDefault,Func<bool> strengthAvailable) {
            var line=new Grid();cell.Children.Add(line);
            line.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(1,GridUnitType.Star) });line.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(1,GridUnitType.Star) });
            System.Windows.Controls.CheckBox? check=null;
            if(key!=null) {
                check=new System.Windows.Controls.CheckBox { Content=new TextBlock { Text=L.T(title),TextWrapping=System.Windows.TextWrapping.Wrap },IsChecked=GraphicsFlag(key,factory),Margin=new Thickness(0,0,8,0) };
                check.ToolTip=L.T(help);
                line.Children.Add(check);
                void Toggle() { Ini.Set(key,check.IsChecked==true?"1":"0");Ws.SaveSoon();UpdateGraphicsVisibility(); }
                check.Checked+=(_,_)=>Toggle();check.Unchecked+=(_,_)=>Toggle();
                _graphicsReload.Add(()=>check.IsChecked=GraphicsFlag(key,factory));
                GraphicsDefaultMenu(check,()=>{ Ini.Set(key,factory?"1":"0");check.IsChecked=factory; });
            } else { var label=new TextBlock { Text=L.T(title),TextWrapping=System.Windows.TextWrapping.Wrap,Margin=new Thickness(0,0,8,0) };line.Children.Add(label); }
            _graphicsVisibility.Add((cell,available));
            if(strengthKey==null) return;
            var values=new StackPanel();Grid.SetColumn(values,1);line.Children.Add(values);
            _graphicsVisibility.Add((values,()=> (key==null || GraphicsFlag(key,factory)) && strengthAvailable()));
            var number=new System.Windows.Controls.TextBox { Width=84,MinWidth=84,Margin=new Thickness(5,0,0,0) };
            var slider=new System.Windows.Controls.Slider { Minimum=1,Maximum=100,SmallChange=1,LargeChange=5,MinWidth=55 };
            var sliderLine=new Grid();
            sliderLine.ColumnDefinitions.Add(new ColumnDefinition());sliderLine.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
            sliderLine.Children.Add(slider);Grid.SetColumn(number,1);sliderLine.Children.Add(number);values.Children.Add(sliderLine);
            bool updating=false;
            double Percent() => Math.Clamp(1+99*(GraphicsNumber(strengthKey,strengthDefault)-lo)/(hi-lo),1,100);
            void Reload() { updating=true;slider.Value=Percent();number.Text=slider.Value.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);updating=false; }
            void Apply(double percent) { if(updating || !double.IsFinite(percent)) return;percent=Math.Clamp(percent,1,100);Ini.Set(strengthKey,(lo+(percent-1)*(hi-lo)/99).ToString("0.0000",System.Globalization.CultureInfo.InvariantCulture));if(new[]{"graphics_ao_strength","graphics_fog","graphics_bloom","graphics_shadow_scale"}.Contains(strengthKey)) Ini.Set("graphics_controls","1");Ws.SaveSoon();Reload();UpdateGraphicsVisibility(); }
            Reload();
            slider.ValueChanged+=(_,_)=>Apply(slider.Value);
            void Commit() { if(double.TryParse(number.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var n)) Apply(n);else Reload(); }
            number.LostFocus+=(_,_)=>Commit();
            number.KeyDown+=(_,e)=>{ if(e.Key==System.Windows.Input.Key.Enter) { Commit();e.Handled=true; } else if(e.Key==System.Windows.Input.Key.Up || e.Key==System.Windows.Input.Key.Down) { Apply(slider.Value+(e.Key==System.Windows.Input.Key.Up?1:-1));e.Handled=true; } };
            slider.ToolTip=L.T(strengthTitle+" — 1: minimum; 100: maximum. Arrows adjust; type in the number box.");
            GraphicsDefaultMenu(values,()=>{ Ini.Set(strengthKey,strengthDefault.ToString(System.Globalization.CultureInfo.InvariantCulture));Reload(); });
            _graphicsReload.Add(Reload);
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
