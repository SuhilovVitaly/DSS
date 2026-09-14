param(
    [Parameter(Mandatory=$true)][string]$Manifest,
    [switch]$Install
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$manifestPath = (Resolve-Path -LiteralPath $Manifest).Path
$assetRoot = Split-Path -Parent $manifestPath
$projectRoot = Split-Path -Parent $PSScriptRoot
$runtimeRoot = Join-Path $projectRoot 'src/DeepSpaceSaga.Client/Images/UI/GameSessionScreenUI'
$entries = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

function Export-Size([string]$Source, [string]$Destination, [int]$Width, [int]$Height) {
    $sourceImage = [System.Drawing.Image]::FromFile($Source)
    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $attributes = [System.Drawing.Imaging.ImageAttributes]::new()
    try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $rect = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
        $graphics.DrawImage($sourceImage, $rect, 0, 0, $sourceImage.Width, $sourceImage.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)
        [void][System.IO.Directory]::CreateDirectory((Split-Path -Parent $Destination))
        $bitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $attributes.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $sourceImage.Dispose() }
}

foreach ($entry in $entries) {
    $source = Join-Path $assetRoot $entry.master
    $export = Join-Path (Join-Path $assetRoot 'GameSessionScreenUI') $entry.file
    Export-Size $source $export $entry.width $entry.height
    if ($Install) {
        $destination = Join-Path $runtimeRoot $entry.file
        [void][System.IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath $export -Destination $destination -Force
    }
}

# Contact sheet uses the exported PNGs, so it shows what the application loads.
$columns = 4
$cellWidth = 320
$cellHeight = 142
$rows = [int][Math]::Ceiling($entries.Count / [double]$columns)
$sheet = [System.Drawing.Bitmap]::new($columns * $cellWidth + 48, $rows * $cellHeight + 112)
$canvas = [System.Drawing.Graphics]::FromImage($sheet)
$font = [System.Drawing.Font]::new('Segoe UI', 10)
$heading = [System.Drawing.Font]::new('Segoe UI', 20, [System.Drawing.FontStyle]::Bold)
$textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#E6EFF5'))
$mutedBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#93ADBE'))
try {
    $canvas.Clear([System.Drawing.ColorTranslator]::FromHtml('#09121B'))
    $canvas.DrawString('DEEP SPACE SAGA / TACTICAL CONTROLS', $heading, $textBrush, 24, 16)
    $canvas.DrawString('Trade UI palette / normal + hover / original runtime sizes', $font, $mutedBrush, 24, 54)
    for ($i=0; $i -lt $entries.Count; $i++) {
        $entry = $entries[$i]
        $x = 24 + ($i % $columns) * $cellWidth
        $y = 100 + [int][Math]::Floor($i / $columns) * $cellHeight
        $png = [System.Drawing.Image]::FromFile((Join-Path (Join-Path $assetRoot 'GameSessionScreenUI') $entry.file))
        try {
            $previewSize = if ($entry.width -le 64) { [int]$entry.width } else { 64 }
            $canvas.DrawImage($png, $x, $y, $previewSize, $previewSize)
            if ($entry.width -eq 64) { $canvas.DrawImage($png, $x + 76, $y + 8, 48, 48) }
        } finally { $png.Dispose() }
        $canvas.DrawString($entry.label, $font, $textBrush, $x, $y + 68)
        $canvas.DrawString([System.IO.Path]::GetFileName($entry.file), $font, $mutedBrush, $x, $y + 90)
    }
    $sheet.Save((Join-Path $assetRoot 'tactical-buttons-contact-sheet.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally { $mutedBrush.Dispose(); $textBrush.Dispose(); $heading.Dispose(); $font.Dispose(); $canvas.Dispose(); $sheet.Dispose() }
Write-Output "Exported $($entries.Count) button images. Installed: $Install"
