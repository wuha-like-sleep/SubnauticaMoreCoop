using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoreCoopManager;

/// <summary>
/// Thin wrapper around Win32 RegisterHotKey / UnregisterHotKey. The hotkeys
/// are system-wide: pressing them while the game has focus still delivers
/// WM_HOTKEY to the registered HWND. MainForm.WndProc routes WM_HOTKEY here.
///
/// IMPORTANT: hwnd must outlive the manager. Pass MainForm.Handle and dispose
/// the manager before the form is destroyed.
/// </summary>
internal sealed class HotkeyManager : IDisposable
{
    [Flags]
    public enum Mods : uint { None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8 }

    public const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 0x1000;
    private bool _disposed;

    public HotkeyManager(IntPtr hwnd) => _hwnd = hwnd;

    /// <summary>
    /// Register a hotkey. Returns the assigned id, or -1 if Windows refused
    /// (usually means another app already holds the combo).
    /// </summary>
    public int Register(Mods modifiers, Keys key, Action handler)
    {
        var id = _nextId++;
        if (!RegisterHotKey(_hwnd, id, (uint)modifiers, (uint)key))
        {
            var err = Marshal.GetLastWin32Error();
            // 1409 = ERROR_HOTKEY_ALREADY_REGISTERED
            throw new Win32Exception(err,
                err == 1409
                    ? $"快捷键 {FormatCombo(modifiers, key)} 已被其他程序占用"
                    : $"注册快捷键 {FormatCombo(modifiers, key)} 失败 (Win32 错误 {err})");
        }
        _handlers[id] = handler;
        return id;
    }

    /// <summary>Unregister everything. Safe to call multiple times.</summary>
    public void UnregisterAll()
    {
        foreach (var id in _handlers.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }
        _handlers.Clear();
    }

    /// <summary>Call from MainForm.WndProc to dispatch WM_HOTKEY messages.</summary>
    public bool TryHandle(ref Message m)
    {
        if (m.Msg != WM_HOTKEY) return false;
        var id = m.WParam.ToInt32();
        if (_handlers.TryGetValue(id, out var handler))
        {
            try { handler(); } catch { /* swallow — never break message loop */ }
            return true;
        }
        return false;
    }

    public static string FormatCombo(Mods mods, Keys key)
    {
        var parts = new List<string>();
        if (mods.HasFlag(Mods.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(Mods.Alt))     parts.Add("Alt");
        if (mods.HasFlag(Mods.Shift))   parts.Add("Shift");
        if (mods.HasFlag(Mods.Win))     parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
    }
}

/// <summary>
/// Writes a single console command to a file the QuickCheats Lua mod polls.
/// Atomic-ish: writes whole file in one call so the Lua side never reads
/// a half-written command.
/// </summary>
internal static class CheatCommandSender
{
    public static readonly string CommandFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MoreCoop", "cheats.cmd");

    public static void Send(string command)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CommandFilePath)!);
            // Write full content in one call — File.WriteAllText handles this atomically enough
            // for our 250ms-poll Lua reader.
            File.WriteAllText(CommandFilePath, command);
        }
        catch
        {
            // Ignore — caller logs to the GUI, no need to crash on disk-full etc.
        }
    }
}
