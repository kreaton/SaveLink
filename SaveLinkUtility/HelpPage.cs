using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SaveLinkUtility;

/// <summary>
/// The Help tab: formatted text (headings, numbered steps, bullets, key chips) rendered as RTF in a
/// borderless, read-only rich text box. In the text, **bold** and `code` are the only markup.
/// </summary>
public sealed class HelpPage : Panel
{
    private readonly RichTextBox _text = new()
    {
        BorderStyle = BorderStyle.None,
        ReadOnly = true,
        BackColor = SystemColors.Window,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        DetectUrls = false,
        ShortcutsEnabled = true,
        Dock = DockStyle.Fill,
        TabStop = false,
    };

    public HelpPage()
    {
        BackColor = SystemColors.Window;
        Padding = new Padding(LogicalToDeviceUnits(16), LogicalToDeviceUnits(12), LogicalToDeviceUnits(4), 0);
        Controls.Add(_text);
        _text.Rtf = BuildRtf();

        // Read-only text still shows a blinking caret; hide it so the page reads like a document.
        _text.GotFocus += (_, _) => HideCaret(_text.Handle);
        _text.MouseUp += (_, _) => HideCaret(_text.Handle);
        _text.SelectionChanged += (_, _) => HideCaret(_text.Handle);
    }

    /// <summary>Called when the tab is shown: back to the top, keyboard scrolling works.</summary>
    public void Activate()
    {
        _text.Select(0, 0);
        _text.ScrollToCaret();
        // Deferred: hiding the other page moves focus around first, and that must not win.
        BeginInvoke(() => _text.Focus());
    }

    // ------------------------------------------------------------------ content

    private static string BuildRtf()
    {
        var r = new RtfWriter();

        r.Title("SaveLinkUtility");
        r.Lead("Turns a web address into a desktop shortcut that carries the site's own icon.");

        r.Heading("Getting started");
        r.Step(1, "Paste a link into **Website URL**, or drag a link, selected text or a `.url` file onto the window. "
                + "If the clipboard holds a link when the program starts, it is filled in for you.");
        r.Step(2, "The page title and icon are looked up automatically. Change the name if you like.");
        r.Step(3, "Click **Create Shortcut**, or press `Enter`.");

        r.Heading("Icon");
        r.Text("The icon is taken from the website: its apple-touch-icon, a declared icon, or `favicon.ico`. "
             + "The links under the preview change it:");
        r.Bullet("**Choose file…** uses your own image (png, jpg, gif, bmp or ico). SVG is not supported.");
        r.Bullet("**Generate** makes a simple letter tile from the site name.");
        r.Bullet("**Reset** goes back to the icon from the website.");
        r.Bullet("**Save as .ico…** saves the current icon as a multi-size `.ico` file wherever you choose.");

        r.Heading("Opening the shortcut");
        r.Bullet("**Default browser** creates a `.url` file. It opens in whichever browser is your default at the time you click it.");
        r.Bullet("**A specific browser** creates a `.lnk` that always opens in that browser, even if you change the default later.");
        r.Bullet("**Open in its own app window** starts the site without tabs or address bar, with its own taskbar button. "
               + "This only works with Chromium-based browsers: Edge, Chrome, Brave and Vivaldi. Firefox has no such mode.");

        r.Heading("Saving");
        r.Bullet("**Save location** is the Desktop by default. Use **Browse…** to pick another folder.");
        r.Bullet("**Save icon locally** stores the icon in `%LOCALAPPDATA%\\SaveLinkUtility\\Icons`. "
               + "The shortcut points at that file, so keep the folder.");
        r.Bullet("**Create shortcut** can be unticked to only save the icon.");
        r.Text("After creating, **Show in folder** reveals the new file in Explorer.");

        r.Heading("Keyboard and command line");
        r.Bullet("`F1` opens this page, `Esc` goes back, `Ctrl+Tab` switches tabs.");
        r.Bullet("`SaveLinkUtility.exe https://example.com` starts with that address filled in.");

        var version = Application.ProductVersion.Split('+')[0];
        r.Footer($"Version {version}");

        return r.ToString();
    }

    // ------------------------------------------------------------------ RTF

    private sealed class RtfWriter
    {
        // Colour table indexes: 1 text, 2 accent, 3 muted, 4 code background.
        private readonly StringBuilder _sb = new(
            @"{\rtf1\ansi\deff0" +
            @"{\fonttbl{\f0\fnil Segoe UI;}{\f1\fnil Segoe UI Semibold;}{\f2\fmodern Consolas;}}" +
            @"{\colortbl;\red32\green32\blue32;" +
            $@"\red{TabStrip.Accent.R}\green{TabStrip.Accent.G}\blue{TabStrip.Accent.B};" +
            @"\red110\green110\blue110;\red236\green236\blue236;}" +
            @"\viewkind4\uc1" + "\n");

        public void Title(string text) => Para(Inline(text), size: 30, color: 1, font: 1, after: 20);
        public void Lead(string text) => Para(Inline(text), size: 20, color: 3, after: 80);
        public void Heading(string text) => Para(Inline(text), size: 23, color: 2, font: 1, before: 220, after: 60);
        public void Text(string text) => Para(Inline(text), size: 19, color: 1, after: 80);
        public void Footer(string text) => Para(Inline(text), size: 17, color: 3, before: 280, after: 240);

        public void Step(int n, string text) =>
            Para($@"{n}.\tab {Inline(text)}", size: 19, color: 1, after: 60, indent: 340, firstLine: -340);

        public void Bullet(string text) =>
            Para($@"{{\cf2\bullet}}\tab {Inline(text)}", size: 19, color: 1, after: 60, indent: 340, firstLine: -220);

        private void Para(string body, int size, int color, int font = 0, int before = 0, int after = 0, int indent = 0, int firstLine = 0)
        {
            var tabs = indent > 0 ? $@"\tx{indent}" : "";
            _sb.Append($@"\pard\sl276\slmult1\sb{before}\sa{after}\li{indent}\fi{firstLine}{tabs}\f{font}\fs{size}\cf{color} {body}\par")
               .Append('\n');
        }

        /// <summary>Escapes text for RTF and turns **bold** into semibold and `code` into a shaded monospace chip.</summary>
        private static string Inline(string text)
        {
            var sb = new StringBuilder();
            bool bold = false, code = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    bold = !bold;
                    sb.Append(bold ? @"{\f1 " : "}");
                    i++;
                }
                else if (c == '`')
                {
                    code = !code;
                    sb.Append(code ? @"{\f2\fs17\highlight4\~" : @"\~}");
                }
                else if (c is '\\' or '{' or '}') sb.Append('\\').Append(c);
                else if (c > 127) sb.Append($@"\u{(short)c}?");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public override string ToString() => _sb + "}";
    }

    [DllImport("user32.dll")]
    private static extern bool HideCaret(IntPtr hWnd);
}
