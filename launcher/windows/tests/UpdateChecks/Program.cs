using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BloodborneLauncher.Core;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
Check(Updater.Repository == "TheHiepDang/BloodborneX", "Fork update repository");
foreach (var (tag, expected) in new[] { ("v0.9.9-nr.rc1",100), ("0.9.9-nr.rc2",200), ("v0.9.9-beta.9.1",901), ("v0.9.9",0) })
{
    Check(Updater.TryVersion(tag, out var version, out var beta) && version == new Version(0,9,9) && beta == expected, $"Parse {tag}");
}
Check(Updater.Compare(new Version(0,9,9),100,new Version(0,9,9),200) < 0, "RC ordering");
Check(Updater.Compare(new Version(0,9,9),200,new Version(0,9,9),0) < 0, "Stable follows RC");
Check(!Updater.TryVersion("not-a-version",out _,out _), "Reject invalid version");
Check(Updater.TryVersion("v1.0.1",out var stableVersion,out var stableBeta) && stableVersion==new Version(1,0,1) && stableBeta==0,"Published stable tag");
Check(Updater.Compare(new Version(1,0,1),0,new Version(0,9,9),200)>0,"Stable 1.0.1 upgrades NR RC2");
Check(Updater.NrLabel(new Version(1,0,1),0)=="v1.0.1 NR","Stable NR label has no RC suffix");
Check(Updater.NrLabel(new Version(0,9,9),200)=="v0.9.9 NR RC2","Existing NR RC label retained");

Check(Updater.NrMetadataCompatible(new JsonObject { ["update_family"]=Updater.UpdateFamily, ["engine"]="lantern-native-nr-0.9.9-nr.rc2" }), "Compatible metadata");
Check(!Updater.NrMetadataCompatible(null), "Missing metadata");
Check(!Updater.NrMetadataCompatible(new JsonObject { ["engine"]="lantern-native-nr-x" }), "Missing family");
Check(!Updater.NrMetadataCompatible(new JsonObject { ["update_family"]=Updater.UpdateFamily, ["engine"]="lantern-fps89" }), "Reject old engine");
using var document = JsonDocument.Parse("""
{"tag_name":"v0.9.9-nr.rc2","html_url":"https://github.com/TheHiepDang/BloodborneX/releases/tag/v0.9.9-nr.rc2","assets":[
 {"name":"LanternEngine-0.9.9-nr.rc2-source.zip","browser_download_url":"https://invalid/source","size":123},
 {"name":"LanternEngine-0.9.9-nr.rc2-linux-x64.zip","browser_download_url":"https://invalid/linux","size":123},
 {"name":"LanternEngine-0.9.9-nr.rc2-win64.zip","browser_download_url":"https://invalid/windows","size":456},
 {"name":"package.json","browser_download_url":"https://invalid/package","size":123}]}
""");
var parse = typeof(Updater).GetMethod("Parse",BindingFlags.Static|BindingFlags.NonPublic)!;
var release = (Updater.Release)parse.Invoke(null,new object[] {document.RootElement})!;
Check(release.ZipUrl == "https://invalid/windows", "Select Windows runtime ZIP, not source or Linux");
using var bloodbornePcDocument = JsonDocument.Parse("""
{"tag_name":"v1.0.3","html_url":"https://github.com/TheHiepDang/BloodborneX/releases/tag/v1.0.3","assets":[
 {"name":"BloodbornePC-1.0.3-source.zip","browser_download_url":"https://invalid/source-pc","size":123},
 {"name":"BloodbornePC-1.0.3-win64.zip","browser_download_url":"https://invalid/windows-pc","size":456},
 {"name":"package.json","browser_download_url":"https://invalid/package-pc","size":123}]}
""");
var bloodbornePcRelease = (Updater.Release)parse.Invoke(null,new object[] { bloodbornePcDocument.RootElement })!;
Check(bloodbornePcRelease.ZipUrl == "https://invalid/windows-pc", "Select BloodbornePC Windows runtime ZIP");
// New primary archive must beat a legacy compatibility alias in either asset order.
foreach (var reverse in new[] {false,true}) {
    var assets=new[] {
        new {name="LanternEngine-1.0.3-win64.zip",browser_download_url="https://invalid/legacy",size=456},
        new {name="BloodborneX-1.0.3-win64.zip",browser_download_url="https://invalid/branded",size=456},
        new {name="BloodborneX-1.0.3-source.zip",browser_download_url="https://invalid/source",size=123}};
    using var branded=JsonDocument.Parse(JsonSerializer.Serialize(new {tag_name="v1.0.3",html_url="https://invalid/release",assets=reverse?assets.Reverse().ToArray():assets}));
    var parsed=(Updater.Release)parse.Invoke(null,new object[]{branded.RootElement})!;
    Check(parsed.ZipUrl=="https://invalid/branded", "Prefer BloodborneX runtime archive, independent of asset order");
}
using var stableDocument = JsonDocument.Parse("""
{"tag_name":"v1.0.1","name":"v1.0.1","html_url":"https://github.com/TheHiepDang/BloodborneX/releases/tag/v1.0.1","assets":[
 {"name":"LanternEngine-1.0.1-source.zip","browser_download_url":"https://invalid/source","size":123},
 {"name":"LanternEngine-1.0.1-win64.zip","browser_download_url":"https://invalid/windows-1.0.1","size":456},
 {"name":"package.json","browser_download_url":"https://invalid/package-1.0.1","size":123}]}
""");
var stableRelease=(Updater.Release)parse.Invoke(null,new object[]{stableDocument.RootElement})!;
Check(stableRelease.Version==new Version(1,0,1) && stableRelease.Beta==0 && stableRelease.ZipUrl=="https://invalid/windows-1.0.1" && stableRelease.PackageUrl=="https://invalid/package-1.0.1","Published stable assets selected");
Check(Updater.NrMetadataCompatible(new JsonObject { ["version"]="1.0.1",["update_family"]=Updater.UpdateFamily,["engine"]="lantern-native-nr-1.0.1" }),"Published stable NR metadata");
var marker = Path.Combine(AppContext.BaseDirectory,"LOCAL-NR-RELEASE");
Check(!File.Exists(marker), "Test output must not already contain a release marker");
try
{
    File.WriteAllText(marker,"test-owned marker");
    Check(Updater.IsLocalBuild && !Updater.EngineOutdated, "NR marker guard");
    Check(Updater.CurrentLabel == "v0.9.9 NR RC1", "Candidate version label");
    Check(Updater.BetaChannel(new LauncherSettings()), "NR defaults to prerelease channel");
    Check(await Updater.NeedsPackageAsync(release), "NR update requires whole package");
    Check(await Updater.SwitchNeedsPackageAsync(release), "NR version switch requires whole package");
    var rejected = false;
    try { await Updater.InstallAsync(release,false); } catch (InvalidOperationException) { rejected=true; }
    Check(rejected,"Reject launcher-only NR update before any download");
}
finally { File.Delete(marker); }
Console.WriteLine("PASS: fork, RC ordering, Windows asset selection, metadata and NR package guards");

// Branded package preserves the executable and user-data contract of already published clients.
Check(Updater.ExeName=="Lantern Engine.exe" && Updater.OldExeName=="Bloodborne Launcher.exe", "Legacy executable discovery stays compatible");
var migrationRoot=Path.Combine(Path.GetTempPath(),"BloodbornePC-updater-"+Guid.NewGuid().ToString("N"));
var incoming=Path.Combine(migrationRoot,"download","BloodbornePC");
var installed=Path.Combine(migrationRoot,"installed");
Directory.CreateDirectory(Path.Combine(incoming,"engine","out"));
Directory.CreateDirectory(Path.Combine(installed,"user","presets"));
try {
    File.WriteAllText(Path.Combine(incoming,Updater.ExeName),"new full launcher");
    File.WriteAllText(Path.Combine(incoming,Updater.OldExeName),"legacy forwarder");
    File.WriteAllText(Path.Combine(incoming,"BloodbornePC.exe"),"new entry forwarder");
    File.WriteAllText(Path.Combine(incoming,"engine","out","bb-probe.exe"),"new engine");
    File.WriteAllText(Path.Combine(incoming,"lantern.ini"),"factory defaults");
    var self=Path.Combine(installed,Updater.ExeName);
    File.WriteAllText(self,"old full launcher");
    File.WriteAllText(Path.Combine(installed,"lantern.ini"),"user settings");
    File.WriteAllText(Path.Combine(installed,"user","presets","mine.bbxpreset"),"user preset");
    var legacyFound=Directory.GetFiles(Path.Combine(migrationRoot,"download"),"Lantern Engine.exe",SearchOption.AllDirectories).Single();
    Check(Path.GetDirectoryName(legacyFound)==incoming,"Published updater finds branded ZIP root through legacy executable");
    var copy=typeof(Updater).GetMethod("CopyPackage",BindingFlags.NonPublic|BindingFlags.Static)!;
    copy.Invoke(null,new object[]{incoming,installed,self,new List<string>()});
    Check(File.ReadAllText(self)=="new full launcher","Old running launcher path replaced for restart");
    Check(File.ReadAllText(Path.Combine(installed,"BloodbornePC.exe"))=="new entry forwarder","Branded entry installed");
    Check(File.ReadAllText(Path.Combine(installed,"engine","out","bb-probe.exe"))=="new engine","Full engine replaced");
    Check(File.ReadAllText(Path.Combine(installed,"lantern.ini"))=="user settings","Existing config preserved");
    Check(File.ReadAllText(Path.Combine(installed,"user","presets","mine.bbxpreset"))=="user preset","Custom preset preserved");
} finally {Directory.Delete(migrationRoot,true);}
Console.WriteLine("PASS: BloodbornePC migration discovery, self-replacement, new entry and user-data preservation");

namespace BloodborneLauncher.Core
{
    public sealed class LauncherSettings { public string Str(string name) => ""; }
    public static class Paths
    {
        public static string PortDir => AppContext.BaseDirectory;
        public static string? DetectPortDir() => null;
    }
}
