param(
    [Parameter(Mandatory)][string]$SourcePath,
    [Parameter(Mandatory)][double]$HeadWidth,
    [Parameter(Mandatory)][double]$HeadHeight,
    [Parameter(Mandatory)][double]$ChinY,
    [string]$ProjectRoot = 'D:/DeepSpaceSaga/DSS',
    [ValidateSet('female','male')][string]$Gender = 'female'
)
$ErrorActionPreference = 'Stop'
$pack = if ($Gender -eq 'male') { 'M4' } else { 'W4' }
$minWidth = if ($Gender -eq 'male') { 350 } else { 355 }
$maxWidth = if ($Gender -eq 'male') { 410 } else { 390 }
if ($HeadWidth -lt $minWidth -or $HeadWidth -gt $maxWidth) { throw "Anatomical head width must be $minWidth..$maxWidth px, excluding hair and ears." }
if ($HeadHeight -lt 470 -or $HeadHeight -gt 520) { throw 'Skull-to-chin height must be 470..520px.' }
if ($ChinY -lt 540 -or $ChinY -gt 570) { throw 'Lowest chin must be at y540..570.' }
Add-Type -AssemblyName System.Drawing
$source = $null; $portrait = $null; $gallery = $null; $graphics = $null
$savedPath = $null; $previewPath = $null; $success = $false
try {
    $source = [System.Drawing.Bitmap]::new([IO.Path]::GetFullPath($SourcePath))
    if ($source.Width -ne $source.Height) { throw 'Source must be square; do not distort an image to fit the canvas.' }
    $portrait = [System.Drawing.Bitmap]::new(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($portrait)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0,0,1024,1024))
    $graphics.Dispose(); $graphics = $null
    foreach ($point in @(@(0,0),@(1023,0),@(0,1023),@(1023,1023),@(100,500),@(924,500))) {
        if ($portrait.GetPixel($point[0],$point[1]).A -ne 0) { throw 'Background is not transparent at the outer canvas; inspect the image.' }
    }
    for ($y=575; $y -le 625; $y+=2) {
        for ($x=430; $x -le 594; $x+=2) {
            if ($portrait.GetPixel($x,$y).A -lt 254) { throw "Incomplete neck at $x,$y. Regenerate the continuous neck before saving." }
        }
    }
    $gallery = [System.Drawing.Bitmap]::new(1536,512,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($gallery)
    $graphics.Clear([System.Drawing.Color]::FromArgb(28,40,54))
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    for ($n=1; $n -le 3; $n++) {
        $costume = $null; $composite = $null; $cg = $null
        try {
            $costume = [System.Drawing.Bitmap]::new((Join-Path $ProjectRoot ('src/DeepSpaceSaga.Client/Images/Persons/{0}/Clothes/clothes-{1:00}.png' -f $pack,$n)))
            $composite = [System.Drawing.Bitmap]::new(1024,1024,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $cg = [System.Drawing.Graphics]::FromImage($composite)
            $cg.Clear([System.Drawing.Color]::Transparent)
            $cg.DrawImageUnscaled($portrait,0,0); $cg.DrawImageUnscaled($costume,0,0)
            for ($y=568; $y -le 656; $y+=2) {
                for ($x=440; $x -le 580; $x+=2) {
                    if ($composite.GetPixel($x,$y).A -lt 254) { throw "Collar gap in costume $n at $x,$y." }
                }
            }
            $graphics.DrawImage($composite,[System.Drawing.Rectangle]::new(($n-1)*512,0,512,512))
        } finally {
            if ($cg) { $cg.Dispose() }; if ($composite) { $composite.Dispose() }; if ($costume) { $costume.Dispose() }
        }
    }
    $folder = Join-Path $ProjectRoot ('src/DeepSpaceSaga.Client/Images/Persons/{0}/Portraits' -f $pack)
    if (-not (Test-Path -LiteralPath $folder -PathType Container)) { throw "Portrait destination missing: $folder" }
    do {
        $suffix = ([Guid]::NewGuid().ToString('N').Substring(0,6)).ToUpperInvariant()
        $savedPath = Join-Path $folder ('CHR-{0}-{1}.png' -f (Get-Date -Format 'yyyyMMdd-HHmmss'),$suffix)
    } while (Test-Path -LiteralPath $savedPath)
    $previewPath = Join-Path ([IO.Path]::GetTempPath()) ('character-generator-{0}.png' -f [Guid]::NewGuid().ToString('N'))
    $portrait.Save($savedPath,[System.Drawing.Imaging.ImageFormat]::Png)
    $gallery.Save($previewPath,[System.Drawing.Imaging.ImageFormat]::Png)
    $success = $true
    [PSCustomObject]@{ Portrait=$savedPath; Preview=$previewPath; Gender=$Gender; Pack=$pack; HeadWidth=$HeadWidth; HeadHeight=$HeadHeight; ChinY=$ChinY; CostumesChecked=3; Note='Inspect preview visually; delete this exact temporary preview in finally.' } | ConvertTo-Json
} finally {
    if ($graphics) { $graphics.Dispose() }; if ($gallery) { $gallery.Dispose() }
    if ($portrait) { $portrait.Dispose() }; if ($source) { $source.Dispose() }
    if (-not $success) {
        if ($savedPath -and (Test-Path -LiteralPath $savedPath)) { Remove-Item -LiteralPath $savedPath }
        if ($previewPath -and (Test-Path -LiteralPath $previewPath)) { Remove-Item -LiteralPath $previewPath }
    }
}
