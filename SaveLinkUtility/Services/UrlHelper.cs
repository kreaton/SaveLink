using System.Text;
using System.Windows.Forms;

namespace SaveLinkUtility.Services;

public static class UrlHelper
{
    /// <summary>
    /// Turns whatever the user pasted ("example.com", "&lt;https://x.com&gt;", multi-line text...)
    /// into an absolute http(s) URI. Returns false if it doesn't look like a web address.
    /// </summary>
    public static bool TryNormalize(string? input, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                     .FirstOrDefault()?.Trim().Trim('<', '>', '"', '\'');
        if (string.IsNullOrEmpty(s)) return false;

        if (!s.Contains("://"))
        {
            if (s.Contains(' ') || !s.Contains('.')) return false;
            s = "https://" + s;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var u)) return false;
        if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(u.Host)) return false;

        uri = u;
        return true;
    }

    /// <summary>Quick check used while dragging, so we can show the right cursor.</summary>
    public static bool HasUrlData(System.Windows.Forms.IDataObject data)
    {
        try
        {
            return data.GetDataPresent("UniformResourceLocatorW")
                || data.GetDataPresent(DataFormats.UnicodeText)
                || data.GetDataPresent(DataFormats.Text)
                || (data.GetDataPresent(DataFormats.FileDrop)
                    && data.GetData(DataFormats.FileDrop) is string[] files
                    && files.Any(f => f.EndsWith(".url", StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Pulls a URL out of a drag/drop payload: browser link, .url file, or plain text.</summary>
    public static string? ExtractUrl(System.Windows.Forms.IDataObject data)
    {
        try
        {
            // Dragging a link or the address-bar icon from a browser.
            if (data.GetDataPresent("UniformResourceLocatorW") &&
                data.GetData("UniformResourceLocatorW") is MemoryStream ms)
            {
                var s = Encoding.Unicode.GetString(ms.ToArray()).TrimEnd('\0').Trim();
                if (s.Length > 0) return s;
            }

            // Dragging an existing .url file from Explorer.
            if (data.GetDataPresent(DataFormats.FileDrop) &&
                data.GetData(DataFormats.FileDrop) is string[] files)
            {
                foreach (var f in files.Where(f => f.EndsWith(".url", StringComparison.OrdinalIgnoreCase)))
                {
                    var u = ReadUrlFile(f);
                    if (u != null) return u;
                }
            }

            // Selected text.
            if (data.GetDataPresent(DataFormats.UnicodeText) &&
                data.GetData(DataFormats.UnicodeText) is string t)
                return t;
            if (data.GetDataPresent(DataFormats.Text) &&
                data.GetData(DataFormats.Text) is string t2)
                return t2;
        }
        catch
        {
            // Another app may hold the data object; treat as "nothing usable".
        }
        return null;
    }

    private static string? ReadUrlFile(string path)
    {
        try
        {
            foreach (var line in File.ReadLines(path))
                if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                    return line[4..].Trim();
        }
        catch { }
        return null;
    }
}
