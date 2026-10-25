using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace BloodborneLauncher.Core
{
    /// <summary>Validates the matched Streamline set installed from NVIDIA's pinned SDK archive.</summary>
    public static class StreamlineRuntime
    {
        public const string Version = "2.14.16";
        public const string SdkSha256 = "32041E51DB0CAB709A271B72A352281B690C2ABBD669A827809401A5DD23956B";
        public static readonly string[] Files =
        {
            "sl.interposer.dll", "sl.common.dll", "sl.dlss_g.dll", "sl.reflex.dll",
            "sl.pcl.dll", "nvngx_dlssg.dll", "NvLowLatencyVk.dll",
            "nvngx_dlss.license.txt", "reflex.license.txt", "LICENSE-Streamline.txt",
        };

        /// <summary>
        /// An older updater copies package files in place, so plugins from an earlier
        /// Streamline installation can remain beside the new matched set. Keep those
        /// plugins recoverable while preventing Streamline from loading a mixed set.
        /// </summary>
        public static string? RetireExtraPlugins(string directory)
        {
            if (!File.Exists(Path.Combine(directory, "streamline-runtime.json"))) return null;
            var extras = Directory.GetFiles(directory, "sl.*.dll")
                .Where(path => !Files.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
                .ToArray();
            if (extras.Length == 0) return null;
            var backup = Path.Combine(directory, "backups", "before-streamline-update-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backup);
            foreach (var path in extras)
                File.Move(path, Path.Combine(backup, Path.GetFileName(path)));
            return backup;
        }

        /// <returns>Null only when every expected file matches the installer manifest.</returns>
        public static string? Problem(string directory)
        {
            if (!Directory.Exists(directory))
                return "Engine folder is missing.";
            var manifestPath = Path.Combine(directory, "streamline-runtime.json");
            if (!File.Exists(manifestPath))
                return "The Streamline runtime is missing. Reinstall the current BloodborneX package.";
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
                var root = document.RootElement;
                if (root.GetProperty("version").GetString() != Version ||
                    !string.Equals(root.GetProperty("sdk_sha256").GetString(), SdkSha256,
                        StringComparison.OrdinalIgnoreCase))
                    return "The Streamline runtime is from a different SDK. Reinstall the current BloodborneX package.";
                var hashes = root.GetProperty("files");
                foreach (var name in Files)
                {
                    var path = Path.Combine(directory, name);
                    if (!File.Exists(path) || new FileInfo(path).Length == 0)
                        return $"{name} is missing or empty. Reinstall the current BloodborneX package.";
                    var expected = hashes.GetProperty(name).GetString();
                    using var stream = File.OpenRead(path);
                    var actual = Convert.ToHexString(SHA256.HashData(stream));
                    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                        return $"{name} differs from the matched runtime. Reinstall the current BloodborneX package.";
                }
                var plugins = Directory.GetFiles(directory, "sl.*.dll")
                    .Select(Path.GetFileName)
                    .Where(name => name != null && !Files.Contains(name, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                if (plugins.Length > 0)
                    return $"An extra Streamline plugin ({plugins[0]}) is present. Reinstall the current BloodborneX package.";
                return null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException
                or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                return "The Streamline runtime record is invalid. Reinstall the current BloodborneX package.";
            }
        }
    }
}
