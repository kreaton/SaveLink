# Builds the Microsoft Store package: artifacts\msix\SaveLink_<version>.msixbundle (x64 + ARM64).
#
#   .\Packaging\Build-Msix.ps1 -BuildNumber 57  build the bundle for Store upload, as version 1.0.57
#   .\Packaging\Build-Msix.ps1 -Install         also install the x64 build locally to try it out
#                                               (needs Developer Mode: Settings > System > For developers)
#
# The version is Major.Minor from Directory.Build.props plus -BuildNumber (use the latest GitLab pipeline
# number, so it matches what CI built). The Store needs a 4th part of 0, added here, and every upload
# must have a higher version than the last.
# The package is unsigned: the Store signs it on upload. Needs the .NET 10 SDK and the Windows SDK.

param(
    [int]$BuildNumber = 0,
    [string[]]$Platforms = @('x64', 'arm64'),
    [switch]$Install
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'SaveLink\SaveLink.csproj'
$out = Join-Path $root 'artifacts\msix'

# Windows only installs an unsigned package with Developer Mode on (otherwise: HRESULT 0x80073CFF).
# Check before spending minutes on the build.
if ($Install) {
    $unlock = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -ErrorAction SilentlyContinue
    if ($unlock.AllowDevelopmentWithoutDevLicense -ne 1) {
        throw 'Developer Mode is off, so Windows refuses to install an unsigned package. Turn it on under Settings > System > For developers > Developer Mode, then run this again.'
    }
}

$version = (dotnet msbuild $project -getProperty:Version "-p:BuildNumber=$BuildNumber").Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Unexpected version '$version' - check MajorMinor in Directory.Build.props." }
$packageVersion = "$version.0"

# makeappx and makepri ship with the Windows SDK; use the newest installed one.
$sdkBin = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.*" -Directory |
    Sort-Object { [version]$_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'x64' } |
    Where-Object { Test-Path (Join-Path $_ 'makeappx.exe') } |
    Select-Object -First 1
if (-not $sdkBin) { throw 'Windows SDK not found (makeappx.exe). Install it with the Visual Studio Installer or winget install Microsoft.WindowsSDK.10.0.26100.' }
$makeappx = Join-Path $sdkBin 'makeappx.exe'
$makepri = Join-Path $sdkBin 'makepri.exe'

function Invoke-Tool([string]$exe, [string[]]$arguments) {
    # These tools are chatty; only show what they said if they fail.
    $output = & $exe @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $output | Out-Host
        throw "$([IO.Path]::GetFileName($exe)) failed with exit code $LASTEXITCODE."
    }
}

Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
$packages = Join-Path $out 'packages'
New-Item $packages -ItemType Directory | Out-Null

# resources.pri tells Windows which logo file to use for each size. It only indexes the Assets folder, so it
# is the same for every platform and is built once.
$priSource = Join-Path $out 'pri'
New-Item $priSource -ItemType Directory | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'Assets') (Join-Path $priSource 'Assets') -Recurse
$manifestTemplate = Get-Content (Join-Path $PSScriptRoot 'Package.appxmanifest') -Raw
$manifestTemplate.Replace('$Version$', $packageVersion).Replace('$Arch$', 'x64') |
    Set-Content (Join-Path $priSource 'AppxManifest.xml') -Encoding utf8
Invoke-Tool $makepri @('createconfig', '/cf', "$out\priconfig.xml", '/dq', 'en-US', '/pv', '10.0.0', '/o')
# By default makepri splits each scale into its own resources.scale-NNN.pri (for Store resource packs);
# keep everything in the one resources.pri instead.
$priConfig = [xml](Get-Content "$out\priconfig.xml")
$priConfig.SelectNodes('//packaging') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
$priConfig.Save("$out\priconfig.xml")
Invoke-Tool $makepri @('new', '/pr', $priSource, '/cf', "$out\priconfig.xml", '/mn', "$priSource\AppxManifest.xml", '/of', "$out\resources.pri", '/o')

foreach ($platform in $Platforms) {
    Write-Host "`n=== $platform ===" -ForegroundColor Cyan
    $layout = Join-Path $out "layout-$platform"

    dotnet publish $project -c Release -r "win-$platform" --self-contained true -p:PublishSingleFile=false -p:DebugType=none "-p:BuildNumber=$BuildNumber" -o $layout -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $platform." }

    Copy-Item (Join-Path $PSScriptRoot 'Assets') (Join-Path $layout 'Assets') -Recurse
    Copy-Item "$out\resources.pri" $layout
    $manifestTemplate.Replace('$Version$', $packageVersion).Replace('$Arch$', $platform) |
        Set-Content (Join-Path $layout 'AppxManifest.xml') -Encoding utf8

    Invoke-Tool $makeappx @('pack', '/d', $layout, '/p', "$packages\SaveLink_${packageVersion}_$platform.msix", '/o')
}

$bundle = Join-Path $out "SaveLink_$packageVersion.msixbundle"
Invoke-Tool $makeappx @('bundle', '/d', $packages, '/p', $bundle, '/bv', $packageVersion, '/o')
Write-Host "`nStore package: $bundle" -ForegroundColor Green

if ($Install) {
    # Registers the unpacked x64 layout, which Windows allows unsigned when Developer Mode is on.
    Add-AppxPackage -Register (Join-Path $out 'layout-x64\AppxManifest.xml') -ForceApplicationShutdown
    Write-Host 'Installed - SaveLink is in the Start menu. Remove it again with: Get-AppxPackage 19966Kreaton.dk.SaveLink | Remove-AppxPackage'
}
