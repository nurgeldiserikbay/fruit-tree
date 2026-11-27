# Builds the Google Play listing images in Builds/store-art from the clean renders
# made by Unity menu-less method FruitTree.Editor.StoreArtCapture.Run (Builds/store-art/raw).
# Usage: powershell -ExecutionPolicy Bypass -File tools/StoreArtCompose.ps1
Add-Type -AssemblyName System.Drawing
$proj = Split-Path -Parent $PSScriptRoot
$raw  = "$proj\Builds\store-art\raw"
$out  = "$proj\Builds\store-art"
New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem $out -File | Remove-Item

$fonts = New-Object Drawing.Text.PrivateFontCollection
$fonts.AddFontFile("$proj\Assets\Resources\Fonts\VarelaRound-Regular.ttf")
$family = $fonts.Families[0]

$jpeg = [Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | ? MimeType -eq 'image/jpeg'
$quality = New-Object Drawing.Imaging.EncoderParameters 1
$quality.Param[0] = New-Object Drawing.Imaging.EncoderParameter ([Drawing.Imaging.Encoder]::Quality), 94L

function New-Canvas($w, $h) {
    $b = New-Object Drawing.Bitmap $w, $h, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [Drawing.Graphics]::FromImage($b)
    $g.InterpolationMode = 'HighQualityBicubic'; $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'; $g.CompositingQuality = 'HighQuality'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    return $b, $g
}

function Draw-Text($g, $text, $size, $color, $centerX, $y, $maxWidth) {
    # One line only: shrink the size until the text fits the width.
    $probe = New-Object Drawing.Font $family, $size, ([Drawing.GraphicsUnit]::Pixel)
    $measured = $g.MeasureString($text, $probe, 100000).Width
    if ($measured -gt $maxWidth * .97) { $size = $size * $maxWidth * .97 / $measured }
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $format = New-Object Drawing.StringFormat; $format.Alignment = 'Center'
    $rect = New-Object Drawing.RectangleF ($centerX - $maxWidth / 2 - 200), $y, ($maxWidth + 400), ($size * 3)
    $path.AddString($text, $family, 0, $size, $rect, $format)
    # Soft shadow, then a thin dark outline, then the fill: readable on any sky.
    $shadow = $path.Clone(); $m = New-Object Drawing.Drawing2D.Matrix; $m.Translate(0, $size * .06); $shadow.Transform($m)
    $g.FillPath((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(110, 10, 30, 12))), $shadow)
    $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(150, 18, 45, 20)), ($size * .09); $pen.LineJoin = 'Round'
    $g.DrawPath($pen, $path)
    $g.FillPath((New-Object Drawing.SolidBrush $color), $path)
}

function Shade-Top($g, $w, $h, $share) {
    $rect = New-Object Drawing.Rectangle 0, 0, $w, ([int]($h * $share))
    $brush = New-Object Drawing.Drawing2D.LinearGradientBrush $rect, ([Drawing.Color]::FromArgb(235, 38, 88, 44)), ([Drawing.Color]::FromArgb(0, 38, 88, 44)), 90
    $g.FillRectangle($brush, $rect)
}

$white = [Drawing.Color]::White
$cream = [Drawing.Color]::FromArgb(255, 252, 232, 168)
$shots = @(
    @('01-apple-park',      'Pick every fruit',          'Rotate the tree and find them all'),
    @('07-apple-closeup',   'Tap to pick, one by one',   'A soft sound with every fruit'),
    @('02-cherry-japanese', '10 fruit trees to unlock',  'Apples, cherries, lemons and more'),
    @('04-plum-rain',       'Sun, rain and storms',      'Weather that changes as you play'),
    @('03-orange-lakeside', '8 peaceful landscapes',     'From lakesides to mountains'),
    @('05-peach-evening',   'No timers. No pressure.',   'Play at your own pace'),
    @('06-lemon-chinese',   'Clear a tree, grow the next','Every tree can bloom again')
)
$n = 1
foreach ($s in $shots) {
    $src = [Drawing.Image]::FromFile("$raw\$($s[0]).png")
    $b, $g = New-Canvas 1080 1920
    $g.DrawImage($src, 0, 0, 1080, 1920)
    Shade-Top $g 1080 1920 .3
    Draw-Text $g $s[1] 86 $white 540 92 1000
    Draw-Text $g $s[2] 46 $cream 540 212 1000
    $b.Save(("$out\screenshot-{0}.jpg" -f $n), $jpeg, $quality)
    $g.Dispose(); $b.Dispose(); $src.Dispose(); $n++
}
# The real home screen, unchanged, as the last screenshot.
$src = [Drawing.Image]::FromFile("$raw\00-home.png"); $b, $g = New-Canvas 1080 1920
$g.DrawImage($src, 0, 0, 1080, 1920); $b.Save(("$out\screenshot-{0}.jpg" -f $n), $jpeg, $quality)
$g.Dispose(); $b.Dispose(); $src.Dispose()

# Feature graphic 1024x500: scene, a soft panel on the left, logo and tagline.
$src = [Drawing.Image]::FromFile("$raw\feature-wide.png"); $b, $g = New-Canvas 1024 500
$g.DrawImage($src, 0, 0, 1024, 500)
$rect = New-Object Drawing.Rectangle 0, 0, 620, 500
$brush = New-Object Drawing.Drawing2D.LinearGradientBrush $rect, ([Drawing.Color]::FromArgb(150, 22, 52, 28)), ([Drawing.Color]::FromArgb(0, 22, 52, 28)), 0
$g.FillRectangle($brush, $rect)
$logo = [Drawing.Image]::FromFile("$proj\Assets\Resources\Branding\FruitTreeLogo.png")
$lw = 440; $lh = [int]($logo.Height * $lw / $logo.Width)
$g.DrawImage($logo, 22, 110, $lw, $lh)
Draw-Text $g 'Relax and pick every fruit' 32 $white 242 (110 + $lh + 8) 470
$b.Save("$out\feature-graphic-1024x500.jpg", $jpeg, $quality)
$g.Dispose(); $b.Dispose(); $src.Dispose(); $logo.Dispose()

# App icon 512x512, 32-bit PNG.
$src = [Drawing.Image]::FromFile("$proj\Assets\Art\FruitTreeAppIcon.png")
$b = New-Object Drawing.Bitmap 512, 512, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [Drawing.Graphics]::FromImage($b); $g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'
$g.DrawImage($src, 0, 0, 512, 512); $b.Save("$out\icon-512.png", [Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $b.Dispose(); $src.Dispose()

Get-ChildItem $out -File | % { "{0}  {1} KB" -f $_.Name, [math]::Round($_.Length / 1KB) }



