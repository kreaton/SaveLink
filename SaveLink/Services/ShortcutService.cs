using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace SaveLink.Services;

public static class ShortcutService
{
    static ShortcutService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // ---------------------------------------------------------------- .url (opens in the default browser)

    public static string CreateUrlFile(Uri url, string name, string folder, string? iconPath)
    {
        Directory.CreateDirectory(folder);
        var path = FileNames.Unique(folder, FileNames.Sanitize(name), ".url");

        var sb = new StringBuilder();
        sb.AppendLine("[InternetShortcut]");
        sb.AppendLine("URL=" + url.AbsoluteUri);
        if (iconPath != null)
        {
            sb.AppendLine("IconIndex=0");
            sb.AppendLine("IconFile=" + iconPath);
        }

        // .url files are classic INI files, read in the system ANSI code page.
        Encoding enc;
        try { enc = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage); }
        catch { enc = new UTF8Encoding(false); }

        File.WriteAllText(path, sb.ToString(), enc);
        return path;
    }

    // ---------------------------------------------------------------- .lnk (opens in a chosen browser)

    /// <summary>
    /// Creates a normal Windows shortcut that launches <paramref name="browser"/> with the URL.
    /// With <paramref name="appWindow"/> the browser starts in "app mode" (Chromium only):
    /// no tabs, no address bar, its own window and taskbar entry - like an iOS home-screen web app.
    /// </summary>
    public static string CreateBrowserShortcut(Uri url, string name, string folder, string? iconPath, BrowserInfo browser, bool appWindow)
    {
        Directory.CreateDirectory(folder);
        var path = FileNames.Unique(folder, FileNames.Sanitize(name), ".lnk");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is not available on this system.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic lnk = shell.CreateShortcut(path);
            try
            {
                lnk.TargetPath = browser.ExePath;
                lnk.Arguments = appWindow ? $"--app=\"{url.AbsoluteUri}\"" : $"\"{url.AbsoluteUri}\"";
                lnk.WorkingDirectory = Path.GetDirectoryName(browser.ExePath) ?? "";
                lnk.Description = name;
                if (iconPath != null) lnk.IconLocation = iconPath + ",0";
                lnk.Save();
            }
            finally
            {
                Marshal.FinalReleaseComObject(lnk);
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
        return path;
    }
}
