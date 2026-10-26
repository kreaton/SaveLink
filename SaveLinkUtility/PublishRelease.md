dotnet publish -c Release -o bin/Publish
pause

# Single file publish

There are two ways to do it, depending on whether the other PC has .NET installed. Run the commands from the solution folder.

**Truly portable (self-contained):** this bundles the .NET runtime, so it runs on any Windows 10/11 x64 machine with nothing installed.

```
dotnet publish SaveLinkUtility -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none
```

**Small (framework-dependent):** this is the command already in the README. The exe is only a few MB, but the target PC needs the .NET 10 Desktop Runtime.

```
dotnet publish SaveLinkUtility -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none
```

Either way, the exe ends up in `SaveLinkUtility\bin\Release\net10.0-windows\win-x64\publish\SaveLinkUtility.exe`. For an ARM64 PC, use `win-arm64` instead of `win-x64`.

To make a plain `dotnet publish -c Release` do the self-contained build, add these lines to the `<PropertyGroup>` in `SaveLinkUtility.csproj`:

```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<DebugType>none</DebugType>
```

- **Size:** the self-contained exe is tens of MB, because it carries the runtime and WinForms. Compression shrinks it but adds a small delay to startup. Remove the compression line if you'd rather have a faster launch. Don't try trimming (`PublishTrimmed`), as WinForms doesn't support it.
- **Settings and icons:** the exe is portable, but the icons it saves go to `%LOCALAPPDATA%\SaveLinkUtility\Icons` on whichever PC runs it. Shortcuts point at those files, so a shortcut created on one PC won't show its icon if you copy it to another.
- **SmartScreen:** an unsigned exe may show a "Windows protected your PC" warning on first run, because it isn't code-signed. "More info", then "Run anyway", gets past it.

Want me to put those lines into the `.csproj` and send you a new zip?



## The easiest way to get .Net 10 is winget, which is built into Windows 10/11. In a terminal, run:

```
winget install Microsoft.DotNet.DesktopRuntime.10
```

The package ID matches the listing on [wingetly.io](https://www.wingetly.io/apps/microsoft/dot-net-desktop-runtime-10); I only saw the listing, so I haven't confirmed that the command itself runs. It installs the .NET 10 Desktop Runtime, which is the one WinForms apps need. The plain ".NET Runtime" and the "ASP.NET Core Runtime" won't work for this app. Pick the x64 build unless the PC is ARM.

If you'd rather not use a terminal, download the **Desktop Runtime** installer from the .NET 10 download page at dotnet.microsoft.com and run it. It's a normal next-next-finish installer.

Microsoft ships runtime patch updates, and the Desktop Runtime updates through Windows Update only if you've opted into Microsoft product updates. Otherwise, re-run `winget upgrade Microsoft.DotNet.DesktopRuntime.10` now and then.

If you're installing on several PCs, or don't want to deal with this at all, the self-contained build from my last message avoids the install entirely.

Sources:
- [.NET Desktop Runtime 10 on wingetly.io](https://www.wingetly.io/apps/microsoft/dot-net-desktop-runtime-10)
- [.NET Desktop Runtime 10.0 silent install guide](https://silentinstallhq.com/net-desktop-runtime-10-0-silent-install-how-to-guide/)