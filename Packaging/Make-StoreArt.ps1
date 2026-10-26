# Generates the Store listing logos in Packaging\StoreArt, for Partner Center > Store listings > Store logos:
#   BoxArt-2160.png     1:1 box art (2160 x 2160)
#   PosterArt-1440x2160.png  2:3 poster art (1440 x 2160), with the app name
#   AppTile-300.png     1:1 app tile icon (300 x 300)
# Run it again whenever the logo changes:  .\Packaging\Make-StoreArt.ps1
#
# docs\images\logo.png is only 256 px, too small to scale up to 2160 px, so the logo is redrawn here as
# shapes, in the coordinates of that 256 px image. Keep the two in step if the design changes.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot 'StoreArt'
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item $out -ItemType Directory | Out-Null

$blue  = [System.Drawing.Color]::FromArgb(0x22, 0x6D, 0xE2)   # plate, top left
$teal  = [System.Drawing.Color]::FromArgb(0x16, 0xAD, 0xAE)   # plate, bottom right
$badge = [System.Drawing.Color]::FromArgb(0x1E, 0x64, 0xDC)   # arrow and its ring
$white = [System.Drawing.Color]::White

function New-Canvas([int]$width, [int]$height) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.CompositingQuality = 'HighQuality'
    $g.TextRenderingHint = 'AntiAlias'
    return $bmp, $g
}

function New-PlateBrush([System.Drawing.RectangleF]$area) {
    New-Object System.Drawing.Drawing2D.LinearGradientBrush $area, $blue, $teal, 45.0
}

# The globe with its arrow badge, without the plate. $brush is the plate's brush: the badge is cut out of the
# globe with a thin gap in that colour. Drawn so the logo's (x, y) lands at ($ox + x * $s, $oy + y * $s).
function Draw-Glyph($g, [float]$ox, [float]$oy, [float]$s, $brush) {
    $state = $g.Save()
    $g.TranslateTransform($ox, $oy)
    $g.ScaleTransform($s, $s)

    # Globe: outline, two meridian curves, the central meridian and three parallels, clipped to the circle.
    $pen = New-Object System.Drawing.Pen $white, 9.5
    $cx = 113; $cy = 120; $r = 64
    $clip = New-Object System.Drawing.Drawing2D.GraphicsPath
    $clip.AddEllipse($cx - $r, $cy - $r, 2 * $r, 2 * $r)
    $g.SetClip($clip, 'Intersect')
    $g.DrawEllipse($pen, $cx - 29, $cy - $r, 58, 2 * $r)
    $g.DrawLine($pen, $cx, $cy - $r, $cx, $cy + $r)
    foreach ($y in 82, 120, 158) { $g.DrawLine($pen, $cx - $r, $y, $cx + $r, $y) }
    $g.ResetClip()
    $g.DrawEllipse($pen, $cx - $r, $cy - $r, 2 * $r, 2 * $r)
    $pen.Dispose(); $clip.Dispose()

    # Badge: a gap in the plate colour, a blue ring, white inside, and an arrow pointing up and to the right.
    # The gap is filled without the scaling, so the gradient lines up with the background around it.
    $bx = 184; $by = 186
    $g.Restore($state)
    $g.FillEllipse($brush, $ox + ($bx - 52) * $s, $oy + ($by - 52) * $s, 104 * $s, 104 * $s)
    $state = $g.Save()
    $g.TranslateTransform($ox, $oy)
    $g.ScaleTransform($s, $s)
    $ring = New-Object System.Drawing.SolidBrush $badge
    $g.FillEllipse($ring, $bx - 48, $by - 48, 96, 96)
    $fill = New-Object System.Drawing.SolidBrush $white
    $g.FillEllipse($fill, $bx - 43, $by - 43, 86, 86)
    $shaft = New-Object System.Drawing.Pen $badge, 14
    $g.DrawLine($shaft, 162, 210, 192, 180)
    $head = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF 209, 162),
        (New-Object System.Drawing.PointF 172, 166),
        (New-Object System.Drawing.PointF 205, 199))
    $g.FillPolygon($ring, $head)
    $ring.Dispose(); $fill.Dispose(); $shaft.Dispose()

    $g.Restore($state)
}

# The glyph's extent in logo coordinates, used to centre it.
$glyphLeft = 43; $glyphTop = 51; $glyphWidth = 189; $glyphHeight = 186

# Square art: the plate fills the whole image (the Store rounds the corners itself), glyph centred.
function Save-Square([int]$size, [string]$name) {
    $bmp, $g = New-Canvas $size $size
    $brush = New-PlateBrush (New-Object System.Drawing.RectangleF 0, 0, $size, $size)
    $g.FillRectangle($brush, 0, 0, $size, $size)
    $s = $size * 0.62 / $glyphWidth
    Draw-Glyph $g (($size - $glyphWidth * $s) / 2 - $glyphLeft * $s) (($size - $glyphHeight * $s) / 2 - $glyphTop * $s) $s $brush
    $g.Dispose(); $brush.Dispose()
    $bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# Poster art: the glyph in the upper part, the name and a one-line description below it.
function Save-Poster([int]$width, [int]$height, [string]$name) {
    $bmp, $g = New-Canvas $width $height
    $brush = New-PlateBrush (New-Object System.Drawing.RectangleF 0, 0, $width, $height)
    $g.FillRectangle($brush, 0, 0, $width, $height)
    $s = $width * 0.6 / $glyphWidth
    Draw-Glyph $g (($width - $glyphWidth * $s) / 2 - $glyphLeft * $s) ($height * 0.2 - $glyphTop * $s) $s $brush

    $center = New-Object System.Drawing.StringFormat
    $center.Alignment = 'Center'
    $text = New-Object System.Drawing.SolidBrush $white
    $title = New-Object System.Drawing.Font 'Segoe UI Semibold', ($width * 0.115), ([System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString('SaveLink', $title, $text, (New-Object System.Drawing.RectangleF 0, ($height * 0.66), $width, ($height * 0.12)), $center)
    $sub = New-Object System.Drawing.Font 'Segoe UI', ($width * 0.042), ([System.Drawing.GraphicsUnit]::Pixel)
    $g.DrawString("Web links as desktop shortcuts`nwith the site's own icon", $sub, $text, (New-Object System.Drawing.RectangleF ($width * 0.08), ($height * 0.79), ($width * 0.84), ($height * 0.12)), $center)
    $title.Dispose(); $sub.Dispose(); $text.Dispose(); $center.Dispose()

    $g.Dispose(); $brush.Dispose()
    $bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

Save-Square 2160 'BoxArt-2160.png'
Save-Square 300 'AppTile-300.png'
Save-Poster 1440 2160 'PosterArt-1440x2160.png'

# A 256 px copy with the rounded plate, to compare against docs\images\logo.png when changing the drawing.
$bmp, $g = New-Canvas 256 256
$brush = New-PlateBrush (New-Object System.Drawing.RectangleF 14, 11, 230, 231)
$plate = New-Object System.Drawing.Drawing2D.GraphicsPath
$d = 112
$plate.AddArc(14, 11, $d, $d, 180, 90); $plate.AddArc(244 - $d, 11, $d, $d, 270, 90)
$plate.AddArc(244 - $d, 242 - $d, $d, $d, 0, 90); $plate.AddArc(14, 242 - $d, $d, $d, 90, 90)
$plate.CloseFigure()
$g.FillPath($brush, $plate)
Draw-Glyph $g 0 0 1 $brush
$g.Dispose(); $brush.Dispose(); $plate.Dispose()
$bmp.Save((Join-Path $out 'logo-check-256.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Get-ChildItem $out | Select-Object Name, Length
