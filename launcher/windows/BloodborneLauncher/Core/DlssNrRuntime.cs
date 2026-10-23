using System.Security.Cryptography;
using System.IO;

namespace BloodborneLauncher.Core
{
    /// <summary>Read-only validation of the user-supplied, never-redistributed NR runtime.</summary>
    public static class DlssNrRuntime
    {
        public const string KnownHash = "CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED";
        public const long KnownSize = 276162560;
        private static string? _cachedPath, _cachedStatus;
        private static DateTime _cachedWrite;

        public static string PathName => new[]
        {
            Path.Combine(Paths.DataDir, "dlss", "nvngx_dlssnr.dll"),
            Path.Combine(Paths.EngineDir, "dlss", "nvngx_dlssnr.dll"),
        }.FirstOrDefault(File.Exists) ?? Path.Combine(Paths.DataDir, "dlss", "nvngx_dlssnr.dll");

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
                if (info.Length != KnownSize)
                    return L.T("Rejected: size does not match the validated DVS production runtime");
                using var stream = File.OpenRead(path);
                if (Convert.ToHexString(SHA256.HashData(stream)) != KnownHash)
                    return L.T("Rejected: SHA-256 does not match the validated runtime");
                _cachedPath = path;
                _cachedWrite = info.LastWriteTimeUtc;
                return _cachedStatus = L.F($"Validated: {System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion}; ready for an RTX GPU");
            }
            catch (Exception error) { return L.F($"Cannot validate runtime: {error.Message}"); }
        }
    }
}
