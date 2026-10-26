using System.Text.Json;

namespace SaveLink.Services;

/// <summary>
/// The user's preferences, stored as JSON in %LOCALAPPDATA%\SaveLink\settings.json. In the Store (MSIX)
/// version Windows quietly redirects that folder into the app's private storage, which is fine for settings.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Where saved .ico files go. Null means <see cref="DefaultIconFolder"/>.</summary>
    public string? IconFolder { get; set; }

    /// <summary>Full path of the preferred browser's exe. Null means the system default browser.</summary>
    public string? BrowserExe { get; set; }

    /// <summary>Whether new shortcuts open in their own app window by default.</summary>
    public bool AppWindow { get; set; }

    /// <summary>
    /// Documents\SaveLink\Icons. Deliberately not in AppData: the Store version's AppData is private to the
    /// app, so Explorer couldn't see icons there, and uninstalling would delete them from under the shortcuts.
    /// </summary>
    public static string DefaultIconFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveLink", "Icons");

    public string EffectiveIconFolder => string.IsNullOrWhiteSpace(IconFolder) ? DefaultIconFolder : IconFolder;

    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveLink", "settings.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Json) ?? new AppSettings();
        }
        catch { /* unreadable or corrupt - start from defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch { /* settings are a convenience; never fail the app over them */ }
    }
}
