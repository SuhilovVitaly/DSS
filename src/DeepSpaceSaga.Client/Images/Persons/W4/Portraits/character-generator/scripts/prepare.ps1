#requires -Version 7.0
<#
Validates and prepares a portrait plus three costume previews using cross-platform Skia.
Requires PowerShell 7+ and .NET SDK 8+. OutputDirectory defaults to the selected pack.
Linux: pwsh ./prepare.ps1 -SourcePath /absolute/head.png -HeadWidth 370 -HeadHeight 490 -ChinY 555
PreviewDirectory defaults to a unique workspace artifacts/validation run; inspect and remove that preview.
#>
param(
    [Parameter(Mandatory)][string]$SourcePath,
    [Parameter(Mandatory)][double]$HeadWidth,
    [Parameter(Mandatory)][double]$HeadHeight,
    [Parameter(Mandatory)][double]$ChinY,
    [string]$ProjectRoot,
    [ValidateSet('female','male')][string]$Gender = 'female',
    [string]$OutputDirectory,
    [string]$PreviewDirectory
)
$ErrorActionPreference = 'Stop'
if (-not $ProjectRoot) {
    $candidate = $PSScriptRoot
    while ($candidate -and -not (Test-Path -LiteralPath (Join-Path $candidate 'DeepSpaceSaga.sln'))) { $candidate = Split-Path -Parent $candidate }
    if (-not $candidate) { throw 'Cannot locate repository; specify ProjectRoot.' }
    $ProjectRoot = $candidate
}
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$pack = if ($Gender -eq 'male') { 'M4' } else { 'W4' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $ProjectRoot "src/DeepSpaceSaga.Client/Images/Persons/$pack/Portraits" }
if (-not $PreviewDirectory) {
    $run = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N')
    $PreviewDirectory = Join-Path (Split-Path -Parent $ProjectRoot) "artifacts/validation/portrait-preparation/$run"
}
$tool = Join-Path $ProjectRoot 'tools/DeepSpaceSaga.PortraitAssets/DeepSpaceSaga.PortraitAssets.csproj'
$culture = [Globalization.CultureInfo]::InvariantCulture
& dotnet run --project $tool -c Release --verbosity quiet -- prepare-portrait ([IO.Path]::GetFullPath($SourcePath)) $HeadWidth.ToString($culture) $HeadHeight.ToString($culture) $ChinY.ToString($culture) $ProjectRoot $Gender ([IO.Path]::GetFullPath($OutputDirectory)) ([IO.Path]::GetFullPath($PreviewDirectory))
if ($LASTEXITCODE -ne 0) { throw "Portrait preparation failed ($LASTEXITCODE)." }
