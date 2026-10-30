using BloodborneLauncher.Core;
using System.Security.Cryptography;

static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
var identities = new[] {
    (DlssNrRuntime.KnownSize,DlssNrRuntime.KnownHash,DlssNrRuntime.RuntimeBuild.OpenNrFp16),
    (DlssNrRuntime.OriginalSize,DlssNrRuntime.OriginalHash,DlssNrRuntime.RuntimeBuild.OriginalRenoDx)
};
foreach (var (size,hash,build) in identities) {
    Check(DlssNrRuntime.Identify(size,hash)==build,"known identity");
    Check(DlssNrRuntime.Identify(size,hash.ToLowerInvariant())==build,"case-insensitive identity");
    Check(DlssNrRuntime.Identify(size+1,hash)==DlssNrRuntime.RuntimeBuild.Unknown,"wrong size");
    Check(DlssNrRuntime.Identify(size,"0"+hash[1..])==DlssNrRuntime.RuntimeBuild.Unknown,"wrong digest");
}
Check(DlssNrRuntime.Identify(DlssNrRuntime.KnownSize,DlssNrRuntime.OriginalHash)==DlssNrRuntime.RuntimeBuild.Unknown,"mixed identity");
Check(DlssNrRuntime.Identify(0,"")==DlssNrRuntime.RuntimeBuild.Unknown,"empty identity");
Check(DlssNrRuntime.PrefersOriginal("NVIDIA","NVIDIA GeForce RTX 5080"),"RTX 5080 route");
Check(DlssNrRuntime.PrefersOriginal("nvidia","geforce rtx 5090 laptop GPU"),"RTX 5090 laptop route");
Check(!DlssNrRuntime.PrefersOriginal("NVIDIA","NVIDIA GeForce RTX 3070"),"RTX 3070 route");
Check(!DlssNrRuntime.PrefersOriginal("NVIDIA","NVIDIA GeForce RTX 4090"),"RTX 4090 route");
Check(!DlssNrRuntime.PrefersOriginal("NVIDIA","NVIDIA Quadro RTX 5000"),"old RTX 5000 is not GeForce 50-series");
Check(!DlssNrRuntime.PrefersOriginal("AMD","GeForce RTX 5080"),"wrong vendor");
Check(DlssNrRuntime.RuntimePath("dlss")==Path.Combine("dlss","nvngx_dlssnr.dll"),"single flat runtime path");
// Stray subfolder DLLs must never make the launcher report NR as installed.
var temporary=Path.Combine(Path.GetTempPath(),"nr-flat-path-"+Guid.NewGuid());
try {
    Paths.DataDir=temporary; Paths.EngineDir=Path.Combine(temporary,"engine");
    foreach (var folder in new[]{"original","compat"}) {
        var nested=Path.Combine(temporary,"dlss",folder);
        Directory.CreateDirectory(nested); File.WriteAllText(Path.Combine(nested,"nvngx_dlssnr.dll"),"test");
    }
    Check(DlssNrRuntime.PathName==Path.Combine(temporary,"dlss","nvngx_dlssnr.dll"),"nested runtimes ignored");
    Check(DlssNrRuntime.Status().StartsWith("Not installed."),"nested files do not enable NR");
    Directory.CreateDirectory(Path.Combine(Paths.EngineDir,"dlss"));
    File.WriteAllText(Path.Combine(Paths.EngineDir,"dlss","nvngx_dlssnr.dll"),"test");
    Check(DlssNrRuntime.DirectoryName==Path.Combine(Paths.EngineDir,"dlss"),"engine flat root fallback");
    File.WriteAllText(Path.Combine(temporary,"dlss","nvngx_dlssnr.dll"),"test");
    Check(DlssNrRuntime.DirectoryName==Path.Combine(temporary,"dlss"),"package flat root preferred");
} finally {
    Directory.Delete(temporary,true); Paths.DataDir=""; Paths.EngineDir="";
}
foreach (var path in args) {
    using var stream=File.OpenRead(path);
    var hash=Convert.ToHexString(SHA256.HashData(stream));
    var identity=DlssNrRuntime.Identify(stream.Length,hash);
    Check(identity!=DlssNrRuntime.RuntimeBuild.Unknown,"actual supplied file rejected");
    Console.WriteLine($"Actual DLL: {identity}, {stream.Length} bytes, SHA256 {hash}");
}
Console.WriteLine("PASS: both exact NR builds; mutations, mismatched size/hash, and unknown files rejected.");

namespace BloodborneLauncher.Core {
    public static class Paths { public static string DataDir="";public static string EngineDir=""; }
    public sealed record Hardware(string Vendor,string Gpu) { public static Hardware Detect()=>new("NVIDIA","NVIDIA GeForce RTX 3070"); }
    public static class L { public static string T(string s)=>s;public static string F(FormattableString s)=>s.ToString(); }
}
