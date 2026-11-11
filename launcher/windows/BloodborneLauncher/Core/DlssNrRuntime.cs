using System.Security.Cryptography;
using System.IO;

namespace BloodborneLauncher.Core
{
    /// <summary>Read-only validation of the user-supplied, never-redistributed NR runtime.</summary>
    public static class DlssNrRuntime
    {
        public const string KnownHash = "CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED";
        public const long KnownSize = 276162560;
        public const string OriginalHash = "2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F";
        public const long OriginalSize = 266424944;
        public enum RuntimeBuild { Unknown, OpenNrFp16, OriginalRenoDx }
        public static RuntimeBuild Identify(long size, string hash) =>
            size == KnownSize && string.Equals(hash, KnownHash, StringComparison.OrdinalIgnoreCase)
                ? RuntimeBuild.OpenNrFp16
                : size == OriginalSize && string.Equals(hash, OriginalHash, StringComparison.OrdinalIgnoreCase)
                    ? RuntimeBuild.OriginalRenoDx : RuntimeBuild.Unknown;
        private static string? _cachedPath, _cachedStatus;
        private static DateTime _cachedWrite;

        public static bool PrefersOriginal(string vendor, string gpu) =>
            string.Equals(vendor, "NVIDIA", StringComparison.OrdinalIgnoreCase) &&
            gpu.Contains("GeForce RTX 50", StringComparison.OrdinalIgnoreCase);
        public static string RuntimePath(string root) => Path.Combine(root, "nvngx_dlssnr.dll");
        // The engine uses its actual Vulkan adapter for runtime compatibility checks.
        public static string DirectoryName => Environment.GetEnvironmentVariable("BB_DLSS_DIR") is { Length: > 0 } external
            ? external : new[]
        {
            Path.Combine(Paths.DataDir, "dlss"), Path.Combine(Paths.EngineDir, "dlss"),
        }.FirstOrDefault(root => File.Exists(RuntimePath(root))) ?? Path.Combine(Paths.DataDir, "dlss");
        public static string PathName => RuntimePath(DirectoryName);

        public static string Status()
        {
            var path = PathName;
            if (!File.Exists(path))
                return L.T("Not installed. Put your own nvngx_dlssnr.dll in the dlss folder; Lantern never downloads or redistributes it");
            try
            {
                var info = new FileInfo(path);
                if (_cachedPath == path && _cachedWrite == info.LastWriteTimeUtc && _cachedStatus != null)
                    return _cachedStatus;
                using var stream = File.OpenRead(path);
                var hash = Convert.ToHexString(SHA256.HashData(stream));
                _cachedPath = path;
                _cachedWrite = info.LastWriteTimeUtc;
                return _cachedStatus = L.F($"User-supplied NR runtime: {info.Length} bytes. Custom hashes are always allowed; the renderer checks exports and initialization. SHA-256 {hash}");
            }
            catch (Exception error) { return L.F($"Cannot validate runtime: {error.Message}"); }
        }
    }
}
