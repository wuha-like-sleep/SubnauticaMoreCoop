using System.Drawing.Drawing2D;

namespace MoreCoopManager;

/// <summary>
/// Bordered card panel with a header label. Replaces WinForms' GroupBox
/// which looks dated and doesn't theme well.
///
/// The header is drawn in the top-left; child controls live in the area
/// below the header (controls add at y >= HeaderHeight + InnerPadding).
/// </summary>
internal sealed class CardPanel : Panel
{
    public const int HeaderHeight = 36;
    public const int InnerPadding = 16;

    private string _cardTitle = "";
    public string CardTitle
    {
        get => _cardTitle;
        set { _cardTitle = value; Invalidate(); }
    }

    public CardPanel()
    {
        BackColor = Theme.CardBackground;
        ForeColor = Theme.TextPrimary;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Subtle 1px border around the whole card
        using var borderPen = new Pen(Theme.CardBorder, 1);
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        // Divider under header
        using var dividerPen = new Pen(Theme.CardBorder, 1);
        g.DrawLine(dividerPen, 1, HeaderHeight, Width - 2, HeaderHeight);

        // Header text
        if (!string.IsNullOrEmpty(_cardTitle))
        {
            using var brush = new SolidBrush(Theme.TextPrimary);
            g.DrawString(_cardTitle, Theme.HeaderFont, brush,
                InnerPadding - 4, 9);
        }
    }
}

/// <summary>
/// Flat button with hover and pressed states, matching the dark theme.
/// Three variants via the static factories: Primary (filled accent),
/// Secondary (dark gray), and Danger (red).
/// </summary>
internal sealed class ModernButton : Button
{
    private readonly Color _baseColor;
    private readonly Color _hoverColor;
    private readonly Color _pressedColor;
    private readonly Color _disabledColor = Color.FromArgb(46, 51, 62);
    private readonly Color _disabledFg    = Color.FromArgb(95, 102, 117);

    public ModernButton(Color baseColor, Color textColor, bool bold = true)
    {
        _baseColor    = baseColor;
        _hoverColor   = ControlPaint.Light(baseColor, 0.15f);
        _pressedColor = ControlPaint.Dark(baseColor, 0.15f);

        BackColor = baseColor;
        ForeColor = textColor;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize        = 0;
        FlatAppearance.MouseOverBackColor = _hoverColor;
        FlatAppearance.MouseDownBackColor = _pressedColor;
        Font = bold ? Theme.ButtonFont : Theme.BodyFont;
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
        Padding = new Padding(4, 0, 4, 0);

        EnabledChanged += (_, _) =>
        {
            BackColor = Enabled ? _baseColor : _disabledColor;
            ForeColor = Enabled ? textColor   : _disabledFg;
        };
    }

    public static ModernButton Primary(string text)
        => new(Theme.Primary, Color.White) { Text = text };

    public static ModernButton Accent(string text)
        => new(Theme.Accent, Color.White) { Text = text };

    public static ModernButton Secondary(string text)
        => new(Theme.ButtonBackground, Theme.TextPrimary) { Text = text };

    public static ModernButton Success(string text)
        => new(Color.FromArgb(67, 160, 71), Color.White) { Text = text };
}

/// <summary>
/// A status row: colored dot + label + value. Used in the Status card.
/// The dot color reflects state (good = green, neutral = gray, bad = red).
/// </summary>
internal sealed class StatusRow : Control
{
    private const int DotDiameter = 10;
    private const int DotMargin   = 6;
    private const int LabelWidth  = 80;

    private Color _dotColor = Theme.StatusNeutral;
    private string _label   = "";
    private string _value   = "";

    public string Label
    {
        get => _label;
        set { _label = value; Invalidate(); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    public Color DotColor
    {
        get => _dotColor;
        set { _dotColor = value; Invalidate(); }
    }

    public StatusRow()
    {
        Font = Theme.BodyFont;
        ForeColor = Theme.TextPrimary;
        BackColor = Theme.CardBackground;
        Height = 26;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var dotY = (Height - DotDiameter) / 2;
        using (var brush = new SolidBrush(_dotColor))
            g.FillEllipse(brush, 0, dotY, DotDiameter, DotDiameter);

        var labelX = DotDiameter + DotMargin;
        using (var brush = new SolidBrush(Theme.TextSecondary))
            g.DrawString(_label, Font, brush, labelX, (Height - Font.Height) / 2);

        var valueX = labelX + LabelWidth;
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            // Truncate with ellipsis if too wide
            var avail = Width - valueX - 4;
            var text = TruncateToWidth(g, _value, Font, avail);
            g.DrawString(text, Font, brush, valueX, (Height - Font.Height) / 2);
        }
    }

    private static string TruncateToWidth(Graphics g, string text, Font font, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (g.MeasureString(text, font).Width <= maxWidth) return text;
        // Binary search-ish for the longest fitting prefix
        var keep = text.Length;
        while (keep > 0 && g.MeasureString(text[..keep] + "…", font).Width > maxWidth) keep--;
        return text[..keep] + "…";
    }
}
