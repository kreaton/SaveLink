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
- Installed browsers are read from `Clients\StartMenuInternet` (HKCU and HKLM, both registry views) - the same list Windows uses for "Default apps". The default browser comes from the `https` association (`AssocQueryString`). Firefox is listed but has no app-window mode (removed in Firefox 86).
- Help is the second tab (F1; Esc goes back). The text lives in `HelpPage.cs`, with `**bold**` and `` `code` `` as the only markup.
- SVG icons are not supported (System.Drawing can't decode them); such sites fall through to the next candidate or the generated icon.
