using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MoreCoopManager;

/// <summary>
/// Locates the Subnautica 2 install directory by inspecting the Steam
/// registry entry and walking through every library declared in
/// steamapps/libraryfolders.vdf.
/// </summary>
internal static class SteamFinder
{
    private const string GameFolderName = "Subnautica 2";

    public static string? FindGamePath()
    {
        var steamRoot = GetSteamRoot();
        if (steamRoot is null) return null;

        // Default library
        var direct = Path.Combine(steamRoot, "steamapps", "common", GameFolderName);
        if (Directory.Exists(direct)) return direct;

        // Other libraries from libraryfolders.vdf
        var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            var content = File.ReadAllText(vdf);
            // Matches: "path"   "C:\\some\\path"
            var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
            foreach (Match m in matches)
            {
                var lib = m.Groups[1].Value.Replace(@"\\", @"\");
                var candidate = Path.Combine(lib, "steamapps", "common", GameFolderName);
                if (Directory.Exists(candidate)) return candidate;
            }
        }

        return null;
    }

    private static string? GetSteamRoot()
    {
        // 64-bit Windows, Steam is 32-bit → WOW6432Node
        if (OperatingSystem.IsWindows())
        {
            using var k64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            var p64 = k64?.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(p64) && Directory.Exists(p64)) return p64;

            using var k32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            var p32 = k32?.GetValue("InstallPath") as string;
            if (!string.IsNullOrEmpty(p32) && Directory.Exists(p32)) return p32;

            // Current user fallback
            using var ku = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            var pu = ku?.GetValue("SteamPath") as string;
            if (!string.IsNullOrEmpty(pu) && Directory.Exists(pu)) return pu;
        }
        return null;
    }
}
