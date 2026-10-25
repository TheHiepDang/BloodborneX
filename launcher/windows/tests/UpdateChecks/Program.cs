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

namespace BloodborneLauncher.Core
{
    public sealed class LauncherSettings { public string Str(string name) => ""; }
    public static class Paths
    {
        public static string PortDir => AppContext.BaseDirectory;
        public static string? DetectPortDir() => null;
    }
}
