# Stand-alone exe (outside the Store)

For the Microsoft Store package, see "Publishing to the Microsoft Store" in the main README. This page is about a single `SaveLink.exe` you can copy to any PC. Run the commands from the solution folder.

**Self-contained (recommended):** bundles the .NET runtime, so it runs on any Windows 10/11 x64 PC with nothing installed. The exe is tens of MB.

```
dotnet publish SaveLink -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none
```

**Framework-dependent:** only a few MB, but the PC needs the .NET 10 Desktop Runtime (`winget install Microsoft.DotNet.DesktopRuntime.10`).

```
dotnet publish SaveLink -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none
```

Either way, the exe ends up in `SaveLink\bin\Release\net10.0-windows\win-x64\publish\SaveLink.exe`. For an ARM64 PC, use `win-arm64` instead of `win-x64`. `PublishRelease.bat` runs a plain `dotnet publish` into `bin\Publish`.

- **Compression** shrinks the self-contained exe but adds a small delay at startup. Leave out `EnableCompressionInSingleFile` for a faster launch.
- **Trimming** (`PublishTrimmed`) isn't supported by WinForms, so don't use it.
- **Icons** are saved on the PC that runs the app (`Documents\SaveLink\Icons` by default), and shortcuts point at them. A shortcut copied to another PC shows no icon there.
- **SmartScreen** may show "Windows protected your PC" on first run, because the exe isn't code-signed. "More info", then "Run anyway", gets past it. The Store version doesn't have this problem, since the Store signs it.
