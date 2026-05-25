using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MoreCoopManager;

/// <summary>
/// All filesystem operations needed to install, configure, and remove the
/// MoreCoop mod. Reads the mod payload (main.lua, settings.json, LICENSE)
/// from this assembly's embedded resources, so the .exe is fully self-contained.
/// </summary>
internal sealed class ModInstaller
{
    public string GamePath { get; }
    public string UE4SSPath => Path.Combine(GamePath, "Subnautica2", "Binaries", "Win64", "ue4ss");
    public string ModPath => Path.Combine(UE4SSPath, "Mods", "MoreCoop");
    public string ModsTxt => Path.Combine(UE4SSPath, "Mods", "mods.txt");
    public string SettingsJson => Path.Combine(ModPath, "config", "settings.json");

    public bool UE4SSInstalled => Directory.Exists(UE4SSPath);
    public bool ModInstalled => File.Exists(Path.Combine(ModPath, "Scripts", "main.lua"));

    public ModInstaller(string gamePath) => GamePath = gamePath;

    public int ReadCurrentMaxPlayers()
    {
        if (!File.Exists(SettingsJson)) return 8;
        var content = File.ReadAllText(SettingsJson);
        var m = Regex.Match(content, @"""MaxPlayers""\s*:\s*(\d+)");
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : 8;
    }

    public void Install(int maxPlayers)
    {
        if (!UE4SSInstalled)
            throw new InvalidOperationException("UE4SS 未安装。请先从 Nexus 下载安装 UE4SS。");

        Directory.CreateDirectory(ModPath);
        Directory.CreateDirectory(Path.Combine(ModPath, "Scripts"));
        Directory.CreateDirectory(Path.Combine(ModPath, "config"));

        // Embedded mod payload → disk
        File.WriteAllText(Path.Combine(ModPath, "enabled.txt"), "", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(ModPath, "Scripts", "main.lua"),
            ReadResource("Resources.main.lua"), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(ModPath, "LICENSE"),
            ReadResource("Resources.LICENSE"), new UTF8Encoding(false));

        // settings.json with chosen MaxPlayers
        var defaultSettings = ReadResource("Resources.settings.json");
        var settings = Regex.Replace(defaultSettings,
            @"""MaxPlayers""\s*:\s*\d+", $"\"MaxPlayers\": {maxPlayers}");
        File.WriteAllText(SettingsJson, settings, new UTF8Encoding(false));

        // Register in mods.txt (idempotent)
        RegisterInModsTxt();
    }

    public void Uninstall()
    {
        if (Directory.Exists(ModPath))
            Directory.Delete(ModPath, recursive: true);

        if (File.Exists(ModsTxt))
        {
            var kept = File.ReadAllLines(ModsTxt)
                .Where(l => !l.TrimStart().StartsWith("MoreCoop", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            File.WriteAllLines(ModsTxt, kept, new UTF8Encoding(false));
        }
    }

    /// <summary>
    /// Update the player cap in settings.json without re-extracting the mod.
    /// UE4SS hot-reloads the config so changes take effect without restarting the game.
    /// </summary>
    public void UpdateMaxPlayers(int newValue)
    {
        if (!File.Exists(SettingsJson))
            throw new InvalidOperationException("settings.json 不存在，请先安装 mod。");

        var content = File.ReadAllText(SettingsJson);
        content = Regex.Replace(content,
            @"""MaxPlayers""\s*:\s*\d+", $"\"MaxPlayers\": {newValue}");
        File.WriteAllText(SettingsJson, content, new UTF8Encoding(false));
    }

    private void RegisterInModsTxt()
    {
        if (File.Exists(ModsTxt))
        {
            var lines = File.ReadAllLines(ModsTxt);
            if (lines.Any(l => l.TrimStart().StartsWith("MoreCoop", StringComparison.OrdinalIgnoreCase)))
                return;

            File.AppendAllText(ModsTxt, Environment.NewLine + "MoreCoop : 1" + Environment.NewLine,
                new UTF8Encoding(false));
        }
        else
        {
            File.WriteAllText(ModsTxt, "MoreCoop : 1" + Environment.NewLine, new UTF8Encoding(false));
        }
    }

    private static string ReadResource(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded resource not found: {suffix}");

        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
