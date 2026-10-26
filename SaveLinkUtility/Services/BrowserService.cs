using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace SaveLinkUtility.Services;

public sealed record BrowserInfo(string Name, string ExePath)
{
    // Chromium-based browsers understand --app=URL (own window, no tabs or address bar).
    // Firefox dropped its equivalent in version 86, and Opera ignores the switch.
    private static readonly HashSet<string> AppModeExes = new(StringComparer.OrdinalIgnoreCase)
    {
        "msedge.exe", "chrome.exe", "brave.exe", "vivaldi.exe", "chromium.exe", "thorium.exe",
    };

    public bool SupportsAppWindow => AppModeExes.Contains(Path.GetFileName(ExePath));
}

/// <summary>
/// Finds installed browsers the way Windows itself does: every browser that can be made the default
/// registers under Clients\StartMenuInternet. The current default comes from the https association.
/// </summary>
public static class BrowserService
{
    private const string ClientsKey = @"SOFTWARE\Clients\StartMenuInternet";

    public static List<BrowserInfo> FindInstalled()
    {
        var found = new List<BrowserInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Per-user installs (e.g. Chrome without admin rights) live in HKCU; machine-wide ones in
        // HKLM, where 32-bit browsers register in the WOW6432Node view.
        var roots = new[]
        {
            (RegistryHive.CurrentUser, RegistryView.Default),
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
        };
        foreach (var (hive, view) in roots)
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var clients = root.OpenSubKey(ClientsKey);
                if (clients == null) continue;

                foreach (var id in clients.GetSubKeyNames())
                {
                    using var key = clients.OpenSubKey(id);
                    using var cmd = key?.OpenSubKey(@"shell\open\command");
                    var exe = ExeFromCommand(cmd?.GetValue(null) as string);
                    if (exe == null || !seen.Add(exe)) continue;
                    // Windows 11 still registers IE, but it only redirects to Edge.
                    if (Path.GetFileName(exe).Equals("iexplore.exe", StringComparison.OrdinalIgnoreCase)) continue;

                    var name = ResolveName(key?.GetValue(null) as string) ?? id;
                    found.Add(new BrowserInfo(name, exe));
                }
            }
            catch { /* unreadable hive - skip it */ }
        }

        return found.OrderBy(b => b.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>The browser that opens https links. Returns a match from <paramref name="installed"/> when possible.</summary>
    public static BrowserInfo? FindDefault(IReadOnlyList<BrowserInfo> installed)
    {
        var exe = QueryAssoc(AssocStrExecutable);
        if (exe == null || !File.Exists(exe)) return null;

        var match = installed.FirstOrDefault(b => string.Equals(b.ExePath, exe, StringComparison.OrdinalIgnoreCase));
        return match ?? new BrowserInfo(QueryAssoc(AssocStrFriendlyAppName) ?? Path.GetFileNameWithoutExtension(exe), exe);
    }

    /// <summary>"\"C:\x\b.exe\" --arg" or "C:\x\b.exe --arg" -> "C:\x\b.exe", if it exists.</summary>
    private static string? ExeFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        command = Environment.ExpandEnvironmentVariables(command.Trim());

        string path;
        if (command.StartsWith('"'))
        {
            var end = command.IndexOf('"', 1);
            path = end > 0 ? command[1..end] : command.Trim('"');
        }
        else
        {
            var end = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            path = end > 0 ? command[..(end + 4)] : command;
        }
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }

    /// <summary>Display names may be resource references like "@C:\x\b.exe,-123".</summary>
    private static string? ResolveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (!name.StartsWith('@')) return name;

        var sb = new StringBuilder(260);
        return SHLoadIndirectString(name, sb, sb.Capacity, IntPtr.Zero) == 0 ? sb.ToString() : null;
    }

    // ---------------------------------------------------------------- Win32

    private const uint AssocfIsProtocol = 0x1000;
    private const uint AssocStrExecutable = 2;
    private const uint AssocStrFriendlyAppName = 4;

    private static string? QueryAssoc(uint what)
    {
        try
        {
            uint len = 0;
            AssocQueryString(AssocfIsProtocol, what, "https", null, null, ref len);
            if (len == 0) return null;
            var sb = new StringBuilder((int)len);
            return AssocQueryString(AssocfIsProtocol, what, "https", null, sb, ref len) == 0 ? sb.ToString() : null;
        }
        catch { return null; }
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int AssocQueryString(uint flags, uint str, string pszAssoc, string? pszExtra, StringBuilder? pszOut, ref uint pcchOut);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);
}
