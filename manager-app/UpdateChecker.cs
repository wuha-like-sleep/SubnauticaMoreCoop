using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MoreCoopManager;

/// <summary>
/// Talks to the GitHub releases API to find out if the user is running an
/// outdated MoreCoopManager, and can fetch the mod's main.lua + settings.json
/// schema from the latest tag without making the user re-download the whole
/// 75 MB exe.
///
/// Background-friendly: every public method is async and swallows network
/// failures by returning null/false (we never throw the GUI's app loop).
/// </summary>
internal static class UpdateChecker
{
    private const string Owner = "wuha-like-sleep";
    private const string Repo  = "SubnauticaMoreCoop";
    private const string ApiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

    /// <summary>Hard-coded version this build represents. Bumped per release.</summary>
    public const string CurrentVersion = "1.7.0";

    /// <summary>Pinned in code so the About dialog can always show it.</summary>
    public const string LatestReleasePageUrl = $"https://github.com/{Owner}/{Repo}/releases/latest";

    public sealed record UpdateInfo(
        string LatestVersion,
        string ReleaseUrl,
        string MainLuaRawUrl,
        bool IsNewer);

    /// <summary>
    /// Hit GitHub's API, parse the latest release. Returns null on any error
    /// (no network, rate-limited, parse failure). The caller treats null as
    /// "couldn't check" — never blocks the UI.
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.Add("User-Agent", $"MoreCoopManager/{CurrentVersion}");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var json = await client.GetStringAsync(ApiUrl, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = root.TryGetProperty("html_url", out var hu) ? hu.GetString() ?? "" : LatestReleasePageUrl;

            var cleanTag = tagName.TrimStart('v', 'V');
            var isNewer  = CompareVersions(cleanTag, CurrentVersion) > 0;

            // Raw URL for main.lua at this tag
            var luaUrl = $"https://raw.githubusercontent.com/{Owner}/{Repo}/{tagName}/MoreCoop/Scripts/main.lua";

            return new UpdateInfo(cleanTag, htmlUrl, luaUrl, isNewer);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Download a single text file (main.lua) from raw.githubusercontent.com
    /// and atomically replace the local copy. UE4SS hot-reloads the script.
    /// </summary>
    public static async Task<(bool ok, string? error)> DownloadAndReplaceAsync(
        string rawUrl, string localPath, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Add("User-Agent", $"MoreCoopManager/{CurrentVersion}");
            var content = await client.GetStringAsync(rawUrl, ct);

            if (string.IsNullOrWhiteSpace(content) || !content.Contains("MoreCoop"))
                return (false, "下载的文件内容看起来不对 (不像 MoreCoop main.lua)");

            // Write to temp then atomic move
            var tmp = localPath + ".tmp";
            await File.WriteAllTextAsync(tmp, content, new UTF8Encoding(false), ct);
            File.Move(tmp, localPath, overwrite: true);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static int CompareVersions(string a, string b)
    {
        try
        {
            // Strip any pre-release suffix (-pre, -beta etc) for plain numeric compare
            var clean = (string s) =>
            {
                var dash = s.IndexOf('-');
                return dash > 0 ? s[..dash] : s;
            };
            var va = new Version(clean(a));
            var vb = new Version(clean(b));
            return va.CompareTo(vb);
        }
        catch
        {
            return string.Compare(a, b, StringComparison.Ordinal);
        }
    }
}
