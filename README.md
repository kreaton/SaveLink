# SaveLinkUtility

A small WinForms (.NET 10, Windows) utility. Give it a URL (paste or drag/drop) and it:

1. fetches the page title and the best available icon (apple-touch-icon, declared `<link rel="icon">`, `/favicon.ico`, then Google's favicon service as a last resort),
2. converts the icon to a multi-size `.ico` stored in `%LOCALAPPDATA%\SaveLinkUtility\Icons`,
3. creates a shortcut on the Desktop (or any folder you choose):
   - **`.url`** file - opens in your default browser,
   - **`.lnk`** file for a specific browser - always opens in that browser, or
   - **`.lnk`** file in app mode - opens in its own window (Chromium `--app` mode: Edge / Chrome / Brave / Vivaldi; no tabs, no address bar).

The icon can be replaced with your own image or a generated letter tile, reset to the website's icon, or saved as a standalone `.ico` file.

## Build and run

Requires Windows and the .NET 10 SDK (or newer).

```
dotnet run --project SaveLinkUtility
```

or open `SaveLinkUtility.sln` in Visual Studio 2026 and press F5.

Single-file release build:

```
dotnet publish SaveLinkUtility -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## Things to know

- On startup the window pre-fills the URL from the clipboard if it holds a link. You can also launch it with a URL argument: `SaveLinkUtility.exe https://example.com`.
- You can drop a browser link, selected text, or an existing `.url` file onto the window.
- The shortcut points at the saved `.ico`, so don't delete the icon folder.
- To disable the Google favicon fallback (it reveals the domain to Google), set `UseGoogleFallback` to `false` in `Services/FaviconService.cs`.
- SVG icons are not supported (System.Drawing can't decode them); such sites fall through to the next candidate or the generated icon.

## Browsers

The **Open with** list holds "Default browser" plus every browser installed on the machine.

**How they are found** (`Services/BrowserService.cs`):

- **Installed browsers** come from `SOFTWARE\Clients\StartMenuInternet`, read in HKCU (per-user installs) and in both the 64- and 32-bit views of HKLM. Every browser that can be made the Windows default must register there, so this is the same list Windows shows under *Default apps*. No install paths are hard-coded. Each entry's `shell\open\command` gives the exe path, and the key's default value gives the display name.
- **Internet Explorer** is skipped. Windows 11 still registers it, but it only redirects to Edge.
- **The default browser** is whatever Windows uses for `https` links, found with `AssocQueryString`. That respects the user's choice in Settings without parsing the `UserChoice` registry key.

**What each choice creates:**

| Choice | File | Behaviour |
|---|---|---|
| Default browser | `.url` | Opens in whichever browser is the default *when clicked*. |
| A specific browser | `.lnk` → `browser.exe "URL"` | Always opens in that browser, even if the default changes. |
| App window | `.lnk` → `browser.exe --app="URL"` | Own window, no tabs or address bar, own taskbar button. |

**App-window support:**

| Browser | Opens links | App window |
|---|---|---|
| Edge, Chrome, Brave, Vivaldi, Chromium | Yes | Yes (`--app=`) |
| Firefox | Yes | No: its site-specific-browser mode was removed in Firefox 86 (`--kiosk` is full-screen only) |
| Opera | Yes | No: ignores `--app=` |

The app-window check is by exe name (`BrowserInfo.SupportsAppWindow`), so a new Chromium browser needs adding to that list. A `.lnk` stores the browser's full path, so it breaks if that browser is uninstalled or moved. A `.url` file never breaks this way.

## Help

Help is the second tab (F1 opens it, Esc goes back). The text is [`SaveLinkUtility/Help.md`](SaveLinkUtility/Help.md), embedded in the exe and converted to RTF by `HelpPage.cs`. Only a small Markdown subset is understood: `#` title (the paragraph after it is the subtitle), `##` headings, `1.` numbered steps, `-` bullets, plain paragraphs, `**bold**` and `` `code` ``. Lines that continue without a blank line are joined.
