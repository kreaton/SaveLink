using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Security.Cryptography;

namespace SaveLink.Services;

/// <summary>Decoding downloaded images, generating a fallback icon, and writing multi-size .ico files.</summary>
public static class IconTools
{
    private static readonly int[] IcoSizes = { 16, 32, 48, 64, 128, 256 };

    // ------------------------------------------------------------------ decoding

    /// <summary>Decodes png/jpg/gif/bmp/ico bytes into a 32-bit bitmap. For .ico the largest frame is used. SVG is not supported.</summary>
    public static Bitmap? TryDecode(byte[] data)
    {
        try
        {
            if (data.Length > 22 && data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0)
                return DecodeIco(data);

            using var ms = new MemoryStream(data);
            using var img = Image.FromStream(ms);
            return ToArgb(img);
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? DecodeIco(byte[] d)
    {
        int count = BitConverter.ToUInt16(d, 4);
        int bestW = 0, bestH = 0, bestSize = 0, bestOffset = 0;

        for (int i = 0; i < count; i++)
        {
            int p = 6 + 16 * i;
            if (p + 16 > d.Length) break;

            int w = d[p] == 0 ? 256 : d[p];
            int h = d[p + 1] == 0 ? 256 : d[p + 1];
            int size = BitConverter.ToInt32(d, p + 8);
            int offset = BitConverter.ToInt32(d, p + 12);
            if (size <= 0 || offset < 0 || offset + size > d.Length) continue;

            if (w * h > bestW * bestH)
                (bestW, bestH, bestSize, bestOffset) = (w, h, size, offset);
        }
        if (bestSize == 0) return null;

        // PNG-compressed frame (typical for 256x256).
        if (bestSize > 8 && d[bestOffset] == 0x89 && d[bestOffset + 1] == 0x50)
        {
            using var pm = new MemoryStream(d, bestOffset, bestSize);
            using var png = Image.FromStream(pm);
            return ToArgb(png);
        }

        // Classic DIB frame: let the Icon class pick it.
        using var im = new MemoryStream(d);
        using var icon = new Icon(im, bestW, bestH);
        using var bmp = icon.ToBitmap();
        return ToArgb(bmp);
    }

    private static Bitmap ToArgb(Image src)
    {
        var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        bmp.SetResolution(96, 96);
        using var g = Graphics.FromImage(bmp);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImage(src, 0, 0, src.Width, src.Height);
        return bmp;
    }

    // ------------------------------------------------------------------ fallback icon

    /// <summary>A rounded gradient tile with the site's first letter, colour derived from the host name.</summary>
    public static Bitmap CreateFallback(string host)
    {
        var name = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
        var letter = name.Length > 0 ? char.ToUpperInvariant(name[0]).ToString() : "?";

        uint hash = 2166136261;
        foreach (var c in name) hash = (hash ^ c) * 16777619;
        double hue = hash % 360;

        var bmp = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
        bmp.SetResolution(96, 96);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        using (var path = RoundedRect(new RectangleF(8, 8, 240, 240), 56))
        using (var brush = new LinearGradientBrush(
                   new Rectangle(0, 0, 256, 256), FromHsv(hue, 0.65, 0.88), FromHsv((hue + 40) % 360, 0.80, 0.62), 45f))
        {
            g.FillPath(brush, path);
        }

        using var font = new Font("Segoe UI", 130, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(letter, font, Brushes.White, new RectangleF(0, 0, 256, 256), sf);

        return bmp;
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color FromHsv(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;
        (double r, double g, double b) = h switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x),
        };
        return Color.FromArgb((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    // ------------------------------------------------------------------ writing .ico

    /// <summary>
    /// Writes the image as a multi-size .ico in <paramref name="directory"/>. The file name contains a
    /// short content hash, so two shortcuts for the same site with different icons never overwrite each other.
    /// </summary>
    public static string SaveIco(Bitmap source, string directory, string key)
    {
        Directory.CreateDirectory(directory);
        var bytes = BuildIco(source);
        var hash = Convert.ToHexString(SHA1.HashData(bytes))[..8].ToLowerInvariant();
        var path = Path.Combine(directory, $"{FileNames.Sanitize(key, "icon")}-{hash}.ico");
        if (!File.Exists(path)) File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>Writes the image as a multi-size .ico at exactly <paramref name="path"/>, overwriting it.</summary>
    public static void WriteIco(Bitmap source, string path) => File.WriteAllBytes(path, BuildIco(source));

    private static byte[] BuildIco(Bitmap source)
    {
        int max = Math.Max(source.Width, source.Height);
        var sizes = IcoSizes.Where(s => s <= 48 || s <= max).ToArray();
        var images = sizes.Select(s => RenderPng(source, s)).ToArray();

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((ushort)0);               // reserved
        bw.Write((ushort)1);               // type: icon
        bw.Write((ushort)images.Length);   // image count

        int offset = 6 + 16 * images.Length;
        for (int i = 0; i < images.Length; i++)
        {
            byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]); // 0 means 256
            bw.Write(dim);                 // width
            bw.Write(dim);                 // height
            bw.Write((byte)0);             // palette colours
            bw.Write((byte)0);             // reserved
            bw.Write((ushort)1);           // colour planes
            bw.Write((ushort)32);          // bits per pixel
            bw.Write(images[i].Length);    // data size
            bw.Write(offset);              // data offset
            offset += images[i].Length;
        }

        foreach (var img in images) bw.Write(img);
        bw.Flush();
        return ms.ToArray();
    }

    private static byte[] RenderPng(Bitmap source, int size)
    {
        using var bmp = Render(source, size);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// The image scaled to fit a size x size square, exactly as each .ico frame is made - so a preview
    /// drawn with this looks like the icon Windows will show.
    /// </summary>
    public static Bitmap Render(Bitmap source, int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        bmp.SetResolution(96, 96);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            double scale = Math.Min((double)size / source.Width, (double)size / source.Height);
            int w = Math.Max(1, (int)Math.Round(source.Width * scale));
            int h = Math.Max(1, (int)Math.Round(source.Height * scale));
            var dest = new Rectangle((size - w) / 2, (size - h) / 2, w, h);

            // TileFlipXY avoids a faint transparent halo at the image edges when scaling.
            using var attrs = new ImageAttributes();
            attrs.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(source, dest, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attrs);
        }
        return bmp;
    }
}
