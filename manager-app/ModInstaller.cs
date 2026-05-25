using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MoreCoopManager;

/// <summary>
/// All filesystem operations needed to install, configure, and remove the
/// MoreCoop mod and (if needed) UE4SS itself. Reads the mod payload and
/// the bundled UE4SS_SN2.zip from this assembly's embedded resources, so
/// the .exe is a fully self-contained installer — no separate downloads.
/// </summary>
internal sealed class ModInstaller
{
    /// <summary>Marker file written into ue4ss/ so uninstall knows whether we installed UE4SS.</summary>
    private const string UE4SSMarkerFile = "installed-by-morecoop.txt";

    public string GamePath { get; }
    public string Win64Path => Path.Combine(GamePath, "Subnautica2", "Binaries", "Win64");
    public string UE4SSPath => Path.Combine(Win64Path, "ue4ss");
    public string ProxyDll => Path.Combine(Win64Path, "dwmapi.dll");
    public string ModPath => Path.Combine(UE4SSPath, "Mods", "MoreCoop");
    public string ModsTxt => Path.Combine(UE4SSPath, "Mods", "mods.txt");
    public string SettingsJson => Path.Combine(ModPath, "config", "settings.json");

    public bool UE4SSInstalled => Directory.Exists(UE4SSPath) && File.Exists(ProxyDll);
    public bool UE4SSInstalledByUs => File.Exists(Path.Combine(UE4SSPath, UE4SSMarkerFile));
    public bool ModInstalled => File.Exists(Path.Combine(ModPath, "Scripts", "main.lua"));

    public ModInstaller(string gamePath) => GamePath = gamePath;

    public int ReadCurrentMaxPlayers()
    {
        if (!File.Exists(SettingsJson)) return 8;
        var content = File.ReadAllText(SettingsJson);
        var m = Regex.Match(content, @"""MaxPlayers""\s*:\s*(\d+)");
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : 8;
    }

    /// <summary>
    /// One-shot install: extracts UE4SS if missing, then writes the MoreCoop mod
    /// payload, then registers in mods.txt. Caller hands in the desired MaxPlayers.
    /// Reports each step via the optional progress callback.
    /// </summary>
    public void Install(int maxPlayers, Action<string>? progress = null)
    {
        if (!Directory.Exists(Win64Path))
            throw new InvalidOperationException(
                $"游戏目录看起来不对, 找不到 Subnautica2\\Binaries\\Win64 子目录:\r\n{Win64Path}");

        // 1) Install UE4SS if missing (extract bundled zip → Win64\)
        if (!UE4SSInstalled)
        {
            progress?.Invoke("UE4SS 未检测到, 正在从内嵌资源解压 (~7 MB)...");
            InstallUE4SS();
            progress?.Invoke("UE4SS 安装完成");
        }
        else
        {
            var origin = UE4SSInstalledByUs ? "本程序之前装的" : "你自己装的";
            progress?.Invoke($"UE4SS 已存在 ({origin})");
        }

        // 2) Write mod payload
        progress?.Invoke("正在写入 MoreCoop mod 文件...");
        Directory.CreateDirectory(ModPath);
        Directory.CreateDirectory(Path.Combine(ModPath, "Scripts"));
        Directory.CreateDirectory(Path.Combine(ModPath, "config"));

        File.WriteAllText(Path.Combine(ModPath, "enabled.txt"), "", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(ModPath, "Scripts", "main.lua"),
            ReadStringResource("Resources.main.lua"), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(ModPath, "LICENSE"),
            ReadStringResource("Resources.LICENSE"), new UTF8Encoding(false));

        var defaultSettings = ReadStringResource("Resources.settings.json");
        var settings = Regex.Replace(defaultSettings,
            @"""MaxPlayers""\s*:\s*\d+", $"\"MaxPlayers\": {maxPlayers}");
        File.WriteAllText(SettingsJson, settings, new UTF8Encoding(false));

        // 3) Register in mods.txt
        progress?.Invoke("正在注册到 UE4SS mods.txt...");
        RegisterInModsTxt();
    }

    /// <summary>
    /// Removes the MoreCoop mod files and mods.txt entry. If <paramref name="alsoRemoveUE4SS"/>
    /// is true AND UE4SS was installed by this tool, also removes UE4SS itself.
    /// </summary>
    public void Uninstall(bool alsoRemoveUE4SS, Action<string>? progress = null)
    {
        if (Directory.Exists(ModPath))
        {
            progress?.Invoke($"正在删除 MoreCoop 文件: {ModPath}");
            Directory.Delete(ModPath, recursive: true);
        }

        if (File.Exists(ModsTxt))
        {
            progress?.Invoke("正在从 mods.txt 移除 MoreCoop 条目");
            var kept = File.ReadAllLines(ModsTxt)
                .Where(l => !l.TrimStart().StartsWith("MoreCoop", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            File.WriteAllLines(ModsTxt, kept, new UTF8Encoding(false));
        }

        if (alsoRemoveUE4SS && UE4SSInstalledByUs)
        {
            progress?.Invoke("正在卸载 UE4SS (本程序之前安装的)...");
            UninstallUE4SS();
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

    // ----------------------------------------------------------------
    // UE4SS install/uninstall (private)
    // ----------------------------------------------------------------

    private void InstallUE4SS()
    {
        // Extract bundled zip to Win64\
        using var stream = LoadResourceStream("Resources.UE4SS_SN2.zip");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var dest = Path.Combine(Win64Path, entry.FullName);

            if (string.IsNullOrEmpty(entry.Name))
            {
                // Directory entry
                Directory.CreateDirectory(dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            // Don't overwrite existing files — respect user's own UE4SS install
            // if somehow Win64Path-level files (e.g. their dwmapi.dll) already exist
            if (File.Exists(dest)) continue;

            using var entryStream = entry.Open();
            using var fileStream = File.Create(dest);
            entryStream.CopyTo(fileStream);
        }

        // Marker so we know we installed UE4SS (and can offer to remove it on uninstall)
        File.WriteAllText(
            Path.Combine(UE4SSPath, UE4SSMarkerFile),
            $"Installed by MoreCoopManager on {DateTime.UtcNow:O}\r\n" +
            "If you remove this file, the manager will treat UE4SS as user-installed and won't auto-uninstall it.\r\n",
            new UTF8Encoding(false));
    }

    private void UninstallUE4SS()
    {
        if (Directory.Exists(UE4SSPath))
            Directory.Delete(UE4SSPath, recursive: true);

        if (File.Exists(ProxyDll))
            File.Delete(ProxyDll);
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

    // ----------------------------------------------------------------
    // Embedded resource helpers
    // ----------------------------------------------------------------

    private static string ReadStringResource(string suffix)
    {
        using var stream = LoadResourceStream(suffix);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static Stream LoadResourceStream(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded resource not found: {suffix}");
        return asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Failed to open resource stream: {name}");
    }
}
