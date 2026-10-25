using System.Security.Cryptography;
using System.Text.Json;
using BloodborneLauncher.Core;

var dir = Path.Combine(AppContext.BaseDirectory, "bloodbornex-streamline-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(dir);
try
{
    var hashes = new Dictionary<string, string>();
    foreach (var name in StreamlineRuntime.Files)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("fixture:" + name);
        File.WriteAllBytes(Path.Combine(dir, name), bytes);
        hashes[name] = Convert.ToHexString(SHA256.HashData(bytes));
    }
    void Manifest(string digest) => File.WriteAllText(
        Path.Combine(dir, "streamline-runtime.json"),
        JsonSerializer.Serialize(new { version = StreamlineRuntime.Version, sdk_sha256 = digest, files = hashes }));
    void Expect(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        Console.WriteLine("PASS " + label);
    }
    Manifest(StreamlineRuntime.SdkSha256);
    Expect(StreamlineRuntime.Problem(dir) is null, "complete matched set");
    var missing = Path.Combine(dir, "sl.pcl.dll");
    File.Move(missing, missing + ".hold");
    Expect(StreamlineRuntime.Problem(dir)?.Contains("sl.pcl.dll") == true, "missing plugin rejected");
    File.Move(missing + ".hold", missing);
    File.AppendAllText(missing, "mixed");
    Expect(StreamlineRuntime.Problem(dir)?.Contains("sl.pcl.dll") == true, "mixed plugin rejected");
    File.WriteAllBytes(missing, System.Text.Encoding.UTF8.GetBytes("fixture:sl.pcl.dll"));
    var extra = Path.Combine(dir, "sl.old.dll");
    File.WriteAllText(extra, "extra");
    Expect(StreamlineRuntime.Problem(dir)?.Contains("extra") == true, "extra plugin rejected");
    var backup = StreamlineRuntime.RetireExtraPlugins(dir);
    Expect(backup != null && !File.Exists(extra) && File.Exists(Path.Combine(backup, "sl.old.dll")) &&
           StreamlineRuntime.Problem(dir) is null, "older plugin backed up after an in-place update");
    Manifest(new string('0', 64));
    Expect(StreamlineRuntime.Problem(dir)?.Contains("different SDK") == true, "wrong SDK rejected");
}
finally
{
    var temporaryRoot = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) +
                        Path.DirectorySeparatorChar;
    var resolved = Path.GetFullPath(dir);
    if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(resolved).StartsWith("bloodbornex-streamline-checks-", StringComparison.Ordinal))
        throw new Exception("Refusing to clean an unexpected test folder");
    if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
}
