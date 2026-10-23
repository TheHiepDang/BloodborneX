using BloodborneLauncher.Core;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
}

// Only memory is changed. Never load/save the player's INI in this test.
var ini = new IniFile();
Check(!ini.NrPerformance, "Old INIs default to full-size NR");
ini.Set("dlss_nr_intensity", "2.00");
ini.Set("dlss_nr_height", "720");
ini.NrPerformance = true;
Check(ini.Get("dlss_nr_height") == "720", "Performance preserves input height");
Check(ini.Get("dlss_nr_model_scale") == "0.750", "Model scale is independent");
Check(ini.NrPerformance, "Existing optimized settings map to checked Performance");
ini.Set("dlss_nr_height", "1080");
Check(ini.NrPerformance && ini.Get("dlss_nr_model_scale") == "0.750", "Height changes retain global Performance");
ini.NrPerformance = false;
Check(ini.Get("dlss_nr_model_scale") == "1.000", "Unchecking restores full-size model");
Check(ini.Get("dlss_nr_height") == "1080", "Unchecking preserves selected height");
Check(ini.Get("dlss_nr_intensity") == "2.00", "Checkbox preserves NR tuning");
Check(!Choices.DlssNrInputs(540).Any(c => c.Value == "720"), "Clamp choices to output height");
Check(Choices.DlssNrInputs(1080).Any(c => c.Value == "1080"), "Keep normal 1080p choice");
Check(Choices.DlssNrInputs(2160).All(c => int.TryParse(c.Value, out _)), "Resolution list has only normal heights");
Console.WriteLine("PASS: global Performance toggle, height independence, old-INI compatibility, tuning preservation");
