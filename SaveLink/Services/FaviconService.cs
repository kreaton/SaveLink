using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SaveLink.Services;

public sealed record PageInfo(string? Title, Bitmap? Icon, Uri? IconUrl);

/// <summary>
/// Downloads a page, reads its title, and finds the best icon:
/// apple-touch-icon / larger declared icons first, then /favicon.ico, then Google's favicon service.
/// </summary>
public sealed class FaviconService
{
    // The last-resort fallback asks Google's favicon service, which tells Google which domain you looked up.
    // Set to false to never contact anyone but the site itself.
    private const bool UseGoogleFallback = true;

    private const int MaxBytes = 5 * 1024 * 1024;
    private const int MaxHtmlChars = 600_000;

    private static readonly HttpClient Http = CreateClient();

    private static readonly Regex LinkTag = new(@"<link\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Attr = new(
        @"([a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>]+))",
        RegexOptions.Compiled);
    private static readonly Regex TitleRx = new(
        @"<title[^>]*>(.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private sealed record Candidate(Uri? Url, byte[]? Data, int Score);

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,*/*;q=0.8");
        return client;
    }

    public async Task<PageInfo> GetAsync(Uri pageUri, CancellationToken ct)
    {
        using var overall = CancellationTokenSource.CreateLinkedTokenSource(ct);
        overall.CancelAfter(TimeSpan.FromSeconds(25));
        var token = overall.Token;

        string? html = null;
        var baseUri = pageUri;

        try
        {
            using var resp = await Http.GetAsync(pageUri, token);
            baseUri = resp.RequestMessage?.RequestUri ?? pageUri;
            var mediaType = resp.Content.Headers.ContentType?.MediaType;
            if (resp.IsSuccessStatusCode && (mediaType == null || mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)))
            {
                html = await resp.Content.ReadAsStringAsync(token);
                if (html.Length > MaxHtmlChars) html = html[..MaxHtmlChars];
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { /* site blocked us or is down - still try the standard icon locations */ }

        var title = html != null ? ExtractTitle(html) : null;
        var candidates = new List<Candidate>();
        if (html != null) candidates.AddRange(FindIconLinks(html, baseUri));

        candidates.Add(new Candidate(new Uri(baseUri, "/favicon.ico"), null, 1));
        if (UseGoogleFallback)
        {
            var origin = baseUri.GetLeftPart(UriPartial.Authority);
            candidates.Add(new Candidate(
                new Uri($"https://www.google.com/s2/favicons?sz=128&domain_url={Uri.EscapeDataString(origin)}"), null, 0));
        }

        var tried = new HashSet<string>();
        foreach (var c in candidates.OrderByDescending(c => c.Score).Take(8))
        {
            ct.ThrowIfCancellationRequested();
            if (token.IsCancellationRequested) break;
            if (c.Url != null && !tried.Add(c.Url.AbsoluteUri)) continue;

            try
            {
                var bytes = c.Data ?? await DownloadAsync(c.Url!, token);
                if (bytes == null) continue;

                var bmp = IconTools.TryDecode(bytes);
                if (bmp == null) continue;
                if (Math.Min(bmp.Width, bmp.Height) < 16) { bmp.Dispose(); continue; }

                return new PageInfo(title, bmp, c.Url);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { /* try the next candidate */ }
        }

        return new PageInfo(title, null, null);
    }

    private static async Task<byte[]?> DownloadAsync(Uri url, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode) return null;
        if (resp.Content.Headers.ContentLength > MaxBytes) return null;
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        return bytes.Length is > 32 and <= MaxBytes ? bytes : null;
    }

    private static string? ExtractTitle(string html)
    {
        var m = TitleRx.Match(html);
        if (!m.Success) return null;
        var t = Regex.Replace(WebUtility.HtmlDecode(m.Groups[1].Value), @"\s+", " ").Trim();
        if (t.Length == 0) return null;
        return t.Length > 80 ? t[..80].TrimEnd() : t;
    }

    private static IEnumerable<Candidate> FindIconLinks(string html, Uri baseUri)
    {
        foreach (Match m in LinkTag.Matches(html))
        {
            var attrs = ParseAttributes(m.Value);
            if (!attrs.TryGetValue("rel", out var rel) || !attrs.TryGetValue("href", out var hrefRaw)) continue;

            var rels = rel.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool apple = rels.Any(r => r.StartsWith("apple-touch-icon", StringComparison.Ordinal));
            bool icon = rels.Contains("icon");
            if (!apple && !icon) continue; // skips mask-icon, manifest, stylesheet...

            var href = WebUtility.HtmlDecode(hrefRaw.Trim());
            attrs.TryGetValue("type", out var type);
            if (type != null && type.Contains("svg", StringComparison.OrdinalIgnoreCase)) continue;

            int size = ParseSize(attrs.GetValueOrDefault("sizes"));

            // Inline data: URI icons.
            if (href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var comma = href.IndexOf(',');
                if (comma > 0 && href[..comma].Contains(";base64", StringComparison.OrdinalIgnoreCase)
                    && !href[..comma].Contains("svg", StringComparison.OrdinalIgnoreCase))
                {
                    byte[]? data = null;
                    try { data = Convert.FromBase64String(href[(comma + 1)..]); } catch { }
                    if (data != null) yield return new Candidate(null, data, Math.Min(size == 0 ? 32 : size, 512) * 2);
                }
                continue;
            }

            if (!Uri.TryCreate(baseUri, href, out var abs)) continue;
            if (abs.Scheme != Uri.UriSchemeHttp && abs.Scheme != Uri.UriSchemeHttps) continue;
            if (abs.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) continue;

            if (size == 0)
                size = apple ? 180 : abs.AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ? 48 : 32;

            yield return new Candidate(abs, null, Math.Min(size, 512) * 2 + (apple ? 1 : 0));
        }
    }

    private static Dictionary<string, string> ParseAttributes(string tag)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match a in Attr.Matches(tag))
        {
            var value = a.Groups[2].Success ? a.Groups[2].Value
                      : a.Groups[3].Success ? a.Groups[3].Value
                      : a.Groups[4].Value;
            dict.TryAdd(a.Groups[1].Value, value);
        }
        return dict;
    }

    /// <summary>"16x16 32x32" -> 32. "any" or missing -> 0.</summary>
    private static int ParseSize(string? sizes)
    {
        if (string.IsNullOrWhiteSpace(sizes)) return 0;
        int best = 0;
        foreach (var part in sizes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var xy = part.ToLowerInvariant().Split('x');
            if (xy.Length == 2 && int.TryParse(xy[0], out var w) && int.TryParse(xy[1], out var h))
                best = Math.Max(best, Math.Min(w, h));
        }
        return best;
    }
}
