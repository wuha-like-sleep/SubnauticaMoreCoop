using System.Runtime.InteropServices;

namespace MoreCoopManager;

/// <summary>
/// Color palette + fonts + DWM helpers for the dark "deep sea" theme.
/// Used by MainForm and the custom controls in ModernControls.cs.
/// </summary>
internal static class Theme
{
    // ──── Backgrounds ────
    public static readonly Color Background       = Color.FromArgb( 22,  27,  34);  // very dark navy
    public static readonly Color CardBackground   = Color.FromArgb( 33,  39,  48);
    public static readonly Color CardBorder       = Color.FromArgb( 58,  64,  79);
    public static readonly Color InputBackground  = Color.FromArgb( 16,  20,  26);

    // ──── Text ────
    public static readonly Color TextPrimary      = Color.FromArgb(232, 236, 244);
    public static readonly Color TextSecondary    = Color.FromArgb(155, 165, 180);
    public static readonly Color TextMuted        = Color.FromArgb(115, 124, 138);

    // ──── Status colors ────
    public static readonly Color StatusGood       = Color.FromArgb( 76, 192, 130);
    public static readonly Color StatusWarn       = Color.FromArgb(255, 169,  77);
    public static readonly Color StatusBad        = Color.FromArgb(239,  83,  80);
    public static readonly Color StatusNeutral    = TextMuted;

    // ──── Action accents ────
    public static readonly Color Primary          = Color.FromArgb( 24, 144, 255);   // bright blue (install)
    public static readonly Color PrimaryHover     = Color.FromArgb( 64, 169, 255);
    public static readonly Color Accent           = Color.FromArgb(  0, 188, 212);   // teal (launch game)
    public static readonly Color AccentHover      = Color.FromArgb( 77, 208, 225);
    public static readonly Color SuccessBadge     = Color.FromArgb( 80, 200, 120);

    // ──── Buttons (secondary) ────
    public static readonly Color ButtonBackground = Color.FromArgb( 51,  59,  74);
    public static readonly Color ButtonHover      = Color.FromArgb( 68,  77,  95);
    public static readonly Color ButtonBorder     = Color.FromArgb( 73,  82, 102);

    // ──── Fonts ────
    public static readonly Font HeaderFont = new("Microsoft YaHei UI", 10F, FontStyle.Bold);
    public static readonly Font BodyFont   = new("Microsoft YaHei UI", 10F);
    public static readonly Font ButtonFont = new("Microsoft YaHei UI", 10F, FontStyle.Bold);
    public static readonly Font MonoFont   = new("Consolas",            10F);
    public static readonly Font BigFont    = new("Microsoft YaHei UI", 14F, FontStyle.Bold);

    // ──── DWM dark title bar (Windows 10 1903+ / Windows 11) ────
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>Call once after the form's handle is created.</summary>
    public static void ApplyDarkTitleBar(IntPtr handle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)) return;
        try
        {
            int useDark = 1;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }
        catch { /* DWM not available, fall back to system title bar */ }
    }
}
