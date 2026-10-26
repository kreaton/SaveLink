using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SaveLinkUtility;

/// <summary>
/// The Help tab: Help.md (embedded in the exe) converted to RTF and shown in a borderless, read-only
/// rich text box. Only a small Markdown subset is understood - see <see cref="MarkdownToRtf"/>.
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
        using var stream = typeof(HelpPage).Assembly.GetManifestResourceStream("SaveLinkUtility.Help.md");
        var markdown = stream == null ? "# Help\n\nThe help text is missing from this build." : new StreamReader(stream).ReadToEnd();

        var r = new RtfWriter();
        MarkdownToRtf(markdown, r);
        r.Footer($"Version {Application.ProductVersion.Split('+')[0]}");
        return r.ToString();
    }

    /// <summary>
    /// Converts the Markdown subset Help.md uses: "# title" (the paragraph after it is the lead),
    /// "## heading", "1. step", "- bullet", plain paragraphs, and **bold** / `code` inline.
    /// Lines that follow on without a blank line are joined, so long items can be wrapped in the file.
    /// </summary>
    private static void MarkdownToRtf(string markdown, RtfWriter r)
    {
        // Each block: what kind it is, its number (for steps), and its text so far.
        var blocks = new List<(char Kind, int Number, string Text)>();
        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) { blocks.Add(('\0', 0, "")); continue; }

            var dot = line.IndexOf(". ", StringComparison.Ordinal);
            if (line.StartsWith("## ")) blocks.Add(('h', 0, line[3..]));
            else if (line.StartsWith("# ")) blocks.Add(('t', 0, line[2..]));
            else if (line.StartsWith("- ") || line.StartsWith("* ")) blocks.Add(('b', 0, line[2..]));
            else if (dot > 0 && int.TryParse(line[..dot], out var n)) blocks.Add(('n', n, line[(dot + 2)..]));
            else if (blocks.Count > 0 && blocks[^1].Kind is 'p' or 'b' or 'n')
                blocks[^1] = blocks[^1] with { Text = blocks[^1].Text + " " + line };
            else blocks.Add(('p', 0, line));
        }

        char previous = '\0';
        foreach (var (kind, number, text) in blocks)
        {
            switch (kind)
            {
                case 't': r.Title(text); break;
                case 'h': r.Heading(text); break;
                case 'b': r.Bullet(text); break;
                case 'n': r.Step(number, text); break;
                case 'p' when previous == 't': r.Lead(text); break;
                case 'p': r.Text(text); break;
                default: continue; // blank line: keeps 'previous' pointing at the last real block
            }
            previous = kind;
        }
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
