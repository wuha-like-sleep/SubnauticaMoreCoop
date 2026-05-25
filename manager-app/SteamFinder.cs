using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MoreCoopManager;

/// <summary>
/// Locates the Subnautica 2 install directory. Priority order:
///   1. User-saved override in HKCU\Software\MoreCoop\GamePath (from "Browse" button)
///   2. Steam registry InstallPath + default library
///   3. Other Steam libraries declared in steamapps/libraryfolders.vdf
///
/// If all three miss, returns null and the UI will surface a "Browse" button.
/// </summary>
internal static class SteamFinder
{
    private const string GameFolderName = "Subnautica 2";
    private const string SavedPathRegistryKey = @"Software\MoreCoop";
    private const string SavedPathValueName = "GamePath";

    /// <summary>
    /// Checks a path looks like a real Subnautica 2 install (must contain the
    /// UE5 project subfolder). Used both to validate auto-detected paths and
    /// to validate what the user picks in the folder browser.
    /// </summary>
    public static bool IsValidGamePath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && Directory.Exists(path)
        && Directory.Exists(Path.Combine(path, "Subnautica2"));

    public static string? FindGamePath()
    {
        // 1. User-saved override wins. If the user pointed us at a folder once,
        //    trust them — they may have moved their Steam library.
        var saved = LoadSavedPath();
        if (IsValidGamePath(saved)) return saved;

        // 2. Default Steam library
        var steamRoot = GetSteamRoot();
        if (steamRoot is not null)
        {
            var direct = Path.Combine(steamRoot, "steamapps", "common", GameFolderName);
            if (IsValidGamePath(direct)) return direct;

            // 3. Other libraries from libraryfolders.vdf
            var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                var content = File.ReadAllText(vdf);
                var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
                foreach (Match m in matches)
                {
                    var lib = m.Groups[1].Value.Replace(@"\\", @"\");
                    var candidate = Path.Combine(lib, "steamapps", "common", GameFolderName);
                    if (IsValidGamePath(candidate)) return candidate;
                }
            }
        }

        return null;
    }

    public static string? LoadSavedPath()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SavedPathRegistryKey);
            return k?.GetValue(SavedPathValueName) as string;
        }
        catch { return null; }
    }

    public static void SaveUserPath(string path)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var k = Registry.CurrentUser.CreateSubKey(SavedPathRegistryKey);
        k?.SetValue(SavedPathValueName, path, RegistryValueKind.String);
    }

    public static void ClearSavedPath()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SavedPathRegistryKey, writable: true);
            k?.DeleteValue(SavedPathValueName, throwOnMissingValue: false);
        }
        catch { /* ignore */ }
    }

    private static string? GetSteamRoot()
    {
        if (!OperatingSystem.IsWindows()) return null;

        using var k64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
        var p64 = k64?.GetValue("InstallPath") as string;
        if (!string.IsNullOrEmpty(p64) && Directory.Exists(p64)) return p64;

        using var k32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
        var p32 = k32?.GetValue("InstallPath") as string;
        if (!string.IsNullOrEmpty(p32) && Directory.Exists(p32)) return p32;

        using var ku = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
        var pu = ku?.GetValue("SteamPath") as string;
        return !string.IsNullOrEmpty(pu) && Directory.Exists(pu) ? pu : null;
    }
}
