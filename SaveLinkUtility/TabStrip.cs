using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SaveLinkUtility;

/// <summary>A flat tab bar: text tabs with an accent underline under the selected one, and an optional key hint.</summary>
public sealed class TabStrip : Control
{
    public static readonly Color Accent = Color.FromArgb(0, 95, 184);

    private readonly List<(string Text, string? Hint)> _tabs = new();
    private int _selected;
    private int _hover = -1;

    public event EventHandler? SelectedIndexChanged;

    public TabStrip()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        AccessibleRole = AccessibleRole.PageTabList;
        TabStop = false;
    }

    /// <summary>Left edge of the first tab's text, so it can line up with the page content below.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TextIndent { get; set; }

    public void AddTab(string text, string? hint = null)
    {
        _tabs.Add((text, hint));
        Invalidate();
    }

    public int Count => _tabs.Count;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            if (value < 0 || value >= _tabs.Count || value == _selected) return;
            _selected = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int Gap => LogicalToDeviceUnits(28);
    private int HintGap => LogicalToDeviceUnits(6);

    private Font HintFont => _hintFont ??= new Font(Font.FontFamily, Font.Size * 0.85f);
    private Font? _hintFont;

    protected override void OnFontChanged(EventArgs e)
    {
        _hintFont?.Dispose();
        _hintFont = null;
        base.OnFontChanged(e);
    }

    /// <summary>Text area of each tab (the underline spans this); the clickable area extends Gap/2 either side.</summary>
    private List<(Rectangle Text, int HintWidth)> TabLayout()
    {
        var result = new List<(Rectangle, int)>();
        int x = TextIndent;
        foreach (var (text, hint) in _tabs)
        {
            var size = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding);
            int hintW = hint == null ? 0 : HintGap + TextRenderer.MeasureText(hint, HintFont, Size.Empty, TextFormatFlags.NoPadding).Width;
            result.Add((new Rectangle(x, 0, size.Width, Height), hintW));
            x += size.Width + hintW + Gap;
        }
        return result;
    }

    private int HitTest(Point p)
    {
        var tabs = TabLayout();
        for (int i = 0; i < tabs.Count; i++)
        {
            var r = tabs[i].Text;
            if (p.X >= r.Left - Gap / 2 && p.X < r.Right + tabs[i].HintWidth + Gap / 2) return i;
        }
        return -1;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var tabs = TabLayout();
        int w = tabs.Count == 0 ? 0 : tabs[^1].Text.Right + tabs[^1].HintWidth + TextIndent;
        return new Size(w, Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        using (var line = new Pen(Color.FromArgb(218, 218, 218)))
            g.DrawLine(line, 0, Height - 1, Width, Height - 1);

        var tabs = TabLayout();
        int bar = LogicalToDeviceUnits(3);
        for (int i = 0; i < tabs.Count; i++)
        {
            var (text, hint) = _tabs[i];
            var r = tabs[i].Text;
            var color = i == _selected ? Color.FromArgb(26, 26, 26)
                      : i == _hover ? Color.FromArgb(64, 64, 64)
                      : Color.FromArgb(110, 110, 110);

            var textRect = new Rectangle(r.X, 0, r.Width, Height - bar);
            TextRenderer.DrawText(g, text, Font, textRect, color,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            if (hint != null)
            {
                var hintRect = new Rectangle(r.Right + HintGap, 0, tabs[i].HintWidth, Height - bar);
                TextRenderer.DrawText(g, hint, HintFont, hintRect, Color.FromArgb(140, 140, 140),
                    TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            if (i == _selected)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Accent);
                using var path = RoundedTop(new Rectangle(r.X, Height - bar, r.Width + tabs[i].HintWidth, bar), bar);
                g.FillPath(brush, path);
                g.SmoothingMode = SmoothingMode.None;
            }
        }
    }

    private static GraphicsPath RoundedTop(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Min(radius * 2, r.Width);
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
        path.CloseFigure();
        return path;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var hit = HitTest(e.Location);
        if (hit != _hover) { _hover = hit; Invalidate(); }
        Cursor = hit >= 0 && hit != _selected ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var hit = HitTest(e.Location);
            if (hit >= 0) SelectedIndex = hit;
        }
        base.OnMouseDown(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hintFont?.Dispose();
        base.Dispose(disposing);
    }
}
