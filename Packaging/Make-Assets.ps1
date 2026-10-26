# Generates the MSIX logo images in Packaging\Assets from the app logo.
# Run it again whenever the logo changes:  .\Packaging\Make-Assets.ps1
#
# The small taskbar/Start sizes (16-48 px) are taken straight from app.ico, which has hand-made frames for
# them; everything else is scaled down from the 256 px logo. "scale-NNN" and "targetsize-NN" in the file
# names are qualifiers Windows uses to pick the right size; Build-Msix.ps1 indexes them into resources.pri.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root   = Split-Path $PSScriptRoot -Parent
$ico    = Join-Path $root 'SaveLink\app.ico'
$logo   = Join-Path $root 'docs\images\logo.png'
$assets = Join-Path $PSScriptRoot 'Assets'

Remove-Item $assets -Recurse -Force -ErrorAction SilentlyContinue
New-Item $assets -ItemType Directory | Out-Null

$source = [System.Drawing.Bitmap]::FromFile($logo)

# Draws the logo at $logoSize, centred on a transparent $width x $height canvas.
function Save-Png([int]$width, [int]$height, [int]$logoSize, [string]$name, $image = $source) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode = 'HighQuality'
    $g.SmoothingMode = 'HighQuality'
    $g.CompositingQuality = 'HighQuality'
    $x = [int](($width - $logoSize) / 2); $y = [int](($height - $logoSize) / 2)
    $g.DrawImage($image, (New-Object System.Drawing.Rectangle $x, $y, $logoSize, $logoSize))
    $g.Dispose()
    $bmp.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# App list, taskbar and title bar icon. "altform-unplated" variants are drawn without a coloured plate behind them.
foreach ($scale in 100, 200) {
    $s = 44 * $scale / 100
    Save-Png $s $s $s "Square44x44Logo.scale-$scale.png"
}
foreach ($size in 16, 24, 32, 48, 256) {
    if ($size -le 48) {
        $frame = (New-Object System.Drawing.Icon $ico, $size, $size).ToBitmap()
    } else {
        $frame = $source
    }
    Save-Png $size $size $size "Square44x44Logo.targetsize-$size.png" $frame
    Save-Png $size $size $size "Square44x44Logo.targetsize-${size}_altform-unplated.png" $frame
}

# Start menu tiles: the logo at about two thirds of the tile, like Windows' own apps.
foreach ($scale in 100, 200) {
    $f = $scale / 100
    Save-Png (150 * $f) (150 * $f) (100 * $f) "Square150x150Logo.scale-$scale.png"
    Save-Png (310 * $f) (150 * $f) (100 * $f) "Wide310x150Logo.scale-$scale.png"
    Save-Png (50 * $f) (50 * $f) (50 * $f) "StoreLogo.scale-$scale.png"
}

$source.Dispose()
Get-ChildItem $assets | Select-Object Name, Length
