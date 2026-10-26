namespace SaveLinkUtility.Services;

public static class FileNames
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Makes any string safe to use as a Windows file name (without extension).</summary>
    public static string Sanitize(string? name, string fallback = "Website")
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((name ?? "").Where(c => !invalid.Contains(c)).ToArray())
            .Trim()
            .TrimEnd('.', ' ');

        if (cleaned.Length > 80) cleaned = cleaned[..80].TrimEnd('.', ' ');
        if (cleaned.Length == 0) cleaned = fallback;
        if (Reserved.Contains(cleaned)) cleaned += "_";
        return cleaned;
    }

    /// <summary>Returns "name.ext", or "name (2).ext", "name (3).ext"... if it already exists.</summary>
    public static string Unique(string folder, string baseName, string extension)
    {
        var path = Path.Combine(folder, baseName + extension);
        for (var i = 2; File.Exists(path); i++)
            path = Path.Combine(folder, $"{baseName} ({i}){extension}");
        return path;
    }
}
