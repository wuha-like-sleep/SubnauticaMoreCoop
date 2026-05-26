namespace MoreCoopManager;

/// <summary>
/// Append-only log file at %APPDATA%\MoreCoop\manager.log. Mirrors everything
/// the user sees in the GUI log box, plus stays around after the app closes
/// so users can share it when reporting bugs.
///
/// Caps the file at ~1 MB by trimming to the last 500 KB when it grows past
/// the cap, so a long-running install loop doesn't fill the disk.
/// </summary>
internal static class FileLog
{
    private const long MaxBytes = 1_000_000;
    private const long TrimToBytes = 500_000;

    private static readonly object _lock = new();
    private static string? _path;

    public static string LogPath => _path ?? "(未初始化)";

    public static void Init()
    {
        if (_path is not null) return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MoreCoop");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "manager.log");

            // Session header
            File.AppendAllText(_path,
                $"{Environment.NewLine}========== MoreCoop Manager 启动 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========" + Environment.NewLine);
        }
        catch
        {
            // No-op — if we can't write to AppData something is very wrong but we
            // don't want to crash the GUI over a logging failure.
            _path = null;
        }
    }

    public static void Append(string message)
    {
        if (_path is null) return;
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                TrimIfTooLarge();
            }
            catch { /* swallow logging failures */ }
        }
    }

    private static void TrimIfTooLarge()
    {
        try
        {
            var info = new FileInfo(_path!);
            if (info.Length <= MaxBytes) return;

            // Keep the tail of the file (last TrimToBytes worth of content)
            var allBytes = File.ReadAllBytes(_path!);
            var keepFrom = allBytes.Length - (int)TrimToBytes;
            var kept = new byte[(int)TrimToBytes];
            Array.Copy(allBytes, keepFrom, kept, 0, (int)TrimToBytes);

            var header = System.Text.Encoding.UTF8.GetBytes(
                $"--- (trimmed older entries to keep file under 1 MB at {DateTime.Now:yyyy-MM-dd HH:mm:ss}) ---{Environment.NewLine}");
            using var fs = File.Create(_path!);
            fs.Write(header, 0, header.Length);
            fs.Write(kept, 0, kept.Length);
        }
        catch { /* swallow */ }
    }
}
