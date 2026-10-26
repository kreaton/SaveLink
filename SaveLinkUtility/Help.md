# SaveLinkUtility

Turns a web address into a desktop shortcut that carries the site's own icon.

## Getting started

1. Paste a link into **Website URL**, or drag a link, selected text or a `.url` file onto the window.
   If the clipboard holds a link when the program starts, it is filled in for you.
2. The page title and icon are looked up automatically. Change the name if you like.
3. Click **Create Shortcut**, or press `Enter`.

## Icon

The icon is taken from the website: its apple-touch-icon, a declared icon, or `favicon.ico`.
The links under the preview change it:

- **Choose file…** uses your own image (png, jpg, gif, bmp or ico). SVG is not supported.
- **Generate** makes a simple letter tile from the site name.
- **Reset** goes back to the icon from the website.
- **Save as .ico…** saves the current icon as a multi-size `.ico` file wherever you choose.

## Opening the shortcut

- **Default browser** creates a `.url` file. It opens in whichever browser is your default at the time you click it.
- **A specific browser** creates a `.lnk` that always opens in that browser, even if you change the default later.
- **Open in its own app window** starts the site without tabs or address bar, with its own taskbar button.
  This only works with Chromium-based browsers: Edge, Chrome, Brave and Vivaldi. Firefox has no such mode.

## Saving

- **Save location** is the Desktop by default. Use **Browse…** to pick another folder.
- **Save icon locally** stores the icon in `%LOCALAPPDATA%\SaveLinkUtility\Icons`.
  The shortcut points at that file, so keep the folder.
- **Create shortcut** can be unticked to only save the icon.

After creating, **Show in folder** reveals the new file in Explorer.

## Keyboard and command line

- `F1` opens this page, `Esc` goes back, `Ctrl+Tab` switches tabs.
- `SaveLinkUtility.exe https://example.com` starts with that address filled in.
