using System.Windows.Controls;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views
{
    /// <summary>Neural model settings and native edit composition.</summary>
    public sealed class NeuralRenderingPage : LauncherPage
    {
        private readonly TextBlock _dlssNrHint;
        public NeuralRenderingPage() : base(L.T("Neural Rendering"),
            L.T("Requires DLSS SR/DLAA and a compatible user-supplied NR runtime. Changes to enable/input height may require a restart."))
        {
            Group(L.T("Model and input"));
            var dlssMore = Body;
            ToggleRow(SymbolRegular.BrainCircuit24, L.T("Enable neural rendering"),
                L.T("Experimental feature 18 pass before DLSS SR/DLAA. Windows and NVIDIA RTX only; off by default"),
                () => Ini.Get("dlss_nr", "0") == "1", v => Ini.Set("dlss_nr", v ? "1" : "0"), dlssMore);
            var nrPerformance = new System.Windows.Controls.CheckBox
            {
                Content = L.T("Performance"), IsChecked = Ini.NrPerformance,
                Margin = new System.Windows.Thickness(12, 0, 0, 0),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                ToolTip = L.T("Use optimized neural rendering at the selected input height"),
            };
            nrPerformance.Checked += (_, _) => { Ini.NrPerformance = true; Ws.SaveSoon(); };
            nrPerformance.Unchecked += (_, _) => { Ini.NrPerformance = false; Ws.SaveSoon(); };
            ComboRow(SymbolRegular.ResizeLarge24, L.T("NR input height"),
                L.T("Overrides the DLSS quality input size. Native AA/DLAA remains at output resolution"),
                Choices.DlssNrInputs(Choices.OutputSize(Ini.Get("output_res", "1920x1080")).H),
                () => Ini.Get("dlss_nr_height", "720"), v => Ini.Set("dlss_nr_height", v),
                dlssMore, width: 180, accessory: nrPerformance);
            IniSlider(SymbolRegular.SlideSize24, L.T("NR intensity"), 0, 2, "dlss_nr_intensity", dlssMore);
            var nrPassSections = new List<Panel>();
            void UpdateNrPassSections()
            {
                var count = int.TryParse(Ini.Get("dlss_nr_passes", "1"), out var n) ? Math.Clamp(n, 1, 3) : 1;
                for (var p = 0; p < nrPassSections.Count; p++)
                    nrPassSections[p].Visibility = Ini.Flag("dlss_nr_customize_passes") && count >= p + 2
                        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            IniCombo(SymbolRegular.Options24, L.T("NR passes"),
                L.T("Chained models with independent histories. More passes cost GPU time and memory; changing this rebuilds the model"),
                Choices.DlssNrPasses, "dlss_nr_passes", dlssMore, changed: UpdateNrPassSections);
            IniToggle(SymbolRegular.Options24, L.T("Customize extra passes"),
                L.T("Off: extra passes inherit the main parameters. On: each enabled extra pass uses its own saved parameters. White point is shared"),
                "dlss_nr_customize_passes", dlssMore, changed: UpdateNrPassSections);
            IniCombo(SymbolRegular.Options24, L.T("NR preset"),
                L.T("Undocumented model hint, not strength. Presets may produce similar results"), Choices.DlssNrPresets,
                "dlss_nr_preset", dlssMore);
            IniCombo(SymbolRegular.Options24, L.T("NR style"),
                L.T("Three community-tested styles; names are not NVIDIA metadata"), Choices.DlssNrStyles,
                "dlss_nr_style", dlssMore);
            IniSlider(SymbolRegular.Shapes24, L.T("NR local structure"), 0, 2, "dlss_nr_local_structure", dlssMore);
            IniSlider(SymbolRegular.Color24, L.T("NR local tone"), 0, 2, "dlss_nr_local_tone", dlssMore);
            IniSlider(SymbolRegular.Shapes24, L.T("NR skin structure"), -1, 2, "dlss_nr_skin_structure", dlssMore, fallback: -1);
            IniToggle(SymbolRegular.Eye24, L.T("NR automatic mask"), null, "dlss_nr_auto_mask", dlssMore);
            IniSlider(SymbolRegular.BrightnessHigh24, L.T("NR white point (all passes)"), 0.125, 8, "dlss_nr_white_point", dlssMore,
                step: 0.025, fallback: 0.125);
            for (var pass = 2; pass <= 3; pass++)
            {
                var section = new StackPanel(); nrPassSections.Add(section); dlssMore.Children.Add(section);
                section.Children.Add(new TextBlock { Text = L.F($"NR pass {pass} parameters"), FontSize = 15,
                    Margin = new System.Windows.Thickness(12, 10, 0, 4) });
                var prefix = $"dlss_nr_pass{pass}_";
                IniCombo(SymbolRegular.Options24, L.T("NR style"),
                    L.T("Use main style preserves the main choice; Standard, Natural and Cinematic override only this pass"),
                    new[] { new Choice(L.T("Use main style"), "-1") }.Concat(Choices.DlssNrStyles).ToArray(),
                    prefix + "style", section);
                IniSlider(SymbolRegular.SlideSize24, L.T("Intensity"), 0, 2, prefix + "intensity", section);
                IniSlider(SymbolRegular.Shapes24, L.T("Local structure"), 0, 2, prefix + "local_structure", section);
                IniSlider(SymbolRegular.Color24, L.T("Local tone"), 0, 2, prefix + "local_tone", section);
                IniSlider(SymbolRegular.Shapes24, L.T("Skin structure"), -1, 2, prefix + "skin_structure", section, fallback: -1);
            }
            UpdateNrPassSections();
            Add(Card(SymbolRegular.ShieldCheckmark24, L.T("Neural runtime"), "", null, out _dlssNrHint), dlssMore);

            Group(L.T("Input and conditioning"));
            IniSlider(SymbolRegular.Shapes24, L.T("Extra neural style"), -1024, 1024,
                "dlss_nr_extra_style", step: 1, fallback: 0);
            Add(Card(SymbolRegular.Options24, L.T("Extra style test"),
                L.T("Zero is stock. Adjust live in the in-game NR tab. Applies after the slider settles, with a brief NR feature/history rebuild. Affects all passes."), null, out _));
            Group(L.T("Fine-tune NR's result"));
            int lookRowsStart = -1;
            void UpdateLookControls()
            {
                if (lookRowsStart < 0) return;
                var log = Ini.Get("dlss_nr_composition", "0") == "1";
                for (var i = lookRowsStart; i < Body.Children.Count; ++i)
                    Body.Children[i].IsEnabled = log && (i == lookRowsStart || Ini.Flag("dlss_nr_look"));
            }
            IniCombo(SymbolRegular.Options24, L.T("Composition"),
                L.T("Legacy preserves the previous appearance. Log transfer enables the controls below."),
                new[] { new Choice(L.T("Legacy"), "0"), new Choice(L.T("Log transfer"), "1") }, "dlss_nr_composition", changed: UpdateLookControls);
            lookRowsStart = Body.Children.Count;
            IniToggle(SymbolRegular.Color24, L.T("Adjust NR's look"),
                L.T("Off uses neutral log transfer. These controls reshape neural edits, before DLSS SR and UI."), "dlss_nr_look", changed: UpdateLookControls);
            IniCombo(SymbolRegular.Options24, L.T("Detail stability"),
                L.T("Constrains amplified detail near strong source edges."),
                new[] { new Choice(L.T("Auto"), "0"), new Choice(L.T("Off"), "1"), new Choice(L.T("Always"), "2") }, "dlss_nr_detail_stability");
            IniSlider(SymbolRegular.SlideSize24, L.T("Edit strength"), 0, 4, "dlss_nr_edit_strength", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Brightening"), 0, 4, "dlss_nr_brightening", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Darkening"), 0, 4, "dlss_nr_darkening", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Colour change"), 0, 4, "dlss_nr_colour", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Hue shift"), 0, 2, "dlss_nr_hue", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Shadows"), 0, 4, "dlss_nr_shadows", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Midtones"), 0, 4, "dlss_nr_midtones", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Highlights"), 0, 4, "dlss_nr_highlights", step: 0.05, fallback: 1);
            Group(L.T("Limits and stability"));
            IniSlider(SymbolRegular.SlideSize24, L.T("Max brightening (stops)"), 0, 8, "dlss_nr_max_brightening", step: 0.05, fallback: 2);
            IniSlider(SymbolRegular.SlideSize24, L.T("Max darkening (stops)"), 0, 8, "dlss_nr_max_darkening", step: 0.05, fallback: 2);
            IniSlider(SymbolRegular.SlideSize24, L.T("Max colour change (stops)"), 0, 8, "dlss_nr_max_colour", step: 0.05, fallback: 2);
            IniSlider(SymbolRegular.SlideSize24, L.T("Near-black protection"), 0, 0.1, "dlss_nr_black_gate", step: 0.001, fallback: 0.001);
            IniSlider(SymbolRegular.SlideSize24, L.T("Large-scale tone"), 0, 3, "dlss_nr_tone", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Fine detail"), 0, 3, "dlss_nr_detail", step: 0.05, fallback: 1);
            IniSlider(SymbolRegular.SlideSize24, L.T("Detail radius (%)"), 0.1, 5, "dlss_nr_radius", step: 0.05, fallback: 1.5);
            IniSlider(SymbolRegular.SlideSize24, L.T("Halo suppression"), 0, 1, "dlss_nr_halo", step: 0.05, fallback: 0);
            IniSlider(SymbolRegular.SlideSize24, L.T("Halo next to dark objects"), 0, 1, "dlss_nr_dark_halo", step: 0.05, fallback: 0);
            IniCombo(SymbolRegular.Options24, L.T("Stabilize"),
                L.T("Static accepts stationary pixels only. Motion reprojects edits using motion and rejects depth changes."),
                new[] { new Choice(L.T("Off"), "0"), new Choice(L.T("Static"), "1"), new Choice(L.T("Motion"), "2") }, "dlss_nr_stabilize");
            IniSlider(SymbolRegular.SlideSize24, L.T("Stabilize time (ms)"), 0, 500, "dlss_nr_stabilize_ms", step: 1, fallback: 60);
            IniToggle(SymbolRegular.Shapes24, L.T("Stabilize detail"),
                L.T("Off stabilizes the broad tone and keeps current fine edits."), "dlss_nr_stabilize_detail");
            Add(Card(SymbolRegular.Options24, L.T("Transfer limits"),
                L.T("Limits apply after strength. Zero stops prevents that change. Edit strength above 1 amplifies existing edits and can expose artifacts."), null, out _));
            UpdateLookControls();
        }

        private void IniSlider(SymbolRegular icon, string title, double min, double max, string key, Panel? into = null,
            double step = 0.05, double fallback = 1.0) =>
            SliderRow(icon, title, null, min, max, step,
                () => ParseDouble(Ini.Get(key, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture)), fallback),
                v => Ini.Set(key, v.ToString(key is "dlss_nr_white_point" or "dlss_nr_black_gate" ? "0.000" : "0.00", System.Globalization.CultureInfo.InvariantCulture)),
                format: key is "dlss_nr_white_point" or "dlss_nr_black_gate" ? "0.000" : "0.00", into: into, defaultValue: fallback);

        private void UpdateRuntime() => SetHint(_dlssNrHint, DlssNrRuntime.Status());
        public override void OnShown() => UpdateRuntime();
    }
}
